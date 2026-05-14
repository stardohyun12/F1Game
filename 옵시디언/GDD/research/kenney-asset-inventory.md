# Kenney 에셋 전수 인벤토리 (PitCrew)

> 2026-04-25 스캔. `F1/Assets/` 하위 Kenney 임포트 전체.
> 컨셉 전환("현실 마을/시골 배송, 고저차 + 열차") 기준으로 "당장 쓸 수 있는 것"을 가려내기 위한 공통 레퍼런스.
> **도로 72종 타일 치수는 별도 문서 참조:** `GDD/research/kenny-roads-catalog.md` (세션 11 D-034).

---

## 섹션 1: 전체 요약

Kenney 에셋은 `Model/`, `UI/`, `Sound/` 세 곳에 분산 임포트.

```
F1/Assets/
├─ Model/
│  ├─ kenney_car-kit/                  (차량 + 디브리 + 카트 + 휠, 53 FBX)
│  ├─ kenney_train-kit/                (기차 + 철로 + 트램, 100+ FBX)
│  ├─ kenney_city-kit-suburban_20/     (교외 건물 21 + 조경 소품 19, 총 40)
│  ├─ kenney_city-kit-commercial_2.1/  (시내 건물 20 + 고층 5 + 디테일 6 + 저폴리 16, 총 47)
│  ├─ kenney_city-kit-industrial_1.0/  (공장 건물 20 + 굴뚝 4 + 탱크 1, 총 25)
│  └─ kenney_city-kit-roads/           (도로 73 FBX, roads-catalog.md 참조)
├─ UI/
│  ├─ kenney_ui-pack/                  (82 sprite · 5색 변종 · Extra 포함)
│  ├─ kenney_ui-pack-space-expansion/  (60 sprite · 5색 변종 · 패널/게이지/크로스헤어)
│  ├─ kenney_input-prompts-pixel-1-bit/ (816 input tile · 키보드/게임패드/마우스, 1-bit 픽셀)
│  └─ Fonts/  (Kenney Future SDF, Kenney Future Narrow SDF)
└─ Sound/
   ├─ kenney_interface-sounds (1)/     (100 ogg · back/click/close/confirmation/error/toggle 등 20카테고리)
   ├─ kenney_ui-pack/Sounds/           (6 ogg · click/switch/tap a/b)
   └─ EngineLoop.mp3                   (엔진 루프, 출처 불명 — Kenney 아님 가능)
```

규모 대략: **3D 모델 280+ FBX, UI 스프라이트 958, SFX 106, 폰트 2종.** 애니/파티클/셰이더/HDRI/NPC rig 없음.

---

## 섹션 2: 카테고리별 상세

### 1. Vehicle Kit (`F1/Assets/Model/kenney_car-kit/Models/FBX format/`)

| 분류 | 파일 (일부) | 수량 | 현재 사용 | 마을 배송 잠재력 |
|---|---|---:|---|---|
| Sedan/SUV | `sedan.fbx`, `sedan-sports.fbx`, `suv.fbx`, `suv-luxury.fbx`, `hatchback-sports.fbx` | 5 | 미사용 | ★★ (경쟁 차량, AI 트래픽) |
| Delivery/Truck | `delivery.fbx`, `delivery-flat.fbx`, `truck.fbx`, `truck-flat.fbx`, `van.fbx`, `garbage-truck.fbx`, `box.fbx` | 7 | 미사용 | **★★★★★ 주인공 차량 후보 (델리버리/밴)** |
| Emergency | `police.fbx`, `ambulance.fbx`, `firetruck.fbx`, `tractor-police.fbx`, `taxi.fbx` | 5 | 미사용 | ★★★ (이벤트·검문) |
| Tractor | `tractor.fbx`, `tractor-shovel.fbx` | 2 | 미사용 | ★★★ (시골 느낌, 장애물·저속차) |
| Kart/Race | `kart-oobi/oodi/ooli/oopi/oozi.fbx`, `race.fbx`, `race-future.fbx` | 7 | `race.fbx` 1종 Race.prefab에 사용 | ★ (마을 컨셉엔 부적합, 향후 미션에만) |
| Debris | `debris-*.fbx` (bolt/bumper/door/plate 등) | 16 | 미사용 | ★★ (사고 연출/화물 파편) |
| Cone | `cone.fbx`, `cone-flat.fbx` | 2 | 미사용 | ★★★ (교통 통제, 주차장) |
| Wheel | `wheel-*.fbx` (default/dark/racing/tractor/truck) | 9 | Race 휠로 간접 | — |

