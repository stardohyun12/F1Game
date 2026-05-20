# Active Context — PitCrew

> 매 세션 시작 시 읽고, 끝날 때 갱신.
> 안정 규칙은 `.claude/rules/`에. 여기는 **상태**만.

---

## 지난 세션 요약

- **세션 28 (2026-05-20): 혼자검증 + W3 차고 마무리 + 비주얼 폴리시 시작 + AI 자산 파이프라인.**
  - git 노이즈 264파일(.meta 재직렬화 등) 청소 + 누락 meta 보완(`e156826`).
  - **차고 검증**: verifier subagent 12/12 ALL GREEN. 헤더 겹침·잘림 수정(`139fb8e`). 차고 캔버스 "안 보임"은 **내 인위적 테스트(MainMenu 캔버스 살린 채 Show)+ScreenCapture 다음프레임 타이밍** 문제 — 실제 플로우(정산→차고로)에선 정상(서브에이전트 확인). order 4000→7000 안전망(`faf7fc2`). 교훈 [[feedback-test-real-flow-not-reflection-harness]] / [[feedback-clean-replay-before-services-diag]].
  - **비주얼 폴리시 진단**: "진짜 게임 느낌" = 에셋 아니라 **아트디렉션**. 라이팅 shadows=None/Default-Skybox/ambient1.0/fog off가 평평함 80%.
  - **낮 무드 시스템**(`1fa1da1`): MoodProfile SO + MoodController(Awake 적용, [ExecuteAlways]). 그림자+따뜻한 태양+Flat앰비언트 대비+fog+포스트 그레이딩. "황혼/야간 나중에 다"는 SO 복제로 가능.
  - **그라데이션 스카이박스**(`0152e8e`): GradientSky.shader 자가 작성(URP HLSL 3색).
  - **차고 F1 머신**(`f008d25`): placeholder 텍스트 → Kenney race.fbx RenderTexture 렌더 + 회전. 메인캠 못 보는 먼 곳 스테이지.
  - **카드보드 화물**(`5ce021a`): 회색 큐브 → 프로시저럴 카드보드 박스(CargoBoxAppearance, 텍스처 코드 생성). 메시·콜라이더 불변→포장 무관.
  - **AI 자산 파이프라인**: Meshy API 키 연결(`.env.local`, gitignore됨, 잔액 2080). **교훈: Meshy 등 AI-3D는 각진 박스에 흐물 → Kenney/프로시저럴이 우월. 유기적 hero만.** 2D 아이콘은 gpt-image-1(키 미설정). OpenAI 키 받으면 부품·화물·UI 아이콘 세트.
