#!/usr/bin/env bash
# 건물 스튜디오 캡처 한 번: Play 켬 → studio_cap.cs(4캠, STUDIO_Slot/<에셋> 만 켬) → sNN_sheet.png → Play 끔.
# 사용: bash docs/qa/tmp/studio_cap.sh <에셋> <단계 번호> <짧은 설명>
#   예) bash docs/qa/tmp/studio_cap.sh depot 0 import
# sheet: 직전 단계(sNN-1_*)가 있으면 위 줄 직전 4컷 / 아래 줄 이번 4컷, 없으면 이번 4컷 한 줄. 열 = road · front · 34 · close.
# 전제: Studio_Building 씬이 열려 있고 STUDIO/STUDIO_Slot 아래에 <에셋> 이름의 오브젝트가 있다.
set -eu
A=$1; N=$2; D=$3
U="bash scripts/ucli-http.sh"
$U exec "UnityEditor.SessionState.SetString(\"studio.cap\", \"$N|$D|$A\"); return \"arg ok\";"
$U editor play --wait
sleep 3   # 첫 프레임·A7 빛(Play 중 RP 교체) 반영 대기
$U exec @docs/qa/tmp/studio_cap.cs
$U editor stop
python -X utf8 docs/qa/tmp/studio_sheet.py "$A" "$N" "$D"
