#!/usr/bin/env bash
# PitCrew SessionEnd hook
# 세션 종료 시 자동으로 session-log.md에 stub 블록 추가.
# 다음 세션 시작 시 사용자가 한 일을 채워넣음 (또는 자동 요약 LLM 추가 가능).

set -euo pipefail

DATE=$(date '+%Y-%m-%d %H:%M')

# session-log.md가 없으면 스킵
if [ ! -f "notes/session-log.md" ]; then
  exit 0
fi

# 이미 같은 날짜 stub이 있으면 스킵 (중복 방지)
if tail -50 notes/session-log.md | grep -q "## ${DATE%% *} — Auto-End stub"; then
  exit 0
fi

cat >> notes/session-log.md <<EOF

## ${DATE%% *} — Auto-End stub (${DATE##* })
- (다음 세션 시작 시 채움: 한 일 / 다음엔 / 메모)
EOF

exit 0
