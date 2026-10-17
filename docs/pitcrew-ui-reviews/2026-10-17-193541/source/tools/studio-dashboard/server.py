#!/usr/bin/env python3
"""PitCrew 로컬 AI 워크스페이스. 새 UI /, 이전 관제탑 /legacy.

한 주소(http://localhost:7878)에서 사이드바로 페이지 전환:
- 🏢 팀: PM+3부서 스튜디오 도면 (state.json + 세션 busy 라이트, 3초 폴링)
- 그 외: pages/*.html 에 떨어진 모든 페이지(맵 도면 등)가 자동으로 탭에 뜸

새 뷰 추가 = pages/ 에 .html 한 장 떨구면 끝. 포트 안 바뀜, 30분 타임아웃 없음.

실행: tools/studio-dashboard/start.ps1 (Claude SDK가 설치된 가상환경 사용)
"""
import json, glob, os, re, time
from urllib.parse import urlsplit, parse_qs
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

PORT = int(os.environ.get('PITCREW_PORT', '7878'))
HERE = os.path.dirname(os.path.abspath(__file__))
STATE = os.path.join(HERE, "state.json")
ACTIVITY = os.path.join(HERE, "activity.jsonl")
PAGES_DIR = os.path.join(HERE, "pages")
SESSIONS = os.path.expanduser("~/.claude/sessions")
os.makedirs(PAGES_DIR, exist_ok=True)


ROOT = os.path.dirname(os.path.dirname(HERE))
AI_LOGS = os.path.join(ROOT, ".ai_logs")
GATES = os.path.join(ROOT, "docs", "gates")


def codex_runs(limit=4):
    """2026-10-13: Codex exec 로그(.ai_logs/codex-*.log)를 피드 항목으로. 'tokens used' 가 없으면 아직 돌고 있는 것."""
    out = []
    for p in sorted(glob.glob(os.path.join(AI_LOGS, "codex-*.log")), key=os.path.getmtime, reverse=True)[:limit]:
        try:
            with open(p, encoding="utf-8", errors="replace") as f:
                txt = f.read()
            name = os.path.basename(p).replace("codex-", "").replace(".log", "")
            done = "tokens used" in txt
            last = ""
            for ln in reversed(txt.splitlines()):
                ln = ln.strip()
                if ln and not ln.startswith(("+", "-", "@@", "exec", "diff", "***")):
                    last = ln[:110]; break
            out.append({"kind": "done" if done else "start", "who": "Codex", "text": f"{name} — " + ("완료: " if done else "작업 중: ") + last, "ts": int(os.path.getmtime(p)), "src": "codex"})
        except Exception:
            pass
    return out


def gate_lines(limit=8):
    """오늘 게이트 기록(docs/gates/YYYY-MM-DD.md)의 '- HH:MM | agent | 대상 | 판정 | …' 줄을 피드로."""
    out = []
    try:
        today = time.strftime("%Y-%m-%d")
        p = os.path.join(GATES, today + ".md")
        if not os.path.exists(p):
            return out
        day = int(time.mktime(time.strptime(today, "%Y-%m-%d")))
        with open(p, encoding="utf-8") as f:
            for ln in f:
                if not ln.startswith("- "):
                    continue
                cells = [c.strip() for c in ln[2:].split("|")]
                if len(cells) < 4:
                    continue
                hhmm = cells[0].replace("x", "0")
                try:
                    h, m = hhmm.split(":"); ts = day + int(h) * 3600 + int(m) * 60
                except Exception:
                    ts = day
                out.append({"kind": "review", "who": cells[1], "text": f"{cells[2]} → {cells[3]}" + (f" · {cells[4][:90]}" if len(cells) > 4 else ""), "ts": ts, "src": "gate"})
    except Exception:
        pass
    return out[-limit:]


def read_activity(n=14):
    """activity.jsonl 마지막 n줄 (hook이 자동 적재) + Codex 실행 로그 + 오늘 게이트 기록. 최신이 위로."""
    out = []
    try:
        with open(ACTIVITY, encoding="utf-8") as f:
            lines = f.readlines()[-n:]
        for ln in lines:
            try:
                out.append(json.loads(ln))
            except Exception:
                pass
    except Exception:
        pass
    out += codex_runs() + gate_lines()
    out.sort(key=lambda r: r.get("ts", 0), reverse=True)
    return out[: n + 12]


def read_state():
    try:
        with open(STATE, encoding="utf-8") as f:
            return json.load(f)
    except Exception as e:
        return {"title": "PITCREW STUDIO", "error": str(e), "departments": []}


