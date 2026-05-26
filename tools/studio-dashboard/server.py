#!/usr/bin/env python3
"""PitCrew 스튜디오 허브 — 의존성 0, 로컬 전용, 읽기 전용, 안 죽음.

한 주소(http://localhost:7878)에서 사이드바로 페이지 전환:
- 🏢 팀: PM+3부서 스튜디오 도면 (state.json + 세션 busy 라이트, 3초 폴링)
- 그 외: pages/*.html 에 떨어진 모든 페이지(맵 도면 등)가 자동으로 탭에 뜸

새 뷰 추가 = pages/ 에 .html 한 장 떨구면 끝. 포트 안 바뀜, 30분 타임아웃 없음.

실행:  py tools/studio-dashboard/server.py   (백그라운드 권장)
"""
import json, glob, os, re, time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

PORT = 7878
HERE = os.path.dirname(os.path.abspath(__file__))
STATE = os.path.join(HERE, "state.json")
ACTIVITY = os.path.join(HERE, "activity.jsonl")
PAGES_DIR = os.path.join(HERE, "pages")
SESSIONS = os.path.expanduser("~/.claude/sessions")
PROJECT = os.path.dirname(os.path.dirname(HERE))  # tools/studio-dashboard -> 프로젝트 루트
BACKLOG = os.path.join(PROJECT, "backlog.md")
os.makedirs(PAGES_DIR, exist_ok=True)


def read_backlog():
    """backlog.md(PM 소유)를 칸별로 파싱. 파이프라인 패널이 씀.
    형식: '## <칸>' 아래 '- [태그] 제목 — brief: ready|todo (커밋)'."""
    cols, cur = [], None
    try:
        with open(BACKLOG, encoding="utf-8") as f:
            for ln in f:
                s = ln.rstrip("\n")
                if s.startswith("## "):
                    cur = {"name": s[3:].strip(), "items": []}
                    cols.append(cur)
                elif cur is not None and s.lstrip().startswith("- "):
                    body = s.lstrip()[2:]
                    tag = ""
                    mt = re.match(r"\[(.+?)\]\s*(.*)", body)
                    if mt:
                        tag, body = mt.group(1), mt.group(2)
                    brief = "ready" if re.search(r"brief:\s*ready", body) else ("todo" if re.search(r"brief:\s*todo", body) else "")
                    commit = ""
                    mc = re.search(r"\(([0-9a-f]{6,40})\)\s*$", body)
                    if mc:
                        commit = mc.group(1)
                    title = re.sub(r"\s*—\s*brief:.*$", "", body)
                    title = re.sub(r"\s*\([0-9a-f]{6,40}\)\s*$", "", title).strip()
                    cur["items"].append({"tag": tag, "title": title, "brief": brief, "commit": commit})
    except Exception:
        pass
    # "다음 작업 브리프 준비됨?" = 대기 칸 첫 항목 brief==ready
    next_ready = False
    for c in cols:
        if "대기" in c["name"] and c["items"]:
            next_ready = c["items"][0]["brief"] == "ready"
            break
    return {"columns": cols, "nextReady": next_ready}


ROLES = [
    ("PM", "프로듀서", "pm"),
    ("메인", "리드 프로그래머", None),
    ("디자이너", "게임 디자이너 (Phase2)", "game-designer"),
    ("아트", "아트 디렉터 (Phase2)", "art-director"),
    ("기획게이트", "흐름·무결성 판정", None),
    ("그래픽게이트", "시각 판정", None),
    ("QA", "풀루프 검증 (Phase3)", "qa"),
    ("하네스", "운영체계 발전 (Phase3)", "harness-engineer"),
]


def read_roster():
    """직원 명단: 각 직군의 마지막 활동(activity.jsonl) + 메모리 파일 mtime."""
    mem_dir = os.path.join(PROJECT, "docs", "studio", "memory")
    acts = read_activity(200)
    out = []
    for who, role, mem in ROLES:
        last_act = next((a for a in acts if a.get("who") == who), None)
        mtime = 0
        if mem:
            mp = os.path.join(mem_dir, mem + ".md")
            if os.path.exists(mp):
                mtime = os.path.getmtime(mp)
        out.append({
            "who": who, "role": role,
            "lastText": (last_act or {}).get("text", ""),
            "lastTs": (last_act or {}).get("ts", 0),
            "memMtime": mtime,
        })
    return out


BUS = os.path.join(PROJECT, "docs", "studio", "bus.md")


