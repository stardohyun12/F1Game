#!/usr/bin/env bash
# 컴파일 끝날 때까지 대기 후 에러 출력
cd /d/Github/F1Game
for i in $(seq 1 60); do
  r=$(bash scripts/ucli-http.sh exec 'return UnityEditor.EditorApplication.isCompiling||UnityEditor.EditorApplication.isUpdating?"c":"r";' 2>/dev/null)
  [ "$r" = "r" ] && break
  sleep 3
done
echo "state=$r"; date +%H:%M
bash scripts/ucli-http.sh console --type error
