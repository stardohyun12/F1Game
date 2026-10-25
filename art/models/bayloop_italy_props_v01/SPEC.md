# D-137 생활 소품 키트

세계 표면은 32px/m, 공용 `pitcrew_d123_32`, Point. 건물 형태를 바꾸지 않고 일본 골목의 생활 밀도를 더하는 독립 배치물이다. 프리팹/콜라이더/실게임 배치는 프로그래밍 담당. Unity 실행 없음.

## 재현
1. `py -3.13 tools/asset-gen/bayloop_italy_props_texture.py`
   이어서 `py -3.13 tools/asset-gen/bayloop_italy_props_r2_texture.py`로 실패분 전용 아틀라스를 만든다.
   마지막으로 `py -3.13 tools/asset-gen/bayloop_italy_props_r3_texture.py`를 실행한다.
2. `D:/Tools/Blender/blender.exe -b -t 6 --python tools/asset-gen/bayloop_italy_props.py`
3. `py -3.13 tools/asset-gen/check_asset.py --building-manifest art/models/bayloop_italy_props_v01/manifest.json --out art/models/bayloop_italy_props_v01/checks.json --provenance art/models/bayloop_italy_props_v01/provenance-staging.csv`

바탕 사진은 기존 A1 CC0 사진 아틀라스, 잎은 Poly Haven leafy_grass 실제 사진(2m)을 64px로 축소하여 재사용했다. 사진 명암을 공용 팔레트의 가까운 값으로 치환했다. 재질의 무작위 노이즈를 만들지 않았다. 잎 사진은 식생의 낮은 해상도 색 분포용이며 레몬 잎의 정밀 식물학적 재현은 아니다. 형태는 직접 작성한 bpy 메시다.

## 피벗과 배치
GLB/FBX: X 오른쪽, Y 위, +Z 앞, 1unit=1m. 모델의 실제 bounds로 원점을 정규화했다.

|소품|피벗|배치 의미|
|vending_machine|floor_back_center|기기 뒤쪽 끝을 벽에 붙임; 뒤면 높이 Y0|
|small_signs_a/b/c/d|floor_back_center|판 뒤끝·밑면 가운데; 실제 벽 높이로 올려 배치|
|planter_row|floor_center|화분 줄 바닥, 전체 bounds X/Z 중심|
|ac_unit|floor_back_center|실외기 뒤쪽 끝 바닥; 벽과 이격은 배치자가 추가|
|meter_box|floor_back_center|계량기 뒤끝·밑면 가운데; 벽 높이로 올림|
|mailbox|floor_back_center|우편함 뒤끝·밑면 가운데; 벽 높이로 올림|
|bicycle|floor_center|타이어 바닥·전체 bounds 중심; 정적 주차 모델|
|lemon_tree_pot|floor_center|화분 바닥·전체 수관 bounds 중심|

정확한 전체 치수·tris는 `manifest.json`이 기준. 자판기는 Fuji 실제 본체 W1181×D731×H1830mm를 시작점으로 앞 상품 진열/받침 깊이를 추가했다. 실외기는 Daikin W765×D285×H550mm 본체에 받침/배관을 추가했다. 자전거는 약 1.77m 길이 생활형이며 특정 상표를 복제하지 않았다. 화분·간판·우편함 등은 생활 규격 범위의 설계치이며 직접 실측했다고 주장하지 않는다.

## 표면/문자
`PropsPhotoAtlas.png` 1024², 각 256칸 안쪽 252px + 2px gutter. 기존 metric UV face()를 재사용하여 모든 면에 32px/m를 유지했다. 바퀴·둥근 금속도 작은 평면 패치로 UV를 구분했다. 라벨은 Pretendard Regular OFL의 `수리 / 세탁 / 식료품 / 차`만 사용한다. 가짜 문자·언어 혼합 없음. 녹은 화분 색과 따로 과장하지 않고 바랜 적색 금속·손때·접지부 명암으로 제한했다.

검토판은 실제 Cycles 960×540 렌더다. 배경은 중성색 평면이며 AI 배경이 아니다. 원본 albedo의 32색 검사와 최종 조명 화면의 색수는 별도다. FBX 수신자는 Point/sRGB/압축없음/Nearest mip 정책을 직접 확인해야 한다.

## 참고: 원 제작사 공개 치수
- Fuji Electric hot/cold can vending machine: https://www.th.fujielectric.com/en/products/Fuji%20Electric%20Vending%20Machine%20-%20Hot%20%26%20Cold%20Can.pdf
- Daikin 2MXS50G dimensional drawing: https://www.daikin.it/content/dam/MDM/TechnicalDrawings/Split/EN/2MXS50G_dim_3D058713_EN.pdf
- Poly Haven CC0 leaf material: https://polyhaven.com/a/leafy_grass

