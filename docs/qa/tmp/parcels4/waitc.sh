#!/bin/bash
# 컴파일 끝날 때까지 기다린 뒤 새 코드(nWin 필드)가 올라왔는지 확인
cd /d/Github/F1Game
for i in $(seq 1 30); do
  r=$(bash scripts/ucli-http.sh exec 'return (UnityEditor.EditorApplication.isCompiling||UnityEditor.EditorApplication.isUpdating) ? "busy" : "idle";' 2>&1 | tail -1)
  [ "$r" = "idle" ] && break
  sleep 4
done
echo "state=$r"
bash scripts/ucli-http.sh exec 'return typeof(ParcelBuild).GetField("nWin", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static) != null;' 2>&1 | tail -2
