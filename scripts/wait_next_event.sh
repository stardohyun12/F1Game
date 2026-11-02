#!/usr/bin/env bash
# PD 깨우기(D-263): queue.log 에 다음 '끝' 사건(커밋·보류·회고/기획 끝)이 생기면 그 줄을 찍고 끝남.
# Hermes terminal(background, notify_on_complete) 로 띄우면 끝날 때 PD 가 대화로 깨어나 결과를 보고한다.
cd /d/Github/F1Game || exit 1
L=.ai_logs/team/queue.log
N=$(wc -l < "$L")
for i in $(seq 1 1440); do
  NEW=$(tail -n +$((N+1)) "$L" | grep -aE "커밋 |보류 |회고 끝|기획 끝|실패" | head -1)
  [ -n "$NEW" ] && { echo "$NEW"; exit 0; }
  sleep 30
done
echo "12시간 동안 끝난 과제 없음"
