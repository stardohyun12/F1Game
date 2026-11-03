#!/bin/bash
# 봇 Status 가 바뀔 때마다 한 줄, done/FAIL 이면 끝
cd /d/Github/F1Game
prev=""
while true; do
  s=$(bash scripts/ucli-http.sh exec 'return OnFootRouteBot.Status;' 2>&1 || true)
  if [ "$s" != "$prev" ]; then echo "$s"; prev="$s"; fi
  case "$s" in done*|FAIL*|*rror*) break;; esac
  sleep 5
done
