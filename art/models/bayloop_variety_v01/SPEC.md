# D-140 매개변수 건물 — 제작·인계 계약

세계32px/m, `pitcrew_d123_32` 원본 albedo, CC0 실사진, Nearest, flat shading, 건물당4,000tris 상한을 생성기 안에 고정한다. AI 이미지/배경0. 이탈리아 해안 회벽·아치·덧창·옥상/기와 형태와 눈높이 간판·설비·화분·빨래를 조합한다. 사진은 기존 A1/D137 공급자 메타데이터와 SHA를 재사용했다.

## 입력과 실행

`tools/asset-gen/bayloop_buildgen.py`, Blender 백그라운드 전용. `--config <JSON>`은 아래 레시피 배열을 받으며 `--type`, `--ids`로 부분 재생성한다. 기본63개 레시피는 `recipes.json`. 같은 입력과 seed는 같은 geometry/UV를 만든다. FBX 바이너리의 타임스탬프까지 같은 것을 의미하지 않는다.

```json
[{
  "id":"town_house_custom_01", "type":"town_house", "width":6.2,
  "depth":7.2, "floors":3, "roof":"terracotta_gable",
  "ground_floor":"entry_steps", "opening_rhythm":"offset",
  "balcony":"single", "shutter_color":"green", "awning":false,
  "wall_color":"cream", "wear":"medium",
  "life":{"sign_count":1,"ac_unit":true,"planters":2,"laundry":true},
  "seed":140007, "zone":"medium", "front_space_m":1.25
}]
```

| 입력 | 허용·실제 효과 |
|---|---|
| type | town_house / detached_house / shop_house / terrace_house / cliff_villa / farmhouse: 주택 조립 문법. 시설5종은 아래 별도 제한 |
| width / depth / floors | 본체폭4~10m, 깊이2~14m, 정수층1~4. 층층집·빌라의 최상층 후퇴 후 유효깊이≥3m. 층고2.8m. 4층은2창열로 폴리 예산 제어 |
| roof | flat(옥상난간+출입실) / terracotta_gable / vault / metal. 기존 교토 처마·격자 문법 사용 안 함 |
| ground_floor | shop_arch / garage / entry_steps / windows. windows도 사람이 들어가는 출입문을 유지 |
| opening_rhythm | paired / regular / offset. seed는 작은 창 위치 변화·옥상 출입실의 좌우에 적용 |
| balcony | none / single / full / alternating. terrace/cliff는 후퇴한 보행 테라스가 우선 |
| terrace_living | 선택 bool, 생략하면 false. 층층 주거의 상층 창·출입문을 교대하고 테라스 생활 부속을 더하는 보정. 기본63개 중 terrace_house_06/12만 true. 문자열 `"false"` 등 bool이 아닌 값은 거부 |
| shutter_color | green / brown. 사진 기반 공용 팔레트 도장/목재 |
| awning | 가게 아치 위 천 차양 여부. 가게 아치 없는 주거 전면에는 차양을 억지로 추가하지 않음 |
| wall_color | cream/chalk/sand/ochre/blue/mist/rose/warm_grey. **6색 계열+2명도 변형**이다. 고정팔레트에 분홍이 없어 rose는 바랜 회갈색 계열 |
| wear | light/medium/heavy: 문턱·아랫벽·배수관 접지 주변 패치 높이/범위. 전체에 랜덤 노이즈를 뿌리지 않음 |
| life | 간판0~3·실외기bool·화분0~4·빨래bool. 상점은 실제 Galmuri 한국어, 주택은 숫자 주소 표지 |
| seed | 정수, 재현 가능한 구체 배치. style을 무작위로 덮어쓰지 않음 |

시설 `chapel_bell/school_small/bus_stop/fuel_stand/cafe_kiosk`는 **요청된 각1개를 위한 고정 기능 프로필**이다. 폭·깊이·팔레트는 입력이지만 지붕/층/개구부·생활 부속은 시설 문법으로 고정하고 미지원 override는 거부한다. seed는 시설의 식별값으로만 기록한다. 모든 입력을 모든 시설에서 자유 조합하는 범용 건축 시스템까지 만들었다고 주장하지 않는다. 학교의 기본층2, 나머지1; 종탑 높이는 별도 형상이다.

