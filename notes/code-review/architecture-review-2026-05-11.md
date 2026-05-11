# Architecture Review — 2026-05-11

> Phase 1/2/3 적용 이후 코드 상태 점검. 남은 SOLID 위반과 우선순위 정리.
> 참조: `GDD/architecture/phase1-dip-service-locator.md`, `phase2-template-method.md`, `phase3-modifier-chain.md`.

---

## 1. 이번 세션에 직접 적용한 것 (Tier 1-B)

### A. RunController.BeginRun 분해 (SRP)

**Before:** 단일 35줄 메서드에 7 책임 (state init / delivery / fuel / cargo / shake / camera / events).

**After:** 본체 8줄 + 명명 메서드 6개. 흐름을 메서드 이름으로 읽음.
- `InitRunState`
- `ActivateDeliveryPoints`
- `ApplyCargoSetup`
- `UpdateShakeSimColor`
- `StartFollowCameraAndCloseBoard`
- `SubscribeDeliveryEvents`

**효과:**
- 새 시작 단계 추가 시 메서드 한 줄 추가 (BeginRun 본체에 한 줄, 새 헬퍼 메서드 하나)
- 단계별 단위 테스트 가능
- 단계 순서 변경 시 영향 범위 명확

**커밋:** `claude/architecture-review-2026-05-11` 브랜치에 포함.

---

## 2. 남은 SOLID 위반 — 우선순위 로드맵

### Tier 2 (시스템 추가 압박 클 때)

#### B. AudioManager.HookEvents — OCP 위반 (잔존)

[F1/Assets/Scripts/Audio/AudioManager.cs:86-102](F1/Assets/Scripts/Audio/AudioManager.cs:86) — Phase 1으로 Find는 사라졌지만, 후크 등록 위치는 여전히 AudioManager가 시스템들을 직접 알고 있음:
```csharp
var cargo = Services.Cargo as CargoBox;
if (cargo != null) { cargo.OnImpact += OnCargoImpact; ... }
```
→ 새 SFX 후크 = AudioManager 수정 필수.

**제안 패턴:** AudioCueBus (정적 이벤트 허브)
- 시스템들이 `AudioCueBus.Publish(CueId.CargoImpact, speed)` 발행
- AudioManager는 `AudioCueBus.Subscribe(CueId.X, PlayClip)` 구독만
- 새 SFX 추가 시 AudioManager 안 건드림

**비용:** ~0.5일. **이득:** 신규 SFX 추가 시 N파일 수정 → 1파일.

---

#### C. TireSystem switch 사다리 5번 반복 — OCP + DRY

[F1/Assets/Scripts/Tires/TireSystem.cs](F1/Assets/Scripts/Tires/TireSystem.cs) 한 파일에 동일한 enum switch가:
- `GripMultiplier` (line 45)
- `SpeedMultiplier` (line 57)
- `Update` wear rate (line 82)
- `DisplayName` (line 94)
- `Description` (line 101)

→ 새 타이어 enum 추가 = 5곳 수정.

**제안 패턴:** `CargoTypePresets`와 동일 — `TireTypePresets` ScriptableObject + 정적 매핑.
- 5 switch → 1 룩업
- 데이터(grip/speed/wear/name/desc)와 코드(`TireSystem`) 분리
- 인스펙터에서 튜닝

**비용:** ~0.5일. **이득:** 패턴 일관성 (이미 CargoTypePresets가 같은 문제를 풀어놓음).

---

#### Phase 2 잔여 — 텍스트 HUD에 Template Method 미적용

[GDD/architecture/phase2-template-method.md](GDD/architecture/phase2-template-method.md)에 명시: "1단계 적용: bar 패턴 (FuelHUD + IntegrityHUD). 텍스트 HUD는 추후."

**대상:** `GearHUD`, `CountdownHUD`, `SpeedometerHUD`, `DeliveryProgressHUD` — 4개. 각각 `BuildIfNeeded` 패턴 복붙 잔존.

**제안:** Phase 2 base class 패턴 확장 (또는 prefab + 데이터 바인더 분리).

**비용:** ~1일. **이득:** 새 텍스트 HUD 추가 시 50줄 → 5줄.

---

### Tier 3 (정합성·안티패턴 정리)

#### D. OrderBoard `_hoverTarget` 자가발명 안티패턴

