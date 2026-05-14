# Phase 2 — Template Method (HUD Base)

> 목적: 7개 HUD에 흩어진 보일러플레이트를 베이스 클래스로 흡수.
> 작성: 2026-05-08 (세션 18). 도현 학습.
> 1단계 적용: bar 패턴 (FuelHUD + IntegrityHUD). 텍스트 HUD는 추후.

---

## 1. 진단 — 무엇이 문제였나

세션 17 코드 감사 (HOT-5):

7개 HUD가 동일한 5단계를 각자 30~50줄로 복제:
1. `Awake()` — Find dep + EnsureCanvasParent + GetComponent<RectTransform> + BuildIfNeeded
2. `EnsureCanvasParent()` — UIRoot 자식인지 확인 (똑같은 6줄)
3. `BuildIfNeeded()` — BG + Fill + Label 절차적 생성 (똑같은 50줄)
4. `Update()` — 의존 null 체크, theme 적용, level 계산, fill width, sprite 스왑, crit blink, 라벨 텍스트
5. (선택) 충돌 flash 등 추가 효과

특히 FuelHUD와 IntegrityHUD는 80% 동일 코드. 차이점 5줄 (의존 타입, level 계산식, 라벨 포맷).

### 문제
- **DRY 위반**: 한 변경이 N파일을 흩뿌림. 새 HUD 추가 시 50줄 복붙.
- **SRP 흐릿**: HUD 한 클래스가 (1) 의존 acquire (2) 캔버스 생성 (3) 시각 빌드 (4) 데이터 표시. 4 책임 합쳐짐.
- **OCP 위반**: 새 효과(impact flash 등) 추가하려면 HUD 안 if/else로 끼워야.

---

## 2. 적용한 패턴 — Template Method

> "한 알고리즘의 골격을 부모가 정의하고, 변하는 단계는 자식이 override한다."
> — Gang of Four (GoF), Design Patterns

**핵심 아이디어**: 부모 클래스가 **불변 골격**(Awake → Update 흐름)을 final로 잠그고, **변하는 hook 메서드**만 abstract으로 노출.

### Before

```csharp
public class FuelHUD : MonoBehaviour {
    [SerializeField] FuelSystem fuel;
    void Awake() {
        if (fuel == null) fuel = Services.Fuel as FuelSystem;
        EnsureCanvasParent();
        rt = GetComponent<RectTransform>();
        BuildIfNeeded();
    }
    void EnsureCanvasParent() { /* 6줄 */ }
    void BuildIfNeeded() { /* 50줄 */ }
    void Update() { /* 30줄: theme, level, fill, blink, label */ }
}
// 132줄. IntegrityHUD도 거의 동일하게 178줄.
```

### After

```csharp
// 부모 — 골격 고정
public abstract class BarHUDBase : MonoBehaviour {
    void Awake() {
        OnAcquireDependency();      // hook
        EnsureCanvasParent();        // 고정
        ...
        BuildIfNeeded();             // 고정
        OnPostAwake();               // hook
    }
    void Update() {
        if (!HasDependency()) ...    // hook
        var level = OnLevel();       // hook
        ...
        label.text = OnLabel(level); // hook
    }
    protected abstract void OnAcquireDependency();
    protected abstract bool HasDependency();
    protected abstract float OnLevel();
    protected abstract string OnLabel(float level);
    protected virtual void OnPostAwake() { }
    protected virtual void OnApplyEffects() { }   // BG color override 가능
}

// 자식 — 변하는 부분만
public class FuelHUD : BarHUDBase {
    [SerializeField] FuelSystem fuel;
    protected override void OnAcquireDependency() {
        if (fuel == null) fuel = Services.Fuel as FuelSystem;
    }
    protected override bool HasDependency() => fuel != null;
    protected override float OnLevel() => fuel.FuelLevel;
    protected override string OnLabel(float l) =>
        fuel.IsDepleted ? "FUEL EMPTY" : $"FUEL  {Mathf.RoundToInt(l*100)}%";
}
// 19줄. IntegrityHUD는 50줄 (충돌 flash 포함).
```

**효과**: 132+178 = 310줄 → 152줄 + 19+50 = 221줄. **89줄 감소**.

