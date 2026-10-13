#!/usr/bin/env bash
# unity-cli 디스패처 — 본가 데탑(stard)의 unity-cli.exe 가 있으면 그걸, 없으면(이 노트북) HTTP 대체(ucli-http.sh).
# stop.sh / regression-check.sh 가 머신에 상관없이 같은 명령으로 Editor 를 때리게 한다. (2026-10-12)
EXE="C:/Users/stard/AppData/Local/unity-cli/unity-cli.exe"
if [ -x "$EXE" ]; then
  exec "$EXE" "$@"
else
  exec bash "$(dirname "$0")/ucli-http.sh" "$@"
fi
