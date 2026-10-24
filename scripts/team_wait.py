#!/usr/bin/env python
"""대기 줄 — "앞 작업이 끝나야 시작할 수 있는 일"을 적어 두고, 앞 작업이 실제로 돌고 있는지 보여 준다.

  python scripts/team_wait.py add "<기다리는 일>" <앞 작업 역할> [다음 담당] [사유]
  python scripts/team_wait.py done <id>        # 기다리던 일을 시작했거나 취소함
  python scripts/team_wait.py list             # 사람이 읽는 요약

상태 (앞 작업 역할의 .claude/team/sessions/<role>.state 로 판단):
  running  = 앞 작업이 지금 돌고 있음 → 정상 대기
  ready    = 앞 작업이 이 대기를 건 뒤에 끝났음 → PD 가 다음 일을 보내야 함 (멈춰 있는 쪽은 PD)
  stalled  = 앞 작업이 돌지 않고, 대기 건 뒤로 끝난 적도 없음 → 아무것도 안 돌고 있음
파일: .claude/team/waits.json (대시보드·team-status.sh 가 읽는다)
"""
import json
import sys
import time
from datetime import datetime
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
WAITS = ROOT / ".claude/team/waits.json"
LABEL = {"running": "⏳ 앞 작업 도는 중", "ready": "▶ 앞 작업 끝남 — PD 차례", "stalled": "⛔ 멈춤 — 아무것도 안 돎"}
SILENT_SEC = 10 * 60  # 작업 중인데 로그가 이만큼 조용하면 죽은 것으로 본다


def load():
    try:
        return json.loads(WAITS.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return []


def save(items):
    WAITS.write_text(json.dumps(items, ensure_ascii=False, indent=1), encoding="utf-8")


def status(item):
    if item["on"] == "pd":  # PD 자신이 할 일 — 팀원을 기다리는 게 아님
        return "ready"
    st_file = ROOT / f".claude/team/sessions/{item['on']}.state"
    try:
        state = st_file.read_text(encoding="utf-8", errors="replace").strip()  # team.sh 가 바이트로 잘라 씀
        mtime = st_file.stat().st_mtime
    except OSError:
        state, mtime = "", 0
    if state.startswith("working"):
        # 프로세스가 강제 종료되면 state 가 "working" 으로 남는다 → 로그가 오래 조용하면 멈춤으로 본다
        log = ROOT / f".ai_logs/team/{item['on']}.live.log"
        quiet = time.time() - (log.stat().st_mtime if log.exists() else mtime)
        if quiet > SILENT_SEC:
            item["quiet_min"] = int(quiet // 60)
            return "stalled"
        return "running"
    return "ready" if mtime > item["created"] else "stalled"


def active():
    """대시보드용: 끝나지 않은 대기 + 계산된 상태."""
    out = []
    for it in load():
        if it.get("done"):
            continue
        s = status(it)
        label = "▶ PD가 지금 할 일" if it["on"] == "pd" else LABEL[s]
        if it.get("quiet_min"):
            label = f"⛔ 멈춤 — {it['on']} 로그 {it['quiet_min']}분째 조용 (죽었을 수 있음)"
        out.append({**it, "status": s, "label": label,
                    "since": datetime.fromtimestamp(it["created"]).strftime("%H:%M")})
    return out


def main(argv):
    cmd = argv[1] if len(argv) > 1 else "list"
    items = load()
    if cmd == "add" and len(argv) >= 4:
        nid = max((i["id"] for i in items), default=0) + 1
        items.append({"id": nid, "what": argv[2], "on": argv[3], "next": argv[4] if len(argv) > 4 else "",
                      "why": argv[5] if len(argv) > 5 else "", "created": time.time(), "done": False})
        save(items)
        print(f"대기 #{nid} 등록")
    elif cmd == "done" and len(argv) >= 3:
        for i in items:
            if str(i["id"]) == argv[2]:
                i["done"] = True
        save(items)
        print(f"대기 #{argv[2]} 닫음")
    elif cmd == "list":
        act = active()
        if not act:
            print("대기 중인 일: 없음")
        for it in act:
            nxt = f" → {it['next']}" if it["next"] else ""
            print(f"#{it['id']} {it['label']} | {it['what']}{nxt} | 기다리는 것: {it['on']} (since {it['since']})")
    else:
        print(__doc__)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
