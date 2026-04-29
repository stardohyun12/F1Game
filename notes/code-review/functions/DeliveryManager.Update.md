# DeliveryManager.Update

**스크립트:** [[DeliveryManager]]
**파일:** `Assets/Scripts/DeliveryManager.cs:Update()`

---

## 호출되는 곳 (incoming)

- Unity Engine (매 프레임)

---

## 호출하는 것 (outgoing)

- 가드: `if (!returning || finished) return;`
- `FindActiveCar()` — ⚠ 매 프레임 FindFirstObjectByType (개선 후보)
- `Vector3.Distance(car, startReturn)`
- 4m 이내면 [[DeliveryManager.Finish]]

---

## 무엇을 하나

**복귀 모드(returning) 시 차가 startReturn에 4m 이내** 도달했는지 검사.
[[RunController.Update]]와 책임 분리: RC = 시간, DM = 거리.

---

## 코드 핵심

```csharp
void Update() {
    if (!returning || finished || startReturn == null) return;
    var car = FindActiveCar();     // ⚠ 매 프레임 FindFirstObjectByType
    if (car == null) return;
    if (Vector3.Distance(car.position, startReturn.position) < 4f) {
        Finish();
    }
}

Transform FindActiveCar() {
    var car = GameObject.FindFirstObjectByType<ArcadeCarController>();
    return car != null ? car.transform : null;
}
```

---

## ⚠ 개선 후보 — FindActiveCar 캐시

`FindFirstObjectByType<ArcadeCarController>()`는 씬 전체 스캔 → 매 프레임 비싼 호출.
Awake에서 1회 캐시 권장.

```csharp
Transform carCache;
void Awake() {
    var c = FindFirstObjectByType<ArcadeCarController>();
    if (c != null) carCache = c.transform;
}
void Update() {
    if (!returning || finished || startReturn == null) return;
    if (carCache == null) carCache = FindActiveCar();
    if (carCache != null && Vector3.Distance(carCache.position, startReturn.position) < 4f)
        Finish();
}
```

자세한 의문은 [[../code-review-questions]] 참고.

---

## 관련 흐름

- [[Flow.DeliveryComplete]] — 마지막 점 도달 후 returning → Finish
