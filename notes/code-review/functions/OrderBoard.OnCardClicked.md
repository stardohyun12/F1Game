# OrderBoard.OnCardClicked

**스크립트:** [[OrderBoard]]
**파일:** `Assets/Scripts/Order/OrderBoard.cs:OnCardClicked(int)`

---

## 호출되는 곳 (incoming)

- [[OrderCardUI.OnPointerClick]] — Bind에서 등록한 콜백

---

## 호출하는 것 (outgoing)

- `lastSelectedJob = currentOffer[idx]` 저장
- `panelRoot.SetActive(false)` — 보드 즉시 닫음
- 마커들 `SetExternalControl(false)` — 자동 모드
- **[[PreRunSetupPanel.Show]]** (FindFirstObjectByType, 있으면)
- 또는 [[RunController.BeginRun]] (PSP 없을 때 fallback)

---

## 무엇을 하나

카드 클릭 → 보드 닫고 **포장 선택 모달로 전환**.

---

## 코드 핵심

```csharp
void OnCardClicked(int cardIndex) {
    if (!isOpen) return;
    if (cardIndex < 0 || cardIndex >= currentOffer.Count) return;
    lastSelectedJob = currentOffer[cardIndex];

    isOpen = false;
    if (panelRoot != null) panelRoot.SetActive(false);
    if (allMarkers != null && lastSelectedJob != null)
        for (int i = 0; i < allMarkers.Count; i++)
            allMarkers[i]?.SetExternalControl(false);

    var setup = FindFirstObjectByType<PreRunSetupPanel>();
    if (setup != null) setup.Show(lastSelectedJob);
    else if (runController != null) runController.BeginRun(lastSelectedJob);
}
```

---

## 관련 흐름

- [[Flow.GameStart]] — 핵심 분기점

---

## 주의

- **PSP fallback** — FindFirstObjectByType는 비싸지만 호출 빈도 낮음 (런당 1회)
- **보드 닫음과 마커 자동 모드 전환은 PSP.Show 전에** — 사용자가 보드 카드 떠나는 시점이 명확
