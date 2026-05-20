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

# 무시할 noise 패턴:
#  - unity-cli 시작 메시지
#  - Unity Connect / Auth / TokenExchange — 네트워크 noise, 우리 코드 무관
#  - 존재 안 하는 메뉴 실행 (no menu named) — exec 실패는 stderr로 잡힘
ERRORS_TRIMMED=$(echo "$ERRORS_RAW" \
  | grep -v "HTTP server started" \
  | grep -v "ProcessInitializeOnLoadAttributes" \
  | grep -v "UnityConnectWebRequestException" \
  | grep -v "TokenExchange" \
  | grep -v "UnityEditor.Connect" \
  | grep -v "ServiceToken" \
  | grep -v "no menu named" \
  | grep -v "ExecuteMenuItem failed" \
  | grep -v '^\[$' | grep -v '^\]$' | grep -v '^\s*"\s*$' \
  | tr -d '[:space:]' || true)

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

# 2) 최근 commit 중 verifier 흔적 없는 시스템 변경 commit 검출 (D-073 강제)
# .cs / .unity / .asset 파일 건드린 commit인데 message에 'verifier'/'verified'
# 흔적 없으면 block. 사소한 docs/markdown/hook commit은 skip.
if command -v git >/dev/null 2>&1; then
  UNVERIFIED=""
  while IFS='|' read -r SHA SUBJECT; do
    [ -z "$SHA" ] && continue
    # 이 commit이 건드린 파일 — 시스템 변경 (.cs/.unity/.asset) 있나?
    SYS_FILES=$(git show --name-only --format="" "$SHA" 2>/dev/null | grep -E '\.(cs|unity|asset)$' || true)
    [ -z "$SYS_FILES" ] && continue
    # commit body에 'verifier' / 'verified' / 'Verified-By' 키워드 있나?
    BODY=$(git show -s --format=%B "$SHA" 2>/dev/null)
    if ! echo "$BODY" | grep -qiE 'verifier|verified|verify'; then
      # skip 옵션: commit message에 '[skip-verifier]' 명시되어 있으면 OK
      if ! echo "$BODY" | grep -qi 'skip-verifier'; then
        UNVERIFIED="${UNVERIFIED}- ${SHA:0:7} ${SUBJECT:0:80}\n"
      fi
    fi
  done < <(git log --since='30 minutes ago' --format='%H|%s' 2>/dev/null)

  if [ -n "$UNVERIFIED" ]; then
    REASON="최근 30분 시스템 변경(.cs/.unity/.asset) commit 중 verifier 흔적 없음 (D-073 위반):\\n${UNVERIFIED}\\n→ 조치: (a) verifier subagent dispatch 후 commit body에 'Verified-By: <findings>' 추가 (git commit --amend), 또는 (b) 단순 fix면 commit message에 '[skip-verifier]' 명시."
    cat <<EOF
{
  "decision": "block",
  "reason": "${REASON}"
}
EOF
    exit 0
  fi
fi

# 2.5) 화면(UI/카메라/마커) 변경 commit에 'Feels-off:' 한 줄 강제 (game-craft-upgrade ①)
# 화면 파일: .cs (UI/Camera/Marker/Canvas/HUD/Screen/Tooltip 경로) / .unity / .prefab
if command -v git >/dev/null 2>&1; then
  MISSING_FEELS=""
  while IFS='|' read -r SHA SUBJECT; do
    [ -z "$SHA" ] && continue
    # 이 commit이 건드린 화면 파일 있나?
    VISUAL_FILES=$(git show --name-only --format="" "$SHA" 2>/dev/null \
      | grep -E '\.(unity|prefab)$|.*(UI|Camera|Marker|Canvas|HUD|Screen|Tooltip).*\.cs$' || true)
    [ -z "$VISUAL_FILES" ] && continue
    # commit body에 'Feels-off:' 있나?
    BODY=$(git show -s --format=%B "$SHA" 2>/dev/null)
    if ! echo "$BODY" | grep -qi 'Feels-off:'; then
      MISSING_FEELS="${MISSING_FEELS}- ${SHA:0:7} ${SUBJECT:0:80}\n"
    fi
  done < <(git log --since='30 minutes ago' --format='%H|%s' 2>/dev/null)

  if [ -n "$MISSING_FEELS" ]; then
    REASON="최근 30분 화면(UI/카메라/마커/.unity/.prefab) 변경 commit에 'Feels-off:' 누락 (no-compromise §10 위반):\\n${MISSING_FEELS}\\n→ 조치: commit body에 'Feels-off: <아직 어색한 점 1개>' 추가 (git commit --amend). '다 좋음'/'없음' 금지 — 무조건 구체적으로 1개."
    cat <<EOF
{
  "decision": "block",
  "reason": "${REASON}"
}
EOF
    exit 0
  fi
fi

# 3) 이번 turn .cs 변경 체크 (uncommitted) — 도현 manual play reminder
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
    "additionalContext": ".cs 파일 ${CS_CHANGED}개 변경됨 (미커밋). manual play 또는 verifier subagent dispatch 권장."
  }
}
EOF
fi
