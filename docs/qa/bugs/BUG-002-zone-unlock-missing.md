# BUG-002 — ComplexTrack의 ZoneUnlockSystem 누락으로 Box 주문 3종만 노출

| 항목 | 값 |
|---|---|
| 심각도 | **S2 핵심경험 훼손** |
| 상태 | FIXED-VERIFIED (2026-10-19, 아래 재검증 빌드) |
| 관련 AC / 스펙 | QA R-06 (TESTPLAN-restart-baseline.md), project_rules/scope.md 채널3 맵 접근도·채널4 배송물자 종류, docs/superpowers/specs/2026-05-20-cargo-raw-goods-design.md |
| 환경 | Windows, Unity 6000.3.19f1, 1920×1080, F1/Assets/ComplexTrack.unity 저장본, 8446797063d3726b2fcb3c1ef7c8d66292107462 작업 트리 |
| 재현율 | 이전 QA의 신규 Play 측정 3/3에서 각각 동일 Box 주문을 선택·완료. 추가 보드 재진입에서도 동일 3종 확인. 이번 등록 작업은 정적 검산만 수행 |

## 재현 절차
1. 에디터 토큰을 확보하고 `F1/Assets/ComplexTrack.unity` 저장본을 단독으로 연다. OpenWorld_v2를 additive로 열거나 시스템을 임시 생성하지 않는다.
2. Play에 진입해 주문 보드를 확인한다.
3. 카드 A 안전 배달(ContainerYard), B 균형 배달(FactoryA), C 도전 배달(Depot)의 화물과 구역 배지를 확인한다.
4. 주문 하나를 선택해 경로 확정→포장→배치→운전→배달→정산을 진행한다. 다시 보드로 돌아가거나 Play를 재진입해 비교한다.

## 기대 결과
ComplexTrack에서도 ZoneUnlockSystem과 해당 구역의 주문 풀을 사용한다. 해금 상태에 맞는 주문·구역 배지가 표시되고, 구성된 원물 화물(Eggs/Glass/Fish/Cake)에 정상 플레이로 접근할 수 있어야 한다. 모든 원물이 첫 화면부터 동시에 보여야 한다는 요구는 아니다.

## 실제 결과
항상 기존 카탈로그 A/B/C 3장만 나타나며 전부 Box, 구역 배지 없음. 원물 4종은 하네스가 임의로 생성하면 검사할 수 있지만 실제 보드에서는 선택할 수 없다. 채널3 해금과 채널4 화물 다양성 경로가 동작하지 않는다.

## 원인과 정적 근거
- `ZoneUnlockSystem.cs.meta` GUID=`89cdf38262fb6fa4fa67e591a55b13f0`.
- 저장된 ComplexTrack의 위 스크립트 참조 **0건**, OpenWorld_v2는 **1건**. 이름 검색이 아니라 GUID 참조를 검산했다.
- `OrderBoard.cs:144–161`: ZoneUnlockSystem.Instance가 있으면 GetAvailableJobs(), 없으면 catalog.jobs로 폴백. source.Count≤offerCount이면 전부 고정 노출.
- ComplexTrack의 OrderBoard catalog GUID `1929d15dbc4bce24cb2c673984cdd403`은 `Data/Orders/OrderCatalog.asset`, offerCount=3.
- 카탈로그는 Order_A_Safe/B_Balanced/C_Risky 세 개만 참조. 세 자산 모두 cargoType=0(Box), 목적지는 각각 ContainerYard/FactoryA/Depot.
- `OrderCardUI.Bind`는 ZoneUnlockSystem이 없으면 구역 이름 없이 job.displayName만 출력한다.

