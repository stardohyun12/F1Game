# Phase 6 — 2팀 Peer Review 자명 채택 패치

> 목적: 팀 A(Architect, SOLID) + 팀 B(Performance/Readability) 독립 감사의 자명·저비용 발견 즉시 반영.
> 작성: 2026-05-11 (세션 19). 도현 학습.
> 통합 리포트: `notes/code-review/synthesis-2026-05-11.md`

---

## 0. 메타 — 왜 두 팀인가

한 시각만으로는 18개 발견을 만들 수 없다. 변경 비용(SOLID) vs 런타임 비용(GC/핫패스)은 **서로 안 보이는 차원**이다.
한 모델 안에서 둘을 동시에 짚으려 하면 어느 쪽도 깊이 못 본다 — 둘로 나눠 독립 감사하고 결과를 합치는 게 더 적은 시야로 더 많이 본다.

이번 사이클 비교:
- 팀 A: 9개 발견 (대부분 Order/Upgrade/Audio 도메인, 변경 비용 관점)
- 팀 B: 6 perf + 3 readability (대부분 HUD/렌더 핫패스, 런타임 관점)
- 겹치는 부분(ResultPanel, HUD 4종, Phase 1 일관성)이 신뢰도 신호.

---

## 1. 이번 패치에 들어간 것 (✅ 6건)

자명·저비용 항목만. 큰 변경(ResultPanel 분할, IRunController DTO, TextHUDBase, RewardCalculator 등)은 결정 대기 ⏸.

### A8. AudioCueBus 도메인 리로드 hook (10m)
`F1/Assets/Scripts/Audio/AudioCueBus.cs`. "Enter Play Mode without Domain Reload" 옵션 켜져 있으면 정적 `_handlers` Dictionary가 이전 플레이의 핸들러를 보존 → stale handler 누수.

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
static void ResetOnEnterPlayMode() => _handlers.Clear();
```

매 Play 진입 시 자동 리셋. Phase 4 자체 보강.

---

### B1. DeliveryHUD Canvas 캐시 (5m)
`F1/Assets/Scripts/DeliveryHUD.cs:114`. Update가 매 프레임 `GetComponentInParent<Canvas>()` 호출 — 부모 체인 traversal.

```csharp
Canvas cachedCanvas;
void Awake() { ...; cachedCanvas = GetComponentInParent<Canvas>(); }
// Update: var canvas = cachedCanvas; if (canvas == null) return;
```

런 중 부모 캔버스는 안 바뀜 → 1회 캐시. GC는 아니지만 60Hz의 무의미한 work 제거.

---

### B2. CountdownHUD 초 단위 변경 감지 (5m)
`F1/Assets/Scripts/Order/CountdownHUD.cs:96`. 매 프레임 `timeText.text = $"{m}:{s:D2}"` — string은 reference type, 매 프레임 alloc.

```csharp
int lastWholeSecond = int.MinValue;
// Update:
int totalSec = Mathf.Max(0, Mathf.FloorToInt(remaining));
if (totalSec != lastWholeSecond) {
    lastWholeSecond = totalSec;
    timeText.text = $"{totalSec / 60}:{totalSec % 60:D2}";
}
```

GC alloc 60Hz → 1Hz. 5분 런 기준 약 900KB → 15KB.

---

### B3. DeliveryProgressHUD 상태/인덱스 변경 감지 (5m)
`F1/Assets/Scripts/UI/DeliveryProgressHUD.cs:107,113`. 매 프레임 인덱스/총수 string 재생성. 변경 거의 없음.

```csharp
enum DisplayState { None, Finished, Returning, Normal }
DisplayState lastState; int lastIdx = -1, lastTotal = -1; string lastTargetLabel;

// 상태 또는 인덱스 변경 시만 text 갱신.
```

`ReferenceEquals`로 string ref 비교 — `OrderJob` 라벨이 같은 인스턴스면 alloc 0.

---

### B4. CargoWarningHUD static readonly Color + const EPS (5m)
`F1/Assets/Scripts/UI/CargoWarningHUD.cs:144`. 매 프레임 `new Color(...)` 4개. Color는 struct라 heap은 아니지만 매직 RGB가 핫패스에 박힘.

```csharp
static readonly Color WarnYellow = new Color(1f, 0.85f, 0.20f, 1f);
static readonly Color CritRed    = new Color(1f, 0.25f, 0.25f, 1f);
static readonly Color WarnBgBase = new Color(0.18f, 0.14f, 0.05f, 0.85f);
const float SafeEpsilon = 0.0001f;
```

가독성 + 매직넘버 통일(R3까지 부분 흡수). 색 튜닝 시 한 곳.

---

### A3. OrderBoard 정적 hover dispatch 콜백화 (30m)
`F1/Assets/Scripts/Order/OrderBoard.cs` + `OrderCardUI.cs`. 호버만 정적 `_hoverTarget` 라우터 — 같은 카드가 클릭은 콜백 패턴인데 호버만 hidden singleton.

Before:
```csharp
// OrderBoard
static OrderBoard _hoverTarget;
public static void NotifyHoverEnter(int idx) { _hoverTarget?.OnCardHoverEnter(idx); }

