#!/usr/bin/env bash
# 사용: bash docs/qa/tmp/sunset3/run.sh <file.cs>  — 파일 내용을 에디터 exec 로
bash scripts/ucli-http.sh exec "$(cat "$1")"
