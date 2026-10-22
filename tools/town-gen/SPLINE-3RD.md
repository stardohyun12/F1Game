# TownGen_Spline 3차 수정 — 프로그래밍팀 기록 (2026-10-20)

태스크 `01a152fb` · 판정 근거 `docs/qa/GATE-2026-10-20-spline-town.md` (그래픽 6/10 FAIL, QA 6P/2F). 커밋 안 함.

## 결과 (측정: `tools/town-gen/qa_mirror/run.sh` → `tools/town-gen/out/qa_mirror_v3/`)

| 항목 | 2차 | 3차 |
|---|---|---|
| M1 어깨 바깥 끝 − 지형 | 262건 / 최대 0.379m | **0건 / 최대 0.00003m** (경계 분류 완전) |
| 관통 (차로·교차점 판·골목·보도) | 0/0/0/0 | 0/0/0/0 |
| M2 경사 위반 (큰길·골목·헤어핀 패드 5%) | 0 | 0 |
| M3 큰길 8m 미만 | 8점 (최소 6.05m) | **0점** (최소 8.18m, 12m 미만 18점은 헤어핀 예외 미분리 참고치) |
| M3 골목 6m 미만 | 0 | 0 (최소 6.24m) |
| M4 집 겹침·기단 | 0 | 0 |
| M7 깎기 6m 초과 (분모 = 바뀐 지형 점) | 0.23% / 최대 8.86m | 0.10% / 최대 9.06m — §2.9 경고 통과(≤1%, ≤10m) |
| M6 연결 덩어리 | 1 | 1 |
| M8 해시 | 같음 | 같음 (`0CB3707D2F89`) |
| 진입로 | 없음 | 293 / 집 300 (계단 163, 겹침 때문에 못 냄 2) |
| 단위 테스트 | 33 | **39 통과** (`dotnet test tools/town-gen/tests`) |
| 회귀 하네스 | 12P/1F | 12P/1F (FAIL = 원래 있던 CargoPolyBudget) |

## 무엇을 바꿨나

1. **골목 입구** — 골목이 큰길 중심선에서 시작해 밝은 돌이 차도 위에 깔렸다(운전 캡처 결함 1의 실제 원인). 입구를 큰길 보도 바깥 끝·보도 높이로 옮기고, 입구 둘레 큰길 어깨는 비운다. `SplineTown.TrimToMouth`.
2. **교차점 판** — 붙는 길은 끊는 점(clipA/clipB)에서 멈추고, 그 안은 부채꼴 판(가운데 = 교차점 높이, 둘레 = 끊는 선 + 모서리 곡선)이 덮는다. 모서리마다 연석·보도·어깨를 둘렀다. 사이각이 좁으면 두 길의 보도 바깥선이 갈라질 때까지 더 뒤에서 끊는다. 끊는 점은 중심선 점에 맞춘다. 판 아래 지형은 부채꼴 삼각형 기준으로 눌러, 판이 꼬여도 맞게 했다. `BuildJunctionLayouts`, `CornerPath`, 씬 `JunctionPatch`.
3. **옹벽** — 교차점 둘레와 다른 길 단면 위를 지나는 조각은 세우지 않는다(교차점에서 벽이 길을 가로지르던 것).
4. **진입로** — 집마다 길 가장자리에서 현관까지 포장한다. 경사가 25%를 넘으면 계단(디딤 약 0.18m)으로 놓고, 지나는 옹벽 조각은 걷어 낸다. `SplineTown.Driveways`, 씬 `Driveway`, 씬 오브젝트 `Roads/Driveways`.
5. **M3** — 아래 다섯 가지를 고쳤다.
   - 10m 간격 교차점 매듭(항구)을 한 교차점으로 합친다: `CollapseShortLinks`, 기준 15m.
   - 같은 두 교차점을 잇는 겹친 중복 길을 하나만 남긴다. 다듬기 뒤에도 한 번 더 본다: `DropDuplicateEdges`.
   - S자에서 반경을 줄이기 전에 꼭짓점을 빼서 편다. 원래 선 이탈 6m 안일 때만 한다.
   - 잇기 길(생성 길)은 펴기·이완 이탈 상한을 20m로 둔다. OSM 6m 상한은 실제 길에만 적용한다.
   - 반경 이완이 이웃 3점(2m) 반경도 보고, 재표본 뒤 다시 검사한다.