// OrderCardUI
public void OnPointerEnter(PointerEventData e) {
    OrderBoard.NotifyHoverEnter(cardIndex);  // 정적 결합
}
```

After:
```csharp
// OrderBoard
card.Bind(i, job, OnCardClicked, OnCardHoverEnter, OnCardHoverExit);

// OrderCardUI
public void Bind(int idx, OrderJob job,
    Action<int> onClick,
    Action<int> onHoverEnter = null,
    Action<int> onHoverExit = null) { ... }

public void OnPointerEnter(PointerEventData e) => onCardHoverEnter?.Invoke(cardIndex);
```

정적 필드 0, hidden singleton 0. 한 씬에 보드 2개 가능. 클릭과 동일한 콜백 시그니처라 학습 비용 0.

---

## 2. 일부러 보류한 것 (⏸ 다음 세션 결정)

- **A1** ResultPanel God class 분할 (6-10h)
- **A2** UpgradeEffect 도메인 누수 (3-5h)
- **A4** TextHUDBase Template Method 확장 (3-4h)
- **A5** RewardCalculator + CargoGradeBook SO (2-3h)
- **A6** UpgradeSystem 싱글톤 잔재 제거 (1-2h)
- **A7** IRunController + IRunSummary DTO (2-3h)
- **B5** DeliveryPointMarker SetColor 캐시 (15m)
- **R1** CargoWarningHUD 한국어 문자열 테이블화 (10m)
- **R2** ResultPanel 멘트 5단 if-사다리 (A1과 함께)

권장 묶음:
- **Phase 7 — Phase 1 마무리**: A6 + A7 + R3 + R1 정리 (4-5h, 한 세션).
- **Phase 8 — ResultPanel 분할**: A1 + R2 + A5 자연 흡수 (6-10h, 한 세션).
- **Phase 9 — TextHUDBase**: A4 + B2/B3 일부 이주 (3-4h, 한 세션).
- **Phase 10 (선택) — Domain Bus**: A2 (3-5h). 효과 개수 늘기 전 적기.
- **post-MVP**: A9 (CargoCatalog SO), 그 외 cosmetic.

---

## 3. 학습 포인트

1. **두 팀 구조의 가치는 "겹치는 곳 = 신뢰"**. ResultPanel을 A/B 둘 다 짚었다 → 가장 시급한 부채.
2. **자명 vs 결정 필요의 구분**. 비용 30분 이하 + 단일 파일 변경 + 컴파일/Play 통과면 자명. 다중 파일 + 인터페이스 변경 + 의존 그래프 흔드는 건 결정.
3. **핫패스 GC는 5분짜리 패치로 70~80% 줄어든다**. B2/B3 두 개로 매 프레임 string alloc 두 자릿수 → 한 자릿수.
4. **DIP 부분 적용은 위험**. Phase 1이 80%라는 건 호출자가 캐스팅으로 우회 중이라는 신호. 인터페이스를 "최소"로 만들면 호출자가 구상으로 내려간다. A7이 이 패턴의 교과서 사례 — 다음 세션 우선.

---

## 4. 파일 변경 요약

- 수정: `F1/Assets/Scripts/Audio/AudioCueBus.cs` (RuntimeInitializeOnLoadMethod hook)
- 수정: `F1/Assets/Scripts/DeliveryHUD.cs` (Canvas 캐시)
- 수정: `F1/Assets/Scripts/Order/CountdownHUD.cs` (초 단위 갱신)
- 수정: `F1/Assets/Scripts/UI/DeliveryProgressHUD.cs` (상태 캐시)
- 수정: `F1/Assets/Scripts/UI/CargoWarningHUD.cs` (static Color + EPS)
- 수정: `F1/Assets/Scripts/Order/OrderCardUI.cs` (Bind 시그니처 확장)
- 수정: `F1/Assets/Scripts/Order/OrderBoard.cs` (정적 hover 제거)

신규 문서: 이 파일 + `notes/code-review/synthesis-2026-05-11.md` + 팀 A/B 원본 리포트.
