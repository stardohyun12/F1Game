# CODEMAP — 파일 → 층 → 맡은 것 (D-287, 과제 2102·2135 1~2단계 · 2201 3~6단계 갱신)

층 순서(ARCHITECTURE.md): Core ← World ← Signals ← Interact/Stages ← View ← Debug. 왼쪽은 오른쪽을 모른다.
**asmdef 로 실제 갈림(6단계, 컴파일러가 막는다).** 화살표 = 「참조한다」. 거꾸로 쓰면 컴파일 에러.
- `PitCrew.Core`(엔진 없음, 참조 없음) · `PitCrew.Core.Tests`(EditMode)
- `PitCrew.Signals`(엔진 없음) → Core
- `PitCrew`(본체 `Scripts/PitCrew.asmdef` — 차·길·건물·OnFoot·Packing·UI·Visual·Services) → Core, Signals
- `PitCrew.Interactables` → PitCrew, Core, Signals
- `PitCrew.Presentation` → PitCrew, Core, Signals, Interactables
- `PitCrew.Stages` → PitCrew, Core, Signals, Interactables, Presentation
- `PitCrew.Debug` → 위 전부 · `PitCrew.Editor`(F1/Assets/Editor) → 위 전부
본체·물건·화면은 감독(OnFootDirector)을 **못 본다**(다른 어셈블리가 위에 있다). 감독에 닿을 일이 있으면 신호(SignalBus)·칠판(DayState/DayView)·인자로.
⚠ 씬은 MonoBehaviour 를 「어셈블리::타입」으로도 찾는다. **파일 이름 ≠ 클래스 이름**인 MonoBehaviour 를 다른 어셈블리로 옮기면 Missing 이 된다 → 불변 검사 「스크립트 Missing 0」.
경로는 `F1/Assets/Scripts/` 기준. 괄호 = 줄 수.

## Core — 숫자와 규칙 (PitCrew.Core, noEngineReferences)
- `Core/Rules.cs` (23) — 수리비 300 · 첫 배달 1,200(기본값, World 에셋이 시작 때 덮어씀 — `static int`) · 문 앞 정산 = 기본 × 배율 반올림
- `Core/HandoverTier.cs` (19) — 화물 3단(80/40%) · 배율 1.0/0.7/0.3 · 반응 대역 글
- `Core/VacationCalendar.cs` (22) — 28칸 · 주말 토·일(1일째 월요일 = 가설)
- `Core/OrderRules.cs` (18) — 첫 배달 단계 글(없음→…→건넴) · 카트 칩 글
- `Core/DayLog.cs` (59) — 방학 며칠째 · 오늘 만난 사람 · 오늘 번 돈 − 수리비
- `Core.Tests/CoreRulesTests.cs` (68) — 위 규칙 17개 테스트
- `Core.Tests/CoreTestRun.cs` (36) — exec 로 테스트 돌리고 결과 파일 쓰기

## World — 명세에서 온 데이터 (PitCrew.World → Core, Signals · D-289)
원천 `world/world.yaml`(59줄) → `python tools/world/gen.py`(검사 `--check`, 에디터 exec 로 에셋 생성·덮어쓰기, 같은 guid) → `F1/Assets/World/*.asset`. `tools/world/schema.json` = 형식, `tools/world/_out/` = 생성 중간물. .asset 은 손으로 안 쓴다.
- `World/PitCrew.World.asmdef` — 참조: Core, Signals. 참조하는 쪽: `PitCrew.Stages`, `PitCrew.Debug`
- `World/WorldPlaces.cs` — 장소 13(이름·역할·발·방향·카트 규칙) + 배달 지점 6(DP_*) + `sourceHash`(yaml 해시, 낡은 에셋 잡이)
- `World/WorldPeople.cs` — 주민 4(어디·역할·확정/가안). 아직 읽는 코드 없음
- `World/WorldOrders.cs` — 화물 5 + 주문 1(첫 배달: 줄리아·달걀 한 판·1200·DP_tea). 아직 읽는 코드 없음
- `World/WorldNumbers.cs` — 수리비·파손 3단·배율·노을 초·달력 칸·첫 주문 값. `ApplyTo()` 가 Core `Rules.RepairCost`·`FirstOrderReward` 에 밀어 넣음(Core 는 에셋을 못 읽는다)
- `World/WorldData.cs` — 로더(에디터 = AssetDatabase, 플레이어 = Resources 대체 → 에셋이 Resources 밖이라 지금은 null)
- 연결 2곳: `Stages/OnFootDirector.cs` Awake 첫 줄 `WorldData.Numbers?.ApplyTo()` · `Debug/DebugPlaces.cs` 목록이 `WorldData.Places` 에서 만들어짐(`Reload()` 로 다시 읽기)
- 불변 검사 `WorldInvariants` 「world.yaml 좌표 ≤0.5m」: 해시·DP_ 좌표·집 방·정비소/보관소 카트 자리를 씬과 대조

