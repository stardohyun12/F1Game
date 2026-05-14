# Team B — Performance & Readability 감사 (2026-05-11)

> 메모리/GC/핫패스/가독성. 팀 A(SOLID)와 독립. 재시도 — Hot path 위주, 발견 5~7개.
> 적용 완료 영역(중복 발견 금지): Phase 1 Service Locator, Phase 4 AudioCueBus, Phase 5 TireTypePresets.

---

## 1. 감사 대상

`F1/Assets/Scripts` 하위 ~27개 MonoBehaviour의 Update/FixedUpdate/LateUpdate 핫패스.
인벤토리 grep: `Update` 27건, `GetComponent*` 다수, `$"…"` interpolation 30+건.

---

## 2. 핫패스 메모리 발견

### B1. DeliveryHUD.Update 매 프레임 `GetComponentInParent<Canvas>()`
**위치:** `F1/Assets/Scripts/DeliveryHUD.cs:114` (Update)
**현상:**
```csharp
var canvas = GetComponentInParent<Canvas>();
Vector2 localPos;
RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, finalScreenPos,
    canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out localPos);
```
**비용:** `GetComponentInParent<T>`는 부모 체인 traversal — 매 프레임 60Hz면 초당 60회. 자체 GC는 없지만 트리 길수록 비례 비용. UI 캔버스는 런 중 부모가 안 바뀜.
**수정안:** Awake/OnEnable 1회 캐시:
```csharp
Canvas cachedCanvas;
void Awake() { rt = GetComponent<RectTransform>(); cachedCanvas = GetComponentInParent<Canvas>(); }
// Update 안: var canvas = cachedCanvas;
```
(Awake에 `cachedCanvas` 라인 추가 + 33줄 early-return 이후 캐시 갱신.)
**우선순위:** 핫

### B2. CountdownHUD.Update — `$"{m}:{s:D2}"` 매 프레임 alloc
**위치:** `F1/Assets/Scripts/Order/CountdownHUD.cs:96` (Update, 런 내내 활성)
**현상:** `timeText.text = $"{m}:{s:D2}";` — 매 프레임 string 생성.
**비용:** 1프레임당 ~40~80B GC alloc. 60Hz면 초당 ~3KB. 5분 런이면 ~900KB → Gen0 GC 빈도 증가.
**수정안:** 초가 바뀔 때만 갱신:
```csharp
int lastSecond = -1;
// Update:
int totalSec = (int)remaining;
if (totalSec != lastSecond) {
    lastSecond = totalSec;
    timeText.text = $"{totalSec/60}:{totalSec%60:D2}";
}
```
**우선순위:** 핫

### B3. DeliveryProgressHUD.Update — string interpolation 매 프레임
**위치:** `F1/Assets/Scripts/UI/DeliveryProgressHUD.cs:107,113`
**현상:**
```csharp
countText.text = $"{manager.TotalCount}/{manager.TotalCount}";   // 반환 상태
countText.text = $"{manager.CurrentIndex + 1}/{manager.TotalCount}";
```
**비용:** 값이 거의 안 바뀌는데 매 프레임 alloc + Text.SetText. 또 매 프레임 `theme.colorPanel` 등도 무조건 set.
**수정안:** 마지막 값 캐시 후 변경 시만 갱신:
```csharp
int lastIdx = -2, lastTotal = -2;
// 변경 시만 string 빌드 + text 할당.
```
**우선순위:** 핫

### B4. CargoWarningHUD.Update — `new Color()` 4개 매 프레임
**위치:** `F1/Assets/Scripts/UI/CargoWarningHUD.cs:144~145` (Update, 핫)
**현상:**
```csharp
Color warnYellow = new Color(1f, 0.85f, 0.20f, 1f);
Color critRed = new Color(1f, 0.25f, 0.25f, 1f);
```
**비용:** Color는 struct라 heap alloc은 없음 — 다만 매 프레임 6floats × 2 stack init. 진짜 문제는 가독성 / 매직 RGB가 핫패스에 박힘. 추가로 `cargo.CurrentLongG / Mathf.Max(0.0001f, ...)` 4회 — 함수 호출 OK지만 0.0001f 매직넘버 중복.
**수정안:** `static readonly` field로 끌어올림:
```csharp
static readonly Color WarnYellow = new Color(1f, 0.85f, 0.20f, 1f);
static readonly Color CritRed = new Color(1f, 0.25f, 0.25f, 1f);
const float EPS = 0.0001f;
```
**우선순위:** 중 (할당 비용은 미미하나 매직넘버 핫패스 거주)

