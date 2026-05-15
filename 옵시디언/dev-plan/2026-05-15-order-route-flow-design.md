---
name: order-route-flow-rework
type: design-spec
created: 2026-05-15
status: amended-2026-05-15
target: Steam Demo (Vertical Slice)
related: "[[2026-05-13-vertical-slice-design]]"
---

# 주문 → 경로 계획 흐름 개편 설계

## ⚠ 2026-05-15 정정 (1차 구현 후 도현 피드백)

**원본 spec의 "1 주문 = 1~3 목적지" 모델은 오해.** 정정된 모델:

| 단위 | 정의 |
|---|---|
| **주문 1장 (OrderJob)** | 물건 1개 + 들를 곳 1곳. **항상 1:1.** |
| **한 런 (Run)** | 주문을 **여러 장 픽** (1~capacity, 채널 5). 픽한 주문들 = 이번 런의 작업. |
| **경로 계획** | 픽한 주문들의 목적지를 어떤 순서로 들를지 결정. |
| **포장 화면** | 픽한 주문 수만큼 아이템 포장 (각 큐브 = 1 주문). |
| **채널 5 capacity** | 동시에 받을 수 있는 **주문 수** (1~3). |

원본의 "잡 → 1~3 destinationLabels"는 "주문 → 정확히 1 destinationLabel"로 좁힘. 한 런의 다중 목적지는 **여러 주문을 픽**해서 얻음. 1차 구현 후 도현 지적 — anchoring on existing `itemCount: 1~3` field 했던 게 원인. 자세한 정정 내용은 각 조각 섹션에 inline 표기.

## 한 줄

주문 보드를 맵 중심으로 바꾸고, **여러 주문을 픽해서 묶고**, 픽한 주문들의 **배달 순서를 정하는 경로 계획 화면**을 추가한다.

## 배경 / 왜

도현 지적: "목적지가 어딘지 맵으로 보고 가야 하는데, 그냥 물건 선택만 하는 거지 — 어디로 배송할지까지 정하지는 못함."

현재 OrderBoard는 카드 우선이고, 모든 잡이 같은 배달지점 풀에서 "앞에서부터 N개"를 쓴다(`DeliveryManager.ActivateSubset`). 그래서 같은 크기 잡끼리는 같은 곳으로 가고, 배달 순서도 `points` 리스트 순서로 고정된다. "어디로 갈지"와 "어떤 순서로 갈지"가 둘 다 플레이어 결정이 아니다.

이건 USP #2(결정 밀도 — 한 런에 결정 4개: 주문/**경로**/세팅/업그레이드)의 "경로" 결정을 복원하는 작업이다. D-070에서 배달 목적지를 1개로 줄이며 경로 계획 단계를 사실상 잘랐는데, 이 spec이 그걸 되살린다.

## 스코프

이 spec은 **3개의 맞물린 조각**을 한 흐름으로 다룬다. 셋이 데이터 모델을 공유하고 순차 흐름을 이루므로 하나의 spec으로 둔다. 구현 계획에서 태스크로 쪼갠다.

### 포함

1. **데이터 모델** — `OrderJob`이 목적지 리스트를 가짐. 물건 1개 = 목적지 1곳.
2. **OrderBoard 개편** — 맵 중심 레이아웃(A안), 잡들의 목적지를 색깔별로 동시 표시.
3. **경로 계획 화면 (신규)** — 잡 고른 후 배달 순서를 정하는 전용 화면.

### 명시적 비포함 (이번 작업 아님)

- 가는 길의 위험/지형 난이도 계산 (도현 미선택)
- 화물과 목적지의 자유 조합 (잡 = 고정된 물건+목적지 묶음)
- 차고/부품 시스템 (W3 별도 작업 — 이 작업 끝난 뒤)
- 새 배달지점 추가 (씬의 기존 8개 재사용)
- 경로 길 찾기(pathfinding) — 거리는 직선 거리로 충분

## 현재 코드 사실 (설계 근거)

