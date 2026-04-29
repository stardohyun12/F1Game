# PackingCardUI.Bind

**스크립트:** [[PackingCardUI]]
**파일:** `Assets/Scripts/Order/PackingCardUI.cs:Bind(PackingTier, Action<PackingTier>)`

---

## 호출되는 곳 (incoming)

- [[PreRunSetupPanel.Show]] → SpawnCardsIfNeeded → Instantiate → Bind

---

## 호출하는 것 (outgoing)

- 텍스트 set (nameText, multiplierText, descText, costText)
- colorStrip.color = TierColor(tier)
- button.onClick 1회 와이어링 (`wired` flag)

---

## 무엇을 하나

카드 인스턴스에 1회 데이터 set + 클릭 콜백 등록.
이후 [[PackingCardUI.SetSelected]]만 호출됨 (다시 Bind 안 함).

---

## 코드 핵심

```csharp
public void Bind(PackingTier tier, Action<PackingTier> onClicked) {
    this.tier = tier;
    this.onClicked = onClicked;

    if (nameText != null) nameText.text = PackingTierHelper.DisplayName(tier);
    if (multiplierText != null) multiplierText.text = $"×{PackingTierHelper.ThresholdMultiplier(tier):F1}";
    if (descText != null) descText.text = PackingTierGuide(tier);
    if (costText != null) {
        int cost = PackingTierHelper.Cost(tier);
        costText.text = cost == 0 ? "무료" : $"₩{cost:N0}";
    }
    if (colorStrip != null) colorStrip.color = TierColor(tier);

    if (button != null && !wired) {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => this.onClicked?.Invoke(this.tier));
        wired = true;
    }
}
```

---

## 관련 흐름

- [[Flow.GameStart]] — 4 카드 인스턴스화
