---
script: F1/Assets/Scripts/DeliveryManager.cs
category: root
phase_applied: [phase1, phase4]
tags: [code/root, code/delivery]
linecount: 121
signature: class DeliveryManager
last_synced: 2026-05-12
---

# DeliveryManager

> 한 줄: 활성 배달지 N개를 추적하고 도달 시 다음 지점으로 전진시키는 **게임 목적의 단순한 표현**.

## 책임 (Single Responsibility)
- **활성 배달지 결정** — `OrderJob.pointCount`만큼 앞에서부터 활성. 나머지는 숨김.
- **현재 인덱스 추적** — `CurrentIndex`, `TotalCount`, `Returning`, `Finished` 상태 노출.
- **완료 이벤트 발행** — `OnDeliveryCompleted (UnityEvent<DeliveryPoint>)` + Phase 4 AudioCueBus 발행.

## 의존 (들어오는 — 누가 나를 부르는가)
- [[Core]] — `Services.Delivery`로 등록.
- [[RunController]] — `Services.Delivery`, `OnDeliveryReached` 호출.
- [[DeliveryProgressHUD]] — 진행도 표시 (B3 변경 감지 적용).
- [[DeliveryHUD]] — 화살표용 `CurrentTarget` 조회.
- [[DeliveryPointMarker]] — 활성 표시 색 갱신.

## 의존 (나가는 — 내가 부르는 것)
- [[DeliveryPoint]] — `SetCompleted()` 호출.
- [[AudioCueBus]] — `AudioCueId.DeliveryCompleted` 발행 (Phase 4).

## 관련 결정
- [[notes/decisions-log#D-040]] 컨셉 전환 — 레이싱 → 현실 배송. DeliveryManager가 *목적*의 코드 표현.
- [[notes/decisions-log#D-051]] F1 복귀 — 배달은 수단, F1 복귀가 목적.

## 관련 학습
- [[phase1-dip-service-locator]] — Services.Delivery로 등록.
- [[phase4-audio-cue-bus]] — 발행 측 한 줄 추가 예제.

## 핫스팟 (변경 가능성 높음)
- **현재 안정** — 121줄, 단순. 변경 빈도 낮음.
- 향후 "배달지 무작위 순서", "동시 N개 활성" 같은 요구 들어오면 손 댐.
