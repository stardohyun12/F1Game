#!/usr/bin/env bash
# 봇 결과(SessionState a4e.wall)가 끝날 때까지 기다렸다 출력. 최대 ~180초.
cd /d/Github/F1Game
n=0
while [ $n -lt 36 ]; do
  out=$(bash scripts/ucli-http.sh exec "return UnityEditor.SessionState.GetString(\"a4e.wall\",\"\");" 2>/dev/null)
  case "$out" in *km/h*) echo "$out"; exit 0 ;; esac
  n=$((n+1)); sleep 5
done
echo "TIMEOUT: $out"