- **씬 배달지점 8개**: DowntownEntry(72,0,0), FactoryNorth(24,0,72), EastHub(96,0,36), LowerWest(24,0,24), SuburbanGate(24,0,0), CentralCross(48,0,36), WestHub(0,0,36), PlateauTop(84,0,72). (y는 0.5)
- **시작점**: `Race` 오브젝트 (42, 0.17, 36) — 차 스폰 셀.
- **`DeliveryManager`**: `points` List<DeliveryPoint>(8개, 직렬화). `ActivateSubset(n)` = 앞 N개 활성. `OnDeliveryReached(p)`는 `p == points[currentIndex]`일 때만 수락 → **배달 순서가 points 리스트 순서로 고정**. 다 끝나면 옵션으로 `startReturn` 복귀 후 `Finish`.
- **`DeliveryPoint`**: label, SphereCollider 트리거. `IsActive`(현재 목표만 true), `IsCompleted`. `Bind(mgr, label)`.
- **`DeliveryPointMarker`**: 기둥+구체 비주얼. `SetColor`/`SetDimmed`/`SetExternalControl` — OrderBoard가 호버 시 외부 제어.
- **`OrderBoard`**: `cardsContainer`(우측 세로), `cardPrefab`(OrderCardUI). `PickOffer` → `SpawnCards`. 호버 → `ApplyHoverState`로 `i < job.pointCount`인 마커만 강조. 클릭 → `Services.Packing.Show(job)`.
- **`OverviewCamera`**: 시작/정산 시 탑다운(75° pitch, center (36,0,24), height 95). OrderBoard 동안 이 카메라가 "맵".
- **`OrderJob`**: SO. `itemCount`[Range 1-3], `pointCount`, `cargoType`, 보상 등.
- **`RunController.BeginRun`**: `ActivateDeliveryPoints(job)` → `Delivery.ActivateSubset(job.pointCount)`. `BeginRun(job, packedItems)` 오버로드 = PackingScreen 핸드오프.
- **`PackingScreen.OnGoClicked`**: `Services.Run.BeginRun(currentJob, itemsCopy)`.
- **흐름**: OrderBoard → PackingScreen → RunController → 주행 → ResultPanel.

---

## 조각 1 — 데이터 모델

### OrderJob 변경

`OrderJob`에 목적지 리스트 추가:

```csharp
[Header("Destinations (채널 8 — 물건 1개 = 목적지 1곳)")]
[Tooltip("이 잡의 배달 목적지. 씬 DeliveryPoint의 label과 일치. 개수 = 물건 수.")]
public List<string> destinationLabels = new List<string>();
```

`itemCount`는 `destinationLabels` 길이에서 파생 (물건 1개 = 목적지 1곳 보장):

```csharp
// 기존: [Range(1,3)] public int itemCount = 1;  ← 직렬화 필드 제거
public int itemCount => destinationLabels != null
    ? Mathf.Clamp(destinationLabels.Count, 1, 3)
    : 1;
```

`pointCount`(현 디자인 잔재)는 `itemCount`와 의미가 겹친다. **`pointCount`는 `itemCount`로 통일** — `pointCount` 읽는 곳을 `itemCount`로 교체하고 필드 제거. (읽는 곳: `RunController.ActivateDeliveryPoints`, `EndRun`의 시간초과 비례보상, `OrderBoard.ApplyHoverState`, `OrderCardUI`.)

> **호환 주의**: `itemCount`를 직렬화 필드 → 프로퍼티로 바꾸면 기존 .asset의 itemCount 값은 무시된다. 읽는 곳은 전부 읽기 전용(PackingScreen 큐브 스폰, OrderCardUI 표시, 채널5 capacity 게이팅)이라 동작은 유지된다. 복구 스크립트가 모든 잡의 `destinationLabels`를 다시 채운다.

### DeliveryManager 변경

"앞에서부터 N개" → "지정된 순서 있는 경로". 마스터 리스트 `points`(8개)는 그대로, **활성 경로** 개념을 추가:

