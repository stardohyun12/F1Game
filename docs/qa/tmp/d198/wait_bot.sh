#!/usr/bin/env bash
# TownPathBot 이 done 이 될 때까지 10초마다 확인 (최대 ~9분)
for i in $(seq 1 55); do
  s=$(bash scripts/ucli-http.sh exec 'return TownPathBot.Status;' 2>&1 | tail -1)
  if [[ "$s" == done* ]]; then break; fi
  sleep 10
done
echo "$s"
