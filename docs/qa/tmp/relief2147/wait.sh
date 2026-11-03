#!/usr/bin/env bash
# 컴파일 끝날 때까지 chk.cs 반복 (최대 ~3분)
cd "$(dirname "$0")/../../../.."
for i in $(seq 1 22); do
  r=$(bash scripts/ucli-http.sh exec @docs/qa/tmp/relief2147/chk.cs 2>&1)
  echo "$r"
  if echo "$r" | grep -q "idle hasRelief"; then exit 0; fi
  sleep 8
done
exit 1
