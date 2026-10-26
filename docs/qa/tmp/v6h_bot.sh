#!/bin/bash
# 과제 0640 — Play 진입 → 봇 cover(Box, Wheels 차례) → 결과를 docs/qa/tmp/v6h/<tag>-<mode>-* 로 복사 → stop·폰트
# 사용: bash docs/qa/tmp/v6h_bot.sh <tag>
cd /d/Github/F1Game
TAG=$1; U="bash scripts/ucli-http.sh"
for M in $2; do
  $U editor play --wait | tail -1
  $U exec 'Application.runInBackground = true; return "rib";' | tail -1
  sed "s/DriveMode.Box;/DriveMode.$M;/" docs/qa/tmp/v6h_bot.cs > docs/qa/tmp/v6h_bot_run.cs
  $U exec @docs/qa/tmp/v6h_bot_run.cs | tail -1
  for i in $(seq 1 120); do
    sleep 10
    S=$($U exec 'return TownPathBot.Status;' | tail -1)
    case "$S" in done*) break;; esac
  done
  echo "$M: $S"
  cp docs/qa/tmp/townv6-bot-cover.txt docs/qa/tmp/v6h/$TAG-$M-bot.txt
  cp docs/qa/tmp/townv6-bot-cover-feel.tsv docs/qa/tmp/v6h/$TAG-$M-bot-feel.tsv
  $U exec 'Time.timeScale = 1f; return "ts";' | tail -1
  $U editor stop | tail -1
  git checkout -- "F1/Assets/UI/Fonts/Korean/" "F1/Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset"
done
echo ALLDONE
