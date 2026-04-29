# RunController.BeginRun ★

**스크립트:** [[RunController]]
**파일:** `Assets/Scripts/Order/RunController.cs:BeginRun(OrderJob, PackingTier?)`

오버로드 두 개:
- `BeginRun(OrderJob job)` — 1-인자, 그냥 `BeginRun(job, null)` 호출
- `BeginRun(OrderJob job, PackingTier? packingOverride)` ★ 세션 16 후속 신규

---

## 호출되는 곳 (incoming)

### 1-인자 버전
- [[OrderBoard.OnCardClicked]] (PSP fallback)
- [[RunController.OpenBoard]] (orderBoard 없을 때 fallback)

### 2-인자 버전 ★
- [[PreRunSetupPanel.OnConfirmClicked]] — selectedTier 전달

---

## 호출하는 것 (outgoing)

- [[DeliveryManager.ActivateSubset]](job.pointCount)
- `fuel.Refill()`
- `cargo.SetCargoType(job.cargoType)` → ApplyPreset 자동
- `cargo.SetPacking(packingOverride ?? job.defaultPacking)` ★ 버그 fix
- `shakeSim.SetBoxColor(BoxColor(cargoType))`
- `overviewCam.GoToFollow(... timeScale = 1)`
- [[OrderBoard.Close]]
- DeliveryManager 이벤트 구독 (`OnDeliveryCompleted`, `OnAllCompleted`)

---

## 무엇을 하나

런 시작. 모든 시스템에 시작 신호 전파.

---

## 코드 핵심

```csharp
public void BeginRun(OrderJob job) {
    BeginRun(job, null);
}

public void BeginRun(OrderJob job, PackingTier? packingOverride) {
    CurrentJob = job;
    Elapsed = 0f;
    CompletedCount = 0;
    RunActive = true;

    if (deliveryManager != null) deliveryManager.ActivateSubset(job.pointCount);
    if (fuel != null) fuel.Refill();
    if (cargo != null) {
        cargo.SetCargoType(job.cargoType);
        cargo.SetPacking(packingOverride ?? job.defaultPacking);  // ★ 핵심
    }
    var shakeSim = FindFirstObjectByType<CargoShakeSimulator>();
    if (shakeSim != null) shakeSim.SetBoxColor(CargoTypePresets.BoxColor(job.cargoType));

    if (overviewCam != null) overviewCam.GoToFollow(() => Time.timeScale = 1f);
    if (orderBoard != null) orderBoard.Close();

    if (deliveryManager != null) {
        deliveryManager.OnDeliveryCompleted.RemoveListener(OnDeliveryCompleted);
        deliveryManager.OnAllCompleted.RemoveListener(OnAllCompleted);
        deliveryManager.OnDeliveryCompleted.AddListener(OnDeliveryCompleted);
        deliveryManager.OnAllCompleted.AddListener(OnAllCompleted);
    }
}
```

---

## ★ 핵심: packingOverride 패턴

`cargo.SetCargoType` 호출 시 `ApplyPreset()` 자동 실행 → packing 필드를 default로 reset함.
**그 후** `cargo.SetPacking(packingOverride ?? job.defaultPacking)` 호출해서 사용자 선택 우선.

이전 버그: PSP가 BeginRun **이전에** SetPacking 호출 → BeginRun이 ApplyPreset+default로 덮어씀.
수정: BeginRun **안에서** override 처리.

---

## 관련 흐름

- [[Flow.GameStart]] — 핵심 종착점 (출발)
