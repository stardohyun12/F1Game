#!/bin/bash
# C-003: 컴파일 끝날 때까지 기다리고 에러 콘솔 출력
cd /d/Github/F1Game
for i in $(seq 1 60); do
  r=$(bash scripts/ucli-http.sh exec 'return UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating ? "busy" : "idle " + (typeof(OnFootDirector).GetProperty("DeliveryCaption") != null);' 2>/dev/null | tail -1)
  if echo "$r" | grep -q "^idle"; then echo "$r"; break; fi
  sleep 3
done
bash scripts/ucli-http.sh console --type error | tail -5
