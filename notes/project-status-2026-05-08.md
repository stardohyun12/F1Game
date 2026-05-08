# PitCrew 프로젝트 현황 스냅샷 — 2026-05-08

> 세션 18 종료 시점 상태. 다음 세션 진입 전 정독 권장.
> 매 세션 `.claude/activeContext.md` 가 가벼운 상태 갱신, 이 문서는 큰 흐름 스냅샷.

---

## 1. 게임 컨셉 (D-051 정정 확인)

**망한 F1 팀이 배달로 자금을 벌어 머신을 복원하고 F1 그랑프리에 복귀한다.**

- 마이크로 루프 (5~15분 한 런): 주문 → 랩핑 → 경로 → 출발 세팅 → 주행 → 정산 → 업그레이드/F1 복원
- 매크로 루프: 망가진 머신 → 부품 복원 N회 반복 → 가동 가능 → F1 복귀 (엔딩)
- 결정 채널 9개 (`GDD/decision-channels.md`): 1.속도 / 2.외형 / 3.맵 접근도 / 4.배송물자 종류 / 5.보관박스 / 6.랩핑 / 7.기본기능(F1 복원 트리) / 8.지도 / 9.연료탱크

---

## 2. 4주 MVP 진행도 (`dev-plan/feature-list.md` 기준)

| 주차 | 핵심 검증 | 진행도 |
|---|---|---|
| 1 | 차 몰기 자체가 재밌는가 | ✓ 거의 완료 (세션 14에서 "그럭저럭/NO" → D-040 컨셉 전환 + 후속 튜닝) |
| 2 | 화물 관리가 긴장감 주는가 | ✓ 8할 (Order/RunController/Result/CargoShake/IntegrityHUD 다 동작. 신호등·UIEffect·suburban props 미진입) |
| 3 | 경로 계획이 의미 있는가 | ⚠ 3할 (NodeGraph 데이터구조만, RoutePlannerPanel 미구현, 맵 고저차 미통합) |
| 4 | 5~15분 한 런이 굴러가는가 | ⚠ 5할 (서킷 1개, 허브 씬 미완, 코너 경고 UI 미진입) |

**현재 위치**: 사실상 2주차 끝~3주차 시작. 코드 리팩터로 한 박자 쉬고 들어옴.

---

## 3. 코드 아키텍처 현황 (Phase 1~3 정착)

### 신규 구조 (세션 17~18)
```
F1/Assets/Scripts/
├── Core/                              ← Phase 1
│   └── Core.cs                        (12 인터페이스 + Services + GameContext)
├── Cargo/Modifiers/                   ← Phase 3
│   ├── CargoStats.cs                  (struct)
│   ├── ICargoModifier.cs              (인터페이스)
│   ├── CargoTypeModifier.cs           (baseline)
│   ├── PackingModifier.cs             (multiplier)
│   └── UpgradeModifiers.cs            (additives + Registry)
└── UI/
    └── BarHUDBase.cs                  ← Phase 2 (Template Method)
```

### 결합도 변화
- **Find 호출**: 36 → 1 (HOT-1 거의 해소). 1개는 CargoSimCamRig name-find fallback (문서화)
- **HUD 보일러플레이트**: FuelHUD 132줄 → 19줄, IntegrityHUD 178줄 → 50줄 (HOT-5 부분 해소)
- **CargoBox.ApplyPreset OCP**: if/else 체인 → Modifier Chain (HOT-2 해소)
- **maxHP 누적 버그**: 자연 해소 (Phase 3 부수 효과)

### 학습 문서 (`GDD/architecture/`)
- `file-map.md` — 전체 코드 구조 맵
- `phase1-dip-service-locator.md` — DIP + Service Locator + Composition Root
- `phase2-template-method.md` — Template Method (HUD)
- `phase3-modifier-chain.md` — Strategy + Registry (CargoBox)

---

## 4. 결합도 핫스팟 잔존

| ID | 위치 | 상태 |
|---|---|---|
| HOT-1 | RunController.Awake 6 Find | ✓ 해소 (Phase 1) |
| HOT-2 | CargoBox.ApplyPreset OCP | ✓ 해소 (Phase 3) |
| HOT-3 | ResultPanel 499라인 God class | ⏳ 보류 (Phase 4 후보) |
| HOT-4 | CargoShakeSimulator GameObject.Find by name | ⚠ 1회 fallback + 경고 (수용 가능) |
| HOT-5 | HUD 7개 복붙 | ⏳ 절반 해소 (FuelHUD/IntegrityHUD만, 텍스트 HUD 4개 보류) |

