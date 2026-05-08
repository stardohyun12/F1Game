# Active Context — PitCrew

> 매 세션 시작 시 읽고, 끝날 때 갱신.
> 안정 규칙은 `.claude/rules/`에. 여기는 **상태**만.

---

## 지난 세션 (세션 17, 2026-04-30 ~ 2026-05-08 작업)

### 코어 변경
- **OpenWorld_v2 신규 씬** (17×13 그리드, 4구역 + NE plateau)
- **`heightMask` 통합** (D-050): TileMapAuto/BuildingPlacer Y offset, Slant Kind 4종 + mask char `n/s/e/w/=/+`
- **미사용 Kenney 모델 41개 활용**: industrial 21 + low-detail 14 + decor 6
- **컨셉 정정** (D-051): "F1 복귀 서사" — 망한 F1팀 → 배달 → 머신 복원 → 그랑프리 복귀
- **플로우 정정** (D-052): 주문 → 랩핑 → 경로 → 출발세팅 → 주행 → 정산 → 업그레이드
- **결정 채널 9 확정** (D-053): `GDD/decision-channels.md`
- **GitHub push** (private repo `stardohyun12/F1Game`): main + claude/angry-montalcini-14cd06 두 브랜치
- **컨텍스트 시스템 구축** (이번 세션 마지막): CLAUDE.md 슬림화 + `.claude/rules/` + activeContext + hooks

### 알려진 한계 (다음 처리 필요)
- **`road-slant.fbx` 시각 한계**: 도로 surface 없는 좁은 wedge. 실제 운전 가능한 ramp가 아님. 대안: road-bridge + cube ramp / road-straight tilt / Unity Terrain.
- **도시 디테일 부족**: 가로수·가로등·인도·주차차량·skybox·fog 모두 미배치. "City Detail Pass" 보고서 (이번 세션 작성) 참고.
- **코드 리팩터 보류**: SOLID/패턴 학습 목적 5단계 리팩터 계획 있음 (Composition Root → HUD base → Modifier Chain → FSM → SO Event Channel). 도현 결정 대기.

---

## 이번 세션 한 일 (세션 18, 2026-05-08)

- **컨텍스트 시스템** (Memory Bank 패턴) 적용 — CLAUDE.md 슬림 + .claude/rules/ + activeContext + hooks
- **Phase 1** 코드 리팩터 완료 (Find 36 → 1) — DIP + Service Locator + Composition Root
- **Phase 2** Template Method (FuelHUD 132→19줄, IntegrityHUD 178→50줄)
- **Phase 3** Modifier Chain (CargoBox.ApplyPreset OCP 위반 해소 + maxHP 누적 버그 부수 해소)
- 학습 문서 4개: `GDD/architecture/file-map.md` + `phase1-dip-service-locator.md` + `phase2-template-method.md` + `phase3-modifier-chain.md`
- 프로젝트 스냅샷: `notes/project-status-2026-05-08.md`
- GitHub commits: `c064f59` (Phase 1 코어), `d9e6a59` (Phase 1 확장). Phase 2+3 미커밋
- ⚠ **Coplay MCP 끊김** — Phase 2+3 컴파일/Play 검증 X. 다음 세션 첫 작업: Coplay 복귀 후 검증

---

## 다음 세션 진입 시 (도현 결정 대기)

### 첫 우선 — Coplay 복귀 후 검증 (필수)
1. `check_compile_errors` — Phase 2+3 컴파일 통과 확인
2. Play 모드 → `[GameContext] 12/12 시스템 등록 완료` + 게임 동작 (Order Board / 주행 / 게이지)
3. 문제 있으면 디버그

### 이후 작업 후보
- **(c) 도시 디테일 패스** (4~6h) — skybox + 가로수 + 가로등 자동 배치 (Coplay 필수)
- **(d) OpenWorld 슬로프 시각 한계** (4~8h) — road-bridge + cube ramp / Unity Terrain (Coplay 필수)
- **게임플레이 진척**: RoutePlannerPanel / F1 복원 트리 시각화 / 신호등
- **Phase 4** (선택, 보류 권장) — RunController State 패턴, ResultPanel 분할
- **Phase 2.5** (선택) — 텍스트 HUD 4개 (`TextHUDBase`) 도입

---

## 잠재 부채 (해소 권장 시점)

- HOT-3: ResultPanel 499라인 God class 분할 (Phase 4 후보, 5~10h)
- ResultPanel 안 EnsureEventSystem static 메서드의 Find — UI bootstrap 영역, 위험 낮음
- UIRoot.Instance lazy Find — 문제 없음 (싱글톤 lazy init 정상)

---

## 자주 참조하는 외부 문서

- USP·컨셉: `GDD/overview.md` (피치 + 매크로/마이크로 루프)
- 결정 채널 9: `GDD/decision-channels.md`
- 결정 이력: `notes/decisions-log.md` (D-001 ~ D-053)
- 시스템 스펙: `GDD/systems/` (cargo / fuel / gears / tires / route-planning)
- 레퍼런스: `GDD/research/reference-games-survey.md`
- 코드 리팩터 계획 (보류): 채팅 로그 기반, 파일화 안 됨

---

## 힌트 (다음 세션을 위한 메모)

- Unity Editor가 켜져 있으면 Coplay MCP로 화면 갱신부터.
- 도현은 표·즉시 실행 옵션 친화. 긴 강의 X.
- "ExulMundi 패턴" = 스코프 부풀림 위험. 저자가 직접 인지 중. 부드럽게 알림.
- F1 복귀 서사 = 메타 진행. 단순 업그레이드 아님.
