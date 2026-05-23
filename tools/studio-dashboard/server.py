#!/usr/bin/env python3
"""PitCrew 스튜디오 대시보드 — 의존성 0, 로컬 전용, 읽기 전용.

- state.json: PM이(Claude가) 쓰는 팀원 명단·상태.  (이 파일 옆)
- ~/.claude/sessions/*.json: CC가 라이브로 쓰는 세션 status(busy/idle).
브라우저로 http://localhost:7878 열면 스튜디오 도면 + 세션 작업중 라이트.
HTML이 3초마다 /api/state 폴링 → 원할 때 열어두면 자동 갱신.

실행:  py tools/studio-dashboard/server.py     (백그라운드 권장)
"""
import json, glob, os, time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

PORT = 7878
HERE = os.path.dirname(os.path.abspath(__file__))
STATE = os.path.join(HERE, "state.json")
SESSIONS = os.path.expanduser("~/.claude/sessions")


def read_state():
    try:
        with open(STATE, encoding="utf-8") as f:
            return json.load(f)
    except Exception as e:
        return {"title": "PITCREW STUDIO", "error": str(e), "departments": []}


def session_activity():
    """CC 세션 status를 라이브로 — 최근 갱신된 세션이 busy면 작업중."""
    busy, latest = False, 0
    for p in glob.glob(os.path.join(SESSIONS, "*.json")):
        try:
            with open(p, encoding="utf-8") as f:
                s = json.load(f)
            up = s.get("updatedAt", 0)
            if up > latest:
                latest = up
                busy = (s.get("status") == "busy")
        except Exception:
            pass
    age_sec = (time.time() * 1000 - latest) / 1000 if latest else None
    # 30초 넘게 갱신 없으면 유휴로 간주
    if age_sec is not None and age_sec > 30:
        busy = False
    return {"busy": busy, "ageSec": round(age_sec) if age_sec is not None else None}


class H(BaseHTTPRequestHandler):
    def log_message(self, *a):
        pass  # 콘솔 조용히

    def _send(self, body, ctype="text/html; charset=utf-8"):
        b = body.encode("utf-8")
        self.send_response(200)
        self.send_header("Content-Type", ctype)
        self.send_header("Content-Length", str(len(b)))
        self.end_headers()
        self.wfile.write(b)

    def do_GET(self):
        if self.path.startswith("/api/state"):
            data = read_state()
            data["session"] = session_activity()
            self._send(json.dumps(data, ensure_ascii=False), "application/json; charset=utf-8")
        else:
            self._send(PAGE)