## 증거
- 정적 검산: `docs/qa/evidence/bug-002-static-2026-10-19.txt`.
- 실제 3런의 입력 대상과 포장·정산 로그: `docs/qa/evidence/run-depot-2026-10-19.txt`, `run-factory-2026-10-19.txt`, `run-container-2026-10-19.txt` 및 각각 `*-runtime-*` 로그.
- 결과 CSV: `docs/qa/evidence/runs-measured-2026-10-19.csv`.
- 실제 Box 포장: `docs/screenshots/auto/auto_20261019_010559.png`; 운전 Box: `auto_20261019_005409.png`. 보드의 세 카드와 배지 부재는 이전 QA의 Windows 화면 관찰이며 별도 보드 PNG는 저장하지 않았다.
- 기존 12 PASS 하네스는 ZoneUnlockSystem 존재 및 보드의 원물 접근성을 검사하지 않아 이 누락을 탐지하지 못했다.

## 판정 정정 / 재검증 조건
이전 QA 보고의 “초기 해금 3종”은 잘못된 원인 해석이다. 관측된 3종과 CSV 수치는 유효하나 **정상 해금 기준선이 아닌 Box 폴백 상태의 측정값**으로 한정한다. QA 게이트는 열린 S2 때문에 FAIL로 정정한다.

수정 뒤 별도 에디터 토큰으로 아래 재검증을 완료했다. 위 FAIL과 누락 근거는 수정 전 이력이다.

## VERIFIED — 2026-10-19
- 빌드: HEAD `8446797063d3726b2fcb3c1ef7c8d66292107462` + 프로그래밍팀의 ComplexTrack/하네스 미커밋 수정. [SHA256](../evidence/bug-002-build-hashes-2026-10-19.txt), Unity 6000.3.19f1.
- 저장씬 Play 재진입, 회귀 **12 PASS / 기존 BUG-001 FAIL 1 / SKIP 0**. 시스템10개 단언에 ZoneUnlock/registry 포함. [로그](../evidence/bug-002-regression-2026-10-19.txt).
- 보드8회/24카드에서 Box·Eggs·Glass·Fish·Cake 노출, 모든 실제 TMP 배지 문구는 **다운타운**. [텍스트](../evidence/bug-002-board-2026-10-19.txt), [실제 화면](../../screenshots/auto/auto_20261019_112248.png).
- 지갑을 AddReward로 0→999→1000→2499→2500 조작: Harbor는1000, Industrial은2500에서 자동 해금. 잡 풀11→14→16(중복 포함). [경계 로그](../evidence/bug-002-boundary-2026-10-19.txt). 해금 단계에서 서로 다른 구역 배지가 표시됐다는 뜻은 아니다.
- 새 Play 지갑0, 유리 주문을 UI에서 선택하고 실제 InputAction 합성 드래그로 포장(비용16/커버1%)·배치 후 합성키로 물리 주행. Plaza 배송/정산1회 성공: **10.2초/42m/무결성100%**, 강제정산·위치변경 없음. [CSV](../evidence/bug-002-run-2026-10-19.csv), [런타임](../evidence/bug-002-runtime-2026-10-19.txt), [정산 화면](../../screenshots/auto/auto_20261019_112412.png).
- 정산 후 Restart 버튼으로 보드 재진입, 원물3카드와 다운타운 배지 재확인. [텍스트](../evidence/bug-002-result-return-2026-10-19.txt).
- 본 건의 시스템 누락/원물 미노출은 해결. 해금 후 중복 주문과 배지 구분 문제는 기획 게이트의 별도 후속사항이며 본 결함 해결로 전체 게이트 PASS를 선언하지 않는다. 성능과 추가 생선 테스트는 GATE §QA 참조.

## 처리 기록
| 날짜 | 담당 | 내용 |
|---|---|---|
| 2026-10-19 | 프로그래밍팀·PD | 시스템 누락과 카탈로그 폴백 진단·검산 전달 |
| 2026-10-19 | QA팀 | GUID·카탈로그·세 주문 정적 재검산, 이전 Play 증거 연결, OPEN S2 등록; 게임 코드·씬 수정 및 신규 Play 없음 |
| 2026-10-19 | QA팀 | 저장씬 Play 재진입·원물 전체 흐름·해금 경계 재검증, FIXED-VERIFIED. QA 도구/문서만 변경 |
