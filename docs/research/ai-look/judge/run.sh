#!/bin/bash
# 익명 판정: 같은 질문을 파일마다 새 세션으로 (반응·출처 숨김)
cd "$(dirname "$0")"
for f in img_k7 img_q2 img_m4; do
  for r in 1 2; do
    claude -p --model sonnet --allowedTools Read --setting-sources user \
"현재 폴더의 ${f}.png 파일 하나만 Read 로 열어 보라(다른 파일은 열지 말 것). 게임 화면의 일부다. 이 이미지 속 건물(집)이 '생성형 AI 이미지나 자동(절차적) 생성물처럼 보이는 이유' 상위 3개를 구체적 시각 근거(형태·창 배치·표면·디테일·색·간판)로 한국어 한 줄씩 적어라. 사람이 손으로 만든 것처럼 보이는 점이 있으면 1개 덧붙여라. 마지막 줄: 'AI·자동생성 같음 점수: N/10'. 출처 추측·검색 금지." > out_${f}_${r}.txt 2>&1
  done
done
echo done
