#!/usr/bin/env bash
# 2253: 새로 고침 → 컴파일 끝날 때까지 chk.cs 반복 (최대 ~3분)
cd "$(dirname "$0")/../../../.."
bash scripts/ucli-http.sh exec 'UnityEditor.AssetDatabase.Refresh(); return "refresh";' 2>&1 | tail -1
for i in $(seq 1 22); do
  r=$(bash scripts/ucli-http.sh exec @docs/qa/tmp/relief3/chk.cs 2>&1)
  echo "$r"
  if echo "$r" | grep -q "idle has2253 True"; then exit 0; fi
  sleep 8
done
exit 1