```csharp
readonly List<DeliveryPoint> route = new List<DeliveryPoint>(); // 플레이어가 정한 순서

/// <summary>label로 DeliveryPoint 찾기 (OrderJob.destinationLabels 해석용).</summary>
public DeliveryPoint FindByLabel(string label);

/// <summary>경로 계획 화면이 정한 순서대로 활성화. route[0]만 active, 나머지 visible-inactive.</summary>
public void ActivateRoute(List<DeliveryPoint> orderedRoute);
```

- `ActivateSubset(n)`은 **폴백 전용으로 유지** (OrderBoard 없는 씬 / FallbackJob). 내부적으로 `points` 앞 N개로 `route`를 채운 뒤 `ActivateRoute` 호출하도록 리팩터.
- `currentIndex`, `CurrentTarget`, `OnDeliveryReached`, `Finish`는 `points` 대신 `route`를 인덱싱하도록 변경. 순차 배달 로직(현재 목표만 active)은 그대로 — 순서가 `route` 순서일 뿐.
- `TotalCount` = `route.Count`.

### 경로 핸드오프

경로 계획 화면 → DeliveryManager로 경로를 넘기는 길:

**RoutePlanScreen이 확정 시 `Services.Delivery.ActivateRoute(orderedList)`를 직접 호출**한다. 그 다음 PackingScreen을 연다. `RunController.BeginRun`의 `ActivateDeliveryPoints`는 이미 세팅된 경로를 건드리지 않는다(경로가 비어있으면 = 폴백 → `ActivateSubset(job.itemCount)`).

이렇게 하면 `BeginRun`/`PackingScreen` 시그니처를 안 바꿔도 된다.

```
RunController.ActivateDeliveryPoints(job):
    if (Delivery.HasPlannedRoute) { /* RoutePlanScreen이 이미 ActivateRoute 함 — 그대로 */ }
    else Delivery.ActivateSubset(job.itemCount);   // 폴백
```

`HasPlannedRoute` = `route.Count > 0 && !finished`. 런 종료 시 `route` 클리어.

---

## 조각 2 — OrderBoard 개편 (맵 중심, 레이아웃 A)

### 레이아웃

- **맵이 메인**: OverviewCamera 뷰가 화면 대부분. (이미 OrderBoard 동안 오버뷰 카메라가 켜져 있음 — 추가 작업 없음.)
- **카드 = 우측 얇은 세로 스트립**: `cardsContainer` RectTransform을 우측으로, 폭 축소(예: 화면폭의 ~22%). OrderCardUI 자체는 그대로 쓰되 스트립 폭에 맞게.
- **차고(시작점) 마커**: `Race` 위치 (42,0.17,36)에 마커 1개 표시. OrderBoard가 Open 시 생성/표시, Close 시 숨김.

### 마커 표시 — 잡들을 동시에

지금은 호버한 잡 1개만 마커가 보인다. 개편 후:

- OrderBoard Open 시, **제시된 모든 잡의 목적지를 동시에** 색깔별로 표시. 색 = `job.color` (잡별로 이미 다름).
- 한 배달지점이 여러 잡에 쓰일 수 있음 → 그 경우 마커는 호버 중인 잡 색을 우선, 아니면 "여러 잡" 중립색 또는 가장 가까운 잡 색. **단순화**: Open 시엔 전부 중립 회색, **호버 시 그 잡의 목적지만 job.color로 강조 + 나머지 dim**. (지금 `ApplyHoverState` 로직을 "i < pointCount" → "destinationLabels에 포함" 으로 교체하면 거의 그대로.)
- 호버 시: 그 잡의 목적지 + **차고에서 목적지들로 잇는 선** (LineRenderer 또는 단순 라인). 거리·분포가 한눈에.

### DeliveryPointMarker 변경

- `ApplyHoverState`가 label 기반으로 동작하도록 — OrderBoard가 `job.destinationLabels`로 어느 마커가 그 잡 것인지 판단.
- 마커에 label 노출 필요 → `DeliveryPointMarker`가 자기 `DeliveryPoint.Label`을 알고 있음(이미 RequireComponent). OrderBoard가 label↔marker 매핑.

