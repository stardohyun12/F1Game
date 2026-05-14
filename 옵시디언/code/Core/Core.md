---
script: F1/Assets/Scripts/Core/Core.cs
category: core
phase_applied: [phase1]
tags: [code/core, code/wiring]
linecount: 222
signature: composite — 12 interfaces + Services + GameContext
last_synced: 2026-05-12
---

# Core

> 한 줄: 시스템들이 서로 모르고도 만나는 약속의 장소. **계약(인터페이스 12) + 명부(Services) + 결혼식(GameContext)**.

## 책임 (Single Responsibility)
- **계약 정의** — 12개 시스템 인터페이스 (`IFuelSystem`, `ICargoBox`, `IRunController` ...)
- **레지스트리** — `Services` static class. `Services.Cargo`, `Services.Run` 등 정적 접근.
- **부트스트랩** — `GameContext` MonoBehaviour. `Awake`에서 SerializeField로 받은 8개 시스템을 Services에 등록 + 누락은 AutoFindMissing fallback.

세 책임이 한 파일에 묶인 이유는 응집 — 인터페이스 추가 시 Services 한 줄 + GameContext 한 줄 동시에 보임. AssetDatabase 부분 import 회피용 단일화 (세션 18 결정).

## 의존 (들어오는 — 누가 나를 부르는가)
- **거의 모든 시스템과 HUD**. 27 파일이 `Services.X`로 접근.
- [[RunController]] — `Services.Delivery`, `Services.Result`, `Services.Cargo`, `Services.Fuel`, `Services.Upgrade`.
- [[AudioManager]] — Phase 4 이후 AudioCueBus 경유 (Services 직접 참조 0).
- 7 HUD — `Services.Run`, `Services.Cargo`, `Services.Fuel`.

## 의존 (나가는 — 내가 부르는 것)
- 없음 — Core는 모든 의존의 *목적지*.
- 단, `GameContext.AutoFindMissing`은 `FindFirstObjectByType<T>()` fallback (씬에 누락 시 1회).

## 관련 결정
- [[notes/decisions-log#D-016]] 하네스 엔지니어링
- 세션 18 결정: 인터페이스 12개를 12 파일 분리 X, 단일 Core.cs 유지.

## 관련 학습
- [[phase1-dip-service-locator]] — 본 파일의 *왜*. Find 36→0의 전환점.
- [[📚 학습 지도]] — Service Locator 패턴 항목.

## 핫스팟 (변경 가능성 높음)
- **새 시스템 추가 시**: `IXxxSystem` 인터페이스 + `Services.Xxx` 필드 + `GameContext` SerializeField + 등록 한 줄. 4곳이지만 한 파일이라 검색 쉬움.
- **Phase 7 예정 변경**: `IRunController`에 `IRunSummary LastSummary` + `RestartFromBoard()` 추가. `IUpgradeSystem`에 `TryPurchase`, `OnWalletChanged`, `OnUpgradePurchased` 추가. `IResultPanel.Show(RunController)` → `Show(IRunSummary)`.

## 노트
- 게임 프로그래밍 관점에서 **"Core 폴더 = 잡동사니 서랍 안티패턴"** 주의. PitCrew의 Core는 plumbing(배관)만이고 gameplay 0 → 깔끔. 새 파일을 `Core/`에 던질지 결정할 때 "공통 *기반*인가, 그 시스템 *자체*인가?" 묻기.
- "한 파일 한 타입" 컨벤션과 어긋남. 응집 우선이라 OK이지만 면접/오픈소스 기여 시 지적 받을 수 있음을 알아둘 것.

**냬 정리**
core 파일의 역할은 게임이 진행되는데 중요한 클래스들을 생성해야한다 알려주고 관리하는 역할을 하고 독립성을 유지해주는 스크립트

***배운점***
	1.interface는 함수의 입출력과 어느정도의 구조를 잡아주는 역할을 함 
	2.core파일같이 중요한 클래스와 함수를 구조화 하고 관리하는 함수가 있으면 관리하기 매우 편해질듯 함.