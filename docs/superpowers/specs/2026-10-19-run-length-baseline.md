# 측정 스펙: 한 런 길이 기준선 (Codex 해안마을) — 2026-10-19

상태: **DONE** (2026-10-19 QA 측정 완료, 결과 §8) · 실행: QA팀(에디터 토큰) · 요청: PD 재기동 점검 3 에 합침
변경 이력: 2026-10-19 기획팀 작성 (QA 측정 조건 확정본) · 2026-10-19 QA 질문 반영 — 초기 해금 3곳(Depot/FactoryA/ContainerYard)으로 3런, Mall 은 해금 강제 4번째 런(선택), 합성 입력 허용·표기, 보관 CSV 이름 수정

## 1. 목적
"한 판 8~12초 / 49~110m"는 2026-05 R0 맵(78×54m) 측정값. 그 뒤 맵이 Codex 해안마을로 통째로 바뀌었고 런 길이는 재측정된 적이 없다. 지금 한 런이 실제로 몇 초인지 기준선을 만든다. 목표 5~15분과의 갭 크기가 해안도로 P1 스펙(D-098) 수치의 근거가 된다.

## 2. 대상
- 씬: `F1/Assets/ComplexTrack.unity` (저장된 Codex 해안마을 설치판). 회귀 점검 끝난 뒤 같은 토큰으로.
- 기록기: `RunMetricsRecorder` 는 `GameContext.Awake` 가 `EnsureExists()` 로 자동 생성(`F1/Assets/Scripts/Core/GameContext.cs:47`). 설치 불필요. `RunController.EndRun` 의 `OnRunEnded` 에서 CSV 1줄 append.
- CSV: `Application.persistentDataPath/runs.csv` (Windows `%USERPROFILE%\AppData\LocalLow\<Company>\<Product>\runs.csv`). 메뉴 `PitCrew/Metrics/Show CSV path` · `Open runs.csv` · `Reset runs.csv`.
- 헤더: `timestamp,run#,success,orders,completed,elapsedSec,distance_m,avgSpeed_kmh,reward,integrity%,walletBefore,walletAfter,fuelPct`

## 3. 절차
1. 기존 `runs.csv` 를 `docs/qa/runs-before-measurement-2026-10-19.csv` 로 복사 보관(QA 확인: 5월 원본이 아니라 7~10월+하네스 기록 혼합) → `Reset runs.csv`.
2. 거리표 (QA 실측 2026-10-19, 출발점→배달지 수평 m): CityHall 18.97 · Plaza 24 · Depot 24 · PierEnd 32.31 · CargoDock 56.60 · Warehouse 60 · FactoryA 62.64 · Station 64.62 · ContainerYard 64.62 · FishMarket 73.24 · Mall 96.19. **전부 100m 안.**
   - **측정 전제(QA 발견, PD 검산으로 원인 정정 2026-10-19)**: 초기 보드에 뜨는 주문은 **ContainerYard / FactoryA / Depot 3종 고정**. 원인은 ZoneUnlockSystem 경로가 아니라 **ComplexTrack 씬에 ZoneUnlockSystem 이 없어 `OrderCatalog.asset`(박스 잡 A/B/C 3개)로 폴백**한 것(`OrderBoard.cs:144-161`). 즉 이 3런은 "채널3 해금이 꺼진 상태"의 기준선이다. Zone_Downtown 에는 잡 11개가 있어 배선이 복원되면 초기 보드 구성이 달라진다(프로그래밍팀 1안 진행 중 → 끝나면 기획 게이트). 측정값(시간·거리) 자체는 배선과 무관하므로 기준선으로 유효. **3런 = Depot(24m) / FactoryA(62.64m) / ContainerYard(64.62m)**, 해금 조작 없음.
   - **4번째 런(선택, 있으면 좋음)**: Mall(96.19m, 최장). 해금을 강제해야 하므로 방법(지갑 debug 충전 또는 ZoneUnlock 강제 호출)을 보고에 **명시**하고 표에 "해금 강제" 표시. 이 런은 "지금 맵의 상한"을 보여 주는 값이지 신규 플레이어 기준선이 아니다.
   - 라벨은 비어 있으면 GameObject 이름 대체(`DeliveryPoint.Awake`).