6. **M1** — 어깨 바깥 선이 지형 격자선·대각선을 지나는 점마다 정점을 넣었다. 바깥 끝이 지형 삼각형 면 위에 정확히 놓인다. `SplineTown.GridCrossings`. 2차에 말한 "지형 상한 고정이 만든 접힘"을, 선형 보간이 그 접힘을 가로지르지 않게 해서 해결했다. 관통 0은 유지했다.

튜닝값 (`SplineTownSettings`, 가설): `junctionMergeMeters 15` · `driveWidthMeters 2.4` · `driveStepGrade 0.25` · `freeEndTrimMeters 30` · `connectorStraightenDeviationMeters 20`.

## 확인 방법

- 다시 만들기: 에디터 메뉴 `PitCrew/TownGen/Build Spline Town` → `Assets/Scenes/TownGen/TownGen_Spline.unity`
- 캡처 3장: `tools/town-gen/out/shots/Spline_top.png` · `Spline_slope.png` · `Spline_drive.png`
- 교차점 가까이 캡처 16장(참고, 토큰 필요 없음): `tools/town-gen/out/shots/dbg/j0~j7_{top,obl}.png`
- 측정: `bash tools/town-gen/qa_mirror/run.sh` (토큰 필요) → `tools/town-gen/out/qa_mirror_v3/spline-measurements.json`
- 테스트: `cd tools/town-gen/tests && dotnet test`

## 측정 도구 주의 (QA 확인 필요)

QA 원본 `docs/qa/tools/spline-measure.py` 는 "큰길 한 줄 = 코어 중심선 점 수 − 1 개의 아스팔트 사각형"을 가정한다(`assert offset==len(quads)`). 3차부터 큰길 띠가 교차점 쪽에서 끊기고 판이 삼각형 부채로 붙어서, 원본은 이번 씬에서 `IndexError` 로 멈춘다. 구조 변경에 맞춰 세 곳만 고친 사본이 `tools/town-gen/qa_mirror/spline-measure-v3.py` 다(파일 머리에 고친 곳 적음).
- 띠 개수: clip 구간 점 수 − 1
- 경사 분류: 호 길이로 가장 가까운 코어 점 표시
- M6: 판으로 띠 끝을 잇는다

이 사본은 작성자 측정이며 QA 독립 판정이 아니다. 코어 내보내기 JSON 의 `edges[].clipA/clipB` 로 끊는 거리를 읽을 수 있다.

## 남은 것

- 운전 캡처 시점이 옮겨졌다. 규칙("가장 긴 큰길 가운데")은 그대로인데, 매듭을 합치고 중복 길을 빼면서 가장 긴 길이 바뀌었다. 지금 화면에는 교차점·집이 없다. 교차점·진입로는 dbg 캡처로 확인한다.
- 좁은 Y 교차점(83,75)의 판에 명암 접힘선이 한 줄 보인다. 판 가운데와 둘레 높이가 달라 부채꼴이 휘기 때문이다. 침투는 아니다.
- 언덕 위 뒤 단 집(계단식 74채)의 진입로는 10~25m 긴 계단이 된다. 의도한 경험인지는 기획·그래픽 판정이 필요하다.
- 실제 OSM 길의 원래 선 이탈 최대 6.7m(상한 6m)는 이번 범위 밖이라 그대로다. 필렛 R12 로 큰 각을 돌면 바깥으로 나간다.
- M7 은 섞기 경계 띠(x 180~220, z≈100)에 12점, 최대 9.06m 다. §2.9 ① 경고 통과지만 8m 넘는 점은 기획 확인 대상이다.
- 씬 삼각형 1,082,254개. 진입로 계단 상자와 어깨 격자 정점이 늘었다.
- 바다 색·물가 경계는 손대지 않았다(지시대로).
