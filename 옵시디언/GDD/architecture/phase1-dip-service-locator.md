# Phase 1 — DIP + Service Locator + Composition Root

> 목적: PitCrew 코드의 36개 `FindFirstObjectByType` 호출을 1곳 와이어링으로 압축.
> 작성: 2026-05-08 (세션 18). 도현 학습 목적 리팩터.

---

## 1. 진단 — 무엇이 문제였나

세션 17 코드 감사 결과:

```
RunController.Awake (RunController.cs:33-41)
├── FindFirstObjectByType<DeliveryManager>()
├── FindFirstObjectByType<OverviewCamera>()
├── FindFirstObjectByType<OrderBoard>()
├── FindFirstObjectByType<ResultPanel>()
├── FindFirstObjectByType<FuelSystem>()
└── FindFirstObjectByType<CargoBox>()

7개 HUD 각자 Awake에서 자기 의존 시스템 Find ...
AudioManager.HookEvents에서 3개 Find ...
```

**합계: 27 파일에서 36회 Find 호출.**

### 왜 문제인가
1. **의존이 코드에 안 보임** — "RunController가 누구를 쓰나?" → 코드만 봐서는 모름. 씬 GameObject 배치에 숨음.
2. **Awake 순서 미정** — Unity는 Awake 순서를 보장하지 않음. Find가 자기 직전에 Awake 안 끝난 객체를 못 찾을 수 있음 (silent null).
3. **테스트 불가** — DeliveryManager 모킹하려면 씬에 진짜 GameObject 필요.
4. **결합도 ★★★** — RunController가 DeliveryManager의 모든 public 메서드를 잠재적으로 알 수 있음.

---

## 2. 적용한 3가지 패턴

### A. DIP — Dependency Inversion Principle (SOLID의 D)

> "고수준 모듈은 저수준 모듈에 의존하면 안 된다.
>  둘 다 추상화에 의존해야 한다."
>                                      — Robert C. Martin

**Before**: `RunController` → 구체 클래스 `DeliveryManager`에 의존
**After**: `RunController` → 인터페이스 `IDeliveryManager`에 의존, `DeliveryManager`도 같은 인터페이스에 의존

```csharp
// Before
public class RunController : MonoBehaviour {
    [SerializeField] DeliveryManager deliveryManager;
    void Start() { deliveryManager.ActivateSubset(5); }
}

// After
public class RunController : MonoBehaviour {
    IDeliveryManager Delivery => Services.Delivery;
    void Start() { Delivery.ActivateSubset(5); }
}
```

**효과**:
- `RunController`는 `IDeliveryManager`가 정의한 메서드만 사용 가능 → API 표면 작아짐
- `DeliveryManager` 구현 바꿔도 `RunController` 그대로 (Liskov 자연 충족)
- 가짜 `MockDeliveryManager`로 테스트 가능

### B. Service Locator (Game Programming Patterns 책)

> Bob Nystrom: 전역 정적 클래스에 시스템들을 등록·조회.
> https://gameprogrammingpatterns.com/service-locator.html

**핵심 아이디어**: "이 시스템 어디 있지?" 를 검색하지 말고, **한 곳에 등록해두고 거기서 꺼내라**.

```csharp
// Services.cs (정적)
public static class Services {
    public static IDeliveryManager Delivery { get; internal set; }
    public static IOrderBoard Board { get; internal set; }
    // 8개 시스템...
}

// 사용
var delivery = Services.Delivery;  // Find 호출 0
```

**트레이드오프**:
- ✓ 매우 단순. DI 컨테이너 (VContainer 등) 학습 비용 X.
- ✓ 36개 Find 호출 모두 제거 가능.
- ✗ 글로벌 정적 상태. 멀티 씬·테스트 시 `Services.Reset()` 필요.
- ✗ 진짜 DI 컨테이너보다 약함 — 컴파일 타임 의존 검증 X.

PitCrew는 1인 + 단일 씬 위주 → Service Locator 가 sweet spot. 진짜 DI는 Phase 2 후보.

### C. Composition Root (Mark Seemann의 패턴)

> "Composition Root는 애플리케이션 시작 지점에 단 하나 두는, 객체들을 조립하는 위치이다."
> — Mark Seemann, "Dependency Injection in .NET"

**구현**: `GameContext.cs`. 씬에 단 하나 두는 MonoBehaviour. 인스펙터에 모든 시스템 참조 노출. `Awake`에서 `Services.X = ...` 등록.

```csharp
[DefaultExecutionOrder(-1000)]   // 다른 컴포넌트 Awake보다 먼저
public class GameContext : MonoBehaviour {
    [SerializeField] DeliveryManager deliveryManager;
    [SerializeField] OrderBoard orderBoard;
    // ...

    void Awake() {
        Services.Reset();
        Services.Delivery = deliveryManager;
        Services.Board = orderBoard;
        // 8개 시스템 등록
    }
}
```

