#!/bin/bash
# usage: bash docs/qa/tmp/wait-key.sh <SessionState key> [max seconds]
# SessionState 값에 DONE 이 뜰 때까지 15초마다 확인
key="$1"; max="${2:-600}"; t=0
while [ "$t" -lt "$max" ]; do
  out=$(bash scripts/ucli-http.sh exec "return UnityEditor.SessionState.GetString(\"$key\",\"\");" 2>/dev/null)
  case "$out" in *DONE*) echo "$out"; exit 0;; esac
  sleep 15; t=$((t+15))
done
echo "TIMEOUT $key"