- 세션 18 (2026-05-08): Phase 1 DIP/Service Locator + Phase 2 Template Method + Phase 3 Modifier Chain.
- 세션 19 (2026-05-11~12): Phase 4 AudioCueBus + Phase 5 TireTypePresets + Phase 6 Peer Review 6건. 미커밋.
- 세션 20 (2026-05-12~13): Unity 검증 도구 Coplay MCP → **unity-cli** 전환. Obsidian vault `옵시디언/` 분리. `_Recovery/` 솎아냄 (108→15). 차/도로 부유·기울임·z-fight 해결. 한글 폰트 atlas/material sub-asset 복구. **화물 손상 인과 변경** (차 G→박스 슬라이드→trunk 박음→손상). 알림 사운드 hook. Superpowers 플러그인 설치. 기획서 .pptx 생성. **Vertical Slice 데모 6주 결정 (D-069)**.
- 세션 21 (2026-05-13): 기획서 W1~W6 폐기, 3D 포장 미니게임 17 Task 구현 (Subagent-driven). D-070 결정. wrap 핸드오프까지 완료 (`RunController.TransferWrapStateToCargo`). 다물건(1~3) items[1+] 폐기는 후속.
- **세션 23 (2026-05-14, 자율 2시간 루프)**: CaptureFullScreen.cs. **W1 채널 3 (맵 해금) 완료** — `Zone`/`ZoneRegistry` SO, `ZoneUnlockSystem` (wallet 차감·auto-unlock·GetZoneOf), `OrderBoard.PickOffer` ZoneUnlockSystem 우선, `OrderCardUI` zone 라벨/tint. Seed: Downtown/Harbor/Industrial. ZoneUnlockPanel UI는 **도현 요청으로 제거** → autoUnlockOnAffordable. W2 brainstorm doc 작성.
- **세션 24 (2026-05-14~15)**: 하네스 점검 + 버그 5건 수정 (차 스폰 겹침 / concave collider / HUD 전화면 표시 / **포장 물건≠트렁크 물건** / PreRunSetupPanel). 세션 18~24 일괄 커밋 (`0a39d66`) 후 기능 단위 전환. **W2 채널 8 완료** — MiniMapHUD + DirectionArrow. **D-071 하네스 운영 확정** — 규칙 3건+ 검증 시 즉시 발효, 평가는 B 트랙 사후 (`session.md`에 B 트랙 섹션 추가). **채널 5 Plan 1 완료** (`bd53ea2`) — 다물건 트렁크 물리: N개 큐브 각자 슬라이드·충돌·손상 (CargoShakeSimulator 단일 박스→N 아이템). 채널 5 설계 결정: 따로 포장 / 최대 3개 / 플레이어 드래그 배치. AI 약점 2건 incidents.md 기록.
- **세션 25 (2026-05-15)**: **채널 5 Plan 2** (트렁크 자유 배치 — 포장→배치→출발 흐름, 마우스 드래그로 트렁크 바닥에 큐브 놓기, `714d489`). **채널 5 Plan 3** (적재함 크기 업그레이드 — TrunkExpand1/2, BoxCapacity 파생, OrderCard 회색 잠금, ResultPanel 5→7 카드, `a0c5a92`). **채널 5 전체 완료** (Plan 1·2·3). 그 다음 도현 결정: "오더보드/차고 시스템은 기획을 조금 더" → brainstorming 진입. 차고/부품 시스템은 레퍼런스 리서치 (`옵시디언/research/garage-parts-reference.md` 완료) 후 후속, OrderBoard 개편 spec/plan 작성 + 인라인 구현. **채널 8 (주문→경로 계획 흐름) 완료** — `OrderJob.destinationLabels` 데이터 모델, `DeliveryManager.ActivateRoute` 경로 기반, `RoutePlanScreen` 신규, `OrderBoard` 맵 중심 레이아웃 A. D-070(목적지 1개) 부활.
- **세션 26 (2026-05-15~16)**: manual play 피드백 폭주 → fix·polish 사이클.
  - **모델 정정** — 1잡=1곳 (`9578bea` `ea2946a` `ae69ca2`). OrderBoard 다중선택 UI + 출발 버튼.
  - **자기검증 3종 즉시 발효** (D-071, `f7d48dd`) — `project_rules/session.md` 코드 변경 commit 전 compile/runtime/visual 셋 다 통과 의무. 같은 세션 4회 반복 → incidents.md 등재. 메모리 [[feedback-self-verify-loop]] + [[reference-input-system-mode]].
  - **폴리시 4영역 완료** — 보드/경로 (`5b60634` `39389f6`), 포장 (`02cd082`), 트렁크캠+미니맵+화살표 (`69c09b9`), 정산 다물건 (`a48cedc`). subagent dispatch 1회 (출발 버튼 overflow 진짜 원인 = VLG childControlHeight).
  - **status audit + 기획서 14슬라이드 현행화** (`83286df` `efb20c7`).
  - **수치 audit 시스템** (`0f1a577`) — RunMetricsRecorder + CSV. 3런 측정 결과 **한 런 8~12초 / 거리 49~110m**. 데모 목표 5~15분과 200~900배 갭. **도시 78×54m 너무 작음이 본질** → 다음 세션 "맵 넓히기" 결정.
  - 배치 단계 Q/E 회전 (`c056910`).
  - 도현 메타 자각: 결단 회피 → 정리/audit/리서치로 회피 패턴. 해법은 작게 쪼개기.

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

## 세션 27 진행 상태 (2026-05-19, 도현 외출로 일시 정지)

**경로 전환**: 맵 넓히기 brainstorm → W3 차고 시스템으로 피벗 (도현 결정). R1 맵은 측정 단계에서 적용 예정.

**기획 마감 (D-073~093, 21개 결정)** — `옵시디언/dev-plan/2026-05-19-garage-realtime-planning.md` + `2026-05-19-restore-economy.md`:
- 6 부품 1 티어, 사이드뷰 분해도, 메커니즘 unlock형
- 가격 1000/4000/4000/8000/12000/25000 (지수형, 자유 게이팅, 엔진 1000 첫 도파민)
- 자금 누적 / 정산액 비례 페널티 / Yes/No 차고 prompt
- UI: 머신 가운데 + 카드 6장 하단 / 회색+부족 / 2~3초 컷씬
- 엔진 +20%/+30% / 변속기 K(다운)/L(업) / 부스트 Space / 타이어 4종+영구마모+펑크=잡실패
- 그랑프리 후속 / 서스 점프대 맵 디자인 트랙으로

