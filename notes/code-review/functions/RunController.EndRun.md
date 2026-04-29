# RunController.EndRun

**스크립트:** [[RunController]]
**파일:** `Assets/Scripts/Order/RunController.cs:EndRun(bool success)`
**private**

---

## 호출되는 곳 (incoming)

- `OnAllCompleted()` (DeliveryManager 이벤트 콜백) → success: true
- [[RunController.Update]] (시간초과) → success: false

---

## 호출하는 것 (outgoing)

- `cargo.IntegrityPercent` 캡처 → LastIntegrityPercent
- 정적 helper `ComputeIntegrityMultiplier(percent)`
- `UpgradeSystem.AddReward(reward)` (reward > 0 시)
- `overviewCam.GoToOverview()`
- `resultPanel.Show(this)`

---

## 무엇을 하나

런 종료. 보상 계산 + 정산 화면 띄움.

---

## 코드 핵심

```csharp
void EndRun(bool success) {
    RunActive = false;
    LastRunSuccess = success;
    LastElapsed = Elapsed;
    LastIntegrityPercent = cargo != null ? cargo.IntegrityPercent : 1f;

    float integrityMul = ComputeIntegrityMultiplier(LastIntegrityPercent);
    int reward;
    if (success) {
        reward = CurrentJob.baseReward + (CurrentJob.timeLimitSec > 0f ? CurrentJob.timeBonus : 0);
    } else {
        // 시간 초과: 완료 비례 보상 (절반 가중)
        float ratio = CurrentJob.pointCount > 0
            ? (float)CompletedCount / CurrentJob.pointCount : 0f;
        reward = Mathf.RoundToInt(CurrentJob.baseReward * ratio * 0.5f);
    }
    reward = Mathf.RoundToInt(reward * integrityMul);
    LastReward = reward;

    if (UpgradeSystem.Instance != null && reward > 0)
        UpgradeSystem.Instance.AddReward(reward);

    Time.timeScale = 0f;
    if (overviewCam != null) overviewCam.GoToOverview();
    if (resultPanel != null) resultPanel.Show(this);
}
```

---

## 보상 계산 식

```
success:  base + (timeBonus if 시간제한 있음)
실패:     base × (CompletedCount / pointCount) × 0.5
공통:     × integrityMul (5단계: 100~80=1.0, 80~50=0.8, 50~20=0.5, 20~1=0.2, 0=0)
```

---

## 관련 흐름

- [[Flow.DeliveryComplete]] — 정상 종료
- [[Flow.TimeOver]] — 시간 초과