**하이라이트:** `delivery.fbx` + `delivery-flat.fbx` + `van.fbx` — 마을 배송 주인공 차량으로 즉시 스왑 가능. `tractor.fbx`는 시골 풍경에 "느린 장애물" 심기 재미 요소.

### 2. Train Kit (`F1/Assets/Model/kenney_train-kit/Models/FBX format/`)

| 분류 | 파일 패턴 | 수량 | 현재 사용 | 마을 배송 잠재력 |
|---|---|---:|---|---|
| 증기기관차 | `train-locomotive-a/b/c.fbx`, `train-locomotive-passenger-a/b.fbx` | 5 | 미사용 | ★★★★ (시골 "건널목 기다림" 긴장) |
| 디젤 | `train-diesel-a/b/c.fbx`, `train-diesel-box-a/b/c.fbx` | 6 | 미사용 | ★★★ |
| 전기 고속 | `train-electric-bullet-a/b/c.fbx` | 3 | 미사용 | ★★ (도시 배경) |
| 전기 통근/지하철/스퀘어/더블 | `train-electric-city/subway/square/double-a/b/c.fbx` | 12 | 미사용 | ★★ |
| 트램 | `train-tram-classic/modern/round.fbx` | 3 | 미사용 | ★★★★ (도심 도로 공유) |
| 객차/화차 | `train-carriage-*` (box/coal/container ×3색/dirt/flatbed/lumber/tank ×2/wood) | 12 | 미사용 | ★★★ (화물 컨테이너 - 시각 디테일) |
| Connector | `train-connector.fbx` | 1 | 미사용 | — |
| 철로 (ground) | `railroad-*` (corner/curve/straight/skew/hill/bump/bend, 3 variant × 18) | 54 | 미사용 | ★★★★★ (철도 레이아웃) |
| 철로 (rail only) | `railroad-rail-*` | 18 | 미사용 | — (겹치기용) |
| 손상 철로 | `railroad-damaged-*` | 18 | 미사용 | ★★ (시골 폐선 분위기) |
| spline / track | `spline-*.fbx`, `track*.fbx` | 7 | 미사용 | ★ |

**하이라이트:** 철도 시스템 전체가 풀패키지. 마을 한복판에 건널목 1~2개 심고 `train-locomotive-a` + `train-carriage-coal ×3`을 주기적으로 지나가게 하면 "5분에 한 번 20초 대기" 이벤트 완성. 트램은 도심 스테이지용 예비.

### 3. City Kit (Suburban) (`F1/Assets/Model/kenney_city-kit-suburban_20/Models/FBX format/`)

| 분류 | 파일 | 수량 | 현재 사용 |
|---|---|---:|---|
| 교외 주택 | `building-type-a` ~ `building-type-u` | 21 | **21/21 BuildingDef 등록 완료** (S 풀 + X 풀) |
| 울타리 | `fence.fbx`, `fence-low.fbx`, `fence-1x2/1x3/1x4/2x2/2x3/3x2/3x3.fbx` | 9 | **미등록** |
| 진입로 | `driveway-long.fbx`, `driveway-short.fbx` | 2 | **미등록** |
| 보도/징검 | `path-long/short.fbx`, `path-stones-long/short/messy.fbx` | 5 | **미등록** |
| 조경 | `planter.fbx`, `tree-large.fbx`, `tree-small.fbx` | 3 | **미등록** |

**전원 디테일 핵심 소품이 전부 미등록.** 현재 DefaultTrackLayout에 S/X 풀만 가져다 씀.

### 4. City Kit (Commercial) (`F1/Assets/Model/kenney_city-kit-commercial_2.1/Models/FBX format/`)

| 분류 | 파일 | 수량 | 현재 사용 |
|---|---|---:|---|
| 표준 건물 | `building-a` ~ `building-n` | 14 | **11/14 등록** (C 풀). 누락: e, j, k는 `commercial_big_*`로 별도 분류. 실제 미등록 하나도 없음 |
| 고층 | `building-skyscraper-a` ~ `building-skyscraper-e` | 5 | **5/5 등록** (H 풀) |
| 저폴리 LOD | `low-detail-building-a` ~ `low-detail-building-n`, `low-detail-building-wide-a/b` | 16 | **전부 미등록** |
| 파사드 디테일 | `detail-awning.fbx`, `detail-awning-wide.fbx`, `detail-overhang.fbx`, `detail-overhang-wide.fbx`, `detail-parasol-a/b.fbx` | 6 | **미등록** |

