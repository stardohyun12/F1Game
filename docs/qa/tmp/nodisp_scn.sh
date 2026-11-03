#!/bin/bash
# usage: nodisp_scn.sh <label>  — 현재 Play 상태에서 A/B 캡처 + 차이 + 판정
cd /d/Github/F1Game
L=$1
run() { sed "s/__MODE__/$1/g;s/__LABEL__/$L/g" docs/qa/tmp/nodisp_check.cs > docs/qa/tmp/_nodisp_run.cs; bash scripts/ucli-http.sh exec @docs/qa/tmp/_nodisp_run.cs | tail -1; }
run A; sleep 1; run B; sleep 1; run R
sed "s/__LABEL__/$L/g" docs/qa/tmp/nodisp_diff.cs > docs/qa/tmp/_nodisp_diff.cs; bash scripts/ucli-http.sh exec @docs/qa/tmp/_nodisp_diff.cs | tail -1
