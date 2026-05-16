# Map Expansion — Round 1 (3배)

**Date**: 2026-05-16 (세션 27)
**Author**: brainstorming with 도현
**Goal**: 도시 17×13 → **30×22 (180×132m, ~3배)**. 한 런 1~2분 목표의 첫 디딤돌.
**Decision class**: D-072 (예약) — 맵 확장 단계적 진입 (절반 먼저 → 측정 → 다음).

---

## 1. 배경

세션 26 `RunMetricsRecorder` 측정: 한 런 **8~12초 / 49~110m**. 데모 목표 5~15분과 200~900배 갭. 원인은 도시가 너무 작음 (17×13 셀, ~102×78m). 거리 확장이 escalation·차고·페이싱 같은 다른 결정의 선결조건.

전체 목표는 ~17배 (~330×230m)지만 한 번에 가지 않음. 단계적: **3배 → 측정 → 더 늘림**. activeContext의 "33×23 → 일단 절반" 직관 적용.

---

## 2. 결정

### 2.1 사양

| 항목 | 현재 | R1 후 |
|---|---|---|
| 셀 그리드 | 17×13 | **30×22** |
| 셀 크기 | 6m | 6m (유지) |
| 실 사이즈 | ~102×78m | **~180×132m** |
| 면적 배수 | 1× | ~3× |
| Layout asset | `OpenWorldLayout.asset` | `OpenWorldLayoutV2.asset` (신규, 기존 보존) |

### 2.2 zone 3개 — 대각선 분할

쿠어덤(수직 3등분) 아닌 대각선/L자. 자연스러운 도시 흐름, "고향→이웃" 잠금 해제 서사 명확.

```
                col 0 ─────────────────────────── col 29
row 21 ┌─────────────────────────────┐
       │                  ░░░░░░░░░░ │   Industrial
       │           ░░░░░░░░░░░░░░░░░ │   (우상)
       │   DOWNTOWN ░░░░░░░░░░░░     │
       │ ▒▒▒▒▒▒▒▒▒▒░░                │   Downtown
       │ ▓▓▓▓▒▒▒▒▒▒▒▒                │   (중앙 대각)
       │ ▓▓▓▓▓▓▒▒▒▒                  │
       │ ▓▓▓▓▓▓▓▓     HARBOR         │
       │ ▓▓▓▓▓▓                      │   Harbor
       │ ▓▓▓                         │   (좌하)
row  0 └─────────────────────────────┘
```

- zone 경계는 **셀→zone 매핑 함수**로 표현. hard rectangle 아님.
- 구현: `TrackLayout`에 `string[] zoneMask` 추가 (`H`/`D`/`I`/`.`). 또는 별도 SO `ZoneMap`. 사용 빈도 낮으므로 `TrackLayout`에 박는 게 자연.
- 기존 `ZoneRegistry`의 3 zone SO (Downtown/Harbor/Industrial) 재사용. `ZoneUnlockSystem.GetZoneOf(deliveryCell)` 함수는 cellToZone lookup으로 단순화.

### 2.3 도로 — 손 ASCII + zone별 성격

procedural 도입 안 함 (660셀은 손으로 1시간). 대신 zone별로 의도된 도로 패턴:

| zone | 격자 패턴 | 표현 의도 | 셀 예산 (roads) |
|---|---|---|---|
| Harbor (좌하) | 긴 동서 직선 2 + 부두 dead-end 3 | 트럭, 속도 좋음 | ~25 |
| Downtown (중앙) | 4셀 블록 격자 (조밀) | 코너 많음, 신호등 자리 | ~50 |
| Industrial (우상) | 8셀 블록 (성김) + 굵은 N-S 간선 | 단순·고속 | ~30 |

3 zone 잇는 **간선(arterial) 2~3개** 보장:
- A1: Harbor 중앙 → Downtown 남측 → Industrial 동측 (L자 횡단)
- A2: Downtown 북측 → Industrial 서측 직결 (대각)
- (A3 optional: Harbor 동단 → Industrial 남단 우회)

**검증 의무**: `RunFullRebuild` Step 6의 `TrackConnectivityVerifier` — 모든 zone 도달, isolated 없음, End 개수 의도된 cul-de-sac만.

### 2.4 배달지 11개

