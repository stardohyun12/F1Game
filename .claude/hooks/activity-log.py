#!/usr/bin/env python3
"""활동 로그 hook — 에이전트 시작/종료를 append-only로 적는다.

studio-dashboard가 이 파일을 tail 해서 "흐르는 활동 피드"로 보여준다.
손으로 state.json 갱신하던 걸 자동으로 대체 (stale 방지).

등록 (.claude/settings.json):
  PreToolUse  matcher "Agent|Task"  → 에이전트 dispatch 시 "start" 한 줄
  SubagentStop matcher "*"          → 에이전트 끝날 때 "done" 한 줄
  Stop         matcher "*"          → 내(PM) 턴 끝 "turn" 한 줄

절대 block 안 함 — 무슨 일이 있어도 exit 0.

CLI 직접 게시 (hook 아닌 이벤트 — 예: 외부 눈 UX 리뷰어 판정):
  python activity-log.py --post --kind review --who UX리뷰어 --text "한 줄 판정"
"""
import sys, os, json, time

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))  # repo root
LOG = os.path.join(ROOT, "tools", "studio-dashboard", "activity.jsonl")
RUNNING = os.path.join(ROOT, "tools", "studio-dashboard", "running.json")  # 지금 도는 에이전트 stack
CAP = 300  # 마지막 N줄만 유지


def push_running(who):
    """에이전트 start 시 이름을 stack에 쌓음 (대시보드 '지금 실행 중' + done 이름 짝)."""
    try:
        stack = []
        if os.path.exists(RUNNING):
            with open(RUNNING, encoding="utf-8") as f:
                stack = json.load(f)
        stack.append({"who": who, "ts": int(time.time())})
        with open(RUNNING, "w", encoding="utf-8") as f:
            json.dump(stack[-20:], f, ensure_ascii=False)
    except Exception:
        pass


def pop_running():
    """SubagentStop 시 가장 최근 start 이름을 꺼냄 (done에 이름 싣기)."""
    try:
        if not os.path.exists(RUNNING):
            return None
        with open(RUNNING, encoding="utf-8") as f:
            stack = json.load(f)
        if not stack:
            return None
        who = stack.pop()["who"]
        with open(RUNNING, "w", encoding="utf-8") as f:
            json.dump(stack, f, ensure_ascii=False)
        return who
    except Exception:
        return None


def write_rec(rec):
    # 깨진 서로게이트 등 인코딩 불가 문자 제거 (파일 쓰기 침묵 실패 방지)
    rec["text"] = str(rec.get("text", "")).encode("utf-8", "ignore").decode("utf-8")[:120]
    rec["who"] = str(rec.get("who", "")).encode("utf-8", "ignore").decode("utf-8")[:40]
    rec["ts"] = int(time.time())
    try:
        os.makedirs(os.path.dirname(LOG), exist_ok=True)
        lines = []
        if os.path.exists(LOG):
            with open(LOG, encoding="utf-8") as f:
                lines = f.readlines()
        lines.append(json.dumps(rec, ensure_ascii=False) + "\n")
        lines = lines[-CAP:]
        with open(LOG, "w", encoding="utf-8") as f:
            f.writelines(lines)
    except Exception:
        pass


def cli_post(argv):
    """--post --kind X --who Y --text Z 로 피드에 직접 한 줄."""
    kv = {}
    i = 0
    while i < len(argv):
        if argv[i].startswith("--") and i + 1 < len(argv):
            kv[argv[i][2:]] = argv[i + 1]
            i += 2
        else:
            i += 1
    write_rec({"kind": kv.get("kind", "review"), "who": kv.get("who", "리뷰어"), "text": kv.get("text", "")})


def main():
    try:
        # Windows에서 sys.stdin.read()는 cp949로 읽어 UTF-8 한글을 망가뜨림.
        # raw bytes를 직접 UTF-8로 디코드해야 한글이 안 깨진다.
        raw = sys.stdin.buffer.read().decode("utf-8", "replace")
        data = json.loads(raw) if raw.strip() else {}
    except Exception:
        data = {}

    event = data.get("hook_event_name", "")
    rec = None

    if event == "PreToolUse" and data.get("tool_name") in ("Agent", "Task"):
        ti = data.get("tool_input", {}) or {}
        who = ti.get("subagent_type") or "에이전트"
        desc = ti.get("description") or ti.get("prompt", "")[:40]
        push_running(who)
        rec = {"kind": "start", "who": who, "text": desc}
    elif event == "SubagentStop":
        who = pop_running() or "에이전트"
        rec = {"kind": "done", "who": who, "text": "작업 완료"}
    elif event == "Stop":
        rec = {"kind": "turn", "who": "PM", "text": "응답 완료"}

    if rec is None:
        return
    write_rec(rec)


if __name__ == "__main__":
    if "--post" in sys.argv:
        cli_post(sys.argv[1:])
    else:
        main()
    sys.exit(0)
