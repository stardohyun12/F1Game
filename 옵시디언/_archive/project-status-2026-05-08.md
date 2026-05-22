# PitCrew 현황 — 2026-05-08

> **세션 18 끝. 다음 세션 진입 전 이 페이지만 봐도 OK.**

---

## ⚡ TL;DR (3줄)

1. **코드 리팩터 Phase 1~3 완료**, 단 후반 코드는 검증 X (Coplay 끊김)
2. **MVP 진행도 2주차 끝 ~ 3주차 시작**. 게임플레이 진척은 3주째 멈춤
3. **다음 우선**: Coplay 복구 → 검증 → 30분 직접 플레이 → 게임 작업 재개

---

## 🎯 게임 컨셉

> 망한 F1 팀이 배달로 자금을 벌어 머신을 복원하고 F1 그랑프리에 복귀한다.

```
[마이크로 루프 — 한 런 5~15분]
주문 → 랩핑 → 경로 → 출발세팅 → 주행 → 정산 → 업그레이드/F1복원
                                                       ↓
[매크로 루프] 망가진 머신 → 부품 N개 복원 → F1 그랑프리 복귀
```

**결정 채널 9개**: 속도 · 외형 · 맵 접근도 · 배송물자 · 박스 · 랩핑 · F1복원트리 · 내비 · 연료탱크

---

## 📊 MVP 진행도 (4주 중)

| 주차 | 검증 질문 | 진행 |
|---|---|---|
| 1 | 차 몰기 자체가 재밌나? | ✅ 거의 |
| 2 | 화물 관리에 긴장감? | ✅ 8할 |
| 3 | 경로 계획이 의미? | ⚠️ 3할 |
| 4 | 5~15분 한 런이 굴러가나? | ⚠️ 5할 |

**위치**: 2주차 끝. 세션 14 이후 게임 직접 플레이 0 (3주째).

---

## 🏗️ 코드 아키텍처 (세션 17~18)

### Phase 1 → 3 적용 결과

| Phase | 패턴 | 효과 |
|---|---|---|
| 1 | DIP + Service Locator + Composition Root | Find 36 → 1 |
| 2 | Template Method (HUD) | FuelHUD/IntegrityHUD 합 310 → 69줄 |
| 3 | Modifier Chain (Strategy + Registry) | CargoBox OCP 해소 + maxHP 버그 해소 |

### 폴더 구조

```
F1/Assets/Scripts/
├── Core/Core.cs                Phase 1 (12 인터페이스 + Services + GameContext)
├── Cargo/Modifiers/            Phase 3 (5 파일)
└── UI/BarHUDBase.cs            Phase 2
```

### 학습 문서

- `GDD/architecture/file-map.md` — 코드 어디에 뭐가 있는지
- `GDD/architecture/phase1-...md` — DIP·Service Locator
- `GDD/architecture/phase2-...md` — Template Method
- `GDD/architecture/phase3-...md` — Modifier Chain

---

## 🔥 결합도 핫스팟

| ID | 위치 | 상태 |
|---|---|---|
| HOT-1 | RunController.Awake 6 Find | ✅ 해소 |
| HOT-2 | CargoBox.ApplyPreset OCP | ✅ 해소 |
| HOT-3 | ResultPanel 499줄 God class | ⏳ 보류 |
| HOT-4 | CargoShakeSimulator name-find | ⚠️ 1회 fallback + 경고 |
| HOT-5 | HUD 7개 복붙 | 🟡 절반 (2/7) |

---

## ⚠️ 검증 미완 (Coplay 복귀 후 필수)

```
[ ] check_compile_errors — 컴파일 0 확인
[ ] Play 모드 → "[GameContext] 12/12 등록" 로그 확인
[ ] 게임 동작 검증 (Order Board / 주행 / 게이지)
[ ] PitCrew/Layout/Build City Detail Pass 메뉴 실행
[ ] PitCrew/Layout/Enhance Slope Tiles 메뉴 실행
```

검증 안 한 상태로 더 쌓으면 토대 무너짐.

---

## 🛑 미해결 결정

### 코드
- D-? Phase 2 텍스트 HUD 4개 (TextHUDBase) 도입 시점?
- D-? HOT-3 ResultPanel 분할 (Phase 4) 진입 시점?

### 게임플레이
- D-? RoutePlannerPanel UI 디자인 (3주차)
- D-? F1 복원 트리 시각화 (채널 #7, USP 직결)
- D-? 신호등·교통량 진입 시점

### 시각
- D-? OpenWorld 슬로프 시각 한계 추가 보강 vs 현재로 충분?
- D-? 도시 디테일 패스 vs 게임플레이 우선?

---

## 📌 GitHub

- **Repo**: `stardohyun12/F1Game` (private)
- **main HEAD**: `bf309f0 refactor(phase2+3) + feat(city-detail+slope)`
- **누적 push**: 5 commits (Phase 1 코어 → 확장 → Phase 2+3 + c/d)

---

## 🚀 다음 세션 진입 순서

```
0~5분    SessionStart hook 자동 출력 정독
5~30분   Coplay 복구 + 검증 (위 체크리스트)
30~60분  직접 30분 플레이 — 게임 체감 1줄 적기
1시간~   체감 기반 다음 작업 결정:
         · "재밌어" → 4주차 진입 (허브 씬, F1 복원 시각화)
         · "헷갈려" → UX 보강 (RoutePlanner / 내비)
         · "감 없음" → 신호등·교통량으로 운전 결정 추가
```

---

## ✋ 핵심 경고

> **마지막 7세션이 다 메타 작업 (컨텍스트 시스템 / 리팩터).**
> **게임플레이 진척은 세션 14 이후 0.**
> **ExulMundi 패턴 (스코프 부풀림→미완성) 위험 신호.**
> **Phase 4/5/6 코드 리팩터는 4주차 MVP 게이트 통과 후로 보류.**

---

## 📝 한 줄 요약

**2주차 끝. 코드 토대 정리 완. 게임 자체로 돌아갈 시간.**
