# 구조 조각 10 — OSM 실제 건물 윤곽을 v9 위에 앉히기 (D-114)

결과 씬 `F1/Assets/Scenes/TownGen/TownGen_Structure_v10.unity`(규칙 집 끔) · 스위치 켠 씬 `TownGen_Structure_v10_fill.unity`(빈 곳만 규칙 집)
측정 `tools/town-gen/out/scene_structure_s10/measure.json` → `osmBuildings` 블록 (fill 판은 `scene_structure_s10fill/`)
QA 톱다운 1장 `tools/town-gen/out/shots/s10_top.png` (땅 전체 4096×3549, 0.78m/px, 위 = 북)
메뉴 `PitCrew/TownGen/Build Structure A + OSM buildings (s10)` · `... + rule fill (s10 fill)`

## 만드는 순서 (재생성)

1. `py -X utf8 tools/town-gen/fetch_sources.py` — 상자 A `way/relation[building]` 를 4×4 칸으로 받아 합침 → `osm/structure_a_buildings_<날짜>.json` (한 번에 물으면 Overpass 502/500, 2026-10-20 실측)
2. `py -X utf8 tools/town-gen/buildings_prep.py` → `out/structure_a/buildings.json`(로컬 m 윤곽) + `buildings_stats.json`(분포)
3. 에디터 메뉴 s10 (약 31초)

## 측정 (2026-10-20 빌드)

- 받은 건물 753개(상자 밖 중심 14 · building=no 1 뺌). 면적 중앙값 102m² · p10 17 · p90 329 · 최대 1,480m². 합 113,612m²
- 구역별(직선거리 근사): 아말피 중심(항구 150m 안) 77 · 가장자리(150~350m) 158 · 그 밖 해안·산 496 · 콘카(씨앗 100m 안) 22
- 층수 태그 있는 건물 4개뿐(2·3층), height 태그 0 → **676/680 이 2~4층 가설**(osm id 해시로 결정적, 층 3m 가설)
- 길과 겹치는 윤곽 85 = 차로(중심선 3m 안) 43 · 보도(4m) 11 · 골목(1.5m) 12 · 어깨만(6m) 19. 앞의 셋(66)과 광장 겹침 7 → 뺌. **놓은 건물 680**(중심 41 · 가장자리 132 · 해안·산 490 · 콘카 17)
- 앉히기(규칙 집과 같은 문턱): 땅에 바로 68 · 벽 내림 280 · 반지하 235 · **3m 넘음(계단식: 바닥 = 최저+3m, 오르막 쪽 지형을 깎음) 97** — 깎기 최대 17.6m
- fill 스위치: 실제 건물 윤곽에서 15m 밖 후보만 → 규칙 집 52(중심 8 · 가장자리 8 · 상점 9 · 콘카 14 · 외딴집 10 등)
- 코어 검사 9/10 PASS, M7 FAIL 12.33m = 기존(v9 와 같은 값, DEM 30m 절벽 도로)

## 알려진 점 / 판단 요청

- **중심 77채 중 21채가 차로에 걸림.** 길 너비는 우리가 만든 단면(차로 반폭 3m) 기준이라 실제 OSM 길보다 넓다. 중심 북쪽 큰길(x≈2840)은 건물 사이를 관통하는 것으로 보임 — 아말피 중심 큰길이 실제로 건물 아래 덮인 길이라 그럴 가능성(추정, 미확인)
- OSM 중심 건물 수가 실제 아말피보다 적어 보임(톱다운에서 빈 땅이 넓음 — 추정). fill 스위치가 그 빈 곳을 메우는 용도
- 계단식 97채는 큰 건물이 가파른 비탈에 걸친 경우 — 위쪽 깎기 최대 17.6m. 지붕이 평평한 단순 상자라 비탈 위에서 상자가 크게 튀어나와 보일 수 있음(화면 판정 거리)
- relation 내부 구멍(안뜰)은 버림. 상자 지붕은 평지붕
