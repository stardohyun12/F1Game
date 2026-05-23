# 맵 구조 재디자인 Implementation Plan

> **For agentic workers:** 이 plan은 Unity 에디터 1개에 묶여 순차 실행. 각 mask 변경은 `TrackConnectivityVerifier`/`IsRoad`/스크린샷으로 검증(테스트=이 검증). subagent 병렬 불가(에디터 단일).

**Goal:** 같은 30×22 `ComplexTrackLayout`을 in-place 편집해, 세 지구(항구/다운타운/공업)가 **눈에 띄게 다른 도로 리듬**을 갖게 재구성 — greybox 단조로움 제거. 중앙 랜드마크 없음, 항구에 바다 평면.

**Architecture:** roadMask/zoneMask/buildingMask/deliveries 데이터 편집(in-place) + 항구 가장자리 단색 바다 평면(씬 쿼드 1개). 도로는 `#`(이웃으로 직선/곡선/T/십자/dead-end 자동). 새 자산 0.

**Tech Stack:** Unity 6000.3.9f1, ScriptableObject YAML, unity-cli, `TrackConnectivityVerifier`(BFS), `RunFullRebuild`.

참조 spec: `docs/superpowers/specs/2026-05-23-map-structure-redesign-design.md`. 목표 도면: 허브 🗺 맵 탭(map-design-v3).

---

## 설계 의도 (v3 도면 → 30×22 그리드)

`#`=도로, `.`=비도로(건물자리). mask[0]=row0=bottom (TrackLayout.cs:5).
- **항구** (한 모서리, rows 16~21 × cols 0~13 영역): **성긴 + 불규칙**. 물가 따라 동서 직선 1, 바다로 뻗은 부두 dead-end 2~3. 도로 밀도 낮음.
- **다운타운** (중앙 띠): **조밀 작은 격자** — 2~3셀 간격 촘촘. 코너 많음.
- **공업지** (반대 모서리, rows 0~9 × cols 16~29 영역): **크고 성긴 8셀 블록** + 굵은(2칸폭) 간선.
- **간선 척추**: 2칸폭 큰길이 세 지구 관통(동서 1 + 남북 1).
- **바다**: 항구 쪽 가장자리 1~2행을 비우고(도로 없음) 그 바깥에 단색 바다 평면 쿼드.

---

## Task 1: roadMask 재작성 (지구별 리듬) + 연결성 통과

**Files:** `F1/Assets/Data/ComplexTrackLayout.asset` (roadMask 22행 블록)

- [ ] **Step 1.1** 현재 roadMask 블록 백업(주석/메모) 후, 위 설계 의도대로 30×30폭 정확히 22행 새 roadMask 작성. 척추(2칸폭 간선) 먼저 깔고 → 다운타운 조밀격자 → 공업 큰블록 → 항구 성긴+부두 순.
- [ ] **Step 1.2** 저장 후 연결성 검증:
  ```bash
  unity-cli editor refresh --compile
  unity-cli menu "PitCrew/Verify Track Connectivity"
  unity-cli console --type log,warning,error --since 10s
  ```
  Expected: `30×22 mask, N road cells, N reachable`, isolated 0, End는 의도된 부두 dead-end 개수만. width mismatch 0.
- [ ] **Step 1.3** isolated/도달불가 있으면 verifier가 준 좌표로 roadMask 수정 → 재검증. **통과까지 반복.**
- [ ] **Step 1.4** Commit: `feat(map): roadMask 지구별 리듬 재구성 (항구 성긴/다운타운 조밀/공업 큰블록)` + Verified-By(연결성 N/N).

## Task 2: zoneMask를 새 지구 경계에 정렬

**Files:** 같은 asset (zoneMask 블록)

