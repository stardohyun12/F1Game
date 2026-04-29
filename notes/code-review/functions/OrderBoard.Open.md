# OrderBoard.Open

**스크립트:** [[OrderBoard]]
**파일:** `Assets/Scripts/Order/OrderBoard.cs:Open()`

---

## 호출되는 곳 (incoming)

- [[RunController.OpenBoard]] — 게임 시작 시
- [[RunController.RestartFromBoard]] — ResultPanel "다시" 버튼
- [[PreRunSetupPanel.OnCancelClicked]] — 취소 시 OrderBoard로 복귀

---

## 호출하는 것 (outgoing)

- `PickOffer()` — catalog.jobs에서 N개 셔플
- `CacheMarkers()` — 씬 DeliveryPoint 모두 수집
- `SpawnCards()` — cardPrefab × N 인스턴스화 + [[OrderCardUI.Bind]]
- `panelRoot.SetActive(true)`
- `ApplyHoverState()` — 마커들에 SetColor/SetDimmed

---

## 무엇을 하나

보드 열기. 카드 N장 새로 만들고, 마커 색깔 강조하고, 패널 활성. **매번 새 셔플** (PickOffer 호출).

---

## 코드 핵심

```csharp
public void Open() {
    isOpen = true;
    hoveredIndex = -1;
    PickOffer();              // 풀에서 N개 셔플
    CacheMarkers();           // 씬 마커 수집
    if (allMarkers != null)
        foreach (var m in allMarkers) m?.SetExternalControl(true);
    SpawnCards();             // 카드 인스턴스화
    if (panelRoot != null) panelRoot.SetActive(true);
    ApplyHoverState();        // 마커 색상 갱신
}
```

---

## 관련 흐름

- [[Flow.GameStart]] — 첫 진입
- [[Flow.Cancel]] — PSP 취소 후 재호출