**구현 진행 (Task 1~11/13 완료, plan: `docs/superpowers/plans/2026-05-19-garage-restore-system.md`)**:
- Task 1: F1Part SO + Catalog + F1PartId (`5be654a`, 경로 fix 포함)
- Task 2: 6 F1Part asset + Catalog asset (`ec25cb7`)
- Task 3: F1RestoreSystem + Services.Restore + GameContext (`8298703`)
- Task 4: 엔진 효과 +20%/+30% (`06bddc1`)
- Task 5: 변속기 K/L 수동 모드 (`41ddc23`)
- Task 6: 에어로 BoostSystem Space (`db5e93f`)
- Task 7: 타이어 + 영구 마모 + 펑크=잡실패 (`8b49de2`)
- Task 8: 도색 머티리얼 swap + 서스 flag (`69058d2`)
- Task 9: GarageScreen UI + Cutscene (`a2a00ba`)
- Task 10: ResultPanel '차고로' 버튼 (`c327645`)
- Task 11: ComplexTrack 씬 wiring — 4 GameObject + GameContext (`9ed1534`) — `Services.Restore=True Boost=True TireWear=True` ALL GREEN

**남은 Task (다음 세션 첫 작업)**:
- **Task 12**: 통합 verifier subagent — 12 항목 검증 (Services 매핑·6 부품 TryRestore·엔진 ratio·Gears.ManualUnlocked·Boost.IsUnlocked·Tires.SelectionUnlocked·TireWearStore 동작·CanAfford·Play 에러 0·integrity·자금 carryover)
- **Task 13**: session-log + activeContext 갱신 (이 파일)

**발견된 이슈 (다음 세션 결정 필요)**:
1. **TireType 실제 3종** (Soft/Medium/Hard) — D-091 기획 4종(드라이/웨트/소프트/하드)과 어긋남. Subagent가 3종으로 매핑 (Soft 1.5 / Medium 1.0 / Hard 0.5). 결정: (A) 3종 유지 (B) Dry/Wet 추가 (C) Medium→Dry rename + Wet 추가.
2. `IUpgradeSystem.OnWalletChanged` 인터페이스 미노출 → GarageScreen Update 폴링 우회. 잠재 부채.
3. `IUpgradeSystem.Spend(int)` Task 3 도중 인터페이스에 추가 (이전 누락).
4. Task 4: 코드 변수 `accelForce`라 plan `effectiveAccelTime` → `effectiveAccelForce`로 적응. +30% 가속 달성.
5. RunStage namespace 전역 — BoostSystem에서 `Services.Run as RunController` 캐스팅. 잠재 부채.

**manual play 빚 (차고 흐름 한 번도 안 굴림)**:
- 정산 → "차고로" 클릭 → GarageScreen 진입
- 엔진 복원 후 다음 런 속도 체감
- K/L 수동 변속 작동
- Space 부스트 (게이지 UI HUD 없음 — 후속)
- 펑크 = 잡 실패 트리거 (마모 30% 강제 후 굴려보기)

**다음 세션 첫 행동**:
1. Task 12 dispatch (verifier subagent) — ALL GREEN 확인
2. Task 13 (session-log + activeContext)
3. manual play 5~10분 — 차고 전체 흐름
4. TireType 4종 결정 (도현 답변)
5. R1 맵 적용 + RunMetricsRecorder 5런
6. 측정 결과 보고 가격 재튜닝 여부

---

## (구) 다음 세션 첫 작업 — 맵 넓히기

세션 27이 차고로 피벗했음. 맵은 측정 단계에서 적용 (이미 R1 spec 완료: `docs/superpowers/specs/2026-05-16-map-expansion-r1-design.md`).

1. **R1 맵 적용** — 도시 17배 (78×54 → ~330×230 어림). 큰 작업이라 brainstorm 한 라운드 후 진입 권장:
   - zone 3개 (다운타운/항구/공업) 영역 어떻게 분할
   - 도로 자동 배치 (`Assets/Editor/_Recovery/` 의 BuildingPlacer/RoadBuilder 패턴, asset.md 규칙 준수)
   - 배달지 8개 → 더 멀게 재배치 + zone별 분포
   - 마커 간 평균 거리 목표: ~500m (한 잡 1~2분)
   - roadMask 수정 → `TrackConnectivityVerifier` 자동 실행 의무 (asset.md)
   - heightMask 도입 시 slope tile만 사용 (D-050)