```powershell
D:/Tools/Blender/blender.exe -b -t 6 --python-exit-code 1 --python tools/asset-gen/bayloop_buildgen.py -- --catalog
D:/Tools/Blender/blender.exe -b -t 6 --python-exit-code 1 --python tools/asset-gen/bayloop_buildgen.py -- --type town_house
D:/Tools/Blender/blender.exe -b -t 2 --python-exit-code 1 --python tools/asset-gen/bayloop_buildgen.py -- --geometry-only
py -3.13 tools/asset-gen/bayloop_buildgen_prepare.py
py -3.13 tools/asset-gen/check_buildgen.py --manifest art/models/bayloop_variety_v01/manifest.json --out art/_review/2026-10-25-bayloop-variety/checks.json --workers 4
```

`--no-render`는 형상·FBX/GLB만 갱신한다. 그 뒤 이전 PNG/게이트를 최신 결과의 증거로 쓰면 안 된다. type별 manifest를 먼저 쓰고 prepare가 완료된 manifest만 합치므로 type별 병렬 제작은 가능하나 같은 id 동시 생성은 금지한다. prepare/provenance/F1 복사는 직렬이다.

## 피벗·구역·앞 공간

앞면+Z, Y-up, 바닥Y0, 1unit=1m, **본체 바닥 앞벽 가운데 원점**. 마당 포함 bbox 중앙으로 옮기지 않는다. `bounds_min/max`는 차양·담장·헛간·계단 돌출까지 포함한 실제 모델 외곽이다. `footprint_m`은 본체 기준이며 `front_space_m`는 설계된 앞 공간 폭이 아닌 깊이 예약값이다. 길에 앉힐 때 실제 전방 돌출 `bounds_max[2]`도 확인한다.

- dense: shop, 일부 terrace. 붙은 집/좁은 길의 용도 태그이며 실제 동네 배치는 빌더 담당.
- medium: town, 일부 terrace, 학교·정류장·키오스크. 집 사이 틈·보도는 빌더가 만든다.
- sparse: detached·farm·villa·성당·주유소. detached는 마당·돌담·열린 대문·통로 포함, 농가는 부속 헛간 포함.

차도→경계→공용 인도1.2~3m→사유 앞공간→건물의 D139 순서를 유지하도록 배치한다. 이 에셋이 공용 인도·도로·지형 기단을 포함한다고 오해하지 않는다. 마당을 포함한 집을 도로 끝에 본체 원점으로 바로 붙이면 안 된다. map building-types 표의 구역/치수 계약과 함께 사용한다.

## 공유 아틀라스·검사 범위

`BayLoopShared_01~04.png`, 각각1024², 4×4칸, 칸256px/양쪽2px 여백/유효252px. 한 모델1재질, 전체4장 공유. `bayloop_buildgen_textures.select_style`가 색·마모·덧창별 UV 칸을 선택한다. 모델 확대 후 UV 스케일을 방치하지 않고 실제 면 길이로 UV를 다시 작성한다.

normal 회벽은 실제사진 명암 중앙값으로 양자화해 작은 면의 변화가 사라지지 않게 했다. 국소 wear_wall은 별도 실제사진 분포다. 인위적 검사 픽셀·랜덤 노이즈는 없다. 사진2m 반복과 균일한 오래됨은 시각 게이트에서 남을 수 있는 한계다. 32색 검사는 **조명 전 albedo** 기준이며 실제 렌더의 조명/AA 결과 색수와 다르다.

check_buildgen은 기존 check_asset→check_buildings.inspect_asset를 그대로 병렬 호출한다. 모든 삼각형 밀도32±10%·POT·팔레트밖0·tris·원점·bounds·출처 장부를 검사한다. 주요면≥0.04㎡ 비단색 검사, 더 작은 면은 보고하되 밀도검사는 유지. 별도 실제 FBX 재임포트로 내장 albedo·정점·축·크기를 대조한다.

최종 시험 투입은 type 대표3개(단일 시설은 실제1개·3방향) 독립 시각 판정 및 전체 톤 시트 판정을 통과한 type만. `gate-summary.json`과 `trial-delivery.json`이 실제 상태 기준이다. 기존 a/b/c·.meta·프리팹을 변경하지 않는다.

**QA/프로그래밍 임포트 필요.** Point·sRGB·압축없음·원본1024, 외부 material remap, 필지/마당 돌출, 문·계단 접근, 콜라이더/LOD를 확인한다. Blender 정적 검사는 Unity 임포트·인게임 카메라/조명·성능·충돌의 검증이 아니다. 최종 도현 승인은 인게임960×540 캡처로 필요하다.

선택 입력 `shop_display`: bool, 기본 false. 상점 앞 실물 진열대·상품·상단 간판을 추가한다. 최종 shop_house_01에 적용.
