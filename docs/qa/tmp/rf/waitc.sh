#!/bin/bash
# 컴파일 끝날 때까지 기다림(최대 3분)
cd /d/Github/F1Game
for i in $(seq 1 60); do
  r=$(bash scripts/ucli-http.sh exec 'return UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating ? "busy" : "idle";' 2>&1 | tail -1)
  if [ "$r" = "idle" ]; then echo idle; exit 0; fi
  sleep 3
done
echo timeout
