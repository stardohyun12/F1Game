#!/usr/bin/env python3
"""guard-pre.py — PreToolUse 훅(D-276): 규칙을 「읽고 기억」이 아니라 훅으로 강제한다.
matcher Bash|Edit|Write|MultiEdit. stdin JSON(tool_name, tool_input) → 막으면 stdout JSON deny(exit 0).
막은 것은 .ai_logs/team/guard.log 에 한 줄. 푸는 법: TEAM_ALLOW_BOT=1(봇 전체 경로), TEAM_UNLOCK=1(잠긴 파일).
등록: .claude/settings.json(PD 세션) + .claude/team/guard.settings.json(team.sh --settings, 팀원 세션은 --setting-sources user 라 따로)."""
import sys, os, json, re, time

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
BOT_RE = re.compile(r"OnFootRouteBot\.(Begin|Run)|BeginFirstOrder|route_feature2|RegressSweep\.Run")
SLEEP_RE = re.compile(r"\bsleep\s+[0-9]{3,}\b|until\s+\[\s*-f\s+.*\]\s*;\s*do\s+sleep")
# 잠긴 경로(docs/team/CARD_TEMPLATE.md 「하지 말 것」 + roles/build.md: 차 조작값·공유 .mat·ProjectSettings·씬·프리팹)
LOCKED = [
    (re.compile(r"/F1/ProjectSettings/"), "F1/ProjectSettings 는 PD 만(runInBackground 는 런타임 코드로)"),
    (re.compile(r"/F1/Assets/.*\.mat$"), "공유 .mat 수정 금지 — 씬 전용 사본이나 전역 셰이더 값으로"),
    (re.compile(r"/ArcadeCarController\.cs$|/DriveFeel\.cs$"), "차 조작값(ArcadeCarController·DriveFeel)은 잠김 — 도현 직접 주행 판정 뒤에만"),
    (re.compile(r"\.unity$"), ".unity 씬은 텍스트 편집 금지 — 에디터(ucli-http.sh exec)로만"),
    (re.compile(r"\.prefab$"), ".prefab 은 텍스트 편집 금지 — 에디터로만"),
]


def log(what):
    try:
        os.makedirs(os.path.join(ROOT, ".ai_logs", "team"), exist_ok=True)
        with open(os.path.join(ROOT, ".ai_logs", "team", "guard.log"), "a", encoding="utf-8") as f:
            f.write(f"[{time.strftime('%m/%d %H:%M')}] {os.environ.get('TEAM_ROLE', 'pd')} 막음: {what}\n")
    except Exception:
        pass


def deny(reason, what):
    log(what)
    print(json.dumps({"hookSpecificOutput": {"hookEventName": "PreToolUse", "permissionDecision": "deny",
                                             "permissionDecisionReason": reason}}, ensure_ascii=False))
    sys.exit(0)


def main():
    try:
        d = json.load(sys.stdin)
    except Exception:
        return
    tool = d.get("tool_name", ""); ti = d.get("tool_input") or {}
    if tool == "Bash":
        cmd = ti.get("command") or ""
        m = BOT_RE.search(cmd)
        if m and os.environ.get("TEAM_ALLOW_BOT") != "1":
            deny("D-270: 봇 전체 경로는 PD 가 따로. 순간이동(DebugWindow.GoPlace)으로 확인 (풀기: TEAM_ALLOW_BOT=1)", f"봇 {m.group(0)} :: {cmd[:120]}")
        m = SLEEP_RE.search(cmd)
        if m:
            deny("D-270: 기다리는 검사 금지(sleep 100+·until -f 루프). 순간이동으로 바로 확인하고 캡처 1~2장", f"대기 {m.group(0)} :: {cmd[:120]}")
    elif tool in ("Edit", "Write", "MultiEdit"):
        fp = (ti.get("file_path") or "").replace("\\", "/")
        if fp and os.environ.get("TEAM_UNLOCK") != "1":
            for rx, why in LOCKED:
                if rx.search(fp):
                    deny(f"잠긴 경로: {why} (풀기: TEAM_UNLOCK=1)", f"{tool} {fp}")


if __name__ == "__main__":
    main()
