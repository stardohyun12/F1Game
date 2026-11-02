#!/usr/bin/env bash
# 리서치 과제 1개를 돌리고 커밋 + queue.log 에 '기획 끝' 기록(PD 깨우기 wait_next_event 가 잡음). 사용: run_research_once.sh <task파일> <결과md>
cd /d/Github/F1Game || exit 1
export GIT_TERMINAL_PROMPT=0 GIT_ASKPASS= SSH_ASKPASS= GCM_INTERACTIVE=never  # 창 없는 실행에서 GitHub 로그인 창 금지(D-268)
T="$1"; OUTMD="$2"; N=$(basename "$T" .task)
mv -f .claude/team/sessions/research.id .claude/team/sessions/archive/research-pre-$N.id 2>/dev/null
echo "[$(date '+%m/%d %H:%M')] 리서치 시작 $N" >> .ai_logs/team/queue.log
bash scripts/team.sh research "$(cat "$T")" > .claude/team/sessions/$N.out 2>&1
if [ -s "$OUTMD" ]; then git add "$OUTMD" docs/team/reports/research-* && git commit -qm "research: $N [skip-verifier]" -q; timeout 300 git push -q
  echo "[$(date '+%m/%d %H:%M')] 기획 끝 $(git log --oneline -1 | cut -c1-8) 리서치 $N" >> .ai_logs/team/queue.log
else echo "[$(date '+%m/%d %H:%M')] 리서치 실패 $N (문서 없음)" >> .ai_logs/team/queue.log; fi
