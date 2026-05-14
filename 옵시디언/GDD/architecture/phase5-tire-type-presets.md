# Phase 5 — TireTypePresets (Data-Driven Lookup)

> 목적: `TireSystem` 안 동일한 enum switch 5번 반복(리뷰 Tier 2-C) 해소.
> 작성: 2026-05-11 (세션 19). 도현 학습.

---

## 1. 진단 — 같은 switch 5번 = 5중 결합

리팩터 전 `TireSystem.cs`에 동일한 `TireType` switch가 다섯 번:

```csharp
// 1. GripMultiplier
float baseMul = currentTire switch {
    TireType.Soft => 1.4f, TireType.Hard => 0.7f, _ => 1.0f,
};

// 2. SpeedMultiplier
return currentTire switch {
    TireType.Soft => 1.05f, TireType.Hard => 0.95f, _ => 1.0f,
};

// 3. Update — wear rate
float rate = currentTire switch {
    TireType.Soft => wearRateSoft, TireType.Hard => wearRateHard, _ => wearRateMedium,
};

// 4. DisplayName (static)
public static string DisplayName(TireType t) => t switch {
    TireType.Soft => "Soft", TireType.Hard => "Hard", _ => "Medium",
};

// 5. Description (static)
public static string Description(TireType t) => t switch {
    TireType.Soft => "그립↑↑ · 빠른 마모 · 고속 코너",
    TireType.Hard => "그립↓ · 느린 마모 · 장거리",
    _ => "균형 · 표준",
};
```

### 왜 문제인가

| 원칙 | 위반 |
|---|---|
| **OCP** | 새 타이어 enum 추가 = **5곳** 수정. 한 곳 빠뜨리면 런타임 버그(컴파일은 통과). |
| **DRY** | 동일 분기 구조가 반복. 데이터 한 줄을 5곳에 흩뿌리고 있음. |
| **데이터-코드 분리** | grip 1.4, speed 1.05 같은 **데이터**가 **메서드 로직**에 박혀 있어 디자이너가 손댈 수 없음. |

세션 17 리뷰는 명시한 해법: "`CargoTypePresets`와 동일 패턴 — `TireTypePresets` 정적 매핑." 이미 같은 코드베이스에 정답 패턴이 존재.

---

## 2. 패턴 — Data-Driven Lookup (CargoTypePresets와 동일)

### 아이디어

타입별 데이터를 **하나의 struct**로 묶고, **enum → struct** 한 번의 룩업으로 통일한다.
호출부는 분기 없이 `TireTypePresets.Get(t).grip` 처럼 필드 접근.

```
[TireSystem]                       [TireTypePresets]
  GripMultiplier ────► Get(t).grip ────► switch (한 곳)
  SpeedMultiplier ───► Get(t).speed
  DisplayName ───────► Get(t).displayName
```

### 구현 (`TireTypePresets.cs`)

```csharp
public static class TireTypePresets
{
    public struct Preset
    {
        public float grip;
        public float speed;
        public string displayName;
        public string description;
    }

    public static Preset Get(TireType t)
    {
        switch (t) {
            case TireType.Soft:
                return new Preset {
                    grip = 1.4f, speed = 1.05f,
                    displayName = "Soft",
                    description = "그립↑↑ · 빠른 마모 · 고속 코너",
                };
            case TireType.Hard:
                return new Preset { grip = 0.7f, speed = 0.95f, ... };
            case TireType.Medium:
            default:
                return new Preset { grip = 1.0f, speed = 1.0f, ... };
        }
    }

    public static string DisplayName(TireType t) => Get(t).displayName;
    public static string Description(TireType t) => Get(t).description;
}
```

### TireSystem 변경

```csharp
// 5 switch → 1 룩업
public float GripMultiplier
    => TireTypePresets.Get(currentTire).grip * Mathf.Lerp(1f, 0.5f, wear);

public float SpeedMultiplier
    => TireTypePresets.Get(currentTire).speed;

public static string DisplayName(TireType t) => TireTypePresets.DisplayName(t);
public static string Description(TireType t) => TireTypePresets.Description(t);
```

### wearRate는 왜 안 옮겼나

`wearRateSoft/Medium/Hard`는 `[SerializeField]`로 **인스펙터에서 디자이너가 튜닝**. preset에 옮기면 인스펙터 노출이 사라짐. 그래서:

```csharp
// TireSystem 내부에 private 메서드 하나로 모음 — switch 1번 유지, 5중 반복은 사라짐.
float CurrentWearRate()
{
    switch (currentTire) {
        case TireType.Soft: return wearRateSoft;
        case TireType.Hard: return wearRateHard;
        default:            return wearRateMedium;
    }
}
```

