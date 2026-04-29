# OrderBoard.OnCardHoverEnter / OnCardHoverExit

**스크립트:** [[OrderBoard]]
**파일:** `Assets/Scripts/Order/OrderBoard.cs:OnCardHoverEnter/Exit(int)`

---

## 호출되는 곳 (incoming)

- [[OrderBoard.NotifyHoverEnter]] → OnCardHoverEnter
- [[OrderBoard.NotifyHoverExit]] → OnCardHoverExit

---

## 호출하는 것 (outgoing)

- 모든 spawnedCards에 [[OrderCardUI.SetHovering]](i == cardIndex)
- `ApplyHoverState()` — 마커들에 SetColor / SetDimmed

---

## 무엇을 하나

호버한 카드만 비주얼 강조 + **그 주문에 해당하는 배달지 마커만 색상 표시**, 나머지는 회색·디밍.

---

## 코드 핵심

```csharp
void OnCardHoverEnter(int cardIndex) {
    if (!isOpen) return;
    hoveredIndex = cardIndex;
    for (int i = 0; i < spawnedCards.Count; i++)
        spawnedCards[i]?.SetHovering(i == cardIndex);
    ApplyHoverState();
}

void OnCardHoverExit(int cardIndex) {
    if (!isOpen) return;
    if (hoveredIndex == cardIndex) {
        hoveredIndex = -1;
        for (int i = 0; i < spawnedCards.Count; i++)
            spawnedCards[i]?.SetHovering(false);
        ApplyHoverState();
    }
}
```

---

## ApplyHoverState 동작 (참고)

```csharp
void ApplyHoverState() {
    for (int i = 0; i < allMarkers.Count; i++) {
        if (hoveredIndex < 0) {
            // 호버 안 함 → 가장 큰 주문 색깔로 fallback
            var fallback = GetLargestJob();
            allMarkers[i].SetColor(fallback?.color ?? Color.white);
            allMarkers[i].SetDimmed(false);
        } else {
            var job = currentOffer[hoveredIndex];
            bool inSet = i < job.pointCount;  // 앞에서부터 N개만 inSet
            allMarkers[i].SetColor(inSet ? job.color : Color.gray);
            allMarkers[i].SetDimmed(!inSet);
        }
    }
}
```

---

## 관련 흐름

- [[Flow.HoverPropagation]]

---

## 주의

- **Exit는 hoveredIndex 일치 검사** — 빠르게 옆 카드로 이동 시 Exit가 늦게 와도 다음 Enter가 이긴 상태 보존
