# Unity 기획 창 (PitCrew → 기획 창)

웹 기획 뷰어(tools/viewer/plan)가 하던 일을 Unity 안에서 한다. 씬(Map_TownV6)이 유일한 진실이다.

1. **열기**: 메뉴 `PitCrew/기획 창`. 맨 위에 고른 물건 이름(건물 이름)과 Unity 좌표 `x y z yaw` 가 나온다.
2. **코멘트**: 물건을 고르고(없으면 화면 전체) 입력칸에 쓰고 [보내기]. 씬 뷰/게임 뷰 중 고른 쪽이 `tools/viewer/feedback/shots/<id>.png` 로 찍히고 `feedback/unity-<씬>.jsonl` 에 한 줄 남는다. 아래 목록을 누르면 그 물건을 고르고 그때 시점으로 카메라를 옮긴다. 다 고쳤으면 [처리됨].
3. **배치**: 건물을 씬에서 직접 옮긴 뒤 [배치 기록]을 누르면 `docs/design/tutorial/placements.yaml` 에 써진다(피벗 = 프리팹 루트). 바뀐 게 없으면 파일을 안 건드린다. [고른 것 접지]는 고른 물건 바닥을 아래 땅에 붙인다.
4. **팔레트**: 라이브러리 버튼(depot·garage·house_home·postoffice·room)을 누르고 씬 뷰를 클릭하면 거기에 놓이고 땅에 붙는다(Esc 취소). 놓은 뒤 [배치 기록]을 눌러야 yaml 에 남는다. 씬 저장은 평소처럼 Ctrl+S.
5. **잠금**: 창이 열려 있고 Unity 가 앞에 있으면 AI 팀원의 에디터 작업이 기다린다(`.claude/team/editor.lock` = `director …`, 30초마다 갱신). 창을 닫으면 풀린다. 맨 아래 줄에 지금 누가 에디터를 쓰는지 나온다.

참고: [튜토리얼 치우기] = D-201, `TUT_*`·OpeningTutorial 을 비활성 루트 `_TUT_HOLD` 아래로 옮긴다(이미 적용됨). 되살리려면 그 자식들을 `TV6_Loop/Tutorial` 아래로 다시 끌면 된다.
