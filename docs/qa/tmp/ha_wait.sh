#!/usr/bin/env bash
# DriveFeelProbe 가 끝날 때까지 기다린다 (최대 ~9분)
cd /d/Github/F1Game
for i in $(seq 1 60); do
  r=$(bash scripts/ucli-http.sh exec 'return DriveFeelProbe.Busy + " " + DriveFeelProbe.Status;' 2>&1 | tail -1)
  case "$r" in False*) echo "$r"; exit 0;; esac
  sleep 8
done
echo "timeout $r"
