# -*- coding: utf-8 -*-
"""PitCrew 스튜디오 창 (D-291) — 읽기 전용. 세 칸: 지금 / 줄(실시간) / 오늘.
버튼·입력 없음. 조작은 Hermes 대화에서. 창을 닫아도 작업엔 영향 없음.
실행: scripts/studio.cmd  (또는 python -X utf8 scripts/studio.py)
"""
import os, sys, time, json, re, io, glob
from datetime import datetime

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
os.chdir(ROOT)
SESS = ".claude/team/sessions"
ROLES = ["build", "art", "research"]
LIVE_LINES = 14
MODEL_LABEL = {"opus": "Opus", "sonnet": "Sonnet", "haiku": "Haiku"}

def rd(p, default=""):
    try:
        with open(p, encoding="utf-8", errors="ignore") as f: return f.read()
    except Exception: return default

def tail(p, n):
    try:
        with open(p, "rb") as f:
            f.seek(0, 2); size = f.tell(); block = 64 * 1024
            f.seek(max(0, size - block)); data = f.read().decode("utf-8", "ignore")
        lines = [l.rstrip("\r") for l in data.splitlines() if l.strip()]
        return lines[-n:]
    except Exception: return []

def mins_since(hhmm):
    try:
        h, m = map(int, hhmm.split(":")); now = datetime.now()
        d = (now.hour * 60 + now.minute) - (h * 60 + m)
        return d if d >= 0 else d + 1440
    except Exception: return None

def box_minutes(task_text):
    m = re.search(r"시간 상자\s*(\d+)\s*분", task_text) or re.search(r"(\d+)\s*분\s*(상한|안)", task_text)
    return int(m.group(1)) if m else None

def live_cost(role):
    # team_log 가 live 로그에 "이번 $x.xx" 를 찍기 전까지는 stream-json 결과 파일에서 추정
    for l in reversed(tail(f".ai_logs/team/{role}.live.log", 400)):
        m = re.search(r"\$(\d+\.\d+)", l)
        if m and ("비용" in l or "cost" in l or "이번" in l): return m.group(1)
    return None

def now_block():
    out = []
    any_working = False
    for role in ROLES:
        st = rd(f"{SESS}/{role}.state").strip()
        if not st.startswith("working"): continue
        any_working = True
        run = {}
        try: run = json.loads(rd(f"{SESS}/{role}.run.json") or "{}")
        except Exception: pass
        name = rd(f"{SESS}/{role}.task_name").strip() or st[14:80]
        task = rd(f"{SESS}/{role}.task")
        start = run.get("start") or st.split()[1] if len(st.split()) > 1 else "?"
        el = mins_since(start); box = box_minutes(task)
        model = MODEL_LABEL.get(run.get("model", ""), run.get("model", "?"))
        eff = run.get("effort", "?"); bud = run.get("budget", "?")
        cost = live_cost(role)
        prog = f"{el}분" + (f"/{box}분" if box else "") if el is not None else ""
        bar = ""
        if el is not None and box:
            k = min(10, int(10 * el / box)); bar = "■" * k + "□" * (10 - k)
        out.append(f" ▶ {name:<24} {model}/{eff}  예산 ${bud}" + (f"  지금 ${cost}" if cost else ""))
        out.append(f"   {start}~  {bar} {prog}")
        last = tail(f".ai_logs/team/{role}.live.log", 1)
        if last:
            l = last[-1]
            m = re.match(r"\[(\d\d:\d\d:\d\d)\]\s*\S+\s*\|\s*(.*)", l)
            if m:
                ago = ""
                try:
                    t = datetime.strptime(m.group(1), "%H:%M:%S").replace(year=datetime.now().year, month=datetime.now().month, day=datetime.now().day)
                    ago = f"{int((datetime.now()-t).total_seconds())}초 전"
                except Exception: pass
                out.append(f"   마지막: {m.group(2)[:70]}  {ago}")
    if not any_working:
        out.append(" (도는 과제 없음 — PD 차례 또는 도현 한 판 대기)")
    return out

def live_block():
    lines = []
    for role in ROLES:
        if rd(f"{SESS}/{role}.state").strip().startswith("working"):
            for l in tail(f".ai_logs/team/{role}.live.log", LIVE_LINES):
                m = re.match(r"\[(\d\d:\d\d):\d\d\]\s*\S+\s*\|\s*(.*)", l)
                lines.append(f" {m.group(1)} {m.group(2)[:90]}" if m else " " + l[:100])
    if not lines:
        for l in tail(".ai_logs/team/build.live.log", 6):
            m = re.match(r"\[(\d\d:\d\d):\d\d\]\s*\S+\s*\|\s*(.*)", l)
            lines.append(f" {m.group(1)} {m.group(2)[:90]}" if m else " " + l[:100])
        lines.append(" (마지막 과제의 끝부분)")
    return lines[-LIVE_LINES:]

def today_block():
    out = []
    today = datetime.now().strftime("%Y-%m-%d")
    done = []
    for l in tail(".ai_logs/task.log", 50):
        parts = l.split("\t")
        if len(parts) >= 5 and parts[0].startswith(today):
            done.append(f"{parts[1]}({parts[4]})")
    out.append(" 끝: " + (" · ".join(done[-6:]) if done else "(아직 없음)"))
    now = rd("docs/NOW.md")
    for sec, label in (("다음 3", "다음"), ("도현 판정 대기", "판정 대기"), ("마지막 한 판 메모", "메모")):
        m = re.search(r"## " + re.escape(sec) + r"\n(.*?)(?=\n## |\Z)", now, re.S)
        if m:
            body = [x.strip(" -") for x in m.group(1).strip().splitlines() if x.strip()]
            if body: out.append(f" {label}: " + " / ".join(b[:60] for b in body[:3]))
    return out

def _w(t):
    # 동아시아 글자는 2칸
    import unicodedata
    return sum(2 if unicodedata.east_asian_width(c) in "WF" else 1 for c in t)

def _fit(t, w):
    out, n = [], 0
    for c in t:
        k = 2 if __import__("unicodedata").east_asian_width(c) in "WF" else 1
        if n + k > w: break
        out.append(c); n += k
    return "".join(out) + " " * (w - n)

def render():
    try: W, H = os.get_terminal_size()
    except Exception: W, H = 100, 38
    W = max(60, W - 1); H = max(20, H)
    def hdr(t): return "┌ " + t + " " + "─" * max(0, W - _w(t) - 3)
    rows = [hdr("지금")] + now_block() + [hdr("줄 (실시간)")] + live_block() + [hdr("오늘")] + today_block()
    rows.append("└" + "─" * (W - 1))
    rows.append(f"  {datetime.now().strftime('%H:%M:%S')} · 읽기 전용 · 조작은 Hermes 대화에서 · 닫아도 됨")
    rows = rows[:H - 1]
    while len(rows) < H - 1: rows.append("")
    return "\n".join(_fit(r, W) for r in rows)

def main():
    try:
        sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")
    except Exception: pass
    os.system("")  # ANSI on Windows
    sys.stdout.write("\x1b[?25l")
    try:
        while True:
            sys.stdout.write("\x1b[H" + render()); sys.stdout.flush()
            time.sleep(2)
    except KeyboardInterrupt:
        pass
    finally:
        sys.stdout.write("\x1b[?25h")

if __name__ == "__main__":
    main()
