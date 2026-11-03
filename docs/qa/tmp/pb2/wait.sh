#!/bin/bash
# wait.sh <cs file> <grep pattern> [max sec] — exec 결과에 패턴이 나올 때까지 기다린다
cd "$(dirname "$0")/../../../.."
max=${3:-240}
for ((i=0; i<max; i+=3)); do
  r=$(bash scripts/ucli-http.sh exec "@$1" 2>&1 | tail -1)
  if echo "$r" | grep -q -- "$2"; then echo "OK $r"; exit 0; fi
  sleep 3
done
echo "TIMEOUT $r"; exit 1
