# Phase 1 상태 — 검증 가이드

> 세션 15 기준. Phase 1 시스템 인프라 100%, UI/씬 95%.
> 이 문서는 도현이 어디까지 왔는지 + 무엇을 검증해야 할지 정리.

---

## 0. 한 번 클릭 통합 setup

`PitCrew/Phase 1/Full Integration Setup` 메뉴 실행 — 모든 자산 + 씬 자동 연결.

만약 단계별로 하고 싶으면:
1. `PitCrew/Order/Generate 8-Order Catalog`
2. `PitCrew/Upgrade/Generate 5-Upgrade Catalog`
3. `PitCrew/Cargo/Setup Cargo View (CCTV)`
4. `PitCrew/Layout/Fix Start Yaw (Auto-Align)`
5. 씬에 UpgradeSystem + AudioManager GameObject 수동 추가

---

## 1. 구현된 시스템 (Week 2~4 인프라 100%)

### 화물 시스템 (5종 × 3축)

| 타입 | Shock | Spin | Tilt | 충돌 | CCTV 색 |
|---|---|---|---|---|---|
| Box | 표준 | 표준 | 표준 | 표준 | 골판지 갈색 |
| Eggs | **극민감** | 보통 | 보통 | 민감 | 상아 |
| Glass | 민감 | 표준 | 보통 | **극민감** | 청 |
| Fish | 보통 | **극민감** | 표준 | 표준 | 청록 |
| Cake | 보통 | 보통 | **극민감** | 보통 | 핑크 |

### Packing (D-041)

| 단계 | 임계 ×배율 | 비용 | 효과 |
|---|---|---|---|
| None | 1.0 | 0 | 매우 조심 |
| **Bubble (default)** | **1.3** | 100 | 일반 주행 안전 |
| Foam | 1.6 | 300 | 공격 운전 가능 |
| Hardcase | 2.0 | 800 | 거의 모든 운전 |

### 운전감 (D-042)

- maxSpeedKmh 130, accelTime 2.0, brakeDecel 8
- FOV 50~100 (속도 비례)
- Speed Shake (60% 속도부터 Perlin 진동)
- **Impact Shake (D-045)** — 충돌 시 카메라 0.35s shake

### 트렁크 박스 시뮬 (D-042~D-047)

- **회전 OFF** (default) — 갈대 제거
- **사면 중력** — 차체 기울기 → 박스 미끄러짐
- **trunk visual rotation 매칭** — 박스 + floor 차량과 함께 기울어짐
- **SimCam worldspace fixed (D-047)** — 카메라 멀미 없음, 평지=평면, 사면=명확
- 박스 floor 정착 (D-046) — y=0.325
- 마찰: staticFrictionG=0.30, kineticFriction=3.0, accelScale=0.7

### 정산 화면 (Day 12)

- 화물 무결성 5등급 (완벽/약간 손상/크게 손상/거의 전파/전파)
- "이야기" 한 줄 ("계란 6개 모두 무사")
- 색상 띠 3단계 (성공+완벽/성공+손상/실패)

### 업그레이드 (Day 21~24)

5종:
1. 서스펜션 강화 (₩800) — Shock 임계 +0.1g
2. 냉동 칸 (₩1500) — Phase 2 해제
3. 지름길 지도 (₩1200) — NodeGraph
4. 부드러운 조향 (₩1000) — Spin +20°/s
5. 튼튼한 적재함 (₩1500) — maxHP +20

ResultPanel 안에서 구매 → 다음 런부터 자동 적용.

### 주문 시스템 (Day 26)

- **8개 풀** (5종 cargoType × 4종 packing 다양성)
- **Shuffle Pick 3개** — 매 보드 오픈마다 다른 3개 노출
- 카드에 화물/포장/운전 가이드 표시

### IntegrityHUD (D-043)

- 라벨: "**계란 87%**" / "**유리 65%**" 형태
- 색 변화 (Green → Yellow → Red)
- 20% 이하 critical blink
- **충돌 flash** — 0.18s 빨강 lerp

### Audio (Day 25)

AudioManager 인프라 + 자동 후크. 도현이 AudioClip 6종 할당 시 즉시 작동:
- cargoImpactClip (속도 비례 볼륨)
- cargoTotaledClip
- deliveryClip
- upgradeBoughtClip
- resultSuccessClip
- resultFailClip

---

## 2. 미구현 (Phase 1 게이트 통과 후 진입)

