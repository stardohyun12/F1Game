# Map Expansion R1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** PitCrew 도시 layout을 17×13에서 **30×22로 3배 확장**하고 zone 3개(Harbor/Downtown/Industrial) 대각선 분할로 재구성. 배달지 7→11개, 마커 간 평균 거리 80~140m로 늘려 한 런 30~60초 도달.

**Architecture:** 활성 layout은 `Assets/Data/ComplexTrackLayout.asset` (모든 Editor 파이프라인이 이 이름으로 하드코딩 탐색). **in-place 편집**으로 진행 — 신규 SO 만들지 않음 (Editor script 4곳 동시 수정 회피, git 롤백으로 안전). `TrackLayout.cs`에 `zoneMask` 필드 추가 (런타임 zone 조회용, R1에서는 미사용 — minimap/escalation 후속 작업용 인프라).

**Tech Stack:** Unity 6000.3.9f1, C# ScriptableObject (YAML asset), unity-cli (Editor 켠 채 검증), `TrackConnectivityVerifier` BFS, `RunFullRebuild` Editor 메뉴.

---

## File Structure

| 파일 | 동작 |
|---|---|
| `F1/Assets/Scripts/Layout/TrackLayout.cs` | 수정 — `zoneMask` 필드 + `GetZoneChar(c, r)` 추가 |
| `F1/Assets/Data/ComplexTrackLayout.asset` | 수정 — 15×9 → 30×22, deliveries 7→11, zoneMask 신규 |
| `F1/Assets/Editor/_Recovery/MapExpansionR1Measure.cs` | 신규 — Play 후 RunMetricsRecorder CSV 자동 5런 측정 트리거 |

**스펙 보정 (brainstorm 후 발견)**: 스펙은 "OpenWorldLayoutV2 신규" 가정이었으나, 활성 layout은 `ComplexTrackLayout`. in-place 편집으로 가닥. 새 asset 만들면 `SetupComplexScene`/`SetupOrderSystem`/`TrackConnectivityVerifier`/`RunFullRebuild` 4곳 모두 이름 변경 필요.

---

## Pre-flight

- [ ] **Step 0a: 자기검증 3종 환경 확인** — Unity Editor 켜져 있는지, unity-cli 응답하는지.

```bash
unity-cli status
```

Expected: `connected` 또는 비슷한 OK 응답. `not connected`면 Unity Editor를 켠다.

- [ ] **Step 0b: 현재 connectivity baseline 측정**

Unity Editor에서:
```bash
unity-cli menu "PitCrew/Verify Track Connectivity"
unity-cli console --type log --since 10s
```

Expected: 15×9 layout, totalRoad ≈ 80~90, reachable == totalRoad, ends ≥ 0. 로그를 따로 메모 (R1 후 비교용).

---

## Task 1: TrackLayout.cs에 zoneMask 필드 추가

**Files:**
- Modify: `F1/Assets/Scripts/Layout/TrackLayout.cs` (line 18~22 사이 height 필드 뒤)

- [ ] **Step 1.1: 필드 + 헬퍼 추가**

`heightMask` 필드 뒤 (line 18 다음)에 추가:

```csharp
    [Tooltip("Zone mask rows (bottom-to-top). 'H'=Harbor, 'D'=Downtown, 'I'=Industrial, '.'=무소속. " +
             "R1 도입 — 런타임 zone 조회 (minimap tint, escalation 등). 비워도 정상 동작.")]
    public string[] zoneMask;
```

그리고 `GetHeight` 함수 위에 추가:

```csharp
    /// <summary>해당 셀의 zone 문자 ('H'/'D'/'I'/'.'). zoneMask 비어있거나 범위 밖이면 '.'.</summary>
    public char GetZoneChar(int c, int r)
    {
        if (zoneMask == null || r < 0 || r >= zoneMask.Length) return '.';
        if (c < 0 || c >= zoneMask[r].Length) return '.';
        return zoneMask[r][c];
    }
```

- [ ] **Step 1.2: 컴파일 클린 확인**

```bash
unity-cli editor refresh --compile
unity-cli console --type error,warning --since 10s
```

Expected: 0건. zoneMask 사용처 없으므로 추가만으로 컴파일 통과해야 함.

