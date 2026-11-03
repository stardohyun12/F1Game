#!/bin/bash
# 컴파일 끝날 때까지 기다림(최대 ~2분)
cd /d/Github/F1Game
for i in $(seq 1 24); do
  r=$(bash scripts/ucli-http.sh exec @docs/qa/tmp/depot/compiled.cs 2>&1 | tail -1)
  echo "$r"
  case "$r" in done*) exit 0;; esac
  timeout 5 tail -f /dev/null
done
