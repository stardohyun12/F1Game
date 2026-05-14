# Active Context — PitCrew

> 매 세션 시작 시 읽고, 끝날 때 갱신.
> 안정 규칙은 `.claude/rules/`에. 여기는 **상태**만.

---

## 지난 세션 요약

- 세션 18 (2026-05-08): Phase 1 DIP/Service Locator + Phase 2 Template Method + Phase 3 Modifier Chain.
- 세션 19 (2026-05-11~12): Phase 4 AudioCueBus + Phase 5 TireTypePresets + Phase 6 Peer Review 6건. 미커밋.
- 세션 20 (2026-05-12~13): Unity 검증 도구 Coplay MCP → **unity-cli** 전환. Obsidian vault `옵시디언/` 분리. `_Recovery/` 솎아냄 (108→15). 차/도로 부유·기울임·z-fight 해결. 한글 폰트 atlas/material sub-asset 복구. **화물 손상 인과 변경** (차 G→박스 슬라이드→trunk 박음→손상). 알림 사운드 hook. Superpowers 플러그인 설치. 기획서 .pptx 생성. **Vertical Slice 데모 6주 결정 (D-069)**.
- 세션 21 (2026-05-13): 기획서 W1~W6 폐기, 3D 포장 미니게임 17 Task 구현 (Subagent-driven). D-070 결정. wrap 핸드오프까지 완료 (`RunController.TransferWrapStateToCargo`). 다물건(1~3) items[1+] 폐기는 후속.
- **세션 23 (2026-05-14, 자율 2시간 루프)**: CaptureFullScreen.cs 작성 (overlay UI 포함 PNG). **W1 채널 3 (맵 해금) 완료** — `Zone`/`ZoneRegistry` SO (+tintColor), `ZoneUnlockSystem` MonoBehaviour (wallet 차감, OnZoneUnlocked 이벤트, GetZoneOf 역참조), `ZoneUnlockPanel`/`ZoneUnlockCard` UI (자동 refresh on wallet change), `OrderBoard.PickOffer` ZoneUnlockSystem 우선 (catalog 폴백), `OrderCardUI`에 zone 라벨/tint 배지. Seed: Zone_Downtown(₩0, 3 jobs, cyan) + Zone_Harbor(₩1,000, 3 jobs, orange) + Zone_Industrial(₩2,500, 2 jobs, olive). 씬 와이어링 자동화 (`PitCrew/W1/Wire Scene (Zones)`, 우하단 위치). 컴파일 0 에러. W2 brainstorm doc 작성 (`옵시디언/dev-plan/2026-05-14-w2-brainstorm.md`). 미커밋.

---

## 현재 북극성

**Steam 데모 출시 — Vertical Slice — 2026-06-24 (6주)**.
풀게임의 모든 시스템 mechanically 작동 + 콘텐츠 1 슬라이스 (1 도시, 화물 5종, F1 부품 2개 복원).
풀 spec: `옵시디언/dev-plan/2026-05-13-vertical-slice-design.md`.

### 6주 일정

| 주차 | 작업 | 기간 |
|---|---|---|
| **W1** | 채널 3 (맵 해금) | 5/13~5/19 |
| **W2** | 채널 5 (박스 크기) + 채널 8 (내비) | 5/20~5/26 |
| **W3** | F1 복원 메타 (차고 + 부품 시스템) | 5/27~6/2 |
| **W4** | 정산→복원 연결 + 데모 시나리오 3런 | 6/3~6/9 |
| **W5** | 사운드 + HUD + onboarding | 6/10~6/16 |
| **W6** | QA + Steam 페이지 자산 | 6/17~6/24 |

---

## 다음 세션 (세션 24) — 첫 작업

