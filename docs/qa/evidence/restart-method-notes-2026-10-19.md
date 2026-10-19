# 실행 방법과 제한

- 원본 회귀를 먼저 수정 없이 실행: 12 PASS / 기존 CargoPolyBudget 1 FAIL / 0 SKIP.
- 실제 Windows UI 클릭으로 주문 카드→출발→추천 순서→확정→포장→배치→출발 확인. 실제 순서는 주문→경로 확정→포장→배치→운전이다.
- OS sky.drag 2회와 배치 1회는 0%/위치 불변. 키 입력은 임시 InputAction 모니터에서 down/up 같은 프레임(16467, 16822)을 관측했다. 이것만으로 게임 버그를 선언하지 않았다.
- Application.isFocused=False, editorInputBehaviorInPlayMode=PointersAndKeyboardsRespectGameViewFocus 확인. 다른 창을 사용하는 사용자와 포커스를 경쟁하지 않고 Play 동안 AllDeviceInputAlwaysGoesToGameView를 메모리에서만 사용했다. 종료 전 원래 값으로 복구한다.
- 프레임별 합성 MouseState에서 포장 ₩33/2% 및 리본 화면 확인: auto_20261019_010216.png. 배치 전 세계 위치 (0,-499.67,0.5), 이동 후 local=(-0.03,0.33,-0.23), 세계 위치 (0.23,-499.67,-0.03): auto_20261019_010256.png. 카메라 전용 작업 공간의 음수 Y이며 실제 지면 관통을 의미하지 않는다.
- 진단용 Depot 41.5초/42m는 포커스 대기 시간이 섞여 제외: runs-diagnostic-excluded-2026-10-19.csv. 원본(7~10월 및 하네스 혼합)도 별도 보관했다. '5월 원본'이라고 부르지 않는다.
- 유효 측정은 매번 Play 재진입하여 차량 시작위치 (72,1,60), 지갑0, 초기 주문 3종으로 초기화했다. 해금 조작 없음.
- UI 자동화는 실제 OrderCardUI.OnPointerClick와 연결된 Button.onClick을 호출한다. BeginRun/EndRun/DeliveryPoint 완료를 직접 호출하지 않는다. 측정 드라이버는 KeyboardState만 큐에 넣으며 transform·속도·시간 배율을 변경하지 않는다.
- W/S 디지털 입력 정책: 속도<목표이면 W(100%), 속도>목표+1m/s이면 S, 그 사이는 해제. 직선 목표5m/s, 방향 오차>25도이면3m/s. 방향 오차±3도 밖이면 A/D. 목표는 하드 속도 제한이 아니며 순간 초과가 있다. W 누적 초는 미계측.
- waypoint는 roadMask 원문을 읽고 선정. Depot (84,60)→(84,36)→(72,36); FactoryA (84,60)→(84,0)→(54,0); ContainerYard (84,60)→(84,0)→(96,0). 수평좌표 X/Z. 최단 경로 또는 사람 평균 경로로 검증한 것은 아니다.
- 메뉴 추정은 호스트 초 단위 시각으로 주문보드 준비→배치 완료를 잰 값이며 연결 도구 왕복이 포함된다. 경로 확정까지만의 별도 스톱워치는 없으므로 인간 메뉴 조작시간 비교에 사용하지 않는다.
- QA 진단 실수 1: null TMP_Text.text를 Contains로 읽은 exec 1회 NullReferenceException. 게임 Update 오류와 구분한다. 이후 재진입 Play 콘솔로 재확인.
- QA 도구 실수 2: FactoryA 셸 실행 중 스크립트에 ContainerYard 분기를 추가해 셸 끝에서 syntax error가 발생했다. 드라이버는 이미 실행 중이었고 실제 배달 정산 CSV/런타임 로그를 별도로 확인했다. 변경 완료본 bash -n 통과 후 다음 런 실행. 이 셸 종료 코드를 게임 FAIL로 세지 않는다.
- 작업 중 게임 코드·씬·에셋 저장/수정 없음. QA 검증 파일만 추가. 기존 작업 트리 변경은 다른 팀 소유로 유지.
