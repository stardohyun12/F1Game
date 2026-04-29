# Functions 인덱스

함수별 deep dive 25개. 각 함수에 호출 incoming/outgoing + 코드 핵심.

## 스크립트별 분류

### [[OrderBoard]]
- public: [[OrderBoard.Open]] / [[OrderBoard.Close]]
- 정적: [[OrderBoard.NotifyHoverEnter]]
- 콜백: [[OrderBoard.OnCardClicked]] / [[OrderBoard.OnCardHoverEnter]]

### [[OrderCardUI]]
- [[OrderCardUI.Bind]] / [[OrderCardUI.SetHovering]]
- [[OrderCardUI.OnPointerEnter]]

### [[PreRunSetupPanel]]
- public: [[PreRunSetupPanel.Show]] / [[PreRunSetupPanel.Hide]]
- 콜백: [[PreRunSetupPanel.OnConfirmClicked]] ★ / [[PreRunSetupPanel.OnCancelClicked]] / [[PreRunSetupPanel.OnCardClicked]]
- 내부: [[PreRunSetupPanel.SpawnCardsIfNeeded]]

### [[PackingCardUI]]
- [[PackingCardUI.Bind]] / [[PackingCardUI.SetSelected]]

### [[RunController]]
- public: [[RunController.BeginRun]] ★ / [[RunController.RestartFromBoard]]
- 내부: [[RunController.OpenBoard]] / [[RunController.EndRun]] / [[RunController.Update]]

### [[DeliveryManager]]
- public: [[DeliveryManager.ActivateSubset]] / [[DeliveryManager.OnDeliveryReached]]
- 내부: [[DeliveryManager.Update]] / [[DeliveryManager.Finish]]

---

## ★ 표시 = 핵심 결정 지점

- [[RunController.BeginRun]] — packingOverride 매개변수 (세션 16 후속 신규)
- [[PreRunSetupPanel.OnConfirmClicked]] — 버그 fix가 작동하는 결정적 함수

---

## 어떻게 탐색?

1. [[Flow.GameStart]] 따라가며 함수들 차례로 클릭
2. 또는 [[scripts/index]]에서 스크립트 단위로 진입
3. 또는 그래프 뷰에서 시각적으로 클릭
