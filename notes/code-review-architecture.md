# 코드 아키텍처 마인드맵 — 세션 16 후속(A) 리팩터

> **읽는 법:** Markmap (markmap.js.org 또는 VSCode "Markmap" 확장)에 붙여넣으면 인터랙티브 트리.
> 일반 마크다운으로도 읽힘. 들여쓰기 = 부모/자식.
>
> **단위 규칙:** 스크립트가 외곽 노드, public 메서드 + Unity 이벤트 핸들러가 자식. private helper는 노트로만 표시.
> **→ 표기 = 다른 스크립트 호출 (cross-script 화살표).**

---

## OrderBoard 흐름 (주문 선택)

### OrderBoard.cs (RunRoot 컴포넌트)
- 입력 (인스펙터)
  - catalog (OrderCatalog)
  - runController (RunController)
  - panelRoot (GameObject) — UIRoot/OrderBoardPanel
  - cardsContainer (RectTransform) — VerticalLayoutGroup
  - cardPrefab (GameObject) — OrderCard.prefab
  - headerText, helpText (TMP_Text, optional)
  - offerCount (int = 3)
- 정적 API (CardUI → Board 통지용)
  - `NotifyHoverEnter(int cardIndex)` ← OrderCardUI.OnPointerEnter 호출
  - `NotifyHoverExit(int cardIndex)` ← OrderCardUI.OnPointerExit 호출
- public API
  - `Open()` — 호출자: RunController.OpenBoard, RestartFromBoard
    - PickOffer (catalog에서 N개 셔플)
    - CacheMarkers (씬의 DeliveryPoint 모두 수집)
    - SpawnCards — Instantiate(cardPrefab) × N → **OrderCardUI.Bind(idx, job, OnCardClicked)**
    - panelRoot 활성, ApplyHoverState
  - `Close()` — 호출자: RunController.BeginRun
    - 카드 destroy
    - panelRoot 비활성
    - 마커 자동 상태 모드 복귀
- Unity 이벤트
  - `Awake()` — _hoverTarget = this, panelRoot 비활성, 헤더/도움말 텍스트 set
  - `OnDestroy()` — _hoverTarget cleanup
- 카드 콜백 (private, 흐름 핵심)
  - `OnCardClicked(int)` — 호출 출처: OrderCardUI.OnPointerClick
    - lastSelectedJob = currentOffer[idx]
    - panelRoot 비활성
    - **PreRunSetupPanel.Show(job)** (없으면 RunController.BeginRun fallback)
  - `OnCardHoverEnter/Exit(int)`
    - 카드들에 SetHovering 분배
    - ApplyHoverState — 마커들에 SetColor/SetDimmed

### OrderCardUI.cs (OrderCard.prefab 컴포넌트)
- 입력 (인스펙터, prefab에 와이어링)
  - bg (Image), colorStrip (Image), hoverStrip (Image)
  - titleText (TMP_Text), tipText (TMP_Text)
  - statRows[6] (OrderCardStatRow{label, value})
  - hoverAlpha, baseBgColor, hoverBgColor, hoverStripAlpha (시각 튠)
- public API
  - `Bind(cardIndex, job, onCardClicked)` — OrderBoard.SpawnCards가 호출
    - 1회 텍스트 set (title/지점/시간/보상/무게/화물/포장/tip)
    - colorStrip.color = job.color
  - `SetHovering(bool)` — OrderBoard.OnCardHoverEnter/Exit가 호출
    - bg/hoverStrip 색만 변경
- Unity 이벤트 (Pointer)
  - `OnPointerEnter` → **OrderBoard.NotifyHoverEnter(cardIndex)**
  - `OnPointerExit` → **OrderBoard.NotifyHoverExit(cardIndex)**
  - `OnPointerClick` → onCardClicked 콜백(OrderBoard에서 등록한 것)

---

## PreRunSetupPanel 흐름 (포장 선택)

### PreRunSetupPanel.cs (RunRoot 컴포넌트)
- 입력 (인스펙터)
  - runController
  - panelRoot — UIRoot/PreRunSetupRoot
  - titleText, cargoText, cargoTipText, totalCostText (TMP_Text)
  - cardsParent (RectTransform with HorizontalLayoutGroup)
  - packingCardPrefab — PackingCard.prefab
  - confirmBtn, confirmText, cancelBtn
- public API
  - `Show(job)` — 호출자: OrderBoard.OnCardClicked
    - SpawnCardsIfNeeded (4 카드 1회 인스턴스화 + Bind)
    - selectedTier 초기화 (job.defaultPacking 또는 Bubble)
    - panelRoot 활성, Refresh
  - `Hide()` — 호출자: OnConfirmClicked, OnCancelClicked, 외부
    - panelRoot 비활성
- Unity 이벤트
  - `Awake()` — runController 자동 찾기, EventSystem 보장, panelRoot 비활성, WireButtons
- 카드 콜백 (private, 흐름 핵심)
  - `OnCardClicked(tier)` — PackingCardUI.button.onClick에서 호출
    - selectedTier = tier; Refresh()
- 버튼 콜백
  - `OnConfirmClicked()` — confirmBtn.onClick
    - Hide()
    - **RunController.BeginRun(currentJob, selectedTier)** ★ 버그 fix 핵심
  - `OnCancelClicked()` — cancelBtn.onClick
    - Hide()
    - **OrderBoard.Open()** (다시 주문 선택으로)

