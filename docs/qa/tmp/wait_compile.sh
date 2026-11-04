#!/usr/bin/env bash
# 컴파일 끝날 때까지 기다린다(최대 ~3분)
cd /d/Github/F1Game
for i in $(seq 1 36); do
  r=$(bash scripts/ucli-http.sh exec @docs/qa/tmp/is_compiling.cs 2>&1 | tail -1)
  case "$r" in *idle*) echo "idle after $i"; exit 0;; esac
  sleep 5
done
echo "timeout: $r"