이건 **데이터가 인스펙터에 있어야 한다는 제약**이 패턴 선택을 제약하는 사례. 모든 데이터가 정적 상수면 preset이 깔끔하지만, 디자이너 튜닝 필요 데이터는 SerializeField로 남겨야 함. 절충: switch는 한 곳으로 모아 5중 → 1중.

---

## 3. 효과

| 항목 | Before | After |
|---|---|---|
| 동일 switch 분기 수 | 5 | 1 (TireTypePresets.Get) + 1 (wearRate, 인스펙터 제약) |
| 새 타이어 추가 시 수정 위치 | 5 + (선택) wearRate 1 = 6 | 2 (`TireType` enum + `Get` switch) + (선택) 인스펙터 1 |
| 디자이너 튜닝 (grip/speed 등) | 코드 수정 + 재컴파일 | 코드 한 곳 |
| 빠뜨림으로 인한 런타임 버그 위험 | 5곳 중 누락 가능 | 1곳 — 컴파일 통과면 모든 호출부 일관 |

### 왜 ScriptableObject가 아닌 static class인가

세션 17 리뷰는 "ScriptableObject" 제안. 하지만 채택은 **static + struct**. 이유:

1. **일관성**: `CargoTypePresets`가 이미 같은 패턴. 두 비슷한 데이터를 다른 메커니즘으로 두면 학습/유지비 증가.
2. **할당 비용**: `Get`이 `new Preset {...}` 반환하지만 **struct는 stack 할당** → GC 0. `GripMultiplier`가 매 프레임 호출되어도 안전.
3. **MVP 스코프**: ScriptableObject는 .asset 파일 관리, 인스펙터 와이어링, 빈 참조 케이스 처리 등 인프라 비용. 데이터가 3개(Soft/Medium/Hard) + 정적이면 over-engineering.

ScriptableObject가 필요해지는 분기점:
- 타이어 종류 10개 넘음 (관리 비용 ↑)
- 모드별/난이도별 데이터 변경 (런타임 swap 필요)
- 인스펙터에서 비프로그래머가 튜닝 (현재 SerializeField로 부분 충족)

이 셋 중 하나라도 발생하면 SO로 마이그레이트.

---

## 4. 패턴 일반화 — 언제 Data Lookup인가

| 신호 | 적합 |
|---|---|
| 같은 enum switch가 ≥3곳 | ✓ |
| 분기마다 단순 값 매핑 (분기마다 로직이 다르면 Strategy 패턴) | ✓ |
| 디자이너 튜닝 필요 → SerializeField | △ (인스펙터로 일부 빼야) |
| 분기별 동작이 복잡한 알고리즘 | ✗ (Strategy/Visitor가 더 적합) |

---

## 5. 함정 — 학습 포인트

1. **`new Preset { ... }` GC 안 나오나?** struct는 stack alloc. Heap 할당이 아니라 GC 영향 0. (단, `object`로 boxing되면 heap. `Get()`이 struct 반환이라 boxing 없음.)
2. **switch expression vs statement**: switch expression(`=>`)이 더 간결하지만 default 누락 시 컴파일 에러를 명시 표현 못 함. 정적 분석으로는 동일. 취향. CargoTypePresets가 statement라 일관성 유지.
3. **enum 값 추가 시 default fallback**: `_ => ...` 또는 `default:`는 새 enum이 추가될 때 **silent fallback**으로 작동 → 누락 발견 늦어짐. 강제 발견하려면 default에서 `throw new ArgumentOutOfRangeException()` — 디버그 빌드에서는 의미 있음.
4. **5중 switch를 그냥 두는 게 더 명시적이지 않나?** 명시 vs 일관성의 트레이드오프. 분기별 로직이 모두 다르면 그게 맞지만, **단순 값 매핑**이면 흩뿌리는 게 오히려 의도 흐림.

---

## 6. 다음 단계 (후보)

- **CargoTypePresets를 ScriptableObject로** — 4종 + Phase 3 modifier-chain까지 데이터 양 증가. 디자이너 튜닝 압박 커지면.
- **PresetTable 추상화** — 두 presets에 공통 분모(`IPresetTable<TKey, TValue>`) 뽑기. 단 셋 이상 생기기 전에는 over-abstraction.

---

## 7. 파일 변경 요약

- 신규: `F1/Assets/Scripts/Tires/TireTypePresets.cs`
- 수정: `F1/Assets/Scripts/Tires/TireSystem.cs` (5 switch → 1 룩업 + `CurrentWearRate()` 추출)