| zone | 개수 | 라벨 후보 |
|---|---|---|
| Harbor | 3 | PierEnd, CargoShip, Warehouse |
| Downtown | 4 | OfficeCornerN, OfficeCornerS, CentralPlaza, MainStation |
| Industrial | 4 | FactoryGateA, FactoryGateB, MotorDepot, ContainerYard |

- start cell = Downtown 중앙 (대각 분할의 한가운데 부근, col≈14 row≈11 어림)
- 잠금: Downtown 시작 unlock, Harbor·Industrial는 `ZoneUnlockSystem` cost로 해제 (cost는 R1 측정 후 재튜닝, 일단 기존 값 유지)
- 마커 간 평균 직선거리 목표: **80~140m** (한 잡 1~2분 가정, 미달 시 R2에서 늘림)

### 2.5 heightMask 정책

- Downtown: 전 평지 (`.`)
- Harbor: 전 평지 (부두 = 해수면 가정, slope 없음)
- Industrial: 일부 1~2 tier elevation, slope tile (`n/s/e/w/=/+`)로만 표현. 일반 road cell에 height 박지 않음 (asset.md D-050).
- 기존 `OpenWorldLayout`의 heightMask 패턴 참고 (이미 1~2 tier 운영 경험 있음).

### 2.6 BuildingCatalog 재사용

기존 `BuildingCatalog` (`F1/Assets/Data/`)에 zone별 building 키 추가만. 신규 모델 X (Kenney 라인업 내, asset.md). 미사용 모델 우선 활용 (industrial 21 + low-detail 14 + decor 6 = 세션 17 확인 41개).

---

## 3. 구현 단위 (plan은 다음 단계에서)

| # | 단위 | 검증 |
|---|---|---|
| 1 | `OpenWorldLayoutV2.asset` 신규 (roadMask·buildingMask·heightMask·zoneMask·deliveries·startCell) | inspector 시각 확인 |
| 2 | `TrackLayout.cs`에 `zoneMask` 필드 + `GetZoneChar(c, r)` 추가 | compile clean |
| 3 | `ZoneUnlockSystem.GetZoneOf` zoneMask 기반으로 단순화 (delivery 라벨 매칭 폐기) | unit test 또는 unity-cli exec |
| 4 | 활성 layout을 R2로 swap (TileMapAuto 사용처) | unity-cli editor refresh + play |
| 5 | `RunFullRebuild` 실행 → `TrackConnectivityVerifier` 통과 확인 | menu 호출 |
| 6 | manual play 1~2런 + `RunMetricsRecorder` 측정 | CSV 확인 |
| 7 | reward/잡 cost/zone unlock cost 1차 재튜닝 (측정 기반) | play |

자세한 step 분해는 writing-plans 단계에서.

---

## 4. 비목표 (이 라운드 안 함)

- 17배 풀 확장 (R2 이후)
- AI 차량 시스템 (그래프만 깨끗하게, AI는 W4~W5)
- 신규 화물 종류, 신규 건물 모델, 신규 UI
- escalation 시스템 (런 안 시간 압박) — 맵 측정 후 별도 brainstorm
- 차고/부품 시스템 — W3
- 폴리시 (회색 큐브, 미니맵 강조, ResultPanel 라벨 간격) — W4+

---

## 5. 측정 기준 (R1 완료 = 이 셋)

1. **`TrackConnectivityVerifier` 통과** — isolated 0, 모든 zone 도달 가능
2. **5런 자동 측정 CSV** — 한 런 30초~60초 도달 (R1은 1~2분 풀 도달 아님)
3. **3 zone 모두 배달지 도달 확인** — 수동 play 3회

3 다 통과면 R1 종료. 그 다음 R2(60×40+) brainstorm 진입.

---

## 6. 리스크 / 메모

- 6m 셀 30개면 카메라 follow가 도시 전체를 한 화면에 못 담음 — 미니맵 의존도 ↑. 미니맵 zoom 재튜닝 필요할 수 있음 (R1 끝에서 확인).
- `OpenWorldLayout.asset` 참조하는 코드/씬 검색 후 swap 누락 없는지 확인 의무.
- 잠금 시스템: 기존 `autoUnlockOnAffordable` (세션 23) 유지. zone unlock cost는 8초 런 기준이라 재튜닝 필수지만 R1 측정 결과 보고 결정.
- `Channel8DowntownEnrich` 같은 기존 Editor enrich 스크립트가 OpenWorldLayout 가정하고 있을 수 있음 — V2 대응 필요할지 grep으로 확인.
