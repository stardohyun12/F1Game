#!/bin/bash
# OnFootRouteBot.Status 가 done/FAIL 이 될 때까지 기다린다(최대 ~200초).
for i in $(seq 1 70); do
  s=$(bash scripts/ucli-http.sh exec 'return OnFootRouteBot.Status;')
  if [[ "$s" == done* || "$s" == FAIL* ]]; then echo "$s"; exit 0; fi
  sleep 3
done
echo "timeout: $s"
