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
# 잠긴 경로(roles/build.md, 카디널 D-281: 차 조작값·공유 .mat·ProjectSettings·씬·프리팹)
LOCKED = [
    (re.compile(r"/F1/ProjectSettings/"), "F1/ProjectSettings 는 PD 만(runInBackground 는 런타임 코드로)"),
    (re.compile(r"/F1/Assets/.*\.mat$"), "공유 .mat 수정 금지 — 씬 전용 사본이나 전역 셰이더 값으로"),
    (re.compile(r"/ArcadeCarController\.cs$|/DriveFeel\.cs$"), "차 조작값(ArcadeCarController·DriveFeel)은 잠김 — 도현 직접 주행 판정 뒤에만"),
    (re.compile(r"\.unity$"), ".unity 씬은 텍스트 편집 금지 — 에디터(ucli-http.sh exec)로만"),
    (re.compile(r"\.prefab$"), ".prefab 은 텍스트 편집 금지 — 에디터로만"),
]
# D-277 브리프 한 장: 팀원 세션(TEAM_ROLE 있음)은 규칙 문서를 열지 않는다 — 시작 글(브리프)에 필요한 규칙이 다 있고, 나머지는 이 훅이 막는다.
RULE_DOCS = re.compile(r"(^|/)(PIPELINE\.md|AGENTS\.md|project_rules/[^/]+\.md|docs/harness/dohyun-checklist\.md|docs/team/FLOW\.md|docs/team/WORKFLOW-v2\.md|archive/harness-v1/.*)$")
RULE_DOCS_CMD = re.compile(r"(^|[\s/])(PIPELINE\.md|AGENTS\.md|project_rules/[^\s/]+\.md|docs/harness/dohyun-checklist\.md|docs/team/FLOW\.md|docs/team/WORKFLOW-v2\.md|archive/harness-v1/[^\s]*)\b")


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
        # D-292: 같은 장면 Play 반복 금지 — 과제당 editor play 최대 2번(확인 1 + 재확인 1)
        if re.search(r"editor\s+play", cmd) and os.environ.get("TEAM_ROLE"):
            cnt_file = ".claude/team/sessions/" + os.environ["TEAM_ROLE"] + ".playcount"
            try: n = int(open(cnt_file).read().strip() or 0)
            except Exception: n = 0
            n += cmd.count("editor play")
            try: open(cnt_file, "w").write(str(n))
            except Exception: pass
            if n > 2 and os.environ.get("TEAM_ALLOW_PLAY") != "1":
                deny("D-292: 과제당 Play 는 2번까지. 같은 장면을 다시 찍지 말고 지금 있는 캡처·로그로 보고하라(캡처는 1장이면 충분, 도현이 직접 플레이한다)", f"play {n}번째 :: {cmd[:120]}")
        m = RULE_DOCS_CMD.search(cmd)
        if m and os.environ.get("TEAM_ROLE") and re.match(r"\s*(cat|head|tail|less|more|sed|awk|grep|rg|type)\b", cmd):
            deny("D-277: 규칙 문서는 읽지 않는다 — 브리프(과제 글)에 필요한 규칙이 다 있다. 나머지는 훅이 막는다", f"규칙문서 {m.group(2)} :: {cmd[:120]}")
    elif tool == "Read":
        fp = (ti.get("file_path") or "").replace("\\", "/")
        if fp and os.environ.get("TEAM_ROLE") and RULE_DOCS.search(fp):
            deny("D-277: 규칙 문서는 읽지 않는다 — 브리프(과제 글)에 필요한 규칙이 다 있다. 나머지는 훅이 막는다", f"Read {fp}")
    elif tool in ("Edit", "Write", "MultiEdit"):
        fp = (ti.get("file_path") or "").replace("\\", "/")
        if fp and os.environ.get("TEAM_UNLOCK") != "1":
            for rx, why in LOCKED:
                if rx.search(fp):
                    deny(f"잠긴 경로: {why} (풀기: TEAM_UNLOCK=1)", f"{tool} {fp}")


if __name__ == "__main__":
    main()