- [ ] **Step 1.3: Commit**

```bash
git add F1/Assets/Scripts/Layout/TrackLayout.cs
git commit -m "feat(layout): TrackLayout.zoneMask 필드 추가 (R1 인프라)

런타임 zone 조회용 (minimap·escalation 후속). R1 자체는 미사용.
GetZoneChar(c,r) 헬퍼 추가, 비워둬도 정상 동작.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: 30×22 roadMask 초안 작성

**Files:**
- Modify: `F1/Assets/Data/ComplexTrackLayout.asset` (lines 15~24, roadMask 블록)

목표 구조:
- **너비 30 × 높이 22**, 외곽 perimeter 전체 도로 (`#`)
- **수평 간선 (E-W) 2개**: row 5 (Harbor↔Downtown 잇는 남측 백본), row 16 (Downtown↔Industrial 잇는 북측 백본). 모두 30칸 다 `#`
- **수직 간선 (N-S) 2개**: col 4 (Harbor 서부), col 14 (중앙 종단), col 22 (Industrial 동부) 전 행 `#`
- **Harbor 구역 (cols 0~13, rows 0~5)**: 긴 직선 위주, 부두 cul-de-sac 2~3개. 도로 밀도 낮음.
- **Downtown 구역 (cols 4~22, rows 5~16)**: 4셀 블록 격자. col 8/12/18 rows 7/10/13에 N-S 보조 도로. 코너 多.
- **Industrial 구역 (cols 14~29, rows 16~21)**: 8셀 블록, 큰 단일 직선 위주. 단순.

- [ ] **Step 2.1: roadMask 30×22 작성** (직접 ASCII 그리고 asset에 박기)

`F1/Assets/Data/ComplexTrackLayout.asset`의 line 15~24 (`roadMask:` ~ `buildingMask:` 직전)를 아래로 교체:

```yaml
  roadMask:
  - '##############################'
  - '#...#........#...............#'
  - '#...#........#...............#'
  - '#...#........#...............#'
  - '#...##########...............#'
  - '##############################'
  - '#...#...#...#...#.#..........#'
  - '#...#...#...#...#.#..........#'
  - '#...#####...#####.#..........#'
  - '#...#...#...#...#.#..........#'
  - '#...#...#...#...#.#..........#'
  - '#...#####...#####.#..........#'
  - '#...#...#...#...#.#..........#'
  - '#...#...#...#...#.#..........#'
  - '#...#####...#####.#..........#'
  - '#...#...#...#...#.#..........#'
  - '##############################'
  - '#............#...#...........#'
  - '#............#...#####.......#'
  - '#............#.......#.......#'
  - '#............#.......#.......#'
  - '##############################'
```

22행 × 30칸. 행 순서: 위가 row 21 (top, Industrial), 아래가 row 0 (bottom, Harbor). YAML은 위→아래로 row 21 → row 0 순으로 읽힌다고 가정하면 잘못. **확인**: TrackLayout.cs:5 주석 `mask[0] corresponds to grid row 0 (bottom). Add rows bottom-to-top in the inspector.` → **mask 첫 줄(top of yaml block) = row 0**. 따라서 위 YAML 첫 줄 `##...##` = row 0 (Harbor 남측 perimeter), 마지막 줄 = row 21 (Industrial 북측 perimeter).

다시 의도 매핑 (YAML 첫 줄 = row 0):
- row 0~4: Harbor 남부 (긴 직선)
- row 5: 첫 horizontal arterial
- row 6~15: Downtown (격자 촘촘)
- row 16: 두 번째 horizontal arterial
- row 17~21: Industrial 북부 (큰 블록)

위 YAML은 이 매핑으로 작성됨. **만약 어색하면 Step 2.3 검증에서 잡힘**.

- [ ] **Step 2.2: 임시로 buildingMask·heightMask·deliveries는 그대로 두고 저장 (Task 3~5에서 채움)**

YAML 라인 수가 변하므로 `buildingMask:` 블록의 9행도 30칸×22행으로 임시로 모두 `.` 채워야 함. 저장 후 Unity에 깨진 mask로 들어가면 `TileMapAuto`가 width mismatch 오류:

`buildingMask:` 블록 (line 25~34)을 일단:

```yaml
  buildingMask:
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
```

22행 30칸 `.`. Task 5에서 zone 별 building 채움.

- [ ] **Step 2.3: 검증 — connectivity verifier 통과 (Verify 메뉴)**

```bash
unity-cli menu "PitCrew/Verify Track Connectivity"
unity-cli console --type log,warning,error --since 10s
```

Expected:
- `totalRoad` ≈ 250~400 (15×9 88셀의 약 3배)
- `reachable == totalRoad` (isolated 0)
- 모든 End는 의도된 곳 (cul-de-sac, 또는 0)
- Width mismatch 오류 0건

실패 시: 로그가 알려주는 좌표 보고 roadMask·buildingMask 폭/연결성 손봄. 이 task에서 iterate.

- [ ] **Step 2.4: startCell을 Downtown 중앙 부근으로 옮김 + commit**

`F1/Assets/Data/ComplexTrackLayout.asset` 끝 부근 `startCell: {x: 1, y: 0}` → `startCell: {x: 14, y: 10}` (col 14 = 중앙 종단, row 10 = Downtown 중심). startYawDeg: 90 유지.

```bash
git add F1/Assets/Data/ComplexTrackLayout.asset
git commit -m "feat(map): R1 roadMask 30×22 + startCell Downtown 중앙

15×9 → 30×22 (~3배 면적, 180×132m).
2개 E-W 간선 (row 5, row 16) + 3개 N-S 간선 (col 4, 14, 22).
zone별 도로 밀도: Harbor 성김 / Downtown 격자 / Industrial 큰 블록.
buildingMask·deliveries는 Task 3-5에서 채움.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: zoneMask 30×22 작성 (대각선 분할)

**Files:**
- Modify: `F1/Assets/Data/ComplexTrackLayout.asset` (`heightMask: []` 다음에 `zoneMask:` 블록 추가)

대각선 분할 규칙 (cell→zone):
- col < 14 AND row < 10 → `H` (Harbor 좌하)
- col >= 14 AND row >= 12 → `I` (Industrial 우상)
- 나머지 → `D` (Downtown 대각 띠)

- [ ] **Step 3.1: zoneMask 블록 삽입**

`heightMask: []` 줄 바로 다음, `cellHeightMeters: 1.5` 줄 직전에 삽입:

```yaml
  zoneMask:
  - 'HHHHHHHHHHHHHHDDDDDDDDDDDDDDDD'
  - 'HHHHHHHHHHHHHHDDDDDDDDDDDDDDDD'
  - 'HHHHHHHHHHHHHHDDDDDDDDDDDDDDDD'
  - 'HHHHHHHHHHHHHHDDDDDDDDDDDDDDDD'
  - 'HHHHHHHHHHHHHHDDDDDDDDDDDDDDDD'
  - 'HHHHHHHHHHHHHHDDDDDDDDDDDDDDDD'
  - 'HHHHHHHHHHHHHHDDDDDDDDDDDDDDDD'
  - 'HHHHHHHHHHHHHHDDDDDDDDDDDDDDDD'
  - 'HHHHHHHHHHHHHHDDDDDDDDDDDDDDDD'
  - 'HHHHHHHHHHHHHHDDDDDDDDDDDDDDDD'
  - 'DDDDDDDDDDDDDDDDDDDDDDDDDDDDDD'
  - 'DDDDDDDDDDDDDDDDDDDDDDDDDDDDDD'
  - 'DDDDDDDDDDDDDDIIIIIIIIIIIIIIII'
  - 'DDDDDDDDDDDDDDIIIIIIIIIIIIIIII'
  - 'DDDDDDDDDDDDDDIIIIIIIIIIIIIIII'
  - 'DDDDDDDDDDDDDDIIIIIIIIIIIIIIII'
  - 'DDDDDDDDDDDDDDIIIIIIIIIIIIIIII'
  - 'DDDDDDDDDDDDDDIIIIIIIIIIIIIIII'
  - 'DDDDDDDDDDDDDDIIIIIIIIIIIIIIII'
  - 'DDDDDDDDDDDDDDIIIIIIIIIIIIIIII'
  - 'DDDDDDDDDDDDDDIIIIIIIIIIIIIIII'
  - 'DDDDDDDDDDDDDDIIIIIIIIIIIIIIII'
