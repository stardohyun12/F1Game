# D-124 화물 전달 목록

| 종류 | GLB (저장소 루트 기준) | 실물 외곽 W×D×H / m | tris |
|---|---|---|---:|
| 상자 | art/models/cargo_box_final/cargo_box_final.glb | .400×.300×.300 | 144 |
| 케이크 | art/models/cargo_cake_v01/cargo_cake_v01.glb | .254×.254×.127 | 290 |
| 계란판 | art/models/cargo_eggs_v01/cargo_eggs_v01.glb | .280×.280×.069 | 1,032 |
| 와인 | art/models/cargo_wine_v01/cargo_wine_v01.glb | .0752×.0752×.300 | 188 |
| 생선 포장 | art/models/cargo_fish_v01/cargo_fish_v01.glb | .400×.300×.095 | 410 |

각 폴더에 `.blend`, 64² PNG, `atlas_manifest.json`, 제작 스크립트·SPEC·출처가 있다. GLB는 Y-up 미터, Blender는 Z-up 미터. 모든 피벗은 바닥 중심. FBX는 생성하지 않았다.

공용 32색 팔레트는 `art/source/palette/pitcrew_d123_32.*`, 밀도 64px/m, 화면 기준 960×540. GLB에 동일 PNG 내장, Point sampler 확인. Unity 임포트 시 Point/압축/알파와 미터 스케일은 프로그래밍팀이 다시 확인해야 한다. 케이크 창은 alpha 0.07 투명 필름; 와인 ContactAO 속성의 엔진 반영은 미검증이다.

계란 포장의 69mm 높이는 제조사 판 44mm와 뚜껑 25mm의 단순 합으로 조립 겹침을 가정하지 않았다. 케이크 내부 치수는 외포장에 맞춘 모델링 가정이다. 생선은 열린 포장 검사 상태로 배송 덮개가 없다.

`compare.png`는 전체 보드, `lineup-scene-960x540.png`는 공통 물리 비례 카탈로그 합성이다. 종별 `scene-960x540.png`는 같은 도로 위치의 작은 물체 비교, `six-faces.png`는 실제 GLB 6방향이다. 원화 합성은 게임 화면 검증이 아니다.

`validation/*.json` 수치 통과와 `visual-gate.md`의 시각 점수를 분리해 판단한다. 도현 미리보기 승인 필요. **QA/프로그래밍 임포트 필요.** F1/Assets 투입·Unity 실행·git commit/push는 하지 않았다.
