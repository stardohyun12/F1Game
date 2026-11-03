#!/usr/bin/env bash
# 컴파일이 끝날 때까지 기다린 뒤 콘솔 오류를 보여 준다.
for i in 1 2 3 4 5 6 7 8 9 10 11 12; do
  r=$(bash scripts/ucli-http.sh exec 'return UnityEditor.EditorApplication.isCompiling.ToString();' 2>&1 | tail -1)
  echo "$i $r"
  [ "$r" = "False" ] && break
  sleep 10
done
bash scripts/ucli-http.sh console 2>&1 | tail -30
