#!/bin/bash
# DestClarityShots.Status 가 running 이 아닐 때까지 5초마다 확인(최대 $1 초). 사용: bash poll_shots.sh 180
lim=${1:-180}; t=0
while [ $t -lt $lim ]; do
  s=$(bash D:/Github/F1Game/scripts/ucli-http.sh exec 'return DestClarityShots.Status;' 2>&1 | tail -1)
  if [ "$s" != "running" ]; then echo "status=$s t=${t}s"; exit 0; fi
  sleep 5; t=$((t+5))
done
echo "timeout status=$s"
