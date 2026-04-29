# RunController.OpenBoard

**스크립트:** [[RunController]]
**파일:** `Assets/Scripts/Order/RunController.cs:OpenBoard()`
**private**

---

## 호출되는 곳 (incoming)

- `Start()` — 게임 시작 시
- [[RunController.RestartFromBoard]] — ResultPanel "다시" 버튼

---

## 호출하는 것 (outgoing)

- `Time.timeScale = 0f` — 일시정지
- `overviewCam.GoToOverview()`
- [[OrderBoard.Open]]
- 또는 `BeginRun(MakeFallbackJob())` (orderBoard 없을 때)

---

## 무엇을 하나

주문 보드 진입 흐름. **timeScale 0 + 오버뷰 카메라 + 보드 열기.**

---

## 코드 핵심

```csharp
void OpenBoard() {
    // OrderBoard 없으면 즉시 BeginRun (timeScale 영구 0 방지 — 세션 14 fix)
    if (orderBoard == null) {
        Time.timeScale = 1f;
        BeginRun(MakeFallbackJob());
        return;
    }
    Time.timeScale = 0f;
    if (overviewCam != null && !overviewCam.IsInOverview)
        overviewCam.GoToOverview();
    orderBoard.Open();
}
```

---

## 관련 흐름

- [[Flow.GameStart]] — 첫 진입

---

## 주의

- **timeScale 0 set은 여기서만**. BeginRun에서 1로 복귀 (overviewCam.GoToFollow 콜백)
- 안전장치: OrderBoard 없으면 영구 정지 안 되게 fallback
