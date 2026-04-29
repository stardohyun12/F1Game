# RunController.Update

**스크립트:** [[RunController]]
**파일:** `Assets/Scripts/Order/RunController.cs:Update()`
**Unity 이벤트** — 매 프레임 (RunActive 시에만 의미 있는 작업)

---

## 호출되는 곳 (incoming)

- Unity Engine (매 프레임)

---

## 호출하는 것 (outgoing)

- `Elapsed += deltaTime`
- 시간 초과 시 [[RunController.EndRun]](false)

---

## 무엇을 하나

**런 시간 추적 + 시간 제한 체크.** [[DeliveryManager.Update]]와 책임 분리됨 (DM = 거리 검사).

---

## 코드 핵심

```csharp
void Update() {
    if (!RunActive) return;
    Elapsed += Time.deltaTime;

    if (CurrentJob != null && CurrentJob.timeLimitSec > 0f && Elapsed >= CurrentJob.timeLimitSec) {
        EndRun(success: false);
    }
}
```

---

## 관련 흐름

- [[Flow.TimeOver]] — 시간 초과 시 EndRun(false)
- (정상 흐름은 DeliveryManager.OnAllCompleted → EndRun(true) — Update 무관)

---

## 주의

- **Update 두 개 분리** — RC = 시간, DM = 거리. 중복 X, race X
- `if (!RunActive) return;` 가드 — 런 비활성 시 매 프레임 no-op
