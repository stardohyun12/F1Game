#!/usr/bin/env bash
# 봇이 끝나거나(arrived/FAIL/done) 제한 시간(초, $1)까지 기다린다.
lim=${1:-500}; t0=$(date +%s)
until s=$(bash scripts/ucli-http.sh exec @docs/qa/tmp/uitex/status.cs 2>&1); echo "$s" | grep -qE "^(arrived|FAIL|done)|FAIL:"; do
  [ $(( $(date +%s) - t0 )) -gt "$lim" ] && { echo "TIMEOUT $s"; exit 0; }
  sleep 5
done
echo "$s"
