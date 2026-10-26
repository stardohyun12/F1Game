#!/usr/bin/env python
"""팀 대시보드 — 역할별 칸: 지금 하는 일·실시간 로그·보고서 + 설정(모델·추론·예산·턴) + 과제 보내기.

실행: python scripts/team_dashboard.py   →  http://localhost:8765  (3초마다 갱신, 워치독 내장 — scripts/team_watchdog.py)
읽는 것: .claude/team/sessions/<role>.state, .ai_logs/team/<role>.live.log, docs/team/reports/, docs/team/BOARD.md
쓰는 것: .claude/team/config.json (설정), 과제 보내기 = bash scripts/team.sh <role> "<과제>" 백그라운드 실행
"""
import json
import os
import re
import subprocess
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
CONFIG = ROOT / ".claude/team/config.json"
ROLES = [("pd", "PD (Hermes)", "#e8a33d"), ("programmer", "프로그래밍팀", "#e0607e"),
         ("qa", "기획·QA팀", "#5aa0e0"), ("art", "아트팀 (Codex)", "#6cc070"), ("artui", "아트팀 UI (Codex)", "#4fb3a0"), ("research", "리서치팀", "#b08be0"), ("mapgen", "맵 생성팀", "#e0a050")]
WORKERS = ["programmer", "qa", "art", "artui", "research", "mapgen"]
# 엔진별 선택지 (빈 값 = 기본값)
ENGINES = {
    "claude": {"models": ["sonnet", "opus", "haiku"], "efforts": ["", "low", "medium", "high", "xhigh", "max"],
               "limits": True},
    "codex": {"models": ["gpt-6-astra"], "efforts": ["", "low", "medium", "high", "xhigh"], "limits": False},
}
ENGINE_OF = {"programmer": "claude", "qa": "claude", "art": "codex", "artui": "codex", "research": "claude", "mapgen": "claude"}
LINE_RE = re.compile(r"^\[(\d\d:\d\d:\d\d)\]\s+\S+\s+\|\s?(.*)$")


def read(p: Path, default=""):
    try:
        return p.read_text(encoding="utf-8", errors="replace")
    except OSError:
        return default


def load_config():
    try:
        return json.loads(read(CONFIG, "{}"))
    except json.JSONDecodeError:
        return {}


