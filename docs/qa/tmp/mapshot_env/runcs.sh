#!/usr/bin/env bash
# 사용: runcs.sh <cs파일> [NAME]  — NAME 이 있으면 __NAME__ 치환. 사본 에디터로 exec.
export UCLI_PROJ=D:/Github/F1Game_shot/F1
cd D:/Github/F1Game
src=$(cat "$1")
[ -n "${2:-}" ] && src="${src//__NAME__/$2}"
timeout 120 bash scripts/ucli-http.sh exec "$src"
