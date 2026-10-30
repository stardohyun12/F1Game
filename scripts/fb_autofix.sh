#!/usr/bin/env bash
# 하네스(10/30, 도현: "해결되었다고 하는거 자동으로 계속 안 옮겨 놓는 문제 해결좀")
# 과제가 끝나면 과제 글에 적힌 도현 코멘트 id 를 찾아, 아직 open 인 것마다 fb.py fixed(같은 카메라 재촬영 + 판정자 둘)를 자동으로 돌린다.
# 팀원이 마지막 단계 전에 끊겨도, PD 가 잊어도 기획 창에 "고쳤음"이 올라가고, 판정이 거부하면 그 이유가 대화에 남는다.
# 사용: bash scripts/fb_autofix.sh "<과제 글>" "<한 줄 요약>"     (team.sh 가 자동 호출)
set -u
cd "$(dirname "$0")/.." || exit 1
TASK="${1:-}"; SUMMARY="${2:-팀원 과제 결과}"
IDS=$(printf '%s' "$TASK" | grep -oE '[0-9]{6}-[0-9a-f]{4}' | sort -u)
[ -z "$IDS" ] && exit 0
for short in $IDS; do
  full=$(python -X utf8 - "$short" <<'EOF'
import json,glob,sys
for f in glob.glob('tools/viewer/feedback/*.jsonl'):
    for l in open(f,encoding='utf-8'):
        if l.strip():
            d=json.loads(l)
            if d.get('id','').endswith(sys.argv[1]) and (d.get('status') or 'open')=='open': print(d['id']); raise SystemExit
EOF
)
  [ -z "$full" ] && { echo "autofix $short: open 아님 — 건너뜀"; continue; }
  case "$full" in *Studio*) export UCLI_PROJ=D:/Github/F1Game/F1Studio;; *) unset UCLI_PROJ;; esac
  echo "autofix $full"
  timeout 600 python -X utf8 scripts/fb.py fixed "$full" "[자동] $SUMMARY" 2>&1 | grep -e "판정:" -e "fixed →" -e "거부" -e "fb_geom" -e "실패" | cut -c1-220
done
