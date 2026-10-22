# BUG-007 — 건물 하부 외곽이 길·보도와 겹침

| 항목 | 값 |
|---|---|
| 심각도 | S3 거슬림 |
| 상태 | FIXED-VERIFIED |
| 관련 AC | spline-town-design.md §6 M4 |
| 환경 | Windows / Unity 6000.3.19f1 / HEAD 8b1cb9a5bae6d8f4580bae3390214de543b3264f + 작업트리 / TownGen_Spline 저장씬 |
| 재현율 | 동일 저장씬 메시 분석 1/1 데이터셋(반복 계산 일치); 별도 빌드 재현율 미측정 |

## 재현 절차
1. 에디터 토큰을 확보하고 `Assets/Scenes/TownGen/TownGen_Spline.unity`를 연다. Play·씬 저장 불필요.
2. 아래 실제 결과의 월드 XYZ로 Scene View를 이동한다.
3. 저장소 루트에서 `bash docs/qa/tools/exec-file.sh docs/qa/tools/spline-export.cs.txt`, `spline-core-check.cs.txt`, `spline-house-base.cs.txt` 순서로 추출한다.
4. `python -X utf8 docs/qa/tools/spline-measure.py` 실행 후 `spline-defects.json`의 `M4_base_road_overlap` 항목과 좌표를 대조한다.

## 기대 결과
건물 바닥 접지 |Δ|≤0.10m, 길·보도와 건물 겹침0.

## 실제 결과
300채 접지는 최대0.0000027m로 정상. 전체 형상 외곽24채는 지붕을 포함하므로 후보로만 취급. 바닥에서25cm 이내 정점의 회전된 외곽으로 좁혀도4채가 길과 교차: #95 보도(256.55325,14.30676,247.60906), #141 골목(232.57614,17.84938,319.77493), #156 차도(208.29703,19.18692,367.51640), #219 보도(106.81894,26.74003,330.20050). 이는 하부 외곽의 평면 교차이며 MeshCollider 관통/플레이 끼임 판정은 아니다.

## 증거
- [측정 원본](../evidence/spline-measurements.json), [좌표별 위반 목록](../evidence/spline-defects.json).
- 증거 파일: `docs/qa/evidence/spline-house-base-overlap.png`.
- [씬 파일 SHA256](../evidence/spline-before-hashes.txt), [QA 게이트·방법과 한계](../GATE-2026-10-20-spline-town.md).
- 생성 보고서의 PASS/FAIL을 재사용하지 않고 저장씬의 월드 메시로 계산했다. 메인 씬 회귀12 PASS/기존 BUG-001 FAIL1, 이 씬의 Play는 수행하지 않았다.

## 처리 기록
| 날짜 | 담당 | 내용 |
|---|---|---|
| 2026-10-20 | QA | 저장씬 메시 독립 측정으로 OPEN 등록. 게임 코드·씬 수정 없음. |
| 2026-10-20 | 프로그래밍팀 보고 접수(QA 기록) | 건물 하부25cm/전체외곽 겹침 모두0. 작성자 자체 측정이며 QA 독립 재검증 전이므로 OPEN 유지. |

후속 보고 출처: 팀 메시지 `01a152db-e830-79e0-aba4-31ed9013147f`. 작성자가 QA 도구를 출력 경로만 바꿔 실행한 `tools/town-gen/qa_mirror/run.sh` 및 `tools/town-gen/out/qa_mirror/` 결과다. 이 기록에서는 해당 측정을 독립 재실행하지 않았고, 위 최초 QA 증거·판정을 교체하지 않았다. 토큰과 재검증 배정 대기.

## QA 재검증 — §2.9 (실행 2026-10-19 KST / 문서 이터레이션 2026-10-20)

**FIXED-VERIFIED** — 300채 전체 외곽/하부25cm 도로·보도 겹침0, 집끼리겹침0, 바닥-기단 최대0.0000026m. 실제 메시 투영 검사이며 Play 물리 검증은 아님.

검증 빌드: HEAD `8b1cb9a5bae6d8f4580bae3390214de543b3264f` + 작업트리, 저장씬 SHA256 `3613978D46042DC7B1D124AAAED1D354E9BFA89FD1D90A59D4915064C2F3284E`. 저장씬을 새로 추출하여 직접 검증했다(1개 저장씬 데이터셋).

재현: 기존 추출 절차 → `python docs/qa/tools/spline-measure.py --input-dir docs/qa/evidence/spline-section29 --output-dir docs/qa/evidence/spline-section29`. 교차점은 `spline-disc-grade.py` 추가 실행.

증거: [측정](../evidence/spline-section29/spline-measurements.json), [전체 결함 좌표](../evidence/spline-section29/spline-defects.json), [QA 최신 판정·한계](../GATE-2026-10-20-spline-town.md). 이전 처리 기록은 수정 전 이력으로 보존.
