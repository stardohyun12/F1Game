#!/usr/bin/env bash
# 1회용(D-260): 본 에디터 줄·backlog·아트 줄이 다 비고 아무도 안 일하면 회고 과제를 research(Opus)로 돌림 → 커밋·알림·BATCH.
cd /d/Github/F1Game || exit 1
PY=/c/Users/rtx40/AppData/Local/Programs/Python/Python313/python.exe
idle(){ ! ls .claude/team/queue/editor/*.task .claude/team/queue/editor/backlog/*.task .claude/team/queue/art/*.task >/dev/null 2>&1 \
  && ! grep -qs '^working' .claude/team/sessions/programmer.state .claude/team/sessions/art.state; }
: # 도현 지시로 바로 시작(D-262)
echo "[$(date '+%m/%d %H:%M')] 회고 시작(research Opus)" >> .ai_logs/team/queue.log
$PY -X utf8 scripts/tg_notify.py "▶ 쌓인 작업 다 끝남 → 회고(제작 방식·AI·컴퓨터 사용) 시작" >/dev/null
mv -f .claude/team/sessions/research.id .claude/team/sessions/archive/research-pre-retro.id 2>/dev/null
bash scripts/team.sh research "$(cat .claude/team/sessions/retro-1102.task)" > .claude/team/sessions/retro-1102.out 2>&1
if [ -s docs/retro/2026-11-02-retro.md ]; then
  git add docs/retro docs/team/reports/research-* && git commit -qm "research: 회고 D-260 [skip-verifier]" -q; timeout 300 git push -q
  C=$(git log --oneline -1 | cut -c1-8); echo "[$(date '+%m/%d %H:%M')] 회고 끝 $C" >> .ai_logs/team/queue.log
  $PY -X utf8 scripts/tg_notify.py "✅ 회고 초안 끝 ($C) docs/retro/2026-11-02-retro.md — PD 가 읽고 정리해서 보낼게" >/dev/null
  printf '\n## %s 📝 회고 초안 (%s) — docs/retro/2026-11-02-retro.md\n' "$(date '+%m/%d %H:%M')" "$C" >> docs/review/BATCH.md
else
  echo "[$(date '+%m/%d %H:%M')] 회고 실패(문서 없음)" >> .ai_logs/team/queue.log
  $PY -X utf8 scripts/tg_notify.py "⚠️ 회고 작업 실패 — 문서 없음. PD 확인 필요" >/dev/null
fi
