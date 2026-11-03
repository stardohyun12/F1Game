#!/bin/bash
# D-286/287 분리 전·후 하루 한 바퀴 로그: bash docs/qa/tmp/dlc_run.sh before|after
cd /d/Github/F1Game
mkdir -p docs/qa/20261103-2102
echo "$1.json" > docs/qa/tmp/dlc_name.txt
bash scripts/ucli-http.sh editor play --wait | tail -1
sleep 2
bash scripts/ucli-http.sh exec @docs/qa/tmp/dlc_begin.cs; echo
for i in $(seq 1 40); do
  sleep 4
  s=$(bash scripts/ucli-http.sh exec @docs/qa/tmp/dlc_status.cs 2>&1)
  if [[ "$s" == *done* ]]; then break; fi
done
echo "status: $s"
