# BUG-008 — 최종 지형 깎기·메우기 깊이 상한 초과

| 항목 | 값 |
|---|---|
| 심각도 | S3 거슬림 |
| 상태 | VERIFIED (기준 개정 경고 통과) |
| 관련 AC | spline-town-design.md §6 M7 / §2.5 |
| 환경 | Windows / Unity 6000.3.19f1 / HEAD 8b1cb9a5bae6d8f4580bae3390214de543b3264f + 작업트리 / TownGen_Spline 저장씬 |
| 재현율 | 동일 저장씬 메시 분석 1/1 데이터셋(반복 계산 일치); 별도 빌드 재현율 미측정 |

## 재현 절차
1. 에디터 토큰을 확보하고 `Assets/Scenes/TownGen/TownGen_Spline.unity`를 연다. Play·씬 저장 불필요.
2. 아래 실제 결과의 월드 XYZ로 Scene View를 이동한다.
3. 저장소 루트에서 `bash docs/qa/tools/exec-file.sh docs/qa/tools/spline-export.cs.txt`, `spline-core-check.cs.txt`, `spline-house-base.cs.txt` 순서로 추출한다.
4. `python -X utf8 docs/qa/tools/spline-measure.py` 실행 후 `spline-defects.json`의 `M7_cutfill` 항목과 좌표를 대조한다.

## 기대 결과
원본 DEM×0.4 대비 최대깊이≤3m, 옹벽 길이 보고.

## 실제 결과
최종 지형48441정점 중2700점이3m 초과. 최대깎기13.36974m: (156,15.14026,102), 원본높이28.51m. 옹벽 메시 실측 길이3852.57m. 작성자 보고10.38m와 달리 최종 저장씬 지형을 원본 DEM에 대조한 값이다.

## 증거
- [측정 원본](../evidence/spline-measurements.json), [좌표별 위반 목록](../evidence/spline-defects.json).
- 증거 파일: `docs/qa/evidence/spline-measurements.json`.
- [씬 파일 SHA256](../evidence/spline-before-hashes.txt), [QA 게이트·방법과 한계](../GATE-2026-10-20-spline-town.md).
- 생성 보고서의 PASS/FAIL을 재사용하지 않고 저장씬의 월드 메시로 계산했다. 메인 씬 회귀12 PASS/기존 BUG-001 FAIL1, 이 씬의 Play는 수행하지 않았다.

## 처리 기록
| 날짜 | 담당 | 내용 |
|---|---|---|
| 2026-10-20 | QA | 저장씬 메시 독립 측정으로 OPEN 등록. 게임 코드·씬 수정 없음. |
| 2026-10-20 | 프로그래밍팀 보고 접수(QA 기록) | 3m 초과2557점, 최대11.18m로 잔존; 옹벽3177m. 작성자 자체 측정이며 QA 독립 재검증 전이므로 OPEN 유지. |

후속 보고 출처: 팀 메시지 `01a152db-e830-79e0-aba4-31ed9013147f`. 작성자가 QA 도구를 출력 경로만 바꿔 실행한 `tools/town-gen/qa_mirror/run.sh` 및 `tools/town-gen/out/qa_mirror/` 결과다. 이 기록에서는 해당 측정을 독립 재실행하지 않았고, 위 최초 QA 증거·판정을 교체하지 않았다. 토큰과 재검증 배정 대기.

## QA 재검증 — §2.9 (실행 2026-10-19 KST / 문서 이터레이션 2026-10-20)

**VERIFIED (기준 개정 경고 통과)** — 기획 회신 01a152f6-fce3-7561-bac6-cc26380f4581 및 최신 §2.9 적용: DEM×0.3 대비 변경≥0.1m 지형11,940점 중6m 초과27점=0.22613%≤1%, 최대 메우기8.85792m (180,28.13892,104)≤10m. 형상 초과 해소가 아닌 승인된 경고 조건 충족. 옹벽1413.528m. m7-over6m.csv에27점 보존.

검증 빌드: HEAD `8b1cb9a5bae6d8f4580bae3390214de543b3264f` + 작업트리, 저장씬 SHA256 `3613978D46042DC7B1D124AAAED1D354E9BFA89FD1D90A59D4915064C2F3284E`. 저장씬을 새로 추출하여 직접 검증했다(1개 저장씬 데이터셋).

재현: 기존 추출 절차 → `python docs/qa/tools/spline-measure.py --input-dir docs/qa/evidence/spline-section29 --output-dir docs/qa/evidence/spline-section29`. 교차점은 `spline-disc-grade.py` 추가 실행.

증거: [측정](../evidence/spline-section29/spline-measurements.json), [전체 결함 좌표](../evidence/spline-section29/spline-defects.json), [QA 최신 판정·한계](../GATE-2026-10-20-spline-town.md). 이전 처리 기록은 수정 전 이력으로 보존.
