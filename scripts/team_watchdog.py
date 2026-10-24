#!/usr/bin/env python
"""팀 워치독 — 팀원이 "작업 중"이라는데 실제로는 멈췄거나 죽은 경우를 잡아서 고친다.

잡는 두 가지:
  1) 죽음: state 는 working 인데 그 역할의 claude/codex 프로세스가 없다
     (호스트가 강제 종료하면 team.sh 의 cleanup 이 안 돌아 state 가 working 으로 남는다)
     → state 를 idle 로 고치고 로그에 ⛔ 를 남긴다. 과제는 1번 자동 재시작.
  2) 무응답: 프로세스는 있는데 live.log 가 stall_min 분 넘게 조용하다 (웹검색 동시 실행 뒤 멈춤 등)
     → 프로세스 트리를 끊는다(team.sh 가 정상 종료하며 state 를 idle 로) → 1번 자동 재시작.
  같은 과제가 두 번째로 멈추면 재시작하지 않고 ⛔ 만 남긴다(대시보드 빨강 → PD 가 본다).

실행: 대시보드(scripts/team_dashboard.py)가 켜지면 60초마다 자동으로 돈다.
     따로 돌릴 땐  python scripts/team_watchdog.py [--once]
설정: .claude/team/config.json 의 "watchdog": {"stall_min": 15, "codex_stall_min": 25}
"""
import hashlib
import json
import re
import subprocess
import sys
import time
from datetime import datetime
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
ROLES = ["programmer", "qa", "art", "artui", "research"]
CODEX = {"art", "artui"}
RETRY = ROOT / ".claude/team/watchdog.json"   # 과제별 자동 재시작 횟수
NOWIN = 0x08000000                             # CREATE_NO_WINDOW
FLAGS = 0x00000200 | NOWIN                     # + CREATE_NEW_PROCESS_GROUP (호스트가 죽여도 같이 안 죽게)
MARK = "(워치독 자동 재시작"


def _read(p: Path):
    try:
        return p.read_text(encoding="utf-8", errors="replace")
    except OSError:
        return ""


def _cfg():
    try:
        w = json.loads(_read(ROOT / ".claude/team/config.json") or "{}").get("watchdog", {})
    except json.JSONDecodeError:
        w = {}
    return float(w.get("stall_min", 15)), float(w.get("codex_stall_min", 25))


def _log(role, msg):
    with open(ROOT / f".ai_logs/team/{role}.live.log", "a", encoding="utf-8") as f:
        f.write(f"[{datetime.now():%H:%M:%S}] {role:<10} | {msg}\n")


def _procs(role):
    """그 역할의 claude/codex 프로세스 PID 목록. 세션 id 가 명령줄에 있다.
    None = 모름(첫 Codex 과제는 id 가 아직 없음, 조회 실패) → 아무것도 안 한다."""
    sid = _read(ROOT / f".claude/team/sessions/{role}.id").strip()
    if not sid or not re.fullmatch(r"[\w\-]+", sid):
        return None
    ps = ("Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -like '*" + sid +
          "*' -and $_.Name -match '^(claude|codex|node)' } | ForEach-Object { $_.ProcessId }")
    try:
        r = subprocess.run(["powershell", "-NoProfile", "-Command", ps], capture_output=True, text=True,
                           timeout=30, creationflags=NOWIN)
    except (OSError, subprocess.TimeoutExpired):
        return None
    if r.returncode != 0:
        return None
    return [int(x) for x in r.stdout.split() if x.isdigit()]


def _last_task(role):
    """team.sh 가 남긴 과제 원문(.claude/team/sessions/<role>.task). 없으면 live.log 의 200자 요약."""
    full = _read(ROOT / f".claude/team/sessions/{role}.task")
    if full.strip():
        return full
    for l in reversed(_read(ROOT / f".ai_logs/team/{role}.live.log").splitlines()):
        if "| 📋 " in l:
            return l.split("| 📋 ", 1)[1]
    return ""


def _restart(role, why, retries):
    task = _last_task(role)
    if task.startswith(MARK):   # 이미 재시작된 과제 → 머리말 떼고 원문으로
        task = task.split("\n\n", 1)[-1]
    key = f"{role}:{hashlib.md5(task[:300].encode()).hexdigest()[:10]}"
    if not task or retries.get(key, 0) >= 1:
        _log(role, f"⛔ 워치독: {why} — 같은 과제가 또 멈춰서 자동 재시작 안 함. PD 확인 필요")
        return False
    retries[key] = retries.get(key, 0) + 1
    task = (f"{MARK}: 직전 시도가 {why}.) 이미 만든 파일을 먼저 읽고 이어서 진행하라. "
            f"웹검색·웹페치는 한 번에 2~3개씩만, 중간 결과는 파일에 바로 저장.\n\n") + task
    out = open(ROOT / f".ai_logs/team/{role}.run.txt", "w", encoding="utf-8")
    subprocess.Popen(["bash", "scripts/team.sh", role, task], cwd=ROOT, stdout=out, stderr=subprocess.STDOUT,
                     stdin=subprocess.DEVNULL, creationflags=FLAGS)
    _log(role, f"🔁 워치독: {why} → 자동 재시작(1회)")
    return True


def _working(st_file):
    return _read(st_file).strip().startswith("working")


def check_once(now=None):
    now = now or time.time()
    stall, codex_stall = _cfg()
    try:
        retries = json.loads(_read(RETRY) or "{}")
    except json.JSONDecodeError:
        retries = {}
    actions = []
    for role in ROLES:
        st_file = ROOT / f".claude/team/sessions/{role}.state"
        if not _working(st_file):
            continue
        log = ROOT / f".ai_logs/team/{role}.live.log"
        quiet = (now - log.stat().st_mtime) / 60 if log.exists() else 999
        if quiet < 2:   # 방금 시작했거나 활발함
            continue
        pids = _procs(role)
        limit = codex_stall if role in CODEX else stall
        if pids == []:
            time.sleep(5)   # 막 끝나서 cleanup 직전일 수 있다 — 다시 본다
            if not _working(st_file) or _procs(role):
                continue
            st_file.write_text(f"idle {datetime.now():%H:%M} (워치독: 프로세스 없음)", encoding="utf-8")
            _log(role, "⛔ 워치독: 작업 중 표시인데 프로세스가 없음(강제 종료됨)")
            actions.append((role, "dead", _restart(role, "프로세스가 강제 종료됨", retries)))
        elif pids and quiet > limit:
            for p in pids:
                subprocess.run(["taskkill", "/PID", str(p), "/T", "/F"], capture_output=True, creationflags=NOWIN)
            _log(role, f"⛔ 워치독: {int(quiet)}분 무응답 → 프로세스 종료")
            for _ in range(30):   # team.sh 가 cleanup 으로 state 를 idle 로 바꿀 때까지
                if not _working(st_file):
                    break
                time.sleep(1)
            else:
                st_file.write_text(f"idle {datetime.now():%H:%M} (워치독: 무응답 종료)", encoding="utf-8")
            actions.append((role, "hung", _restart(role, f"{int(quiet)}분 무응답", retries)))
    if actions:
        RETRY.write_text(json.dumps(retries, ensure_ascii=False), encoding="utf-8")
    return actions


def loop(interval=60):
    while True:
        try:
            check_once()
        except Exception as e:  # 워치독이 죽지 않게
            print("watchdog error:", e, file=sys.stderr)
        time.sleep(interval)


if __name__ == "__main__":
    if "--once" in sys.argv:
        print(check_once())
    else:
        loop()