```

(row 0~9: Harbor 좌하 / Downtown 우하 분할. row 10~11: 전체 Downtown 띠. row 12~21: Downtown 좌상 / Industrial 우상.)

- [ ] **Step 3.2: 컴파일 + 런타임 zone 조회 검증**

```bash
unity-cli editor refresh --compile
unity-cli console --type error,warning --since 10s
```

런타임 한 줄 점검:

```bash
unity-cli exec "var l = UnityEditor.AssetDatabase.LoadAssetAtPath<TrackLayout>(\"Assets/Data/ComplexTrackLayout.asset\"); return $\"H@(0,0)={l.GetZoneChar(0,0)} I@(29,21)={l.GetZoneChar(29,21)} D@(20,11)={l.GetZoneChar(20,11)}\";"
```

Expected: `H@(0,0)=H I@(29,21)=I D@(20,11)=D`.

- [ ] **Step 3.3: Commit**

```bash
git add F1/Assets/Data/ComplexTrackLayout.asset
git commit -m "feat(map): R1 zoneMask 대각선 3분할 (H/D/I)

Harbor 좌하 / Downtown 대각 띠 / Industrial 우상.
runtime GetZoneChar(c,r) 검증 OK.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: heightMask — Industrial 일부 elevation

**Files:**
- Modify: `F1/Assets/Data/ComplexTrackLayout.asset` (`heightMask: []` 교체)

규칙 (asset.md D-050):
- Harbor·Downtown: 전 평지 (`.`)
- Industrial: 일부 1 tier elevation, slope tile 사용 (`n/s/e/w/=`). **road cell에 직접 `1`/`2` 박지 않음** → 인접 도로 단절 step 발생.

R1은 단순화: heightMask 전부 평지 (`.`)로 두고 elevation은 R2 이후로 미룸. Industrial 시각적 차별은 building 종류로만 표현.

- [ ] **Step 4.1: heightMask를 22행 30칸 `.` 로 교체**

`heightMask: []` 한 줄을 아래로:

```yaml
  heightMask:
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
  - ..............................
```

- [ ] **Step 4.2: refresh + connectivity 재검증**

```bash
unity-cli editor refresh --compile
unity-cli menu "PitCrew/Verify Track Connectivity"
unity-cli console --type log,warning,error --since 10s
```

Expected: Task 2와 동일 결과 (heightMask 평지 변경은 connectivity에 무영향).

- [ ] **Step 4.3: Commit**