[F1/Assets/Scripts/Order/OrderBoard.cs:51-58, 64](F1/Assets/Scripts/Order/OrderBoard.cs:51) — static 호버 라우터:
```csharp
static OrderBoard _hoverTarget;
public static void NotifyHoverEnter(int cardIndex) {
    if (_hoverTarget != null) _hoverTarget.OnCardHoverEnter(cardIndex);
}
```

같은 클래스에서 클릭/포장은 Action 콜백으로 잘 처리(`OnCardClicked`, `OnPackingChanged`)하면서 호버만 정적 라우팅 → **일관성 위반**.

**제안:** `CardUI.Bind`에 호버 콜백도 추가 (기존 콜백 패턴 확장). 정적 결합 0.

**비용:** ~0.5일. **이득:** 코드 스타일 통일, 멀티-OrderBoard 환경 가능.

---

### Tier 4 (위험도 낮음, 마지막)

#### F. CargoSimCamRig 문자열 Find — 잔존

[F1/Assets/Scripts/Cargo/CargoShakeSimulator.cs:163-171](F1/Assets/Scripts/Cargo/CargoShakeSimulator.cs:163) — Phase 1으로 SerializeField 우선 + 1회 fallback으로 개선됐으나 `GameObject.Find("CargoSimCamRig")` 잔존:
```csharp
// Phase 1: GameObject.Find by name 1회 fallback (SerializeField 비어있으면).
var rigGO = GameObject.Find("CargoSimCamRig");
```

이름 변경 시 컴파일 에러 없이 런타임 실패. 위험도는 fallback이라 낮음.

**제안:** SerializeField를 항상 인스펙터에서 와이어링하는 정책 + fallback 제거 또는 LogError 강화.

**비용:** ~0.2일. **이득:** silent failure 제거.

---

## 3. 의도적으로 제외한 것 (워크트리 stale 정보)

이전 분석은 워크트리(`claude/angry-montalcini-14cd06`, main의 옛 시점에서 분기) 기반이라 main 최신 상태와 어긋남. 다음 항목은 main에 이미 적용됨:

- **E. ResultPanel IMGUI → UGUI** — main은 이미 UGUI 전환 완료 ([F1/Assets/Scripts/Order/ResultPanel.cs:10](F1/Assets/Scripts/Order/ResultPanel.cs:10) "IMGUI(OnGUI) → UGUI 자가구성").
- **CargoBox OCP (HOT-2)** — Phase 3 modifier-chain 적용 완료 (`F1/Assets/Scripts/Cargo/Modifiers/` 5 파일, 커밋 `bf309f0`).
- **HUD Template Method (HOT-5) bar 패턴** — Phase 2 1단계 완료 (FuelHUD/IntegrityHUD). 텍스트 HUD는 Tier 2 잔여로 위에 명시.
- **Phase 1 DIP** — `Core/Core.cs` 단일 파일에 인터페이스 12개 + Services + GameContext 완비 (커밋 `c064f59`, `d9e6a59`). Find 36→0 달성.

---

## 4. 권고 진행 순서

1. **이번 세션 끝:** Tier 1-B 적용 완료 (이 PR/커밋).
2. **다음 1 세션:** B (AudioCueBus) — 0.5일.
3. **다음 1~2 세션:** C (TireTypePresets) — 0.5일. CargoTypePresets 패턴 일관화.
4. **여유 있을 때:** Phase 2 텍스트 HUD 확장, D (OrderBoard 호버 일관화), F (CargoSimCamRig fallback 제거).

전부 합쳐 ~3일 분량. MVP 4주 스코프 안이면 B/C까지만, 4주 후 폴리싱 단계에서 Phase 2 + D + F.

---

## 5. 학습 자산 cross-reference

이 리뷰는 다음 문서를 전제로 함:
- `GDD/architecture/phase1-dip-service-locator.md` — DIP/Service Locator 학습
- `GDD/architecture/phase2-template-method.md` — HUD Template Method 학습
- `GDD/architecture/phase3-modifier-chain.md` — CargoBox OCP 학습
- `GDD/architecture/file-map.md` — 파일 구조 청사진

위 학습 노트들이 SOLID 원칙의 이론·패턴 설명을 담당. 이 리뷰는 그 위에서 **남은 위반·미적용 부분의 인벤토리**.
