#!/bin/bash
# 컴파일 끝날 때까지 기다리고 Editor.log 의 error CS 마지막 15줄
cd /d/Github/F1Game
for i in $(seq 1 40); do
  r=$(bash scripts/ucli-http.sh exec 'return UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating;' 2>&1 | tail -1)
  [ "$r" = "False" ] && break
  sleep 4
done
echo "compiling=$r"
grep -a "error CS" "$LOCALAPPDATA/Unity/Editor/Editor.log" | tail -15
