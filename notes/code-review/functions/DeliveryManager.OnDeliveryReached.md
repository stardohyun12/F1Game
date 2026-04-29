# DeliveryManager.OnDeliveryReached

**스크립트:** [[DeliveryManager]]
**파일:** `Assets/Scripts/DeliveryManager.cs:OnDeliveryReached(DeliveryPoint)`

---

## 호출되는 곳 (incoming)

- DeliveryPoint — 트리거 진입 시

---

## 호출하는 것 (outgoing)

- `point.SetCompleted()`
- **OnDeliveryCompleted UnityEvent Invoke** → [[RunController]] 구독자 (`CompletedCount++`)
- `currentIndex++`
- 다음 점 `SetActive(true)` (있으면) 또는 `Finish()` (마지막이면)

---

## 무엇을 하나

배달지 도착 처리. 다음 점 활성 또는 returning 모드 진입.

---

## 코드 핵심

```csharp
public void OnDeliveryReached(DeliveryPoint p) {
    if (finished || returning) return;
    if (p != points[currentIndex]) return;     // 잘못된 점 체크

    p.SetCompleted();
    OnDeliveryCompleted?.Invoke(p);
    currentIndex++;

    int limit = activeCount > 0 ? activeCount : points.Count;
    if (currentIndex >= limit) {
        // 마지막 점 도달
        returning = (startReturn != null);
        if (!returning) Finish();
    } else {
        points[currentIndex].SetActive(true);  // 다음 활성
    }
}
```

---

## 관련 흐름

- [[Flow.DeliveryComplete]]

---

## 주의

- **순서 강제** — currentIndex와 일치하지 않으면 무시 (점 N→N-1로 점프 방지)
- **returning 분기** — startReturn 있으면 복귀 모드 (Update가 거리 검사), 없으면 즉시 Finish
