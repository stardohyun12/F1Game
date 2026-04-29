# OrderBoard

**파일:** `Assets/Scripts/Order/OrderBoard.cs`
**부착 위치:** RunRoot GameObject
**역할:** 주문 보드 orchestrator — IMGUI에서 Canvas/uGUI로 리팩터됨 (세션 16 후속 A)

---

## 인스펙터 입력

- **catalog** (OrderCatalog) — 주문 풀
- **runController** ([[RunController]]) — fallback BeginRun 호출용
- **panelRoot** (GameObject) — `UIRoot/OrderBoardPanel`. 활성/비활성 토글
- **cardsContainer** (RectTransform) — 카드 instantiate 부모. VerticalLayoutGroup
- **cardPrefab** (GameObject) — `Assets/Prefabs/UI/OrderCard.prefab`. [[OrderCardUI]] 컴포넌트
- **headerText** / **helpText** (TMP_Text, optional)
- **offerCount** (int = 3) — 매번 셔플로 노출할 카드 수

---

## 함수

### public API
- [[OrderBoard.Open]] — 보드 띄우기
- [[OrderBoard.Close]] — 보드 닫기

### 정적 (CardUI → Board 통지)
- [[OrderBoard.NotifyHoverEnter]] ← [[OrderCardUI.OnPointerEnter]] 호출
- [[OrderBoard.NotifyHoverExit]] ← [[OrderCardUI.OnPointerExit]] 호출

### Unity 이벤트
- `Awake()` — `_hoverTarget = this`, panelRoot 비활성, 헤더/도움말 set
- `OnDestroy()` — `_hoverTarget` cleanup

### 카드 콜백 (private)
- [[OrderBoard.OnCardClicked]] — 카드 클릭 → [[PreRunSetupPanel.Show]]
- [[OrderBoard.OnCardHoverEnter]] / [[OrderBoard.OnCardHoverExit]]

### 내부 helper
- `PickOffer()` — catalog.jobs에서 N개 셔플
- `SpawnCards()` — cardPrefab × N 인스턴스 + [[OrderCardUI.Bind]]
- `CacheMarkers()` — 씬 DeliveryPoint 모두 수집
- `ApplyHoverState()` — 마커들에 SetColor/SetDimmed
- `GetLargestJob()` — 가장 큰 주문 (호버 안 할 때 default)

---

## 협력 객체

- [[OrderCardUI]] — prefab 통해 카드 인스턴스화 + 호버/클릭 콜백
- [[PreRunSetupPanel]] — 카드 클릭 시 Show
- [[RunController]] — fallback (PSP 없을 때 직접 BeginRun)
- DeliveryPointMarker — 마커 색깔/디밍 (호버 시)

---

## 관련 흐름

- [[Flow.GameStart]] — Open 호출
- [[Flow.HoverPropagation]] — 호버 전파
- [[Flow.Cancel]] — PSP 취소 후 재진입

---

## 주의 사항

- **정적 `_hoverTarget`** 사용 — 단일 인스턴스 가정. 멀티 OrderBoard 씬은 비표준
- **카드 lifecycle** — Open마다 destroy + spawn (재사용 X)
- **panelRoot 토글** — destroy 안 함, SetActive만
