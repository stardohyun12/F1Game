[EDITOR][OPUS][아키텍처대로 나누기 — docs/ARCHITECTURE.md (D-287), 동작은 그대로]
도현 원문: "우리 게임에 알맞는 아키텍쳐 정하고 그 방식으로 스크립트를 나누자 그리고 구현할 때 하나씩 고칠 수 있는 쪽으로" → **docs/ARCHITECTURE.md 를 먼저 읽고 그 층·폴더·asmdef·규칙 7개 그대로.** 동작·값·씬은 바꾸지 않는다 — 자리만 옮긴다. 시간 상자 240분(끝나면 바로 마무리). 새 기능 금지. 04 가 만든 Packing/Hands/ 는 Interactables(Box/Bubble/Tape)·Presentation(Hands 그리기)로 같이 옮긴다.
순서(각 단계 끝마다 컴파일 0 확인, 중간 커밋은 하지 말고 끝에 한 번):
1. asmdef 7개와 폴더. Core 는 noEngineReferences. 참조 방향은 ARCHITECTURE 대로. 기존 스크립트는 일단 맞는 폴더로 **이동만**(guid 유지 — .meta 같이 이동, 씬 참조 안 깨짐).
2. Core: 돈·파손 확률·수리비·달력(28일·주말)·주문 규칙을 순수 C# 클래스로 꺼낸다(지금 코드의 수식·수치 그대로). `Core.Tests/`(이 프로젝트에서 실제로 도는 쪽: Unity Test Framework EditMode 또는 csproj dotnet test) 테스트 10개 이상: 파손 3단 80/40%·×1.0/0.7/0.3, 수리 −300, 첫 배달 +1,200, 달력 28칸·주말 등. 게임 코드는 Core 를 호출하게.
3. Signals: 이벤트 버스 + 지금 흐름에 실제 있는 신호만(NotePicked, BoxPacked, BoxLoaded, Arrived, HandedOver, DuskStarted, SleptIn, DayStarted, KeyReceived…).
4. Stages: DayDirector(200줄 안) + Stage_Morning/Garage/Depot/Packing/Drive/Door/Return/DayEnd. OnFootDirector 의 단계 코드를 여기로. 씬 컴포넌트 이름·직렬화 필드가 바뀌면 씬 참조값을 에디터 exec 로 옮겨 적는다(.unity 손 편집 금지).
5. Interactables: IInteractable(Prompt/CanUse/Use) + 물건 하나 = 파일 하나(Note, Board, Box, Bubble, Tape, Cart, Door, Bed, Diary, Calendar, Radio…). 단계는 물건을 직접 부르지 않고 신호만.
6. Presentation: DotScreenUI 는 View 모델만 그린다. 상태를 뒤지는 코드는 Stages/Interactables 로. SunsetLook·소리·손도 신호만.
7. Debug: DebugWindow·봇·GarageDriveCheck·WorldInvariants·WorldSnapshot → Debug asmdef. 옛 이름 호출만 고침.
확인(안 됐으면 실패): ① 컴파일 0 ② asmdef 거꾸로 참조 0 ③ Core 테스트 전부 통과(실행 로그) ④ `world_check` 통과 ⑤ **분리 전·후 같은 Play 로그** — 과제 01 play_log.json 순서(없음→쪽지→포장→실음→도착→건넴→노을→침대→일기→아침) 입력 시뮬 1회, 전환 시각 ±1초 ⑥ Missing 컴포넌트·끊긴 참조 0(exec 검사) ⑦ 파일별 줄 수 전·후 표, 전부 400줄 이하. 캡처 1장.
보고 4줄 + `docs/world/CODEMAP.md`(파일 → 층 → 맡은 것 한 줄씩, 브리프가 읽음) + 안 옮긴 것과 이유. 씬 저장.
