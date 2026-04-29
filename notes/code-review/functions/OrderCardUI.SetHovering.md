# OrderCardUI.SetHovering

**스크립트:** [[OrderCardUI]]
**파일:** `Assets/Scripts/Order/OrderCardUI.cs:SetHovering(bool)`

---

## 호출되는 곳 (incoming)

- [[OrderBoard.OnCardHoverEnter]] (호버한 카드만 true, 나머지 false)
- [[OrderBoard.OnCardHoverExit]] (모든 카드 false)

---

## 호출하는 것 (outgoing)

- bg.color, hoverStrip.color 변경만

---

## 무엇을 하나

호버 비주얼 토글. 데이터 변경 X, 색만.

---

## 코드 핵심

```csharp
public void SetHovering(bool hovering) {
    if (bg != null) bg.color = hovering ? hoverBgColor : baseBgColor;
    if (hoverStrip != null && job != null) {
        var c = job.color;
        c.a = hovering ? hoverStripAlpha : 0f;
        hoverStrip.color = c;
    }
}
```

---

## 관련 흐름

- [[Flow.HoverPropagation]]
