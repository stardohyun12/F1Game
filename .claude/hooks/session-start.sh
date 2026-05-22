#!/usr/bin/env bash
# PitCrew SessionStart hook
# 세션 시작 시 자동 체크리스트 + 최근 상태 컨텍스트 주입.
# 출력은 Claude의 additionalContext에 합쳐짐.

set -euo pipefail

# JSON 문자열 값 안전 이스케이프 (backslash → quote → newline 순서 중요)
escape_json() {
  local s="$1"
  s="${s//\\/\\\\}"
  s="${s//\"/\\\"}"
  s="${s//$'\t'/\\t}"
  s="${s//$'\r'/}"
  s="${s//$'\n'/\\n}"
  printf '%s' "$s"
}

# 마지막 session-log 블록 추출 (가장 최근 ## 헤더부터 끝까지)
LAST_LOG=""
if [ -f "notes/session-log.md" ]; then
  LAST_LOG=$(awk '/^## /{found=NR; buf=""; buf=$0; next} found{buf=buf"\n"$0} END{if(found) print buf}' notes/session-log.md | tail -100)
fi

# 관제탑 (단일 본거지 — 세션 시작 시 제일 먼저)
CONTROL=""
if [ -f "docs/CONTROL.md" ]; then
  CONTROL=$(cat docs/CONTROL.md)
fi

# activeContext 전체
ACTIVE=""
if [ -f ".claude/activeContext.md" ]; then
  ACTIVE=$(cat .claude/activeContext.md)
fi

# JSON으로 hookSpecificOutput 출력
CONTROL_E=$(escape_json "$CONTROL")
ACTIVE_E=$(escape_json "$ACTIVE")
LAST_LOG_E=$(escape_json "$LAST_LOG")
HINT="### 권장 첫 행동\n1. 위 관제탑 §3(진행 중)·§5(막힌 것)에서 첫 작업 선택.\n2. Unity 검증은 unity-cli (Editor 켠 채): unity-cli console --type error,warning.\n3. git status 확인. 세션 끝에 scripts/refresh-control.ps1로 관제탑 갱신."

cat <<EOF
{
  "hookSpecificOutput": {
    "hookEventName": "SessionStart",
    "additionalContext": "## 세션 시작 — 관제탑부터 (단일 본거지 docs/CONTROL.md)\n\n${CONTROL_E}\n\n---\n\n### activeContext (세션별 상세)\n\n${ACTIVE_E}\n\n---\n\n### 지난 session-log 마지막 블록\n\n${LAST_LOG_E}\n\n---\n\n${HINT}"
  }
}
EOF
