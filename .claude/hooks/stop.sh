#!/usr/bin/env bash
# PitCrew Stop hook
# Claude turn 종료 직전 자동 검증:
#   1) unity-cli console --type error — 컴파일 에러 있으면 block (Claude 다시 진입해서 fix)
#   2) 이번 turn에 .cs 변경 있으면 manual play 검증 reminder (block 아님)
# Editor 안 켜졌으면 unity-cli timeout — silent skip.

set -euo pipefail

# 머신별 분기는 scripts/ucli.sh 가 한다 (본가 데탑 exe / 노트북 HTTP 대체). 2026-10-12
UNITY_CLI="$(cd "$(dirname "$0")/../.." && pwd)/scripts/ucli.sh"

# 0) Editor 도달성 확인 — 검증 가능 여부를 먼저 안다 (false-green 방지).
# unity-cli status 가 5s 안에 정상 응답하면 1, 아니면 0.
UNITY_REACHABLE=0
if [ -f "$UNITY_CLI" ]; then
  if timeout 5 bash "$UNITY_CLI" status >/dev/null 2>&1; then
    UNITY_REACHABLE=1
  fi
fi

# 0.5) 이번 turn 시스템 파일(.cs/.unity/.asset) 변경 여부 (uncommitted + staged)
SYS_DIRTY=0
if command -v git >/dev/null 2>&1; then
  SYS_DIRTY=$( { git diff --name-only 2>/dev/null; git diff --cached --name-only 2>/dev/null; } \
    | grep -cE '\.(cs|unity|asset)$' || true)
fi

# Editor 미연결인데 시스템 변경 있음 → 검증 불가 = false-green 위험. 크게 경고 (block 아님).
if [ "$UNITY_REACHABLE" -eq 0 ] && [ "$SYS_DIRTY" -gt 0 ]; then
  cat <<EOF
{
  "hookSpecificOutput": {
    "hookEventName": "Stop",
    "additionalContext": "⚠⚠ 검증 불가 (false-green 위험): Editor 미연결(unity-cli status 무응답)인데 시스템 파일(.cs/.unity/.asset) ${SYS_DIRTY}개 변경됨. 컴파일/런타임 검증이 이번 turn에 한 번도 안 돌았다. 완료 보고 금지. → Editor 켜고 'unity-cli editor refresh --compile' + 'bash scripts/regression-check.sh'로 회귀 점검 후 마무리."
  }
}
EOF
fi

# 1) Unity 컴파일 에러 체크 (timeout 5s) — Editor 연결됐을 때만 의미 있음
ERRORS_RAW=""
if [ "$UNITY_REACHABLE" -eq 1 ]; then
  ERRORS_RAW=$(timeout 5 bash "$UNITY_CLI" console --type error 2>/dev/null || true)
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
    BODY=$(git show -s --format=%B "$SHA" 2>/dev/null)
    # skip 옵션: '[skip-verifier]' 명시되면 통과 (단순 fix)
    if echo "$BODY" | grep -qi 'skip-verifier'; then
      continue
    fi
    # 형식 말고 결과 검증 (D-073): 'Verified-By:' 줄 뒤에 실질 내용이 있어야 통과.
    #   - 'Verified-By:' 줄이 없으면 위반.
    #   - 있어도 뒤 내용이 공허("OK"/"통과" 등 한 단어)거나 12자 미만이면 위반.
    VLINE=$(echo "$BODY" | grep -iE '^[[:space:]]*Verified-By:' | head -1 || true)
    if [ -z "$VLINE" ]; then
      UNVERIFIED="${UNVERIFIED}- ${SHA:0:7} ${SUBJECT:0:80} (Verified-By: 줄 없음)\n"
      continue
    fi
    EVIDENCE=$(echo "$VLINE" | sed -E 's/^[[:space:]]*Verified-By:[[:space:]]*//I')
    EVID_TRIM=$(echo "$EVIDENCE" | tr -d '[:space:]')
    # 공허한 한 단어 (대소문자 무시)
    if echo "$EVID_TRIM" | grep -qiE '^(ok|통과|done|pass|passed|yes|완료|확인|green|allgreen)$'; then
      UNVERIFIED="${UNVERIFIED}- ${SHA:0:7} ${SUBJECT:0:80} (Verified-By 내용 공허: '${EVIDENCE}')\n"
    elif [ "${#EVID_TRIM}" -lt 12 ]; then
      UNVERIFIED="${UNVERIFIED}- ${SHA:0:7} ${SUBJECT:0:80} (Verified-By 내용 너무 짧음: '${EVIDENCE}')\n"
    fi
  done < <(git log --since='30 minutes ago' --format='%H|%s' 2>/dev/null)

  if [ -n "$UNVERIFIED" ]; then
    REASON="최근 30분 시스템 변경(.cs/.unity/.asset) commit의 검증 증거 부실 (D-073, 형식 아닌 결과 검증):\\n${UNVERIFIED}\\n→ 조치: (a) verifier subagent dispatch 또는 'bash scripts/regression-check.sh' 실행 후, commit body에 'Verified-By: <실제 측정/단언 결과 12자 이상>' 추가 (git commit --amend). 'OK'/'통과' 같은 한 단어 금지 — 무엇을 어떻게 확인했는지. 또는 (b) 단순 fix면 commit message에 '[skip-verifier]' 명시."
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

