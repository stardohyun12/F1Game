"""회고 개선안 3(D-264): 도현 메시지가 답 없이 5분 넘게 남아 있거나, 게이트웨이 심장박동이 10분 넘게 멈추면 알림(+ 게이트웨이 ensure).
queue_watchdog.ps1 이 5분마다 부름. 같은 문제로는 30분에 한 번만 알림."""
import os, sqlite3, time, json, pathlib, subprocess, sys
H = pathlib.Path(os.environ.get("LOCALAPPDATA", r"C:\Users\rtx40\AppData\Local")) / "hermes"
SID = "20261025_112145_e0dd17cf"   # PitCrew 텔레그램 스레드 5864 세션
root = pathlib.Path("D:/Github/F1Game"); flag = root / ".ai_logs/team/unanswered.flag"
now = time.time()
c = sqlite3.connect(f"file:{H / 'state.db'}?mode=ro", uri=True)
last_user = c.execute("select max(timestamp) from messages where session_id=? and role='user'", (SID,)).fetchone()[0] or 0
last_other = c.execute("select max(timestamp) from messages where session_id=? and role in ('assistant','tool')", (SID,)).fetchone()[0] or 0
hb = c.execute("select max(last_heartbeat) from gateway_heartbeats where profile='default'").fetchone()[0] or 0
problems = []
if last_user > last_other and now - last_user > 300:
    problems.append(f"도현 메시지가 {int((now-last_user)/60)}분째 답 없음")
if now - hb > 600:
    problems.append(f"게이트웨이 심장박동 {int((now-hb)/60)}분째 멈춤")
if not problems:
    flag.unlink(missing_ok=True); sys.exit(0)
if flag.exists() and now - flag.stat().st_mtime < 1800: sys.exit(0)
flag.write_text(json.dumps(problems, ensure_ascii=False), encoding="utf-8")
if any("게이트웨이" in p for p in problems):
    subprocess.run(["hermes", "gateway", "ensure"], capture_output=True, timeout=120, shell=True)
py = sys.executable
subprocess.run([py, "-X", "utf8", str(root / "scripts/tg_notify.py"), "⚠️ PD 응답 감시: " + " / ".join(problems) + " — 게이트웨이 확인함. 다시 한 번 메시지 보내 줘."])
with open(root / ".ai_logs/team/queue.log", "a", encoding="utf-8") as f:
    f.write(time.strftime("[%m/%d %H:%M] ") + "응답 감시: " + " / ".join(problems) + "\n")