def session_activity():
    busy, latest = False, 0
    for p in glob.glob(os.path.join(SESSIONS, "*.json")):
        try:
            with open(p, encoding="utf-8") as f:
                s = json.load(f)
            up = s.get("updatedAt", 0)
            if up > latest:
                latest, busy = up, (s.get("status") == "busy")
        except Exception:
            pass
    age = (time.time() * 1000 - latest) / 1000 if latest else None
    if age is not None and age > 30:
        busy = False
    return {"busy": busy, "ageSec": round(age) if age is not None else None}


CONTROL_MD = os.path.join(os.path.dirname(os.path.dirname(HERE)), "docs", "CONTROL.md")
SHOTS_DIR = os.path.join(os.path.dirname(os.path.dirname(HERE)), "docs", "screenshots")


def newest_shot():
    """docs/screenshots 하위(재귀) 최신 .png 경로. 내가 작업하며 찍는 스샷이 자동 반영."""
    try:
        files = glob.glob(os.path.join(SHOTS_DIR, "**", "*.png"), recursive=True)
        return max(files, key=os.path.getmtime) if files else None
    except Exception:
        return None


def md_to_html(md):
    """의존성 0 미니 마크다운 — 헤더/표/리스트/볼드/hr/인용. CONTROL.md 정리뷰용."""
    import html as _h
    out, i, lines = [], 0, md.split("\n")
    while i < len(lines):
        ln = lines[i]
        s = ln.strip()
        if not s:
            i += 1; continue
        if s.startswith("```"):  # 코드블록 통째로
            i += 1; buf = []
            while i < len(lines) and not lines[i].strip().startswith("```"):
                buf.append(_h.escape(lines[i])); i += 1
            i += 1; out.append("<pre>" + "\n".join(buf) + "</pre>"); continue
        if re.match(r"^#{1,6}\s", s):
            n = len(s) - len(s.lstrip("#")); out.append(f"<h{n}>{inline(s[n:].strip())}</h{n}>"); i += 1; continue
        if re.match(r"^(---+|\*\*\*+)$", s):
            out.append("<hr>"); i += 1; continue
        if s.startswith(">"):
            out.append(f"<blockquote>{inline(s[1:].strip())}</blockquote>"); i += 1; continue
        if "|" in s and i + 1 < len(lines) and re.match(r"^\s*\|?[\s:|-]+\|?\s*$", lines[i + 1]):
            rows = []
            while i < len(lines) and "|" in lines[i]:
                rows.append(lines[i]); i += 1
            out.append(render_table(rows)); continue
        if re.match(r"^[-*]\s", s):
            out.append("<ul>")
            while i < len(lines) and re.match(r"^\s*[-*]\s", lines[i]):
                out.append(f"<li>{inline(lines[i].strip()[2:])}</li>"); i += 1
            out.append("</ul>"); continue
        out.append(f"<p>{inline(s)}</p>"); i += 1
    return "\n".join(out)


def inline(t):
    import html as _h
    t = _h.escape(t)
    t = re.sub(r"\*\*(.+?)\*\*", r"<b>\1</b>", t)
    t = re.sub(r"`(.+?)`", r"<code>\1</code>", t)
    return t


def render_table(rows):
    def cells(r):
        return [c.strip() for c in r.strip().strip("|").split("|")]
    head = cells(rows[0]); body = rows[2:]
    h = "".join(f"<th>{inline(c)}</th>" for c in head)
    b = "".join("<tr>" + "".join(f"<td>{inline(c)}</td>" for c in cells(r)) + "</tr>" for r in body)
    return f"<table><thead><tr>{h}</tr></thead><tbody>{b}</tbody></table>"


def control_page():
    try:
        with open(CONTROL_MD, encoding="utf-8") as f:
            body = md_to_html(f.read())
    except Exception as e:
        body = f"<p>CONTROL.md 못 읽음: {e}</p>"
    return ("<!DOCTYPE html><html lang='ko'><head><meta charset='utf-8'><style>"
            "body{margin:0;padding:20px 28px;background:#fbf8f1;color:#2b313a;"
            "font-family:system-ui,'Malgun Gothic',sans-serif;line-height:1.55;max-width:900px}"
            "h1{font-size:22px;border-bottom:2px solid #5aa469;padding-bottom:6px}"
            "h2{font-size:17px;margin-top:26px;color:#2d3b45}h3{font-size:14px;color:#4a5560}"
            "table{border-collapse:collapse;margin:10px 0;font-size:13px}"
            "th,td{border:1px solid #cfc7b4;padding:5px 9px;text-align:left}th{background:#e6e1d6}"
            "code{background:#eceadf;padding:1px 5px;border-radius:4px;font-size:12px}"
            "blockquote{border-left:3px solid #9c6f64;margin:8px 0;padding:2px 12px;color:#6a7280;background:#f1ede2}"
            "hr{border:0;border-top:1px solid #d8d2c2;margin:18px 0}pre{background:#2d3b45;color:#e6e1d6;padding:12px;border-radius:8px;overflow:auto;font-size:12px}"
            "ul{margin:6px 0}</style></head><body>" + body + "</body></html>")