3. 3런, 각 런 = 실제 플레이 흐름: 주문보드 → **경로 확정 → 포장 → 트렁크 배치** → 운전 → 배달 → 정산 (QA 실측 순서로 정정. 처음 쓴 "포장→배치→경로"는 틀림). `CoastalTownSmoke` 의 `BeginRun(null)`·`Place` 텔레포트 경로는 쓰지 않는다. **합성 입력(PD 허용)은 쓰되** 보고에 "합성 입력(키 down/up 동일 프레임 관측 때문)"이라고 명시하고, 운전 구간은 실제 주행 속도로 진행(순간이동·속도 배율 금지).
   - **실제 적용된 입력 정책(QA 2026-10-19, 원문 `docs/qa/tools/synthetic-drive.cs.txt`)**: W=100% 디지털. 속도 < 목표면 W, 목표+1m/s 초과면 S, 그 사이 키 뗌. 직선 목표 5m/s(18km/h), 방향 오차 > 25° 면 3m/s, 조향 오차 ±3° 밖에서 A/D. → **저속·고정 경로 정책 기준선**이다(18km/h 는 입력 목표값이지 하드 상한 아님 — FactoryA 로그에서 순간 6.58m/s 관측, QA 보정). 해석(추론, 측정 아님): 같은 경로를 능숙하게 풀스로틀로 모는 사람보다는 느릴 가능성이 높다. 단 처음 하는 사람은 길 찾기·충돌로 더 오래 걸릴 수도 있어 **인간 플레이의 상한이라고 단정하지 않는다.** 갭(목표 5~15분) 결론은 어느 쪽이든 자릿수가 달라 바뀌지 않는다.
4. 런마다 손으로 기록: 목적지 라벨 · 화물 종류 · 주문보드 진입~경로 확정까지 메뉴 구간 초(스톱워치, "추정" 표시). `elapsedSec` 는 운전 구간(BeginRun~EndRun)만이다.
5. 흐름이 막힌 런(카드가 라벨 못 찾음·차 박힘·배달 트리거 안 됨)은 success=0 으로 두고 사유 기록 → `docs/qa/bugs/`. 세션32 주문 라벨↔씬 배달지 미스매치 재발 전례.

## 4. 보고 (수용 기준)
- [ ] `runs.csv` 3줄 원문 + 5월 보관 파일 경로
- [ ] 표: 런# / 목적지 / 직선거리 m / elapsedSec / distance_m / avgSpeed_kmh / success / 메뉴 구간 추정 초
- [ ] 출발점→11 배달지 거리표
- [ ] 비교 한 줄: 5월 R0 8~12초·49~110m / 목표 5~15분
- [ ] 토큰 반납 3줄(컴파일 / Play 여부 / 미저장 씬 변경)

## 5. 범위 밖
포장 품질·보상·손상 수치 해석(이번은 시간·거리만). 측정값에 따른 스펙 수정은 기획팀이 P1 스펙에서 한다.

## 8. 결과 (QA 2026-10-19, 원문 `docs/qa/GATE-2026-10-19-restart-baseline.md` §QA, CSV `docs/qa/evidence/runs-measured-2026-10-19.csv`)
- Depot 24.00m → 10.1초 · 41m · 14.7km/h · 성공
- FactoryA 62.64m → 20.7초 · 96m · 16.7km/h · 성공
- ContainerYard 64.62m → 17.3초 · 78m · 16.3km/h · 성공
- 배달지 11곳 전부 출발점에서 100m 안(최장 Mall 96.19m). Mall 강제 해금 런은 생략.
- 메뉴 구간 합성 추정 16/16/38초는 도구 왕복 포함 → 사람 페이싱으로 쓰지 않음(미계측).
- **결론**: 운전 10~21초 vs 목표 5~15분 — 자릿수 차이. 5월 R0(8~12초)와 같은 자리이므로 Codex 해안마을 설치는 런 길이를 바꾸지 않았다. D-098·D-104 의 수치 근거.
- **P2 스펙으로 가져갈 것**: ① 마을 재생성 시 배달지 간 거리 설계값(가설: 마을 안 150~400m, 마을 간 1.5~2km) ② 시작 잡 3종을 거리 단계×화물 종류로 재작성(지금은 가까운 Depot 가 ₩1800 으로 가장 비쌈) ③ ~~초기 보드에 원물 화물이 뜨는지~~ → 원인 확정: 씬에 ZoneUnlockSystem 없음(카탈로그 폴백). 프로그래밍팀이 Zones 배선 복원 중, 복원 뒤 Zone_Downtown 11개 잡의 보상 분포로 ②를 다시 본다.
