#!/usr/bin/env bash
# 1회용: 지금 도는 과제가 끝나고 줄이 비면 창 있는 대기열을 끄고 창 없는(headless) 대기열로 갈아탐.
cd /d/Github/F1Game || exit 1
T="$1"
for i in $(seq 1 400); do grep -qE "(커밋 .*|보류 )$T" .ai_logs/team/queue.log && ! grep -q '^working' .claude/team/sessions/programmer.state && break; sleep 20; done
mkdir -p .claude/team/queue/editor/staged; mv .claude/team/queue/editor/*.task .claude/team/queue/editor/staged/ 2>/dev/null
sleep 5
powershell -NoProfile -Command "Get-CimInstance Win32_Process | ? { \$_.Name -eq 'bash.exe' -and \$_.CommandLine -match 'bash scripts/team_queue.sh' -and \$_.CommandLine -notmatch 'Get-CimInstance' } | % { taskkill /F /T /PID \$_.ProcessId 2>&1 | Out-Null }"
sleep 3; rm -f .claude/team/queue/*.pid
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/start_queues.ps1 >> .ai_logs/team/queue.log 2>&1
echo "[$(date '+%m/%d %H:%M')] PD: 대기열 창 없는 실행으로 전환" >> .ai_logs/team/queue.log