def list_pages():
    """좌측 nav 목록: 팀(내장) + 관제탑 + pages/*.html (파일의 첫 <h2>를 제목으로)."""
    out = [{"id": "team", "title": "🏢 팀"}, {"id": "control", "title": "📋 관제탑"}]
    for p in sorted(glob.glob(os.path.join(PAGES_DIR, "*.html"))):
        stem = os.path.splitext(os.path.basename(p))[0]
        title = stem
        try:
            with open(p, encoding="utf-8") as f:
                head = f.read(2000)
            m = re.search(r"<h2[^>]*>(.*?)</h2>", head, re.S)
            if m:
                title = re.sub(r"<[^>]+>", "", m.group(1)).strip()[:40]
        except Exception:
            pass
        out.append({"id": "page:" + stem, "title": "🗺 " + title})
    return out


SAFE = re.compile(r"^[A-Za-z0-9_\-]+$")


UPLOAD_LIMIT = 20 * 1024 * 1024

def body_limit(path):
    return UPLOAD_LIMIT * 4 // 3 + 4096 if urlsplit(path).path == '/api/hub/upload' else 250000

class H(BaseHTTPRequestHandler):
    runtime_proxy = False
    def log_message(self, *a):
        pass

    def _send(self, body, ctype="text/html; charset=utf-8", code=200):
        b = body.encode("utf-8") if isinstance(body, str) else body
        self.send_response(code)
        self.send_header("Content-Type", ctype)
        self.send_header("Content-Length", str(len(b)))
        self.send_header("Cache-Control", "no-store")
        self.send_header("X-Content-Type-Options", "nosniff")
        self.send_header("Referrer-Policy", "same-origin")
        self.send_header("X-Frame-Options", "SAMEORIGIN")
        if urlsplit(self.path).path.startswith('/view/page/'):
            self.send_header('Content-Security-Policy', 'sandbox allow-scripts')
        self.end_headers()
        try:
            self.wfile.write(b)
        except (BrokenPipeError, ConnectionResetError):
            pass

    def _local(self):
        host = self.headers.get('Host', '')
        allowed = {f'localhost:{PORT}', f'127.0.0.1:{PORT}'}
        if host not in allowed:
            return False
        origin = self.headers.get('Origin')
        if origin and origin not in {'http://' + h for h in allowed}:
            return False
        return self.headers.get('Sec-Fetch-Site') != 'cross-site'

    _roots_cache = (0.0, [])
    def _image_roots(self):
        """Project folders + PitCrew data folder. Refreshed at most once a minute."""
        import time
        from pathlib import Path
        at, roots = H._roots_cache
        if time.time() - at < 60 and roots: return roots
        from hub import ROOT, MEDIA
        roots = [ROOT.resolve(), MEDIA.parent.resolve()]
        try:
            if self.runtime_proxy:
                from runtime import forward
                code, body = forward('/api/hub/projects', None, None, timeout=5)
                rows = json.loads(body) if code == 200 else []
            else:
                from hub import get_hub
                rows = get_hub().projects()
            roots += [Path(r['path']).resolve() for r in rows if r.get('path')]
        except Exception:
            pass
        H._roots_cache = (time.time(), roots)
        return roots

    def _local_image(self, raw):
        from pathlib import Path
        kinds = {'.png': 'image/png', '.jpg': 'image/jpeg', '.jpeg': 'image/jpeg', '.gif': 'image/gif', '.webp': 'image/webp', '.bmp': 'image/bmp'}
        try:
            from hub import ROOT
            import re as _re
            raw = raw.replace('\\', '/')
            if raw.lower().startswith('file://'):
                raw = raw[7:]
                if not raw.startswith('/') and not _re.match(r'^[A-Za-z]:/', raw):
                    return self._json({'error': '외부 공유 경로는 열 수 없습니다.'}, 403)
            if raw.startswith('//'):
                return self._json({'error': '외부 공유 경로는 열 수 없습니다.'}, 403)
            raw = _re.sub(r'^/([A-Za-z]:)', r'\1', raw)  # "/D:/x.png" from Markdown
            p = Path(raw)
            p = Path(os.path.abspath(p if p.is_absolute() else ROOT / p))
            # Check the lexical boundary before resolve/stat can touch an untrusted path.
            roots = self._image_roots()
            if not any(p.is_relative_to(r) for r in roots):
                return self._json({'error': '프로젝트 폴더 밖의 파일은 열 수 없습니다.'}, 403)
            if p.suffix.lower() not in kinds:
                return self._json({'error': '이미지를 찾을 수 없습니다.'}, 404)
            p = p.resolve()
            # Keep the second check: a project-local link can resolve outside the roots.
            if not any(p.is_relative_to(r) for r in roots):
                return self._json({'error': '프로젝트 폴더 밖의 파일은 열 수 없습니다.'}, 403)
        except (OSError, ValueError, RuntimeError):
            return self._json({'error': '경로가 올바르지 않습니다.'}, 400)
        if p.suffix.lower() not in kinds or not p.is_file():
            return self._json({'error': '이미지를 찾을 수 없습니다.'}, 404)
        if p.stat().st_size > 30 * 1024 * 1024:
            return self._json({'error': '이미지가 너무 큽니다.'}, 413)
        return self._send(p.read_bytes(), kinds[p.suffix.lower()])

    def _json(self, body, code=200):
        self._send(json.dumps(body, ensure_ascii=False), 'application/json; charset=utf-8', code)

    def _proxy_runtime(self):
        if not self._local():
            return self._json({'error': '로컬 접근만 허용됩니다.'}, 403)
        if not urlsplit(self.path).path.startswith('/api/hub/'):
            return self._json({'error': '없는 API입니다.'}, 404)
        try:
            body = None
            if self.command == 'POST':
                length = int(self.headers.get('Content-Length', '0'))
                if not 0 < length <= body_limit(self.path):
                    return self._json({'error': '요청 크기가 올바르지 않습니다.'}, 413)
                self.connection.settimeout(15)
                body = self.rfile.read(length)
            from runtime import forward
            code, result = forward(self.path, body, self.headers.get('X-PitCrew-Token'))
            return self._send(result, 'application/json; charset=utf-8', code)
        except (OSError, ValueError, KeyError, RuntimeError):
            return self._json({'error': '실행 서비스에 연결하지 못했습니다. PitCrew.cmd를 실행해 복구하세요. 전송한 작업은 상태를 확인한 뒤 다시 시도하세요.'}, 503)

    def do_POST(self):
        if self.runtime_proxy:
            return self._proxy_runtime()
        # Read bounded input before returning an auth error: on Windows, closing a
        # socket with unread request bytes can reset it and hide the HTTP status.
        try:
            length = int(self.headers.get('Content-Length', '0'))
            if not 0 < length <= body_limit(self.path):
                return self._json({'error': '요청 크기가 올바르지 않습니다.'}, 413)
            self.connection.settimeout(15)
            raw = self.rfile.read(length)
        except (OSError, ValueError):
            return self._json({'error': '요청 본문을 읽지 못했습니다.'}, 400)
        from hub import get_hub, text
        hub = get_hub()
        if not self._local() or self.headers.get('X-PitCrew-Token') != hub.token:
            return self._json({'error': '로컬 앱에서만 요청할 수 있습니다. 페이지를 새로고침하세요.'}, 403)
        try:
            d = json.loads(raw)
            if not isinstance(d, dict): raise ValueError('객체 형식의 요청이 필요합니다.')
            path = urlsplit(self.path).path
            project = parse_qs(urlsplit(self.path).query).get('project',['default'])[0]
            hub = get_hub(project)
            if path == '/api/hub/projects': result = hub.add_project(d)
            elif path == '/api/hub/agents': result = hub.save_agent(d)
            elif path == '/api/hub/goal': result = hub.goal(d['agent'],d)
            elif path == '/api/hub/file-baselines': result = hub.file_baselines(d)
            elif path == '/api/hub/teams': result = hub.save_team(d)
            elif path == '/api/hub/team-member': result = hub.change_team_member(d)
            elif path == '/api/hub/team-links': result = hub.save_team_links(d)
            elif path == '/api/hub/import': result = hub.import_sources(d)
            elif path == '/api/hub/send': result = hub.send(d['agent'], d)
            elif path == '/api/hub/stop': result = hub.stop(d['agent'])
            elif path == '/api/hub/decide': result = hub.decide(d['id'], d)
            elif path == '/api/hub/queue': result = hub.queue_action(d)
            elif path == '/api/hub/handoff': result = hub.save_handoff(d)
            elif path == '/api/hub/revoke-allow': result = hub.revoke_allow(d)
            elif path == '/api/hub/upload': result = hub.upload(d)
            elif path == '/api/hub/research': result = hub.research(d)
            elif path == '/api/hub/add-cli': result = hub.add_cli(d)
            elif path == '/api/hub/cycle':
                action = d.get('action')
                if action == 'start': result = hub.start_cycle(d)
                elif action == 'retry': result = hub.retry_cycle(d.get('id'))
                elif action == 'stop': result = hub.stop_cycle(d.get('id'))
                elif action == 'hold': result = hub.hold_cycle(d.get('id'))
                elif action == 'release': result = hub.release_cycle(d.get('id'))
                else: raise ValueError('지원하지 않는 목표 동작입니다. start, retry, stop, hold, release 중 하나를 지정하세요.')
            elif path == '/api/hub/continue-conversation': result = hub.continue_conversation(d['agent'])
            elif path == '/api/hub/resume-source': result = hub.resume_source(d['agent'])
            elif path == '/api/hub/watch': result = hub.watch(d['source'])
            elif path == '/api/hub/archive': result = hub.archive(d['agent'])
            elif path == '/api/hub/restore': result = hub.restore(d['agent'])
            elif path == '/api/hub/shared':
                hub.store.put('shared', text(d.get('text', ''), 30000))
                hub.store.activity(None, '팀 공유 컨텍스트 업데이트'); result = {'ok': True}
            else: return self._json({'error': '없는 API입니다.'}, 404)
            self._json(result)
        except (ValueError, KeyError, TypeError) as e:
            self._json({'error': str(e)}, 400)
        except Exception as e:
            self._json({'error': str(e)[:2000]}, 500)

    def do_GET(self):
        path = urlsplit(self.path).path
        if not self._local():
            return self._json({'error': '로컬 접근만 허용됩니다.'}, 403)
        if path.startswith('/api/hub/'):
            if self.runtime_proxy:
                return self._proxy_runtime()
            from hub import get_hub
            hub = get_hub(); q = parse_qs(urlsplit(self.path).query)
            try:
                if path == '/api/hub/runtime-identity':
                    import runtime_identity
                    result=runtime_identity.snapshot(getattr(hub.owner,'runtime_identity',None),os.path.dirname(__file__))
                    result['runtime']=getattr(hub.owner,'runtime',{'independent':False})
                    return self._json(result)
                if path == '/api/hub/maintenance-status':
                    from runtime import maintenance_status
                    return self._json(maintenance_status(hub))
                hub = get_hub(q.get('project',['default'])[0])
                if path == '/api/hub/projects': result = hub.projects()
                elif path == '/api/hub/runtime-health': result = getattr(hub, 'runtime', {'independent': False})
                elif path == '/api/hub/usage': result = hub.usage()
                elif path == '/api/hub/team': result = hub.team()
                elif path == '/api/hub/flow': result = hub.flow()
                elif path == '/api/hub/commands': result = hub.commands(q.get('agent', [''])[0])
                elif path == '/api/hub/goal': result = hub.goal(q.get('agent', [''])[0])
                elif path == '/api/hub/stage-report': result = hub.stage_report(q.get('id', [''])[0],q.get('stage', [''])[0])
                elif path == '/api/hub/goal-history': result = hub.goal_history(q.get('q', [''])[0],q.get('page', ['1'])[0],q.get('id', [''])[0])
                elif path == '/api/hub/file-baselines': result = hub.file_baselines(bid=q.get('id',[''])[0],goal=q.get('goal',[''])[0])
                elif path == '/api/hub/files': result = hub.project_files(q.get('q', [''])[0])
                elif path == '/api/hub/search': result = hub.search(q.get('q', [''])[0])
                elif path == '/api/hub/artifacts':
                    if q.get('browse', [''])[0] == '1':
                        result = hub.artifacts(q.get('agent', [''])[0], query=q.get('q', [''])[0], image_page=q.get('image_page', ['1'])[0],
                                               input_page=q.get('input_page', ['1'])[0], browse=True)
                    else: result = hub.artifacts(q.get('agent', [''])[0])
                elif path == '/api/hub/workspace-changes':
                    import workspace_changes
                    result = workspace_changes.snapshot(hub.root)
                elif path == '/api/hub/unity-status':
                    import unity_status  # read-only: heartbeat files + one fixed loopback query per fresh editor
                    result = unity_status.snapshot(hub.root)
                elif path == '/api/hub/unity-console':
                    import unity_console
                    result = unity_console.snapshot(hub.root, q.get('path', [''])[0])
                elif path == '/api/hub/bootstrap':
                    if 'client' in q: hub.client_report(*(x == '1' for x in (q['client'][0] + ',0').split(',')[:2]))
                    result = hub.bootstrap()
                elif path == '/api/hub/detail': result = hub.detail_view(q.get('agent', [''])[0], q.get('etag', [''])[0])
                elif path == '/api/hub/models': result = hub.models(q.get('provider', [''])[0], q.get('refresh', [''])[0] == '1')
                elif path == '/api/hub/sources': result = hub.source_catalog()
                elif path == '/api/hub/archived': result = [a for a in hub.store.agents(include_archived=True) if a.get('archived')]
                elif path == '/api/hub/message': result = hub.message(q.get('agent', [''])[0], q.get('id', [''])[0])
                elif path == '/api/hub/export': result = hub.export(q.get('agent', [''])[0])
                else: return self._json({'error': '없는 API입니다.'}, 404)
                return self._json(result)
            except (ValueError, KeyError) as e:
                return self._json({'error': str(e)}, 400)
            except Exception as e:
                return self._json({'error': str(e)[:2000]}, 500)
        if path == '/local-image':
            return self._local_image(parse_qs(urlsplit(self.path).query).get('path', [''])[0])
        if path.startswith('/media/'):
            import re
            from hub import MEDIA
            name = path[len('/media/'):]
            if not re.fullmatch(r'[0-9a-f]{32}\.(png|jpg|gif|webp)', name) or not (MEDIA / name).is_file():
                return self._json({'error': '이미지를 찾을 수 없습니다.'}, 404)
            ext = name.rsplit('.', 1)[1]
            return self._send((MEDIA / name).read_bytes(), 'image/' + ('jpeg' if ext == 'jpg' else ext))
        if path in ('/', '/hub', '/app.css', '/app.js', '/connection.js', '/teams.js', '/workspace.js', '/organize.js', '/product.css', '/reader.js', '/notify.js', '/queue.js', '/attach.js', '/composer.js', '/markdown-it.min.js'):
            filename = {'/': 'index.html', '/hub': 'index.html', '/app.css': 'app.css', '/app.js': 'app.js', '/connection.js': 'connection.js', '/teams.js': 'teams.js', '/workspace.js':'workspace.js', '/product.css':'product.css', '/organize.js':'organize.js', '/reader.js':'reader.js', '/notify.js':'notify.js', '/queue.js':'queue.js', '/attach.js':'attach.js', '/composer.js':'composer.js', '/markdown-it.min.js':'markdown-it.min.js'}[path]
            ctype = 'text/css' if filename.endswith('.css') else ('text/javascript' if filename.endswith('.js') else 'text/html')
            with open(os.path.join(HERE, 'web', filename), encoding='utf-8') as f:
                return self._send(f.read(), ctype + '; charset=utf-8')
        if path == "/" or path == "":
            self._send(SHELL)
        elif path == '/legacy':
            self._send(SHELL)
        elif path == "/api/state":
            d = read_state(); d["session"] = session_activity()
            self._send(json.dumps(d, ensure_ascii=False), "application/json; charset=utf-8")
        elif path == "/api/activity":
            self._send(json.dumps(read_activity(), ensure_ascii=False), "application/json; charset=utf-8")
        elif path == "/api/pages":
            self._send(json.dumps(list_pages(), ensure_ascii=False), "application/json; charset=utf-8")
        elif path == "/api/latest-image":
            fp = newest_shot()
            if not fp:
                return self._send("no image", code=404)
            with open(fp, "rb") as f:
                self._send(f.read(), "image/png")
        elif path == "/api/running":
            try:
                with open(os.path.join(HERE, "running.json"), encoding="utf-8") as f:
                    stack = json.load(f)
            except Exception:
                stack = []
            # 10분 넘은 stale 항목 제거(짝 못 맞춘 done 방지)
            now = time.time()
            stack = [x for x in stack if now - x.get("ts", 0) < 600]
            self._send(json.dumps(stack, ensure_ascii=False), "application/json; charset=utf-8")
        elif path == "/api/latest-image-info":
            fp = newest_shot()
            info = {"name": os.path.basename(fp), "mtime": os.path.getmtime(fp)} if fp else {"name": None, "mtime": 0}
            self._send(json.dumps(info, ensure_ascii=False), "application/json; charset=utf-8")
        elif path == "/view/team":
            self._send(TEAM_PAGE)
        elif path == "/view/control":
            self._send(control_page())
        elif path.startswith("/view/page/"):
            name = path[len("/view/page/"):]
            if not SAFE.match(name):
                return self._send("bad name", code=400)
            fp = os.path.join(PAGES_DIR, name + ".html")
            if not os.path.exists(fp):
                return self._send("not found", code=404)
            with open(fp, encoding="utf-8") as f:
                self._send(f.read())
        else:
            self._send("not found", code=404)


