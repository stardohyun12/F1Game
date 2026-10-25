#!/usr/bin/env bash
# QA 0313: 컴파일 끝날 때까지 대기 후 dll 시각
sleep 5
for i in $(seq 1 60); do
  r=$(bash scripts/ucli-http.sh exec 'return UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating ? "c" : "ok";' 2>&1)
  [ "$r" = "ok" ] && break
  sleep 3
done
echo "state=$r iter=$i"
ls -la --time-style=full-iso F1/Library/ScriptAssemblies/PitCrew*.dll
date
