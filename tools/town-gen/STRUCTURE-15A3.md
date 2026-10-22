# 조각 15a-3 — 도로 위 건물 고침 (v15a3, 2026-10-20)

근거: `.ai_logs/o1-diag/REPORT.md` (O1=0 인데 SS163 Via Matteo Camera 위 건물 덩어리).
씬 `F1/Assets/Scenes/TownGen/TownGen_Structure_v15a3.unity` · 메뉴 `PitCrew/TownGen/Build Structure A v15a3 (no car-tunnel exception, scene-mesh O2)`.

## 바꾼 것
1. **차도 터널 예외 제거** — `BuildV2(carTunnel: false)`: 차도·좁은 차로 중 OSM `tunnel/covered` 태그 way(4개)에 대한 `segUnder` 예외를 끔. 생성기는 이 길을 땅 위 열린 길로 그리므로, 그 위 건물은 `road_carriageway` 로 빠진다. 보행길의 건물 밑 구간(`under`)은 `TownGenTownPaths` 가 원래대로 안 그린다(변경 없음).
2. **검사 O2** — `TownGenSceneOverlap.Measure` (`F1/Assets/Editor/TownGen/TownGenSceneOverlap.cs`). 씬에 실제 놓인 메시로 잰다.
   - 건물 = `OsmBuildings/*` 지붕 삼각형 덩어리(한 채), 길 = `Roads/*` 위 향한 삼각형, 0.5m 칸.
   - 상자 전체(400m 제한 없음). 차도(Asphalt·Alleys·TownSheet_Lane·PlazaPockets) + 보도(Sidewalk_Curb·TownSheet_Walk·TownSteps·PublicStairs·Driveways) 겹침 = FAIL. 흙 띠(Shoulder) = 숫자만.
   - 예외: 길 면 + 3m ≤ 건물 벽 아랫단(길이 실제로 건물 밑)일 때만.
   - 결과: `tools/town-gen/out/scene_structure_s15a3/o2_scene_overlap.json` (+ `measure.json` 의 `sceneOverlapO2`, report 의 O2 줄).
3. 확인 캡처 2장: `tools/town-gen/out/shots/s15a3_o2_top_2718_2291.png`, `s15a3_o2_obl_2718_2291.png` (진단 캡처와 같은 시점).

## 결과 (실측)
| 항목 | v15a2 (O2 를 그 씬에 돌림) | v15a3 |
|---|---|---|
| 차도 겹침 | 6채 308.5㎡ | **0채 0㎡** |
| 보도 겹침 | 9채 103.5㎡ | 3채 10.25㎡ |
| 흙 띠(숫자만) | 37채 526.3㎡ | 31채 354㎡ |
| O2 | FAIL 9채 | **FAIL 3채 (보도만)** |
| 놓인 OSM 건물 | 703 | 696 (−7, 모두 road_carriageway) |
| O1 | PASS | PASS |

O2 를 v15a2 씬에 돌린 값이 진단 측정(REPORT.md)과 같다 → 검사가 그 버그를 잡는다는 회귀 증거.

## 남은 것 — 보도 3채 (판단 필요)
조각 11 규칙(중심 400m 안 건물은 보도 겹침이면 살리고 길이 밑으로 숨음, `keptOverSidewalkOrAlley`=3)으로 일부러 남긴 건물이다.
- (2867,2333) 832㎡ 건물, 보도 5.75㎡ · (3010,2335) 보도 2.5㎡ · (2761,2313) 보도 2㎡. 항구에서 75 / 191 / 84m.
- 세 곳 모두 보도 면(4.1~8.6m)이 건물 바닥(3.4 / 4.2 / 7.5m)보다 높다 → 보도가 벽 안으로 들어가 끝난다(건물이 뜨지는 않음).
- 선택: A) 보도 겹침도 건물 뺌(조각 10 규칙, 3채 빠짐) · B) 건물 살리고 보도 면을 건물 윤곽에서 잘라냄 · C) O2 에서 중심 보도 겹침은 숫자만.

## 15a-4 — 보도 3채: B 적용 (2026-10-20)
- 결정 B(PD): 건물 3채는 두고 `Sidewalk_Curb` 면을 놓인 건물 윤곽에서 잘라냄. `TownGenWalkClip.Clip` (`F1/Assets/Editor/TownGen/TownGenWalkClip.cs`), `BuildV2(walkClip: true)` — v15a3 메뉴만 켬.
  - 위 면: xz 에서 삼각형 − 건물 윤곽(귀 자르기 볼록 조각마다 볼록 빼기), 남은 조각은 원래 평면 높이. 옆면: 무게중심이 윤곽 안이면 뺌. 예외는 O2 와 같음(보도 + 3m ≤ 벽 아랫단이면 안 뺌).
- 결과(실측, 같은 씬 v15a3): 잘린 건물 3채 · 잘린 삼각형 32 · 뺀 면적 10.4㎡(O2 측정 10.25㎡) · 보도 삼각형 66,024 → 66,066.
- **O2 PASS: 차도 0채 0㎡ · 보도 0채 0㎡** · 흙 띠 31채 354㎡(숫자만) · 놓인 건물 696(그대로). `o2_scene_overlap.json` 의 `walkClip` 에 수치.
- 캡처: `tools/town-gen/out/shots/s15a3_walkclip_2867_2333.png`, `_3010_2335.png`, `_2761_2313.png`.
- M3·M7 FAIL 은 15a 부터 있던 것(이번 변경과 무관, 길 생성 단계).
