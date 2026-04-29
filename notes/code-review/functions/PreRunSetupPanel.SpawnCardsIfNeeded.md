# PreRunSetupPanel.SpawnCardsIfNeeded

**스크립트:** [[PreRunSetupPanel]]
**파일:** `Assets/Scripts/Order/PreRunSetupPanel.cs:SpawnCardsIfNeeded()`
**private** — Show가 호출

---

## 호출되는 곳 (incoming)

- [[PreRunSetupPanel.Show]]

---

## 호출하는 것 (outgoing)

- `Instantiate(packingCardPrefab, cardsParent)` × 4
- [[PackingCardUI.Bind]] (각 인스턴스에 tier + OnCardClicked 콜백 등록)

---

## 무엇을 하나

**1회만** 카드 4장 (None/Bubble/Foam/Hardcase) 인스턴스화. `cardsSpawned` flag로 멱등성 보장.

---

## 코드 핵심

```csharp
static readonly PackingTier[] kPackingOrder = {
    PackingTier.None, PackingTier.Bubble, PackingTier.Foam, PackingTier.Hardcase
};

void SpawnCardsIfNeeded() {
    if (cardsSpawned) return;
    if (packingCardPrefab == null || cardsParent == null) {
        Debug.LogWarning("[PreRunSetupPanel] 와이어링 누락");
        return;
    }
    spawnedCards.Clear();
    for (int i = 0; i < kPackingOrder.Length; i++) {
        var go = Instantiate(packingCardPrefab, cardsParent);
        var card = go.GetComponent<PackingCardUI>();
        if (card == null) { Destroy(go); continue; }
        card.Bind(kPackingOrder[i], OnCardClicked);
        spawnedCards.Add(card);
    }
    cardsSpawned = true;
}
```

---

## 트레이드오프

- **장점:** Hide/Show 반복 시 instantiate 비용 0
- **단점:** prefab 디자인 변경 후 Play 재시작 필요 (런타임 인스턴스는 이전 prefab 기반)
- 현재 사용 흐름 (Show 자주 X) 에서는 장점이 큼

---

## 관련 흐름

- [[Flow.GameStart]]
