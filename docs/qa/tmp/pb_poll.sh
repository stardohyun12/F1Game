#!/usr/bin/env bash
# PackBench 봇이 끝날 때까지(BotStatus 가 done 으로 시작) 3초마다 확인. 최대 $1 번(기본 20).
cd "$(dirname "$0")/../../.."
n="${1:-20}"
for i in $(seq 1 "$n"); do
  r=$(bash scripts/ucli-http.sh exec @docs/qa/tmp/packbench_status.cs 2>&1 | tail -1)
  case "$r" in done*) echo "$r"; exit 0;; esac
  sleep 3
done
echo "TIMEOUT: $r"; exit 1
