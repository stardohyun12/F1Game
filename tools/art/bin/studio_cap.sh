#!/usr/bin/env bash
# 건물 스튜디오 캡처 한 번: Play 켬 → studio_cap.cs(4캠, STUDIO_Slot/<에셋> 만 켬) → sNN_sheet.png → Play 끔.
# 사용: bash tools/art/bin/studio_cap.sh <에셋> <단계 번호> <짧은 설명>
#   예) bash tools/art/bin/studio_cap.sh depot 0 import
# sheet: 직전 단계(sNN-1_*)가 있으면 위 줄 직전 4컷 / 아래 줄 이번 4컷, 없으면 이번 4컷 한 줄. 열 = road · front · 34 · close.
# 전제: F1Studio 에디터(scripts/open-studio.ps1)가 떠 있고 Studio_Building 씬에 STUDIO/STUDIO_Slot/<에셋> 이 있다(다른 씬이면 자동으로 연다).
# 에디터 = 건물 스튜디오 프로젝트 F1Studio 만(UCLI_PROJ). 메인 F1 에디터엔 아무것도 보내지 않는다.
set -eu
A=$1; N=$2; D=$3
export UCLI_PROJ="${UCLI_PROJ:-D:/Github/F1Game/F1Studio}"
U="bash scripts/ucli-http.sh"
$U exec @docs/qa/tmp/studio_open.cs
$U exec "UnityEditor.SessionState.SetString(\"studio.cap\", \"$N|$D|$A\"); return \"arg ok\";"
$U editor play --wait
sleep 3   # 첫 프레임·A7 빛(Play 중 RP 교체) 반영 대기
$U exec @docs/qa/tmp/studio_cap.cs
$U editor stop
python -X utf8 docs/qa/tmp/studio_sheet.py "$A" "$N" "$D"
