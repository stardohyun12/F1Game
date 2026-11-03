#!/bin/bash
# 기능 2 봇 상태를 10초마다 — 끝나면 멈춘다.
for i in $(seq 1 30); do
  s=$(bash scripts/ucli-http.sh exec @docs/qa/tmp/feature2/bot_status.cs)
  echo "[$i] ${s:0:300}"
  case "$s" in *done*|*arrived*|*delivered=True*|*fail*|*FAIL*) break;; esac
  sleep 10
done
