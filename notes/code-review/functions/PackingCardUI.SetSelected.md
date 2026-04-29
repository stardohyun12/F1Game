# PackingCardUI.SetSelected

**스크립트:** [[PackingCardUI]]
**파일:** `Assets/Scripts/Order/PackingCardUI.cs:SetSelected(bool)`

---

## 호출되는 곳 (incoming)

- [[PreRunSetupPanel.OnCardClicked]] → Refresh → 모든 카드 SetSelected(card.Tier == selectedTier)

---

## 호출하는 것 (outgoing)

- bg.color 변경만

---

## 무엇을 하나

선택 비주얼 토글. 데이터 변경 X.

---

## 코드 핵심

```csharp
public void SetSelected(bool selected) {
    if (bg != null) bg.color = selected ? selectedColor : normalColor;
}
```

---

## 관련 흐름

- [[Flow.GameStart]] — 사용자 카드 클릭 후
