# Team A — Architect 감사 (2026-05-11)

> SOLID / 결합 / 응집 / 패턴 일관성 관점. 팀 B(성능/가독성/네이밍)와 독립.
> Phase 1~5(DIP·Template Method·Modifier Chain·AudioCueBus·TireTypePresets)와 이전 리뷰의 Tier 2-B/C는 잘된 점으로만 기재, 재발견 X.

---

## 1. 감사 대상

본 파일(스캔 + 정독, 총 18개):
- Core: `Core/Core.cs`
- 런 흐름: `Order/RunController.cs`, `Order/ResultPanel.cs`, `Order/OrderBoard.cs`, `Order/OrderCardUI.cs`, `Order/PreRunSetupPanel.cs`
- 시스템: `Cargo/CargoBox.cs`, `Cargo/Modifiers/UpgradeModifiers.cs`, `Upgrades/UpgradeSystem.cs`, `Tires/TireSystem.cs`, `DeliveryManager.cs`
- 오디오: `Audio/AudioManager.cs`, `Audio/AudioCueBus.cs`
- HUD: `UI/BarHUDBase.cs`, `UI/SpeedometerHUD.cs`, `UI/DeliveryProgressHUD.cs`, `GearHUD.cs`, `Order/CountdownHUD.cs`

의도적 제외:
- `ArcadeCarController.cs`, `DriftVfxController.cs`, `EngineAudio.cs`, `FollowCamera.cs`, `CarVisualLean.cs`: 차량 물리·시각. 팀 B(성능) 영역.
- `Layout/*`, `Route/NodeGraph.cs`, `Traffic/*`: 데이터/맵 빌드. 이번 감사 사이클 밖 (MVP scope 외 폴리싱 후보).
- `Cargo/CargoShakeSimulator.cs`, `CargoView.cs`, `CargoType.cs`: 시각·enum 정의. SOLID 이슈 적음.

---

## 2. 발견 (우선순위 정렬)

