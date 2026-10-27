#!/usr/bin/env python
"""팀원 세션 자동 정리(compact 대신 새 세션으로 교체).

긴 --resume 세션은 턴당 비용이 오르고 지난 맥락이 섞인다. 역할이 idle 일 때
세션 누적 비용이 한도를 넘으면 세션 id 를 archive/ 로 옮겨, 다음 과제부터 새 세션으로
시작하게 한다. 새 세션은 과제에 적힌 보고서·결정 기록만 읽는다.

사용:  python scripts/team_autorotate.py            # 60초마다 감시(백그라운드)
       python scripts/team_autorotate.py --once     # 한 번만
       python scripts/team_autorotate.py --now ROLE  # 지금 바로 (idle 일 때만)
환경변수 TEAM_ROTATE_USD (기본 15)
"""
import os, re, sys, time, shutil, datetime, pathlib

ROOT = pathlib.Path(__file__).resolve().parent.parent
SESS = ROOT / ".claude/team/sessions"
LOGS = ROOT / ".ai_logs/team"
ARCH = SESS / "archive"
LIMIT = float(os.environ.get("TEAM_ROTATE_USD", "15"))
ROLES = ["programmer", "qa", "art", "artui", "research", "mapgen", "art2"]


def idle(role):
    st = SESS / f"{role}.state"
    return st.exists() and st.read_text(encoding="utf-8", errors="ignore").startswith("idle")


def cumulative(role):
    log = LOGS / f"{role}.live.log"
    if not log.exists():
        return 0.0
    tail = log.read_bytes()[-20000:].decode("utf-8", errors="ignore")
    m = re.findall(r"세션 누적 \$([0-9.]+)", tail)
    return float(m[-1]) if m else 0.0


def rotate(role, why):
    sid = SESS / f"{role}.id"
    if not sid.exists() or sid.stat().st_size == 0:
        return False
    ARCH.mkdir(exist_ok=True)
    stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M")
    shutil.move(str(sid), str(ARCH / f"{role}-{stamp}.id"))
    cost = SESS / f"{role}.cost"
    if cost.exists():
        shutil.move(str(cost), str(ARCH / f"{role}-{stamp}.cost"))
    with open(LOGS / f"{role}.live.log", "a", encoding="utf-8") as f:
        f.write(f"[{datetime.datetime.now():%H:%M:%S}] {role:10s} | 🔄 세션 정리: {why} → 다음 과제부터 새 세션\n")
    print(f"{stamp} {role}: rotated ({why})", flush=True)
    return True


def sweep():
    for r in ROLES:
        if idle(r):
            c = cumulative(r)
            if c > LIMIT:
                rotate(r, f"누적 ${c:.2f} > ${LIMIT:.0f}")


if __name__ == "__main__":
    if "--now" in sys.argv:
        r = sys.argv[sys.argv.index("--now") + 1]
        print(rotate(r, "수동") if idle(r) else f"{r} 작업 중 — 건너뜀")
    elif "--once" in sys.argv:
        sweep()
    else:
        while True:
            try:
                sweep()
            except Exception as e:
                print("err", e, flush=True)
            time.sleep(60)
