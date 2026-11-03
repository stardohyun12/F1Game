#!/usr/bin/env bash
# artlive 1950: fbx 하나를 Library 에 넣고 → 임포트 → Play → 보관소 캡처(태그) → Stop.  사용: cap_cycle_1950.sh <fbx> <tag>
set -e
cd /d/Github/F1Game
cp "$1" F1/Assets/Art/Library/Buildings/depot/q3_depot.fbx
U="bash scripts/ucli-http.sh"
$U exec @docs/qa/tmp/library_import.cs | tail -1
echo "UnityEditor.SessionState.SetString(\"fb.cap\",\"$2\"); UnityEditor.SessionState.SetString(\"fb.only\",\"depot\"); return \"tag $2\";" > docs/qa/tmp/_tag_1950.cs
$U exec @docs/qa/tmp/_tag_1950.cs
$U editor play --wait
$U exec @docs/qa/tmp/fb_cap_1857.cs || true
$U editor stop
