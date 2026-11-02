"""도현 텔레그램 스레드(5864)에 직접 알림. 사용: python -X utf8 scripts/tg_notify.py "글" [사진경로]
hermes send 는 DM 스레드에서 실패(Thread not found) → Bot API 직접. 토큰은 hermes .env 에서만 읽고 출력 안 함."""
import os, sys, json, urllib.request, uuid, pathlib
env = pathlib.Path(os.environ.get("LOCALAPPDATA", "")) / "hermes" / ".env"
tok = next(l.split("=", 1)[1].strip().strip('"') for l in env.read_text(encoding="utf-8").splitlines() if l.startswith("TELEGRAM_BOT_TOKEN="))
CHAT, THREAD = "7449275102", "5864"
txt = sys.argv[1] if len(sys.argv) > 1 else sys.stdin.read()
# D-265: 도현은 데스크톱 대화에서 봄 — 텔레그램은 긴급(응답 감시·한도)만. 조용 모드 파일이 있으면 나머지는 안 보냄
if (pathlib.Path("D:/Github/F1Game/.claude/team/tg_quiet").exists() and not txt.startswith(("⚠️ PD 응답 감시", "⚠️ Codex"))):
    print("tg quiet (skipped)"); sys.exit(0)
img = sys.argv[2] if len(sys.argv) > 2 else ""
base = f"https://api.telegram.org/bot{tok}/"
if img and os.path.isfile(img):
    b = uuid.uuid4().hex; parts = []
    for k, v in (("chat_id", CHAT), ("message_thread_id", THREAD), ("caption", txt[:1000])):
        parts.append(f'--{b}\r\nContent-Disposition: form-data; name="{k}"\r\n\r\n{v}\r\n'.encode())
    parts.append(f'--{b}\r\nContent-Disposition: form-data; name="photo"; filename="{os.path.basename(img)}"\r\nContent-Type: image/png\r\n\r\n'.encode() + open(img, "rb").read() + b"\r\n")
    parts.append(f"--{b}--\r\n".encode())
    req = urllib.request.Request(base + "sendPhoto", data=b"".join(parts), headers={"Content-Type": f"multipart/form-data; boundary={b}"})
else:
    req = urllib.request.Request(base + "sendMessage", data=json.dumps({"chat_id": CHAT, "message_thread_id": int(THREAD), "text": txt[:4000]}).encode(), headers={"Content-Type": "application/json"})
try:
    r = json.loads(urllib.request.urlopen(req, timeout=60).read()); print("tg ok" if r.get("ok") else f"tg FAIL {r.get('description')}")
except Exception as e:
    print("tg FAIL", getattr(e, "read", lambda: str(e).encode())()[:200])
