# BUG-006 — 도로 중심선 곡률 반경 미달

| 항목 | 값 |
|---|---|
| 심각도 | S3 거슬림 |
| 상태 | OPEN |
| 관련 AC | spline-town-design.md §6 M3 |
| 환경 | Windows / Unity 6000.3.19f1 / HEAD 8b1cb9a5bae6d8f4580bae3390214de543b3264f + 작업트리 / TownGen_Spline 저장씬 |
| 재현율 | 동일 저장씬 메시 분석 1/1 데이터셋(반복 계산 일치); 별도 빌드 재현율 미측정 |

## 재현 절차
1. 에디터 토큰을 확보하고 `Assets/Scenes/TownGen/TownGen_Spline.unity`를 연다. Play·씬 저장 불필요.
2. 아래 실제 결과의 월드 XYZ로 Scene View를 이동한다.
3. 저장소 루트에서 `bash docs/qa/tools/exec-file.sh docs/qa/tools/spline-export.cs.txt`, `spline-core-check.cs.txt`, `spline-house-base.cs.txt` 순서로 추출한다.
4. `python -X utf8 docs/qa/tools/spline-measure.py` 실행 후 `spline-defects.json`의 `M3_` 항목과 좌표를 대조한다.

## 기대 결과
큰길≥12m, 헤어핀도≥8m, 골목≥6m.

## 실제 결과
메시 인접 단면 중심3점(약2m) 외접원: 큰길 최소2.50045m at (94.66584,24.98841,308.05713), 절대 하한8m 미달25점. 골목 최소4.61591m at (200.95273,3.22125,45.80612),6m 미달3점. 작성자 방식과 같은 ±2표본(약4m)으로도 큰길 최소4.68476m여서 FAIL. 골목4m창은6.50049m로 통과하므로 검사 창 차이를 명시한다.

## 증거
- [측정 원본](../evidence/spline-measurements.json), [좌표별 위반 목록](../evidence/spline-defects.json).
- 증거 파일: `docs/qa/evidence/spline-tight-curve.png`.
- [씬 파일 SHA256](../evidence/spline-before-hashes.txt), [QA 게이트·방법과 한계](../GATE-2026-10-20-spline-town.md).
- 생성 보고서의 PASS/FAIL을 재사용하지 않고 저장씬의 월드 메시로 계산했다. 메인 씬 회귀12 PASS/기존 BUG-001 FAIL1, 이 씬의 Play는 수행하지 않았다.

## 처리 기록
| 날짜 | 담당 | 내용 |
|---|---|---|
| 2026-10-20 | QA | 저장씬 메시 독립 측정으로 OPEN 등록. 게임 코드·씬 수정 없음. |
| 2026-10-20 | 프로그래밍팀 보고 접수(QA 기록) | 큰길 최소반경2.84m, 절대하한 미달23점 잔존. 작성자 자체 측정이며 QA 독립 재검증 전이므로 OPEN 유지. |

후속 보고 출처: 팀 메시지 `01a152db-e830-79e0-aba4-31ed9013147f`. 작성자가 QA 도구를 출력 경로만 바꿔 실행한 `tools/town-gen/qa_mirror/run.sh` 및 `tools/town-gen/out/qa_mirror/` 결과다. 이 기록에서는 해당 측정을 독립 재실행하지 않았고, 위 최초 QA 증거·판정을 교체하지 않았다. 토큰과 재검증 배정 대기.

## QA 재검증 — §2.9 (실행 2026-10-19 KST / 문서 이터레이션 2026-10-20)

**OPEN** — 약2m 인접3점 반경: 큰길 최소6.05342m (367.24408,29.72640,390.86975), 8m 미만8표본. 골목 최소6.16653m로 해소. 약4m 창 최소8.31924m과 구분. OSM 이탈6/10m 독립 미검증.

검증 빌드: HEAD `8b1cb9a5bae6d8f4580bae3390214de543b3264f` + 작업트리, 저장씬 SHA256 `3613978D46042DC7B1D124AAAED1D354E9BFA89FD1D90A59D4915064C2F3284E`. 저장씬을 새로 추출하여 직접 검증했다(1개 저장씬 데이터셋).

재현: 기존 추출 절차 → `python docs/qa/tools/spline-measure.py --input-dir docs/qa/evidence/spline-section29 --output-dir docs/qa/evidence/spline-section29`. 교차점은 `spline-disc-grade.py` 추가 실행.

증거: [측정](../evidence/spline-section29/spline-measurements.json), [전체 결함 좌표](../evidence/spline-section29/spline-defects.json), [QA 최신 판정·한계](../GATE-2026-10-20-spline-town.md). 이전 처리 기록은 수정 전 이력으로 보존.

## 3차 QA — OPEN 유지 (2026-10-19 KST)

기존 큰길8m 미만은0, 최소8.18183m; 골목6m 미만0, 최소6.24363m. 그러나 **일반 굽이12m 미만3점**이 남아 OPEN이다.

| 반경(m) | 좌표(x,y,z) | 판정 |
|---|---|---|
| 11.96144 | (185.76093,12.18932,367.21985) | 일반 굽이 |
| 11.89384 | (178.24092,12.40265,364.62735) | 일반 굽이 |
| 11.95676 | (132.83395,33.42156,135.97537) | 생성 길 S자, 일반 굽이 |

기획 회신 `01a15370-bccd-7022-8e88-e07179f17564`·스펙 §2.3 최신 정의 적용: **반경<30m인 연속 굽이의 시작·끝 방향 차≥120°**일 때만 그 구간에8m 허용. 실제메시 중심선으로 측정한 첫 두 점 구간은59.638°, 세 번째 점 구간은72.358°여서 예외에 해당하지 않는다. 진짜 헤어핀2구간(175.771°/164.086°)의15점은 예외 통과. 구현 hairpin=true 라벨만으로 통과시키지 않았다. 고정40m 창의 절대 회전 합은 참고치로만 남겼다.

재현: `python docs/qa/tools/spline-radius-audit.py docs/qa/evidence/spline-third`. [좌표·굽이 구간](../evidence/spline-third/radius-exceptions.json). 씬 SHA256 `AC520B81E0421208608BFD6C0F38DCDF43E4E136B832B4478CDE73E2A8499A77`, HEAD `8b1cb9a` + 작업트리. OSM 원본 선 대응 관계는 여전히 독립 확정 못함; 같은 highway 최근접 선 검사는 위반 하한 검사에 그친다.
