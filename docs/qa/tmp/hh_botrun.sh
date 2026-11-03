#!/usr/bin/env bash
# 20261030-2334: Play 중 OnFootRouteBot 을 돌리고 끝날 때까지 기다림
cd /d/Github/F1Game
bash scripts/ucli-http.sh exec 'return OnFootRouteBot.Begin("D:/Github/F1Game/docs/qa/evidence/feature1b", "D:/Github/F1Game/docs/qa/tmp/feature1b");'
for i in $(seq 1 36); do
  sleep 5
  s=$(bash scripts/ucli-http.sh exec 'return OnFootRouteBot.Status;')
  echo "$i $s"
  case "$s" in done*|FAIL*) break;; esac
done
cat docs/qa/tmp/feature1b/route.txt
