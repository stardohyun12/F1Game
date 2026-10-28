#!/usr/bin/env bash
# 규칙 게이트. 사용: bash tools/art/bin/check.sh <건물> → 마지막 줄을 보고서에 붙인다.
cd "$(dirname "$0")/../../.." || exit 1
python -X utf8 tools/art/check_rules.py --only "$1" --gate --md "docs/art/rule-audit/last_$1.md" 2>&1 | tail -3
