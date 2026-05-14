# 주행의 재미 리서치 — "돌아다니는 재미"를 어떻게 설계할 것인가

> 세션 14 (2026-04-25) · 1주차 플레이 피드백 반영.
> 컨셉 전환: 레이싱 서킷 문법 → 시뮬+탐험 각도 (ETS2 / Snowrunner / Pacific Drive / Death Stranding / Short Hike / Firewatch 라인).
> 병렬 웹조사 7건 + 기존 `map-design-research.md`(세션 11) 위에 적층.

---

## 1. 컨셉 전환 배경

1주차 Play 테스트 후 도현 피드백 세 줄:
(1) "맵 돌아다니기 자체의 재미 없음"
(2) "주행할 때 신경써야 할 요소(실시간 화면 관리)가 있으면 좋겠음"
(3) "맵의 크기와 생김새가 너무 비슷. 오픈월드에서 느끼는 돌아다니는 재미 없음"

그리고 방향성: "트랙이 꼭 배송을 위한 게 아니라, 실제 있을법한 지형이나 마을에서 배송을 하는 것. 지금 가진 요소로 최대한 마을 느낌, 고저차, 열차." 즉 **레이싱 서킷 문법의 평면 격자**에서 **시뮬+탐험의 마을·지형**으로 축을 옮기라는 요청이다. 세션 11에서 잡은 Lynch 5요소 프레임은 **유효하되 불충분** — 도시 인지 이론은 "길을 잃지 않는다"까지만 해결하고, "돌아다니는 것 자체가 재미있다"는 다른 층의 문제다. 이 층을 다룬 게임들을 해부한다.

---

## 2. 레퍼런스 비교 분석

### 2-1. Euro Truck Simulator 2 (SCS)
**재미의 정체:** "A→B" 그 자체를 콘텐츠화한 원형. 단조로운 장거리 운전이 재미있는 이유는 세 가지 — (a) **지역 고유성**: 남유럽 좁은 마을 ↔ 스칸디나비아 국경 ↔ 알프스 ↔ 영국 해안이 각기 다른 "공기"를 준다. (b) **쉽게 배우고 오래 파기**: 주차/루트 선택/연료/휴식은 직관적이지만 깊이가 있음 — 연료 효율 루트 vs 속도 루트 선택이 실제로 의미. (c) **옵션 심층 시스템**: 피로·화물 종류·실제 법규를 플레이어가 켜고 끌 수 있어 자기 톤 조절 가능. 라디오·대시보드·시간대 변화가 지루함의 임계점을 늦춘다.
**PitCrew 교훈:** 5~15분 런 안에서도 "이 구역은 다르다"가 느껴져야 한다. 세션 11 Lynch 구역은 팔레트만 분리됐고 **공기**가 분리되지 않았다.

### 2-2. Snowrunner (Saber Interactive)
**재미의 정체:** **지형이 저항한다.** 진흙/빙판/강/눈의 점성 차이가 그냥 텍스처가 아니라 물리 수치 — 어두운 점토엔 바퀴가 빠지고, 저속 1단 기어로 트랙션을 찾는다. "돌아가는 게 빠르다"는 역전 구조. 맵 제작자는 wetness mask를 별도 브러시로 칠해 "여긴 젖은 땅"을 **시각-물리 일치**시킨다. 긴장감은 속도가 아니라 **저속에서의 실패 비용**에서 나온다 — 한 번 빠지면 10분 견인.
**PitCrew 교훈:** 고저차 + "이 길은 위험하다" 시각 신호 + 물리적 저항. Kenney `road-slant` 경사 타일을 단순 장식 아닌 **속도 저하 + 화물 Shock 증가 구간**으로 쓰면 즉시 적용 가능.

### 2-3. Pacific Drive (Ironwood)
**재미의 정체:** **루트가 매번 달라지고, 매번 불안하다.** 출발 전 정션(Junction) 지도를 보고 루트를 고른다 — 각 정션엔 시간/이상현상/dead-end 가능성이 표시. 주행 중엔 **계측 UI 부하** — 배터리/연료/타이어/트렁크 상태를 대시보드 계기로 끊임없이 체크. 스테이션(차고)은 **유일한 안전지대**이자 회귀점. 로그라이트 루프: 출발 → 자원 수집 → 귀환 → 업그레이드.
**PitCrew 교훈:** (a) **출발 전 경로 계획 UI**가 "배달 보드"와 물리적으로 일치해야 한다 (세션 11 Burnout 결과와 동일). (b) 주행 중 **계기 1~2개**가 항상 움직여야 한다 — 도현 피드백 (2)번 "신경쓸 요소"의 정답. 연료 또는 Shock 게이지. (c) 차고 = 픽업 허브 = 회귀점의 삼위일체.

