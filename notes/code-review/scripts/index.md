# Scripts 인덱스

세션 16 후속(A) 리팩터로 6개 스크립트가 핵심 흐름을 구성.

## 책임별 분류

### Orchestrator (큰 그림)
- [[OrderBoard]] — 주문 보드 표시 + 카드 클릭 → 다음 단계
- [[PreRunSetupPanel]] — 포장 선택 모달 + 출발 트리거
- [[RunController]] — 런 lifecycle (시간/보상/결과)

### Prefab 컴포넌트 (재사용 단위)
- [[OrderCardUI]] — 주문 카드 (instanced 3장)
- [[PackingCardUI]] — 포장 카드 (instanced 4장)

### 데이터 트래커
- [[DeliveryManager]] — 배달지 진행 + UnityEvent 발행

---

## 결합 강도

```
RunController ─UnityEvent─ DeliveryManager
     ↑
     └─ 인스펙터 ref ─ OrderBoard ──정적 콜백── OrderCardUI
                              ↓
                         PreRunSetupPanel ──Button.onClick── PackingCardUI
                              ↓
                         (인스펙터 ref) RunController
```

- **정적 콜백** (OrderBoard._hoverTarget): UI prefab → orchestrator
- **UnityEvent** (DeliveryManager.On*): 데이터 트래커 → orchestrator
- **인스펙터 참조** (RunController.cargo, OrderBoard.runController, ...): orchestrator → 의존 객체

---

## "어디서 시작?"

코드 리뷰 첫 진입은 [[RunController]] → [[RunController.OpenBoard]] → [[OrderBoard.Open]] 순서가 자연.