## Signals — 흐름 신호 (PitCrew.Signals, noEngineReferences)
- `Signals/Signals.cs` (82) — 신호 enum + SignalBus: Raise(신호, 글, arg)·On/Off(듣기)·Mark(로그 한 줄 부탁)·RaiseStep
  - 흐름 9개(쪽지·포장·실음·도착·건넴·노을·잠·새날·열쇠) + 물건 → 단계 부탁 10개(TalkAsked·BoardOpened·PackOpened·BoxTaken·CartBoarded·RepairAsked·DoorKnocked·CalendarOpened·SeatTaken·DiaryClosed)
- `Signals/DayState.cs` (25) — 하루 상태 칠판(정적). 감독이 매 프레임 써 넣고(SyncDay) 물건이 읽는다 — 물건은 감독을 모른다

## Stages — 하루 단계 (PitCrew.Stages)
**단계 8개 전부 독립 클래스**(MonoBehaviour 아님, `DayStage` 상속). 감독이 Awake 에서 만들고 Enter/Tick/Exit/Always 를 부르며, 단계는 신호를 Listen 한다.
감독 partial 조각(Day*.cs·Director*.cs)은 단계가 아니라 감독 자신의 몸(프레임 루프·대화·카메라·HUD·QA 입구).
- `Stages/DayStage.cs` (35) — 단계 바탕: Mode · Enter/Tick/Exit/Always · NewDay(DayStarted 때 자기 것만 되돌림) · Listen/Unplug
- `Stages/OnFootDirector.cs` (219) — 감독 본체: 상태(Mode)·Awake(단계 8개 만들기)/Start(Plug: 시선 배선·신호 귀)·SyncDay·Mark/로그
- `Stages/DirectorWiring.cs` (69) — 감독 씬 배선(인스펙터 필드)만. 단계는 직렬화를 못 해서 씬 값은 감독에 남고 단계가 `d.필드` 로 읽는다
- `Stages/DirectorFacade.cs` (80) — 바깥(봇·디버그·불변 검사)이 쓰던 감독 공개 이름 → 단계 값 전달만. 리플렉션 대상 EnterPack·TakePacked·EnterCart 도 여기.
  ⚠ 에디트 모드(불변 검사)엔 Awake 가 안 돌아 단계가 null — 에디트 모드에서 읽히는 Garage 는 지연 생성(`Garage => garage ??= …`)
