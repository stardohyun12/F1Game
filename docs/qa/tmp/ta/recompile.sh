#!/usr/bin/env bash
# 에셋 새로고침 → 컴파일 끝날 때까지 대기 → 에러 콘솔
cd D:/Github/F1Game
U="bash scripts/ucli-http.sh"
$U exec 'UnityEditor.AssetDatabase.Refresh(); return "ok";'
sleep 3
for i in $(seq 1 40); do
  r=$($U exec 'return UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating ? "busy" : "idle";' 2>&1 | tail -1)
  [ "$r" = "idle" ] && break
  sleep 3
done
echo "state: $r"
$U console --type error 2>&1 | tail -20
