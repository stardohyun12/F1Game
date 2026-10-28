#!/bin/bash
# 봇 끝날 때까지 20초마다 상태 확인, 끝나면 결과 파일 출력
cd /d/Github/F1Game
while true; do
  s=$(bash scripts/ucli-http.sh exec 'return TownPathBot.Status;' 2>&1 | tail -1)
  echo "$s" | grep -q done && break
  sleep 20
done
cat docs/qa/tmp/townv6-bot-loopBox.txt
