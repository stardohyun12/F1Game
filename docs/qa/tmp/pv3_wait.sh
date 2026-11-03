#!/usr/bin/env bash
# 컴파일 끝날 때까지 기다린 뒤 에러 콘솔
cd /d/Github/F1Game
echo 'return UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating ? "c" : "idle";' > docs/qa/tmp/pv3_isc.cs
sleep 4
for i in $(seq 1 60); do
  r=$(bash scripts/ucli-http.sh exec @docs/qa/tmp/pv3_isc.cs 2>&1)
  [ "$r" = "idle" ] && break
  sleep 3
done
echo "state=$r"
bash scripts/ucli-http.sh console --type error
