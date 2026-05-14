---
script: F1/Assets/Scripts/Order/RunController.cs
category: order
phase_applied: [phase1, phase6]
tags: [code/order, code/loop-spine]
linecount: 235
signature: class RunController
last_synced: 2026-05-12
---

# RunController

> 한 줄: 런의 시작·진행·종료를 관장하는 **게임 척추**. OrderBoard 클릭부터 ResultPanel까지의 흐름이 여기에.

## 책임 (Single Responsibility)
- **런 생명주기** — `Start` → `OpenBoard` → `BeginRun(job)` → `Update` (시간 체크) → `EndRun` → `ResultPanel.Show`.
- **BeginRun 분해** (2026-05-11 커밋 3fd3fbb) — 7 명명 메서드로 SRP 분해: `InitRunState`, `ActivateDeliveryPoints`, `ApplyCargoSetup`, `UpdateShakeSimColor`, `StartFollowCameraAndCloseBoard`, `SubscribeDeliveryEvents`.
- **보상 계산** — `EndRun`에서 `baseReward + timeBonus + integrityMul`. 정책이 컨트롤러에 박혀 있음(Phase 8 RewardCalculator로 분리 예정).
- **integrity 등급** — `ComputeIntegrityMultiplier`, `IntegrityGrade` (5단계). ResultPanel.BuildStory와 중복 (Phase 8에서 SO화).

## 의존 (들어오는 — 누가 나를 부르는가)
- [[Core]] — `Services.Run`으로 등록.
- [[CountdownHUD]] — `controller.TimeRemaining`, `RunActive`.
- [[OrderBoard]] — `RunController.BeginRun(job)` 호출.
- [[ResultPanel]] — `controller.CurrentJob`, `LastIntegrityPercent`, `LastReward` 등 **구상 캐스팅으로 접근 중** (A7 누수).
- [[PreRunSetupPanel]] — `Services.Run as RunController`.

## 의존 (나가는 — 내가 부르는 것)
- [[DeliveryManager]] — `OnDeliveryCompleted` 이벤트 구독.
- [[OrderBoard]] — `OpenBoard` / `Close`.
- [[ResultPanel]] — `Show(this)` 호출 (Phase 7에서 `Show(IRunSummary)`로 변경 예정).
- [[FuelSystem]], [[CargoBox]], [[CargoShakeSimulator]] — BeginRun 헬퍼에서 초기화.
- [[FollowCamera]] — 카메라 시작.

## 관련 결정
- [[notes/decisions-log#D-051]] F1 복귀 서사 — 한 런의 보상이 메타 진척으로.
- [[notes/decisions-log#D-053]] 결정 채널 9 — 각 dial이 한 런 안에 어떻게 묶이는지.

## 관련 학습
- [[phase1-dip-service-locator]] — Find 6개 제거.
- [[phase6-peer-review-patches]] — A7 IRunController DTO 결정 대기.
- 커밋 `3fd3fbb` — BeginRun 분해 (SRP).

## 핫스팟 (변경 가능성 높음)
- **Phase 7 (다음)**: `LastSummary` 필드 + `IRunSummary` 캡처. `Services.Result.Show(LastSummary)`로 호출.
- **Phase 8**: `EndRun`의 보상 공식 + `IntegrityGrade` → `RewardCalculator` POCO + `CargoGradeBook` SO.
- **현재 누수**: 호출자들이 `IRunController`로는 부족해서 `as RunController` 캐스팅 중 (CountdownHUD, OrderBoard, PreRunSetupPanel, ResultPanel).
