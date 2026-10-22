# BUG-010 — 진입로가 보도·집 기단 높이와 맞지 않음

| 항목 | 값 |
|---|---|
| 심각도 | S3 거슬림 |
| 상태 | OPEN |
| 관련 기준 | 3차 QA 지시: 길·집 접점 확인 / 스펙 §2.6 건물 접지 |
| 환경 | Windows / Unity 6000.3.19f1 / HEAD 8b1cb9a + 작업트리 / spline 3차 저장씬 |
| 재현율 | 1/1 저장씬; 숫자로 확인된 집 끝 높이차15채·길 끝97채(중복 가능) |

## 재현 절차

1. `Assets/Scenes/TownGen/TownGen_Spline.unity`를 열고 집262의 진입로 끝 `(175.03739,24.81111,166.02645)`를 본다.
2. 진입로 상면 Y=24.811108과 바로 안쪽 집 기단 Y=24.413076을 비교한다.
3. 새 추출본에 `python docs/qa/tools/spline-third-extra.py docs/qa/evidence/spline-third`를 실행하고 `door_gap`·`entry_gap` 항목을 확인한다.

## 기대 / 실제

현관 쪽 마지막 상면이 집 기단으로 이어져야 한다. 실제로 진입로 끝이 기단보다 **0.39803m** 높다. 길 쪽 사례 집261 `(157.37209,17.52974,287.77564)`도 보도보다 **0.32933m** 높다.

검사는 끝에서5mm 안쪽 진입로와 접점 반대편20mm의 실제 표면을 폭 방향3점에서 대조했다. 탐색 기준은 집 끝10cm, 길 끝 경사판10cm/계단20cm이며 별도 기획 수용치가 아니라 QA 결함 탐지 기준이다. 참조 표면을 찾지 못한 길115/879표본·집15/879표본은 실패 수에 넣지 않고 미검증으로 남겼다. 단순히 표면 조회가 없다는 이유로 결함을 만들지 않았다.

## 증거 / 처리

- [측정](../evidence/spline-third/driveway-check.json), [집 끝 주변 화면](../evidence/spline-third/spline-drive-door-v3.png).
- 저장씬 SHA256 `AC520B81E0421208608BFD6C0F38DCDF43E4E136B832B4478CDE73E2A8499A77`.
- 2026-10-19 KST QA OPEN. 화면은 주변 맥락용이며 수치는 실제 메시 비교 증거다. Play 이동·충돌은 미검증. [관련 지형 돌출](BUG-009-spline-driveway-terrain-penetration.md).
