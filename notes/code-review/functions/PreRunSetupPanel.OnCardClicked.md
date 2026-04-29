# PreRunSetupPanel.OnCardClicked

**스크립트:** [[PreRunSetupPanel]]
**파일:** `Assets/Scripts/Order/PreRunSetupPanel.cs:OnCardClicked(PackingTier)`

---

## 호출되는 곳 (incoming)

- [[PackingCardUI.Bind]]에서 등록한 button.onClick 람다

---

## 호출하는 것 (outgoing)

- `selectedTier = tier`
- `Refresh()` — 카드 SetSelected, 합계 텍스트, 출발 버튼 라벨 갱신

---

## 무엇을 하나

포장 카드 클릭 → 선택 변경. 출발 X (별도 버튼).

---

## 코드 핵심

```csharp
void OnCardClicked(PackingTier tier) {
    selectedTier = tier;
    Refresh();
}

void Refresh() {
    // 카드 강조
    for (int i = 0; i < spawnedCards.Count; i++)
        spawnedCards[i]?.SetSelected(spawnedCards[i].Tier == selectedTier);

    // 합계 + 출발 라벨
    int packCost = PackingTierHelper.Cost(selectedTier);
    int reward = currentJob != null ? currentJob.baseReward : 0;
    if (totalCostText != null)
        totalCostText.text = packCost > 0
            ? $"포장비 ₩{packCost:N0}  /  예상 보상 ₩{reward:N0}"
            : $"무료 포장  /  예상 보상 ₩{reward:N0}";
    if (confirmText != null)
        confirmText.text = $"출발 ({PackingTierHelper.DisplayName(selectedTier)})";
}
```

---

## 관련 흐름

- [[Flow.GameStart]] — 포장 선택 단계
