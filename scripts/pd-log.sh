#!/usr/bin/env bash
# PD(Hermes) 활동을 대시보드에 남긴다. 사용: bash scripts/pd-log.sh "<내용>" [task "<과제>"|idle]
cd "$(dirname "$0")/.." || exit 1; mkdir -p .ai_logs/team .claude/team/sessions
L=.ai_logs/team/pd.live.log; T=$(date +%H:%M:%S)
case "${2:-}" in
  task) printf '\n[%s] pd         | ===== 새 과제 =====\n[%s] pd         | 📋 %s\n' "$T" "$T" "$3" >> $L; echo "working $(date +%H:%M)" > .claude/team/sessions/pd.state;;
  idle) echo "idle $(date +%H:%M)" > .claude/team/sessions/pd.state;;
esac
[ -n "${1:-}" ] && printf '[%s] pd         | %s\n' "$T" "$1" >> $L