### 상호작용

- 카드 호버 → 위 강조. 카드 클릭 → **경로 계획 화면**으로 (`Services.Packing.Show` 아님). 조각 3 참조.
- 채널 5 잠금(capacity < itemCount)은 그대로 — 잠긴 카드는 클릭 무시.

### 구현 범위

- `OrderBoard.cs`: `ApplyHoverState` label 기반으로, 차고 마커 생성/표시, 호버 시 라인 그리기, 클릭 → 경로 계획 화면.
- 씬: `cardsContainer` RectTransform 우측 스트립으로 (복구 스크립트로 조정 — asset.md 패턴).
- `DeliveryPointMarker.cs`: 필요 시 label getter / 라인 연결점 노출.

---

## 조각 3 — 경로 계획 화면 (신규 `RoutePlanScreen`)

### 역할

잡을 고른 후, 그 잡의 목적지들을 맵에서 보고 **배달 순서**를 정한다.

### 흐름

```
OrderBoard 카드 클릭
  → RoutePlanScreen.Show(job)         ← 신규
  → 플레이어가 순서 지정 → "확정"
  → Services.Delivery.ActivateRoute(orderedList)
  → Services.Packing.Show(job)         ← 기존 포장 화면
  → ... → RunController.BeginRun(job, packedItems)
```

### 화면 구성

- **맵**: OverviewCamera 뷰 그대로 (OrderBoard와 같은 "맵"). RoutePlanScreen은 자체 ScreenSpaceOverlay 캔버스 + 오버뷰 카메라 활용.
- **목적지 마커**: 이 잡의 destinationLabels에 해당하는 DeliveryPoint들만 `job.color`로 강조, 나머지 dim. 차고 마커도 표시.
- **순서 지정 — 클릭 시퀀스**: 플레이어가 맵의 목적지 마커를 **배달하고 싶은 순서대로 클릭**. 클릭하면 그 마커에 순번(1,2,3) 배지가 붙고, 차고→1→2→3 경로선이 그려진다. 이미 클릭한 마커를 다시 클릭하면 그 마커부터 뒤를 취소.
  - 마커 클릭 = 카메라에서 ray → DeliveryPoint collider (트렁크 배치 드래그와 같은 패턴, `Physics.SyncTransforms` 주의).
- **우측 패널**: 순서 리스트 (1. WestHub … ), 총 직선 거리, "추천 순서" 버튼, "재설정" 버튼, "확정" 버튼.
  - **추천 순서**: 차고에서 시작하는 최근접 이웃(nearest-neighbor) 순서를 자동 채움. "게임이 도와주는" 부분 — 플레이어는 받아들이거나 다시 짤 수 있음.
  - **확정**: 모든 목적지가 순서에 들어갔을 때만 활성. 누르면 `ActivateRoute` → PackingScreen.
- 물건이 1개면 순서 결정이 trivial → 목적지 1곳 자동 선택 + "확정"만. (화면은 뜨되 즉시 확정 가능.)

### 거리 표시

직선 거리 합 (차고→1→2→…). pathfinding 없음. `Vector3.Distance` XZ 평면. 숫자는 "약 Nm" 또는 상대 막대. **단순 숫자(미터)로 충분.**

### 컴포넌트

- 신규 `RoutePlanScreen.cs` — `IRoutePlanScreen` 인터페이스 + `Services.RoutePlan` 등록 (Core.cs / GameContext).
  - `PackingScreen`의 자체 캔버스 구축 패턴을 참고 (BuildUIIfNeeded 류).
- 신규 `RoutePlanScreenInput.cs` 또는 RoutePlanScreen 내부에서 마커 클릭 처리 (PackingScreenInput 패턴).
- `DeliveryPointMarker`: 순번 배지 표시 메서드 (`SetRouteOrder(int n)` / `ClearRouteOrder()`).

### 폴백

`Services.RoutePlan`이 없거나 OrderBoard 없는 씬 → 기존처럼 OrderBoard 클릭이 바로 PackingScreen, RunController가 `ActivateSubset` 폴백.