def read_bus(n=40):
    """docs/studio/bus.md 메시지 로그 파싱. 형식: '- [시각] 보낸이→받는이: 내용'. 최신이 아래."""
    msgs = []
    try:
        with open(BUS, encoding="utf-8") as f:
            for ln in f:
                m = re.match(r"^-\s*\[(.*?)\]\s*(.+?)\s*→\s*(.+?)\s*:\s*(.*)$", ln.strip())
                if m:
                    msgs.append({"ts": m.group(1), "frm": m.group(2), "to": m.group(3), "text": m.group(4)})
    except Exception:
        pass
    return msgs[-n:]


def read_activity(n=14):
    """activity.jsonl 마지막 n줄 (hook이 자동 적재). 최신이 위로."""
    try:
        with open(ACTIVITY, encoding="utf-8") as f:
            lines = f.readlines()[-n:]
        out = []
        for ln in lines:
            try:
                out.append(json.loads(ln))
            except Exception:
                pass
        return list(reversed(out))
    except Exception:
        return []


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


class H(BaseHTTPRequestHandler):
    def log_message(self, *a):
        pass

    def _send(self, body, ctype="text/html; charset=utf-8", code=200):
        b = body.encode("utf-8") if isinstance(body, str) else body
        self.send_response(code)
        self.send_header("Content-Type", ctype)
        self.send_header("Content-Length", str(len(b)))
        self.end_headers()
        self.wfile.write(b)

    def do_GET(self):
        path = self.path.split("?")[0]
        if path == "/" or path == "":
            self._send(SHELL)
        elif path == "/api/state":
            d = read_state(); d["session"] = session_activity()
            self._send(json.dumps(d, ensure_ascii=False), "application/json; charset=utf-8")
        elif path == "/api/activity":
            self._send(json.dumps(read_activity(), ensure_ascii=False), "application/json; charset=utf-8")
        elif path == "/api/pages":
            self._send(json.dumps(list_pages(), ensure_ascii=False), "application/json; charset=utf-8")
        elif path == "/api/backlog":
            self._send(json.dumps(read_backlog(), ensure_ascii=False), "application/json; charset=utf-8")
        elif path == "/api/roster":
            self._send(json.dumps(read_roster(), ensure_ascii=False), "application/json; charset=utf-8")
        elif path == "/api/bus":
            self._send(json.dumps(read_bus(), ensure_ascii=False), "application/json; charset=utf-8")
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
function ago(ts){const d=Math.max(0,Math.floor(Date.now()/1000-ts));
  if(d<60)return d+"초 전";if(d<3600)return Math.floor(d/60)+"분 전";return Math.floor(d/3600)+"시간 전";}
let lastFeedTop=0;
async function feed(){try{
  const a=await (await fetch('/api/activity?_='+Date.now())).json();
  const f=document.getElementById('feed');
  if(!a.length){f.innerHTML='<div class="fitem"><span class="ft">아직 활동 없음 — 에이전트 띄우면 여기 흐름</span></div>';return;}
  f.innerHTML=a.map(r=>`<div class="fitem k-${r.kind}"><span class="fi">${FIC[r.kind]||'·'}</span><span class="fw">${r.who||''}</span><span class="ft">${r.text||''}</span><span class="fa">${ago(r.ts)}</span></div>`).join('');
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
    let dk=(d.desks&&d.desks.length)?d.desks.map(k=>`<div class="desk"><div class="ava">${IC[k.status]||'💤'}</div><div class="dk"><div class="role">${k.role||''}</div><div class="nm">${k.name||''}</div><div class="tk">${k.task||''}</div></div><span class="badge b-${k.status||'idle'}">${k.status||'idle'}</span></div>`).join(''):'<div class="empty">대기 중</div>';
    el.innerHTML=`<h2>${d.icon||''} ${d.name}실 ${d.status==='active'?'● 활성':'○ 대기'}</h2><div class="note">${d.note||''}</div>${dk}`;
    w.appendChild(el);});
}catch(e){document.getElementById('pm').textContent='상태 못 읽음: '+e;}}
tick();feed();setInterval(tick,3000);setInterval(feed,3000);
</script></body></html>"""


if __name__ == "__main__":
    print(f"PitCrew Hub → http://localhost:{PORT}  (pages dir: {PAGES_DIR})")
    ThreadingHTTPServer(("127.0.0.1", PORT), H).serve_forever()