**하이라이트:** low-detail 16종 — 원거리 LOD 용도라 대형 맵에서 프러스텀 바깥 건물로 유용. `detail-parasol-a/b`는 가게 앞 차양으로 마을 상점 디테일.

### 5. City Kit (Industrial) (`F1/Assets/Model/kenney_city-kit-industrial_1.0/Models/FBX format/`)

| 분류 | 파일 | 수량 | 현재 사용 |
|---|---|---:|---|
| 공장 | `building-a` ~ `building-t` | 20 | **F 풀(2) + W 풀(3) = 5개만 등록.** 15개 미등록 |
| 굴뚝 | `chimney-basic/small/medium/large.fbx` | 4 | **T 풀에 2개만(large, medium)**. basic/small 미등록 |
| 기타 | `detail-tank.fbx` | 1 | **미등록** |

**하이라이트:** 공장 건물 15종, 작은 굴뚝 2종, 석유탱크 1종이 놀고 있음. 마을 외곽 "작은 공장 지대" 조성에 활용 가능.

### 6. Road Kit (`F1/Assets/Model/kenney_city-kit-roads/`)

73종 FBX. 상세 치수는 `kenny-roads-catalog.md` 참조. 여기선 **컨셉 전환에 쓸만한 미활용 카테고리만** 추출.

| 분류 | 대표 파일 | 수량 | 현재 사용 | 잠재력 |
|---|---|---:|---|---|
| 다리 / 교각 | `road-bridge.fbx`, `bridge-pillar.fbx`, `bridge-pillar-wide.fbx` | 3 | **전부 미등록** | ★★★★★ (강/계곡 건너기) |
| 경사/고저차 | `road-slant.fbx`, `road-slant-curve.fbx`, `road-slant-flat.fbx`, `road-slant-high.fbx`, `tile-slant.fbx`, `tile-slantHigh.fbx` (+ barrier 변종) | 11 | **미등록** | ★★★★★ (언덕 시골 지형) |
| 로터리 | `road-roundabout.fbx`, `road-roundabout-barrier.fbx` | 2 | 미등록 | ★★★ |
| 진입 | `road-driveway-single.fbx`, `road-driveway-double.fbx` (+barrier) | 4 | 미등록 | ★★★ (목적지 건물 문앞) |
| 가로등 | `light-curved.fbx`, `light-curved-cross.fbx`, `light-curved-double.fbx`, `light-square.fbx`, `light-square-cross.fbx`, `light-square-double.fbx` | 6 | **미등록** | ★★★★ (야간 마을 분위기) |
| 표지판 | `sign-highway.fbx`, `sign-highway-wide.fbx`, `sign-highway-detailed.fbx` | 3 | 미등록 | ★★★ (거리 안내) |
| 공사 | `construction-cone.fbx`, `construction-barrier.fbx`, `construction-light.fbx` | 3 | 미등록 | ★★★ (도로 공사 이벤트) |
| 건널목 | `road-crossing.fbx` | 1 | 미등록 | ★★★★★ (열차 교차) |
| 인도 | `road-bend-sidewalk.fbx` | 1 | 미등록 | ★★ |
| 높낮이 타일 | `tile-high.fbx`, `tile-low.fbx` | 2 | 미등록 | ★★★ (지형 기복) |

### 7. UI Pack — 기본 (`F1/Assets/UI/kenney_ui-pack/PNG/{색상}/Default/`)

82종 스프라이트. 색상 5종(Grey/Blue/Red/Green/Yellow). 카테고리:

| 카테고리 | 스프라이트 (수) | 현재 사용 |
|---|---|---|
| Button rectangle/round/square | 각 10 variant (border/depth/flat/gloss/gradient/line × depth 유무) | `button_rectangle_depth_flat[Grey]`, `button_rectangle_flat[Yellow]` 2종만 |
| Slide (horizontal/vertical, grey/color) | 12 (section, wide 변종 포함) | `slide_horizontal_grey_section_wide[Grey]` 1종만 |
| Check (round/square, color/grey) | 12 (checkmark/cross/square 변종) | 미사용 |
| Icon (checkmark/circle/cross/square, outline 유무) | 8 | 미사용 |
| Arrow (basic/decorative × NSEW × small/large) | 16 | `arrow_decorative_n_small` UITheme.arrowNorth 등록 |
| Star | 3 | 미사용 |

### 8. UI Pack — Extra (`.../PNG/Grey/Extra/Default/`)

