#!/usr/bin/env bash
# 팀 한눈 요약: 누가 일하는 중인지 · 마지막 활동 · 에디터 잠금 · 최근 보고서.
cd "$(dirname "$0")/.." || exit 1
echo "== 팀 상태 $(date +%H:%M) =="
for r in programmer qa art artui research; do
  st=$(cat ".claude/team/sessions/$r.state" 2>/dev/null || echo "없음")
  last=$(tail -n 1 ".ai_logs/team/$r.live.log" 2>/dev/null | cut -c1-140)
  printf '%-10s %s\n           └ %s\n' "$r" "$st" "${last:-(기록 없음)}"
done
echo "에디터 잠금: $(cat .claude/team/editor.lock 2>/dev/null || echo 없음)"
echo "대기 줄:"; python scripts/team_wait.py list | sed 's/^/  /'
echo "최근 보고서:"; ls -t docs/team/reports/*.md 2>/dev/null | head -5 | sed 's/^/  /'
