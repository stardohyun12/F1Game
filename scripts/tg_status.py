"""텔레그램 스레드에 '지금 상태' 메시지 하나를 고정해 두고 1분마다 고쳐 씀(D-254: 일하는지 안 하는지 보이게).
사용: python -X utf8 scripts/tg_status.py  (무한 루프, 창 없이 start_queues.ps1 이 띄움)"""
import os, json, time, glob, pathlib, re, urllib.request, datetime as dt
root = pathlib.Path("D:/Github/F1Game"); os.chdir(root)
env = pathlib.Path(os.environ.get("LOCALAPPDATA", r"C:\Users\rtx40\AppData\Local")) / "hermes" / ".env"
tok = next(l.split("=", 1)[1].strip().strip('"') for l in env.read_text(encoding="utf-8").splitlines() if l.startswith("TELEGRAM_BOT_TOKEN="))
CHAT, THREAD = "7449275102", 5864
API = f"https://api.telegram.org/bot{tok}/"
IDF = root / ".claude/team/tg_status_msg.txt"
pidf = root / ".claude/team/tg_status.pid"; pidf.write_text(str(os.getpid()))

def call(m, **kw):
    r = urllib.request.Request(API + m, data=json.dumps(kw).encode(), headers={"Content-Type": "application/json"})
    return json.loads(urllib.request.urlopen(r, timeout=30).read())

def mins(t): return int((time.time() - t) / 60)

ROLES = [("editor", "build", "구현"), ("verify", "verify", "검증"), ("art", "art", "아트"), ("research", "research", "리서치")]   # 일꾼 5개(2026-11-03, PD 는 대화)
def status():
    now = dt.datetime.now().strftime("%H:%M")
    out = [f"📋 PitCrew 지금 상태 ({now} 갱신)"]
    busy = False
    for line, role, label in ROLES:
        st = (root / f".claude/team/sessions/{role}.state")
        s = st.read_text(encoding="utf-8", errors="ignore").strip() if st.exists() else "idle"
        live = root / f".ai_logs/team/{role}.live.log"
        if s.startswith("working"):
            busy = True
            title = re.sub(r"^working \S+ ", "", s)
            full = title
            groups = re.findall(r"\[([^\]]*)\]", title)
            title = ([g for g in groups if g not in ("EDITOR", "OPUS", "research", "pd", "NOLIMIT")] or [title])[0][:60]
            last = ""
            if live.exists():
                m = mins(live.stat().st_mtime)
                tail = live.read_bytes()[-1500:].decode("utf-8", "ignore").strip().splitlines()
                act = re.sub(r"^\[\S+\] \S+\s*\|\s*", "", tail[-1])[:70] if tail else ""
                flag = "🟢" if m < 5 else ("🟡" if m < 20 else "🔴 멈춘 듯")
                last = f"\n   {flag} 마지막 움직임 {m}분 전: {act}"
            started = s.split()[1] if len(s.split()) > 1 else "?"
            prog = ""
            try:
                h, mi = map(int, started.split(":")); n = dt.datetime.now()
                el = (n.hour * 60 + n.minute - h * 60 - mi) % 1440
                box = 60
                tk = ""
                for fp in glob.glob(f".claude/team/queue/{line}/done/*.task") if line else []:
                    head = open(fp, encoding="utf-8", errors="ignore").read(600)
                    if head[:30] == full[:30]: tk = head
                mm = re.search(r"(\d+)분 (넘으면|안)", tk)
                if mm: box = int(mm.group(1))
                pct = min(99, int(el * 100 / box))
                bar = "■" * (pct // 10) + "□" * (10 - pct // 10)
                rep = sorted(glob.glob(f"docs/team/reports/{role}-*.md"), key=os.path.getmtime)
                secs = ""
                if rep:
                    t = open(rep[-1], encoding="utf-8", errors="ignore").read()
                    parts = t.split(chr(10) + "## ")[1:]
                    filled = sum(1 for x in parts if len(x.strip().splitlines()) > 1 and "작성 중" not in x)
                    secs = f" · 보고서 {filled}/{max(4, len(parts))}절"
                prog = chr(10) + f"   진행 {bar} {el}분/{box}분 시간상자{secs}"
            except Exception:
                pass
            out.append(f"▶ {label}: {title} (시작 {started}){prog}{last}")
        if line:
            nxt = sorted(glob.glob(f".claude/team/queue/{line}/*.task"))
            if nxt: out.append(f"   다음 대기: " + ", ".join(pathlib.Path(n).stem for n in nxt[:3]))
    if not busy: out.append("⏸ 지금 도는 작업 없음 — PD 차례 또는 도현 답 대기")
    # 오늘 전체 진행: 오늘 끝난 과제 수 / (끝난 + 대기 + 예비)
    try:
        ql_ = (root / ".ai_logs/team/queue.log").read_text(encoding="utf-8", errors="ignore").splitlines()
        today = dt.datetime.now().strftime("%m/%d")
        done_n = sum(1 for l in ql_ if l.startswith(f"[{today}") and "queue/editor 커밋" in l)
        wait_n = len(glob.glob(".claude/team/queue/editor/*.task")) + len(glob.glob(".claude/team/queue/editor/backlog/*.task")) + (1 if busy else 0)
        out.append(f"오늘 끝낸 과제 {done_n}개 · 남은 과제 {wait_n}개(지금 것 포함)")
    except Exception:
        pass
    ql = root / ".ai_logs/team/queue.log"
    if ql.exists():
        last3 = ql.read_text(encoding="utf-8", errors="ignore").strip().splitlines()[-3:]
        out.append("최근: " + " / ".join(re.sub(r"^\[\S+ ", "[", l)[:70] for l in last3))
    return "\n".join(out)

prev = ""; prevkey = None
def key_of(txt): return tuple(l for l in txt.splitlines() if l.startswith(("▶", "⏸")))
while True:
    try:
        txt = status()
        if txt != prev:
            mid = IDF.read_text().strip() if IDF.exists() else ""
            ok = False
            k = key_of(txt)
            if mid and prevkey is not None and k != prevkey:
                # 작업이 바뀌면 옛 메시지를 지우고 맨 아래에 새로 올림(위로 묻혀 안 보이는 것 방지)
                try: call("deleteMessage", chat_id=CHAT, message_id=int(mid))
                except Exception: pass
                mid = ""
            prevkey = k
            if mid:
                try:
                    call("editMessageText", chat_id=CHAT, message_id=int(mid), text=txt); ok = True
                except Exception as e:
                    ok = "not modified" in str(getattr(e, "read", lambda: b"")()) 
            if not ok:
                r = call("sendMessage", chat_id=CHAT, message_thread_id=THREAD, text=txt, disable_notification=True)
                mid = str(r["result"]["message_id"]); IDF.write_text(mid)
                try: call("pinChatMessage", chat_id=CHAT, message_id=int(mid), disable_notification=True)
                except Exception: pass
            prev = txt
    except Exception as e:
        (root / ".ai_logs/team/tg_status.err").write_text(f"{dt.datetime.now()} {e}")
    time.sleep(60)