| 카테고리 | 파일 | 활용 |
|---|---|---|
| Icon — Arrow up/down (dark/light/outline) | 6 | **방향 지시 HUD** |
| Icon — Play (dark/light/outline) | 3 | 재생 버튼 |
| Icon — Repeat (dark/light/outline) | 3 | 재시도 |
| Input (rectangle/square, outline) | 4 | 숫자 입력, 탐색창 |
| Divider | 2 | 섹션 구분 |
| Line-only Button | 6 | 서브 버튼 |

### 9. UI Pack — Space Expansion (`F1/Assets/UI/kenney_ui-pack-space-expansion/PNG/{색상}/Default/`)

60종. 공간/테크 스타일. 카테고리:

| 카테고리 | 파일 | 활용 |
|---|---|---|
| Bar — round/square × gloss × large/small (l/m/r/square 조각) | 40 | **체력/연료/화물무게 게이지** (9-slice) |
| Button square header — blade/large/notch/small × rectangle/square × screws 유무 | 16 | 미션 제목 패널, 섹션 헤더 |
| Crosshair color a/b/c/d | 4 | 미니맵 포커스, 목적지 마커 |

### 9b. UI Pack — Space Expansion Extra

`panel_glass*`, `panel_rectangle*`, `cursor_a~h`, `crosshair_a~d` — 반투명 글래스 패널(9종), 커서(8종), 크로스헤어(4종).
**UITheme.panelDark/panelBright/panelGloss는 이 Extra에서 가져온 것.**

### 10. Input Prompts Pixel 1-bit (`F1/Assets/UI/kenney_input-prompts-pixel-1-bit/`)

- 816 타일. 키보드 전키, 게임패드(Xbox/PS/Switch), 마우스, 터치, VR 컨트롤러 제스처까지.
- 현재 스타일(SDF 폰트 + 고해상도 sprite)과 **비주얼 불일치** (1-bit 픽셀). 키 바인딩 UI에 섞어 쓰기 어려움.
- 활용 권장: 튜토리얼 팝업(키 힌트) 또는 로고/의도적 레트로 씬. **MVP 스코프 밖.**

### 11. Fonts (`F1/Assets/UI/`)

| 파일 | 포맷 | 현재 사용 |
|---|---|---|
| `kenney_ui-pack/Font/Kenney Future.ttf` | TTF | **사용 중단** (session-13에서 가독성 문제로 LiberationSans로 폴백) |
| `kenney_ui-pack/Font/Kenney Future Narrow.ttf` | TTF | 동상 |
| `UI/Fonts/KenneyFuture SDF.asset` | TMP SDF | `UITheme.fontPrimary` (guid 8f58...) |
| `UI/Fonts/KenneyFutureNarrow SDF.asset` | TMP SDF | `UITheme.fontCondensed` (guid 5d94...) |

**Kenney에 다른 폰트 없음.** Rocket/Pixel/Mini 등 Kenney 외부 폰트는 미임포트.

### 12. Audio / SFX

| 팩 | 경로 | 수량 | 현재 사용 |
|---|---|---:|---|
| interface-sounds | `F1/Assets/Sound/kenney_interface-sounds (1)/Audio/` | 100 ogg (20 카테고리 × 3~5 variant) | click_001, back_003, bong_001, confirmation_001 (UITheme.sfx*) |
| ui-pack/Sounds | `F1/Assets/UI/kenney_ui-pack/Sounds/` | 6 ogg (click/switch/tap a/b) | 미사용 (중복) |
| 엔진음 | `F1/Assets/Sound/EngineLoop.mp3` | 1 mp3 | Race 차량 엔진 (출처 불명) |

**interface-sounds 20 카테고리:**
`back · bong · click · close · confirmation · drop · error · glass · glitch · maximize · minimize · open · pluck · question · scratch · scroll · select · switch · tick · toggle`

**주목:** `drop_*`(배달 성공 SFX 후보), `error_*`(실패), `scratch_*`(브레이크/긁힘), `pluck_*`(아이템 획득), `tick_*`(타이머/QTE). 현재 4종만 사용 → 잔여 96종 중 최소 5~6종은 게임플레이 SFX에 편입 가능.

**Kenney `impact-sounds`/`digital-audio`/`rpg-audio` 등은 미임포트.** 차량 엔진음/충돌음/환경음(빗소리/바람/새)은 Kenney 에셋에 없음.

### 13. Animation / VFX / Shader

