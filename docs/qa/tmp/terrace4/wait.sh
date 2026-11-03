#!/bin/sh
# 도메인 리로드 끝날 때까지 기다린 뒤 컴파일 실패 여부 출력
for i in $(seq 1 24); do
  r=$(bash scripts/ucli-http.sh exec 'return UnityEditor.EditorApplication.isCompiling+" failed="+UnityEditor.EditorUtility.scriptCompilationFailed;' 2>&1 | tail -1)
  case "$r" in FAIL*|True*) sleep 5;; *) echo "$r"; exit 0;; esac
done
echo timeout