**SRP (Single Responsibility) 적용**:
- 이 클래스의 책임 = **와이어링 한 곳**.
- 그 외 0. 게임 로직 X, UI X, 데이터 X.

**효과**: "누가 누구를 만드는가" 가 코드에 명시. 의존 그래프가 1 파일에 압축.

---

## 3. PitCrew 적용 (Phase 1 단계)

### 신규 파일 (3개)
- `F1/Assets/Scripts/Core/IGameSystems.cs` — 8개 인터페이스
- `F1/Assets/Scripts/Core/Services.cs` — 정적 Service Locator
- `F1/Assets/Scripts/Core/GameContext.cs` — Composition Root MonoBehaviour

### 수정 (8개 + 리팩터 대상들)
- 8개 구체 클래스에 `: IInterface` 추가 (마커만, 동작 변경 0)
- `RunController.cs` — Find 6개 → `Services.X` 접근
- 7개 HUD — 각자 Find → `Services.X`
- `AudioManager.cs` — HookEvents의 3 Find → `Services.X`

### 씬 작업
- `OpenWorld_v2.unity`에 빈 GameObject 만들고 `GameContext.cs` 부착
- 인스펙터에 8개 시스템 드래그
- 또는 `SetupPhase1Integration` 메뉴에 자동화 단계 추가

---

## 4. 학습 단계 권장

### 단계 1 (지금) — 패턴 이해 + 인터페이스 도입
- `IGameSystems.cs` 정독 — 인터페이스가 "API 명세"인 점 체감
- `Services.cs` 정독 — 정적 레지스트리 단순함 체감
- `GameContext.cs` 정독 — 와이어링 한 곳에 모임을 봄

### 단계 2 — RunController 리팩터 비교
- Before: 6 Find 호출 + 6 SerializeField fallback
- After: Services.X 접근 6번. Awake 비움.
- diff 보고 "줄어든 코드 = 줄어든 결합" 체감.

### 단계 3 — 씬에서 동작 확인
- GameContext GameObject 추가
- Play 모드 → 콘솔에 `[GameContext] 8/8 시스템 등록 완료` 확인
- 기능 동작 그대로인지 검증

### 단계 4 (Phase 2) — 다른 곳에 확장
- HUD 7개에 같은 패턴 적용
- AudioManager.HookEvents 정리

---

## 5. 회피한 함정

### "왜 ECS·DOTS·MVC·MVVM 안 가는가?"

| 패턴 | 학습 비용 | PitCrew 가치 | 판정 |
|---|---|---|---|
| ECS / DOTS | 3~6개월 | 차 1대 + 배달지 7개에 과한 도구 | ❌ |
| MVVM (Reactive) | 1~2개월 | uGUI는 자동 데이터바인딩 X, ROI 낮음 | ❌ |
| 진짜 DI 컨테이너 (VContainer) | 1~2주 | 1인 + 매니저 3개엔 오버킬 | ❌ Phase 2 |
| MVC/MVP | 2~4주 | 화면 3개라 미세 ROI | ❌ Phase 3 |
| **Service Locator + DIP** | **2~3시간** | **Find 36개 즉시 제거** | ✅ Phase 1 |

**도현의 ExulMundi 함정 인지** — 이 단계에서 ECS·MVVM 같은 거 손대면 정확히 미완성 패턴 재현. Phase 1 = 가장 작은 단계로 가장 큰 효과.

---

## 6. 다음 단계 (Phase 2 후보)

이 Phase 1 정착되면:
1. **Phase 2** — HUD `BarHUDBase` (Template Method, OCP). 7개 복붙 → 1 베이스 + N 자식.
2. **Phase 3** — `CargoBox.ApplyPreset` Modifier Chain (OCP, Strategy/Decorator). 새 업그레이드 추가 시 CargoBox 안 건드림.
3. **Phase 4** — `RunController` State 패턴 FSM. `RunActive` bool → 5상태 객체.
4. **Phase 5** — SO Event Channel (Observer 통일).

각 Phase는 코어 루프 동작 후 진입. MVP 4주 게이트 통과가 우선.

---

## 7. 참고 자료

- Robert C. Martin — "Clean Architecture" Ch 11 (DIP)
- Bob Nystrom — "Game Programming Patterns" Service Locator: https://gameprogrammingpatterns.com/service-locator.html
- Mark Seemann — "Dependency Injection in .NET" (Composition Root 정의)
- Unity Learn — Programming Patterns: https://unity.com/how-to/build-modular-codebase-mvc-and-mvp-programming-patterns
