# 2팀 감사 통합 리포트 (2026-05-11)

> 팀 A (Architect: SOLID/결합/응집) + 팀 B (Performance/Readability: GC/핫패스/가독성)
> 독립 감사. 결과 비교 → 채택 결정 → 적용 흐름.
>
> 원본:
> - [team-a-architect-2026-05-11.md](team-a-architect-2026-05-11.md) (발견 9개)
> - [team-b-perf-readability-2026-05-11.md](team-b-perf-readability-2026-05-11.md) (perf 6 + readability 3)

---

## 1. 통합 우선순위 표

| ID | 카테고리 | 위치 | 비용 | 채택 |
|---|---|---|---|---|
| **B1** | Perf 핫 | `DeliveryHUD.cs:114` Update | 5m | ✅ 즉시 |
| **B2** | GC 핫 | `Order/CountdownHUD.cs:96` | 5m | ✅ 즉시 |
| **B3** | GC 핫 | `UI/DeliveryProgressHUD.cs:107,113` | 5m | ✅ 즉시 |
| **B4** | 가독성/할당 | `UI/CargoWarningHUD.cs:144` | 5m | ✅ 즉시 |
| **A3** | DIP/일관성 | `Order/OrderBoard.cs:51-60` static hover | 30m | ✅ 즉시 |
| **A8** | Lifecycle | `Audio/AudioCueBus.cs` reset hook | 10m | ✅ 완료 |
| **A6** | DIP/싱글톤 | `Upgrades/UpgradeSystem.Instance` 잔재 | 1-2h | 🟨 다음 세션 |
| **B5** | Perf 중 | `Order/DeliveryPointMarker.Update` 마커별 sin | 15m | 🟨 다음 세션 |
| **R1** | 가독성 | `CargoWarningHUD` 문자열 6종 인라인 | 10m | 🟨 다음 세션 |
| **R3** | 가독성 | `EPS = 0.0001f` 매직넘버 통일 | 5m | 🟨 다음 세션 |
| **A2** | OCP/Domain | UpgradeEffect 도메인 누수 (Modifier 일반화) | 3-5h | ⏸ 결정 필요 |
| **A4** | DRY/패턴 | `TextHUDBase` 4 HUD 통합 (B2/B3 흡수) | 3-4h | ⏸ 결정 필요 |
| **A5** | SRP/정책 | `RewardCalculator` + `CargoGradeBook` SO | 2-3h | ⏸ 결정 필요 |
| **A7** | DIP | `IRunController` + `IRunSummary` DTO | 2-3h | ⏸ 결정 필요 (Phase 1 마무리) |
| **A1** | SRP/God | `ResultPanel` MVP 분할 (R2 흡수) | 6-10h | ⏸ 결정 필요 (Phase 6) |
| **B6** | Perf 저 | `CargoView.Update` self-disable | 5m | △ 채택 가능, 작음 |
| **A9** | 데이터 분리 | `CargoCatalog` SO | 4-6h | 🔵 post-MVP |
| **R2** | 가독성 | `ResultPanel` 멘트 5단 if-사다리 | A1과 함께 | A1 종속 |

범례: ✅ 즉시 · 🟨 다음 세션 · ⏸ 사용자 결정 · 🔵 post-MVP · △ 보류 무관

---

## 2. 두 팀 시각이 겹친 부분

### ResultPanel
- **A1**: God class, MVP(Model-View-Presenter)+Builder 분리.
- **R2**: 멘트 5단 if-사다리 → 테이블화.
- **A5**: RewardCalculator/CargoGradeBook SO 분리 (등급/카피 중복 제거).
→ 통합: **A1 작업 시 R2/A5 자연 흡수**. ResultPanel을 풀면 정책·데이터·뷰가 같이 분리됨.