- **Kenney 팩 전체에 rigged NPC, 애니메이션 클립 없음.** (`FBX format/` 안에 걷기 애니 포함된 캐릭터 모델 부재)
- 파티클/셰이더 없음. URP 기본 셰이더만 사용. DriftSkid/DriftSmoke는 Unity 내장 파티클 + 머티리얼 커스텀.
- HDRI/Skybox 없음. Unity Default Skybox 사용.

---

## 섹션 3: 마을 배송 컨셉에 당장 쓸 수 있는 미사용 TOP 15

| # | 에셋 | 경로 | 용도 |
|---|---|---|---|
| 1 | `delivery.fbx` + `delivery-flat.fbx` + `van.fbx` | `kenney_car-kit/.../FBX format/` | **주인공 차량 스왑** (현재 race.fbx) |
| 2 | `train-locomotive-a.fbx` + `train-carriage-box.fbx` × 3 | `kenney_train-kit/.../FBX format/` | 마을 건널목 주기적 통과 |
| 3 | `railroad-straight.fbx` + `railroad-corner-small.fbx` + `road-crossing.fbx` | 각 kit | 철로 레이아웃 + 도로 교차 |
| 4 | `light-curved.fbx` + `light-square.fbx` (Road kit 가로등 6종) | `kenney_city-kit-roads/.../FBX format/` | 야간/저녁 분위기 |
| 5 | `fence.fbx`, `fence-1x2.fbx` ~ `fence-3x3.fbx`, `fence-low.fbx` (9종) | `kenney_city-kit-suburban_20/.../FBX format/` | 마당 경계, 농장 느낌 |
| 6 | `tree-large.fbx` + `tree-small.fbx` + `planter.fbx` | 동상 | **자연 배치 (현재 0개 심어져 있음)** |
| 7 | `road-bridge.fbx` + `bridge-pillar.fbx` × 2 | `kenney_city-kit-roads/.../FBX format/` | 강/계곡 다리 |
| 8 | `road-slant.fbx` + `tile-slant.fbx` + `tile-high.fbx` + `tile-low.fbx` (경사/높낮이 11종) | 동상 | **도현 피드백 "고저차" 직접 대응** |
| 9 | `tractor.fbx` + `tractor-shovel.fbx` | `kenney_car-kit/.../FBX format/` | 시골 느린 장애차량, 경적 이벤트 |
| 10 | `sign-highway.fbx` × 3 | `kenney_city-kit-roads/.../FBX format/` | 거리 안내 표지판 (지명 decal) |
| 11 | `construction-cone.fbx` + `construction-barrier.fbx` + `construction-light.fbx` | 동상 | 도로 공사 이벤트 (우회) |
| 12 | `path-stones-long/short/messy.fbx` + `path-long/short.fbx` + `driveway-long/short.fbx` | `kenney_city-kit-suburban_20/.../FBX format/` | 집 진입로 (목적지 시각화) |
| 13 | `chimney-small.fbx` + `chimney-basic.fbx` + `detail-tank.fbx` (미등록 공장 소품 3종) | `kenney_city-kit-industrial_1.0/.../FBX format/` | 마을 외곽 작은 공장 |
| 14 | `detail-parasol-a/b.fbx` + `detail-awning.fbx` × 2 + `detail-overhang.fbx` × 2 | `kenney_city-kit-commercial_2.1/.../FBX format/` | 상점 파사드 (가게 느낌) |
| 15 | `icon_arrow_up_outline.png` + `crosshair_color_a.png` (UI Extra) | `kenney_ui-pack*/PNG/.../Extra/Default/` | 목적지 화살표 HUD + 미니맵 마커 |

---

## 섹션 4: Kenney에 없어 보이는 것 (공백)

MVP 컨셉 달성을 위해 부족한데 현재 임포트된 Kenney 팩에는 없는 것:

| 공백 | 영향 | 대응 |
|---|---|---|
| **Rigged NPC (보행자/주민)** | 마을에 사람 없음 → "현실감" 결여 | Kenney `toon-characters`/`blocky-characters` 팩 추가 임포트 검토. 없으면 단순 Capsule 프록시 |
| **동물 (개/고양이/소/닭)** | 시골 생동감 | Kenney `animated-animals`/`platformer-characters` 팩 필요 |
| **환경음 (빗소리/바람/새/엔진)** | 현재 `EngineLoop.mp3` 1개 | Kenney `impact-sounds`, `rpg-audio`, 외부 freesound |
| **충돌음 (크래쉬/스크랩)** | 차량 충돌 SFX 없음 | Kenney `impact-sounds` (미임포트) 또는 generation |
| **날씨 VFX (비/눈)** | "밤/비" 분위기 불가 | Unity URP Particle 직접 제작 |
| **Skybox / HDRI** | 하늘이 기본값 단색 | Unity Asset Store 무료 Skybox |
| **셰이더 (지형 블렌드, 물결)** | 강/호수 표현 불가 | Shader Graph 직접 |
| **애니메이션 클립 (차문/깃발)** | 깃발/나무 정적 | Animator 직접 |
| **특수 폰트 (Pixel/Rocket)** | Future 계열만 | Kenney `kenney-fonts` 번들 확인 필요 |

