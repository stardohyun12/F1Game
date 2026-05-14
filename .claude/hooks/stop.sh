#!/usr/bin/env bash
# PitCrew Stop hook
# Claude turn 종료 직전 자동 검증:
#   1) unity-cli console --type error — 컴파일 에러 있으면 block (Claude 다시 진입해서 fix)
#   2) 이번 turn에 .cs 변경 있으면 manual play 검증 reminder (block 아님)
# Editor 안 켜졌으면 unity-cli timeout — silent skip.

set -euo pipefail

UNITY_CLI="C:/Users/stard/AppData/Local/unity-cli/unity-cli.exe"

# 1) Unity 컴파일 에러 체크 (timeout 5s)
ERRORS_RAW=""
if [ -x "$UNITY_CLI" ]; then
  ERRORS_RAW=$(timeout 5 "$UNITY_CLI" console --type error 2>/dev/null || true)
fi

# unity-cli 정상 출력의 noise 한 줄 제거 (HTTP server started 메시지)
# 진짜 에러만 남는지 검사
ERRORS_TRIMMED=$(echo "$ERRORS_RAW" | grep -v "HTTP server started" | grep -v "ProcessInitializeOnLoadAttributes" | grep -v '^\[$' | grep -v '^\]$' | grep -v '^\s*"\s*$' | tr -d '[:space:]' || true)

if [ -n "$ERRORS_TRIMMED" ] && [ "$ERRORS_TRIMMED" != "[]" ]; then
  # 진짜 에러 — block
  ERRORS_ESCAPED=$(echo "$ERRORS_RAW" | sed 's/"/\\"/g' | tr '\n' ' ')
  cat <<EOF
{
  "decision": "block",
  "reason": "Unity 컴파일 에러 — 완료 보고 전 fix 필요: ${ERRORS_ESCAPED}"
}
EOF
  exit 0
fi

# 2) 이번 turn .cs 변경 체크 (git diff)
CS_CHANGED=0
if command -v git >/dev/null 2>&1; then
  CS_CHANGED=$(git diff --name-only 2>/dev/null | grep -c "\.cs$" || true)
  CS_STAGED=$(git diff --cached --name-only 2>/dev/null | grep -c "\.cs$" || true)
  CS_CHANGED=$((CS_CHANGED + CS_STAGED))
fi

if [ "$CS_CHANGED" -gt 0 ]; then
  cat <<EOF
{
  "hookSpecificOutput": {
    "hookEventName": "Stop",
    "additionalContext": ".cs 파일 ${CS_CHANGED}개 변경됨. ai-blindspots 룰 1: manual play 또는 screenshot 확인 안 했으면 도현에게 직접 실행 요청. 시각·UI·카메라·머티리얼 관련 변경이면 특히 의무."
  }
}
EOF
fi
