# Phase 4 — AudioCueBus (Event Bus / Mediator)

> 목적: `AudioManager.HookEvents`의 OCP 위반(리뷰 Tier 2-B) 해소.
> 작성: 2026-05-11 (세션 19). 도현 학습.

---

## 1. 진단 — "구독자가 발행자를 직접 안다" 안티패턴

리팩터 전 `AudioManager.cs:86-102` (Phase 1 적용 후에도 잔존):

```csharp
void HookEvents()
{
    var cargo = Services.Cargo as CargoBox;
    if (cargo != null)
    {
        cargo.OnImpact  += OnCargoImpact;
        cargo.OnTotaled += OnCargoTotaled;
    }
    if (Services.Delivery != null)
        Services.Delivery.OnDeliveryCompleted.AddListener(_ => PlayClip(deliveryClip));
    var ups = Services.Upgrade as UpgradeSystem;
    if (ups != null)
        ups.OnUpgradePurchased += _ => PlayClip(upgradeBoughtClip);
}
```

### 왜 문제인가

| 원칙 | 위반 |
|---|---|
| **OCP** | 새 SFX 후크 추가 = `AudioManager.cs` 수정. 닫혀 있어야 할 모듈이 매번 열림. |
| **DIP (부분)** | Phase 1으로 `Services.Cargo`(인터페이스)는 받았지만, **이벤트 풍부한 API가 인터페이스에 없어** `as CargoBox` concrete cast로 되돌아감. |
| **응집** | AudioManager가 "오디오 인프라" 책임 + "어떤 시스템이 어떤 사운드를 갖는지 매핑" 책임 둘 다 짊어짐. |

세션 17 리뷰가 명시: "Phase 1으로 Find는 사라졌지만 후크 등록 위치는 여전히 AudioManager가 시스템들을 직접 알고 있음."

---

## 2. 패턴 — Event Bus (Pub/Sub Mediator)

### 아이디어

**발행자(시스템)** 와 **구독자(AudioManager)** 사이에 정적 중계자를 둔다.
시스템은 "내가 어떤 cue를 발행한다"만 안다. AudioManager는 "어떤 cue가 들어오면 어떤 clip을 재생한다"만 안다.
서로의 존재는 모른다.

```
[CargoBox]          [AudioCueBus]              [AudioManager]
  Publish ──────────► (id → handlers map) ─────► Subscribe
                                                 (CueId → clip)
```

### 구현 (`AudioCueBus.cs`)

```csharp
public enum AudioCueId {
    CargoImpact, CargoTotaled, DeliveryCompleted,
    UpgradePurchased, RunResultSuccess, RunResultFail,
}

public static class AudioCueBus
{
    static readonly Dictionary<AudioCueId, Action<float>> _handlers = new();

    public static void Subscribe(AudioCueId id, Action<float> h) { ... }
    public static void Unsubscribe(AudioCueId id, Action<float> h) { ... }
    public static void Publish(AudioCueId id, float value = 1f) {
        if (_handlers.TryGetValue(id, out var h)) h?.Invoke(value);
    }
    public static void Clear() => _handlers.Clear();
}
```

시그니처를 `(id, float)`로 통일 — value는 cue별 의미를 가진다(예: `CargoImpact`는 speedKmh, 의미 없는 cue는 0/1).
복수 시그니처 지원하려면 제네릭 채널로 가야 하나, MVP 스코프에선 단일 float로 충분.

### 발행 측 (시스템들)

```csharp
// CargoBox.cs
OnImpact?.Invoke(speedKmh);                   // 게임 로직(IntegrityHUD/FollowCamera)
AudioCueBus.Publish(AudioCueId.CargoImpact, speedKmh);  // 오디오

// DeliveryManager.cs
OnDeliveryCompleted?.Invoke(p);
AudioCueBus.Publish(AudioCueId.DeliveryCompleted);

// UpgradeSystem.cs
OnUpgradePurchased?.Invoke(def.effect);
AudioCueBus.Publish(AudioCueId.UpgradePurchased);
```

**의도적 이중 발행**: 자체 이벤트(`OnImpact`)는 IntegrityHUD/FollowCamera가 게임 로직용으로 구독 중이라 유지. AudioCueBus는 **오디오 전용 채널**로 분리. 시스템이 사라져도 오디오는 cue 안 받으면 그만.

### 구독 측 (AudioManager)

