# PD1720 전달 정보

공용 팔레트·밀도·실물 외곽은 유지했다. 파일은 `art/models/`에만 있다. 검증 결과는 `validation/`과 독립 게이트를 분리해 본다.

| 종 | 최종 모델 경로 | tris | 외곽 W×D×H mm |
|---|---|---:|---|
| 상자 | art/models/cargo_box_final_v2/cargo_box_final_v2.glb | 144 | 400×300×300 |
| 케이크 | art/models/cargo_cake_v02/cargo_cake_v02.glb | 564 | 254×254×127 |
| 계란 | art/models/cargo_eggs_v02/cargo_eggs_v02.glb | 1,848 | 280×280×69 |
| 와인 유지 | art/models/cargo_wine_v01/cargo_wine_v01.glb | 188 | 75.2×75.2×300 |
| 생선 | art/models/cargo_fish_v02/cargo_fish_v02.glb | 494 | 400×300×95 |

각 모델 폴더에 BLEND·64² PNG·재생성 스크립트·SPEC·출처가 있다. FBX 미생성. GLB는 미터·Y-up, 피벗 바닥 중심, Point sampler. 게임에는 미투입이다.

재현: 저장소 루트에서 `py -3.13 art/_review/2026-10-23-cargo-set-v2/check_set.py`; 공통 렌더는 Blender `-b -P art/_review/2026-10-23-cargo-set-v2/render_set.py -- --kind all`; 시트는 `py -3.13 art/_review/2026-10-23-cargo-set-v2/build_review.py`.

계란 69mm 높이는 기존 제조사 판+뚜껑의 합산 가정이다. 창 구조는 치수에 맞춘 새 포장 설계로 같은 제조사 SKU의 복제가 아니다. 케이크는 알파 필름 대신 빈 창·가는 반짝선으로 맑은 투명창을 표현하며 투명 재질을 실제 구현한 것은 아니다. 생선 비닐·얼음 역시 불투명 밝은 면 표현이며 배송 덮개가 없다. 생선 뒷벽만75mm로 낮추고 앞·옆95mm를 유지했으므로 외곽 치수만 제조사값이며 완전한 제품 복제는 아니다. 미술 단순화의 한계는 도현 확인 대상이다.

카메라와 조명: 주변광(0.64,0.70,0.69), 같은 색 옅은 합성 안개, 따뜻한 한 방향광. 공통 모델 장면 각도는 고도26.565도, 960×540. 이전 비교의 약21.5도보다 내용물 상단이 더 드러나므로 순수 모델 변경만의 A/B는 아니다. Cycles128samples(이전16), 디노이즈 미사용. 원화 합성은 Unity 실행·적재·접지 검증을 대신하지 않는다.

도현 미리보기 승인 필요. QA/프로그래밍 임포트 필요. Unity·F1/Assets·commit/push 실행하지 않았다.
