# 생선 택배 v02 — PD1720 2판

| 항목 | 값·근거 |
|---|---|
| 외측 | 400×300×95mm. BEWI 042A 제조사 공개값 재사용, 직접 실측 아님 |
| 상태 | 열린 포장 확인. 배송용 뚜껑 없음 |
| 내용물 | 324mm 생선 1마리, 높이10~14mm 얼음5개, 비닐 가장자리 접힘2곳. 어종·내부 배치는 제작 추정 |
| 예산 | GLB 실측 494 tris / 2,000 |
| 텍스처 | 64×64 PNG, 공용32색 중9색, 팔레트 밖0, Point |
| 밀도 | 실제 GLB 삼각형 두 주축 60.247~64.003px/m. 목표64 ±10% 통과 |
| 단위·피벗 | m, glTF Y-up, 바닥 중심 |
| 식별점 | 어두운 등과 은빛 배, 깊은 V 꼬리, 흰 눈둘레, 낮은 얼음5개. 꼬리 주변은 비움 |
| 표기 | 앞 송장 픽셀선, 옆 삼각머리 ↑↑. 글자 없음 |

외벽 6방향 UV는 각각 분리했다. 인쇄가 내벽에 반복되지 않도록 내벽은 무인쇄 바탕 영역(30,54)으로 옮겼다. 동일 크기인 벽 안팎 삼각형 중 검사기가 내벽을 대표로 고르지 않도록 안쪽 벽·안쪽 베벨을 중심 팬으로 분할했다(**+48 tris**). 실제 밀도·검사 허용치는 바꾸지 않았다. 이 변경은 외벽 6면을 검사 대표로 유지하는 명시적인 구조 변경이다. `geometry.json`에 면 법선·UV 원점과 이유 기록. 검사기의 6방향 대표 검증은 모든 UV 겹침을 감사하는 검사는 아니다.

2판에서는 수조처럼 먼저 보였던 뒤벽을 95→75mm로 낮췄다. 앞·옆벽은95mm이며 외곽400×300×95mm는 유지했다. **제조사 외곽값을 바탕으로 식별을 위해 고친 개방 검사 형상이며, BEWI 실제 제품의 벽 높이를 그대로 재현한 모델은 아니다.** 독립 1판 게이트6점의 결함(회청 내용물끼리 합쳐짐, 높은 밝은 뒤틀, 얼음/인쇄 경쟁)을 근거로 바꿨다. 공통2판 게이트는 별도.

비닐은 반투명 셰이더 대신 작은 접힌 가장자리의 밝은 면으로 표현했다. 불필요한 알파 노이즈를 피하지만 실제 비닐의 투명도/반사는 구현하지 않았다. 얼음은 불투명 픽셀 명암으로 표현한다. 위·내부의 그늘은 공통 조명과 contact vertex AO. Unity 머티리얼·AO 대응은 QA/프로그래밍 임포트 필요.

재생성: `py -3.13 art/models/cargo_fish_v02/build_atlas.py` → `D:/Tools/Blender/blender.exe -b -t 4 -P art/models/cargo_fish_v02/generate.py`.

검사: `py -3.13 tools/asset-gen/check_asset.py --model art/models/cargo_fish_v02/cargo_fish_v02.glb --texture art/models/cargo_fish_v02/cargo_fish_v02_atlas_64.png --palette art/source/palette/pitcrew_d123_32.gpl --out art/models/cargo_fish_v02/validation-premerge.json --density 64 --max-tris 2000 --dimensions .4 .095 .3 --shape cargo --surface-manifest art/models/cargo_fish_v02/atlas_manifest.json`.

제작자 검사에서는 출처 장부 병합 대기 항목만 false. `provenance-row.json`을 root가 병합한 후 통합 검사를 다시 실행한다. 독립 시각 게이트 점수는 공통 리뷰 보고서에 기록. Unity·F1/Assets 투입·commit/push 실행하지 않음.
