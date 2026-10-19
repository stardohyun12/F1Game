# 재기동 기준선 테스트 플랜

대상: ComplexTrack, 커밋 8446797063d3726b2fcb3c1ef7c8d66292107462 + 작업 트리. 실제 Unity 6000.3.19f1, Windows, Game View 1920×1080.
근거: PIPELINE §3/§9, scripts/regression-check.sh, docs/superpowers/specs/2026-10-19-run-length-baseline.md §4, 기존 포장 설계. game/RUN.md·docs/reports/IMPL-*는 현재 체크아웃에 없음. project_rules/roles.md는 시작 시 부재했으나 팀이 복구한 뒤 읽음.

| ID | 전제 | 절차 | 기대 결과 | 확인 방법 | 우선순위 |
|---|---|---|---|---|---|
| R-01 | 에디터 토큰 보유, 저장씬 열림 | 원본 regression-check.sh 실행 | 12 PASS, 기존 CargoPolyBudget 1 FAIL 외 신규 실패 없음 | 자동, 원본 출력 보관 | P0 |
| R-02 | Play, 주문보드 | 실제 카드 클릭→출발→추천 경로→확정 | 포장 진입, 주문 동일 | 수동 UI+런타임 대조 | P0 |
| R-03 | 포장 | 실제 가로·세로 드래그 및 회전 입력 | 랩 생성, 비용/커버율 갱신 | 수동+입력 프레임 진단 | P0 |
| R-04 | 포장 | 배치하기→화물 드래그→출발 | 배치가 바뀌고 Driving에 동일 화물 전달 | 수동 UI+런타임 대조 | P0 |
| R-05 | Driving | 운전시점 화면 3장 캡처 | UI 포함 PNG, 실제 주행 화면 | 자동 캡처+이미지 확인 | P1 |
| R-06 | ComplexTrack 저장본 단독 로드 | ZoneUnlockSystem 존재·카탈로그 연결 확인, 보드 배지와 해금별 원물 주문 접근 확인 | 정상 구역 주문 풀 사용, 원물 화물 접근 가능 | 정적+Play, BUG-002 회귀 | P0 |
| M-01 | 기록기 설치, 기존 CSV 존재 | 기존 파일 보관 후 Reset 메뉴 실행, 실제 3런 | 신규 CSV 3줄, 기존 자료 보존 | 자동+파일 확인 | P0 |
| M-02 | 씬 출발 위치 | 11 DeliveryPoint 수평거리 산출 | min/median/max 및 대상 기록 | 자동, 읽기 전용 exec | P1 |
| M-03 | 런당 주문 1개 | 수정 스펙의 초기 해금 Depot/FactoryA/ContainerYard 각 완료까지 주행 | 목적지/직선거리/시간/이동거리/평균속도/성공/메뉴 추정 표 | 수동 또는 명시적 합성 입력, CSV | P0 |
| M-04 | 측정 후 | 5월 R0와 목표를 함께 기록 | 8~12초·49~110m / 목표 5~15분, 새 측정과 혼동 없음 | 문서 확인 | P1 |
| M-05 | 종료 | Stop, 콘솔·컴파일·dirty 조회 | 토큰 반납 3줄, 변경 저장 없음 | 자동 | P0 |

기존 사례 재현: 회귀 스크립트를 수정 없이 먼저 실행한다. 이전 false-green 사례를 따라 BeginRun/강제 EndRun·텔레포트는 측정에 사용하지 않는다. 도구 왕복·정지 시간이 섞인 합성 입력 결과는 인간 페이싱으로 해석하지 않는다.
