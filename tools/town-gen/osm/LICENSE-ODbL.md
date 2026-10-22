# OpenStreetMap 원본 — 출처·라이선스

- 데이터: © OpenStreetMap contributors, Open Database License (ODbL) 1.0 — https://www.openstreetmap.org/copyright
- 받은 것: Overpass API `way["highway"]` 두 상자 (쿼리·미러·OSM 기준 시각은 각 `*.meta.json`)
  - `amalfi_harbor_2026-10-19.json` — (S,W,N,E) 40.6315, 14.5990, 40.6365, 14.6070 · way 224 · OSM base 2026-10-19T02:38:41Z
  - `positano_slope_2026-10-19.json` — 40.6270, 14.4800, 40.6330, 14.4900 · way 251 · OSM base 2026-10-19T02:39:45Z
- 캐시 상자는 스펙 조각(아말피 240×100m, 포지타노 480×300m)보다 넉넉하게 잡았다. 실제 자르기·위치는 E2 앞단 A 에서 정하고 여기에 추가 기록한다.
- 게임 크레딧 표기: "© OpenStreetMap contributors"
- 가공본(roadMask·선형 도로망 JSON)은 ODbL 파생 DB 로 공개한다(D-107 수용).

## 가공 내용 (E1~E2 에서 채움)
- (아직 없음)

## 조각 10 (D-114, 2026-10-20) — 건물 윤곽
- `structure_a_buildings_2026-10-19.json` — 상자 A(40.6130, 14.5690, 40.6375, 14.6060) `way/relation["building"]`, 4×4 칸으로 나눠 받아 합침(한 번에 물으면 502). way 767 · relation 1. 쿼리·미러·OSM 기준 시각은 `.meta.json`
- 가공본 `out/structure_a/buildings.json`(로컬 m 윤곽·층수·구역)도 ODbL 파생 DB. 씬 크레딧 오브젝트 "© OpenStreetMap contributors (ODbL)" 유지
