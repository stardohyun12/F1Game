# RunController.RestartFromBoard

**스크립트:** [[RunController]]
**파일:** `Assets/Scripts/Order/RunController.cs:RestartFromBoard()`

---

## 호출되는 곳 (incoming)

- ResultPanel "다시" 버튼 (정산 화면)

---

## 호출하는 것 (outgoing)

- `resultPanel.Hide()`
- [[RunController.OpenBoard]] → [[OrderBoard.Open]]

---

## 무엇을 하나

정산 화면 닫고 새 런 시작 (보드부터 다시).

---

## 코드 핵심

```csharp
public void RestartFromBoard() {
    if (resultPanel != null) resultPanel.Hide();
    OpenBoard();
}
```

---

## 관련 흐름

- [[Flow.DeliveryComplete]] — 정산 후 재시작
- [[Flow.TimeOver]] — 실패 후 재시작
