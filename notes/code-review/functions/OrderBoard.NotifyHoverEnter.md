# OrderBoard.NotifyHoverEnter / NotifyHoverExit

**스크립트:** [[OrderBoard]]
**파일:** `Assets/Scripts/Order/OrderBoard.cs:NotifyHoverEnter(int) / NotifyHoverExit(int)`
**정적 메서드** — 인스턴스 ref 없이 호출 가능

---

## 호출되는 곳 (incoming)

- [[OrderCardUI.OnPointerEnter]] → NotifyHoverEnter
- [[OrderCardUI.OnPointerExit]] → NotifyHoverExit

---

## 호출하는 것 (outgoing)

- `_hoverTarget?.OnCardHoverEnter(idx)` 또는 `OnCardHoverExit(idx)`

---

## 무엇을 하나

OrderCardUI prefab 인스턴스가 OrderBoard 인스턴스를 직접 참조하지 못 해도 호버 통지 가능하게 하는 **정적 라우팅**.

`_hoverTarget`은 OrderBoard.Awake에서 `this`로 set, OnDestroy에서 cleanup.

---

## 코드 핵심

```csharp
static OrderBoard _hoverTarget;

public static void NotifyHoverEnter(int cardIndex) {
    if (_hoverTarget != null) _hoverTarget.OnCardHoverEnter(cardIndex);
}
public static void NotifyHoverExit(int cardIndex) {
    if (_hoverTarget != null) _hoverTarget.OnCardHoverExit(cardIndex);
}

void Awake() { _hoverTarget = this; ... }
void OnDestroy() { if (_hoverTarget == this) _hoverTarget = null; }
```

---

## 왜 정적인가?

OrderCardUI는 prefab → 런타임 인스턴스화 → OrderBoard 인스턴스 ref가 인스펙터로 미리 set 안 됨. Bind에서 콜백 받아도 되지만, **호버는 매 프레임 발생**해서 람다 캡처 부담 크고, 정적 라우팅이 더 깔끔.

---

## 한계

- **단일 인스턴스 가정** — 멀티 OrderBoard 시 마지막 Awake가 winner
- 현재 사용에선 OK. 멀티 씬 ↔ DontDestroyOnLoad는 별도 검토 필요
