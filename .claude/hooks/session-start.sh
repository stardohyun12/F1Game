#!/usr/bin/env bash
# PitCrew SessionStart hook
# 세션 시작 시 자동 체크리스트 + 최근 상태 컨텍스트 주입.
# 출력은 Claude의 additionalContext에 합쳐짐.

set -euo pipefail

# 마지막 session-log 블록 추출 (가장 최근 ## 헤더부터 끝까지)
LAST_LOG=""
if [ -f "notes/session-log.md" ]; then
  LAST_LOG=$(awk '/^## /{found=NR; buf=""; buf=$0; next} found{buf=buf"\n"$0} END{if(found) print buf}' notes/session-log.md | tail -100)
fi

# activeContext 전체
ACTIVE=""
if [ -f ".claude/activeContext.md" ]; then
  ACTIVE=$(cat .claude/activeContext.md)
fi

# JSON으로 hookSpecificOutput 출력
cat <<EOF
{
  "hookSpecificOutput": {
    "hookEventName": "SessionStart",
    "additionalContext": "## 세션 시작 자동 체크리스트\n\n### 지난 session-log 마지막 블록\n\n${LAST_LOG//$'\n'/\\n}\n\n---\n\n### activeContext (대기 결정·목표·힌트)\n\n${ACTIVE//$'\n'/\\n}\n\n---\n\n### 권장 첫 행동\n1. Unity Editor 켜져 있으면 \`mcp__coplay-mcp__check_compile_errors\`로 컴파일 상태.\n2. \`git status\` 확인.\n3. 위 activeContext의 '이번 세션 목표' 또는 '대기 결정'에서 첫 작업 선택."
  }
}
EOF
