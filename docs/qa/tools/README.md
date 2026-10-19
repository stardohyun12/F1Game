# 재기동 QA 재사용 도구

## BUG-002 재검증 추가 도구
후속 실행 확인(2026-10-19): 위 정적 수정의 런타임 미검증 상태를 해소했다. 동일 Play에서 유리 Plaza(8.5초/30m) 정산→Restart→생선 PierEnd(3.1초/10m) 모두 성공. 두 번째 QA_ROUTE는 현재(66.99,0,83.79), source=(11,14)에서 계산했다. 증거 `../evidence/dedup-continuous-runtime-2026-10-19.json` 및 `dedup-continuous-runs-2026-10-19.csv`. 이는 두 경로의 실제 실행 확인이며 전체 목적지/차량 방향/장애물 탈출 보증은 아니다. 기존 하드코딩 시기의 CSV와 분리한다.

중복 카드 재검증: Play 후 `bash docs/qa/tools/exec-file.sh docs/qa/tools/dedup-board.cs.txt`. 지갑을 최소2500으로 조작한 뒤 Open을20회 호출하여 실제 카드의 OrderJob 참조·화물·제목을 집계한다. 20회는 한 exec 안에서 동기적으로 실행하며 마지막 보드만 화면 캡처한다.

2026-10-19 후속 정적 수정: 세 drive 스크립트는 현재 차량 위치의 최근접 도로 셀에서 목적지까지 roadMask 4방향 BFS를 계산하고 직선 구간을 압축한다. 공통 원문은 `current-position-route.cs.txt`이며 세 exec 파일에 같은 블록을 포함한다. 게임 경로 UI는 현재 차 기준 목적지 순서/직선을 계산하므로 주행 경유점 생성기로 직접 재사용하지 않았다. 대신 기존 TrackLayout과 TileMapAuto.CellToWorld 매핑을 재사용한다. 현재 ComplexTrack 전용이며 도로에서6m 이상 이탈/경로 없음은 실패 반환한다. 장애물 회피·후진 탈출 기능은 없다. **최초 수정 시점에는 정적 확인만 했다. 이후 두 경로 실행 확인은 위 후속 기록 참조. 최초 성공 CSV는 수정 전 도구의 결과다.**

`exec-file.sh`로 `bug-002-*.cs.txt`를 Play에서 실행한다. board는 8회 재오픈하여 실제 카드 텍스트를 기록한다. boundary는 지갑 0에서 999/1000/2499/2500으로 AddReward 이벤트를 발생시키는 명시적 테스트 조작이다. 경계 검사 뒤 Stop/Play로 초기화한다.

select는 유리(Plaza), select-fish는 생선(PierEnd)이 나올 때까지 최대30회 보드를 열고 실제 카드 클릭 이벤트를 호출한다. BoardGoButton → Suggest → Confirm 버튼 이벤트 후 drag, BtnGo 후 drag, 마지막 drive 순서다. drive는 BtnGo로 출발하고 프레임별 합성 키를 넣으며 실제 물리/배달 트리거를 사용한다. 스크린샷을 주행 중 찍으면 프레임 표본이 오염되므로 FPS 비교에서는 제외한다. 평균 FPS=1000/meanMs, 최저 순간 FPS=1000/maxMs; 릴리스 성능 보증이 아니다.

에디터 토큰 필수. 저장소 루트에서 실행. 게임 코드/씬 저장 없음.

1. 기존 persistentDataPath/runs.csv를 별도 보관한다. 필요하면 `PitCrew/Metrics/Reset runs.csv` 메뉴로 초기화한다.
2. Play가 꺼져 있을 때 `bash docs/qa/tools/synthetic-run.sh Depot` 실행. FactoryA/ContainerYard도 지원한다. ContainerYard는 포장·배치 드래그를 추가한다.
3. 스크립트는 드라이버 등록 후 종료한다. Result와 실제 CSV를 확인한다. 드라이버는 최대90초 후 키를 놓으며 정산을 강제하지 않는다. 멈춤/실패 시 임의로 성공 CSV를 만들지 않는다.
4. 다음 런 전 `bash scripts/ucli.sh editor stop`. 각 런은 Play 재진입으로 초기화한다.
5. **마지막에는 입력 설정 원복**: exec로 `UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.PointersAndKeyboardsRespectGameViewFocus; return 1;` 실행 후 Stop. 설정/씬 저장 금지. 콘솔·isPlaying·scene.isDirty 확인 후 토큰 반납.

합성 입력 정책·한계는 ../evidence/restart-method-notes-2026-10-19.md 참조. UI 이벤트를 호출하며, 실제 OS 입력 사용성 검증을 대체하지 않는다. 모든 경로는 현재 씬의 roadMask/초기 주문에 한정한다. 스크립트 실행 중 파일을 편집하지 않는다.
