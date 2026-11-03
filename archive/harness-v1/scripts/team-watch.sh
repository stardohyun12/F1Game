#!/usr/bin/env bash
# 팀원들이 지금 뭘 하는지 실시간으로 본다 (Ctrl+C 로 종료).
# 사용: bash scripts/team-watch.sh            # 전원
#       bash scripts/team-watch.sh programmer # 한 명
cd "$(dirname "$0")/.." || exit 1
mkdir -p .ai_logs/team
if [ -n "${1:-}" ]; then
  touch ".ai_logs/team/$1.live.log"; tail -n 30 -F ".ai_logs/team/$1.live.log"
else
  for r in programmer qa art; do touch ".ai_logs/team/$r.live.log"; done
  tail -n 15 -F .ai_logs/team/*.live.log
fi
