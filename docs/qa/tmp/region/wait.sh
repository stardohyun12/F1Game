#!/usr/bin/env bash
# 컴파일 끝날 때까지 기다림 (최대 3분)
cd "$(dirname "$0")/../../../.."
for i in $(seq 1 36); do
  r=$(bash scripts/ucli-http.sh exec 'return UnityEditor.EditorApplication.isCompiling ? "busy" : "idle";' 2>&1 | tail -1)
  [ "$r" = "idle" ] && break
  sleep 5
done
echo "compile: $r"
bash scripts/ucli-http.sh console --type error 2>&1 | head -8
