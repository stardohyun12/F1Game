# DeliveryManager

**파일:** `Assets/Scripts/DeliveryManager.cs`
**부착 위치:** DeliverySystem/DeliveryManager GameObject
**역할:** 배달 지점 진행 트래커 — 현재 타겟, 다음 활성, 도착 판정

---

## 인스펙터 입력

- **points** (List<DeliveryPoint>) — 모든 배달지
- **startReturn** (Transform, optional) — 마지막 배달 후 복귀 지점

---

## 외부 이벤트 (UnityEvent)

- **OnAllCompleted** — 마지막 도착 + 복귀 완료 시
- **OnDeliveryCompleted(DeliveryPoint)** — 매 배달 완료 시

[[RunController]]가 `Awake`에서 구독.

---

## public 상태

- `CurrentIndex` (int)
- `TotalCount` (int) — activeCount 또는 points.Count
- `Returning` (bool) — 복귀 모드?
- `Finished` (bool)
- `CurrentTarget` (DeliveryPoint)
- `ReturnTarget` (Transform)

---

## private state

- `currentIndex` (int)
- `returning`, `finished` (bool)
- `activeCount` (int) — -1 = 전체. RunController가 ActivateSubset에서 세팅
- `carCache` (Transform) — Awake에서 1회 캐시된 차 reference (cycle 1 Task Y, f960949)

---

## 함수

### public API
- [[DeliveryManager.ActivateSubset]] — 호출자: [[RunController.BeginRun]]
  - 앞에서부터 N개만 활성, 첫 번째 SetActive(true)
- [[DeliveryManager.OnDeliveryReached]] — 호출자: DeliveryPoint (트리거 진입)
  - 다음 점 활성 또는 returning 모드
- `SetPoints(list, retn)` — Editor convenience

### Unity 이벤트
- [[DeliveryManager.Awake]] — carCache 캐시 (Cycle 1 Task Y)
- `Start()` — activeCount 미설정 시 전체 활성
- [[DeliveryManager.Update]] — returning 상태에서만 startReturn 거리 검사 → [[DeliveryManager.Finish]]

### 내부 helper
- [[DeliveryManager.Finish]] — finished=true, OnAllCompleted Invoke
- `FindActiveCar()` — fallback only. Awake 캐시(carCache) 실패 시 Update에서 1회만 호출 (cycle 1 Task Y, f960949)

---

## 협력 객체

- DeliveryPoint — 배달지 트리거 (외부 → DM)
- [[RunController]] — UnityEvent 두 개 구독 (DM → RC)

---

## 관련 흐름

- [[Flow.GameStart]] — ActivateSubset 호출
- [[Flow.DeliveryComplete]] — OnDeliveryReached → OnAllCompleted

---

## 핵심 사항

- **Update 책임** — returning 상태에만 거리 검사. [[RunController.Update]] (시간)와 책임 분리
- **이벤트 결합** — UnityEvent 두 개로 RunController와 느슨한 결합. 직접 참조 X
- **✅ FindActiveCar Awake 캐시 적용 완료** — cycle 1 Task Y, commit `f960949`. 매 프레임 FindFirstObjectByType 제거. FindActiveCar는 Awake 실패(late spawn / respawn) 시 fallback 전용
