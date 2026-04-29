# OrderCardUI.Bind

**스크립트:** [[OrderCardUI]]
**파일:** `Assets/Scripts/Order/OrderCardUI.cs:Bind(int, OrderJob, Action<int>)`

---

## 호출되는 곳 (incoming)

- [[OrderBoard.Open]] → SpawnCards → Instantiate(cardPrefab) → Bind

---

## 호출하는 것 (outgoing)

- 모든 텍스트 set (titleText, statRows[0..5], tipText)
- colorStrip.color = job.color

---

## 무엇을 하나

카드 인스턴스에 1회 데이터 set + 클릭 콜백 등록.
이후 호버는 [[OrderCardUI.SetHovering]]만 호출됨 (다시 Bind 안 함).

---

## 코드 핵심

```csharp
public void Bind(int cardIndex, OrderJob job, Action<int> onCardClicked) {
    this.cardIndex = cardIndex;
    this.job = job;
    this.onCardClicked = onCardClicked;

    if (titleText != null) titleText.text = job.displayName;
    if (colorStrip != null) colorStrip.color = job.color;
    if (bg != null) bg.color = baseBgColor;

    SetStatRow(0, "지점", $"{job.pointCount}곳");
    SetStatRow(1, "시간", job.timeLimitSec > 0f
        ? $"{Mathf.RoundToInt(job.timeLimitSec / 60f)}분 ..."
        : "무제한");
    string rewardStr = job.timeBonus > 0
        ? $"₩{job.baseReward:N0} + ₩{job.timeBonus:N0}"
        : $"₩{job.baseReward:N0}";
    SetStatRow(2, "보상", rewardStr);
    SetStatRow(3, "무게", job.WeightLabel);
    SetStatRow(4, "화물", CargoTypePresets.DisplayName(job.cargoType));
    SetStatRow(5, "포장", PackingTierHelper.DisplayName(job.defaultPacking));

    if (tipText != null) tipText.text = $"→ {CargoTypePresets.DriveTip(...)}\n→ ...";
}
```

---

## 관련 흐름

- [[Flow.GameStart]] — 카드 인스턴스화 시점
