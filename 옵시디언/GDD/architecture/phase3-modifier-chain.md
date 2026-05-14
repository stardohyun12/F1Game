# Phase 3 — Modifier Chain (Strategy + Registry)

> 목적: `CargoBox.ApplyPreset`의 OCP 위반(HOT-2) 해소.
> 작성: 2026-05-08 (세션 18). 도현 학습.

---

## 1. 진단 — OCP 위반 교과서 사례

세션 17 코드 감사 (HOT-2). `CargoBox.cs:134-177` 한 메서드가:

```csharp
public void ApplyPreset() {
    // 1단계: 11 필드 baseline 대입
    var p = CargoTypePresets.Get(cargoType);
    longWarnG = p.longWarnG;  // ... 11개

    // 2단계: 5 필드 multiplier 적용
    float packMul = PackingTierHelper.ThresholdMultiplier(packing);
    longWarnG *= packMul; // ... 5개

    // 3단계: 3개 if/else로 upgrade additive
    var ups = UpgradeSystem.Instance;
    if (ups != null) {
        if (ups.IsOwned(UpgradeEffect.SuspensionUpgrade)) {
            longWarnG += 0.1f; latWarnG += 0.1f;
        }
        if (ups.IsOwned(UpgradeEffect.SmoothSteering)) {
            spinWarnDegPerSec += 20f;
        }
        if (ups.IsOwned(UpgradeEffect.ReinforcedTrunk)) {
            maxHP += 20f; integrityHP = maxHP;
        }
    }
}
```

### 무엇이 문제인가

- **OCP 위반**: 새 업그레이드 추가하려면 `CargoBox.cs` 수정 필수. "닫혀 있어야 할" 핵심 도메인이 매 업그레이드마다 변경됨.
- **숨은 버그**: `maxHP += 20f` 누적. ApplyPreset이 한 프레임에 2번 호출되면 maxHP=140 (cumulative). 실제 BeginRun이 SetCargoType + SetPacking 둘 다 호출 → 버그 발생 중.
- **읽기 어려움**: 한 메서드가 4가지 일 (preset / multiplier / 3종 upgrade).
- **테스트 어려움**: 부분 단위 테스트 불가. 전체 ApplyPreset만 테스트 가능.

---

## 2. 적용한 패턴 — Modifier Chain

조합 패턴: **Strategy + Registry**.

> Strategy: "한 작업을 수행하는 알고리즘 군을 인터페이스로 추상화하고 교체 가능하게 만든다." — GoF
> Registry: "특정 키→객체 생성 매핑을 한 곳에 모아 동적 lookup 가능하게."

### 핵심 아이디어

각 변환 단계를 **객체화**:
- `CargoTypeModifier` — baseline 설정
- `PackingModifier` — 곱셈
- `SuspensionUpgradeModifier` / `SmoothSteeringUpgradeModifier` / `ReinforcedTrunkUpgradeModifier` — 덧셈

전부 `ICargoModifier` 인터페이스 구현:
```csharp
public interface ICargoModifier {
    void Apply(ref CargoStats stats);
}
```

### Before vs After

```csharp
// Before (CargoBox.cs:134-177, 약 44줄)
public void ApplyPreset() {
    var p = CargoTypePresets.Get(...);
    /* 11 lines baseline */
    /* 5 lines multiplier */
    if (ups.IsOwned(...A)) { ... }
    if (ups.IsOwned(...B)) { ... }
    if (ups.IsOwned(...C)) { ... }
    /* 새 업그레이드 추가 = 이 메서드 또 수정 */
}

// After (~30줄, 골격은 다시 안 변함)
public void ApplyPreset() {
    var modifiers = new List<ICargoModifier> {
        new CargoTypeModifier(cargoType, maxHpBaseline),
        new PackingModifier(packing),
    };
    foreach (UpgradeEffect e in Enum.GetValues(typeof(UpgradeEffect))) {
        if (!ups.IsOwned(e)) continue;
        var mod = UpgradeModifierRegistry.Create(e);
        if (mod != null) modifiers.Add(mod);
    }
    var stats = new CargoStats();
    foreach (var m in modifiers) m.Apply(ref stats);
    /* stats → CargoBox 필드 복사 */
}
```

새 업그레이드 추가 시:
1. 새 클래스 `MyNewUpgradeModifier : ICargoModifier` 작성 (별도 파일)
2. `UpgradeModifierRegistry.map`에 한 줄: `{ UpgradeEffect.MyNew, () => new MyNewUpgradeModifier() }`
3. **`CargoBox.cs`는 안 건드림.** 이게 OCP.

---

## 3. SOLID 매핑

### OCP (Open-Closed) 충족 ★
- 새 동작 추가 시 기존 코드 변경 X. 새 클래스만 추가.
- `CargoBox`는 modifier 인터페이스에만 의존, 구현체는 모름.