---

## 흐름 통합

```
[차고/시작]
  → [주문 보드]      ← 조각 2: 맵 중심, 잡 목적지 색깔별
  → [경로 계획]      ← 조각 3: 신규, 배달 순서 지정
  → [포장]           ← 기존 PackingScreen (변경 없음)
  → [출발/주행]      ← RunController, DeliveryManager가 plannedRoute 순서로
  → [정산]
```

서비스 등록 (`Core.cs` `Services`): `IRoutePlanScreen RoutePlan` 추가. `GameContext`가 와이어링.

---

## 에러 처리 / 엣지

- **destinationLabels에 씬에 없는 label**: `FindByLabel`이 null 반환 → 그 목적지 스킵 + 경고 로그. 경로가 비면 폴백.
- **OrderJob.destinationLabels 비어있음**: itemCount=1로 간주, 폴백 경로(ActivateSubset(1)).
- **RoutePlanScreen에서 확정 안 하고 뒤로**: 데모 범위에선 뒤로가기 없음 — 확정만. (취소가 필요하면 후속.)
- **물건 수 > 적재함 capacity**: 채널 5 게이팅이 OrderBoard에서 이미 막음 — RoutePlanScreen은 capacity 통과한 잡만 받음.
- **DeliveryManager.route가 런 중 비었을 때**: `CurrentTarget` null 가드 (이미 있음).

## 테스트 (unity-cli, Editor 켠 채 — `Application.runInBackground=true` 필수)

1. **데이터 모델**: 복구 스크립트 실행 후 잡들의 `destinationLabels`가 채워졌는지, `itemCount`가 길이와 일치하는지 exec로 확인.
2. **DeliveryManager**: `ActivateRoute([A,B,C])` 후 `CurrentTarget==A`, A 배달 시뮬 → `CurrentTarget==B` 순으로 진행되는지.
3. **OrderBoard**: Play → 보드 열림 → 맵 메인 + 우측 카드 스트립 + 차고 마커. 카드 호버 → 그 잡 목적지만 강조 + 라인. ScreenCapture 스크린샷.
4. **RoutePlanScreen**: 카드 클릭 → 경로 계획 화면. 마커 클릭 순서대로 순번 배지 + 경로선. "추천 순서" 동작. "확정" → PackingScreen 뜨는지.
5. **풀 흐름**: 보드 → 경로(3목적지 잡) → 포장 → 출발 → DeliveryManager가 정한 순서대로 CurrentTarget 진행 → 정산.

## 복구 스크립트 (asset.md 패턴)

`Channel8RouteSetup.cs` ([MenuItem]): 9개 잡(Pool 8 + Order_C)에 `destinationLabels` 지정. 잡마다 씬의 8개 배달지점 중 1~3개를 **서로 다르게** 배분 (B안 — 진짜 다른 목적지). 멱등.

`itemCount`가 `destinationLabels.Count`로 파생되므로, **현재 각 잡의 itemCount를 그대로 유지하도록** 그 수만큼 label을 준다:
- Order_05_StandardBox: 2개 (채널 5에서 itemCount=2로 설정됨) → 서로 다른 2곳
- Order_C_Risky("대량 배송"): 3개 → 흩어진 3곳
- 나머지 Pool 잡 7개: 각 1개 → 서로 다른 단일 목적지 (7개 잡에 가능한 한 1:1)

label은 씬 DeliveryPoint 이름 사용: DowntownEntry / FactoryNorth / EastHub / LowerWest / SuburbanGate / CentralCross / WestHub / PlateauTop.

## 미해결 (도현 깨면 확인)

- 경로 계획 = **클릭 시퀀스**로 잡음 (드래그 리스트 대신). 맵 중심이라 마커 직접 클릭이 자연스러움 — 별로면 드래그 리스트로 전환.
- "추천 순서"는 최근접 이웃. 더 똑똑한 게 필요하면 후속.
- `pointCount` 필드 제거 = D-070 잔재 정리. 의도와 맞는지 확인.