---

## 5. 미해결 결정·이슈 (도현 답변 필요)

### 코드 검증 미완 (Coplay 끊김)
- **세션 18 후반부 코드 변경 (Phase 2 + 3)** 컴파일 검증 X (Coplay MCP 끊김)
- Coplay 복귀 후 `mcp__coplay-mcp__check_compile_errors` 1회 실행 필요
- Play 모드 진입해서 12/12 등록 + 게임 동작 확인 필요

### 시각·맵 작업 보류
- **OpenWorld 슬로프 시각 한계** (D-050 후속): `road-slant.fbx`가 좁은 wedge라 운전 가능 ramp 아님. 대안 검토 필요 (road-bridge + cube ramp / Unity Terrain)
- **도시 디테일 패스**: skybox + 가로수 + 가로등 + 인도 자동 배치. 4~6h 작업, 미사용 Kenney 모델 22개 활용 가능

### 게임플레이 결정 대기
- **D-?** RoutePlannerPanel UI 디자인 (3주차 미완)
- **D-?** F1 복원 트리 시각화 (채널 #7) — 어떻게 인스펙터 보여줄지
- **D-?** 신호등·교통량 시스템 진입 시점
- **D-?** 도시 디테일 vs 슬로프 시각 한계 우선순위

### 메타·운영
- **D-?** notes/ 설계 성격 3건 (ideas-parking, open-questions, conventions) → GDD/로 이동 여부

---

## 6. GitHub 상태

- **Repo**: `https://github.com/stardohyun12/F1Game` (private)
- **main 브랜치 HEAD**: `d9e6a59 refactor(phase1+): Find 36 → 1` (세션 18 진행 중. 세션 18 후반 Phase 2+3은 미커밋)
- **claude/angry-montalcini-14cd06**: 11 commits 보존 (이전 리팩터 이력)
- **다음 push 후보**: Phase 2 + Phase 3 코드 + 학습 문서 3개

---

## 7. 컨텍스트 관리 시스템 (세션 18 신규)

Cline Memory Bank 패턴 적용. md 파일만 사용:

```
CLAUDE.md (30줄, 진입점만)
.claude/
  ├── rules/
  │   ├── scope.md            스코프 + 컨셉 + 채널 9
  │   ├── asset.md            에셋·맵 배치 규칙
  │   ├── session.md          시작·종료 프로토콜
  │   └── communication.md    톤 + 도현 프로필
  ├── activeContext.md        매 세션 갱신 (상태 전용)
  ├── hooks/
  │   ├── session-start.sh    자동 체크리스트 주입
  │   └── session-end.sh      자동 session-log stub
  └── settings.json           hook 등록
```

- 다음 세션 시작 시 SessionStart hook이 자동으로 session-log + activeContext 인젝션
- "지난번 뭐 했지" 작성 부담 0

---

## 8. 다음 세션 진입 권장 순서

### 0~5분: 컨텍스트 복원
1. 자동 SessionStart hook 출력 정독 (지난 일·이번 목표·힌트)
2. **이 문서** (`notes/project-status-2026-05-08.md`) 정독
3. Coplay MCP 연결 상태 확인 → 없으면 사용자에게 Claude Desktop 재시작 안내

### 5~15분: 코드 검증
1. `mcp__coplay-mcp__check_compile_errors` — Phase 2+3 코드 컴파일 통과 확인
2. Play 모드 → `[GameContext] 12/12 시스템 등록 완료` 확인 + 게임 동작 확인 (Order Board, 주행, 화물 게이지)
3. 필요시 디버그

### 15분 이후: 다음 작업 결정
- (c) 도시 디테일 패스 진입
- (d) OpenWorld 슬로프 시각 한계 해결
- 또는 게임플레이 진척 (RoutePlannerPanel / F1 복원 트리 / 신호등)

---

## 9. 한 줄 요약

> **2주차 끝 + 코드 리팩터 Phase 1~3 (DIP·Template·Strategy) 완료. 시각/맵 작업과 게임플레이 진척이 다음 우선. F1 복귀 서사가 메타 진행축.**