PAGE = r"""<!DOCTYPE html><html lang="ko"><head><meta charset="utf-8">
<title>PitCrew Studio</title>
<style>
 body{margin:0;background:#222831;color:#e6e1d6;font-family:system-ui,'Malgun Gothic',sans-serif}
 .top{display:flex;align-items:center;gap:14px;padding:14px 20px;background:#2d3b45;border-bottom:1px solid #1a242b}
 .top h1{font-size:18px;margin:0;letter-spacing:1px}
 .light{margin-left:auto;font-size:13px;padding:5px 12px;border-radius:20px;font-weight:700}
 .on{background:#1f5130;color:#9be8ad} .off{background:#3a3f47;color:#9aa0a8}
 .pm{margin:18px 20px;background:#2d3b45;border-radius:10px;padding:12px 16px}
 .pm .r{font-size:11px;color:#aebcc4;letter-spacing:1px} .pm .d{font-size:13px;margin-top:3px}
 .deps{display:grid;grid-template-columns:repeat(3,1fr);gap:16px;padding:0 20px 24px}
 .dep{background:#e6e1d6;color:#2b313a;border-radius:10px;overflow:hidden;border:2px solid #3a3f47}
 .dep.active{border-color:#5aa469;box-shadow:0 0 0 2px #5aa46955}
 .dep h2{font-size:14px;margin:0;padding:10px 12px;color:#fff;background:#5b7186}
 .dep.k0 h2{background:#5aa469} .dep.k1 h2{background:#5b7186} .dep.k2 h2{background:#9c6f64}
 .note{font-size:11px;color:#6a7280;padding:8px 12px 0}
 .desk{display:flex;gap:10px;align-items:flex-start;margin:10px 12px;background:#fbf8f1;border:1px solid #cfc7b4;border-radius:8px;padding:9px}
 .ava{width:30px;height:30px;border-radius:50%;background:#dfe3e8;display:flex;align-items:center;justify-content:center;font-size:15px;flex:none}
 .dk .role{font-size:10px;color:#8a8270;text-transform:uppercase}
 .dk .nm{font-size:13px;font-weight:700;color:#2d3b45;margin:1px 0 3px}
 .dk .tk{font-size:11px;color:#4a5560;line-height:1.35}
 .badge{margin-left:auto;font-size:10px;font-weight:700;padding:2px 7px;border-radius:12px;flex:none;height:fit-content}
 .b-working{background:#fff0cf;color:#9a6b00} .b-done{background:#d6f0d8;color:#2c7a39} .b-idle{background:#e8e8e8;color:#888}
 .empty{font-size:11px;color:#9aa0a8;padding:10px 12px}
 .foot{padding:0 20px 20px;font-size:11px;color:#8a909a}
</style></head><body>
 <div class="top"><h1>🏢 <span id="title">PITCREW STUDIO</span></h1>
   <span id="light" class="light off">…</span></div>
 <div class="pm"><div class="r">👔 PM (나/Claude)</div><div class="d" id="pm">—</div></div>
 <div class="deps" id="deps"></div>
 <div class="foot">읽기 전용 로컬 뷰 · 3초마다 자동 갱신 · 언제든 새로고침/닫기 OK · <span id="focus"></span></div>
<script>
const ICON_STATUS={working:"🟢",done:"✅",idle:"💤"};
async function tick(){
  try{
    const s=await (await fetch('/api/state?_='+Date.now())).json();
    document.getElementById('title').textContent=s.title||'PITCREW STUDIO';
    document.getElementById('pm').textContent=(s.pm&&s.pm.doing)||'대기';
    document.getElementById('focus').textContent='포커스: '+(s.focus||'-');
    const L=document.getElementById('light');
    if(s.session&&s.session.busy){L.className='light on';L.textContent='🟢 세션 작업 중';}
    else{L.className='light off';L.textContent='💤 세션 유휴';}
    const wrap=document.getElementById('deps');wrap.innerHTML='';
    (s.departments||[]).forEach((d,i)=>{
      const el=document.createElement('div');
      el.className='dep k'+i+(d.status==='active'?' active':'');
      let desks=(d.desks&&d.desks.length)?d.desks.map(k=>`
        <div class="desk"><div class="ava">${ICON_STATUS[k.status]||'💤'}</div>
          <div class="dk"><div class="role">${k.role||''}</div>
            <div class="nm">${k.name||''}</div><div class="tk">${k.task||''}</div></div>
          <span class="badge b-${k.status||'idle'}">${k.status||'idle'}</span></div>`).join('')
        :'<div class="empty">대기 중 — 배정된 작업 없음</div>';
      el.innerHTML=`<h2>${d.icon||''} ${d.name}실 ${d.status==='active'?'● 활성':'○ 대기'}</h2>
        <div class="note">${d.note||''}</div>${desks}`;
      wrap.appendChild(el);
    });
  }catch(e){document.getElementById('pm').textContent='상태 못 읽음: '+e;}
}
tick();setInterval(tick,3000);
</script></body></html>"""


if __name__ == "__main__":
    print(f"PitCrew Studio dashboard → http://localhost:{PORT}")
    ThreadingHTTPServer(("127.0.0.1", PORT), H).serve_forever()
