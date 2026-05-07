# Programmer Report — Cycle 2 (검증 전용, 2026-04-30 main)

## 커밋 f960949 정적 검증

| 체크 | 상태 | 비고 |
|---|---|---|
| 컴파일 가능성 | ✅ | `FindFirstObjectByType<ArcadeCarController>()` Unity 6 표준 API. 괄호/세미콜론/타입 모두 정상. using 추가 불필요. |
| Unity null check 동작 (UnityEngine.Object) | ✅ | `Transform`은 `UnityEngine.Object` 파생 → `==/!=` 오버로드로 destroyed 객체를 null로 인식. `if (carCache == null) carCache = FindActiveCar()` 안전. |
| Late spawn fallback | ✅ | DeliveryManager Awake가 ArcadeCarController spawn보다 빠르면 carCache=null → Update에서 매 프레임 fallback FindActiveCar 호출 (차 잡힐 때까지). 동작 정확. |
| Respawn 시 갱신 | ✅ | 차 destroy 시 `carCache == null` 트루 → fallback이 새 인스턴스 자동 캐시. |
| Update 분기 동등성 | ✅ | f960949 전후 관찰 가능 동작 동일. carCache 비어있을 때만 FindActiveCar 1회, 그 외엔 캐시 재사용. |
| 단일 차량 가정 의존성 | 변동 없음 | 원본 `FindFirstObjectByType` 동일 가정. 다중 차량 도입 시 두 코드 모두 재설계 필요 (이번 커밋이 만든 회귀 아님). |

### 시나리오 walk-through
- **A. 정상 spawn 순서 (Car Awake → DM Awake)**: Awake에서 carCache 즉시 채워짐. Update의 `if (carCache == null)` false → FindActiveCar 미호출 (성능 이득 = 매 프레임 -1 scene scan).
- **B. Late spawn (DM Awake → Car Awake)**: carCache=null. returning 진입 시 Update가 fallback 호출, 차 잡히면 캐시. returning 미진입이면 호출조차 없음 (early return).
- **C. Run 중 차 destroy/respawn**: carCache가 destroyed Transform → Unity 오버로드로 `== null` true → 다음 Update에서 fallback이 새 차 캐시. 기능 회귀 없음.
- **D. 컴포넌트만 add/remove (GameObject 유지)**: 캐시는 `transform`이라 영향 없음. ArcadeCarController 분리돼도 transform 살아있으면 거리 검사 계속됨 — 이건 원본도 동일했고 검증 범위 밖.

## 발견 이슈

### 🔴 Critical
- 없음.

### 🟡 Major
- 없음. 9줄 변경, 동작 동등, late spawn 보호 유지. Cycle 1 feedback의 "carCache stale 참조 가능성" 우려는 Unity의 `==/!=` 오버로드로 해소됨 (위 시나리오 C 참조).

### 🟢 Minor
- (기존 cycle 1 발견 그대로 유지) `FindActiveCar()` 메서드는 fallback 한 줄 외 호출처 없음 → inline 가능하나 가독성 손실 vs 코드 라인 1 줄어듬, 의미 없는 정리.
- (신규) DeliveryManager가 ArcadeCarController 단일 인스턴스 가정. 향후 다중 차량 도입 시 RunController가 active car Transform을 주입하는 구조로 바꾸는 게 더 깨끗함 — 현재 MVP 스코프 밖, ideas-parking 후보.

## Obsidian vault drift

- **`notes/code-review/scripts/DeliveryManager.md`**: **drift 있음**.
  - L51: `FindActiveCar()` — ⚠ 매 프레임 FindFirstObjectByType (캐시 권장)` — f960949에서 캐시 적용됐으므로 표현 갱신 필요.
  - L73: `**개선 후보** — FindActiveCar() Awake 캐시` — 이미 적용됐으므로 "완료"로 변경 또는 항목 제거.
  - L46~47 부근 Unity 이벤트 목록에 `Awake()` 항목 누락 — 신규 메서드.
- **`notes/code-review/functions/DeliveryManager.Update.md`**: **drift 있음**.
  - L17: `FindActiveCar()` — ⚠ 매 프레임 FindFirstObjectByType (개선 후보)` — 이제 fallback only.
  - L32~46 코드 핵심 블록이 변경 전 코드. 현재 코드(carCache 분기)로 갱신 필요.
  - L50~67 "⚠ 개선 후보" 섹션 — 이미 적용됐으므로 "✅ 적용 완료 (f960949)"로 표시.
  - **신규 노트 누락**: `DeliveryManager.Awake.md`가 없음. Awake가 추가되었으니 functions/ 폴더에 동급 노트 생성 권장 (incoming = Unity, outgoing = FindFirstObjectByType + carCache 캐시).

(자동 수정 금지 — 보고만. 후속 수정은 별도 사이클/사람 결정.)

## Dead code/워닝 누적

### 사이클 1 발견 중 해소
- 없음. Cycle 1 PART 1 자체가 "미해결 표지 0건, 미사용 using 0건, 미사용 SerializeField는 정적 grep으로 판정 불가" 결론이라 해소할 항목 자체가 없었음.

### 새로 발견 (코드 변경 없이 보고만)
- `DeliveryManager.FindActiveCar()` — 이제 호출처 1곳뿐 (Update의 fallback). 사적 메서드라 외부 영향 없음. inline해도 됨 (Minor, 미수정).
- `DeliveryManager.SetPoints(list, retn)` (L110~114) — public이지만 이번 검증 범위에서 호출처 grep 미확인. 코멘트는 "Editor convenience: register points programmatically (used by setup script)"이지만 실제 setup script 위치 확인 필요. **자동 판정 금지** (Editor/_Recovery 또는 외부 메뉴 스크립트가 호출 가능). 사람 결정용 후보로만 적재.
- 그 외 신규 dead code/warning 후보 없음.

## 사이클 3 권고

**Task Z 진행 OK.** 근거:
- f960949 동작 동등성 + 모든 엣지 케이스(late spawn / respawn / destroy) 통과.
- Critical 0, Major 0. carCache stale 우려는 Unity `==` 오버로드로 자연 해소.
- 다만 Cycle 3에서 코드 변경 외 **Obsidian vault drift 동기화 작업**을 1개 보조 작업으로 끼워넣기 권장 (DeliveryManager.md / DeliveryManager.Update.md 수정 + DeliveryManager.Awake.md 신규). 분량 작고 코드 위험 없음.
- Task Z(Editor 헬퍼 정리) 자체는 별도 영역이라 Task Y와 충돌 없음.
