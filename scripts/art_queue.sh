#!/usr/bin/env bash
# Codex 아트 줄(D-259): queue/art/*.task 를 순서대로 team.sh art 로. 끝날 때마다 텔레그램 알림 + BATCH.md. Codex 한도 걸리면 멈추고 알림.
cd /d/Github/F1Game || exit 1
Q=.claude/team/queue/art; mkdir -p $Q/done $Q/hold
PY=/c/Users/rtx40/AppData/Local/Programs/Python/Python313/python.exe
PIDF=$Q.pid; if [ -s "$PIDF" ] && kill -0 "$(cat $PIDF)" 2>/dev/null; then exit 0; fi; echo $$ > $PIDF
log(){ echo "[$(date '+%m/%d %H:%M')] queue/art $*" >> .ai_logs/team/queue.log; }
while true; do
  T=$(ls $Q/*.task 2>/dev/null | sort | head -1)
  [ -z "$T" ] && { sleep 120; continue; }
  N=$(basename "$T" .task); mv "$T" $Q/done/
  log "시작 $N (Codex)"
  OUT=$(bash scripts/team.sh art "$(cat $Q/done/$N.task)" 2>&1); echo "$OUT" | tail -40 > $Q/done/$N.out
  if echo "$OUT" | grep -qiE "rate.?limit|usage limit|quota|429"; then
    log "보류 $N — Codex 한도"; mv $Q/done/$N.task $Q/hold/; $PY -X utf8 scripts/tg_notify.py "⚠️ Codex 아트: 한도에 걸려 멈춤 ($N)" >/dev/null; sleep 3600; continue
  fi
  git checkout -- F1/Assets/UI/Fonts 2>/dev/null
  git add art/_review/2026-11-02-* art/source/depot-props art/source/packing-materials art/source/sunset-props art/source/paper-things art/provenance.csv docs/team/reports/art-* 2>/dev/null
  git commit -qm "art(Codex): $N [skip-verifier]" 2>/dev/null && timeout 600 git push -q
  C=$(git log --oneline -1 | cut -c1-8); log "커밋 $C art: $N"
  SHEET=$(ls -t art/_review/2026-11-02-*/*.png 2>/dev/null | head -1)
  $PY -X utf8 scripts/tg_notify.py "🎨 Codex 아트 후보 끝: $N ($C) — 확인은 BATCH 에서 한 번에" "$SHEET" >/dev/null
  printf '\n## %s 🎨 아트 후보 %s (%s)\n시트: %s\n' "$(date '+%m/%d %H:%M')" "$N" "$C" "$SHEET" >> docs/review/BATCH.md
done