### 2-4. Death Stranding (Kojima Productions)
**재미의 정체:** **이동이 곧 계획이고, 계획이 곧 이야기다.** 하중/무게중심/지형 스캔이 통합 시스템 — 쌓은 높이, 좌우 밸런스, 발밑 지형 경사가 전부 물리로 작용. 플레이어들이 각자 다리·로프·도로를 세우면 **비동기 멀티로 공유** → 지도 위에 "먼저 간 사람의 흔적"이 누적. 이동이 창작 행위가 된다.
**PitCrew 교훈:** (a) **화물 배치가 운전에 영향** — Shock 시스템을 단순 "급가속 → 손상"이 아니라 "포장 상자 쌓은 방향 × 급커브"로 확장하면 한 층 더 깊어진다 (MVP 초과 가능성 있음 → Tier 3 검토). (b) **누적 경험** — 여러 런에 걸쳐 같은 지점이 반복 방문되면, 거기에 "자주 지나가는 길" 시각 표식(쓰레기 정리됨, 노면 반들반들) 같은 저비용 장식이 감정 이입을 만든다.

### 2-5. A Short Hike (Adam Robinson-Yu)
**재미의 정체:** **작은 맵이 오히려 크게 느껴지는 마법.** 핵심 3가지 — (a) **수직 탐험**: 정상이 목적이고, 절벽·계곡·동굴·좁은 길이 높이 축에서 다양성을 만듦. (b) **랜드마크 밀도**: 맵이 작기에 시야에 늘 2~3개의 "저기 뭐 있나?"가 있음. (c) **미니맵 없음**: 플레이어가 **주의 깊게 지형을 보는 수밖에 없음**. 신호와 기호(깃발, 벤치, 전망대)가 곧 지도.
**PitCrew 교훈:** 이 게임이 PitCrew의 **가장 가까운 스케일 참조**다. 13×9 격자는 크기 문제가 아니라 **수직 축 부재** + **랜드마크 밀도 부족** 문제다. Kenney `tile-high/slantHigh` + `sign-highway`를 활용한 "전망대 뷰 포인트 1~2개"가 산출한다.

### 2-6. Firewatch (Campo Santo)
**재미의 정체:** **지도와 실제 지형을 대조하는 행위 자체가 게임플레이.** 종이 지도를 손에 들고 나침반으로 방향 잡음 — UI가 아닌 소품. 레벨은 겉보기엔 자유 탐험이지만 시야축(sightline)으로 **슬며시 목적지 쪽으로 유도**. 화재 연기가 진행에 따라 시야를 좁힘 → 환경 자체가 감정 곡선.
**PitCrew 교훈:** (a) **미니맵 의존도 낮추기** (세션 11 학술 결과 재확인). (b) 주행 목적지에 **시각적 앵커** — 멀리서 보이는 굴뚝 연기, 점멸 네온, 높은 건물. 플레이어가 "저기구나" 판단할 수 있도록. (c) 환경을 천천히 바꿔 감정 누적 (시간대/안개) — 단 이건 Tier 3.

### 2-7. Forza Horizon 5/6 (대비점)
**재미의 정체 & 차이점:** 오픈월드 레이싱이지만 **바이옴당 시그니처 랜드마크** 원칙은 위 모두와 일치. "If you can see it, you can drive it." 다만 장르가 달라서 — 바이옴 11개 같은 스케일은 4주 MVP 불가. **3~4개 마이크로 바이옴**으로 축소한 세션 11 결정이 유효.
**PitCrew 교훈:** "탐험을 게임 활동화" — 단순 드라이브만으로도 보상이 있어야 한다. 표지판 발견, 포토 스팟, 작은 컬렉터블. MVP에서는 "배달 중 경유 유인" 수준으로 충분.

### 공통 분모 — "돌아다니는 재미"의 5요소