1. **W1 채널 3 도현 manual 검증** — Play 진입 → 우하단 ZoneUnlockPanel 보이는지 / 잡 선택 → 출발 → 정산에서 wallet 충전 → OrderBoard 재오픈 시 항구 카드 affordable color 되는지 / 클릭 → 잡 풀 4→8 확장 시연.
2. **포장 시연 Play 모드 manual 테스트** — 잡 선택 → 포장 (마우스 드래그, WASD 회전, 1/2/3 재료, 휠 폭) → 출발 → 운전에서 충격 → 정산 손상값이 wrap 정도에 따라 다른지.
3. **W1 polish (선택)**: OrderCard에 zone 라벨 배지, Zone별 카드 컬러 차이, ZoneUnlockCard prefab 디자인.
4. **W2 시작 (5/20~)**: 채널 5 (박스 크기 capacity 2→4) + 채널 8 (지도/내비). 일단 채널 5 브레인스토밍 — capacity 업그레이드는 UpgradeDef 1~2개 추가로 끝날 수도.
5. **차고/부품 시스템 결정** — 도현이 구체 결정 알려준 후 별도 spec 진입 (W3 작업).
6. 다물건 잡(1~3) items[1+] 활용 후속 결정 (현 구현은 items[0]만 cargo에 이관, 나머지 폐기).
7. 화물 손상 인과 변경 (D-068) Play 테스트.

### W1 미해결 사항

- ZoneUnlockPanel 위치는 우하단으로 옮겼지만 prefab 디자인 안 됐음 (런타임 폴백 카드 사용). prefab 만들면 더 멋짐.
- Screenshot pipeline에서 timeScale=0/Canvas refresh stale frame 가끔 발생. 내부 state는 정확함.
- ResultPanel과 ZoneUnlockPanel 분리됨 — 같은 자리에 안 보이지만 사용자 흐름상(정산→OrderBoard) 자연스러움.

---

## 잠재 부채 (Phase 7~9 — 데모 일정에 따라 보류 또는 인터리브)

- `IRunController` 추상화 새는 곳 — `as RunController` 캐스팅 4-5곳 (Phase 7)
- `UpgradeSystem.Instance` 직접 접근 `ResultPanel.cs:365,411` (Phase 7)
- ResultPanel 499라인 God class 분할 (Phase 8 — W3 F1 복원 메타 작업 중 자연스럽게 손볼 수 있음)
- 4 HUD TextHUDBase 통합 (Phase 9 — W5 폴리시 때 인터리브 가능)

---

## 미커밋 (모두)

세션 18~20 작업 전부:
- Phase 4/5/6 코드 + 학습 문서
- Unity-cli 워크플로 (settings.json, rules, memory)
- Obsidian vault 분리 (옵시디언/) + 경로 갱신
- `_Recovery/` 정리 (93 archive 이동)
- 차/도로 fix 시리즈 (FixFloatingCarAndRoad, AlignBaseHorizon, RemoveFlatGrassCubes, RaiseRoadTiles, FixRoadAsphaltZFight)
- 한글 폰트 복구 (FixKoreanFontAtlas)
- 화물 손상 인과 변경 (CargoBox + CargoShakeSimulator)
- TuneCargoShake 인스턴스 값
- 알림 사운드 hook (~/.claude/settings.json)
- 기획서 .pptx + 생성 스크립트
- Vertical Slice spec + 결정 D-069 + session log

---

## 자주 참조하는 외부 문서

- USP·컨셉: `옵시디언/GDD/overview.md`
- 결정 채널 9: `옵시디언/GDD/decision-channels.md`
- 결정 이력: `옵시디언/notes/decisions-log.md`
- 시스템 스펙: `옵시디언/GDD/systems/`
- 학습 노트: `옵시디언/GDD/architecture/phase{1..6}-*.md`
- 데모 spec: `옵시디언/dev-plan/2026-05-13-vertical-slice-design.md`

---

## 힌트 (다음 세션을 위한 메모)

- Unity 검증은 **반드시 unity-cli**. Editor 켠 채. batch/MCP 금지.
- "데모인데 풀게임 기능 다 넣자" — 마음가짐만. 실제 추가는 W1~W4 표 기준. 신규 시스템은 `ideas-parking.md` 직행.
- Brainstorming → writing-plans 흐름 따를 것 (Superpowers 스킬).
- 6주는 AI 가속해도 1인분 4개월치. 시각 폴리시 양보 가능, 코어 시스템 양보 X.
- 화물 손상 인과 변경 (D-068) — Play 테스트 필요. 박스 충돌 빈도 확인 후 boxWallDamageMul 튜닝.