- `Stages/DayDirector.cs` (89) — 매 프레임 Update: 시선 물건 하나 → 안내 글 → E 면 물건.Use() · Board/Diary 모드는 단계.Tick
- `Stages/DayReset.cs` (23) — 다음 날 아침 감독 쪽 되돌리기. 단계 것은 각 단계 NewDay 가 한다
- `Stages/Stage_Morning.cs` (78) — **클래스.** 달력 열기·닫기·옛 배달 점 끄기
- `Stages/Stage_Garage.cs` (75) — **클래스.** 사장 대사·Talked·아침 카트 자리·정비(수리)
- `Stages/Stage_Depot.cs` (151) — **클래스.** 맡은 주문(PickedJob)·손에 든 전표(Held)·칠판 줄. 듣기: BoardOpened, NotePicked
- `Stages/Stage_Packing.cs` (124) — **클래스.** 작업대 들어가기·나오기·싼 상자 들기
- `Stages/Stage_Drive.cs` (171) — **클래스.** 카트 타기·내리기·도착·첫 주문 시작/끝
- `Stages/Stage_Door.cs` (193) — **클래스.** 상자 들고 다니기·문 두드리기·건네기
- `Stages/Stage_Return.cs` (106) — **클래스.** 노을 올리기·자리 앉기/서기
- `Stages/Stage_DayEnd.cs` (60) — **클래스.** 일기 펼침·아침. 듣기: SleptIn, DiaryClosed→날짜+1·DayStarted
- `Stages/DayTalk.cs` (109) — 대화(말풍선 줄·화자 보기)·자막
- `Stages/DayLook.cs` (97) — 카메라 돌려 보여주기(목적지·물건)
- `Stages/DayScreen.cs` (207) — 배달 안내 글·HUD 모으기·옛 UI 만들기 · LateUpdate 에서 DayView 적기(WriteView)
- `Stages/DayDebug.cs` (174) — 순간이동·도착 흉내 등 QA 진입점(#if UNITY_EDITOR)

## Interactables — 손에 닿는 물건 (PitCrew.Interactables)
물건 = 「E 로 뭘 하나」만 안다: 안내 글(Prompt)·쓸 수 있나(CanUse)·칩 자리(Anchor)·E(Use = 신호 쏘기). 감독을 모르고 DayState 만 읽는다.
- `Interactables/IInteractable.cs` (27) — 인터페이스 + 바탕 클래스 Interactable(CanUse 기본 = 글이 "E " 로 시작)
- `Interactables/Gaze.cs` (155) — 시선 레이 하나로 지금 보는 물건 고르기 · 물건 목록 만들기
- `Interactables/Note.cs` 쪽지 · `Board.cs` 칠판 · `PackTable.cs` 작업대 · `Cart.cs` 카트 · `Repair.cs` 고치기 · `Boss.cs` 사장
- `Interactables/KnockDoor.cs` 배달지 문 · `Door.cs` 경첩 문 · `Seat.cs` 낚시 자리 · `WallCalendar.cs` 달력 · `Bed.cs` 침대 · `Diary.cs` 일기
- `Interactables/DepotPackSpot.cs` (166) — 보관소 포장 자리(6단계에 OnFoot/ 에서 옮김 — PackBench 를 쓰고 Cart/Gaze/PackTable 이 쓴다)
- `Interactables/Pack/PackBench*.cs` — 작업대 본체·손·시트·이동·흐름·소리·Sim·봇·Day·Qa2
- `Interactables/Pack/PackBox*.cs` — 상자·채움 재기·흔들기
- `Interactables/Pack/PackItem.cs` (99) — 넣는 물건(PackItem·PackItemKind)
- `Interactables/Pack/BubbleSheet.cs` (338) — 뽁뽁이 시트

## 본체(PitCrew)에 남긴 것 — 6단계 이유 있음
- `Packing/PackSupply.cs` (16) — 작업대 재료 더미(PackSupply·PackSupplyKind). 씬이 「PitCrew::PitCrew.PackBench.PackSupply」로 찾아서 본체에 둔다. 제 파일로 떼어 Tape·Labels Missing 도 사라짐
- `UI/ScreenFonts.cs` (22) — `IScreenFonts`(본문·작은 글꼴) + `ScreenFonts.Find()`. 칠판·달력(본체)이 DotScreenUI(화면) 대신 이걸로 글꼴을 얻는다
- `Visual/LowResRenderTest.cs` (148) — 360 저해상도 렌더(값 그대로). 본체 5곳(FishingSpot·HeldPaper·A7LightToggles·PCLookScene·SunsetLook)이 써서 Presentation 에서 옮김
- `OnFoot/OpeningSequence.cs` — 감독 대신 `Begin(player, calendar, bed, onFoot)` 인자로 받는다
- `Garage/RepairSpot.cs` — 감독 대신 `Services.Car` → 씬의 ArcadeCarController 로 카트를 찾는다

## Presentation — 그리기 (PitCrew.Presentation)
- `Presentation/DayView.cs` (56) — 화면용 칠판(정적). 감독이 매 프레임 적고(실행 순서 0) DotScreenUI(200)가 같은 프레임에 읽는다. 화면 → 감독 참조 0
- `Presentation/DotScreenUI.cs` (275) — 도트 UI 본체: 필드·만들기 + PixelSnapText · IScreenFonts 구현
- `Presentation/DotScreenUI.Widgets.cs` (141) — 그림 조각(상자·꼬리·깃발·점·화살)
- `Presentation/DotScreenUI.Frame.cs` (113) — LateUpdate·돈 글자·옛 HUD 숨기기/되돌리기
- `Presentation/DotScreenUI.Dest.cs` (223) — 목적지 깃발·빛기둥·지도
- `Presentation/DotScreenUI.Drive.cs` (180) — 주행 화살·전표·E 칩·카트 표시·말풍선
- `Presentation/DotScreenUI.Texture.cs` (278) — 로파이 질감 스프라이트·인쇄 리그
- `Presentation/DotScreenUI.Bubble.cs` (162) — 말풍선·전표·칠판 판을 세상에 붙이기
- `Presentation/DotScreenUI.Diary.cs` (132) — 일기장
- `Presentation/PackBenchCursor.cs` (171) — 작업대 조준점·칩

## Debug (PitCrew.Debug)
- `Debug/*` — DebugWindow·DebugPlaces·봇(OnFootRouteBot·TownPathBot)·GarageDriveCheck·WorldInvariants(16개)·WorldSnapshot·DayLoopCheck·각종 Probe. 감독을 직접 읽어도 된다(맨 위 층)

## Services (본체, 층 밖 — 옛 서비스 로케이터)
- `Services/Core.cs` · `Services/GameContext.cs` · `Services/RunMetricsRecorder.cs` — 옛 Core/ 폴더에서 비켜 둠(이름 겹침 방지)