SHELL = r"""<!DOCTYPE html><html lang="ko"><head><meta charset="utf-8">
<title>PitCrew Hub</title><style>
 html,body{margin:0;height:100%;font-family:system-ui,'Malgun Gothic',sans-serif}
 .wrap{display:flex;height:100vh}
 .nav{width:190px;flex:none;background:#222831;color:#e6e1d6;padding:14px 0;overflow:auto}
 .nav .brand{font-size:15px;font-weight:700;letter-spacing:1px;padding:0 16px 12px;border-bottom:1px solid #3a3f47;margin-bottom:8px}
 .nav button{display:block;width:100%;text-align:left;background:none;border:0;color:#c7ccd2;font-size:14px;padding:11px 16px;cursor:pointer}
 .nav button:hover{background:#2d3b45}
 .nav button.active{background:#5aa469;color:#fff;font-weight:700}
 .nav .hint{font-size:10px;color:#6a727c;padding:14px 16px;line-height:1.5}
 .frame{flex:1;border:0;width:100%;height:100%;background:#fff}
</style></head><body>
 <div class="wrap">
   <div class="nav"><div class="brand">🏢 PITCREW HUB</div><div id="tabs"></div>
     <div class="hint">새 뷰 = pages/ 에 .html 떨구면 자동 탭.<br>포트 고정 7878 · 안 죽음.</div></div>
   <iframe id="frame" class="frame" src="/view/team"></iframe>
 </div>
<script>
let cur='team';
async function loadNav(){
  const pages=await (await fetch('/api/pages')).json();
  const t=document.getElementById('tabs');t.innerHTML='';
  pages.forEach(p=>{
    const b=document.createElement('button');b.textContent=p.title;
    b.className=(('page:'.length&&p.id===cur)||p.id===cur)?'active':'';
    b.onclick=()=>{cur=p.id;
      const src = p.id==='team' ? '/view/team' : (p.id==='control' ? '/view/control' : '/view/page/'+p.id.slice(5));
      document.getElementById('frame').src = src;
      [...t.children].forEach(c=>c.className='');b.className='active';};
    t.appendChild(b);
  });
}
loadNav();setInterval(loadNav,5000);
</script></body></html>"""


