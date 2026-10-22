# BUG-009 — 진입로·계단 위로 지형이 돌출

| 항목 | 값 |
|---|---|
| 심각도 | S3 거슬림 |
| 상태 | OPEN |
| 관련 기준 | 3차 QA 지시: 진입로가 뜨거나 겹치지 않을 것; 스펙 §2.5 지형 정합 |
| 환경 | Windows / Unity 6000.3.19f1 / HEAD 8b1cb9a + 작업트리 / 아래 저장씬 SHA256 |
| 재현율 | 1/1 저장씬 데이터셋, 293개 진입로 중 16개에서 확인 |

## 재현 절차

1. `Assets/Scenes/TownGen/TownGen_Spline.unity` 저장본을 연다(토큰 필요, 저장·Play 불필요).
2. `(431.94657,27.68943,213.86512)`의 계단을 씬 뷰로 본다. 집287의 계단 윗부분에 잔디가 드러난다.
3. `docs/qa/evidence/spline-third/`의 추출 C# 3개를 실행한 뒤 `python docs/qa/tools/spline-third-extra.py docs/qa/evidence/spline-third`를 실행한다.

## 기대 / 실제

진입로 상면이 지형에 가려지지 않아야 한다. 실제로 위 좌표에서 지형이 계단 상면보다 **0.40136m** 높다. 다른 예: 집267 `(463.75467,26.61316,172.10611)`에서 0.28617m. 지형 삼각형 보간과 실제 진입로 상면의 정점·중심 표본으로 확인했으며 5cm를 초과하는 16채를 기록했다. 전체 연속 면 전수 검사가 아니므로 실제 수는 더 많을 수 있다.

## 증거 / 처리

- [계단 화면](../evidence/spline-third/spline-drive-terrain-v3.png), [진입로 측정](../evidence/spline-third/driveway-check.json)의 `terrain_through_drive`.
- 저장씬 SHA256 `AC520B81E0421208608BFD6C0F38DCDF43E4E136B832B4478CDE73E2A8499A77`.
- 2026-10-19 KST QA 직접 재현·OPEN. 물리/통행 영향은 Play 미검증. 관련 [BUG-010](BUG-010-spline-driveway-endpoint-step.md), [BUG-011](BUG-011-spline-driveway-floating-base.md).