- **Day 19 RoutePlannerPanel UI** — NodeGraph 인스턴스 의존 (도현 그래프 작성 후)
- **Day 22~23 Hub Scene** — 별도 차고 씬, GameStateManager
- **Tile heightMask 실제 적용** — TileMapAuto Y offset (Editor 작업)
- **TireSystem 통합** — ArcadeCarController.sideGripDamping 곱하기 적용 (현재 정의만)
- **5개 업그레이드 효과 5종** — ChilledHold, ShortcutMap는 Phase 2

---

## 3. 도현 검증 체크리스트 (Phase 1 게이트)

### A. 운전감 (5분 주행)

- [ ] 130km/h 속도감 충분한가
- [ ] 코너 시 FOV 변화 + 속도 진동 체감
- [ ] 충돌 시 카메라 shake 강도 적절
- [ ] 갈대 사라졌나 (회전 OFF)

### B. 화물 시스템 (각 타입 1런씩)

- [ ] **Box** + Bubble + 일반 주행 → 95+ 유지
- [ ] **Eggs** + Bubble + 같은 코스 → 80~90 (경고)
- [ ] **Glass** + 작은 충돌 → 큰 데미지
- [ ] **Fish** + 급커브 → 빠른 데미지
- [ ] **Cake** + 사면 → 빠른 데미지
- [ ] CCTV 박스 색상 화물별 다름
- [ ] IntegrityHUD에 "계란 87%" 형태 라벨

### C. 트렁크 박스 (CCTV)

- [ ] 평지 정지 → 박스 floor 위 가만히 (D-046)
- [ ] 사면 정차 (작은 기울기 < 15°) → 박스 안 움직임 (D-046 마찰)
- [ ] 사면 정차 (큰 기울기) → 박스 사면 따라 미끄러짐 (D-044)
- [ ] 평지에서 carmera 시점이 평면 (D-047)
- [ ] 사면에서 trunk + 박스 같이 기울어진 게 명확히 보임

### D. 주문 + 보드 (3런 이상)

- [ ] 매번 다른 3개 주문 노출 (Shuffle Pick)
- [ ] 카드에 화물/포장/운전 가이드 표시
- [ ] 8개 풀 다 볼 수 있음

### E. 정산 + 업그레이드 (3런)

- [ ] 정산 화면 화물 이야기 한 줄 즉각 인지
- [ ] 보유 자금 누적
- [ ] 업그레이드 5개 카드 보임
- [ ] 자금 부족 = 비활성, 충분 = 구매 가능
- [ ] 구매 후 다음 런 효과 적용 (Suspension = 임계 +0.1g 등)

### F. 30분 연속 (Phase 1 핵심 게이트)

- [ ] 30분 연속 플레이 지루하지 않음
- [ ] 최소 5런 이상 완수
- [ ] 화물 타입에 따라 자발적으로 운전 패턴 다름
- [ ] 정산 후 다음 런이 기대됨

---

## 4. 게이트 통과 시 다음 단계

### Phase 1 정식 마무리 (Week 4 Day 27~28)

- 통합 플레이 1시간 (10런 이상)
- 모든 화물 × 모든 포장 조합 시도
- 업그레이드 트리 5개 모두 구매 시도
- 버그 리스트 작성

### Phase 2 진입 조건

게이트 5개 중 3개 이상 YES → Phase 2.

Phase 2 첫 작업 (디렉터 진단 따라):
1. 비주얼 정체성 결정 (코지 / 긴장 / 코믹 / 하이브리드)
2. 시그니처 메커닉 폴리싱 (화물 시스템 더 화려하게)
3. 사운드 디자인 본격
4. 첫 5분 임팩트 (튜토리얼)

게이트 NO 시 → 부족 영역 1주 추가 작업 (디렉터 권고: "조금만 더"는 통과 신호 아님).

---

## 5. 알려진 한계 / TODO

- AudioClip 6종 도현이 직접 할당 필요
- NodeGraph SO 인스턴스 도현이 인스펙터 작성 필요
- Hub Scene 미구현 (정산 → "허브" 흐름 없음)
- TileMap heightMask 시각 적용 안 됨 (시뮬은 가능)
- TireSystem 효과 ArcadeCarController에 통합 안 됨 (정의만)
- 5개 업그레이드 중 ChilledHold / ShortcutMap는 Phase 2

---

## 6. 도현 자평 한 줄 (매주 끝)

```
이번 주 가장 흥분된 순간: ____________________
이번 주 가장 답답했던 순간: ____________________
```

CLAUDE.md 의식 #5 참조.

---

*세션 15 종료 시점 / 2026-04-28*
