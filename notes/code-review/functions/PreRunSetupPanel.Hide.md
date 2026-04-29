# PreRunSetupPanel.Hide

**스크립트:** [[PreRunSetupPanel]]
**파일:** `Assets/Scripts/Order/PreRunSetupPanel.cs:Hide()`

---

## 호출되는 곳 (incoming)

- [[PreRunSetupPanel.OnConfirmClicked]] — 출발
- [[PreRunSetupPanel.OnCancelClicked]] — 취소
- 외부 (필요 시)

---

## 호출하는 것 (outgoing)

- `panelRoot.SetActive(false)`

---

## 무엇을 하나

패널 비활성. 카드는 destroy **안** 함 (재사용 위해 보존).

---

## 코드 핵심

```csharp
public void Hide() {
    isOpen = false;
    if (panelRoot != null) panelRoot.SetActive(false);
}
```

---

## 주의

- **카드 영구 보존** — `cardsSpawned = true` flag로 다음 Show 시 재사용
- prefab 디자인 변경 후 Play 재시작 필요 (런타임 인스턴스는 옛 prefab 기반)