```bash
git add F1/Assets/Data/ComplexTrackLayout.asset
git commit -m "chore(map): heightMask 30×22 평지로 확장 (R1 elevation 보류)

Industrial elevation은 slope tile 패턴이 필요 — R2에서 다룸.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: buildingMask 작성 (zone별 building 종류)

**Files:**
- Modify: `F1/Assets/Data/ComplexTrackLayout.asset` (buildingMask 22행 30칸)

BuildingCatalog 키 (기존, SetupLayoutAssets.cs 참조):
- `S` suburban (작은 집), `C` commercial (상점), `H` highrise (스카이라인), `T` tower (굴뚝)
- `F` factory (2×2 대형), `W` warehouse, `K` (commercial big), `P` plaza
- `.` 빈 셀

zone별 추천:
- Harbor (row 0~9, col 0~13): `W` warehouse 위주 + `T` tower(굴뚝) 1~2개. 부두 느낌.
- Downtown (대각 띠): `H` highrise 위주 + `C` commercial 중심. 스카이라인.
- Industrial (row 12~21, col 14~29): `F` factory 2×2 (좌상 모서리만 박음) + `W` warehouse + `T` tower 굴뚝 多.

도로 인접해야만 의미 (BuildingPlacer가 road-adjacent만 사용). roadMask 안 도로 사이 빈 셀에 채움.

- [ ] **Step 5.1: buildingMask 22행 30칸 작성 (Task 2에서 빈 `.` 박은 것 교체)**

(예시 — 도로 사이 빈 공간 잘 보고 채워야 함. roadMask와 셀-by-셀 대응 점검 필수.)

대략적 의도 (도로 셀은 `.` 유지, 비도로 셀에 building 박음):

```yaml
  buildingMask:
  - ..............................
  - .WW.WWWW.WWW.WWWW.WWWW.WWWW..
  - .W..W..W.W.W.W..W.W..W.W..W..
  - .WW.WWWW.WWW.WWWW.WWWW.WWWW..
  - ..............................
  - ..............................
  - .HHH.HHH.HHH.HHH..HHH.HHH....
  - .H.H.H.H.H.H.H.H..H.H.H.H....
  - .HHH.HHH.HHH.HHH..HHH.HHH....
  - .HHH.HHH.HHH.HHH..HHH.HHH....
  - .CCC.CCC.CCC.CCC..CCC.CCC....
  - .HHH.HHH.HHH.HHH..HHH.HHH....
  - .HHH.HHH.HHH.HHH..HHH.HHH....
  - .CCC.CCC.CCC.CCC..CCC.CCC....
  - .HHH.HHH.HHH.HHH..HHH.HHH....
  - .HHH.HHH.HHH.HHH..HHH.HHH....
  - ..............................
  - .WWW.WWW.WWW...FF.FF..TTT....
  - .W.W.W.W.W.W...FF.FF..T.T....
  - .WWW.WWW.WWW...FF.FF..TTT....
  - .WWW.WWW.WWW...FF.FF..TTT....
  - ..............................
```

**중요**: 위는 의도 가이드. roadMask `#`와 겹치는 자리는 building 안 됨. 셀-by-셀로 roadMask와 대조하면서 road cell은 `.`로 비우고 빈 cell에만 building 박을 것. BuildingPlacer가 road-conflict 경고 띄움.

- [ ] **Step 5.2: BuildingPlacer 검증**

```bash
unity-cli editor refresh --compile
unity-cli menu "PitCrew/Run Full Rebuild"
unity-cli console --type log,warning,error --since 30s
```

Expected:
- `[RunFullRebuild] DONE` 로그
- BuildingPlacer 경고 (road-overlap, missing key 등) 0건
- TrackConnectivityVerifier `OK`

building이 도로 위에 박혔다고 경고 뜨면 해당 좌표 buildingMask에서 `.`로 바꾸고 재시도.

- [ ] **Step 5.3: 시각 확인 (top-down 스크린샷)**

```bash
unity-cli screenshot --view scene --output captures/r1-topdown.png
```

`captures/r1-topdown.png`를 열어 도시가 30×22로 채워졌는지, zone별 building 색감 구분되는지 눈으로 확인.

- [ ] **Step 5.4: Commit**

```bash
git add F1/Assets/Data/ComplexTrackLayout.asset captures/r1-topdown.png
git commit -m "feat(map): R1 buildingMask zone별 분포 (Harbor W / Downtown H+C / Industrial F+W+T)

도로-비도로 셀 대응 점검 완료. BuildingPlacer 경고 0.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 6: 배달지 11개 재배치

**Files:**
- Modify: `F1/Assets/Data/ComplexTrackLayout.asset` (`deliveries:` 블록)

zone별 분배:
- Harbor 3: PierEnd, CargoShip, Warehouse (rows 0~5, cols 0~13)
- Downtown 4: OfficeCornerN, OfficeCornerS, CentralPlaza, MainStation (rows 6~15, cols 4~22)
- Industrial 4: FactoryGateA, FactoryGateB, MotorDepot, ContainerYard (rows 16~21, cols 14~29)

각 cell은 **roadMask가 `#` (도로)인 셀** 위에 둠. 배달지 마커는 도로 옆 cell 아니라 도로 cell.

- [ ] **Step 6.1: deliveries 블록 교체**

기존 7개 deliveries (line 38~59 부근)를 아래로 교체:

```yaml
  deliveries:
  - col: 0
    row: 1
    label: PierEnd
  - col: 4
    row: 0
    label: CargoShip
  - col: 13
    row: 3
    label: Warehouse
  - col: 4
    row: 10
    label: OfficeCornerS
  - col: 18
    row: 8
    label: OfficeCornerN
  - col: 14
    row: 13
    label: CentralPlaza
  - col: 22
    row: 11
    label: MainStation
  - col: 16
    row: 21
    label: FactoryGateA
  - col: 28
    row: 17
    label: FactoryGateB
  - col: 25
    row: 21
    label: MotorDepot
  - col: 29
    row: 16
    label: ContainerYard
```

**주의**: 각 (col, row) 좌표가 roadMask에서 `#`인지 직접 확인. `.`이면 도로 외 위치 → 마커 표시 깨짐. Task 2의 roadMask 그대로면 위 좌표는 perimeter(`#`)와 간선 위. 다시 한 번 cross-check 필요.

- [ ] **Step 6.2: 좌표 cross-check (unity-cli exec로 자동)**

```bash
unity-cli exec "var l = UnityEditor.AssetDatabase.LoadAssetAtPath<TrackLayout>(\"Assets/Data/ComplexTrackLayout.asset\"); var bad = new System.Collections.Generic.List<string>(); foreach (var d in l.deliveries) { if (!l.IsRoad(d.col, d.row)) bad.Add($\"{d.label}({d.col},{d.row})\"); } return bad.Count == 0 ? \"all-on-road\" : \"BAD: \" + string.Join(\",\", bad);"
```

Expected: `all-on-road`. `BAD: ...`면 해당 deliveries 좌표 수정.

- [ ] **Step 6.3: SetupOrderSystem + DeliveryManager 재실행**

```bash
unity-cli menu "PitCrew/Run Full Rebuild"
unity-cli console --type log,warning,error --since 30s
```

Expected: `Layout 'ComplexTrackLayout' deliveries: 11개` 로그.

- [ ] **Step 6.4: Commit**

```bash
git add F1/Assets/Data/ComplexTrackLayout.asset
git commit -m "feat(map): R1 배달지 7→11 재배치 (zone당 H3/D4/I4)

모든 좌표 roadMask=='#' 확인.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 7: Play 테스트 + 5런 자동 측정

**Files:**
- Create: `F1/Assets/Editor/_Recovery/MapExpansionR1Measure.cs`

기존 `RunMetricsRecorder`가 런 CSV를 자동 기록. 이 task는 5런을 manual play로 돌리고 결과 보는 것.

- [ ] **Step 7.1: Play 진입 + frameCount 확인 (Step() 함정 회피)**

```bash
unity-cli editor play --wait
unity-cli exec "return $\"frame={UnityEngine.Time.frameCount} time={UnityEngine.Time.time:F2}\";"
```

Expected: frame이 0이 아니고 증가 중. (`feedback-unity-step-pauses`)

- [ ] **Step 7.2: console 런타임 오류 확인**

```bash
unity-cli console --type error --since 10s
```

Expected: 0건. Input System 등 런타임-only 오류 (`reference-input-system-mode`) 잡힘.

- [ ] **Step 7.3: 도현에게 5런 수동 플레이 요청**

`feedback-verify-before-done` 적용. 도현에게:

> "30×22 맵 R1 빌드 완료. Editor가 Play 상태. 차로 5번 배달 (zone마다 한 번씩이라도) 돌고 끝나면 알려주세요. RunMetricsRecorder가 CSV 자동 기록."

도현이 끝나면 다음 step.

- [ ] **Step 7.4: CSV 결과 확인**

```bash
unity-cli menu "PitCrew/Metrics/Open Run Metrics CSV"
```

CSV 마지막 5행 확인. 측정 기준 (spec §5):
- 한 런 30~60초 도달 (현재 8~12s)
- 3 zone 모두 배달지 도달

미달이면 메모만 (R2에서 더 늘림). R1 완료 기준은 "측정값 받음"이지 "목표 달성"이 아님.

- [ ] **Step 7.5: Play 종료 + commit (CSV가 .gitignore면 commit 안 함)**

```bash
unity-cli editor stop
git status
```

CSV가 git에 안 들어가면 결과를 plan 끝에 코멘트 또는 `옵시디언/notes/session-log.md`에 기록.

```bash
git add -A
git commit -m "test(map): R1 5런 측정 — 한 런 [N]s / 거리 [M]m