제작사 자료는 비례 참고만 했고 해당 도안/로고를 텍스처로 사용하지 않았다. CC0 원본 URL·다운로드 URL·SHA는 leaf-source.json 및 기존 source_manifests에 보존했다. 중앙 provenance 동시 쓰기를 피하려고 `provenance-pending.json`을 제출한다.

## 첫 독립 게이트 후 교정 1회
- 통과한 자판기·간판a/c·실외기·우편함은 변경하지 않았다. `accepted-r1-unchanged.json`은 GLB/FBX/PNG SHA가 모두 동일하다는 증거다.
- 실패 6개만 `PropsPhotoAtlas_r2.png`를 쓴다. 간판b/d는 3cm 판·글자 중앙·턱 삭제, 계량기는 실제 Pretendard 5px 숫자 `073`·점검문·굽어 벽으로 들어가는 전선관을 넣었다.
- 화분은 실제 빈 입구와 테두리 아래 흙이다. 식물은 넓은 6각 타원 잎, 레몬은 줄기로 연결된 6분할 과실이다. 과실은 공용 팔레트 23 중심이며 기존 CC0 회색 기와 사진의 낮은 해상도 명암을 22/23/31로 전용했다. 레몬 실사 스캔을 확보했다고 주장하지 않는다.
- 자전거는 핸들 연결·열린 바구니·체인링/체인 추가. 764 tris로 상한 800 안이다.
- 최신 GLB/FBX/geometry.json은 각 이름 파일이 기준이다. 원본 `.blend`의 조합은 `props.blend`(초기), `props_small_signs_b,small_signs_c.blend`(글자 폭 보정), `props_small_signs_b,small_signs_d,planter_row,meter_box,bicycle,lemon_tree_pot.blend`(교정1), `props_lemon_tree_pot.blend`(교정1의 최종 과실 연결) 순이다. 전체를 일괄 재현하면 현재 생성기의 최신 11개가 만들어진다.
- 최초 게이트 렌더는 검토폴더 `iterations/r1/`에 보존했다. 게이트 결과는 root가 별도 기록한다.

## 마지막 교정 2회 — R3
- 독립 R2에서 자전거 7점 통과. 자전거 포함 통과 6개 파일은 변경하지 않았다(`accepted-r2-unchanged.json`). 나머지 5개만 `PropsPhotoAtlas_r3.png`를 사용한다.
- 문자: 실제 [Galmuri 공식 README](https://github.com/quiple/galmuri)의 원격자 크기인 Galmuri11 **12px**, Galmuri7 **8px**를 사용했다. `11`/`7`이라는 이름을 그대로 raster size로 쓰지 않았다. 글자는 `세탁`, `차`, 숫자 `07`이다.
- 폰트 파일은 기존 `art/_review/2026-10-23-ui-final/fonts/Galmuri11.ttf`, `art/models/cargo_box_v01/fonts/Galmuri7.ttf`. 두 폴더의 Galmuri-OFL.txt/LICENSE.txt는 SIL OFL 1.1이다. 폰트 파일 SHA와 출처를 pending provenance에도 추가했다.
- UV 문자 영역은 실제 30×18px, 16×18px, 12×10px이며 각각 32px/m에 맞게 메시 면의 실측 크기를 맞췄다. `glyph-validation-r3.json`에 실제 font bbox/면 크기를 기록하고 16배 nearest 확대 원본을 `glyph_*.png`로 보존했다. 실제 Blender 렌더에서도 온전한 글자와 07을 직접 확인했다.
- 잎은 길이 0.11~0.164m, 폭 0.05~0.066m, 가운데 능선 1cm를 가진 6면 접힌 타원이다. 각 잎의 밑부분은 실제 줄기 선분에 연결되고 각도·방향을 바꿨다. 레몬은 74장, 화분 줄은 72장이다. 총 tris는 화분786/레몬788로 상한 안이다.
- 화분의 실제 사진 명암은 팔레트21/24 두 가까운 흙갈색으로 좁혔다. 과실은 기존 23/25 밝은 색으로 변경하고 짧은 자루로 가지에 붙였다. 숫자창 아래 계기는 사선 지침·눈금 4개를 추가했다.
- R2 렌더는 검토폴더 `iterations/r2/`에 보존. 최신 소스는 `props_small_signs_b,small_signs_d,planter_row,meter_box,lemon_tree_pot.blend`와 마지막 수관 보강 `props_lemon_tree_pot.blend`이다. 최종 gate는 작성자가 결정하지 않는다. 이번 게이트 미통과분은 추가 수정 없이 보류한다.