TEAM_PAGE = r"""<!DOCTYPE html><html lang="ko"><head><meta charset="utf-8"><title>팀</title><style>
 body{margin:0;background:#222831;color:#e6e1d6;font-family:system-ui,'Malgun Gothic',sans-serif}
 .top{display:flex;align-items:center;gap:14px;padding:14px 20px;background:#2d3b45}
 .top h1{font-size:17px;margin:0;letter-spacing:1px}
 .light{margin-left:auto;font-size:13px;padding:5px 12px;border-radius:20px;font-weight:700}
 .on{background:#1f5130;color:#9be8ad} .off{background:#3a3f47;color:#9aa0a8}
 .pm{margin:16px 20px;background:#2d3b45;border-radius:10px;padding:12px 16px}
 .pm .r{font-size:11px;color:#aebcc4;letter-spacing:1px} .pm .d{font-size:13px;margin-top:3px}
 .deps{display:grid;grid-template-columns:repeat(3,1fr);gap:14px;padding:0 20px 24px}
 .dep{background:#e6e1d6;color:#2b313a;border-radius:10px;overflow:hidden;border:2px solid #3a3f47}
 .dep.active{border-color:#5aa469;box-shadow:0 0 0 2px #5aa46955}
 .dep h2{font-size:13px;margin:0;padding:9px 12px;color:#fff;background:#5b7186}
 .dep.k0 h2{background:#5aa469}.dep.k1 h2{background:#5b7186}.dep.k2 h2{background:#9c6f64}
 .note{font-size:11px;color:#6a7280;padding:7px 12px 0}
 .desk{display:flex;gap:10px;align-items:flex-start;margin:9px 12px;background:#fbf8f1;border:1px solid #cfc7b4;border-radius:8px;padding:8px}
 .ava{width:28px;height:28px;border-radius:50%;background:#dfe3e8;display:flex;align-items:center;justify-content:center;font-size:14px;flex:none}
 .dk .role{font-size:10px;color:#8a8270;text-transform:uppercase}.dk .nm{font-size:12px;font-weight:700;color:#2d3b45;margin:1px 0 3px}.dk .tk{font-size:11px;color:#4a5560;line-height:1.3}
 .badge{margin-left:auto;font-size:9px;font-weight:700;padding:2px 6px;border-radius:12px;flex:none}
 .b-working{background:#fff0cf;color:#9a6b00}.b-done{background:#d6f0d8;color:#2c7a39}.b-idle{background:#e8e8e8;color:#888}
 .empty{font-size:11px;color:#9aa0a8;padding:9px 12px}
 .foot{padding:0 20px 18px;font-size:11px;color:#8a909a}
 .feed{margin:14px 20px;background:#1b212a;border-radius:10px;padding:8px 6px;max-height:188px;overflow:hidden}
 .feed .fh{font-size:11px;color:#aebcc4;letter-spacing:1px;padding:2px 12px 8px}
 .fitem{display:flex;align-items:center;gap:9px;padding:6px 12px;font-size:12px;border-left:3px solid #3a3f47;margin:2px 6px;background:#222b35;border-radius:6px;animation:slidein .45s ease}
 .fitem.k-start{border-left-color:#e0a83b} .fitem.k-done{border-left-color:#5aa469} .fitem.k-turn{border-left-color:#5b7186}
 .fitem.k-review{border-left-color:#c75b5b;background:#2e2329}
 .fitem .fi{font-size:14px;flex:none}
 .fitem .fw{font-weight:700;color:#cdd4db;flex:none}
 .fitem .ft{color:#9aa6b0;flex:1;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
 .fitem .fa{font-size:10px;color:#6a727c;flex:none}
 @keyframes slidein{from{opacity:0;transform:translateY(-8px)}to{opacity:1;transform:none}}
 @keyframes pulse{0%,100%{opacity:1}50%{opacity:.45}}
 .b-working{animation:pulse 1.1s ease-in-out infinite}
</style></head><body>
 <div class="top"><h1>🏢 <span id="title">PITCREW STUDIO</span></h1><span id="light" class="light off">…</span></div>
 <div class="pm"><div class="r">👔 PM (나/Claude)</div><div class="d" id="pm">—</div></div>
 <div class="feed"><div class="fh">📋 활동 피드 (hook 자동 기록 · 최신순)</div><div id="feed"></div></div>
 <div class="deps" id="deps"></div>
 <div class="foot">읽기 전용 · 3초 갱신 · <span id="focus"></span></div>
<script>
const IC={working:"🟢",done:"✅",idle:"💤"};
const FIC={start:"🚀",done:"✅",turn:"💬",review:"👁"};
function safe(t){return String(t??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));}
function ago(ts){const d=Math.max(0,Math.floor(Date.now()/1000-ts));
  if(d<60)return d+"초 전";if(d<3600)return Math.floor(d/60)+"분 전";return Math.floor(d/3600)+"시간 전";}
let lastFeedTop=0;
async function feed(){try{
  const a=await (await fetch('/api/activity?_='+Date.now())).json();
  const f=document.getElementById('feed');
  if(!a.length){f.innerHTML='<div class="fitem"><span class="ft">아직 활동 없음 — 에이전트 띄우면 여기 흐름</span></div>';return;}
  f.innerHTML=a.map(r=>`<div class="fitem k-${safe(r.kind)}"><span class="fi">${FIC[r.kind]||'·'}</span><span class="fw">${safe(r.who)}</span><span class="ft">${safe(r.text)}</span><span class="fa">${ago(r.ts)}</span></div>`).join('');
}catch(e){}}
async function tick(){try{
  const s=await (await fetch('/api/state?_='+Date.now())).json();
  document.getElementById('title').textContent=s.title||'PITCREW STUDIO';
  document.getElementById('pm').textContent=(s.pm&&s.pm.doing)||'대기';
  document.getElementById('focus').textContent='포커스: '+(s.focus||'-');
  const L=document.getElementById('light');
  if(s.session&&s.session.busy){L.className='light on';L.textContent='🟢 세션 작업 중';}else{L.className='light off';L.textContent='💤 세션 유휴';}
  const w=document.getElementById('deps');w.innerHTML='';
  (s.departments||[]).forEach((d,i)=>{const el=document.createElement('div');
    el.className='dep k'+i+(d.status==='active'?' active':'');
    let dk=(d.desks&&d.desks.length)?d.desks.map(k=>`<div class="desk"><div class="ava">${IC[k.status]||'💤'}</div><div class="dk"><div class="role">${safe(k.role)}</div><div class="nm">${safe(k.name)}</div><div class="tk">${safe(k.task)}</div></div><span class="badge b-${safe(k.status||'idle')}">${safe(k.status||'idle')}</span></div>`).join(''):'<div class="empty">대기 중</div>';
    el.innerHTML=`<h2>${safe(d.icon)} ${safe(d.name)}실 ${d.status==='active'?'● 활성':'○ 대기'}</h2><div class="note">${safe(d.note)}</div>${dk}`;
    w.appendChild(el);});
}catch(e){document.getElementById('pm').textContent='상태 못 읽음: '+e;}}
tick();feed();setInterval(tick,3000);setInterval(feed,3000);
</script></body></html>"""


if __name__ == "__main__":
    from runtime import ensure_running
    ensure_running()
    H.runtime_proxy = True
    print(f"PitCrew Hub → http://localhost:{PORT}  (pages dir: {PAGES_DIR})")
    ThreadingHTTPServer(("127.0.0.1", PORT), H).serve_forever()
