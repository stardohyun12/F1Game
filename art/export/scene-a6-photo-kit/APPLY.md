# 사진 기반 생활 소품·함석·돌담

**도현 승인 필요. 승인 전 F1/Assets에 넣지 않음. QA/프로그래밍 임포트 필요.**
검토: `art/_review/2026-10-27-photo-kit/index.html` / `sheet.png`.

## 소품

동명 FBX와 PNG를 함께 사용. 단위 m, 피벗 바닥 중앙, Blender 정면 −Y. 한 메시·한 재질·UV 1세트. 배치 회전은 씬에서 확인.

|파일 이름|폭×깊이×높이(m)|tris|PNG|용도|
|---|---|---:|---|---|
|vending_G1|1.181×0.731×1.830|92|256×256|정비소/가게 앞 흰 자판기|
|vending_G2|1.181×0.731×1.830|92|256×256|가게 앞 주황 자판기|
|postbox_G1|0.810×0.560×1.380|56|256×128|길가 우체통; 본체 0.83m+지주 0.55m|
|ac_G3|0.800×0.285×0.550|92|256×128|창고/건물 옆 지상 실외기|

자판기는 [Fuji 표준 모델 규격](https://www.th.fujielectric.com/en/products/Fuji%20Electric%20Vending%20Machine%20-%20Hot%20%26%20Cold%20Can.pdf), 우체통은 [일본 우정박물관 13호 규격](https://www.postalmuseum.jp/column/transition/post_10.html), 실외기는 [Mitsubishi 규격](https://www.mitsubishielectric.com.au/product/msz-ef-wall-mounted-indoor-unit/)을 크기 기준으로 삼았다. 사진 속 자판기·GREE 실외기의 정확한 모델 실측은 아니며 표준 동급 규격 적용이다.

## 타일 텍스처

Scene_A6d_Town 길 벽·집 앞 돌담 높이 0.9m에 적용. 돌담 전체 높이에 이미지를 억지로 맞추지 말고 아래 물리 크기로 UV를 계산한다. 면의 가로 길이 L이면 U=L/가로m, V=높이/세로m. 상단은 평면 투영(X/Y), 측면은 길이/높이 투영으로 분리해 늘어짐을 피한다.

|PNG|픽셀|타일 1회 물리 크기|높이 0.9m의 V 반복|
|---|---|---|---:|
|stone_H1.png|64×48|2×1.5m|0.6|
|stone_H2.png|64×42|2×1.3125m|0.685714|
|stone_H3.png|64×64|2×2m|0.45|
|tin_F2.png|64×64|2×2m|해당 면 높이/2|
|tin_F3.png|32×64|1×2m|해당 면 높이/2|

타일은 Wrap Repeat, sRGB, Point, 우선 무압축·원본 크기 유지. NPOT인 H1/H2를 POT로 리사이즈하지 않는다. 멀리서 깜박임/밉맵 정책은 실제 720 게임 컷에서 QA 확인. 돌담 좌우·상하 끝 RGB 완전 동일; 무작위 얼룩을 추가하지 않았다. 같은 무늬 반복은 남으므로 H1~H3를 벽 구간별로 나누어 배치.

## 정비소에 바로 바꿔 끼우기

`garage_tin_F2_atlas.png` 또는 `garage_tin_F3_atlas.png`는 기존 `scene-a-v5b-garage/garage_baked32.png`와 같은 4096×1024 크기다. **기존 garage_v5_combo.fbx와 UV를 그대로 두고 BaseMap만 교체**한다. 둘 중 한 장 사용. 외벽 함석·박공·지붕 외부만 바뀐다. 실내/비함석 영역 픽셀은 원본과 동일하다. 타일 PNG를 기존 전체 아틀라스 자리에 직접 넣으면 안 된다.

창고의 다른 모델에는 위 tin 타일을 32 texel/m로 투영. 독립 타일은 수직면에서 구운 빛이므로 지붕 방향에 맞춘 최종 베이크가 필요하다. 정비소 전용 아틀라스는 실제 지붕 방향에서 이미 구웠다.

## 재질·빛·재현

- Unlit, Tint 흰색, 추가 채도 증폭/조명 중복 적용 없음. UV 섬 여백 3px 이상. 실외기는 회색 7단, 돌은 따뜻한 회색 6단, 붉은 함석은 붉은 계열로 제한.
- `palette-photo-kit.gpl`: 정비소 32색을 보존하고 생활 소품 및 회색·붉은 명암 칸 추가. 69색 공용 팔레트, 개별 이미지 실제 색 수는 texture-validation.json.
- 원근 보정 → 날짜 제외 크롭 → 저주파 빛 압축 → 면적 축소 약 32 texel/m → 팔레트 → Cycles 128 diffuse+AO → V5 +1stop/그늘 보정 → 재료별 팔레트, 디더 없음.
- 원본 V5와 같은 태양 energy 2.4 / RGB(1,.83,.60) / angle .10 / 방향(.7,.65,-.45), sky RGB(.30,.52,.65) strength .65, max bounces 4, AO .70+.30×AO. 내부 조명은 기존 정비소 픽셀 그대로 보존.
- `art/source/photo-kit/`: prepare.py → refine.py(함석 세로 골·실외기 회색 보정) → build_bake.py / garage_bake.py → finish_textures.py → clean_tiles.py / clean_garage.py → export_render.py → package.py → audit.py. 정비소만 재촬영할 때 render_garage.py. 좌표/행렬 manifest.json, 원본 보정 크롭 crops/, Blender 중간 파일 보관. refine 단계의 사진색은 베이크 후 최종 공용 팔레트로 양자화한다.
- 사진 출처·SHA256: 동봉 provenance.csv 및 art/provenance.csv. 기존 photo-picks/SOURCES.md의 CC0 검증 기록을 재사용. 생성형 이미지·Unity 실행·씬 변경·commit/push 없음.

## 확인과 남은 단계

FBX 4개 Blender 재임포트: 치수 오차 0.00001m 미만, UV 1세트. 텍스처 5장 양 끝 RGB 차이 0, 정비소 두 교체본 함석 밖 픽셀 동일. geometry-validation.json / texture-validation.json 참조.
Blender 참고 시트에서 소품/타일 전후 카메라는 동일. Unity 씬 적용 및 게임 카메라 B의 720 전후·근접 비교는 실행하지 않았다. 도현 미리보기 승인 후 QA/프로그래밍이 임포트하고 실제 장면에서 최종 확인한다.
