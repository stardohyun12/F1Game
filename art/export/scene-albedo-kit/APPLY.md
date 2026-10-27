# D-185 albedo 인계 — 도현 승인 필요

35 PNG. **형태·UV·치수·바퀴 이름/위치를 변경하지 않았으므로 기존 FBX를 그대로 사용**한다. 파일 이름의 `baked32`는 교체 호환성을 위해 남긴 이름이며 이 폴더의 해당 PNG는 조명 없는 색 입력이다. 원본 export 폴더와 F1/Assets는 수정하지 않았다.

## Unity 연결

|대상 / 기존 재질 이름|새 PNG|연결 슬롯|
|---|---|---|
|SA6d_garage_baked32|garage_baked32.png|`_BaseMap`; 기존 garage_v5_combo.fbx/UV 유지|
|정비소 F2/F3 선택안, 동일 재질|garage_tin_F2_atlas.png 또는 garage_tin_F3_atlas.png|위와 같은 슬롯에 **셋 중 한 장**. 함석만 다른 albedo 아틀라스|
|dirt_v5_combo.fbx 마당 재질|dirt_baked32.png|기존 dirt 아틀라스 입력 `_BaseMap`. SA6d_yard_ground_soft는 별도 타일 입력일 수 있으므로 UV/현재 텍스처 확인 후 연결; 타일 맵 자리에 아틀라스를 넣지 않음|
|SA6d_drum_01~03, SA6d_tires_01~02, SA6d_transformer_01|동명 PNG|`_BaseMap`; `_HasR2=1`인 재질이면 활성 `_BaseMapR2`도 교체|
|SA6d_pot_01~02, SA6d_bench_01, SA6d_bicycle_01|동명 PNG|같음|
|SA6d_cone_01~02, SA6d_fence_01~02, SA6d_pole_01, SA6d_streetlight_01|동명 PNG|같음|
|SA6d_autumn_tree_01/02/03/05_soft|autumn_tree_01/02/03/05.png|현재 `_R2=1`, `_HasR2=1`이므로 **`_BaseMapR2`**. `_BaseMap`도 같은 albedo로 맞춰 토글 시 구운 판 복귀 방지|
|autumn_tree_04 신규/기존 배치 재질|autumn_tree_04.png|현재 SceneA6d 전용 재질 없음. 사용하는 활성 BaseMap/R2에 연결|
|SA6d_kart_v6e|kart_v6e.png|`_BaseMap`. 기존 kart_v6e.fbx와 k_tire*/k_hub* 그대로|
|사진 키트 vending_G1/G2, postbox_G1, ac_G3의 동명 unlit 재질|동명 PNG|D-185 실시간 조명 재질로 바꾸고 `_BaseMap`에 연결. 기존 FBX 사용|
|새 함석/돌담 재질|tin_F2/F3.png, stone_H1/H2/H3.png|D-185 재질 `_BaseMap`; 기존 타일 UV·물리 축척 유지|

SceneA6d 재질 이름·현재 슬롯은 읽기만 했다. 새 D-185 셰이더 이름은 이 작업에서 생성/확정하지 않았다. 프로그래밍팀이 해당 셰이더에 연결한다. URP Lit로 임시 확인한다면 Base Map=`_BaseMap`, Tint 흰색, Metallic=0을 기준으로 한다. 새 텍스처를 Emission에 넣거나 기존 Unlit 그대로 두면 실시간 빛이 반영되지 않는다.

## 임포트 설정

- sRGB 켬, Point, 기존 해상도/UV Scale·Offset 유지. 우선 무압축 검증. 돌담 H1/H2의 NPOT 크기도 유지.
- `_Sat=1`부터 확인. 기존 전기함 2.6·타이어 1.3 증폭을 새 albedo에 겹치지 않는다. 조명은 게임의 해/주변광/계단 셰이더가 담당. 기존 고정 AO/청록 그늘/화이트밸런스 보정의 중복은 프로그래밍팀 확인.
- 나무 알파는 원본과 바이트 동일. 기존 Cutoff/양면 설정 유지. 빛 없는 비교 시트의 색이 실제 게임의 조명 결과는 아니다.
- 타일 반복 물리 크기: F2 2×2m, F3 1×2m, H1 2×1.5m, H2 2×1.3125m, H3 2×2m. 0.9m 돌담 V 반복은 각각 0.6/0.685714/0.45.
- PNG별 `.gpl`에 실제 사용 색 기록. 기존 조명용 팔레트로 회색을 청록에 매핑하지 않도록 원래 재료색을 포함한 개별 팔레트 사용, 디더 없음.