| # | 요소 | 설명 |
|---|---|---|
| 1 | **지역 고유성** | 각 구역이 시각·공기(톤/조명/소리)·동선에서 다름 |
| 2 | **물리적 저항 또는 계기 부하** | 지형·날씨·연료 등 "신경쓸 무엇"이 실시간 작동 |
| 3 | **랜드마크 밀도와 수직성** | 시야에 늘 "저기로 가볼까"가 있음. 특히 고저차 |
| 4 | **경로 선택의 의미** | 최단/안전/풍경 중 트레이드오프. 지름길·샛길 보상 |
| 5 | **회귀점(허브)** | 차고/스테이션/광장이 루프의 앵커. 귀환 감각 |

---

## 3. "마을 배송" 재미 요소 카탈로그

| 요소 | 효과 | Kenney 구현 가능성 | MVP 난이도 (1~5) |
|---|---|---|---|
| **지형 고저차 (언덕)** | 시야 변화, 속도 관리 긴장, 랜드마크 돋보임 | `road-slant`/`slant-curve`/`slant-flat`/`tile-slantHigh` 이미 실측됨 | 3 — 타일 연결 규칙 추가 필요 |
| **다리/고가** | 수직 레이어링, 시각 드라마 | `road-bridge` + `bridge-pillar`/`bridge-pillar-wide` 실측됨 | 3 — Y축 배치 규칙 |
| **터널/지하도** | 잠시 시야 차단 → 출구에서 개방감 | Kenney 직접 터널 없음. `road-slant`로 내려갔다가 `road-square`로 덮는 수제 해석 가능 | 4 — 자작 필요 |
| **열차/대형 이동체** | 예측 가능 이벤트 교차, 리듬 | Kenney city-kit에 train 없음. Car Kit 버스로 대체 or 별도 train 에셋 도입 | 3~4 — 단순 Transform.Translate waypoint |
| **랜드마크 군집** | 길찾기, 정체성 | skyscraper 5종 + 굴뚝/공장/K앵커 이미 배치. **추가: `sign-highway-detailed`** | 1 — 재배치 |
| **전망대/뷰 포인트** | 잠시 멈춤, 방향 재확인, 쉼표 | `tile-slantHigh` 위에 `light-square` | 2 |
| **동선 다양성 (지름길·샛길·cul-de-sac)** | 반복 런 의미, 학습 보상 | 이미 지름길 4, dead-end 2 있음 (D-034~D-036). 추가 `road-split` Y분기 | 1~2 |
| **구역 정체성 (팔레트/사운드/조명)** | 공기 분리 — "저기 다운타운이다" | 팔레트는 세션 11, 조명/사운드는 미적용 | 2~3 |
| **서사 힌트 (간판/포스터/우편함)** | 환경 스토리 | `sign-highway-*` + city-kit 소품 | 2 — 디테일 프롭 pass |
| **인프라 (주유소·정비소·열차역)** | 계기 루프와 연결점, 허브 확장 | city-kit 내 존재 여부 확인 필요. 없으면 기성 building에 라벨만 | 2~3 |
| **NPC 차량** | 생명감, 우발적 장애물 | Car Kit 3종 + waypoint 이동 | 3 — 충돌 처리 주의 |
| **보행자/동물** | 생명감, 속도 제약 | Kenney character-kit 소품 사용. Animator 최소 | 4 — 애니메이션 비용 |
| **시간대 변화 (낮/저녁)** | 감정 누적, 재방문 변화감 | URP Directional Light 각도/색 | 2 — 프리셋 전환만 |
| **동적 날씨 (비/안개)** | 시야 축소, 물리 변화 | URP Fog 있음. 파티클 커스텀 필요 | 5 — **아웃** |
| **화물 밸런스 (스택/무게)** | 주행 스타일이 손상에 직결 | Shock 이미 있음. 확장 설계 | 4 — Tier 3 후보 |
| **누적 흔적 (자주 지난 길 반질반질)** | 반복 플레이 감정 | 노면 타일 swap (URP material decal 수준) | 4 — Tier 3 |

---

## 4. 현재 PitCrew 맵 진단

`SetupComplexScene.cs` (15×9) + `decisions-log.md` D-033~D-038 + 도현 피드백 교차 분석.

