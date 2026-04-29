# OrderCardUI Pointer 이벤트

**스크립트:** [[OrderCardUI]]
**파일:** `Assets/Scripts/Order/OrderCardUI.cs`
**Unity 이벤트:** IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler

---

## OnPointerEnter / OnPointerExit

### 호출되는 곳 (incoming)
- Unity EventSystem (마우스 카드 위 진입/이탈)

### 호출하는 것 (outgoing)
- [[OrderBoard.NotifyHoverEnter]] (Enter) / [[OrderBoard.NotifyHoverExit]] (Exit)

### 코드
```csharp
public void OnPointerEnter(PointerEventData e) => OrderBoard.NotifyHoverEnter(cardIndex);
public void OnPointerExit(PointerEventData e) => OrderBoard.NotifyHoverExit(cardIndex);
```

---

## OnPointerClick

### 호출되는 곳 (incoming)
- Unity EventSystem (마우스 카드 클릭)

### 호출하는 것 (outgoing)
- `onCardClicked?.Invoke(cardIndex)` — Bind에서 등록한 람다 (= [[OrderBoard.OnCardClicked]])

### 코드
```csharp
public void OnPointerClick(PointerEventData e) {
    onCardClicked?.Invoke(cardIndex);
}
```

---

## 의존성

- 카드 root에 **Image (raycast target)** + **CanvasScaler** 위 Canvas + **GraphicRaycaster** + 씬에 EventSystem 필요
- prefab에 위 셋업 모두 됨

---

## 관련 흐름

- [[Flow.HoverPropagation]] — Enter/Exit
- [[Flow.GameStart]] — Click
