[EDITOR][OPUS][기능별로 다 나누기 — 감독·포장·화면 UI 분리, 동작은 그대로 (D-286)]
도현 원문: "먼저 해서 기능별로 다 나눠". 목적: 과제마다 1,747줄을 읽는 일을 없앤다. **동작·값·씬은 1도 바꾸지 않는다** — 자리만 옮긴다. 시간 상자 150분. 새 기능 금지.
대상: `F1/Assets/Scripts/OnFoot/OnFootDirector.cs`(1,747줄) · `Packing/Bench/PackBench.cs`(1,012줄, partial 포함) · `UI/DotScreenUI.cs`(900줄). 그 외는 손대지 않는다.
나누는 기준 = world/SPEC.md §6 단계. 폴더 `F1/Assets/Scripts/Day/` 아래 단계마다 파일 하나(partial class 가 아니라 **독립 클래스**, 감독이 인터페이스로 부름):
- `DayDirector.cs` — 지금 단계가 무엇인지·전환만. 200줄 안. (OnFootDirector 는 이 이름으로 바뀌거나 얇은 껍데기로 남김 — 씬 참조가 깨지지 않게 컴포넌트 이름·직렬화 필드는 유지. 필드를 옮기면 씬의 참조값도 옮겨 적을 것 — 에디터 exec 로 복사, 손으로 .unity 편집 금지.)
- `Stage_Morning.cs`(방·달력·라디오·집 앞) · `Stage_Garage.cs`(사장·열쇠·수리·카트 받기) · `Stage_Depot.cs`(칠판·쪽지·직원) · `Stage_Packing.cs`(작업대 연결 — 포장 내부는 Packing/ 에 그대로) · `Stage_Drive.cs`(싣기·목적지·방향 표시 켜고 끄기) · `Stage_Door.cs`(문 앞·건네기·결과·돈) · `Stage_Return.cs`(노을·귀가) · `Stage_DayEnd.cs`(침대·일기·달력·다음 날).
- 화면 UI: `DotScreenUI.cs` 는 그리기만 남기고, "언제 무엇을 보일지" 결정은 각 Stage 가 UI 에 요청(ShowChip/ShowArrow/…)하는 식으로. UI 가 게임 상태를 직접 뒤지는 코드는 Stage 로.
- 포장: PackBench 의 Step 상태기·커서·BotRun·Day 연결을 `Packing/Bench/{PackFlow,PackCursor,PackBot,PackDay}.cs` 로. (04 가 만든 Packing/Hands/ 는 건드리지 않는다.)
- 디버그(DebugWindow·OnFootRouteBot·GarageDriveCheck 등)가 옛 이름을 부르면 호출만 고친다.
확인(이게 안 됐으면 실패할 검사): ① 컴파일 0 ② `world_check` 통과(15/15 이상) ③ **분리 전·후 같은 Play 로그**: 과제 01 의 play_log.json 과 같은 순서(없음→쪽지→포장→실음→도착→건넴→노을→침대→일기→아침)가 그대로 나오는지 입력 시뮬로 1회, 단계 전환 시각 ±1초 ④ 씬 참조 끊김 0(Missing 컴포넌트 검사 exec) ⑤ 파일별 줄 수 표(전·후, 모든 파일 400줄 이하). 캡처는 1장(아무 단계)만. 보고 4줄 + 파일 지도(파일→맡은 단계 한 줄씩) → docs/world/CODEMAP.md 로 저장(브리프가 읽게). 씬 저장.