신규 bar HUD 추가 시 ~15줄만 작성하면 됨.

---

## 3. SOLID 매핑

### OCP (Open-Closed) 충족
- 기존 HUD 동작 변경하지 않고 새 HUD 추가 가능 (BarHUDBase는 닫힘, 자식은 열림).
- 새 효과(예: 새 sprite swap 규칙)는 BarHUDBase 안에 가산하면 모든 자식 자동 수혜.

### SRP (Single Responsibility) 명확화
- BarHUDBase: **시각 골격 생성 + Update 흐름**.
- 자식 HUD: **의존 + 데이터 매핑** (이 HUD가 무엇을 보여줄지).
- 책임 2개로 분리됨.

### LSP (Liskov Substitution) 자연 충족
- 자식이 부모 abstract 메서드만 override. 부모 골격은 깨지 않음.
- 어떤 BarHUDBase 자식이든 동일한 Awake/Update 패턴.

### Composition vs Inheritance
- Unity에서 `MonoBehaviour` 자체가 컴포지션 단위. 그 위에 1단 상속(BarHUDBase)은 안전.
- 2~3단 상속은 LSP 깨지기 쉬워 권장 X.

---

## 4. PitCrew 적용 결과

### Phase 2 1단계 (이번 세션)

**대상**: FuelHUD + IntegrityHUD (bar 패턴 둘)
**산출**:
- `F1/Assets/Scripts/UI/BarHUDBase.cs` (신규, 152줄)
- `FuelHUD.cs` (132 → 19줄)
- `IntegrityHUD.cs` (178 → 50줄)

**검증** (Coplay 복귀 후 필요):
- 컴파일 0 에러
- Play 모드: 연료 게이지·무결성 게이지 시각 동일하게 동작

### Phase 2 2단계 (후속)

**대상**: 텍스트 전용 HUD 4개 (GearHUD, CountdownHUD, SpeedometerHUD, DeliveryProgressHUD)
**예상 패턴**: `TextHUDBase` (BG + Label만, Fill 없음)
**비용**: 추가 2~3h

---

## 5. 학습 단계 권장

### 단계 1 — 비교 정독
- `BarHUDBase.cs` 정독: 골격이 무엇이고 hook이 무엇인지 분리해서 봄
- `FuelHUD.cs` (after) vs `IntegrityHUD.cs` (after) — 같은 부모 다른 자식 비교
- "공통 골격은 부모, 변하는 부분만 자식" 체감

### 단계 2 — Hook 추가 실습 (선택)
- 새 효과 (예: BG에 펄스 효과) 추가
- BarHUDBase에 `OnApplyEffects` 사용 → IntegrityHUD가 어떻게 끼우는지

### 단계 3 — 텍스트 HUD 적용
- TextHUDBase 직접 작성 (선택 학습)

---

## 6. 회피한 함정

| 안티패턴 | 왜 안 함 |
|---|---|
| 다중 상속 (BarHUDBase ← FlashableHUD ← MonoBehaviour) | C# 클래스 다중 상속 X. 인터페이스 + 컴포지션이 답 |
| 깊은 상속 트리 | 2단 이상은 LSP 깨지기 쉬움. 1단만 |
| `protected` 남용 | 자식이 부모 내부를 마구 만지면 캡슐화 깨짐. abstract hook으로 통제 |
| Inheritance 강요 | 텍스트 HUD에 BarHUDBase 강제 적용 X. 다른 베이스 (TextHUDBase) 분리 |

---

## 7. 다음 단계 — Phase 3

CargoBox.ApplyPreset의 OCP 위반 (Strategy/Decorator 패턴으로 풀 예정). 같은 OCP 원리지만 이번엔 **상속이 아니라 컴포지션** 기반. 그래서 학습 가치 보완.

---

## 8. 참고 자료

- Gang of Four (Gamma·Helm·Johnson·Vlissides) — Design Patterns Ch 5 Template Method
- Robert C. Martin — Clean Code Ch 6 (Forms vs Functions)
- Unity Learn — Modular Code Patterns: https://unity.com/how-to/build-modular-codebase-mvc-and-mvp-programming-patterns