### A1. ResultPanel = God Class · UI 빌더 + 데이터 바인딩 + 정책(스토리/등급) 혼재 — SRP 정면 위반
**위치:** `F1/Assets/Scripts/Order/ResultPanel.cs:17-499`
**현상:** 한 클래스가 (a) `EnsureBuilt`/`BuildUpgradeCards`에서 1200~1600px UGUI 트리 procedural 생성, (b) `Refresh`에서 controller·Services에서 데이터 끌어와 바인딩, (c) `BuildStory` / `FormatTimeWithBonus`에서 화물 등급별 카피 문구·시간 포맷 정책, (d) `OnUpgradeClick`에서 `UpgradeSystem.Instance.TryPurchase` 트랜잭션까지 직접 호출.
```
public void Show(RunController ctrl) { controller = ctrl; ... EnsureBuilt(); Refresh(); }
void EnsureBuilt() { /* 220줄: GameObject 생성·anchor·sizeDelta·sprite */ }
void Refresh()    { /* 110줄: text 바인딩 + 색 + 카드 상태 */ }
static string BuildStory(OrderJob job, float integrityPercent) { /* 등급별 카피 6종 */ }
void OnUpgradeClick(int idx) { ups.TryPurchase(def); Refresh(); }
```
**문제:** 변경 사유가 4개로 분기 — 레이아웃 픽셀 튜닝(도현 #2-4 등 빈번), 카피 톤, 업그레이드 데이터 모델, 트랜잭션 정책. 어느 한 줄을 만져도 다른 셋이 같이 재컴파일·재테스트 대상이 됨. 또 `EnsureEventSystem` 같은 정적 부트스트랩까지 들어와 있어 응집도가 더 떨어진다.
**제안 패턴:** **MVP(Model-View-Presenter) + Builder 분리.**
- `ResultPanelView` (MonoBehaviour, 빌드만 — `EnsureBuilt`/`BuildUpgradeCards` + 카드 RT/Image/TMP 노출). 도현이 향후 prefab로 옮길 때 자연 분리선.
- `ResultPresenter` (POCO, `IResultPanel` 구현, `RunController`·`Services` 의존). View에 데이터 push, View는 Services를 모름.
- `ResultStoryBook` (정적 또는 SO, `BuildStory`/`IntegrityGrade` 카피 테이블). 카피 변경이 다른 코드 안 건드림.
- 업그레이드 구매는 Presenter→`Services.Upgrade.TryPurchase` (인터페이스에 `TryPurchase` 추가). `UpgradeSystem.Instance` 직접 접근 제거 (Phase 1 일관성).
**비용/이득:** 6~10h. 카피 변경 시 컴파일 영향 4파일→1 SO. PreRunSetupPanel·OrderBoard도 동일 분할 가능 (재사용 design).
**우선순위:** Tier 2 (해소 권장).

---

### A2. UpgradeEffect 분기 로직이 도메인 시스템 안으로 새고 있음 — Domain-Driven Open/Closed
**위치:** `F1/Assets/Scripts/Upgrades/UpgradeSystem.cs:6-17` 주석 + `F1/Assets/Scripts/Cargo/Modifiers/UpgradeModifiers.cs:34-47`
**현상:** Phase 3가 `CargoBox`의 OCP는 잘 잡았지만, **CargoBox 외 시스템**(`OrderCatalog`의 ChilledHold, `RoutePlannerPanel`의 ShortcutMap, Day 22 Fuel 효과 등)은 Registry에 못 들어옴 — 시그니처가 `ICargoModifier`로 묶여 있기 때문.
```
// UpgradeModifierRegistry — CargoBox에만 적용되는 modifier 매핑
// ChilledHold / ShortcutMap → null 반환 → 각 시스템이 IsOwned로 또 따로 분기
```
UpgradeSystem.cs 주석이 이미 시인 — "CargoBox는 modifier로, Catalog/RoutePlanner는 IsOwned 체크". 새 효과가 어디 영향 줄지 알려면 두 곳 검색해야 함 (응집 깨짐).
**문제:** OCP의 한 단계 위 — "한 효과가 N개 시스템에 영향"인 경우 modifier chain은 시스템별로 N개 필요한데, Phase 3 패턴은 한 인터페이스(`ICargoModifier`)에 고정. 효과를 추가할 때 닿는 파일이 결국 폭증.
**제안 패턴:** **Strategy + Domain Bus.** `IUpgradeEffectHandler<TContext>` 제네릭 또는 도메인별 Modifier 인터페이스(`ICargoModifier`, `ICatalogFilter`, `IRouteAugmentor`). 한 `UpgradeEffect`는 자기 도메인 modifier들의 묶음 클래스로 정의 — 예: `class ChilledHoldEffect { ICatalogFilter catalogPart; ICargoModifier cargoPart; }`. 시스템들이 `Services.Upgrade.Resolve<ICatalogFilter>()` 같은 식으로 자기 도메인 modifier 리스트만 가져옴.
**비용/이득:** 3~5h (지금 modifier 3개뿐이라 비용 낮음). 이후 업그레이드 8개 추가될 때 매번 "IsOwned 어디서 또 봐야 하지?" 추적 비용 사라짐.
**우선순위:** Tier 2 (지금 손대면 cheap, 효과 5개 넘으면 비싸짐).

---

### A3. 정적 호버 dispatch — `OrderBoard._hoverTarget` 정적 필드는 hidden singleton 안티패턴
**위치:** `F1/Assets/Scripts/Order/OrderBoard.cs:51-60`, `OrderCardUI.cs:108-115`
**현상:**
```
static OrderBoard _hoverTarget;
public static void NotifyHoverEnter(int cardIndex) { if (_hoverTarget != null) _hoverTarget.OnCardHoverEnter(cardIndex); }
// OrderCardUI.OnPointerEnter → OrderBoard.NotifyHoverEnter(cardIndex);
```
**문제:** 카드는 자기 부모 보드를 정적 전역으로 찾는다. DIP 위반(카드가 구체 클래스 OrderBoard에 직접 의존) + Phase 1의 Service Locator 일관성 깨짐(여기는 Services 안 거침) + 한 씬에 보드 2개 못 둠(현재는 문제 없지만 미래 게임내 광고판/허브 사이드 보드 도입 시 폭발). `Bind`에서 콜백을 받는 `onCardClicked` 패턴은 이미 잘 되어 있는데 hover만 따로 정적인 것도 일관성 깨짐.
**제안 패턴:** **Observer via callback (이미 클릭이 쓰는 패턴 그대로).** `card.Bind(cardIndex, job, onClick, onHoverEnter, onHoverExit)`. OrderBoard는 그냥 `OnCardHoverEnter/Exit`을 method group으로 넘김. 정적 필드 0, Find 0.
**비용/이득:** 30분. 정적 상태 1개 제거 + Bind 시그니처가 카드의 모든 외향 채널을 보여주는 문서가 됨.
**우선순위:** Tier 3 (저비용, 일관성 가치).

---

### A4. HUD 4종에서 `BuildIfNeeded` 패턴 중복 — Template Method가 절반만 적용됨
**위치:** `F1/Assets/Scripts/GearHUD.cs:49-97`, `UI/SpeedometerHUD.cs:43-98`, `UI/DeliveryProgressHUD.cs:39-87`, `Order/CountdownHUD.cs:42-74`
**현상:** Phase 2가 `BarHUDBase`로 FuelHUD/IntegrityHUD를 통합했는데, **텍스트+BG 패턴 HUD 4종**은 여전히 각자 `EnsureCanvasParent` + `BuildIfNeeded`(BG sprite + Sliced + theme.colorPanel + RectTransform anchor)를 거의 동일하게 50줄씩 반복.
```
// GearHUD.BuildIfNeeded vs CountdownHUD.BuildIfNeeded vs SpeedometerHUD.BuildIfNeeded
//   bg = new GameObject(...); bg.sprite = theme.panelDark; bg.type = Image.Type.Sliced; ...
//   bg.anchorMin/Max/offsetMin/Max 동일 ...
```
또 `Update`에서도 `theme.colorPanel` 재대입 (theme 변경 추적용) 4곳 동일.
**문제:** SRP는 OK지만 **DRY/패턴 일관성**이 깨졌고, Phase 2 학습 노트와 실제 코드 사이의 간극이 생긴다(미래 도현이 "왜 Bar는 base, Text는 안 함?" 헷갈림). 새 텍스트 HUD 추가 = 50줄 또 복붙.
**제안 패턴:** **`TextHUDBase` (Template Method, BarHUDBase 자매).** abstract hook: `OnAcquireDependency` / `HasDependency` / `OnLevel` (또는 `OnPrimaryText`) / `OnSecondaryText` / `OnApplyColor`. activeContext가 "Phase 2.5 — TextHUDBase 도입(선택)"으로 이미 인지하고 있음 — 그 옵션을 Tier 3 작업으로 굳히기.
**비용/이득:** 3~4h. 4 HUD 각 ~50줄 → 각 ~15~20줄. 새 텍스트 HUD 추가 비용 50줄→10줄.
**우선순위:** Tier 3 (cosmetic하지만 학습 + 패턴 일관성 보존 가치).

---

### A5. RunController의 보상 계산 = 정책이 컨트롤러에 박힘 — SRP 변경 축 충돌
**위치:** `F1/Assets/Scripts/Order/RunController.cs:169-227`
**현상:** `EndRun`이 (a) 상태 전이(RunActive=false), (b) integrity 캡처, (c) 보상 공식(baseReward + timeBonus + ratio×0.5 + integrityMul), (d) `ComputeIntegrityMultiplier` / `IntegrityGrade` 정책(GDD 5단계 카피와 1:1), (e) Services 호출(Upgrade.AddReward, Result.Show)을 한 메서드+같은 클래스에서 함.
```
public static float ComputeIntegrityMultiplier(float integrityPercent) {
    if (i >= 80f) return 1.0f;  if (i >= 50f) return 0.8f; ...
}
public static string IntegrityGrade(float integrityPercent) { ... }
```
이미 `ResultPanel.BuildStory`(같은 5단계)와 의미가 중복돼 카피 변경 시 두 파일 동기화가 필요.
**문제:** GDD systems/cargo.md 5단계 정책은 게임 디자인이 자주 만지는 데이터(보상 곡선, 등급 라벨). 이게 컨트롤러 안 static 메서드면 디자인 변경 → 코드 컴파일 → 테스트 사이클. 또 `RunController`는 이미 "런 생명주기 + 보상 계산" 두 책임.
**제안 패턴:** **`RewardCalculator` (POCO 또는 SO) + `CargoGradeBook` (SO).** RunController는 `var result = rewardCalc.Compute(job, completed, elapsed, integrityPercent)`만 호출. 5단계 표는 ScriptableObject(`CargoGradeBook` — thresholds[], multipliers[], labels[], stories[]) — 디자이너가 인스펙터에서 곡선 튜닝. ResultPanel의 BuildStory도 같은 SO 참조 → 카피 중복 0.
**비용/이득:** 2~3h. 5단계 곡선 튜닝 비용: 코드편집→재컴파일→재플레이 → 인스펙터 슬라이더만으로. F1 복귀 메타와 함께 보상 곡선이 게임 핵심 dial이므로 비용 대비 효과 큼.
**우선순위:** Tier 2.

---

### A6. UpgradeSystem이 싱글톤 + Service Locator 양다리 — Phase 1 일관성 부분 누락
**위치:** `F1/Assets/Scripts/Upgrades/UpgradeSystem.cs:22-23` + `ResultPanel.cs:365, 411`, `CargoBox.cs:157`
**현상:**
```
// UpgradeSystem.cs
public static UpgradeSystem Instance { get; private set; }
// ResultPanel.cs (Phase 1 이후에도)
var ups = UpgradeSystem.Instance;   // ← Services.Upgrade 안 거침
if (ups.TryPurchase(def)) ...
```
**문제:** Phase 1 학습 문서가 "Find/Instance 직접 접근 → Services 경유"라고 명시했는데, `ResultPanel`은 여전히 `UpgradeSystem.Instance` 직접 접근. `TryPurchase`/`Wallet` 이벤트 구독이 `IUpgradeSystem` 인터페이스에 없어서 Phase 1 추상화가 새고 있음 (Interface Segregation은 아니고 ISP 반대 — 인터페이스가 너무 작음).
**제안 패턴:** **인터페이스 확장 + 싱글톤 제거.** `IUpgradeSystem`에 `TryPurchase(UpgradeDef)`, `event Action<int> OnWalletChanged`, `event Action<UpgradeEffect> OnUpgradePurchased` 추가. `static Instance` 제거 (`GameContext`만 등록 책임). ResultPanel은 `Services.Upgrade.TryPurchase(def)`. CargoBox.ApplyPreset도 같은 인터페이스 사용 (이미 `Services.Upgrade.IsOwned`은 쓰고 있어서 절반만 일관).
**비용/이득:** 1~2h. Phase 1 일관성 100% + 테스트용 mock UpgradeSystem 가능.
**우선순위:** Tier 3.

---

### A7. `IRunController` 인터페이스가 호출자 요구를 만족 못함 → 호출자가 구상 캐스팅으로 우회 — DIP 새는 추상화
**위치:** `F1/Assets/Scripts/Core/Core.cs:66-72` + `Order/ResultPanel.cs:23, 60, 296-318`
**현상:** `IRunController`는 `BeginRun(job)`, `TimeRemaining`, `RunActive`만 노출. 하지만 ResultPanel은 `controller.CurrentJob`, `controller.LastIntegrityPercent`, `controller.CompletedCount`, `controller.LastReward`, `controller.LastRunSuccess`, `controller.LastElapsed`, `controller.RestartFromBoard()`를 다 쓰는데 — `Show(RunController ctrl)` 시그니처 자체가 **구상 타입**으로 받음.
```
public void Show(RunController ctrl)   // ← IRunController 아님
```
`IResultPanel.Show(RunController ctrl)`도 인터페이스에 구상 타입을 넣어 인터페이스가 구상에 묶임.
**문제:** Phase 1이 만든 `Services.Run as RunController` 캐스팅이 곳곳에서 발생할 수밖에 없는 근본 원인. CountdownHUD/OrderBoard도 `Services.Run as RunController` 패턴. 인터페이스가 "최소" 너무 작아서 호출자가 다시 구상으로 내려감 — DIP가 의도와 반대로 작동.
**제안 패턴:** **`IRunController`에 결과 캡처 표면 추가 + `IResultPanel.Show`는 결과 DTO 받음.**
```
interface IRunSummary { OrderJob Job; bool Success; int Reward; float Elapsed; float IntegrityPercent; int CompletedCount; }
interface IRunController { ... void RestartFromBoard(); IRunSummary LastSummary { get; } }
interface IResultPanel { void Show(IRunSummary summary); }
```
RunController.EndRun이 `LastSummary`를 set, ResultPanel은 DTO만 의존 → 테스트 시 fake summary로 UI 점검 가능.
**비용/이득:** 2~3h. `as RunController` 캐스팅 4~5곳 제거 + 정산 화면 단독 테스트 가능.
**우선순위:** Tier 2 (Phase 1의 마무리. 이거 빠지면 Phase 1이 "절반").

---

### A8. AudioCueBus는 정적 mutable state — 도메인 리로드/씬 전환 시 stale handler 위험
**위치:** `F1/Assets/Scripts/Audio/AudioCueBus.cs:30-56`
**현상:**
```
static readonly Dictionary<AudioCueId, Action<float>> _handlers = new ...;
public static void Clear() => _handlers.Clear();
```
Unity의 "Enter Play Mode without Domain Reload" 옵션이 켜져 있으면 정적 dictionary가 이전 플레이의 핸들러를 보존 → AudioManager가 OnDestroy에서 Unsubscribe하더라도 다른 publisher의 정적 캡처가 남을 수 있음. 또 게임 자체에 멀티 씬 도입 시 핸들러 중복 등록 가능.
**문제:** Phase 4 패턴 자체는 OK지만 **Unity-aware lifecycle**이 비어 있다. `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`로 자동 Clear가 없음. 학습용 Bus 패턴이지만 "도메인 리로드 안 함" 옵션의 함정은 직접 부딪쳐 보기 전엔 안 보임.
**제안 패턴:** **Subsystem-registered 초기화 hook 한 줄.**
```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
static void ResetOnEnterPlayMode() => _handlers.Clear();
```
또는 `AudioManager`가 Awake에서 `AudioCueBus.Clear()` 호출(단일 publisher 가정 시).
**비용/이득:** 10분. 미래의 1시간 디버깅 회피.
**우선순위:** Tier 3.

---

### A9. `CargoTypePresets.BoxColor` 같은 정적 룩업이 도메인 정책을 코드에 박음 — 데이터 vs 코드 경계
**위치:** `F1/Assets/Scripts/Order/RunController.cs:128`, ResultPanel·OrderCardUI 등에서 `CargoTypePresets.DisplayName` / `DriveTip` / `BoxColor` 정적 호출 다발. `TireTypePresets`(Phase 5)도 같은 패턴.
**현상:** Phase 5에서 TireType을 enum→정적 dictionary로 정리한 건 좋지만, 화물 종류 데이터(이름·색·운전팁·sensitivities 11종)는 결국 코드에 박힌 채. ScriptableObject 한 단계 더 가야 진짜 OCP.
**문제:** "결정 채널 9" 중 "배송물자 종류"가 dial로 등장하는데, 화물 종류 추가/튜닝이 **코드 변경**이라는 점에서 디자인 dial이 코드 dial이 되어 있음. 디자이너 친화도 ↓.
**제안 패턴:** **`CargoTypeDef` (SO) + `CargoCatalog` (SO list).** enum은 유지(빠른 비교용) 하되 룩업은 catalog에서. RunController는 `Services.CargoCatalog.Get(job.cargoType).boxColor`. Tier 3 / 4 — MVP 이후 가치.
**비용/이득:** 4~6h, MVP 안에 안 들어가도 됨. 4주차 이후 또는 "화물 종류 dial" 작업 시 묶어서.
**우선순위:** Tier 4 (post-MVP).

---

## 3. 잘된 점 (역참조용)

- **Phase 1 Service Locator (`Core/Core.cs`)**: GameContext가 단일 Composition Root로 잘 작동. `[DefaultExecutionOrder(-1000)]` + `AutoFindMissing` fallback + 등록 카운터(`12/12`) 모두 모범. Phase 1 학습 노트와 코드가 일치.
- **Phase 2 `BarHUDBase`**: Template Method 골격이 깔끔. abstract hook 4개 + virtual hook 2개로 변/불변 경계 분명.
- **Phase 3 `ICargoModifier` chain + `UpgradeModifierRegistry`**: CargoBox.ApplyPreset이 OCP를 정확히 만족. 새 modifier 추가가 Registry 한 줄.
- **`RunController.BeginRun`을 7개 명명 메서드로 분해(최근 커밋 3fd3fbb)**: SRP 향상 + 가독성 + 호출 순서가 그대로 시나리오 문서.
- **`OrderCardUI.Bind(idx, job, onClick)`** 콜백 주입 — 클릭은 DIP 잘 지킴 (A3에서 호버만 보강하면 완성).
- **`AudioCueBus.Subscribe/Unsubscribe`의 delegate combine 패턴**: thread-safety는 약해도 Unity 단일 스레드 가정에선 충분, OCP 의도 명확.

---

## 4. 팀 A 한 줄 요약

"Phase 1~5는 80점이지만 마무리(`IRunController` 확장 A7, `UpgradeSystem` 일관성 A6, `ResultPanel` 분할 A1)를 안 끝내면 다음 기능 추가마다 캐스팅·싱글톤·God class가 다시 새 나간다 — 더 늘리지 말고 4개월 동안 학습한 패턴을 같은 자리에서 한 번 더 닫아라."