def role_data(role):
    state = read(ROOT / f".claude/team/sessions/{role}.state", "없음").strip()
    lines = read(ROOT / f".ai_logs/team/{role}.live.log").splitlines()
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
    log = ROOT / f".ai_logs/team/{role}.live.log"
    quiet = int((time.time() - log.stat().st_mtime) // 60) if log.exists() else 0
    return {"state": state, "task": task, "events": events[-40:], "reports": [r.name for r in reports],
            "working": state.startswith("working"), "quiet": quiet}


def waits():
    try:
        import team_wait  # 같은 scripts/ 폴더
        return team_wait.active()
    except Exception:  # 대기 줄이 깨져도 대시보드는 산다
        return []


def api():
    return {"roles": {r: role_data(r) for r, _, _ in ROLES},
            "waits": waits(),
            "board": read(ROOT / "docs/team/BOARD.md"),
            "lock": read(ROOT / ".claude/team/editor.lock", "").strip(),
            "config": load_config()}


def save_config(role, values):
    cfg = load_config()
    cur = cfg.get(role, {})
    eng = ENGINES[ENGINE_OF[role]]
    cur["model"] = re.sub(r"[^\w.\-]", "", str(values.get("model", "")))
    if ENGINE_OF[role] == "claude" and cur["model"] not in eng["models"]:
        raise ValueError(f"모델은 {', '.join(eng['models'])} 중 하나여야 해요 (opus = 최신 Opus)")
    cur["effort"] = values.get("effort", "") if values.get("effort", "") in eng["efforts"] else ""
    if eng["limits"]:
        cur["budget"] = max(0.1, min(50.0, float(values.get("budget") or 5)))
        cur["turns"] = max(1, min(300, int(float(values.get("turns") or 80))))
    cfg[role] = cur
    CONFIG.write_text(json.dumps(cfg, ensure_ascii=False, indent=1), encoding="utf-8")
    return cur


def send_task(role, text, editor):
    if role_data(role)["working"]:
        return False, "이 팀원은 지금 일하는 중이에요. 끝난 뒤 보내세요."
    text = text.strip()
    if not text:
        return False, "과제가 비어 있어요."
    if editor and "[EDITOR]" not in text:
        text = "[EDITOR] " + text
    lock = read(ROOT / ".claude/team/editor.lock", "").strip()
    if "[EDITOR]" in text and lock and lock != role:
        return False, f"에디터를 {lock} 가 쓰는 중이에요."
    out = open(ROOT / f".ai_logs/team/{role}.run.txt", "w", encoding="utf-8")
    flags = getattr(subprocess, "CREATE_NEW_PROCESS_GROUP", 0) | getattr(subprocess, "CREATE_NO_WINDOW", 0)
    subprocess.Popen(["bash", "scripts/team.sh", role, "[도현 직접] " + text], cwd=ROOT, stdout=out,
                     stderr=subprocess.STDOUT, stdin=subprocess.DEVNULL, creationflags=flags)
    return True, "보냈어요."


def reset_session(role):
    if role_data(role)["working"]:
        return False, "일하는 중에는 초기화할 수 없어요."
    p = ROOT / f".claude/team/sessions/{role}.id"
    if p.exists():
        p.unlink()
    (ROOT / f".claude/team/sessions/{role}.cost").unlink(missing_ok=True)
    return True, "기억을 지웠어요. 다음 과제부터 새 대화로 시작해요."


PAGE = r"""<!doctype html><html lang=ko><meta charset=utf-8><title>F1Game 팀</title>
<style>
*{box-sizing:border-box}
body{margin:0;background:#1e1e1e;color:#ddd;font:14px/1.45 'Pretendard','Malgun Gothic',sans-serif}
header{display:flex;gap:12px;align-items:center;padding:12px 20px;border-bottom:1px solid #333}
header h1{font-size:18px;margin:0 8px 0 0}.chip{padding:3px 10px;border-radius:12px;background:#2c2c2c;font-size:12px}
.cols{display:grid;grid-template-columns:repeat(6,minmax(0,1fr));gap:12px;padding:12px}
.col{background:#262626;border:1px solid #333;border-radius:8px;display:flex;flex-direction:column;height:calc(100vh - 230px);min-height:420px}
.col h2{font-size:15px;margin:0;padding:10px 12px;border-bottom:1px solid #333;display:flex;justify-content:space-between;align-items:center;gap:6px}
.badge{font-size:11px;padding:2px 8px;border-radius:10px;background:#3a3a3a;white-space:nowrap}.badge.on{background:#2f6b3a;color:#cfe}
.meta{display:block;font-size:11px;color:#8a8a8a;font-weight:normal;margin-top:2px}
.gear{background:none;border:1px solid #444;color:#bbb;border-radius:6px;cursor:pointer;padding:2px 7px;font-size:13px}
.gear:hover{border-color:#777;color:#fff}
.task{padding:8px 12px;background:#2d2d2d;border-bottom:1px solid #333;font-size:12px;color:#bbb;max-height:80px;overflow:auto}
.log{flex:1;overflow:auto;padding:6px 12px;font-size:12.5px}.ev{padding:3px 0;border-bottom:1px solid #2e2e2e}
.ev,.task{overflow-wrap:anywhere}.ev time{color:#777;margin-right:6px;font-family:monospace}
.rep{padding:6px 12px;border-top:1px solid #333;font-size:12px;color:#9ab}
.send{border-top:1px solid #333;padding:8px;display:flex;flex-direction:column;gap:6px}
.send textarea{width:100%;min-height:52px;resize:vertical;background:#1c1c1c;color:#ddd;border:1px solid #3a3a3a;border-radius:6px;padding:6px;font:inherit;font-size:12.5px}
.row{display:flex;gap:8px;align-items:center;justify-content:space-between;font-size:12px;color:#aaa}
button.pri{background:#3d5a80;color:#fff;border:0;border-radius:6px;padding:5px 12px;cursor:pointer}button.pri:disabled{opacity:.4;cursor:default}
.settings{display:none;padding:10px 12px;border-bottom:1px solid #333;background:#2a2a2a;font-size:12.5px}
.settings.open{display:block}.settings label{display:grid;grid-template-columns:62px 1fr;align-items:center;gap:8px;margin:4px 0}
.settings input,.settings select{background:#1c1c1c;color:#ddd;border:1px solid #3a3a3a;border-radius:5px;padding:4px 6px;font:inherit;width:100%}
.settings .hint{color:#888;font-size:11px;margin:4px 0 8px}
.danger{background:none;border:1px solid #6a3a3a;color:#d99;border-radius:6px;padding:4px 10px;cursor:pointer}
.board{margin:0 12px 12px;background:#262626;border:1px solid #333;border-radius:8px;padding:8px 14px;white-space:pre-wrap;font-size:12px;max-height:140px;overflow:auto}
.waits{display:flex;flex-direction:column;gap:6px;padding:10px 12px 0}
.wait{display:flex;gap:10px;align-items:center;padding:8px 12px;border-radius:8px;border:1px solid #333;background:#262626;font-size:13px}
.wait b{white-space:nowrap}.wait .sub{color:#8a8a8a;font-size:12px;margin-left:auto;white-space:nowrap}
.wait.running{border-color:#3d5a80}.wait.ready{border-color:#c9a03a;background:#2e2a1e}.wait.stalled{border-color:#a04848;background:#2e1e1e}
#toast{position:fixed;right:16px;bottom:16px;background:#333;border:1px solid #555;padding:8px 14px;border-radius:8px;display:none}
</style>
<header><h1>F1Game 팀</h1><span class=chip id=lock></span><span class=chip id=upd></span></header>
<div class=waits id=waits></div><div class=cols id=cols></div><div class=board id=board></div><div id=toast></div>
<script>
const ROLES=__ROLES__, ENGINES=__ENGINES__, ENGINE_OF=__ENGINE_OF__, WORKERS=__WORKERS__;
const $=id=>document.getElementById(id);
const esc=s=>String(s).replace(/[&<>"]/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]));
function toast(m){const t=$('toast');t.textContent=m;t.style.display='block';clearTimeout(t._h);t._h=setTimeout(()=>t.style.display='none',3000)}
async function post(url,body){const r=await fetch(url,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)});return r.json()}
const effLabel={'':'기본','low':'낮음','medium':'보통','high':'높음','xhigh':'매우 높음','max':'최대'};

function build(){
 $('cols').innerHTML=ROLES.map(([r,name,color])=>{
  const w=WORKERS.includes(r), eng=w?ENGINES[ENGINE_OF[r]]:null;
  const set=w?`<div class=settings id=set-${r}>
    <label>모델${ENGINE_OF[r]==='claude'
      ?`<select id=model-${r}>${eng.models.map(m=>`<option value="${m}">${m}${m==='opus'?' (최신 Opus)':m==='sonnet'?' (최신 Sonnet)':m==='haiku'?' (최신 Haiku)':''}</option>`).join('')}</select>`
      :`<input list=ml-${r} id=model-${r} placeholder="기본 (codex 설정)"><datalist id=ml-${r}>${eng.models.map(m=>`<option value="${m}">`).join('')}</datalist>`}</label>
    <label>추론<select id=effort-${r}>${eng.efforts.map(e=>`<option value="${e}">${effLabel[e]}</option>`).join('')}</select></label>
    ${eng.limits?`<label>예산 $<input type=number step=0.5 min=0.1 max=50 id=budget-${r}></label>
    <label>최대 턴<input type=number step=10 min=1 max=300 id=turns-${r}></label>`:''}
    <div class=hint>${eng.limits?'과제 하나당 상한. 넘으면 그 자리에서 멈춰요.':'Codex 사용량에서 나가요.'} 다음 과제부터 적용.</div>
    <div class=row><button class=danger onclick="resetMem('${r}')">기억 초기화</button><button class=pri onclick="saveSet('${r}')">저장</button></div>
   </div>`:'';
  const send=w?`<div class=send><textarea id=txt-${r} placeholder="${name}에게 과제 보내기"></textarea>
    <div class=row><label><input type=checkbox id=ed-${r}> 에디터 필요</label><button class=pri id=btn-${r} onclick="sendTask('${r}')">보내기</button></div></div>`:'';
  return `<div class=col><h2><span><span style="color:${color}">${name}</span><span class=meta id=meta-${r}></span></span>
   <span style="display:flex;gap:6px;align-items:center"><span class=badge id=badge-${r}></span>${w?`<button class=gear title="설정" onclick="$('set-${r}').classList.toggle('open')">⚙</button>`:''}</span></h2>
   ${set}<div class=task id=task-${r} style="display:none"></div><div class=log id=log-${r}></div>
   <div class=rep id=rep-${r}></div>${send}</div>`}).join('');
}
let filled=false;
function fillSettings(cfg){
 for(const r of WORKERS){const c=cfg[r]||{};
  $('model-'+r).value=c.model||(ENGINE_OF[r]==='claude'?'sonnet':''); $('effort-'+r).value=c.effort||'';
  if($('budget-'+r)){$('budget-'+r).value=c.budget??5;$('turns-'+r).value=c.turns??80}}
}
function metaText(r,cfg){const c=cfg[r]||{};
 const m=c.model||(ENGINE_OF[r]==='codex'?'codex 기본':'sonnet');
 return m+' · 추론 '+effLabel[c.effort||'']+(ENGINE_OF[r]==='claude'?' · $'+(c.budget??5)+' · '+(c.turns??80)+'턴':'')}
async function tick(){
 const d=await (await fetch('/api')).json();
 $('lock').textContent='에디터: '+(d.lock||'비어 있음');
 $('upd').textContent='갱신 '+new Date().toLocaleTimeString();
 if(!filled){fillSettings(d.config);filled=true}
 for(const [r] of ROLES){const x=d.roles[r];
  $('badge-'+r).textContent=x.working?(x.quiet>=10?'⛔ 로그 '+x.quiet+'분 조용':'작업 중'):(x.state.startsWith('idle')?'쉬는 중 '+x.state.slice(5):x.state);
  $('badge-'+r).className='badge '+(x.working&&x.quiet<10?'on':'');
  if(WORKERS.includes(r)){$('meta-'+r).textContent=metaText(r,d.config);$('btn-'+r).disabled=x.working}
  const t=$('task-'+r); t.style.display=x.task?'block':'none'; t.textContent=x.task?'📋 '+x.task:'';
  const html=x.events.slice().reverse().map(e=>`<div class=ev><time>${e.t}</time>${esc(e.msg)}</div>`).join('')||'<div class=ev style="color:#666">활동 없음</div>';
  const lg=$('log-'+r); if(lg._h!==html){lg.innerHTML=html;lg._h=html}
  $('rep-'+r).innerHTML=x.reports.map(esc).join('<br>')||'보고서 없음';
 }
 $('board').textContent=d.board;
 $('waits').innerHTML=(d.waits||[]).map(w=>`<div class="wait ${w.status}"><b>${esc(w.label)}</b><span>${esc(w.what)}${w.next?' → '+esc(w.next):''}</span>
   <span class=sub>기다리는 것: ${esc(w.on)}${w.why?' · '+esc(w.why):''} · ${w.since}부터</span></div>`).join('');
}
async function saveSet(r){const v={model:$('model-'+r).value,effort:$('effort-'+r).value};
 if($('budget-'+r)){v.budget=$('budget-'+r).value;v.turns=$('turns-'+r).value}
 const res=await post('/config',{role:r,values:v}); toast(res.ok?'저장했어요 (다음 과제부터)':res.msg); tick()}
async function sendTask(r){const res=await post('/task',{role:r,text:$('txt-'+r).value,editor:$('ed-'+r).checked});
 toast(res.msg); if(res.ok){$('txt-'+r).value='';$('ed-'+r).checked=false} setTimeout(tick,800)}
async function resetMem(r){if(!confirm('이 팀원의 대화 기억을 지울까요? (보고서·파일은 그대로)'))return;
 const res=await post('/reset',{role:r}); toast(res.msg)}
build();tick();setInterval(tick,3000);
</script></html>"""


class H(BaseHTTPRequestHandler):
    def log_message(self, format, *args):  # noqa: A002 — 요청 로그 끔
        pass

    def _send(self, body: bytes, ctype: str):
        self.send_response(200)
        self.send_header("Content-Type", ctype)
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(body)

    def _json(self, obj):
        self._send(json.dumps(obj, ensure_ascii=False).encode(), "application/json")

    def do_GET(self):
        if self.path.startswith("/api"):
            return self._json(api())
        page = (PAGE.replace("__ROLES__", json.dumps(ROLES, ensure_ascii=False))
                .replace("__ENGINES__", json.dumps(ENGINES))
                .replace("__ENGINE_OF__", json.dumps(ENGINE_OF))
                .replace("__WORKERS__", json.dumps(WORKERS)))
        self._send(page.encode(), "text/html; charset=utf-8")

    def do_POST(self):
        # 로컬 페이지에서 온 요청만 받는다
        origin = self.headers.get("Origin", "")
        if origin and not re.match(r"^http://(localhost|127\.0\.0\.1)(:\d+)?$", origin):
            self.send_error(403)
            return
        try:
            data = json.loads(self.rfile.read(int(self.headers.get("Content-Length", 0))) or b"{}")
            role = data.get("role")
            if role not in WORKERS:
                return self._json({"ok": False, "msg": "알 수 없는 역할"})
            if self.path == "/config":
                return self._json({"ok": True, "config": save_config(role, data.get("values", {}))})
            if self.path == "/task":
                ok, msg = send_task(role, data.get("text", ""), bool(data.get("editor")))
                return self._json({"ok": ok, "msg": msg})
            if self.path == "/reset":
                ok, msg = reset_session(role)
                return self._json({"ok": ok, "msg": msg})
            self.send_error(404)
        except Exception as e:  # 대시보드가 죽지 않게
            self._json({"ok": False, "msg": f"오류: {e}"})


if __name__ == "__main__":
    import threading
    import team_watchdog  # 같은 scripts/ 폴더 — 멈춘/죽은 팀원을 60초마다 잡는다
    threading.Thread(target=team_watchdog.loop, daemon=True).start()
    port = int(os.environ.get("TEAM_DASH_PORT", "8765"))
    print(f"http://localhost:{port}")
    ThreadingHTTPServer(("127.0.0.1", port), H).serve_forever()
