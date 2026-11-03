#!/usr/bin/env bash
# 컴파일·임포트 끝날 때까지 기다린 뒤 콘솔 에러 출력 (D-252 작업용)
cd /d/Github/F1Game
for i in $(seq 1 80); do
  r=$(bash scripts/ucli-http.sh exec 'return (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating) ? "busy" : "idle";' 2>/dev/null)
  [ "$r" = "idle" ] && break
  sleep 3
done
echo "state=$r"
bash scripts/ucli-http.sh console --type error
