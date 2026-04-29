# DeliveryManager.Update

**스크립트:** [[DeliveryManager]]
**파일:** `Assets/Scripts/DeliveryManager.cs:Update()`

---

## 호출되는 곳 (incoming)

- Unity Engine (매 프레임)

---

## 호출하는 것 (outgoing)

- 가드: `if (!returning || finished || startReturn == null) return;`
- `carCache` 사용 (Awake에서 캐시됨) — null이면 fallback `FindActiveCar()` 1회 (cycle 1 Task Y, f960949)
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
    // simple distance check to return zone
    if (carCache == null) carCache = FindActiveCar();   // late spawn / respawn fallback
    var car = carCache;
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

`carCache`는 [[DeliveryManager.Awake]]에서 1회 채움. Update는 정상 경로에서 캐시만 사용.

---

## ✅ 캐시 적용됨 (cycle 1 Task Y, f960949)

`FindFirstObjectByType<ArcadeCarController>()`는 씬 전체 스캔 → 매 프레임 비싼 호출이었음.
Awake 캐시(carCache) 도입으로 정상 spawn 순서에서는 매 프레임 scene scan 0건.

late spawn (DM Awake → Car Awake) / respawn (Car destroy) 시에는 Update의 `carCache == null` 분기가 fallback으로 `FindActiveCar()`를 호출해 새로 캐시. Unity의 `==/!=` 오버로드가 destroyed Transform을 null로 인식하므로 stale reference 위험 없음.

검증 walk-through: cycle 2 programmer 리포트 시나리오 A~D 참조.

---

## 관련 흐름

- [[Flow.DeliveryComplete]] — 마지막 점 도달 후 returning → Finish
