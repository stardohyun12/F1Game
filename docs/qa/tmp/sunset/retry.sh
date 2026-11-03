#!/bin/bash
# usage: retry.sh <file.cs> — editor 가 리로드 중이면 기다렸다 다시
for i in $(seq 1 15); do
  out=$(bash scripts/ucli-http.sh exec @"$1" 2>&1 | tail -5)
  if ! echo "$out" | grep -q "request error"; then echo "$out"; exit 0; fi
  python -c "import time;time.sleep(6)"
done
echo "$out"
