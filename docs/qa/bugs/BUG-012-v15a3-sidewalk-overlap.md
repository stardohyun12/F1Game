# BUG-012 — v15a3 보도와 건물 3채 겹침

| 항목 | 값 |
|---|---|
| 심각도 | S3 거슬림 (주행·보행 진행불가 미검증) |
| 상태 | VERIFIED (2026-10-20, v15a3 + walkClip, 제공 JSON·캡처 범위) |
| 관련 AC | O2-WALK, 배정 기준 차도·보도 겹침 0 |
| 환경 | Windows, Unity 프로젝트 6000.3.19f1, v15a3 출력, HEAD a081c04b709fcf5de84512c4bab232100169967a + 작업트리 |
| 재현율 | 제공 데이터셋 1/1; 별도 빌드 재현 미측정 |

## 재현 절차
1. 저장소 루트에서 `node docs/qa/tools/check-o2-overlap.cjs`를 실행한다.
2. 출력 `failures`의 `overlap_m2.Sidewalk_Curb`를 확인한다.
3. 세 값 5.75, 2.5, 2를 합산한다. 종료 코드 1과 FAIL을 확인한다.

## 기대 결과
보도 겹침 0채, 0㎡.

## 실제 결과
3채, 10.25㎡. 월드 X/Z 중심 (2867.1,2332.9): 5.75㎡, (3009.7,2335.2): 2.5㎡, (2760.6,2312.7): 2㎡.

## 증거
- `tools/town-gen/out/scene_structure_s15a3/o2_scene_overlap.json`
- `docs/qa/o2-overlap-recount-2026-10-20.json` (입력 SHA256 포함)
- `docs/qa/GATE-2026-10-20-o2-overlap.md`
- 관련: `BUG-007-spline-house-road-overlap.md`는 다른 씬의 수정 검증 이력이며 이번 v15a3를 검증한 기록이 아니다.

## 처리 기록
| 날짜 | 담당 | 내용 |
|---|---|---|
| 2026-10-20 | QA | 건물별 JSON 재집계로 OPEN. 에디터·Play 실행 없음. |
| 2026-10-20 | QA | VERIFIED / v15a3 + 15a-4 walkClip. 변경된 원자료 SHA256 057421581830a48ef6798c57b8633785c6f5f1109998a41fd06e91c647f3f2cd에서 보도 0채·0㎡ 재집계, exit 0. 세 위치 추가 캡처 직접 확인. 에디터·Play·정밀 메시 재측정 미실행. |

최신 증거: `docs/qa/o2-overlap-recount-2026-10-20-walkclip.json`. 위 재현 절차는 같은 경로의 최신 입력에서는 PASS가 되며, 이전 실제 결과는 수정 전 이력이다.
