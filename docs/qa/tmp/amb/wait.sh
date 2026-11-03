#!/usr/bin/env bash
# 컴파일 끝날 때까지 기다림
cd /d/Github/F1Game
bash scripts/ucli-http.sh exec @docs/qa/tmp/amb/refresh.cs
for i in $(seq 1 30); do
  r=$(bash scripts/ucli-http.sh exec @docs/qa/tmp/amb/ready.cs 2>&1)
  echo "$r"
  [ "$r" = "ready" ] && break
  sleep 4
done
bash scripts/ucli-http.sh console 2>&1 | tail -5
