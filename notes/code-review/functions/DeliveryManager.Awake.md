# DeliveryManager.Awake

**스크립트:** [[DeliveryManager]]
**파일:** `Assets/Scripts/DeliveryManager.cs:Awake()`

---

## 호출되는 곳 (incoming)

- Unity Engine (씬 로드 직후 1회)

---

## 호출하는 것 (outgoing)

- `FindFirstObjectByType<ArcadeCarController>()` — 씬에서 차 1회 탐색
- `carCache = c.transform` — Transform reference 저장 (private 필드)

---

## 무엇을 하나

차 Transform reference를 1회 캐시. [[DeliveryManager.Update]]의 매 프레임 `FindFirstObjectByType` 비용 제거.

late spawn (DM Awake가 Car Awake보다 빠른 경우) 또는 respawn에는 carCache가 null일 수 있음 — 이 경우 Update의 fallback (`if (carCache == null) carCache = FindActiveCar();`)이 처리.

---

## 코드 핵심

```csharp
Transform carCache;

void Awake() {
    var c = FindFirstObjectByType<ArcadeCarController>();
    if (c != null) carCache = c.transform;
}
```

---

## 관련 흐름

- [[Flow.GameStart]] — 씬 로드 시 차/매니저 동시 spawn

---

## 주의

- `FindFirstObjectByType`는 비활성 오브젝트 미포함 (기본 동작). 차 GameObject가 Awake 시점에 비활성이면 carCache=null → Update fallback 경로 진입
- Unity의 `Transform == null` 오버로드 덕분에 차가 destroy되면 carCache가 자동으로 null처럼 평가 → 다음 Update에서 fallback이 새 인스턴스 캐시
- 단일 차량 가정. 다중 차량 도입 시 RunController가 active car Transform을 주입하는 구조로 변경 필요 (현재 MVP 스코프 밖)

---

## 도입 이력

- cycle 1 Task Y (자율 루프, 2026-04-30), commit `f960949`
- 이전: `FindActiveCar()`를 Update 매 프레임 호출 → 씬 전체 스캔 비용 누적
