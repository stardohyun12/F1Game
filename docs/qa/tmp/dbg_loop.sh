#!/bin/bash
# 9곳 이동 → 상태 JSON + 캡처
cd /d/Github/F1Game
n=(room front boss cartside tea sunset harbor sink main)
for i in 0 1 2 3 4 5 6 7 8; do
  printf 'UnityEditor.SessionState.SetInt("dbg.i",%d); UnityEditor.SessionState.SetString("dbg.name","%02d_%s"); return "ok";' $i $((i+1)) ${n[$i]} > docs/qa/tmp/dbg_set.cs
  bash scripts/ucli-http.sh exec @docs/qa/tmp/dbg_set.cs >/dev/null 2>&1
  echo "GO $i: $(bash scripts/ucli-http.sh exec @docs/qa/tmp/dbg_go.cs 2>&1 | tail -1)"
  sleep 1.5
  bash scripts/ucli-http.sh exec @docs/qa/tmp/dbg_state.cs 2>&1 | tail -1
  sleep 1
done