```csharp
Action<float> _cargoImpactH, _cargoTotaledH, ...; // Unsubscribe용 핸들러 보존

void SubscribeCues() {
    _cargoImpactH   = v => PlayClip(cargoImpactClip, Mathf.Clamp01(v / 50f));
    _cargoTotaledH  = _ => PlayClip(cargoTotaledClip);
    // ... 6개

    AudioCueBus.Subscribe(AudioCueId.CargoImpact,  _cargoImpactH);
    // ...
}

void OnDestroy() { UnsubscribeCues(); ... }
```

`Action<float>`을 필드로 보존하는 이유: 람다를 인라인으로 두 번 만들면 다른 인스턴스라 `Unsubscribe`가 작동 안 함. 람다를 한 번 만들어 캡처해야 정확히 해제 가능.

---

## 3. 효과

| 항목 | Before | After |
|---|---|---|
| 새 SFX 후크 추가 | `AudioManager.HookEvents` 직접 수정 + concrete cast | enum 항목 + Publish 한 줄 + Subscribe 한 줄 |
| AudioManager가 직접 참조하는 시스템 | CargoBox, DeliveryManager, UpgradeSystem | **0개** |
| concrete cast (`as CargoBox`) | 2개 | 0개 |
| 시스템 제거 시 AudioManager 영향 | 컴파일 에러 | 0 (조용히 cue 없음) |

### "AudioManager 한 줄 수정도 결국 수정 아닌가?"

본질은 **결합 방향**이다.
- Before: AudioManager가 *어떤 시스템이 어떤 이벤트를 갖는지* 알아야 함. 시스템 N개 → AudioManager가 N개의 API 모양 학습.
- After: AudioManager는 *cue 카탈로그(enum)*만 안다. 시스템은 0개도, 1000개도 AudioManager는 같은 코드.

새 시스템 추가 시:
1. 그 시스템이 cue 발행 결정 → 자기 코드에 한 줄.
2. AudioManager는 그 시스템 자체를 모름. 새 clip 매핑은 "오디오 정책" 영역이라 AudioManager 수정이 자연스러움.

---

## 4. 패턴 일반화 — 언제 이 패턴을 쓰는가

| 상황 | 적합 |
|---|---|
| 발행자 N, 구독자 1 (또는 소수) — 다대일/다대다 | ✓ |
| 발행자와 구독자 라이프사이클 다름 | ✓ (자체 이벤트는 발행자가 사라지면 끊김) |
| 발행자가 구독자 존재 자체를 몰라도 OK | ✓ |
| 발행자가 구독자 응답을 받아야 함 | ✗ (이건 그냥 메서드 호출이 낫다) |
| 발행 순서나 결과 순서가 게임 로직에 중요 | △ (Dictionary 순회 순서 보장 약함) |

오디오, 통계 수집, 도전과제, 튜토리얼 트리거 등이 전형 적합 영역.

---

## 5. 함정 — 학습 포인트

1. **이중 발행이 안티패턴인가?** 아니다. 같은 사건이 게임 로직(자체 이벤트)과 오디오(cue) 둘 다 트리거하면 두 채널 발행이 정확한 모델. 채널을 하나로 합치려는 게 잘못된 단순화.
2. **AudioCueBus.Clear()는 왜 있나?** Unity 도메인 리로드(스크립트 컴파일 후)에서 static 필드가 살아남으면 stale 핸들러가 남는다. Editor에서 종종 발생. `RuntimeInitializeOnLoadMethod`로 자동 호출하는 게 향후 보강 포인트.
3. **테스트성**: cue 시그니처가 단순해서 단위 테스트가 쉽다. `AudioCueBus.Publish(CueId.CargoImpact, 30f)` 한 줄로 핸들러 호출 검증.

---

## 6. 다음 단계 (후보)

- **테스트 추가** — AudioCueBus만 격리된 NUnit 테스트.
- **자동 Clear** — 도메인 리로드 시 정리. `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset() => AudioCueBus.Clear();`.
- **구독 누락 경고** — `enum` 모든 값이 적어도 한 번 Subscribe 되었는지 시작 시 점검.

---

## 7. 파일 변경 요약

- 신규: `F1/Assets/Scripts/Audio/AudioCueBus.cs` (enum + static class)
- 수정: `F1/Assets/Scripts/Audio/AudioManager.cs` (HookEvents → SubscribeCues / UnsubscribeCues)
- 수정: `F1/Assets/Scripts/Cargo/CargoBox.cs` (3곳 Publish 추가)
- 수정: `F1/Assets/Scripts/DeliveryManager.cs` (1곳 Publish 추가)
- 수정: `F1/Assets/Scripts/Upgrades/UpgradeSystem.cs` (1곳 Publish 추가)
