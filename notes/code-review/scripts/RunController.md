# RunController

**파일:** `Assets/Scripts/Order/RunController.cs`
**부착 위치:** RunRoot GameObject
**역할:** 한 런의 lifecycle 관리 — 시간 추적, 보상 계산, ResultPanel 트리거

---

## 인스펙터 입력

- **deliveryManager** ([[DeliveryManager]])
- **overviewCam** (OverviewCamera)
- **orderBoard** ([[OrderBoard]])
- **resultPanel** (ResultPanel)
- **fuel** (FuelSystem)
- **cargo** (CargoBox)

---

## public 상태

- `CurrentJob` (OrderJob)
- `Elapsed` (float, 초)
- `RunActive` (bool)
- `CompletedCount` (int)
- `LastRunSuccess`, `LastReward`, `LastElapsed`, `LastIntegrityPercent`

---

## 함수

### public API
- [[RunController.BeginRun]] — 1-인자 (그냥 null 넘김)
- [[RunController.BeginRun]] (2-인자) ★ 세션 16 후속 — packingOverride 받음
- [[RunController.RestartFromBoard]] — ResultPanel "다시" 버튼

### 정적 helper
- `ComputeIntegrityMultiplier(percent)` — 5단계 (1.0/0.8/0.5/0.2/0)
- `IntegrityGrade(percent)` — "완벽" / "약간 손상" / ...

### Unity 이벤트
- `Awake()` — 누락된 reference 자동 찾기 (FindFirstObjectByType)
- `Start()` — [[RunController.OpenBoard]]
- [[RunController.Update]] — Elapsed 누적 + 시간초과 체크 → EndRun(false)

### 이벤트 콜백 (DeliveryManager 구독)
- `OnDeliveryCompleted(point)` — CompletedCount++
- `OnAllCompleted()` — [[RunController.EndRun]](true)

### 내부 helper
- [[RunController.OpenBoard]] — timeScale=0, OverviewCam GoToOverview, [[OrderBoard.Open]]
- [[RunController.EndRun]] — 보상 계산, UpgradeSystem.AddReward, ResultPanel.Show
- `MakeFallbackJob()` (정적) — OrderBoard 없을 때 디폴트

---

## 협력 객체

- [[OrderBoard]] — Open / Close
- ResultPanel — Show
- [[DeliveryManager]] — ActivateSubset + OnDeliveryCompleted/OnAllCompleted UnityEvent 구독
- CargoBox — SetCargoType, SetPacking
- UpgradeSystem — AddReward
- FuelSystem — Refill
- OverviewCamera — GoToOverview / GoToFollow

---

## 관련 흐름

- [[Flow.GameStart]] — OpenBoard / BeginRun
- [[Flow.DeliveryComplete]] — OnAllCompleted → EndRun
- [[Flow.TimeOver]] — Update 시간초과 → EndRun

---

## 핵심 사항

- **Update 책임 분리** — [[RunController.Update]] = 시간 추적, [[DeliveryManager.Update]] = 거리 검사. 중복 X, race X
- **BeginRun 오버로드** — 세션 16 후속 추가. PreRunSetupPanel 흐름의 selectedTier 보존을 위한 fix
- **보상 계산 흐름** — `success ? base + bonus : base × ratio × 0.5` → `× integrityMul (5단계)`
