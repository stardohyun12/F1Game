#!/bin/bash
# 사용: bash docs/qa/tmp/a7_run.sh <cs 파일>
cd "$(dirname "$0")/../../.." && bash scripts/ucli-http.sh exec "$(cat "$1")"