---

## 섹션 5: 이미 사용 중인 에셋 체크리스트 (중복 조사 방지)

### 차량
- `F1/Assets/Model/kenney_car-kit/Models/FBX format/race.fbx` — Race.prefab 차체
- `wheel-racing.fbx` — Race 휠 (4개)

### 건물 (`F1/Assets/Data/Buildings/` 에 등록된 53 BuildingDef — CLAUDE.md의 "40개"는 과거 수치)
- **Suburban (S/X 풀):** `building-type-a~u` 중 21개 전부 (S 풀 18개 + X 풀 3개)
- **Commercial (C 풀):** `building-a~n` 중 11개 (a,b,c,d,f,g,h,i,l,m) + big-commercial 풀(K)에 e,j,k,n 4개
- **Skyscraper (H 풀):** `building-skyscraper-a~e` 5개
- **Factory (F 풀):** industrial의 `building-c, building-l` 2개
- **Warehouse (W 풀):** industrial의 `building-e, building-f, building-g` 3개
- **Tower (T 풀):** `chimney-large, chimney-medium` 2개
- **Plaza (P 풀):** `road-square.fbx` 1개

**상세 pool 정의:** `F1/Assets/Editor/SetupLayoutAssets.cs` line 22~82, Entry 등록 line 110~118.

### UI Theme (`F1/Assets/Resources/UITheme.asset`)
- `fontPrimary` → `KenneyFuture SDF.asset` (guid 8f58...)
- `fontCondensed` → `KenneyFutureNarrow SDF.asset` (guid 5d94...)
- `panelDark/Bright/Gloss` → space-expansion/Extra 패널 3종
- `barTrack` + `fillOk/Warn/Crit/Accent` → space-expansion bar 5종
- `buttonNormal/Highlight` → space-expansion panel_glass 계열 2종
- `arrowNorth` → ui-pack Grey `arrow_decorative_n_small.png` 등
- `iconCheck` → ui-pack Grey `icon_checkmark.png` 등

**실제 런타임 폰트:** session-13 폴리시 커밋(`4ce5a64`, `42d6a1c`)에서 가독성 이슈로 **LiberationSans로 런타임 교체**. SDF는 backup 상태.

### SFX (UITheme)
- `sfxClick` → `interface-sounds/Audio/click_001.ogg` (guid f932...)
- `sfxBack` → `back_003.ogg` (guid 0989...)
- `sfxConfirm` → `confirmation_001.ogg` (guid cf0e...)
- `sfxShift` → `bong_001.ogg` (guid 3fd4...)

### 도로
- 세션 11에서 DefaultTrackLayout/ComplexTrackLayout 마스크 기반 자동 배치. 세부 `SetupLayoutAssets.cs` + `TileMapAuto.cs`.
- **실제 쓰이는 도로 FBX는 `kenny-roads-catalog.md` 참조.** 73종 중 상당수 미활용 (bridge/slant/roundabout/crossing/light/sign 전체 포함).

---

## 부록: 즉시 활용 우선순위 (리서치 A/B/C 인풋)

- **리서치 A (마을 컨셉):** 섹션 3 TOP 15 중 #1~#12가 핵심 원재료. 철도/가로등/울타리/나무/경사로 조합으로 마을 분위기 98% 커버.
- **리서치 B (긴장 요소):** #2 (열차 통과), #9 (트랙터 장애), #11 (공사 우회), #15 (목적지 HUD). 시간 압박 + 환경 방해 요소.
- **리서치 C (UI 고급화):** 섹션 2-9 bar 9-slice 게이지 확장, Extra 아이콘(arrow up/down/play/repeat), crosshair로 미니맵 마커. 현재 3종 사용 → 20종으로 확장 여력.

> **다음 액션:** 리서치 A/B/C 각각 이 문서를 인풋으로 받아서 "어떤 미등록 에셋을 어떻게 배치할지" 구체적 레이아웃 기획.
