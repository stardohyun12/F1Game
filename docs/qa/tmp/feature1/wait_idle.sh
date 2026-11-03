#!/usr/bin/env bash
# 사본 에디터가 컴파일·리로드를 끝내고 응답할 때까지 기다린다(최대 ~3분).
for i in $(seq 1 60); do
  r=$(bash scripts/ucli-http.sh exec 'return UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating ? "busy" : "idle";' 2>/dev/null)
  [ "$r" = "idle" ] && { echo "idle after ${i}"; exit 0; }
  sleep 3
done
echo "timeout"; exit 1
