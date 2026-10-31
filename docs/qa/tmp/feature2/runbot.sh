#!/usr/bin/env bash
# 사본 에디터에서 Play → 봇 시작 → arrived/FAIL 까지 상태 변화만 출력. 사용: bash runbot.sh [최대초]
cd /d/Github/F1Game
D=docs/qa/tmp/feature2
U="bash scripts/ucli-http.sh"
$U editor play --wait
$U exec @$D/bot_start.cs
last=""; end=$((SECONDS + ${1:-560}))
while [ $SECONDS -lt $end ]; do
  s=$($U exec @$D/bot_status.cs)
  key="${s%% frame=*}"
  if [ "$key" != "$last" ]; then echo "[$SECONDS s] $s"; last="$key"; fi
  case "$key" in status=FAIL*|status=arrived|*"no director"*) break;; esac
  sleep 5
done
