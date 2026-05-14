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
- **세션 24 (2026-05-14~15)**: 하네스 점검 + 버그 5건 수정 (차 스폰 겹침 / concave collider / HUD 전화면 표시 / **포장 물건≠트렁크 물건** / PreRunSetupPanel). 세션 18~24 일괄 커밋 (`0a39d66`) 후 기능 단위 전환. **W2 채널 8 완료** — MiniMapHUD + DirectionArrow. **D-071 하네스 운영 확정** — 규칙 3건+ 검증 시 즉시 발효, 평가는 B 트랙 사후 (`session.md`에 B 트랙 섹션 추가). **채널 5 Plan 1 완료** (`bd53ea2`) — 다물건 트렁크 물리: N개 큐브 각자 슬라이드·충돌·손상 (CargoShakeSimulator 단일 박스→N 아이템). 채널 5 설계 결정: 따로 포장 / 최대 3개 / 플레이어 드래그 배치. AI 약점 2건 incidents.md 기록.

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

1. **🟡 도현 manual play — 검증 빚 누적** (전부 unity-cli 자동 step 검증만, 직접 플레이 안 함):
   - 세션 24 버그 수정 5건 — 특히 **트렁크 CCTV에 내가 포장한 큐브(리본 포함)** 가 보이는지 (실제 페인팅 후).
   - W2 채널 8 — 미니맵(좌상단)·방향 화살표(차 위) 런 중 표시.
   - 채널 5 Plan 1 — Order_C(itemCount=3) 잡으로 3개 화물이 트렁크에서 슬라이드·충돌하는지.
2. **채널 5 Plan 2 — 플레이어 드래그 배치**: 포장 후 트렁크 3D 뷰에 물건을 드래그해 위치 결정, 위치가 물리에 영향. Plan 1의 자동 그리드 배치 대체. (plan 파일 없음 — Plan 1 패턴 따라 작성 후 실행)
3. **채널 5 Plan 3 — 박스 크기 업그레이드**: UpgradeEffect.BoxCapacity + UpgradeDef 2개 + OrderCard 회색표시 (잡 itemCount > capacity면 잠금). 멀티 잡 카탈로그 확장.
4. **W3 차고/부품 시스템 결정** — 도현 구체 결정 후 spec 진입.
5. 화물 손상 인과 변경 (D-068) Play 테스트 — 박스 충돌 빈도 확인 후 boxWallDamageMul 튜닝.

### 미해결 / 메모

- **채널 5 슬라이드 튜닝**: velocityDeadZone(0.02)이 약한 사면(25°)의 프레임당 증분보다 커서, 약한 슬로프에선 화물이 안 미끄러짐. 운전 G(급제동·코너)면 작동. 옛 단일 박스 코드도 동일했던 **선재(先在) 튜닝 이슈** — Plan 1에서 안 건드림. 데모 슬로프에서 화물 안 미끄러지면 deadzone 낮추거나 accelScale 올릴 것.
- 미니맵: 배달 **목표** 마커가 다른 마커와 구분 안 됨 — 폴리시 때 강조 (W4+).
- ResultPanel 라벨↔값 간격 큼 — 도현 눈으로 확인.
- 포장 박스가 밋밋한 회색 큐브 — 폴리시 (W4+).
- `EditorApplication.Step()`은 에디터를 멈춤 → 런타임 거동 검증 전 frameCount 확인 ([[feedback-unity-step-pauses]]).

---

## 잠재 부채 (Phase 7~9 — 데모 일정에 따라 보류 또는 인터리브)

- `IRunController` 추상화 새는 곳 — `as RunController` 캐스팅 4-5곳 (Phase 7)
- `UpgradeSystem.Instance` 직접 접근 `ResultPanel.cs:365,411` (Phase 7)
- ResultPanel 499라인 God class 분할 (Phase 8 — W3 F1 복원 메타 작업 중 자연스럽게 손볼 수 있음)
- 4 HUD TextHUDBase 통합 (Phase 9 — W5 폴리시 때 인터리브 가능)

---

## 미커밋

없음 — 전부 기능 단위로 커밋됨.
최근: `0a39d66` 세션18~24 일괄 → `606ce2e` 미니맵 → `06c6c3a` 방향화살표 → `16f28c7` 세션24 docs → `baf7fd4` D-071+채널5 plan → `bd53ea2` 채널5 Plan1.

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
