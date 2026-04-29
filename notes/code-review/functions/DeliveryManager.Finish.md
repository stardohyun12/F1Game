# DeliveryManager.Finish

**스크립트:** [[DeliveryManager]]
**파일:** `Assets/Scripts/DeliveryManager.cs:Finish()`
**private**

---

## 호출되는 곳 (incoming)

- [[DeliveryManager.OnDeliveryReached]] — startReturn 없을 때 즉시
- [[DeliveryManager.Update]] — startReturn 4m 이내 도달

---

## 호출하는 것 (outgoing)

- `finished = true`
- `returning = false`
- **OnAllCompleted UnityEvent Invoke** → [[RunController]] 구독자 (`EndRun(true)`)

---

## 무엇을 하나

런의 배달 단계 종료. RunController가 정산 트리거.

---

## 코드 핵심

```csharp
void Finish() {
    finished = true;
    returning = false;
    OnAllCompleted?.Invoke();
    Debug.Log("DeliveryManager: all deliveries complete + returned.");
}
```

---

## 관련 흐름

- [[Flow.DeliveryComplete]]

---

## 주의

- `finished = true` 후엔 Update 가드(`!returning || finished`)에 막혀 추가 호출 X
- OnAllCompleted 구독자가 RunController 외에도 있다면 모두 알림 받음 (UnityEvent 패턴)
