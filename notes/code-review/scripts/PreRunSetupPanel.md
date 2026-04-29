# PreRunSetupPanel

**파일:** `Assets/Scripts/Order/PreRunSetupPanel.cs`
**부착 위치:** RunRoot GameObject (세션 16 후속 추가)
**역할:** 출발 전 화물·포장 선택 모달 — procedural Canvas → prefab 패턴 리팩터

---

## 인스펙터 입력

- **runController** ([[RunController]])
- **panelRoot** (GameObject) — `UIRoot/PreRunSetupRoot`
- **titleText** / **cargoText** / **cargoTipText** / **totalCostText** (TMP_Text)
- **cardsParent** (RectTransform with HorizontalLayoutGroup) — 4 카드 부모
- **packingCardPrefab** (GameObject) — `Assets/Prefabs/UI/PackingCard.prefab`. [[PackingCardUI]] 컴포넌트
- **confirmBtn** / **cancelBtn** (Button)
- **confirmText** (TMP_Text) — 출발 버튼 라벨 ("출발 (뽁뽁이)" 형식)

---

## 함수

### public API
- [[PreRunSetupPanel.Show]] — 모달 띄우기 + 카드 1회 인스턴스화
- [[PreRunSetupPanel.Hide]] — 비활성

### Unity 이벤트
- `Awake()` — runController 자동 찾기, EventSystem 보장, panelRoot 비활성, 버튼 와이어링

### 카드 콜백
- [[PreRunSetupPanel.OnCardClicked]] ← [[PackingCardUI.button]] onClick
  - selectedTier = tier; Refresh()

### 버튼 콜백
- [[PreRunSetupPanel.OnConfirmClicked]] ← confirmBtn.onClick
  - Hide() + [[RunController.BeginRun]] 오버로드 호출 ★ 버그 fix
- [[PreRunSetupPanel.OnCancelClicked]] ← cancelBtn.onClick
  - Hide() + [[OrderBoard.Open]]

### 내부 helper
- `SpawnCardsIfNeeded()` — packingCardPrefab × 4 (None/Bubble/Foam/Hardcase) 인스턴스화 + [[PackingCardUI.Bind]]. **1회만**
- `Refresh()` — 화물 텍스트 + 카드 SetSelected + 합계/출발 라벨 갱신
- `WireButtons()` — confirmBtn/cancelBtn onClick 1회 와이어링
- `EnsureEventSystem()` (정적) — InputSystemUIInputModule 보장

---

## 협력 객체

- [[OrderBoard]] — 취소 시 Open 호출
- [[PackingCardUI]] — 4장 인스턴스화 + 콜백
- [[RunController]] — 출발 시 BeginRun(job, tier)

---

## 관련 흐름

- [[Flow.GameStart]] — Show 호출, OnConfirmClicked로 진행
- [[Flow.Cancel]] — OnCancelClicked

---

## 핵심 사항

- **카드 인스턴스 영구 보존** — `cardsSpawned` flag로 첫 Show만 인스턴스화. Hide/Show 반복해도 destroy 안 함
- **selectedTier 초기화** — `Show(job)`에서 `job.defaultPacking ?? Bubble`
- **버그 fix** — OnConfirmClicked가 [[RunController.BeginRun]] 오버로드(`BeginRun(job, tier)`) 호출. 이전엔 `cargo.SetPacking(selectedTier)` 후 BeginRun이 덮어쓰던 문제 해결
