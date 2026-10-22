# 구조 조각 11 — 아말피 중심(항구점 반경 400m)을 '실제 마을'로

씬 `F1/Assets/Scenes/TownGen/TownGen_Structure_v11.unity` (나머지는 v10 과 같은 입력·규칙)
측정 `tools/town-gen/out/scene_structure_s11/measure.json` → `townCore` 블록
QA 톱다운 1장 `tools/town-gen/out/shots/s11_core_top.png` (항구점 중심 840m 사각, 3072px = 0.27m/px, 위 = 북)
메뉴 `PitCrew/TownGen/Build Structure A + Amalfi town core (s11)` (약 15초)

## 만드는 순서 (재생성)

1. `py -X utf8 tools/town-gen/town_paths_prep.py` → `out/structure_a/town_paths.json` (반경 400m 안 highway 전 종류 + tunnel/covered/layer/width 태그 + area=yes 광장 고리)
2. 에디터 메뉴 s11

## 빠져 있던 것 (조각 10 기준, 반경 400m OSM 실측)

- OSM 길이: footway 2,909m · steps 1,640 · primary 1,047 · pedestrian 1,045 · residential 793 · service 475 · living_street 292 · path 149
- 조각 10 은 `network.json`(major·minor·pedestrian·steps)만 읽어 **footway 2.9km·path 0.1km 가 통째로 없었다.** pedestrian 은 중심 200m 골목 완화 안만, steps 는 200m 안만.
- 광장 6곳(Piazza Duomo 1,040m² · Piazza dei Dogi 913 · Spirito Santo 635 · Largo Scario 260 · 2곳)이 `area=yes` 면인데 **둘레 선(골목)으로 들어갔다.**

## 조각 10 에서 중심 건물이 빠진 원인 (python 대조: 중심 150m 건물 ↔ 가장 가까운 OSM 길)

- **광장 둘레 선**: 광장 면 둘레가 건물 벽 선을 그대로 따라 그려져 있어, 둘레를 골목으로 그리면 둘레 건물이 전부 "길에 걸침" → 빠짐 (Piazza dei Dogi·Duomo·Largo Scario·Cortile 둘레 약 25채가 거리 0~1.5m)
- **SS163 터널 구간**: Via Matteo Camera(way 158974287·191288044, tunnel=yes, layer=-1)가 건물 아래를 지나는데, 지상 6m 차도로 그려 위 건물 약 9채가 걸림
- covered/tunnel 보행로(1503580164·153211994·1321467020) 위 3채
- 그리고 우리 차도 단면(차로 6m + 보도)이 실제 길보다 넓어 보도 겹침으로 빠진 몇 채

## 이번 규칙

- 차도(primary·residential 등)는 SplineTown 이 그대로(v10 과 같음). **living_street·service·pedestrian·footway·steps·path 는 OSM 선 그대로** 따로 깐다.
- 폭: OSM `width` 태그가 있으면 그것(예: Scalinata del Duomo 11m, Via Lorenzo d'Amalfi 4m), 없으면 가설 — living_street 4 · service 4 · pedestrian 3.5 · footway 2 · steps 2 · path 1.5 (m)
- 건물과 겹치면 **건물은 그대로, 길 폭을 줄인다**: 반폭 = min(가설 반폭, 가장 가까운 건물 윤곽까지 거리), 최소 0.35m
- 중심선이 건물 안: tunnel/covered 태그 있으면 "건물 아래로 지나감", 없으면 "건물 관통"으로 세고 그리지 않음(상자 안에 숨음)
- 광장 면 = 1m 칸 포장, 건물 안 칸은 뺀다. **만든 광장(30×24)은 v11 에서 끔** — 실제 광장 6곳이 대신
- 중심 400m 건물: 차로(중심선 3m) 겹침일 때만 뺀다, 보도·골목 겹침은 살림. tunnel/covered 차도 선분은 겹침 판정에서 뺌
- steps = 디딤 0.35m·단 0.17m 상자(공용 계단과 같은 꼴), 나머지 = 지형 위 0.06m 띠. 보행길·광장 칸은 지형 2m 격자로

## 측정 (2026-10-19 빌드)

- 그린 길이: footway 2,541m · steps 1,399 · pedestrian 815 · living_street 284 · service 225 · path 149
- 건물 아래로 지나감(태그): pedestrian 212 · steps 179 · footway 66 · living_street 8 / 건물 관통(태그 없음): steps 58 · footway 20 · pedestrian 18
- 폭 줄임 구간: pedestrian 443 · steps 210 · footway 126 · living_street 89
- 바다 위라 안 그림: service 250 · footway 281 (방파제·부두 선 — 땅이 바다로 처리된 곳)
- 차도(반경 안): 큰길 1,234m · 골목 69m
- 건물(반경 안 OSM 252): **놓음 241 · 뺌 11(전부 차로 겹침)**. 같은 길에 조각 10 규칙이면 20 뺌. 터널 차도 위 건물 6채 살림, 보도·골목 겹침 3채 살림. 구역 중심(150m) 41 → 67
- 건물 사이 틈(이웃 윤곽까지 최소 거리): 중앙값 **0m**(붙어 있음) · p90 2.5m · 0.3m 안 붙은 건물 199/241
- 길에 면한 건물(윤곽이 보행길 가장자리·광장·차도 보도 끝에서 1.5m 안): **61%(147/241)** — 보행길 135 · 차도 18 · 광장 33. 나머지는 블록 안쪽(다른 건물에 둘러싸인) 건물로 추정

## 알려진 점 / 판단 요청

- **중심 차도 곡선 12 → 10(−298m)**: living_street(Via Lorenzo d'Amalfi 등)·service 를 차도 생성기에서 빼고 4m 보행길로 그린 결과로 추정(미확인). 배달 차가 중심 골짜기 길로 못 들어갈 수 있음 — 그 길을 차로 둘지 기획 판단
- SS163 터널 구간(Via Matteo Camera)은 지상 차도 그대로 + 위 건물 살림 → 차도가 건물 상자 안을 지나감(지붕 없는 터널 구멍 아님)
- 건물 관통(태그 없음) 96m — OSM 선과 건물 윤곽이 1~2m 어긋난 곳으로 추정
- 지붕 평평한 단순 상자, 높이 대부분 2~4층 가설(조각 10 과 같음). 보행길·광장 재질 = 돌 포장 하나