### PackingCardUI.cs (PackingCard.prefab 컴포넌트)
- 입력 (인스펙터, prefab에 와이어링)
  - bg (Image), colorStrip (Image)
  - nameText, multiplierText, descText, costText (TMP_Text)
  - button (Button)
  - selectedColor, normalColor (시각 튠)
- public API
  - `Bind(tier, onClicked)` — PreRunSetupPanel.SpawnCardsIfNeeded가 호출
    - 1회 텍스트 set + button.onClick 와이어링
  - `SetSelected(bool)` — PreRunSetupPanel.Refresh가 호출
    - bg.color만 변경
- 정적 helper (UI 디자이너 영역)
  - `TierColor(tier)` — 색 결정
  - `PackingTierGuide(tier)` — desc 텍스트

---

## 런 lifecycle (시간 + 보상)

### RunController.cs (RunRoot 컴포넌트)
- 입력 (인스펙터)
  - deliveryManager, overviewCam, orderBoard, resultPanel, fuel, cargo
- public state
  - CurrentJob, Elapsed, RunActive, CompletedCount
  - LastRunSuccess, LastReward, LastElapsed, LastIntegrityPercent
- public API
  - `BeginRun(OrderJob job)` — 1-인자 버전, 그냥 BeginRun(job, null) 호출
  - `BeginRun(OrderJob job, PackingTier? packingOverride)` ★ Phase 2 신규
    - DeliveryManager.ActivateSubset(job.pointCount)
    - Fuel.Refill
    - cargo.SetCargoType(job.cargoType) → ApplyPreset
    - **cargo.SetPacking(packingOverride ?? job.defaultPacking)** ★ 버그 fix
    - shakeSim.SetBoxColor
    - overviewCam.GoToFollow → timeScale 1
    - **OrderBoard.Close()**
    - DeliveryManager 이벤트 구독 (OnDeliveryCompleted/OnAllCompleted)
  - `RestartFromBoard()` — ResultPanel "다시" 버튼이 호출
    - resultPanel.Hide()
    - OpenBoard() → OrderBoard.Open()
- Unity 이벤트
  - `Start()` — OpenBoard() 호출
  - `Update()` — Elapsed += deltaTime + 시간 초과 체크 → EndRun(false)
- 이벤트 콜백
  - `OnDeliveryCompleted(point)` ← DeliveryManager.OnDeliveryCompleted UnityEvent
    - CompletedCount++
  - `OnAllCompleted()` ← DeliveryManager.OnAllCompleted UnityEvent
    - EndRun(true)
- private
  - `OpenBoard()` — timeScale=0, overviewCam GoToOverview, **OrderBoard.Open**
  - `EndRun(success)` — 보상 계산, **UpgradeSystem.AddReward**, timeScale=0, **ResultPanel.Show(this)**

### DeliveryManager.cs (DeliverySystem/DeliveryManager 컴포넌트)
- 입력 (인스펙터)
  - points (List<DeliveryPoint>)
  - startReturn (Transform)
- 이벤트 (외부 구독)
  - OnAllCompleted (UnityEvent)
  - OnDeliveryCompleted (UnityEvent<DeliveryPoint>)
- public state
  - CurrentIndex, TotalCount, Returning, Finished
  - CurrentTarget, ReturnTarget
- public API
  - `ActivateSubset(int n)` — 호출자: RunController.BeginRun
    - 앞에서부터 N개만 활성, 첫 번째 SetActive(true)
  - `OnDeliveryReached(point)` — 호출자: DeliveryPoint (트리거 진입 시)
    - point.SetCompleted, **OnDeliveryCompleted Invoke**
    - 다음 점 활성 또는 returning 모드
- Unity 이벤트
  - `Start()` — activeCount 미설정 시 전체 활성
  - `Update()` — returning 상태에서만, 차가 startReturn 4m 이내면 Finish
- private
  - `Finish()` — finished=true, **OnAllCompleted Invoke**
  - `FindActiveCar()` ⚠ 매 프레임 FindFirstObjectByType (캐시 권장)

---

## 이벤트 그래프 (한 눈)

```
DeliveryPoint (트리거)
  → DeliveryManager.OnDeliveryReached
    → OnDeliveryCompleted UnityEvent
      → RunController.OnDeliveryCompleted (CompletedCount++)
    → OnAllCompleted UnityEvent (마지막 도착 시)
      → RunController.OnAllCompleted → EndRun(true)
        → ResultPanel.Show

OrderCardUI.OnPointerClick
  → OrderBoard.OnCardClicked
    → PreRunSetupPanel.Show

PackingCardUI.button.onClick
  → PreRunSetupPanel.OnCardClicked (selectedTier 변경)

confirmBtn.onClick
  → PreRunSetupPanel.OnConfirmClicked
    → RunController.BeginRun(job, tier) ★ 오버로드

cancelBtn.onClick
  → PreRunSetupPanel.OnCancelClicked
    → OrderBoard.Open
```

---

## 전체 구조 요약 (한 단락)

**RunRoot** GameObject 하나가 4개 MonoBehaviour를 들고 있음 (OrderBoard, RunController, ResultPanel, PreRunSetupPanel — 세션 16 후속 추가).
**UIRoot** Canvas 자식에 패널 두 개 (OrderBoardPanel, PreRunSetupRoot) — 토글로 전환.
**DeliveryManager**는 별도 GameObject (DeliverySystem 하위).
카드는 두 종류 prefab (`OrderCard.prefab`, `PackingCard.prefab`)에서 런타임에 인스턴스화.
스크립트끼리는 **UnityEvent + 정적 콜백**으로 느슨하게 결합. 직접 참조는 인스펙터 와이어링으로만.
