# OrderBoard.Close

**스크립트:** [[OrderBoard]]
**파일:** `Assets/Scripts/Order/OrderBoard.cs:Close()`

---

## 호출되는 곳 (incoming)

- [[RunController.BeginRun]] (오버로드) — 출발 시

---

## 호출하는 것 (outgoing)

- 카드 모두 `Destroy(c.gameObject)`
- `panelRoot.SetActive(false)`
- 마커 자동 상태로 (`SetExternalControl(false)`)

---

## 무엇을 하나

보드 닫기. 카드 destroy해서 메모리 해제, 패널 비활성, **마커는 그대로 표시**하되 자동 색상 모드 (lastSelectedJob.color).

---

## 코드 핵심

```csharp
public void Close() {
    isOpen = false;
    if (panelRoot != null) panelRoot.SetActive(false);
    foreach (var c in spawnedCards) if (c != null) Destroy(c.gameObject);
    spawnedCards.Clear();
    // 런 중에도 마커는 보임 — lastSelectedJob에 해당하는 점만
    if (allMarkers != null && lastSelectedJob != null) {
        for (int i = 0; i < allMarkers.Count; i++) {
            bool inSet = i < lastSelectedJob.pointCount;
            allMarkers[i].SetVisible(inSet);
            if (inSet) {
                allMarkers[i].SetColor(lastSelectedJob.color);
                allMarkers[i].SetExternalControl(false); // 자동 모드
            }
        }
    }
}
```

---

## 관련 흐름

- [[Flow.GameStart]] — BeginRun이 Close 호출

---

## 주의

- 카드는 destroy. panelRoot은 deactivate만 (재사용).
- 마커 visibility는 lastSelectedJob 기준 (선택한 주문의 점만).