| 진단 항목 | 현 상태 | 근본 원인 | 레퍼런스 대비 갭 |
|---|---|---|---|
| **고저차** | 전부 Y=0 평면 | `road-slant` 타일 미도입 | Short Hike, Snowrunner 모두 수직성이 핵심. PitCrew 0% |
| **구역 공기 분리** | 팔레트만 분리(세션 11) | 조명/사운드/건물 높이 차등 없음 | ETS2·Forza 바이옴당 시그니처. PitCrew는 "같은 공기의 색깔만 다른 구역" |
| **랜드마크 임팩트** | 4개(K/T/F×2), 평지 | 높이 차로 멀리서 안 보임 | Short Hike: 맵 크기 대비 랜드마크 밀도 3배 이상 |
| **주행 중 부하** | 속도계만, Shock 수동 | 실시간 계기 변화 요소 부재 | Pacific Drive 4~5개 계기. 도현 피드백 (2)와 직접 대응 |
| **경로 선택 의미** | 지름길 4, dead-end 2 | 지름길 간 차별 없음 (동일 거리/안전도) | Snowrunner "진흙 우회 vs 직진" 같은 트레이드오프 없음 |
| **허브 앵커링** | Start 1곳, DeliveryHub 1곳 | 차고/픽업/정비 분리 안됨 | Pacific Drive 차고 = 모든 루프의 기준점 |
| **생명감** | 정적 건물만 | NPC/동물/보행자/이동체 0개 | ETS2 라디오, Forza 동물/교통. PitCrew는 "무인 도시" |
| **서사 힌트** | 없음 | 간판/포스터/우편함 0개 | Firewatch 환경 스토리가 감정 몰입 핵심 |
| **맵 "끝" 감각** | 외곽 페이드아웃 | Edge 요소 부재 | 세션 11에서 이미 권고, 미적용 |

**핵심 요약 3줄:**
- **평면성**이 1차 범인. 고저차가 들어가면 랜드마크 임팩트·구역 공기·경로 선택 세 가지가 동시 해결된다.
- **주행 중 부하** 부재가 2차. 도현 피드백 (2)번은 "계기 1~2개"로 즉시 답할 수 있다.
- **생명감·서사** 부재가 3차. MVP 4주 한계상 부분 적용만.

---

## 5. 개선안 우선순위 (Tier 1/2/3)

### Tier 1 — 1주 내, 큰 효과 (MVP 통합)

| # | 항목 | 추정 시간 | 기대 효과 |
|---|---|---|---|
| T1-A | **고저차 도입** — 외곽 링의 한 변(예: 북쪽 row 8)에 `road-slant` 업힐 + `tile-slantHigh` 언덕 1곳. Downtown 쪽을 내려다보는 뷰 | 1~2일 | 수직성·랜드마크 가시성·"전망" 동시 해결 |
| T1-B | **경사 구간 물리 효과** — slant 위에서 속도 감쇄 + Shock 가산. `ArcadeCarController`에 1차 수정 | 반나절 | 도현 피드백 (2) "신경쓸 요소" 직결 |
| T1-C | **랜드마크 재배치 + 높이 차등** — Downtown skyscraper Y-scale 1.5~2.0, 외곽 건물 0.8~1.2. `BuildingPlacer` 한 필드 추가 | 반나절 | 스카이라인 위계. 세션 11 파킹 항목 해제 |
| T1-D | **허브 앵커링** — Start/픽업/정비를 한 광장(roundabout `road-roundabout`)으로 통합. Downtown 중앙 3×3 점유 | 반나절~1일 | Pacific Drive 차고 패턴. 루프 체감 상승 |
| T1-E | **연료 또는 Shock 게이지 HUD 강화** — 기존 Shock을 실시간 부드러운 계기로 변환 | 반나절 | 주행 중 "보는 것"이 생김. 도현 (2) 답 |

**Tier 1 합계 3~4일.** 1주차 재미 검증 실패 → 2주차 초반에 T1 전부 투입 → 재검증 루프.

### Tier 2 — 2~3주 범위 (MVP 후반부 가능)

