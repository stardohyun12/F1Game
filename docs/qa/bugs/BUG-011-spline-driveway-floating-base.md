# BUG-011 — 진입로 측면 하단이 지지면 위로 뜸

| 항목 | 값 |
|---|---|
| 심각도 | S3 거슬림 |
| 상태 | OPEN |
| 관련 기준 | 3차 QA 지시: 진입로가 뜨지 않는지 확인 |
| 환경 | Windows / Unity 6000.3.19f1 / HEAD 8b1cb9a + 작업트리 / spline 3차 저장씬 |
| 재현율 | 1/1 저장씬; 진입로293개 중12개에서 하단 정점 공백 확인 |

## 재현 절차

1. `Assets/Scenes/TownGen/TownGen_Spline.unity`를 열고 집176 진입로 옆면 `(365.69867,19.16291,217.19107)`을 확대한다.
2. 해당 XZ의 진입로 최하단 정점과 지형·어깨·기단·도로 중 가장 높은 지지면을 비교한다.
3. 새 추출본에서 `python docs/qa/tools/spline-third-extra.py docs/qa/evidence/spline-third` 실행 후 `floating_column`을 확인한다.

## 기대 / 실제

진입로 옆면 하단이 지형 또는 지지면까지 내려와야 한다. 실제 하단 Y=19.162909, 최고 지지면 Y=18.885681로 **0.27723m** 공백이 있다. 같은 XZ 실제 정점 Y는 `[19.35291,19.35291,19.1629086]`이며 더 낮은 바닥 정점이 없다. 집158 `(301.86124,15.33839,268.60477)`에서도 0.21793m 공백 확인.

정점별 지지면 조회에서 5cm 초과를 기록했다. 메시 내부의 구조 안정성이나 통행 충돌을 주장하는 검사가 아니다.

## 증거 / 처리

- [측정](../evidence/spline-third/driveway-check.json), [해당 진입로 주변](../evidence/spline-third/spline-drive-floating-v3.png). 공백 수치는 메시 대조이며 주변 화면에서는 시점상 가려질 수 있다.
- 저장씬 SHA256 `AC520B81E0421208608BFD6C0F38DCDF43E4E136B832B4478CDE73E2A8499A77`.
- 2026-10-19 KST QA OPEN. [관련 지형 돌출](BUG-009-spline-driveway-terrain-penetration.md)과 증상이 달라 분리 등록.
