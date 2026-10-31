#!/usr/bin/env bash
# 사본 에디터(8095)가 exec 에 답할 때까지 기다린다(최대 약 30분).
cd /d/Github/F1Game
for i in $(seq 1 30); do
  if timeout 45 bash scripts/ucli-http.sh exec 'return "alive compiling=" + UnityEditor.EditorApplication.isCompiling;'; then
    date "+%H:%M:%S"; exit 0
  fi
  sleep 15
done
echo "still blocked"; date "+%H:%M:%S"; exit 1