[측정 결과 요약]

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 8: activeContext 갱신 + 세션 종료 준비

**Files:**
- Modify: `.claude/activeContext.md`
- Modify: `옵시디언/notes/session-log.md`
- Modify: `옵시디언/notes/decisions-log.md` (D-072 등록)

- [ ] **Step 8.1: D-072 결정 기록**

`옵시디언/notes/decisions-log.md` 끝에 추가:

```markdown
## D-072 — 맵 확장 단계적 진입 (2026-05-16, 세션 27)

세션 26 RunMetricsRecorder 측정 결과 한 런 8~12s/49~110m → 데모 목표 5~15분과 200~900배 갭. 거리 확장이 escalation·차고·페이싱 선결조건. 한 번에 17배 가지 않고 단계적 (R1: 3배 → 측정 → R2 결정). zone 대각선 분할 (Harbor 좌하 / Downtown 대각 / Industrial 우상), 손 ASCII roadMask 유지, 배달지 7→11.
```

- [ ] **Step 8.2: activeContext.md "다음 세션" 섹션 업데이트**

`.claude/activeContext.md`의 "다음 세션 (세션 27) — 첫 작업" 섹션을 다음 세션(28)용으로 교체:

```markdown
## 다음 세션 (세션 28) — 첫 작업

R1 측정 결과를 본 뒤 R2 결정. 한 런이:
- 30~60s 도달했으면 → escalation brainstorm 진입 (런 안 시간 압박)
- 여전히 < 30s면 → R2 (60×40 정도, 6배) 추가 확장

reward·zone unlock cost 재튜닝도 R2 (또는 escalation) 작업과 함께.
```

"지난 세션 요약"에 세션 27 블록 추가 (요약 3~4줄).

- [ ] **Step 8.3: session-log.md에 세션 27 블록**

```markdown
## 세션 27 (2026-05-16)

**한 일**: 맵 R1 확장 — ComplexTrackLayout 15×9 → 30×22 (~3배). zone 대각선 분할 (H/D/I), zoneMask 필드 신규, 배달지 11개. TrackConnectivityVerifier 통과, 5런 측정 [결과].

**다음엔**: R1 측정 결과 따라 R2 또는 escalation brainstorm 선택.

**메모**: ComplexTrackLayout in-place 편집 (OpenWorldLayoutV2 신규는 spec 오류). 스펙 보정 plan 헤더에 기록.
```

- [ ] **Step 8.4: Commit + 브랜치 push (도현 확인 후)**

```bash
git add .claude/activeContext.md 옵시디언/notes/
git commit -m "docs(session): 세션 27 종료 — 맵 R1 완료, D-072 등록

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

push 여부는 도현에게 확인 후.

---

## Verification Gates (전체 plan 통과 기준 — spec §5)

1. **TrackConnectivityVerifier 통과** — isolated 0, 모든 zone 도달 가능 (Task 2.3 + Task 5.2)
2. **5런 자동 측정 CSV** — Task 7.4에서 기록됨
3. **3 zone 모두 배달지 도달 확인** — Task 7.3 수동 play

3 다 통과면 R1 종료, R2 brainstorm 진입.

---

## Risk / Notes

- **mask 행 순서**: TrackLayout.cs:5 주석 = `mask[0] = row 0 (bottom)`. YAML asset에서 첫 줄이 row 0. 시각적으로 위/아래가 헷갈리면 unity-cli exec로 `l.IsRoad(c, r)` 직접 조회로 확인.
- **buildingMask · roadMask 길이 일치 필수**. `TileMapAuto: row N width X != expected Y` 경고 뜨면 30 정확히 맞췄는지 확인.
- 30×22 = 660 셀 → BuildingPlacer/streetlight/traffic light scripts가 더 느려질 수 있음. 30s 이상 걸리면 unity-cli timeout 늘림.
- 카메라 follow가 30셀 폭을 못 따라잡으면 zoom out 또는 미니맵 의존도 ↑. R1 끝에서 확인.
- 채널 5 슬라이드 deadzone 튜닝(activeContext 메모)은 R1 범위 외 — 슬로프 없으니 무관.