### SRP (Single Responsibility) 명확화
- `CargoTypeModifier`: baseline 설정만.
- `PackingModifier`: multiplier 적용만.
- `SuspensionUpgradeModifier`: long/lat warn 0.1 추가만.
- 각 클래스 책임 1개 → 한 줄 요약 가능.

### LSP (Liskov Substitution)
- 어떤 ICargoModifier든 chain에 들어가서 정상 동작. 각자 stats를 변형하기만.

### ISP (Interface Segregation)
- ICargoModifier는 메서드 1개 (Apply). 호출자는 그것만 알면 됨.

### DIP (Dependency Inversion)
- `CargoBox`는 구체 modifier가 아니라 `ICargoModifier` 인터페이스에 의존. Registry로 주입.

**SOLID 5개 모두 자연 충족.** Phase 3는 SOLID 학습의 정석.

---

## 4. 부수 효과 — 숨은 버그 해소

기존 `maxHP += 20f` 누적 버그가 사라짐:
- `CargoTypeModifier`가 매 chain마다 `stats.maxHP = maxHpBaseline`으로 리셋.
- 그 위에 `ReinforcedTrunkUpgradeModifier`가 +20 추가.
- ApplyPreset 100번 호출해도 maxHP는 일관 (baseline + 20).

`maxHpBaseline`은 CargoBox.Awake에서 한 번 캡처. 이후 인스펙터 값 변경되어도 영향 X (런 안정성).

---

## 5. 신규 파일 구조

```
F1/Assets/Scripts/Cargo/
├── CargoBox.cs                       (ApplyPreset 리팩터됨)
└── Modifiers/
    ├── CargoStats.cs                  struct — 11 필드 + maxHP
    ├── ICargoModifier.cs              인터페이스 — Apply(ref CargoStats)
    ├── CargoTypeModifier.cs           baseline (preset에서 가져옴)
    ├── PackingModifier.cs             multiplier (5 필드 곱)
    └── UpgradeModifiers.cs            3개 업그레이드 modifier + Registry
```

---

## 6. Decorator 패턴이라고도 부른다

이 패턴은 **Strategy** (인터페이스 교체)와 **Decorator** (변환 wrapper 누적)의 혼합:
- 각 modifier가 stats를 받아 변형 후 다음 modifier로 넘김 → Decorator 결.
- 인터페이스로 교체 가능 → Strategy 결.

**용어가 중요한 게 아니라 "하나의 변환 단계 = 하나의 객체"라는 형태가 핵심**. GoF 책 어떤 챕터를 보든 동일 결론.

---

## 7. 학습 단계 권장

### 단계 1 — 비교 정독
- `CargoBox.cs` git diff (Before/After)
- `Cargo/Modifiers/` 5개 파일 정독
- "한 if/else가 한 클래스로 객체화"되는 형태 체감

### 단계 2 — 새 modifier 작성 실습
- 가상 업그레이드 "Aerodynamics" — 모든 임계 +0.05 가산
- `AerodynamicsUpgradeModifier : ICargoModifier` 클래스 작성
- `UpgradeModifierRegistry.map`에 1줄 추가
- CargoBox 안 건드리고 동작하는지 확인

### 단계 3 — 결정 채널 연결
- `GDD/decision-channels.md`의 9채널 중 1·5·7·9가 modifier로 분리 가능
- "baseline (4 화물 종류) × multiplier (6 포장) + additive (1·5·7·9 업그레이드)"

---

## 8. 회피한 함정

| 안티패턴 | 왜 안 함 |
|---|---|
| 각 upgrade를 CargoBox 안 if/else 체인 | OCP 위반. 매 추가마다 CargoBox 수정 |
| 모든 upgrade를 enum + switch | switch가 결국 OCP 위반 형태. enum 추가 시 switch 수정 |
| ScriptableObject + 가산 효과 | SO 100개 추가 시 매 SO마다 ApplyPreset 안에 분기 필요 — 같은 문제 |
| 너무 추상화 (4단 상속) | LSP 깨질 위험. 1단으로 충분 |

---

## 9. 다음 단계 — Phase 4

- Phase 4: `RunController` State 패턴 (5 상태 객체) — 게임 흐름 관리
- Phase 5: SO Event Channel — 이벤트 통신 통일

---

## 10. 참고 자료

- Gang of Four — Design Patterns Ch 5 Strategy
- Robert C. Martin — Clean Code Ch 10 (Classes), Clean Architecture Ch 8 (OCP)
- Bob Nystrom — Game Programming Patterns Type Object: https://gameprogrammingpatterns.com/type-object.html
- Refactoring Guru — Strategy: https://refactoring.guru/design-patterns/strategy
