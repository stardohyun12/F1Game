# STYLE 적용 메모 — 승인 대기 / 아트 1

**도현 승인 필요. QA/프로그래밍 임포트 필요. Unity 실행·씬 저장·F1/Assets 투입 안 함.**

## 소품 14종

동명 PNG만 교체: bench_01, bicycle_01, cone_01/02, drum_01/02/03, fence_01/02, pole_01, pot_01/02, tires_01/02.
원형은 `art/export/scene-a2-kit/*.fbx` 그대로. UV0·형태·크기·원본 알파 보존. 새 FBX 없음. 각 RGB는 공용 `art/palette/style32.gpl`의 4색; 작업 시점 사본 `style32-snapshot.gpl`. 3단 명암 + 바닥 9cm 이내 접지 한 단. 블록은 월드 1/11m 셀로 칠함. 기존 아틀라스 크기는 UV 호환을 위해 유지하므로 PNG 해상도 자체를 11/m로 해석하지 말 것. 아주 작은 삼각형·알파 철망·자전거 살은 원본에 남아 있는 예외다.

Import: sRGB On, Point, 비압축, NPOT None, Max Size 원본 이상(전신주 4096), alpha clip 0.5, 양면. 아틀라스 Clamp. 승인 비교는 mipmap Off; 움직임 지글거림과 원경 밉맵은 QA 주행 비교 후 결정. 알파 철망은 원래 고주파 무늬를 보존했으므로 STYLE 규칙4까지 해결된 것으로 보지 않는다.

## 자연 3장 / UV 변경 불필요

| PNG | 재질 슬롯 | 기존 UV/m | 32px 이미지에서 tiling(X,Y) | 목표 |
|---|---|---:|---|---|
| bush_style.png | SA3_bush / _BaseMap | 0.5 | (0.6875,0.6875) | 11 texel/m |
| mound_style.png | SA3_mound, SA3_clump / _BaseMap | 1/32 | (11,11) | 11 texel/m |
| rock_style.png | SA3_rock / _BaseMap | 0.5 | (0.6875,0.6875) | 11 texel/m |

32×32, 각 3색, Repeat, Point, sRGB, 비압축, offset=(0,0), 반복 길이32/11=2.909m. 경사면은 원래 삼각형별 투영 UV의 왜곡이 남음. `docs/qa/tmp/scene_a3_build.cs` Tri/SetUVs를 읽어 계산했으며 실제 씬 임포트로 측정하지 않았다. `bush/mound/rock_proxy_*`는 같은 대용 메시의 비교이며 Unity 메시가 아니다.

## 중복 음영과 외곽선 방지 — 프로그래밍팀

- 실제 Renderer가 참조하는 **씬 전용 사본**에서 `_BaseMap` 교체. A4는 `SA4_*` 복사 재질을 쓰므로 SA3 원본만 바꾸지 말 것.
- `_HasR2=0`, `_R1Ok=0`, `_LightAmt=0`, `_R3Ok=0`; `_Ramp/_Outline=0`만으로는 SceneA3Toggles.Apply가 다시 켤 수 있음.
- `_GradAmt=0`, `_FootDark=0`, `_Blotch=0`, `_Tint=white`(clump 추가 녹색 제거), `_Sat=1`, `_WB=white`, `_Lift=0`.
- 소품은 PNG에 이미 접지가 있으므로 추가 부드러운 AO 금지.
- 자연 반복 텍스처에 접지 띠를 넣으면 측면 전체에 반복되므로 넣지 않았다. **자연의 접지 한 단은 프로그래밍 작업 필요**: 각 물체 지면 기준 높이 h<0.09m일 때 공용 팔레트 `#4C4F44`로 한 번만 바꾸는 step. 현 셰이더 `_FootDark`는 smoothstep 곱이라 대체 불가. 합쳐진 지형 메시라면 각 덩어리 지면 높이를 따로 공급해야 한다. 그림자가 이미 접지를 담당하는 배치에서는 이중 적용하지 않는다.
- 안개·Grade가 최종 색을 늘리는 것은 PNG 색 수와 별도. 최종 카메라 B와 주행 영상에서 다시 판정.

## 확인 근거 / 한계

`metrics.json`, `validation.json`: RGB 팔레트·검정·원본 해시·알파·크기·고립 픽셀 수치. 1px 고립은 PNG 내 불투명 4이웃 기준이며 화면상의 물리적 텍셀이나 알파 가는 선의 지글거림 검증을 대신하지 않는다.
Blender 5.2.2에서 원본 FBX를 먼저 재현하고 동일 emission·카메라·64px/m 조건에서 PNG만 교체했다. 조명·안개·노출은 Unity와 다르므로 게임 내 완성 판정 아님.
나무는 원본 높이13.5m·원본 PNG를 그대로 렌더했다. b_game의 가을 나무는 게임 셰이더/색 보정 뒤의 모습이므로 Blender 원본은 더 복잡하고 어둡게 보인다. 나무를 임의 재착색해 승인된 모습이라고 표시하지 않았다. 원본 조건 재현은 Blender 원형/UV/PNG 수준이고 Unity 컷 재현은 미실행이다.
기존 조사 `docs/research/2026-10-26-ghibli-town-grammar.md`의 큰 얼룩·색 있는 그늘 원리와 STYLE의 3단 제한을 적용했다. 별도 외부 다운로드·AI 이미지 도구 없음. CC0/CC-BY 원형 사용권과 표기는 기존 `CREDITS.md` 및 kit.json에서 확인; 이번 작업에서 원작자 웹페이지를 재검증하지 않았다.

재생성: Blender `-b --python art/source/kit-style/extract.py` → `py -3.13 art/source/kit-style/paint.py` → Blender `-b --python art/source/kit-style/render.py` → `py -3.13 art/source/kit-style/package.py`.
