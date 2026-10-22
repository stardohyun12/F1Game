# BUG-005 — 큰길·헤어핀 종단 경사 상한 초과

| 항목 | 값 |
|---|---|
| 심각도 | S3 거슬림 |
| 상태 | REOPENED |
| 관련 AC | spline-town-design.md §6 M2 / §2.3 |
| 환경 | Windows / Unity 6000.3.19f1 / HEAD 8b1cb9a5bae6d8f4580bae3390214de543b3264f + 작업트리 / TownGen_Spline 저장씬 |
| 재현율 | 동일 저장씬 메시 분석 1/1 데이터셋(반복 계산 일치); 별도 빌드 재현율 미측정 |

## 재현 절차
1. 에디터 토큰을 확보하고 `Assets/Scenes/TownGen/TownGen_Spline.unity`를 연다. Play·씬 저장 불필요.
2. 아래 실제 결과의 월드 XYZ로 Scene View를 이동한다.
3. 저장소 루트에서 `bash docs/qa/tools/exec-file.sh docs/qa/tools/spline-export.cs.txt`, `spline-core-check.cs.txt`, `spline-house-base.cs.txt` 순서로 추출한다.
4. `python -X utf8 docs/qa/tools/spline-measure.py` 실행 후 `spline-defects.json`의 `M2_ / hairpin_grade` 항목과 좌표를 대조한다.

## 기대 결과
큰길≤8%, 골목≤12%, 헤어핀 패드≤3%.

## 실제 결과
큰길 최대8.021516%: (377.60564,33.58777,308.24257), 1499구간 중5개가8%+0.001%p 초과. 약40m 호 길이 창의 누적 방향변화≥120도로 재분류한 헤어핀191표본 중36개가3% 초과; 최대8.000256%: (366.05164,38.65853,367.09781). 골목 최대12.000084%는 float 허용오차 내.

## 증거
- [측정 원본](../evidence/spline-measurements.json), [좌표별 위반 목록](../evidence/spline-defects.json).
- 증거 파일: `docs/qa/evidence/spline-measurements.json`.
- [씬 파일 SHA256](../evidence/spline-before-hashes.txt), [QA 게이트·방법과 한계](../GATE-2026-10-20-spline-town.md).
- 생성 보고서의 PASS/FAIL을 재사용하지 않고 저장씬의 월드 메시로 계산했다. 메인 씬 회귀12 PASS/기존 BUG-001 FAIL1, 이 씬의 Play는 수행하지 않았다.

## 처리 기록
| 날짜 | 담당 | 내용 |
|---|---|---|
| 2026-10-20 | QA | 저장씬 메시 독립 측정으로 OPEN 등록. 게임 코드·씬 수정 없음. |
| 2026-10-20 | 프로그래밍팀 보고 접수(QA 기록) | 일반 큰길 경사 초과0(최대7.96%), 헤어핀 초과2/193 잔존. 작성자 자체 측정이며 QA 독립 재검증 전이므로 OPEN 유지. |

후속 보고 출처: 팀 메시지 `01a152db-e830-79e0-aba4-31ed9013147f`. 작성자가 QA 도구를 출력 경로만 바꿔 실행한 `tools/town-gen/qa_mirror/run.sh` 및 `tools/town-gen/out/qa_mirror/` 결과다. 이 기록에서는 해당 측정을 독립 재실행하지 않았고, 위 최초 QA 증거·판정을 교체하지 않았다. 토큰과 재검증 배정 대기.

## QA 재검증 — §2.9 (실행 2026-10-19 KST / 문서 이터레이션 2026-10-20)

**FIXED-VERIFIED** — §2.9 OSM8%/생성10%/패드5%/골목12% 적용: 1,427구간 위반0. 독립 헤어핀106표본 최대4.97529%, 교차점308삼각형 기울기0%. 기존3% 기준이 아님.

검증 빌드: HEAD `8b1cb9a5bae6d8f4580bae3390214de543b3264f` + 작업트리, 저장씬 SHA256 `3613978D46042DC7B1D124AAAED1D354E9BFA89FD1D90A59D4915064C2F3284E`. 저장씬을 새로 추출하여 직접 검증했다(1개 저장씬 데이터셋).

재현: 기존 추출 절차 → `python docs/qa/tools/spline-measure.py --input-dir docs/qa/evidence/spline-section29 --output-dir docs/qa/evidence/spline-section29`. 교차점은 `spline-disc-grade.py` 추가 실행.

증거: [측정](../evidence/spline-section29/spline-measurements.json), [전체 결함 좌표](../evidence/spline-section29/spline-defects.json), [QA 최신 판정·한계](../GATE-2026-10-20-spline-town.md). 이전 처리 기록은 수정 전 이력으로 보존.

## 3차 QA REOPENED — 2026-10-19 KST

**REOPENED.** 길 띠 종단은 위반0이지만 새 교차점 판의258삼각형 중220개가 §2.9 패드5% 상한 초과. 실제 삼각형 평면 기울기를 측정했다. 투영면적1㎡ 이상인 면도184개 위반: 대표 `(73.46071,6.46066,74.58632)`는 **81.422%**, 면적5.13088㎡. 최대1064.816%는 면적0.005713㎡의 좁은 면 `(63.76020,5.27871,60.19249)`이므로 최대값과 넓은 면 사례를 구분한다. 작은 삼각형 수치만으로 판정한 것이 아니다.

재현: 현재 저장씬 추출 후 `python docs/qa/tools/spline-disc-grade.py docs/qa/evidence/spline-third`; 결과의 각 vertices/triangle_index로 직접 대조. 씬 Play·저장 불필요. [삼각형 좌표·면적·경사](../evidence/spline-third/junction-grade.json), [교차점 화면](../evidence/spline-third/spline-junction-v3.png). 빌드 HEAD `8b1cb9a` + 작업트리 / 씬 SHA256 `AC520B81E0421208608BFD6C0F38DCDF43E4E136B832B4478CDE73E2A8499A77`. 재현1/1 저장씬. 물리 통행 영향은 미검증, S3 유지.