| # | 항목 | 기대 효과 |
|---|---|---|
| T2-A | **열차/버스 waypoint 이동체 1~2대** — city-kit 타일에 평면 레일 칠하고 Translate 이동 | 예측 가능 이벤트 교차. 리듬 |
| T2-B | **NPC 차량 2~3대** — Car Kit 3종이 고정 루프 도는 수준 | 생명감, 가벼운 장애물 |
| T2-C | **다리 구간 1개** — 지름길 4 중 하나를 `road-bridge` + pillar로 승격 | 수직 레이어링 |
| T2-D | **서사 힌트 pass** — `sign-highway-*`, 포스터 데칼 10~15개 구역별 분산 | 환경 스토리 시작 |
| T2-E | **시간대 2종 프리셋** — 낮/저녁 URP 프로파일. 런 시작 시 선택 | 재방문 변화감 |
| T2-F | **구역별 사운드 레이어** — Downtown 저음 웅성 / 산업 기계 / 교외 새소리. 5초 루프도 효과 큼 | ETS2 공기 분리 |

### Tier 3 — v2 이후 (파킹)

- 동적 날씨(비/안개 + 물리 변화)
- 화물 배치/스택 시뮬레이션 (Death Stranding 결)
- 누적 흔적 노면 변화 (자주 지난 길 시각화)
- 비동기 공유(로프/쉼터) — Kojima형
- 보행자/동물 애니메이션 (Kenney character-kit)
- 터널/지하도 자작 에셋
- 미니맵 allocentric 디자인 (세션 11 파킹 유지)

---

## 6. 다음 세션 Top 3 착수 제안

### 제안 1 — T1-A + T1-B 묶음: **"북쪽 언덕" 구간 도입**
- `road-slant`·`road-slant-curve`·`tile-slantHigh`를 `TileMapAuto`가 mask로 해석하도록 확장 (현재 `#` 단일 → `#`/`/`/`^` 분리)
- 북쪽 row 7~8 한 구간에 업힐 → 정점 → 다운힐
- `ArcadeCarController`에 slope speed penalty + Shock 가산
- **검증:** Downtown 중앙 교차로(H4 부활지)에서 고저차 인지되는가? 언덕 올라 아래 조망되는가?
- **리스크:** D-024/D-026 교훈 — slant 타일은 y축 bounds 복잡. `PreviewBend.cs` 패턴으로 side-by-side 검증 필수.

### 제안 2 — T1-C + T1-D 묶음: **랜드마크 위계 + 중앙 허브**
- Downtown skyscraper 5종 Y-scale 랜덤 1.5~2.2배, 외곽 건물 0.8~1.2배
- 중앙 교차로를 `road-roundabout` (3×3 타일)로 교체 → Start/픽업/정비를 광장에 통합
- 로터리 둘레에 `light-square` 4개 + `sign-highway-detailed` 2개
- **검증:** 맵 어느 위치에서도 Downtown 스카이라인 보이는가? 루프 종료 시 "허브로 돌아왔다" 감각 있는가?
- **리스크:** roundabout 3×3이 격자 좌표계와 정렬되는지 확인 (`kenny-roads-catalog.md`: maxXZ 3.0). `TrackConnectivityVerifier` 재실행 필수 (세션 13 D-038 교훈).

### 제안 3 — T1-E 단독: **실시간 계기 HUD**
- 기존 Shock 값을 프레임당 smooth-interpolated 게이지로 변환
- HUD 하단에 아날로그 미터 1개 (Speed 옆)
- 경사 진입 시 Shock 증가율 시각적으로 드러남
- **검증:** 도현에게 "주행 중 신경쓸 요소 생겼다" 직접 피드백 요청
- **리스크:** 낮음. UI 레이어만 건드림. 기존 Shock 계산식 변경 금지 (기능 안정성).

**권장 순서:** 제안 3 → 제안 1 → 제안 2.
근거: 제안 3이 가장 저비용이고 즉시 피드백 확인 가능 (도현 (2) 직접 답). 제안 1이 가장 큰 시각 변화(도현 (1)(3) 동시 답), 위험도도 가장 큼 → 두 번째 배치. 제안 2는 제안 1이 검증된 후 위계 재조정으로 가는 게 낭비 적음.

---

## 7. 출처

