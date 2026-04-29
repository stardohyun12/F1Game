# DeliveryManager.ActivateSubset

**스크립트:** [[DeliveryManager]]
**파일:** `Assets/Scripts/DeliveryManager.cs:ActivateSubset(int n)`

---

## 호출되는 곳 (incoming)

- [[RunController.BeginRun]]

---

## 호출하는 것 (outgoing)

- `points[i].Bind(this, label)` × N
- `points[i].gameObject.SetActive(used)` (used = i < N)
- `points[i].SetActive(used && i == 0)` (첫 번째만 활성)

---

## 무엇을 하나

배달 지점 풀에서 **앞에서부터 N개**만 활성. 나머지는 비활성. **첫 번째 점만 SetActive(true)** (현재 타겟).

---

## 코드 핵심

```csharp
public void ActivateSubset(int n) {
    activeCount = Mathf.Clamp(n, 0, points.Count);
    currentIndex = 0;
    returning = false;
    finished = false;

    for (int i = 0; i < points.Count; i++) {
        if (points[i] == null) continue;
        points[i].Bind(this, points[i].Label);
        bool used = i < activeCount;
        points[i].gameObject.SetActive(used);
        points[i].SetActive(used && i == 0);
    }
}
```

---

## 관련 흐름

- [[Flow.GameStart]] — BeginRun → ActivateSubset

---

## 주의

- **shuffle 안 함** — 항상 앞에서부터 N개. 시드 기반 랜덤은 별도 구현 필요
- `Bind`은 매번 호출 (런 진행 상태 reset)
