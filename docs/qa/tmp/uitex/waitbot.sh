#!/bin/bash
# 봇 끝(arrived/FAIL) 까지 15초마다 확인, 최대 10분
cd /d/Github/F1Game
for i in $(seq 1 40); do
  s=$(bash scripts/ucli-http.sh exec @docs/qa/tmp/uitex/status.cs 2>&1)
  if echo "$s" | grep -qE "arrived|FAIL|rror"; then echo "$s"; exit 0; fi
  sleep 15
done
echo "timeout: $s"