### B5. DeliveryPointMarker.Update — 모든 마커가 매 프레임 sin 계산
**위치:** `F1/Assets/Scripts/Order/DeliveryPointMarker.cs:46`
**현상:** 활성 마커마다 `Mathf.Sin(Time.unscaledTime * 4f)` + `ApplyColor` 호출. `ApplyColor`는 내부에서 material property 변경 → 매 프레임 SetColor.
**비용:** 마커 N개 × 60Hz. N=10이면 600회/s sin+SetColor. SetColor가 instanced material 깨우면 batching 손실.
**수정안:**
- 활성 마커가 1개라는 가정이면 `dp.IsActive`인 것만 sin 계산하고 나머지(else 분기 + Completed)는 색이 안 바뀌므로 캐시된 마지막 색과 비교해 변경 시만 ApplyColor.
- 이미 분기는 있음 → `lastAppliedColor` 캐시 + `Color.Approximately` 비교만 추가.
**우선순위:** 중

### B6. CargoView.Update — 매 프레임 null 체크만 (false alarm 가능)
**위치:** `F1/Assets/Scripts/Cargo/CargoView.cs:96`
**현상:**
```csharp
if (view == null) return;
if (view.texture == null) { TryLoadRT(); ... }
```
**비용:** RT가 한 번 잡히면 그 뒤 모든 호출은 그냥 두 null 비교 후 리턴 — 거의 0. 하지만 **Update 자체가 등록돼 있는 게 비용**. RT 잡힌 후 `enabled = false`로 끄는 게 더 깨끗.
**수정안:** `if (renderTexture != null) { view.texture = renderTexture; enabled = false; return; }`
**우선순위:** 저

---

## 3. 가독성 발견

### R1. CargoWarningHUD `Source` 분기 — switch expression 안에 한국어 문자열 4종 인라인
**위치:** `F1/Assets/Scripts/UI/CargoWarningHUD.cs:133~140`
**현상:** `"급제동" / "급가속" / "◀ 코너" / 코너 ▶" / "스핀" / "경사"` 모두 인라인 매직 문자열. 로컬라이즈/문구 수정 시 분산.
**수정안:** `static readonly string[]` 또는 `const` 6개를 클래스 상단에 모음. 한 화면에서 표를 보듯 정렬되면 변경 비용↓.
**우선순위:** 중

### R2. ResultPanel 멘트 5단계 if-사다리 (`ResultPanel.cs:446~451`)
**현상:**
```csharp
if (i >= 95f) return $"\"{name}, 한 점 흠 없이 도착했음.\"";
if (i >= 80f) return $"\"{name}, 잘 가져왔음.\"";
...
```
**가독성:** 임계값 5개 + 멘트 5개가 코드에 박힘. Phase 5 TireTypePresets 패턴처럼 `(threshold, msg)` 테이블로 추출하면 readability + 디자이너 수정 가능성↑.
**수정안:**
```csharp
static readonly (float min, string template)[] Tiers = {
    (95f, "\"{0}, 한 점 흠 없이 도착했음.\""),
    (80f, "\"{0}, 잘 가져왔음.\""),
    (50f, ...),
};
```
**우선순위:** 저 (Phase 4 / ResultPanel 분할 시 함께)

### R3. `Mathf.Max(0.0001f, ...)` 매직넘버가 여러 파일에서 반복
**위치:** `CargoWarningHUD.cs:117~120`, 다수
**현상:** 0-divide 가드 상수가 파일마다 별도. 통일된 `MathUtil.Epsilon` 같은 const 없음.
**수정안:** 작은 `MathConst.SafeEpsilon = 0.0001f` 정의. 또는 그냥 각 클래스 상단 `const float EPS`.
**우선순위:** 저

---

## 4. 잘된 점

- **Phase 1 Service Locator**: Update 핫패스에 `Find` 흔적 없음. 캐시는 Awake로 모두 이동.
- **CargoView.Update**가 미니멀 — RT 로드만 처리, 나머진 캐시.
- **SpeedometerHUD.Update**의 `int.ToString()`은 string interpolation 안 씀 (allocation 적음). 단, 매 프레임 같은 값이면 캐시 추가 시 더 줄어듦.
- **RunController.Update**: `Time.deltaTime` 누산 + early return. 본질적 hot work만 함.
- **DriftVfxController** 등은 Awake에서 GetComponentInParent fallback 후 캐시.

---

## 5. 한 줄 요약

핫패스 GC alloc 3건(B1~B3)이 명백한 우선 처리 대상 — DeliveryHUD canvas 캐시 + CountdownHUD/ProgressHUD "값 바뀔 때만 text 갱신" 패턴 도입으로 런 중 GC 압력 ~70~80% 감소 예상, 작업량은 각 5분 내외.