2. 맵 확장 후 reward/잡/zone unlock cost 모두 재튜닝 필요 (현재 8초 기준이라 17배 시간이면 보상도 비례).
3. **다시 RunMetricsRecorder 측정** — 맵 넓힌 후 5런. 한 런이 1~2분으로 늘면 그 다음에 escalation 박을 시간 생김.
4. **이후 순서 (도현 직관 기반)**: 맵 → 측정 → escalation brainstorm → 차고/부품 → 화물 비주얼.

### 미해결 / 메모

- **세션 26 manual play 빚 (여전히)**: 채널 5 Plan 1/2/3 + 채널 8 풀 흐름 + 폴리시 4영역 + Q/E 회전. 맵 넓히기 전 한 번 굴려보면 더 정확한 base.
- **데모 컨셉 갭**: 한 런 8~12초 vs 목표 5~15분. 200~900배 차이. **거리 확장 + escalation 둘 다 필요** (거리만으론 부족).
- 도현 결단 회피 패턴 — 큰 결정을 1시간 prototype으로 쪼개기. 맵 확장도 "33×23 → 일단 절반 (160×110) → 측정 → 더 늘림" 단계로.
- 채널 5 슬라이드 튜닝, 미니맵 목표 강조 (영역 3에서 일부 처리됨), 화물 비주얼 회색 큐브 — 세션 26 status audit 참조.
- `EditorApplication.Step()`은 에디터를 멈춤 → 런타임 거동 검증 전 frameCount 확인 ([[feedback-unity-step-pauses]]).

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

## 세션 29 (2026-05-20~21): 화물 원물화 B안 — 와인 파일럿 GREEN

**문제(도현)**: 화물이 포장 전부터 카드보드 박스라 포장이 무의미. → **원물(깨지기 쉬운)이 보이고 버블랩으로 싸는 게 보여야** 의미.
**결정**: B안(원물 유지+랩핑) / Meshy 생성 / 버블랩 / 1번 파일럿(와인) 먼저, UV 엉키면 2번(숨긴 프록시) 폴백.
spec `docs/superpowers/specs/2026-05-20-cargo-raw-goods-design.md`, plan `docs/superpowers/plans/2026-05-20-cargo-raw-goods-wine-pilot.md`.

**커밋**:
- `36cac5c` CargoModelCatalog SO (CargoType→원물, Box/미스=큐브 폴백)
- `c4fb963` Meshy 와인병(refine→remesh 27k) + `scripts/meshy-generate.ps1` + **AssetPreviewTool**(중립 배경 1줄 렌더, Kenney side-by-side — [[feedback-asset-review-before-integration]])
- `88bd787` Cargo_Glass 프리팹(초록 유리 #0C4721, root→정규화 자식) + 카탈로그 와이어
- `bab5125` SpawnItem 카탈로그 조회 + WirePainterFor 자식 메시 콜라이더
- `0cca578` 작업대 와인병 크기 0.5→1.0

**검증**: UV 프로브(앞0.38/뒤0.14 분리=겹침無→폴백불필요) + **검증 서브에이전트 PILOT GREEN 6/6**(병 스폰 17610v / 커버리지8.7% / 흡수 칠한0.25·안칠한0.0 / Play에러0 / 배치 CargoSimRoot).

**아트 교훈**: Meshy 사실적(흰 100k)은 Kenney 로우폴리 월드와 결 안 맞음 → remesh 저폴리 + **쨍한 단색**이 톤 맞춤 핵심. "유기물=Meshy" 가정은 화물(Kenney 옆)엔 재검토 필요. [[feedback-asset-review-before-integration]] / [[feedback-one-decision-surface]].

**남은 것 (다음)**:
- **버블랩이 병에 감기는 시각 확인** — 기능 GREEN인데 wrap 리본이 곡면에 붙은 모습은 manual play로 봐야(브러시 스트로크). **도현 manual play 1순위**.
- 포장 작업대 **받침 plane**(병이 허공에 뜸 — 0cca578 Feels-off).
- 나머지 3종(Eggs 계란판 / Fish 생선 / Cake 케이크) Meshy 생성→카탈로그 엔트리 복제 (`meshy-generate.ps1` 재사용, art 교훈 적용: 로우폴리+단색).
- 텍스처(현재 단색) 라벨/유리 질감은 후속. Meshy 잔액 ~2070.

## 미커밋

없음.

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
