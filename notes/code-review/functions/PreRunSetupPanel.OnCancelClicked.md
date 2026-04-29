# PreRunSetupPanel.OnCancelClicked

**스크립트:** [[PreRunSetupPanel]]
**파일:** `Assets/Scripts/Order/PreRunSetupPanel.cs:OnCancelClicked()`

---

## 호출되는 곳 (incoming)

- cancelBtn.onClick

---

## 호출하는 것 (outgoing)

- [[PreRunSetupPanel.Hide]]
- [[OrderBoard.Open]] — 다시 주문 선택으로 (FindFirstObjectByType)

---

## 무엇을 하나

포장 선택 취소. 보드로 돌아감. **timeScale은 0 유지** (이미 OrderBoard.Open 직전 set 안 됨, 진입 전 0이었기 때문).

---

## 코드 핵심

```csharp
void OnCancelClicked() {
    Hide();
    var board = FindFirstObjectByType<OrderBoard>();
    if (board != null) board.Open();
}
```

---

## 관련 흐름

- [[Flow.Cancel]]

---

## 주의

- **OrderBoard.Open이 새 셔플** — 다시 같은 카드 안 나옴. 사용자 인지 필요할 수도
- timeScale 복귀 책임 추적: [[../code-review-questions]] 참고
