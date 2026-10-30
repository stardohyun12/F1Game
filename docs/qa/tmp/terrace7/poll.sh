#!/bin/bash
# TownPathBot.Status 가 done 이 될 때까지 10초 간격(최대 3분)
cd /d/Github/F1Game
for i in $(seq 1 18); do
  s=$(bash scripts/ucli-http.sh exec 'return TownPathBot.Status;')
  echo "$s"
  case "$s" in done*) exit 0;; esac
  sleep 10
done