- [ ] **Step 2.1** 새 roadMask의 지구 영역에 맞춰 zoneMask(`H`/`D`/`I`) 재작성.
- [ ] **Step 2.2** 검증:
  ```bash
  unity-cli exec "var l=UnityEditor.AssetDatabase.LoadAssetAtPath<TrackLayout>(\"Assets/Data/ComplexTrackLayout.asset\"); return $\"항구셀={l.GetZoneChar(2,18)} 다운={l.GetZoneChar(14,11)} 공업={l.GetZoneChar(24,4)}\";"
  ```
  Expected: 각 지구 좌표가 H/D/I로 (실제 좌표는 Task1 결과에 맞춤).
- [ ] **Step 2.3** Commit.

## Task 3: buildingMask 지구별 분포

**Files:** 같은 asset (buildingMask)

- [ ] **Step 3.1** 비도로(`.`) 셀에만 building. 항구=`W`창고+`T`굴뚝, 다운타운=`H`고층+`C`상업, 공업=`F`공장+`W`창고+`T`. 도로셀과 셀-by-셀 대조(도로 위 building 금지).
- [ ] **Step 3.2** `unity-cli menu "PitCrew/Run Full Rebuild"` → BuildingPlacer 경고(도로 overlap) 0건, ConnectivityVerifier OK.
- [ ] **Step 3.3** Commit.

## Task 4: 항구 바다 평면

**Files:** `F1/Assets/Editor/_Recovery/HarborSea.cs` (신규, [MenuItem]) 또는 씬 직접

- [ ] **Step 4.1** 항구 가장자리 바깥에 단색(짙은 청록) 평면 쿼드 1개 배치하는 Editor 메뉴 작성(멱등: 있으면 갱신). 위치/크기는 항구 영역 월드좌표 기준.
- [ ] **Step 4.2** 실행 후 씬뷰 스크린샷으로 바다가 항구 옆에 보이는지 확인.
- [ ] **Step 4.3** Commit (+Feels-off, 화면 변경).

## Task 5: 배달지 11개 재배치

**Files:** 같은 asset (deliveries)

- [ ] **Step 5.1** 지구별 분배(항구3/다운4/공업4 등)로 좌표 갱신, 모두 도로셀 위.
- [ ] **Step 5.2** 전수 검증:
  ```bash
  unity-cli exec "var l=UnityEditor.AssetDatabase.LoadAssetAtPath<TrackLayout>(\"Assets/Data/ComplexTrackLayout.asset\"); var bad=new System.Collections.Generic.List<string>(); foreach(var d in l.deliveries) if(!l.IsRoad(d.col,d.row)) bad.Add($\"{d.label}({d.col},{d.row})\"); return bad.Count==0?\"all-on-road\":\"BAD:\"+string.Join(\",\",bad);"
  ```
  Expected: `all-on-road`.
- [ ] **Step 5.3** Commit.

## Task 6: 리빌드 + 시각 검증

- [ ] **Step 6.1** `unity-cli menu "PitCrew/Run Full Rebuild"` → 에러 0.
- [ ] **Step 6.2** top-down 스크린샷 → 세 지구가 다른 리듬으로 읽히는지 + 항구 바다 눈으로 확인. 허브 🗺 맵 탭과 대조.
- [ ] **Step 6.3** 검증 서브에이전트 dispatch(D-073): connectivity·deliveries·zone·rebuild 에러 0 표로. 클린 후 최종 commit.

## Verification Gates (spec §6)
1. TrackConnectivityVerifier: isolated 0, 부두 외 End 0.
2. 배달지 11개 전부 도로 위 (Task5 IsRoad).
3. top-down 스샷: 세 지구 시각 구분 + 항구 바다.
4. `bash scripts/regression-check.sh` FAIL 0.

## Risk
- 30×22 손 ASCII는 connectivity 반복 필요(복붙 1회 X). verifier 좌표로 iterate.
- 바다 평면이 씬에 저장/회귀 포함되는지 확인.
- zoneMask 방향(mask[0]=bottom) 헷갈리면 IsRoad/GetZoneChar exec로 직접 확인.