# 2.6) 화면 변경 commit에 스크린샷 강제 (game-craft-upgrade ③)
# 화면 파일 건드린 commit은 body에 'Screenshot: <경로>' + 그 파일이 실제 존재해야 함.
# 목적: 화면 바꾸면 결과를 눈으로 보게 강제 (PitCrew/Capture/Auto Screenshot 메뉴로 캡처).
if command -v git >/dev/null 2>&1; then
  MISSING_SHOT=""
  while IFS='|' read -r SHA SUBJECT; do
    [ -z "$SHA" ] && continue
    VISUAL_FILES=$(git show --name-only --format="" "$SHA" 2>/dev/null \
      | grep -E '\.(unity|prefab)$|.*(UI|Camera|Marker|Canvas|HUD|Screen|Tooltip).*\.cs$' || true)
    [ -z "$VISUAL_FILES" ] && continue
    BODY=$(git show -s --format=%B "$SHA" 2>/dev/null)
    # 'Screenshot:' 줄에서 경로 추출
    SHOT_PATH=$(echo "$BODY" | grep -ioE 'Screenshot:[[:space:]]*[^[:space:]]+' | head -1 | sed -E 's/.*Screenshot:[[:space:]]*//I' || true)
    if [ -z "$SHOT_PATH" ] || [ ! -f "$SHOT_PATH" ]; then
      MISSING_SHOT="${MISSING_SHOT}- ${SHA:0:7} ${SUBJECT:0:70} (경로='${SHOT_PATH:-없음}')\n"
    fi
  done < <(git log --since='30 minutes ago' --format='%H|%s' 2>/dev/null)

  if [ -n "$MISSING_SHOT" ]; then
    REASON="최근 30분 화면 변경 commit에 스크린샷 누락 (no-compromise §10 ③ 위반):\\n${MISSING_SHOT}\\n→ 조치: Play 중 'unity-cli menu \"PitCrew/Capture/Auto Screenshot\"' 실행 → docs/screenshots/auto/ 에 png 생성 → commit body에 'Screenshot: docs/screenshots/auto/auto_XXXX.png' 추가 (git commit --amend). 경로의 파일이 실제 존재해야 통과."
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

# 2026-10-13: 게이트 기록(docs/gates/오늘.md)이 변경된 .cs 보다 새로우면 이미 검증된 것 — 매 stop 마다 반복 알림 안 함.
GATE_FILE="$(cd "$(dirname "$0")/../.." && pwd)/docs/gates/$(date +%Y-%m-%d).md"
# 2026-10-14: 자정이 지나 오늘 파일이 아직 없으면 가장 최근 게이트 파일로 (새벽 작업 때 반복 알림 방지)
[ -f "$GATE_FILE" ] || GATE_FILE="$(ls -t "$(cd "$(dirname "$0")/../.." && pwd)"/docs/gates/*.md 2>/dev/null | head -1)"
# 2026-10-20: AionUi 팀 모드(PIPELINE §2)는 게이트 기록을 docs/qa/GATE-*.md 에 쓴다 — 둘 중 더 새 파일을 기준으로.
_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
_QA_GATE="$(ls -t "$_ROOT"/docs/qa/GATE-*.md 2>/dev/null | grep -v TEMPLATE | head -1)"
if [ -n "$_QA_GATE" ] && { [ ! -f "$GATE_FILE" ] || [ "$(stat -c %Y "$_QA_GATE")" -gt "$(stat -c %Y "$GATE_FILE")" ]; }; then GATE_FILE="$_QA_GATE"; fi
GATED=0
if [ "$CS_CHANGED" -gt 0 ] && [ -f "$GATE_FILE" ]; then
  NEWEST_CS=0
  for f in $( { git diff --name-only; git diff --cached --name-only; } 2>/dev/null | grep "\.cs$"); do
    m=$(stat -c %Y "$f" 2>/dev/null || echo 0); [ "$m" -gt "$NEWEST_CS" ] && NEWEST_CS=$m
  done
  GATE_M=$(stat -c %Y "$GATE_FILE" 2>/dev/null || echo 0)
  [ "$GATE_M" -ge "$NEWEST_CS" ] && GATED=1
fi

if [ "$CS_CHANGED" -gt 0 ] && [ "$GATED" -eq 0 ]; then
  cat <<EOF
{
  "hookSpecificOutput": {
    "hookEventName": "Stop",
    "additionalContext": ".cs 파일 ${CS_CHANGED}개 변경됨 (미커밋). manual play 또는 verifier subagent dispatch 권장."
  }
}
EOF
fi
