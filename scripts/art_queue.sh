#!/usr/bin/env bash
# Codex 아트 줄(D-259): queue/art/*.task 를 순서대로 team.sh art 로(엔진 = config art.engine, 기본 codex). Codex 한도 걸리면 멈추고 알림.
# 2026-11-03 팀 재편(docs/team/FLOW.md): art 일꾼은 하나 — studio 줄(Claude 아트)과 동시에 돌지 않게 art.state 를 본다.
# 카드 과제(이름에 C-NNN)는 끝나면 card.py advance(아트 → 통합 → build 대기열).
cd /d/Github/F1Game || exit 1
export GIT_TERMINAL_PROMPT=0 GIT_ASKPASS= SSH_ASKPASS= GCM_INTERACTIVE=never  # 창 없는 실행에서 GitHub 로그인 창 금지(D-268)
Q=.claude/team/queue/art; mkdir -p $Q/done $Q/hold
PY=/c/Users/rtx40/AppData/Local/Programs/Python/Python313/python.exe
PIDF=$Q.pid; if [ -s "$PIDF" ] && kill -0 "$(cat $PIDF)" 2>/dev/null; then exit 0; fi; echo $$ > $PIDF
log(){ echo "[$(date '+%m/%d %H:%M')] queue/art $*" >> .ai_logs/team/queue.log; }
while true; do
  T=$(ls $Q/*.task 2>/dev/null | sort | head -1)
  [ -z "$T" ] && { sleep 120; continue; }
  grep -q '^working' .claude/team/sessions/art.state 2>/dev/null && { sleep 60; continue; }   # 일꾼마다 카드 1장
  N=$(basename "$T" .task); mv "$T" $Q/done/
  CARD=$(printf '%s' "$N" | grep -oE 'C-[0-9]{3}' | head -1)
  log "시작 $N (art)"
  OUT=$(bash scripts/team.sh art "$(cat $Q/done/$N.task)" 2>&1); echo "$OUT" | tail -40 > $Q/done/$N.out
  if echo "$OUT" | grep -qiE "rate.?limit|usage limit|quota|429"; then
    log "보류 $N — Codex 한도"; mv $Q/done/$N.task $Q/hold/; $PY -X utf8 scripts/tg_notify.py "⚠️ Codex 아트: 한도에 걸려 멈춤 ($N)" >/dev/null
    [ -n "$CARD" ] && $PY -X utf8 scripts/card.py hold "$CARD" "Codex 한도" >/dev/null 2>&1
    sleep 3600; continue
  fi
  git checkout -- F1/Assets/UI/Fonts 2>/dev/null
  git add $(ls -d art/_review/$(date +%Y-%m-%d)-* 2>/dev/null) art/source art/provenance.csv docs/team/reports/art-* docs/team/cards docs/team/handoffs.jsonl docs/team/board.json 2>/dev/null
  git commit -qm "art: $N [skip-verifier]" 2>/dev/null && timeout 600 git push -q
  C=$(git log --oneline -1 | cut -c1-8); log "커밋 $C art: $N"
  if [ -n "$CARD" ]; then
    $PY -X utf8 scripts/card.py advance "$CARD" >> $Q/done/$N.out 2>&1
    git add docs/team/cards docs/team/handoffs.jsonl docs/team/board.json 2>/dev/null; git commit -qm "card: $CARD art 끝 [skip-verifier]" 2>/dev/null && timeout 600 git push -q
  fi
done
