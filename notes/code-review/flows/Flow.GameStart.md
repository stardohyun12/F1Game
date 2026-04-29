# Flow: 게임 시작 → 출발 (정상)

가장 친숙한 흐름. 사용자가 게임 시작부터 출발 버튼까지 거치는 함수 사슬.

---

## 시퀀스

```mermaid
sequenceDiagram
    autonumber
    participant U as 사용자
    participant RC as RunController
    participant OB as OrderBoard
    participant OC as OrderCardUI
    participant PSP as PreRunSetupPanel
    participant PC as PackingCardUI
    participant Cargo as CargoBox
    participant DM as DeliveryManager

    U->>RC: 게임 시작
    RC->>RC: Start() → OpenBoard()
    RC->>OB: Open()
    OB->>OB: PickOffer + CacheMarkers
    OB->>OC: Instantiate × 3 + Bind(idx, job, OnCardClicked)
    Note right of OB: panelRoot 활성

    U->>OC: 주문 카드 클릭
    OC->>OB: onCardClicked(idx)
    OB->>OB: lastSelectedJob 저장
    Note right of OB: panelRoot 비활성
    OB->>PSP: Show(job)
    PSP->>PSP: SpawnCardsIfNeeded
    PSP->>PC: Instantiate × 4 + Bind(tier, OnCardClicked)
    Note right of PSP: panelRoot 활성, Refresh

    U->>PC: 포장 카드 클릭 (Foam)
    PC->>PSP: onClicked(Foam)
    PSP->>PSP: selectedTier = Foam; Refresh()

    U->>PSP: 출발 버튼 클릭
    PSP->>PSP: OnConfirmClicked()
    PSP->>PSP: Hide()
    PSP->>RC: BeginRun(job, Foam) ★
    RC->>DM: ActivateSubset(job.pointCount)
    RC->>Cargo: SetCargoType + SetPacking(Foam)
    RC->>OB: Close()
    Note over RC: timeScale = 1, 운전 시작
```

---

## 거치는 함수 (클릭하며 깊게)

1. [[RunController.OpenBoard]]
2. [[OrderBoard.Open]] → [[OrderCardUI.Bind]] (× 3)
3. [[OrderCardUI.OnPointerClick]] → [[OrderBoard.OnCardClicked]]
4. [[PreRunSetupPanel.Show]] → [[PreRunSetupPanel.SpawnCardsIfNeeded]] → [[PackingCardUI.Bind]] (× 4)
5. [[PackingCardUI.Bind]] (button.onClick) → [[PreRunSetupPanel.OnCardClicked]]
6. [[PreRunSetupPanel.OnConfirmClicked]] ★ → [[RunController.BeginRun]] ★ → [[OrderBoard.Close]]

---

## 핵심 결정 지점

- **15번 (PSP → RC.BeginRun(job, tier))**: selectedTier가 cargo까지 도달. 세션 16 후속 버그 fix.
- **16번 (RC.SetPacking)**: `cargo.SetPacking(packingOverride ?? job.defaultPacking)` — 사용자 선택 보존.

자세히: [[../code-review-questions]] "해결됨" 섹션의 BeginRun 버그 fix 설명.

---

## 한 줄 답

사용자 클릭 → 출발 함수 사슬:
```
OC.OnPointerClick → OB.OnCardClicked → PSP.Show
→ (포장 카드 클릭) PC.button → PSP.OnCardClicked
→ (출발 버튼) PSP.OnConfirmClicked → RC.BeginRun(job, tier)
```
