# Active Context — PitCrew

> 매 세션 시작 시 읽고, 끝날 때 갱신.
> 안정 규칙은 `.claude/rules/`에. 여기는 **상태**만.

---

## 지난 세션 요약

- 세션 18 (2026-05-08): Phase 1 DIP/Service Locator + Phase 2 Template Method + Phase 3 Modifier Chain.
- 세션 19 (2026-05-11~12): Phase 4 AudioCueBus + Phase 5 TireTypePresets + Phase 6 Peer Review 6건. 미커밋.
- 세션 20 (2026-05-12~13): Unity 검증 도구 Coplay MCP → **unity-cli** 전환. Obsidian vault `옵시디언/` 분리. `_Recovery/` 솎아냄 (108→15). 차/도로 부유·기울임·z-fight 해결. 한글 폰트 atlas/material sub-asset 복구. **화물 손상 인과 변경** (차 G→박스 슬라이드→trunk 박음→손상). 알림 사운드 hook. Superpowers 플러그인 설치. 기획서 .pptx 생성. **Vertical Slice 데모 6주 결정 (D-069)**.
- 세션 21 (2026-05-13): 기획서 W1~W6 폐기, 3D 포장 미니게임 17 Task 구현 (Subagent-driven). D-070 결정. wrap 핸드오프까지 완료 (`RunController.TransferWrapStateToCargo`). 다물건(1~3) items[1+] 폐기는 후속.
- **세션 23 (2026-05-14, 자율 2시간 루프)**: CaptureFullScreen.cs. **W1 채널 3 (맵 해금) 완료** — `Zone`/`ZoneRegistry` SO, `ZoneUnlockSystem` (wallet 차감·auto-unlock·GetZoneOf), `OrderBoard.PickOffer` ZoneUnlockSystem 우선, `OrderCardUI` zone 라벨/tint. Seed: Downtown/Harbor/Industrial. ZoneUnlockPanel UI는 **도현 요청으로 제거** → autoUnlockOnAffordable. W2 brainstorm doc 작성.
- **세션 24 (2026-05-14)**: 하네스 점검 (UI/게임플레이) + 버그 5건 수정. **세션 18~24 일괄 커밋** (`0a39d66`) 후 기능 단위 커밋 전환. 버그: 차 스폰↔배달마커 겹침 / concave MeshCollider 에러 / IntegrityHUD·CargoView 전 화면 표시 (BarHUDBase.ShouldShow 훅) / **포장 물건 ≠ 트렁크 CCTV 물건** (CargoShakeSimulator.MountWrappedItem — 포장 큐브를 SimCam이 비추는 Box 자리에 마운트) / PreRunSetupPanel 빈 오브젝트. **W2 채널 8 완료**: MiniMapHUD (`606ce2e`) + DirectionArrow (`06c6c3a`). AI 약점 2건 incidents.md 기록.

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

## 다음 세션 (세션 25) — 첫 작업

1. **도현 manual play — 세션 24 수정 5건 확인** (지금까지 unity-cli 자동 step 검증만 함, 직접 플레이 필요):
   - 차 스폰: CentralCross 마커 안 겹치고 도로 위인지.
   - 포장 → 출발 → **트렁크 CCTV에 내가 포장한 큐브(리본 포함)가 보이는지** (핵심 — 실제 페인팅 후 확인).
   - IntegrityHUD("상자 100%")·트렁크캠이 주문/포장/정산 화면에서 숨고 런 중에만 보이는지.
   - 미니맵(좌상단)·방향 화살표(차 위) 런 중 표시, 비런 숨김.
2. **🔴 채널 5 (박스 크기) — 도현 결정 필요 (W2 차단됨)**: "한 잡당 동시 화물 수 cap"인데 **다물건 잡이 실제로 작동 안 함** (RunController가 items[0]만 마운트, CargoShakeSimulator는 박스 1개, itemCount Range(1,3)). 결정해야 빌드 가능:
   - 다물건 n개가 트렁크에서 어떻게? (나란히 / 쌓기 / 각자 흔들림·손상?)
   - 각 물건 따로 wrap? 손상 합산 방식?
   - 결정 나면 채널 5는 UpgradeEffect.BoxCapacity + UpgradeDef 2개 + OrderCard 회색표시로 마무리 (1~2일).
3. **W3 차고/부품 시스템 결정** — 도현 구체 결정 후 spec 진입.
4. 화물 손상 인과 변경 (D-068) Play 테스트 — 박스 충돌 빈도 확인 후 boxWallDamageMul 튜닝.

### 미해결 / 메모

- 미니맵: 현재 월드 그대로 비춤 (도로/건물/마커 점). 배달 **목표** 마커가 다른 마커와 구분 안 됨 — 폴리시 때 강조 고려 (W4+).
- ResultPanel 라벨↔값 간격 큼 — 의도면 OK, 아니면 좁히기 (도현 눈으로 확인).
- 포장 박스가 밋밋한 회색 큐브 — 폴리시 (W4+).
- Screenshot pipeline: timeScale=0 화면은 stale frame 가끔. `EditorApplication.Step()`은 에디터를 멈춤 → 런타임 거동 검증 전 frameCount 확인 ([[feedback-unity-step-pauses]]).

---

## 잠재 부채 (Phase 7~9 — 데모 일정에 따라 보류 또는 인터리브)

- `IRunController` 추상화 새는 곳 — `as RunController` 캐스팅 4-5곳 (Phase 7)
- `UpgradeSystem.Instance` 직접 접근 `ResultPanel.cs:365,411` (Phase 7)
- ResultPanel 499라인 God class 분할 (Phase 8 — W3 F1 복원 메타 작업 중 자연스럽게 손볼 수 있음)
- 4 HUD TextHUDBase 통합 (Phase 9 — W5 폴리시 때 인터리브 가능)

---

## 미커밋

없음 — 세션 24에 전부 커밋 완료. 다음부터 기능 단위 커밋.
최근 커밋: `0a39d66` (세션 18~24 일괄) → `606ce2e` (미니맵) → `06c6c3a` (방향 화살표).

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
