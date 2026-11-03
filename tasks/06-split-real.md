[EDITOR][OPUS][아키텍처 2단계 — 단계·물건을 진짜 독립 클래스로, 한 단계씩 (D-287)]
05(b038e04b)는 안전한 1단계였다: Core·Signals·Debug asmdef, Core 테스트 17개, 큰 파일을 partial 로 자름. **아직 OnFootDirector 는 하나의 클래스(partial 14조각)고, UI 는 감독 상태를 직접 읽고, 신호는 쏘기만 하고 듣는 쪽이 없다.** 도현 목표 "구현할 때 하나씩 고칠 수 있는 쪽"은 이게 돼야 한다. docs/ARCHITECTURE.md 그대로. 시간 상자 240분. 동작 불변(05 와 같은 before/after 비교 하네스 DayLoopCheck 로 확인).
순서 — **한 단계 끝날 때마다 컴파일 0 + DayLoopCheck 같음** 확인하고 다음으로. 중간에 시간이 다 되면 거기까지 커밋(끝낸 단계만, 반쯤 한 단계는 되돌림).
1. `IInteractable { string Prompt; bool CanUse; void Use; }` + 물건 하나 = 파일 하나: Note(쪽지)·Board(칠판)·Cart(타기/싣기)·Door(문)·Bed·Diary. 감독의 "시선 맞으면 E 칩" 공통 코드는 `Interactables/Gaze.cs` 하나로. 물건은 신호를 쏜다(NotePicked, BoxLoaded, HandedOver, SleptIn…). 감독의 해당 분기는 삭제.
2. Stage_Depot 와 Stage_DayEnd 를 **독립 클래스**(MonoBehaviour 아님, DayDirector 가 생성·Enter/Tick/Exit 호출)로. 신호를 **듣고** 다음 단계로 넘긴다. 감독 partial 조각에서 그 코드 삭제.
3. Stage_Drive·Stage_Door·Stage_Return 도 같은 방식.
4. Stage_Morning·Stage_Garage(열쇠는 아직 없음 — 04 stash 에 있음, 넣지 말 것)·Stage_Packing(Packing 내부는 그대로, 연결만).
5. Presentation: DotScreenUI 가 읽는 감독 필드를 `View` 모델(ChipText·Arrow·Money·Dusk…)로 바꾸고, View 는 신호로 갱신. 감독 직접 참조 0.
6. 1~5 가 끝나면 Stages·Interactables·Presentation asmdef 를 실제로 만든다(거꾸로 참조가 남아 있으면 컴파일이 알려 줌 — 그걸 고친다). 감독을 참조하던 37개 파일 목록과 처리(신호로 바꿈/Debug 층 이동/남김+이유)를 표로.
확인: 컴파일 0 · asmdef 역참조 0 · Core 테스트 17 통과 · world_check 통과 · DayLoopCheck before(05 after.json)=after 단계 순서 같음 ±1초 · Missing 추가 0 · 400줄 이하 · 감독 직접 참조 수 전(37)→후. 캡처 1장. 보고 4줄 + docs/world/CODEMAP.md 갱신(층·파일·맡은 것) + 안 한 것과 이유. 씬 저장.
