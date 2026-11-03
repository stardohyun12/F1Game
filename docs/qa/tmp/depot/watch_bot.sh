#!/bin/bash
# 봇 Status 가 바뀔 때마다 한 줄, 끝(done/FAIL/arrived)이면 종료
cd /d/Github/F1Game
prev=""
for i in $(seq 1 120); do
  s=$(bash scripts/ucli-http.sh exec @docs/qa/tmp/depot/bot_status.cs 2>&1 | tail -1)
  st=$(echo "$s" | cut -d'|' -f1)
  if [ "$st" != "$prev" ]; then echo "$s"; prev="$st"; fi
  case "$st" in done*|FAIL*|arrived*) exit 0;; esac
  sleep 5
done
