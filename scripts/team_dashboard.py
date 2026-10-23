#!/usr/bin/env python
"""팀 대시보드 — 역할별 칸에 지금 하는 일·실시간 로그·보고서를 보여준다.

실행: python scripts/team_dashboard.py   →  http://localhost:8765  (3초마다 갱신)
읽는 것: .claude/team/sessions/<role>.state, .ai_logs/team/<role>.live.log, docs/team/reports/, docs/team/BOARD.md
"""
import html
import json
import os
import re
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
ROLES = [("pd", "PD (Hermes)", "#e8a33d"), ("programmer", "프로그래밍팀", "#e0607e"),
         ("qa", "기획·QA팀", "#5aa0e0"), ("art", "아트팀 (Codex)", "#6cc070")]
LINE_RE = re.compile(r"^\[(\d\d:\d\d:\d\d)\]\s+\S+\s+\|\s?(.*)$")


def read(p: Path, default=""):
    try:
        return p.read_text(encoding="utf-8", errors="replace")
    except OSError:
        return default


def role_data(role):
    state = read(ROOT / f".claude/team/sessions/{role}.state", "없음").strip()
    lines = read(ROOT / f".ai_logs/team/{role}.live.log").splitlines()
    # 마지막 과제부터만
    start = max((i for i, l in enumerate(lines) if "===== 새 과제 =====" in l), default=0)
    events, task = [], ""
    for l in lines[start:]:
        m = LINE_RE.match(l.strip())
        if not m:
            continue
        t, msg = m.groups()
        if msg.startswith("📋"):
            task = msg[1:].strip()
        elif "새 과제" not in msg:
            events.append({"t": t, "msg": msg})
    reports = sorted((ROOT / "docs/team/reports").glob(f"{role}-*.md"), key=os.path.getmtime, reverse=True)[:3]
    return {"state": state, "task": task, "events": events[-40:],
            "reports": [r.name for r in reports],
            "working": state.startswith("working")}


def api():
    board = read(ROOT / "docs/team/BOARD.md")
    lock = read(ROOT / ".claude/team/editor.lock", "").strip()
    return {"roles": {r: role_data(r) for r, _, _ in ROLES}, "board": board, "lock": lock}


PAGE = """<!doctype html><html lang=ko><meta charset=utf-8><title>F1Game 팀</title>
<style>
body{margin:0;background:#1e1e1e;color:#ddd;font:14px/1.45 'Pretendard','Malgun Gothic',sans-serif}
header{display:flex;gap:16px;align-items:center;padding:12px 20px;border-bottom:1px solid #333}
header h1{font-size:18px;margin:0}.chip{padding:3px 10px;border-radius:12px;background:#2c2c2c;font-size:12px}
.ev,.task{overflow-wrap:anywhere}
.cols{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:12px;padding:12px}
.col{background:#262626;border:1px solid #333;border-radius:8px;display:flex;flex-direction:column;height:calc(100vh - 250px)}
.col h2{font-size:15px;margin:0;padding:10px 12px;border-bottom:1px solid #333;display:flex;justify-content:space-between}
.badge{font-size:11px;padding:2px 8px;border-radius:10px;background:#3a3a3a}.badge.on{background:#2f6b3a;color:#cfe}
.task{padding:8px 12px;background:#2d2d2d;border-bottom:1px solid #333;font-size:12px;color:#bbb;max-height:80px;overflow:auto}
.log{flex:1;overflow:auto;padding:6px 12px;font-size:12.5px}.ev{padding:3px 0;border-bottom:1px solid #2e2e2e}
.ev time{color:#777;margin-right:6px;font-family:monospace}.rep{padding:6px 12px;border-top:1px solid #333;font-size:12px;color:#9ab}
.board{margin:0 12px 12px;background:#262626;border:1px solid #333;border-radius:8px;padding:8px 14px;white-space:pre-wrap;font-size:12px;max-height:150px;overflow:auto}
</style>
<header><h1>F1Game 팀</h1><span class=chip id=lock></span><span class=chip id=upd></span></header>
<div class=cols id=cols></div><div class=board id=board></div>
<script>
const ROLES=__ROLES__;
const esc=s=>s.replace(/[&<>]/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;'}[c]));
async function tick(){
 const d=await (await fetch('/api')).json();
 document.getElementById('lock').textContent='에디터: '+(d.lock||'비어 있음');
 document.getElementById('upd').textContent='갱신 '+new Date().toLocaleTimeString();
 const cols=document.getElementById('cols');
 cols.innerHTML=ROLES.map(([r,name,color])=>{const x=d.roles[r];
  const ev=x.events.slice().reverse().map(e=>`<div class=ev><time>${e.t}</time>${esc(e.msg)}</div>`).join('')||'<div class=ev style="color:#666">활동 없음</div>';
  return `<div class=col><h2><span style="color:${color}">${name}</span><span class="badge ${x.working?'on':''}">${x.working?'작업 중':esc(x.state)}</span></h2>
  ${x.task?`<div class=task>📋 ${esc(x.task)}</div>`:''}<div class=log>${ev}</div>
  <div class=rep>${x.reports.map(esc).join('<br>')||'보고서 없음'}</div></div>`}).join('');
 document.getElementById('board').textContent=d.board;
}
tick();setInterval(tick,3000);
</script></html>"""


class H(BaseHTTPRequestHandler):
    def log_message(self, format, *args):  # noqa: A002 — 요청 로그 끔
        pass

    def do_GET(self):
        if self.path.startswith("/api"):
            body, ctype = json.dumps(api(), ensure_ascii=False).encode(), "application/json"
        else:
            body = PAGE.replace("__ROLES__", json.dumps(ROLES, ensure_ascii=False)).encode()
            ctype = "text/html; charset=utf-8"
        self.send_response(200)
        self.send_header("Content-Type", ctype)
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(body)


if __name__ == "__main__":
    port = int(os.environ.get("TEAM_DASH_PORT", "8765"))
    print(f"http://localhost:{port}")
    ThreadingHTTPServer(("127.0.0.1", port), H).serve_forever()
