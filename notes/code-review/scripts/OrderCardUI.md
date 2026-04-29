# OrderCardUI

**파일:** `Assets/Scripts/Order/OrderCardUI.cs`
**부착 위치:** `Assets/Prefabs/UI/OrderCard.prefab` root
**역할:** 주문 카드 prefab 컴포넌트 — 데이터 바인딩 + Pointer 이벤트만 담당

---

## 인스펙터 입력 (prefab에 와이어링)

### Background
- **bg** (Image) — 카드 전체 배경. 호버 시 색 변경
- **colorStrip** (Image) — 좌측 세로 색상 띠 (job.color)
- **hoverStrip** (Image) — 우측 강조 띠 (호버 시 알파)

### Title
- **titleText** (TMP_Text)

### Stat Rows (6개)
- **statRows[6]** — `OrderCardStatRow { TMP_Text label, TMP_Text value }`
- 순서: 지점 / 시간 / 보상 / 무게 / 화물 / 포장

### Tip
- **tipText** (TMP_Text) — 멀티라인. 운전 가이드 + 포장 가이드

### 시각 튠
- baseBgColor, hoverBgColor, hoverAlpha, hoverStripAlpha

---

## 함수

### public API
- [[OrderCardUI.Bind]] — 1회 데이터 set
- [[OrderCardUI.SetHovering]] — 호버 비주얼 토글

### Unity 이벤트 (Pointer)
- [[OrderCardUI.OnPointerEnter]] → [[OrderBoard.NotifyHoverEnter]]
- [[OrderCardUI.OnPointerExit]] → [[OrderBoard.NotifyHoverExit]]
- [[OrderCardUI.OnPointerClick]] → onCardClicked 콜백

### 내부 helper
- `SetStatRow(idx, label, val)` — 6행 중 하나에 텍스트 set

---

## 협력 객체

- [[OrderBoard]] — 정적 콜백 통해 호버/클릭 통지

---

## 관련 흐름

- [[Flow.GameStart]] — Bind 호출
- [[Flow.HoverPropagation]] — Pointer 이벤트

---

## 데이터 흐름 한 줄

OrderBoard.SpawnCards → Instantiate → [[OrderCardUI.Bind]](idx, job, onCardClicked) → 1회 텍스트 set, 이후 SetHovering만 호출됨.

---

## 보조 타입

```csharp
[Serializable]
public class OrderCardStatRow {
    public TMP_Text label;
    public TMP_Text value;
}
```

prefab에서 인스펙터로 6쌍 (지점/시간/보상/무게/화물/포장) 와이어링.