## 제거/보존 판단

|부분|처리|
|---|---|
|정비소·dirt·일반 소품·사진 소품|기존 작업 파일의 Base Color만 같은 BakedUV로 복사. Blender EMIT는 **UV 색 전사 용도**이며 해·하늘빛·직접/간접광·AO pass 및 V5 노출/청록 lift를 사용하지 않음|
|PO1 전기함|면 방향별 밝기와 밝고 어두운 테두리 제거. 문짝·문 경계·경고표지·손잡이·환기구의 그린 디테일 유지|
|PO1 타이어|윗면 104/옆면 70/아랫면 40의 방향별 밝기를 70으로 통일. 옆면 트레드 잉크는 유지|
|나무 기둥|면 법선에 따른 3단 조명색 제거. 회갈색 기본색 111/80/59와 기존 세로 결 약 −4%만 유지|
|나무 잎|PO1/R2의 큰 잎 덩어리 색 경계·알파 유지. 원본 무조명/조명 자료의 면 평균 비율을 비교하되 보정은 ±4%로 제한. 아래 한계 참조|
|카트|v6e의 재료 평균 배색 유지. 결은 조명 없는 원재료 이미지에서 가져와 ±4% 명도만 적용. 번호판 07 픽셀 동일. 상자·끈에 곱했던 방향별 밝기 계수를 제거|
|사진 함석·돌담|원근/크롭/광량 평탄화된 **조명 전** 소스 사용. 새 태양·하늘빛·AO 없음. 기존 사진의 부품/줄눈/골은 재료 디테일로 유지|

### 나무 판단의 한계

현재 R2 잎은 이미 덩어리화·팔레트 변환된 이미지라 과거 구운 명암과 그려진 덩어리 명암을 픽셀별로 완전히 역분리할 수 없다. 원본 사진으로 복귀하면 승인된 덩어리 표현이 사라지고, 강한 역보정은 삼각형 얼룩을 만들었다. 따라서 **잎 내부의 모든 과거 조명 성분이 완전히 제거되었다고 검증하지 못했다.** 형태/색 덩어리를 우선 보존한 보수적 후보이며 도현 판단 필요. `art/source/albedo-kit/tree-decisions.json`에 5그루별 비교 기준·보정 범위 기록.

## 검증·다음 단계

`validation.json`: 35장 크기·알파·원본/산출 SHA256·기존 FBX 대응. `art/source/albedo-kit/transfer-validation.json`: 조명 없는 색 전사 및 작업 중 정점·면·행렬·UV 해시 동일. 카트 번호판 픽셀 동일, 새 FBX 0개.

`art/_review/2026-10-27-albedo-kit/sheet.png` / `index.html`에서 구운 판과 albedo를 동일 카메라로 비교. 나무 판단 및 전체 배색은 **도현 승인 필요**. 승인 후 **QA/프로그래밍 임포트 필요** — Scene_A6d_Town 실시간 빛·그림자 토글과 게임 카메라 B 720 컷 확인. Unity·Play·씬 수정·commit/push는 실행하지 않았다.

재현 순서: `art/source/albedo-kit/`의 bake_color.py + kart_source.py + photo_garage.py → finish.py → merge_photo.py → trees.py → photo_padding.py → render.py → package.py. Blender 공식 [Bake 문서](https://docs.blender.org/manual/en/4.4/render/cycles/baking.html)의 색/조명 분리 방식과 기존 생성기의 UV 전사 절차를 재사용했다.