### 업계/디자인
- [Euro Truck Simulator 2 — Why Play? (Quora)](https://www.quora.com/Why-do-people-play-Euro-Truck-Simulator-2-Whats-the-appeal-of-games-such-as-this-Not-an-accusation-but-a-genuine-question)
- [The Gamer — Most Picturesque Routes in ETS2](https://www.thegamer.com/euro-truck-simulator-2-best-routes/)
- [Gaming Nexus — ETS2 Review](https://www.gamingnexus.com/Article/Euro-Truck-Simulator-2/Item3756.aspx)
- [Univers-Simu — SnowRunner Terrain Physics Interview](https://www.univers-simu.com/en/news/snowrunner-nous-parle-de-la-physique-du-terrain-7903/)
- [Mudrunner Mods — SnowRunner Terrain Physics](https://www.mudrunnermods.com/terrain-physics-snowrunner/)
- [Gaming Trend — SnowRunner Review](https://gamingtrend.com/reviews/a-muddy-good-time-snowrunner-review/)
- [GamingBolt — Pacific Drive Everything You Need to Know](https://gamingbolt.com/pacific-drive-everything-you-need-to-know)
- [Eneba — Pacific Drive Endless Expeditions Roguelite](https://www.eneba.com/hub/news/weird-driving-sim-pacific-drive-gets-roguelite-mode-in-endless-expeditions-update/)
- [Pacific Drive Wiki — Anomalies](https://pacific-drive.fandom.com/wiki/Anomalies)
- [Game Truth — Death Stranding Cargo & Route Planning](https://www.gametruth.com/guides/death-stranding-cargo-management-guide-route-planning-and-weight-balance/)
- [505 Games — Death Stranding Beginner's Guide](https://deathstrandingpc.505games.com/en/death-stranding-directors-cut-beginners-guide/)
- [Jordan Thayer — Death Stranding: A Playground of Algorithms](https://jordanthayer.com/death-stranding-a-playground-of-algorithms/)
- [Game Developer — A Design Discussion on Death Stranding](https://www.gamedeveloper.com/design/a-design-discussion-on-death-stranding)
- [PlayStation Blog — A Short Hike: Crafting a Tiny Open World](https://blog.playstation.com/2021/08/05/crafting-a-tiny-open-world-a-look-behind-the-scenes-at-the-creation-of-a-short-hike/)
- [Alexia Mandeville — A Short Hike Design Breakdown](https://alexiamandeville.medium.com/game-design-breakdown-a-short-hike-5a7a17d740e5)
- [Game Developer — How Firewatch Builds Character Through Map Design](https://www.gamedeveloper.com/design/how-firewatch-builds-character-through-map-design)
- [Game Developer — How Firewatch's UI Enhances Immersion](https://www.gamedeveloper.com/design/how-firewatch-s-ui-enhances-player-immersion)
- [Academia — Environment as Narratives: Firewatch](https://www.academia.edu/33975111/Environment_as_Narratives_Firewatch_Analysis)
- [Forza — Horizon 5 Biomes & Seasons](https://forza.net/news/exploring-forza-horizon-5-biomes-and-seasons)
- [Xbox Wire — Forza Horizon 6 Preview Japan](https://news.xbox.com/en-us/2026/04/08/forza-horizon-6-preview-japan-collectibles-seamless-races-open-world-design/)

### 내부 문서
- `GDD/research/map-design-research.md` — 세션 11 Lynch 5요소 리서치
- `GDD/research/kenny-roads-catalog.md` — 도로 타일 72 FBX 실측
- `notes/decisions-log.md` — D-024~D-038 맵 결정 히스토리
- `F1/Assets/Editor/SetupComplexScene.cs` — 현재 15×9 mask 레이아웃

---

## 핵심 3줄 요약

1. **"돌아다니는 재미"의 공통 해법은 (a) 지역 고유성 (b) 실시간 계기 부하 (c) 수직성·랜드마크 밀도** — 세 요소 모두 현재 PitCrew 맵에서 비어 있음. 세션 11 Lynch 프레임은 길찾기까지만 해결했고, "재미"는 다른 층이었음.
2. **Tier 1 5개 항목을 2주차에 전부 투입하면 1주일에 끝난다** — 고저차(T1-A/B), 랜드마크 위계(T1-C), 중앙 허브 로터리(T1-D), 실시간 계기 HUD(T1-E). 전부 Kenney 기존 타일(`road-slant`, `road-roundabout`, `tile-slantHigh`) + 기존 컨트롤러 수정만.
3. **다음 세션 권장 순서: 제안 3(HUD) → 제안 1(언덕) → 제안 2(허브).** 피드백 루프 짧은 것부터, 시각 변화 큰 것 다음, 위계 재조정 마지막. 열차·NPC·시간대는 Tier 2로 파킹 — MVP 4주 안에 들어갈 수 있지만 1주차 피드백의 직접 답이 아님.
