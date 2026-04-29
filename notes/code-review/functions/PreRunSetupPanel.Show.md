# PreRunSetupPanel.Show

**스크립트:** [[PreRunSetupPanel]]
**파일:** `Assets/Scripts/Order/PreRunSetupPanel.cs:Show(OrderJob)`

---

## 호출되는 곳 (incoming)

- [[OrderBoard.OnCardClicked]] — 사용자 카드 클릭

---

## 호출하는 것 (outgoing)

- `WireButtons()` — confirm/cancel 버튼 onClick 1회 와이어링
- `SpawnCardsIfNeeded()` — packingCardPrefab × 4 인스턴스 + [[PackingCardUI.Bind]]
- `panelRoot.SetActive(true)`
- `Refresh()` — 화물 텍스트, 카드 SetSelected, 합계/출발 라벨

---

## 무엇을 하나

포장 선택 모달 활성. **첫 호출 시 카드 4장 인스턴스화** (이후 재사용).

---

## 코드 핵심

```csharp
public void Show(OrderJob job) {
    currentJob = job;
    isOpen = true;
    WireButtons();
    SpawnCardsIfNeeded();

    selectedTier = job != null && System.Enum.IsDefined(typeof(PackingTier), (int)job.defaultPacking)
        ? job.defaultPacking
        : PackingTier.Bubble;

    if (panelRoot != null) panelRoot.SetActive(true);
    Refresh();
}
```

---

## SpawnCardsIfNeeded (참고)

```csharp
void SpawnCardsIfNeeded() {
    if (cardsSpawned) return;     // 1회만 인스턴스화
    for (int i = 0; i < kPackingOrder.Length; i++) {
        var go = Instantiate(packingCardPrefab, cardsParent);
        var card = go.GetComponent<PackingCardUI>();
        card.Bind(kPackingOrder[i], OnCardClicked);
        spawnedCards.Add(card);
    }
    cardsSpawned = true;
}
```

---

## 관련 흐름

- [[Flow.GameStart]] — 핵심 진입