### HUD 4종
- **A4**: TextHUDBase Template Method (DRY/패턴).
- **B2/B3**: 매 프레임 string interpolation (GC).
→ 통합: **TextHUDBase 도입 시 base에서 "값 바뀔 때만 갱신" 패턴 통합** — A4가 B2/B3를 자연 흡수.
- 다만 TextHUDBase는 3~4h, B2/B3 임시 패치는 5분씩.
- 권장: **지금은 B2/B3 패치만**, TextHUDBase는 한 세션 통째 잡을 때 (A4가 들어오면 B2/B3 패치가 잠시 base로 이주).

### Phase 1 추상화 새는 곳
- **A6**: `UpgradeSystem.Instance` 직접 접근 잔존.
- **A7**: `IRunController`가 너무 좁아 호출자가 `as RunController`로 우회.
→ 통합: 둘 다 "Phase 1을 80%만 끝낸 결과". 같은 세션에 묶어 마무리하는 게 효율.

---

## 3. 차이가 드러난 부분

- 팀 A는 **변경 비용** 관점, 팀 B는 **런타임 비용** 관점.
- 같은 ResultPanel에 대해 A는 "분할", B는 "테이블화"라는 다른 단위로 접근.
- 팀 A가 본 코드(Order/, Upgrades/, Audio/)와 팀 B가 본 코드(HUD, CargoView, DeliveryPointMarker)가 거의 안 겹침 — **각자 다른 깊이로 파고들어** 9 vs 9개 발견을 만든 게 2팀 구조의 효과.

---

## 4. 이번 세션 즉시 적용 (✅ 6건)

세션 한 토막에 끝나는 자명 항목. 비용 합 ~55분.

1. **A8** AudioCueBus `[RuntimeInitializeOnLoadMethod]` 자동 Clear — **완료**.
2. **B1** DeliveryHUD canvas 캐시.
3. **B2** CountdownHUD 초 단위 변경 감지 → string alloc 1/60.
4. **B3** DeliveryProgressHUD 값 변경 감지.
5. **B4** CargoWarningHUD `static readonly Color` + `const float EPS`.
6. **A3** OrderBoard 정적 hover dispatch → `Bind` 콜백 일관화.

---

## 5. 도현 결정 후보 (다음 세션 이상)

각각 한 세션 통째로 적합. 의존 관계:

```
A7 (IRunController DTO)  ←─┐
A6 (UpgradeSystem 일관)  ←─┼─ Phase 6 "Phase 1 마무리" 한 세션 (4-5h)
                            │   R3, R1도 곁들이기 좋음.
A5 (RewardCalculator) ←────┘
A1 (ResultPanel 분할) ────── Phase 7 한 세션 (6-10h, R2 자연 흡수)
A4 (TextHUDBase) ──────────  Phase 8 한 세션 (3-4h, B2/B3 잠시 패치 이주)
A2 (UpgradeEffect 일반화) ── Phase 9 (3-5h, 효과 개수 늘기 전에)
A9 (CargoCatalog SO) ─────── post-MVP
```

이번 세션에 마저 가는 건 권장 X — 위 작업 하나하나가 학습 노트 한 편씩 분량. ExulMundi 패턴(스코프 부풀림) 주의 영역.

---

## 6. 학습 가치 (도현 메타)

이번 2팀 감사에서 배울 점:

1. **독립 감사가 가치 있다.** 한 모델/관점이 9개 보면 다른 관점이 추가로 9개. 합치면 18개. 한 명이 18개 다 보려면 시야가 너무 흩어진다.
2. **겹치는 부분은 신뢰도 신호.** ResultPanel을 A/B 둘 다 짚었다 = 가장 시급한 부채.
3. **변경 비용 vs 런타임 비용은 다른 dial.** 둘 다 측정해야 트레이드오프가 보임. 한쪽만 보면 "지나친 추상화"나 "조기 최적화" 양쪽으로 빠짐.
4. **자명한 항목부터 적용 → 큰 항목은 결정 대기.** 이번 ✅ 6건은 의심 없이 처리. ⏸ 5건은 의도(스코프)를 정한 뒤 진입.
