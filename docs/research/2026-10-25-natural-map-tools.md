# 자연스러운 맵 제작 — 가져다 쓸 도구·기법 조사 (D-132)

> PD 과제 20261025-0834 (08:02 과제 재시작). **절마다 바로 저장**하며 진행 — 아래 §마다 "작성됨/예정" 표시.
> 증거 라벨: [확인]=원문·API·리포 직접 열어 봄 / [2차]=검색 요약·인덱스 / [측정]=이 리포 파일 / [추정] / [미확인].
> 라이선스 열 기준: **상업 Steam 출시 가능 여부**. "도구 라이선스"와 "도구가 만든 결과물의 권리"는 다르다 — 따로 적는다.

| 절 | 내용 | 상태 |
|---|---|---|
| §1 | 길·지형 도구 (Splines·오픈소스 road 도구·Terrain vs 메시) | 작성됨 |
| §2 | 지형 생성 (노이즈·침식·Gaea·World Machine·A.N.T.·DEM 라이선스) | 작성됨 |
| §3 | 길에 붙은 마을 배치 (파셀·옹벽·WFC·Townscaper) | 작성됨 |
| §4 | 퀄리티 도구 (픽셀 셰이더·디칼·산포·CC0·EDC 1차 자료) | 작성됨 |
| §5 | 레퍼런스 운전 게임의 제작 방식 | 작성됨 (1차 자료 거의 없음) |
| §6 | 종합표 + 추천 1안/대안 + 넣지 말 것 + 규칙 제안 | 작성됨 |
| §7 | 모르는 것 / 미확인 | 작성됨 |

---

## §1 길·지형 도구

### 1-1. 이 리포에 이미 있는 것 (새로 받을 필요 없음) [측정]

| 이미 있음 | 무엇 | 위치 | 비고 |
|---|---|---|---|
| `roadgen` (MIT) | 도로망 + 정착지 생성 라이브러리. Editor 전용 어셈블리 | `F1/Assets/Editor/ThirdParty/roadgen/SOURCE.md:1-8` | 커밋 f36001e(2024-03-02), 패치 2건. **도로망(교차로 그래프) 쪽이지 "비탈에 길을 얹는" 쪽이 아님** [추정: 파일명 RoadNetworkGenerator·RoadDensityBasedSettlementSpawner 기준, 내부는 안 읽음] |
| `SplineTown.CutFill` | 지형 격자(`TerrainGrid`)를 길 단면에 맞춰 **깎고 메우고, 높이차가 임계 넘으면 옹벽 생성**. 길 밑 −0.10 m, 어깨 6 m, 블렌드 6 m 같은 값이 코드에 있음 | `F1/Assets/Editor/TownGen/Core/SplineTown.cs:1975-1999` | 엔진 비의존(`IHeightField` 입력). D-132 의 "지형 먼저 → 길 얹기 → 옹벽" 순서의 **길 얹기·옹벽 단계가 이미 코드로 있다** |
| `TownGenSpline.TerrainMesh` | `TerrainGrid` → 메시 | `F1/Assets/Editor/TownGen/TownGenSpline.cs:572` | 지형을 Unity Terrain 이 아니라 **메시**로 만든다 |
| `WorldSurface.shader` | 공용 월드 셰이더 — `_MainTex`(픽셀 아틀라스), `_Ambient`, `_FogColor`, `_FogStart/_FogEnd` | `F1/Assets/Art/CoastalTown/WorldSurface.shader:3-32` | 규칙 11(조명·안개 한 규칙)이 **이 셰이더 속성**에 걸려 있다 (아래 1-3) |

⚠ **D-117 로 닫힌 것은 "마을 생성기의 정밀도 작업"이다.** 위 `CutFill`/`TerrainMesh` 를 **200 m 구간에 재사용**하는 건 정밀도 재개가 아니라 다른 문제지만, TownGen 전체를 다시 돌리자는 뜻은 아니다 — 필요하면 **함수 단위로 복사해 쓰는 것**만 본다. (재사용 여부는 PD/도현 판단. 이 보고서는 "있다"는 사실만 적는다.)

### 1-2. 스플라인 길 도구

| 도구/기법 | 무엇에 쓰나(우리 구간) | 라이선스 (Steam 상업) | Unity 6 호환 / 유지 | 도입 비용 | 출처 |
|---|---|---|---|---|---|
| **Unity Splines** `com.unity.splines` (현재 매니페스트에 **없음**) | 오르막 길 중심선(제어점)을 씬에서 손으로 찍고 편집. 샘플에 "도로 만들기·곡선 따라 프리팹 배치" 씬 포함 | **Unity Companion License** — "Unity 의존 프로젝트용"으로 명시. 상업 조항은 페이지에 없고 링크된 본문 미열람 → **Unity 안에서 Unity 게임에 쓰는 한 문제 없을 가능성 높음 [추정], 원문 확인 [미확인]** | "Unity 2022.3 이상 지원"(문서) [확인]. 최신 2.8.4 (2026-03-10) [2차]. **6000.3.19f1 에서 실제 설치·컴파일은 미확인** (이 리포 PackageCache 에 없음) | 설치 10분 + 중심선 곡선 → 기존 `CutFill` 입력(`Vec2[]`)으로 샘플링하는 어댑터 반나절 [추정] | [매뉴얼 index](https://docs.unity3d.com/Packages/com.unity.splines@2.8/manual/index.html) · [라이선스](https://docs.unity3d.com/Packages/com.unity.splines@2.8/license/LICENSE.html) |
| Unity Splines — **Spline Extrude** | 스플라인을 따라 메시 압출. **문서 설명은 관·전선·밧줄 같은 원형 단면 위주**(2.7 문서: 원형 단면 + 면수·캡·노멀 뒤집기). 평평한 길/연석 단면을 직접 못 넣는 것으로 보임 [추정 — 사용자 정의 단면 지원 여부 미확인] | 위와 같음 | 위와 같음 | 길 단면용으론 **쓰지 말고** 기존 길 단면 코드(`SplineTown` 단면) 사용 권장 | [Extrude 컴포넌트 2.7](https://docs.unity3d.com/Packages/com.unity.splines@2.7/manual/extrude-component.html) |
| Unity Splines — **Spline Instantiate** | 길을 따라 가로등·울타리 기둥·돌담 조각 같은 반복물 배치. 방식: Instance Count / Spline Distance / Linear Distance, 아이템별 확률, 정렬(Spline Element / Spline Object / World Space) | 위와 같음 | 2.4 문서 기준 [확인]; 2.8 의 속성 이름은 다를 수 있음 | 낮음 (컴포넌트 붙이기) | [Instantiate 2.4](https://docs.unity3d.com/Packages/com.unity.splines@2.4/manual/instantiate-component.html) |
| **SplineMesh** (methusalah) | 베지어 곡선을 따라 메시 변형. 유료 버전이 Asset Store 에도 있음(내용 동일) | **MIT** [확인: GitHub API spdx=MIT] | **마지막 push 2023-07-21**, 아카이브 아님, 이슈 28. Unity 버전 표기 없음 → **Unity 6 미확인**. Splines 패키지와 기능 겹침 | 중간 — 겹치므로 **굳이 안 받음** | [GitHub](https://github.com/methusalah/SplineMesh) · [API](https://api.github.com/repos/methusalah/SplineMesh) |
| **Path-Creator** (Sebastian Lague) | 베지어 경로 편집기 + `RoadMeshCreator` 예제 | **MIT** [확인: API] | **마지막 push 2024-05-02** (☆2,099), 아카이브 아님. Unity 6 미확인 | 낮지만 Splines 와 겹침 | [GitHub](https://github.com/SebLague/Path-Creator) · [API](https://api.github.com/repos/SebLague/Path-Creator) |
| MarcusElg "Road Creator" | 무료 도로 에디터 | 인덱스에는 MIT·아카이브로 나왔으나 [2차], **내가 시도한 저장소 이름(`MarcusElg/Road-Creator`)은 404** → 이름 확인 못 함 | **미확인** | — | 이 항목은 근거 부족 — **추천 대상에서 제외** |
| EasyRoads 무료판 | — | 유료 Asset Store 계열. 이 조사에서 라이선스·호환 직접 확인 안 함 | **미확인** | — | 참고만 (과제 규정상 추천 제외) |

### 1-3. Terrain(Unity 지형 오브젝트) vs 메시 지형 — 960×540 Point 업스케일 + 64 px/m 팔레트 룩에 무엇이 맞나

판단 근거는 **이 리포의 코드 사실 2개 + 공개 자료 1개**다. 실제 캡처 시험은 안 했다 → 결론은 [추정].

| 비교 | Unity Terrain | 메시 지형 (TownGen 방식) |
|---|---|---|
| **규칙 11(안개·주변광 한 규칙) 연결** | `MoodController.SyncWorldMaterials` 는 `_Ambient`·`_FogColor`·`_FogStart`·`_FogEnd` **네 속성이 다 있는 머티리얼만** 갱신한다 (`MoodController.cs:88-91`). Terrain 은 자체 지형 셰이더(TerrainLit 계열)를 쓰므로 **이 네 속성이 없어 무드 전환(D-125 갈 때/올 때)에서 빠진다** [측정+추정] | `WorldSurface.shader` 하나를 그대로 씀 → 무드·안개·팔레트 일치 [측정] |
| **픽셀 질감(64 px/m, 공용 팔레트)** | 질감을 "레이어 4장씩 + 컨트롤맵"으로 섞는 구조. 팔레트 한 장으로 고정하려면 지형 셰이더를 따로 만들어야 함 [추정] | 월드 셰이더가 이미 `_MainTex` 아틀라스 + 타일 UV 로 처리 [측정] |
| **절벽·옹벽·계단 단차** | 높이맵은 **격자 하나의 높이 함수**라 수직 절벽·오버행이 안 된다(경사만). 옹벽·단차는 어차피 별도 메시 [추정] | 메시라 수직면·옹벽 자유 |
| 높이맵 해상도 | 33·65·129…4097 으로 고정. 해상도를 낮춰도 **로우폴리 각진 룩은 자동으로 안 나온다** (포럼 답변: 별도 메싱 필요) | 격자 간격을 마음대로 (TownGen 은 2 m) |
| 공개된 대안 | 로우폴리 전용 Terrain 대체 에셋(유료 PolyTerrain)·평면 셰이딩 지형 셰이더 포럼 글·오픈소스 DudeiTerrain 존재 → "Terrain 을 그대로 쓰면 이 룩이 안 나온다"는 방증 | — |
| Terrain 의 장점 | LOD·지형 도구(Terrain Tools)·충돌이 기본 제공 | 직접 콜라이더(MeshCollider) 구성 필요 — 200 m 구간 규모면 문제 없을 것으로 보임 [추정] |

**결론(추정)**: 이 프로젝트는 **메시 지형**이 맞다 — 이유는 취향이 아니라 (1) 규칙 11 이 셰이더 속성에 걸려 있고 (2) 수직 단차를 표현해야 하고 (3) 이미 메시 지형 코드가 있어서다. 확인된 반대 근거 없음. 단 **실제 캡처 비교는 안 함 [미확인]**.

출처: [Terrain.heightmapMaximumLOD/TerrainData.heightmapResolution 문서 (6000.3)](https://docs.unity3d.com/ScriptReference/TerrainData-heightmapResolution.html) · [포럼: 블록/로우폴리 지형](https://discussions.unity.com/t/getting-the-low-poly-blocky-look-in-untiy-terrain-engine/420598) · [PolyTerrain(유료, 참고)](https://assetstore.unity.com/packages/tools/terrain/polyterrain-blocky-low-poly-terrain-styles-219992) · [DudeiTerrain](https://github.com/matdudq/DudeiTerrain)

### 1-4. "Terrain 높이를 스플라인에 맞춰 깎는" 기존 도구

- 오픈소스(MIT 등)로 **"스플라인 따라 Terrain 깎기"**를 하는 저장소는 이번 검색에서 못 찾았다 [2차: 08:03 이전 검색]. Terrain Former 는 상용, Unity Terrain Tools 에 평탄화·브리지 브러시는 있음 [2차].
- 어차피 §1-3 결론이 메시 지형이므로 **필요 없다** — 이 리포의 `CutFill` 이 같은 일을 한다.

---

## §2 지형 생성 (절벽·계단식 비탈·실제 고도 참고)

### 2-1. 이미 확인돼 있는 것 (이 리포 문서 — 다시 조사하지 않음)

| 사실 | 위치 | 상태 |
|---|---|---|
| Copernicus DEM GLO-30 은 무료·전 세계·기한 없음, **가공·결합 허용(Art.4), 상업 사용 제한 조항 없음**. 가공본에는 6(b) 문구, 배포 시 6(c) 면책 문장을 크레딧에 | `tools/town-gen/dem/ATTRIBUTION.md:3-22` | [확인] 라이선스 PDF 원문 대조 2026-10-19 |
| 포지타노·아말피를 덮는 타일 `Copernicus_DSM_COG_10_N40_00_E014_00_DEM.tif` 가 이미 캐시돼 있고 3단 양자화 heightMask 로 쓴 적 있음 | `tools/town-gen/dem/ATTRIBUTION.md:3,13` | [확인] |
| 지형은 `IHeightField`(함수) 입력 → `TerrainGrid` → 메시. 즉 **높이를 돌려주는 함수만 바꾸면** 길 얹기·옹벽이 따라온다 | `SplineTown.cs:1982-1989` | [측정] |

### 2-2. 도구/기법 표

| 도구/기법 | 무엇에 쓰나 (우리 구간) | 라이선스 (Steam 상업) | 유지·호환 | 도입 비용 | 출처 |
|---|---|---|---|---|---|
| **손으로 쓴 높이 함수** (`IHeightField` 구현: 제어점 몇 개로 언덕·절벽 + 계단식 단차 + 약한 노이즈) | 200 m 오르막 → 꼭대기 → 바다 쪽 절벽. 높이차를 사람이 직접 "연출"한다 | 자체 코드 — 문제 없음 | 순수 C#, Unity 버전 무관 | 0.5~1일 [추정] | 이 리포 `SplineTown.cs:1982` (함수 입력 구조) |
| **Copernicus DEM → "참고 실루엣"** | 포지타노 비탈의 전체 고저차·경사 방향·바다까지 거리를 **숫자 몇 개로** 읽어 위 함수의 제어점에 옮긴다. 30 m 해상도라 200 m 구간이면 칸이 약 7개 [계산] → 모양 복제는 불가능, 실루엣 용도로만 | **상업 가능**, 크레딧 문구 필수 (위 2-1) | 데이터라 호환 문제 없음. 교토는 **새 타일 필요**: 타일 이름 규칙(남서 모서리) 상 청수사·산넨자카(약 N34.99°, E135.78°)는 `N34_00_E135_00`, 북쪽 언덕은 `N35` 타일에 걸릴 수 있음 [계산, 미내려받음] | 이미 캐시 있음(포지타노). 교토 타일 내려받기 + 단면 뽑기 반나절 [추정] | [ATTRIBUTION.md](../../tools/town-gen/dem/ATTRIBUTION.md) · [라이선스 요약](https://documentation.dataspace.copernicus.eu/APIs/SentinelHub/Data/DEM/resources/license/License-COPDEM-30.pdf) |
| 국토지리원(GSI) 표고 타일 (일본) | 교토를 더 촘촘히 참고하고 싶을 때 | 국토지리원 콘텐츠 이용규약(PDL1.0 준거): 출처 명기, **가공했다는 표기 별도 필요**. 다만 "측량법상 승인이 불필요한지"는 **확인 못 함** [2차] | — | — | [GSI 규약 안내(검색 결과)](https://www.gsi.go.jp/kikakuchousei/kikakuchousei40182.html) · 결론: **교토도 Copernicus 로 통일**(이미 확인된 라이선스) |
| **Blender A.N.T. Landscape** | 노이즈 지형 + 침식(Landscape Eroder) + Displace 의 Strata(계단 모양 추정, 설명 못 찾음)를 Blender 안에서 | 애드온은 **GPL**(Blender 번들). 하지만 "Blender 로 만든 것은 만든 사람 소유"라고 Blender 가 명시 — **만든 메시를 게임에 넣는 건 문제 없음**. GPL 은 *스크립트를 공개 배포할 때* 따라붙는 의무 | Blender 번들이라 유지됨. 높이맵 export 기능은 문서에서 못 찾음 [미확인] | 반나절 + 메시를 Unity 로 가져와 `CutFill` 격자와 합치는 연결 1일 [추정] | [매뉴얼](https://docs.blender.org/manual/pt/4.1/addons/add_mesh/ant_landscape.html) · [Blender 라이선스](https://www.blender.org/about/license/) |
| **Gaea** (QuadSpinner) | 침식·절벽이 잘 나오는 지형 도구 | **Community 는 평가·비상업용** → Steam 출시용 지형엔 못 씀. **Indie $99(연 매출/자본 $100,000 이하)** 유료. Community 해상도는 출처끼리 불일치(생성 4096/내보내기 1024 vs 1024) | 현행 | 유료 → **참고만**(과제 규정) | [Download](https://www.quadspinner.com/Download) · [Order/FAQ](https://quadspinner.com/Order/) · [Gaea2 라이선스 PDF](https://www.quadspinner.com/PDF/Gaea2.pdf) |
| **World Machine Basic** | 같음 | **비상업 전용**, 출력 1025×1025 상한. 정의상 "게임 풍경 등 돈이 되는 모든 사용"이 상업 | 현행 | 유료 → **참고만** | [World Machine 다운로드](https://world-machine.com/download.php) |

### 2-3. 판단 (추정 — 시험 안 함)

1. **이번 구간 규모(200 m)에 침식 도구는 과하다.** 화면은 960×540·월드 텍셀 32 px/m 라 침식 골짜기의 잔주름은 한 픽셀 안에 들어간다 [추정]. 읽히는 건 **큰 덩어리(오르막 기울기, 꼭대기 평지, 바다 쪽 낭떠러지, 단차)**뿐이다.
2. 그래서 "지형 먼저"는 **높이 함수에 제어점 5~8개**면 시작된다. 계단식 단차는 높이를 일정 간격으로 양자화하고 사이를 부드럽게 잇는 함수로 만든다 [일반 기법, 출처 못 찾음 → 가설].
3. 실제 고도 데이터의 값어치는 **"포지타노는 몇 m 에 몇 m 를 오르나"** 같은 비례 근거다 — 그 값을 쓰는 순간 **크레딧 문구 의무**가 생긴다고 보고 6(b) 문구를 넣어 둔다(비용 0, 안전).

---

## §3 길에 붙은 마을 배치 (비탈 집·상점가·"복도" 문제)

### 3-1. "길과 평행한 한 줄 건물(복도)"은 배치 좌표계가 길 기준이라서다 [측정]

**(A) 지금 코스(BuildPostcardSlice) — 건물 좌표가 `(길 진행거리 s, 길 옆 거리 d)`**

| 코드 사실 | 위치 | 뜻 |
|---|---|---|
| 해안 구간 집: `s += 26f` 마다 `Prefab(bld, HouseName(s), s, 11f, -.3f, …)` — **옆 거리 11 m 고정, 높이 −0.3 고정**, 발판 `Pad(…)` 도 같은 값 | `BuildPostcardSlice.cs:277-282` | 지형 높이를 읽지 않고 **길에서 같은 거리·같은 높이**에 놓는다 → 길과 평행한 줄 |
| 나무: 14 m 마다 좌우 번갈아 `side * 7.5f` | `BuildPostcardSlice.cs:296-300` | 같은 이유(규칙 격자) |
| 길가 필지 띠 `Lot_L/R` = 길 반폭 바깥으로 폭 12 m 평평한 띠, `Strip(...)` | `BuildPostcardSlice.cs:194-195` | 평평한 띠 |

→ D-132 의 "평행한 한 줄 + 평평한 띠"는 **좌표계 문제**다. 건물을 `(x,z)` 로 놓고 높이를 `terrain.HeightAt(x,z)` 로 읽는 순서로 뒤집어야 한다 [추정 — 구현 방법은 PD/프로그래머 몫].

**(B) 예전 TownGen 의 필지 코드 (재사용 후보 — 함수 단위 복사만 고려)**

| 코드 사실 | 위치 | 뜻 |
|---|---|---|
| 집 필지 정면 = 길, 뒤로 `houseDepthMeters = 7` | `SplineTown.cs:164-165` | 길 한쪽 줄에 깊이 7 m 한 겹 |
| `setbackMeters = 0.5f` **고정값**, 큰길 집 앞 간격 `houseFrontGapMeters = 1.0` | `SplineTown.cs:166-168` | 후퇴(setback)가 필지마다 같다 → 건물 앞면이 한 직선 |
| 모델을 먼저 고르고 **필지 폭 = 모델 폭 + 틈(0~2 m)** → "붙은 줄집" | `SplineTown.cs:175`, `TownGenSpline.cs:60` | 틈이 거의 없는 연속 외벽 = 복도 |
| 기단 높이 상한 `plinthMaxMeters = 2.5`, 넘으면 "한 단 뒤로 물림" | `SplineTown.cs:169-170` | 비탈 대응은 있으나 **길과 평행하게 한 단 뒤**일 뿐 |
| 밀도 = 10×10 m 필지마다 확률 p = 목표밀도 × f_경사 × f_바다 × f_길 (경사 ≤15 % →1, >35 % →0) | `TownGenStructure.cs:1083-1086` | 이 밀도 지도는 **OSM 흩뿌림용(TownGenStructure)** 이고 `SplineTown` 필지와 다른 경로다. >35 % 경사에 집을 안 두는 규칙이라 그대로 쓰면 포지타노식 "경사에 쌓인 집"이 안 나온다. 어느 경로가 어디까지 쓰이는지는 **전부 읽지 못함 [미확인]** |

→ TownGen 쪽도 **후퇴 고정·틈 0~2 m 의 "붙은 줄집"** 이라 그대로 가져오면 (A)와 비슷한 줄이 나온다 [추정]. 장점은 *지형 높이를 읽어 기단을 만든다*(`plinthY`, `terrainMin/Max`)는 점 하나. (D-117 이 닫은 건 마을 생성기 *정밀도 작업*. 이번 건 새 구간의 배치 규칙이라 따로 본다 — 재개 여부는 PD/도현 판단.)

### 3-2. 도구/문헌 표

| 도구/기법 | 무엇에 쓰나 | 라이선스 (Steam 상업) | 유지·호환 | 도입 비용 | 출처 |
|---|---|---|---|---|---|
| **Wave Function Collapse + Marching Cubes (Townscaper 방식)** | 불규칙 사각 격자 위에 모서리 조각을 WFC 로 맞춰 집을 짓는다. 지붕·정원 조각엔 우선순위 | 알고리즘(특허 언급 못 찾음 [미확인]) | 개발자 본인 글: **Townscaper 구현에 약 3개월, Bad North 생성 시스템은 약 2년** [확인]. 높이·지형 처리는 글에 설명 없음 | **11일 안에 새로 만들기 불가** [계산: 11일 < 3개월] | [Stålberg 글(Game Developer)](https://www.gamedeveloper.com/game-platforms/how-townscaper-works-a-story-four-games-in-the-making) |
| mxgmn/WaveFunctionCollapse | WFC 원본(C#, 비트맵·타일맵) | **MIT** [확인: LICENSE 본문 1~3행, "샘플 이미지·타일은 소프트웨어가 아님"] | 평면 타일 용도, 3D 비탈과 거리 | — | [GitHub](https://github.com/mxgmn/WaveFunctionCollapse) · [LICENSE](https://raw.githubusercontent.com/mxgmn/WaveFunctionCollapse/master/LICENSE) |
| marian42/wavefunctioncollapse | Unity 프로젝트: 블록 모듈 + WFC(백트래킹)로 무한 도시. `ModulePrototype` 컴포넌트로 모듈·이웃 규칙을 씬에서 작성 | **MIT** [확인: API] | ★4,867, **마지막 push 2021-03-28** → Unity 6 호환 **미확인**. 모듈 세트를 새로 만들어야 함(우리 건물 프리팹은 커넥터 없음) | 모듈 작성만 며칠 [추정] → **이번 주 부적합** | [GitHub](https://github.com/marian42/wavefunctioncollapse) |
| BorisTheBrave/DeBroglie | 제약 WFC 라이브러리(C#): 연결 보장·고리 제약 등 | **MIT** [확인: API] | ★528, push 2025-12-21 → 유지 중. 격자에 묶임 | 규칙 설계가 곧 공수 | [GitHub](https://github.com/BorisTheBrave/DeBroglie) · 지난 조사 `옵시디언/research/city-generator-oss-2026-10-19.md` ⑥ |
| **Emilien 외 2012, "Procedural generation of villages on arbitrary terrains"** | 지형 위 마을: ①정착 씨앗 둘레로 **이방성 정복(anisotropic conquest)** 으로 필지 분할 ②**경사에 적응하는 열린 모양 문법**으로 3D 형태. 우리 문제와 정확히 같은 문제 | 논문(코드 못 찾음) | 2012 Visual Computer | 구현하면 수 주 [추정] → **아이디어만** | [HAL 초록](https://hal.archives-ouvertes.fr/hal-00694525) · [Springer](https://link.springer.com/article/10.1007%2Fs00371-012-0699-7) |
| FritzsHero/RoadArchitect | 스플라인 도로 + 절차 교차로 | **MIT** [확인: API] | ★365, **push 2026-10-08** → 유지 중("ongoing but slow"). 코드 API 문서 없음 [미확인], Unity 6 미확인 | 지난 조사의 결론: 통째로 넣지 말고 **교차로 메시 코드만 읽고 참고** | [GitHub](https://github.com/FritzsHero/RoadArchitect) |
| lazysquirrellabs **Terraced Terrain Generator (TTG)** | "계단식 지형 메시 절차 생성" Unity 도구 | **MIT** [확인: API] | ★72, push 2026-04-26. Unity 6 미확인. 설명만 읽음 — **계단식 대지 모양이 포지타노형인지 못 봄** | 설치·시험 반나절 [추정] | [GitHub](https://github.com/lazysquirrellabs/TTG) |
| Kenney Modular Buildings / Quaternius Medieval Village | 벽·창·지붕 조각을 조합해 집 변형 / 돌벽·계단·아치 조각 | **CC0** (지난 조사 [페이지] — 내용물 미열람) | 에셋이라 호환 문제 없음 | 임포트+조합 규칙 1~1.5일 [지난 조사의 가설] | `옵시디언/research/oss-assets-2026-10-20.md` ①③ |
| Morro (Blender 파벨라 생성기) | 비탈을 따라 쌓인 집 + 아래쪽 옹벽을 닫는 생성 | **상용 마켓 상품** → 참고만 | — | — | [Superhive](https://superhivemarket.com/products/morro-procedural-favela-generator) |
| Seele Scatter Regions | 마을·농장 산포 (UE5.5 플러그인) | MIT | **Unreal 전용 → 제외** | — | 검색 결과 요약만 |
| CityEngine (Esri) | 규칙 기반 도시 | 상용 — 이 조사에서 라이선스 직접 확인 안 함 [미확인] | — | — | **제외**(과제 규정: 유료는 참고만) |

### 3-3. 판단 (추정 — 사람이 돌려보기 전)

1. **"가져다 쓸 마을 배치 오픈소스"는 11일 안에 없다.** WFC 계열은 구현 기간이 개월 단위(Stålberg 본인 수치)이고, 비탈 마을을 직접 다루는 코드는 논문뿐이며, 상용 Blender 도구(Morro)는 참고만 된다.
2. **200 m 구간의 건물 수는 손으로 다룰 크기다.** 양쪽 길가 필지를 8 m 폭으로 잡으면 최대 약 50개, 절반이 비면 약 25채 [계산: 200 m×2변÷8 m]. 그래서 "생성기 도입"보다 **건물을 `(x,z)` 로 놓고 지형 높이를 읽는 작은 배치 코드(규칙 4개)를 새로 짜고 마지막에 손으로 다듬는 편**이 싸다 [추정] — TownGen 필지 코드를 통째로 돌리는 것도, WFC 를 도입하는 것도 아니다.
3. 바꿀 규칙 후보(전부 **가설** — 실제 캡처로 검증 필요): (a) 후퇴를 필지마다 0~3 m 로 흔들기 (b) 필지 사이 틈·골목·계단을 4~6채마다 끼우기 (c) 경사 컷(>35 % → 0)을 풀고 **뒤쪽 줄을 지형에 얹어 한 단 위로**(길보다 높은 두 번째 줄) (d) 집의 방향을 길이 아니라 **등고선**에 가깝게 ±10° 틀기. (c)가 "비탈에 쌓인 집"의 핵심이다.
4. 교토 비탈 상점가(산넨자카형)는 **반대로 틈이 거의 없는 외벽이 맞다**(길에 바로 붙은 상점가). 같은 코드에서 후퇴 0·틈 0 으로 두되 **길바닥이 계단·경사 단차로 오르내리고, 처마·간판·소품 높이 밀도**가 복도감을 깬다 [추정 — 산넨자카 현장 근거는 이 조사에서 직접 확인 안 함].

---

## §4 퀄리티를 올리는 무료 도구

### 4-1. 먼저, 이미 있는 것 [측정]

`WorldSurface.shader`(`F1/Assets/Art/CoastalTown/WorldSurface.shader:3-62`)는 이미 (a) 월드 좌표 기준 타일 UV(`_WorldScale` 0.5 = 1 m 에 0.5 번 반복), (b) 화면을 단색 한 규칙으로 묶는 `_Ambient`+`_FogColor` 선형 안개, (c) 두 번째 칸을 **큰 덩어리 무늬로 섞는 `_Patch`**(값 노이즈를 `step` 으로 잘라 "계단식 경계 = 픽셀아트 결", `:53-58`)를 한다. 즉 **픽셀 셰이더·안개·큰 얼룩은 새로 받을 필요가 없다.** 반대로 이 셰이더는 **정점색(COLOR)을 입력으로 받지 않는다**(`Attributes` 에 position·normal·uv 뿐, `:38`) — 마모 마스크를 메시에 굽는 길은 셰이더 3~5줄 추가가 선행이다 [추정: 줄 수].

### 4-2. 표

| 도구/기법 | 무엇에 쓰나 (우리 구간) | 라이선스 (Steam 상업) | 유지·호환 | 도입 비용 | 출처 |
|---|---|---|---|---|---|
| **외부 PS1/픽셀 셰이더** — PSX Shader4Unity(Kelvym Campos, 오픈소스로 소개), Daniel Ilett Retro Shaders Pro(유료) | 정점 흔들림·어파인 텍스처 | PSX Shader4Unity **라이선스 못 확인** [미확인]. Ilett 은 유료 → 참고만 | PSX Shader4Unity 는 **Unity 2022.3.18f1 URP 에서만 시험**, 개발 중 표기 | **도입 비용 > 이득**: 위 4-1 의 자체 셰이더가 있고 브리프 규칙 3(960×540 Point, 세계 32 px/m)·규칙 11(안개·주변광 한 규칙)이 이미 룩을 정한다 [추정] → **넣지 않음** | [PSX Shader4Unity](https://kelvymcampos.itch.io/psx-shader4unity) · [Retro Shaders Pro](https://danielilett.itch.io/psx-shaders-pro-for-unity-urp) |
| **URP Decal Projector** (Unity 내장) | 벽·길의 세월 흔적(D-128): 얼룩·금·타이어 자국을 면 위에 투영 | 내장 기능(Unity 6 URP 문서) | 문서 [확인]: Decal Shader Graph 머티리얼 필요 · **투명면엔 안 먹음** · **SRP Batcher 미지원**(GPU 인스턴싱 또는 아틀라스 권장). 우리 `WorldSurface`(`SRPDefaultUnlit` 패스, `:22`)에 DBuffer 모드가 먹는지는 **미확인** — 스크린스페이스 모드 시험 필요 | 시험 반나절 [추정] | [URP Decal 문서 (6000.0)](http://docs.unity3d.com/6000.0/Documentation/Manual/urp/renderer-feature-decal.html) |
| **얇은 쿼드 디칼**(자체 기법) | 같은 아틀라스 칸을 쓴 납작한 메시 조각을 면에서 1~2 cm 띄워 붙임. 셰이더 호환 걱정 없음 | 자체 | 없음 | 낮음 [추정] | 이 리포 `WorldSurface`(`_Tile`, `:5`) |
| **정점색 마모 마스크** — 직접 굽기(Editor 스크립트: 높이·경사·법선으로 계산) | 비탈 아래쪽 얼룩·벽 밑 이끼 같은 연속 마모. 손으로 칠하지 않음 | 자체 | 셰이더에 COLOR 입력 추가 필요(4-1) | 셰이더 수정 + 굽기 스크립트 약 1일 [추정] | — |
| Dillo-GameDev/Unity-VertexPaintTool | 에디터에서 손으로 정점색 칠하기 | **MIT**(검색 결과 표기, LICENSE 본문 미열람) | 대상 Unity 2022.3.59 — URP 셰이더 호환 **미확인**, 우리 셰이더에 `_VertexColorArray` 연결 필요 | 이식 반나절+ | [GitHub](https://github.com/Dillo-GameDev/Unity-VertexPaintTool) |
| **소품 산포** — `SplineInstantiate`(길 따라) / Poisson-disk(면적) | 가로등·돌담 기둥·나무·화분을 길을 따라, 숲·수풀은 면적으로 | Splines 는 Unity Companion License(§1-2). Poisson-disk 는 이 리포 `TownGenStructure.cs:1235` 에 이미 **시드 고정 구현**이 있음 [측정] | Splines 설치 시 | 낮음 | [Spline Instantiate 2.4](https://docs.unity3d.com/Packages/com.unity.splines@2.4/manual/instantiate-component.html) |
| **CC0 소스** (Poly Haven·ambientCG 외) | 조각 에셋·소품 | **Kenney = CC0, 상업 가능, 표기 불필요**(출처: Kenney 지원 페이지) [확인]. Quaternius·KayKit = CC0(지난 조사 [페이지], 이번에 재확인 안 함) | 에셋 | 미리보기 확인 후 | [Kenney 지원](https://kenney.nl/support) · `옵시디언/research/oss-assets-2026-10-20.md` |
| **Easy Delivery Co 의 제작 방식** | — | — | **지도·도로 설계에 대한 1차 자료를 못 찾음.** 80.lv 개발 기사는 도로·안개·경계 언급이 없다(지난 조사 [확인]). 검색에서 나온 건 개발자 Sam C.·Unity·"원래 로컬 분할화면 레이싱으로 시작"뿐(Wikipedia [2차]) | — | `docs/research/2026-10-23-speed-and-scale.md:7-8,107` · [Wikipedia](https://en.wikipedia.org/wiki/Easy_Delivery_Co.) |

### 4-3. 판단 (추정)

1. **"퀄리티를 올리는 무료 도구"의 답은 대부분 "이미 있다"**: 픽셀 룩(셰이더+Point), 안개(월드 셰이더+MoodController), 큰 얼룩(`_Patch`). 새로 필요한 건 **정점색 입력 하나**와 **디칼 시험 하나**다.
2. 디칼은 D-128 마모 용도로 **두 갈래 중 싼 쪽부터**: 얇은 쿼드(호환 위험 0) → 부족하면 Decal Projector 시험.
3. 소품 산포는 `SplineInstantiate`(길 따라)와 기존 Poisson-disk(면적) 두 개면 이번 구간에 충분하다.

---

## §5 레퍼런스 운전 게임의 제작 방식 (1차 자료 있을 때만)

**결론: 이 항목은 "미확인"이다 — 운전 게임 중 길 제작 방식의 1차 자료를 못 찾았다.**

| 게임 | 찾은 것 | 쓸 수 있나 |
|---|---|---|
| Art of Rally (Funselektor, 저해상 룩) | 인터뷰(Sausage Factory 팟캐스트·Traxion.GG·Jalopnik 카메라 글)가 있다는 것까지만. **트랙·지형을 어떻게 만들었는지 본문은 못 읽음** [미확인] | ✗ |
| Easy Delivery Co | §4-2 마지막 행 — 도로 설계 언급 없음 | ✗ |
| Desolate Drive (OursBleu, 2020 devlog "Making of the roads") | 도로를 **가상 텍스처 → 스플라인 메시 → 다시 가상 텍스처**로 오갔고, 교차로에 공을 들였고, **젖음(wetness) 효과를 막판에 넣자 사실적으로 보였다**. 이유·지형 접합 방법은 글에 없음 [확인] | 약함 — "길 표면 처리가 룩을 결정한다"는 정도 |
| A Short Hike (비운전, 같은 계열 저해상 3D) | "가능한 적은 픽셀로 아름답고 **읽히는** 3D 세계", **평면 통일 셰이딩 · 안티앨리어싱 없음 · 읽기 위한 부드러운 외곽선**, 그리고 "플레이어가 뻔한 길의 정반대로 가도 되게 설계" [확인: PlayStation Blog, 본인 글]. 지형을 어떻게 깎았는지는 글에 없음 | 룩 규칙의 방증(우리 규칙 7·11 과 같은 방향) |
| Unity 포럼 2025-07-09 요청 글 | Spline 패키지로는 **지형과 함께 가는 도로를 "기본 제공"으로는 못 만든다**, 지형을 스플라인 따라 변형하려면 커스텀 스크립트가 필요하다는 서술 [확인: 요청 글 — 공식 문서 아님] | 우리가 `CutFill` 을 쓰는 이유의 방증 |

출처: [Art of Rally (Wikipedia)](https://en.wikipedia.org/wiki/Art_of_Rally) · [Desolate Drive devlog](https://itch.io/devlog/203883/making-of-the-roads.amp) · [A Short Hike — PlayStation Blog](https://blog.playstation.com/2021/08/05/crafting-a-tiny-open-world-a-look-behind-the-scenes-at-the-creation-of-a-short-hike/) · [GDC "Crafting A Tiny Open World"(로그인 필요, 미열람)](https://gdcvault.com/play/1026613/Independent-Games-Summit-Crafting-A) · [Unity 포럼 요청 글](https://discussions.unity.com/t/road-system-for-unity-based-on-the-current-spline-package-and-the-terrain-system/1664437)

---

## §6 종합 — "언덕 위 도착 구간 200 m 를 이번 주에 만든다면"

### 6-0. 먼저 알아둘 것 (PD 전제 확인)

| 항목 | PD 과제 문구 | 이 리포에서 확인된 것 |
|---|---|---|
| 텍셀 밀도 | "텍셀 64px/m" | 브리프 규칙 3: **세계 32 px/m, 화물만 64 px/m**(D-124) — `art/_briefs/2026-10-23-art-direction-v2.md:35` [측정]. 지형·건물 텍스처 기준은 32 |
| Unity Splines 설치 | "패키지 확인" | 매니페스트에 **없음**. 그러나 **6000.3.19f1 에디터가 `com.unity.splines-2.9.0.tgz` 를 번들**로 갖고 있다 — `C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Data\Resources\PackageManager\Editor\com.unity.splines-2.9.0.tgz` [확인: 파일 존재]. 이 에디터와 맞춰 나온 버전이라는 강한 정황이지만 **설치·컴파일은 안 해봄** [미확인]. (앞서 검색 요약의 "최신 2.8.4"는 낡은 정보였다) |
| 현재 코스의 건물 배치 | "고정 반경 굽이·평평한 띠·평행한 한 줄" | 건물은 `(길 거리 s, 옆 거리 d=11 m 고정, y=−0.3 고정)` 로 놓인다 — `BuildPostcardSlice.cs:277-282` [측정] |

### 6-1. 종합표 (채택 / 참고 / 제외)

| 도구/기법 | 무엇에 쓰나 (우리 구간) | 라이선스 (Steam 상업) | Unity 6 호환·유지 | 도입 비용 | 판정 | 출처 |
|---|---|---|---|---|---|---|
| Unity Splines (`SplineContainer`) | 오르막·갈림길 중심선을 씬에서 제어점으로 편집 | Unity Companion License (Unity 의존 프로젝트용; 본문 미열람) | 에디터 번들 2.9.0 [확인], 설치 시험 [미확인] | 0.5일 | **채택** | §1-2 |
| 손으로 쓴 높이 함수 (`IHeightField`) | 언덕·꼭대기·바다 쪽 절벽·계단 단차 | 자체 코드 | 해당 없음 | 0.5~1일 | **채택** | §2-2 |
| 길 얹기 + 옹벽 (`SplineTown.CutFill` 의 *알고리즘*) | 지형을 중심선에 맞춰 깎고 메우고 높이차가 크면 옹벽 | 이 리포 코드 | 순수 C#, 엔진 비의존 | 복사 1~2일 / 주석(`:1976-1980`)대로 재작성 ~100줄 [추정] | **채택** (D-117 재개 아님·함수 단위 — PD 확인) | §1-1 |
| 메시 지형 (Terrain 오브젝트 아님) | 규칙 11 연결, 수직 단차 | — | — | 기존 코드 | **채택** | §1-3 |
| 새 배치 규칙 4개 (후퇴 변동·틈·뒤 줄 한 단 위·방향 틀기) | "복도" 해소, 비탈에 쌓인 집 | 자체 코드 | — | 1~1.5일 | **채택** | §3-3 |
| `SplineInstantiate` + 기존 Poisson-disk | 가로등·난간 기둥·수풀 산포 | Companion License / 자체 | 위와 같음 | 0.5일 | 채택 | §4-2 |
| 정점색 마모 마스크 (직접 굽기) | D-128 세월 흔적을 이유 있는 곳에만 | 자체 | 셰이더 COLOR 입력 추가 | 1일 | 채택 | §4-2 |
| Copernicus DEM (참고 실루엣) | 비례 숫자 몇 개 읽기 | 상업 가능·크레딧 6(b)+6(c) | 데이터 | 반나절 | 채택(숫자만) | §2-2 |
| URP Decal Projector | D-128 마모 | 내장 | 우리 셰이더와 호환 **미확인** | 시험 반나절 | **시험 후 결정** | §4-2 |
| Blender A.N.T. Landscape | (대안안) 높이맵 원본 | 애드온 GPL, 결과물은 만든 사람 소유 | 번들 | 1~1.5일 | **대안안에서만** | §2-2 |
| Kenney Modular Buildings / Quaternius | 집 변형 조각 | CC0 (Kenney 확인, Quaternius 지난 조사) | 에셋 | 1~1.5일 | 참고 — 쓰면 `art/provenance.csv` 에 URL·날짜 기록(브리프 규칙 8) | §3-2 |
| Unity Terrain 오브젝트 | — | — | — | — | **제외** | §1-3 |
| Gaea / World Machine 무료판 | — | **비상업** | — | — | **제외** (유료 Indie $99는 참고만) | §2-2 |
| WFC 계열 (Townscaper식·marian42·DeBroglie·mxgmn) | — | MIT 들이지만 | 구현 개월 단위 / 2021 push | 며칠~개월 | **제외** | §3-2 |
| SplineMesh · Path-Creator · RoadArchitect · TTG | — | MIT | Unity 6 미확인, Splines 와 겹침 | — | **제외**(RoadArchitect 는 교차로 코드 읽기만) | §1-2, §3-2 |
| 외부 PSX/픽셀 셰이더 | — | 라이선스 미확인/유료 | 2022.3 에서만 시험 | — | **제외** | §4-2 |
| OceanFlow | 물가 거품 | **GPL-3** | — | — | 제외 (원리만 차용 — 지난 조사) | `oss-assets-2026-10-20.md` ⑤ |
| GSI 일본 DEM | — | PDL 계열 + 측량법 확인 못 함 | — | — | 제외 (교토도 Copernicus) | §2-2 |

### 6-2. 추천 1안 — "스플라인 중심선 + 손 높이 함수 + 길 얹기(옹벽) + 새 배치 코드"

이번 주 순서와 시간(전부 [추정], 11일 중 6~7일):

| 순서 | 할 일 | 시간 | 이 단계의 확인 방법 |
|---|---|---|---|
| 1 | `com.unity.splines` 설치(에디터 번들 2.9.0), `SplineContainer` 한 개에 제어점 6~10개 — 좁은 오르막 → 꼭대기 → 갈림길 Y | 0.5일 | 컴파일 통과, 스플라인 점을 끌면 길 메시가 다시 만들어짐(수동 왕복 없음) |
| 2 | `HilltopHeight : IHeightField` — 제어점 5~8개로 언덕·꼭대기 평지·바다 쪽 낭떠러지 + 계단 단차. 비례(고저차·경사)는 포지타노 DEM 단면에서 읽은 숫자만 | 0.5~1일 | 지형만 렌더한 캡처 1장 |
| 3 | 중심선 → 길 얹기(깎기/메우기) + 옹벽. 알고리즘은 `SplineTown.cs:1976-1980` 주석에 거리 규칙이 다 적혀 있다(길 밑 −0.10 m · 어깨 · 블렌드) | 1~2일 | 옹벽이 생기는 곳이 "높이차 > 임계"인 곳뿐 |
| 4 | 배치 코드: 건물을 `(x,z)` 로 정하고 `높이 = 지형 높이`를 읽어 기단/옹벽을 만든다. 규칙 4개(§3-3) | 1~1.5일 | 앞면 후퇴가 분산됨, 길보다 높은 두 번째 줄이 보임 |
| 5 | 소품(`SplineInstantiate`·Poisson-disk) + 정점색 마모(§4) | 1일 | 마모가 *이유 있는 곳*(벽 밑·연석·처마 밑)에만 있음 — 규칙 6 |
| 6 | 실제 게임 캡처 **1× → 흑실루엣 → 4×** 를 도현이 직접 판정, 수정 | 1~2일 | 브리프 규칙 12 |

**왜 이 조합인가**: (1) 지형을 먼저 정하고(함수) 길을 위에 얹는 D-132 순서 그대로다 (2) 새로 받는 것은 **패키지 1개(번들)** 뿐이고 나머지는 이 리포 코드·셰이더로 해결된다 (3) 라이선스 위험 항목이 없다 — 유일한 의무는 DEM 크레딧 문구 (4) 어느 단계도 AI 생성물을 쓰지 않는다 (브리프 규칙 1).

**이 안의 약점 3개 (추정)**: ① 손 함수 지형이 "너무 매끈하다"는 판정이 날 수 있다 → 대안안 ② 3단계의 `CutFill` 복사는 의존 구조(`SplineTownResult`·설정) 크기를 안 읽어 난이도 미확인 → 재작성(~100줄)으로 후퇴 가능 ③ 규칙 4개의 수치는 전부 가설.

### 6-3. 대안 1안 — "높이 함수 자리에 Blender 높이맵을 끼운다"

- 위 파이프라인은 지형이 `IHeightField`(함수) 입력이라, **함수 대신 래스터 높이맵을 읽는 구현으로 바꾸면** 나머지는 그대로다.
- 높이맵은 Blender(A.N.T. Landscape 노이즈 + 침식 + 스컬프트, 에이전트가 파이썬으로 스크립트)에서 만들어 16비트 PNG/RAW 로 내보낸다. A.N.T. 의 높이맵 내보내기 기능은 문서에서 못 찾았으므로 Blender 파이썬으로 격자 높이를 직접 샘플링하는 방식을 가정한다 [추정, 시험 필요].
- **언제 고르나(트리거)**: 1안 2단계 캡처에서 도현이 "지형이 인공적"이라 판정했을 때. 비용 +1~1.5일.
- 비용·위험: Blender↔Unity 왕복이 생긴다("수동 왕복은 버그" 규칙) → 내보내기를 한 번 실행으로 묶는 스크립트가 선행. 32 px/m 화면에서 침식 디테일이 보일지는 의문 [추정, §2-3].

### 6-4. 넣지 말아야 할 것과 이유

| 넣지 말 것 | 이유 (근거) |
|---|---|
| Unity Terrain 오브젝트 | `MoodController` 가 `_Ambient/_FogColor/_FogStart/_FogEnd` 가진 머티리얼만 갱신 → 지형이 무드 전환(D-125)에서 빠짐 (`MoodController.cs:88-91`), 높이맵은 수직 단차 불가 [측정+추정] |
| Gaea·World Machine 무료판으로 만든 지형 | 두 도구 모두 무료판은 비상업 전용 → Steam 출시물에 들어가면 라이선스 위반 가능 [2차] |
| WFC·Townscaper 식 생성기 | 본인 글 기준 Townscaper 구현 약 3개월, Bad North 약 2년 [확인]. 11일에 새로 못 만들고 marian42 판은 2021 이후 업데이트 없음 |
| SplineMesh · Path-Creator · RoadArchitect 통째 도입 | Splines(번들)와 기능이 겹치고 Unity 6 시험 안 됨. 필요한 건 길 메시가 아니라 *지형에 얹기* |
| 외부 PSX/픽셀 셰이더 | 자체 `WorldSurface` + Point 업스케일이 이미 룩을 정함. 라이선스 미확인·2022.3 시험뿐 |
| 실제 DEM 을 지형으로 그대로 복제 | 30 m 해상도 → 200 m 에 칸 약 7개 [계산]. 크레딧 의무만 생김. 숫자만 읽는다 |
| 일본 GSI 표고 타일 | 측량법상 승인 필요 여부를 확인 못 함 [2차] — 이미 확인된 Copernicus 로 통일 |
| GPL 코드의 셰이더(OceanFlow 등) | GPL-3: 닫힌 상용 게임에 그대로 넣기 어려움(지난 조사). 원리만 차용 |
| 유료 에셋(EasyRoads·PolyTerrain·Gaea Indie·Morro) | 과제 규정상 참고만 |

### 6-5. 프로젝트 규칙 제안 (시작값 — 전부 가설, 실제 캡처로 조정)

| 규칙 | 시작값 | 확인 방법 |
|---|---|---|
| 길 굽이 반경 | 구간 안에 최소 3종 이상(예: 12 / 25 / 60 m), 같은 반경 연속 2회 초과 금지 | 중심선 곡률 로그 |
| 건물 후퇴 | 필지마다 0~3 m 분산, 같은 후퇴 3채 연속 금지 (교토 상점가는 후퇴 0 허용) | 배치 로그 |
| 높이 | 건물 기단 윗면이 길 높이와 다른 건물 ≥ 40 %, 길보다 높은 두 번째 줄 존재 | 배치 로그 |
| 틈 | 4~6채마다 골목·계단·빈 터 1곳 | 배치 로그 |
| 지형 | 길 옆 평평한 띠 폭 ≤ 어깨 폭(6 m), 그 밖은 원지형 | 단면 캡처 |
| 마모 | 벽 밑·연석·처마 밑 같은 이유 있는 곳에만 (브리프 규칙 6) | 캡처 |
| 외부 에셋 | 쓰는 즉시 `art/provenance.csv` 에 URL·날짜·AI 아님 기록 (브리프 규칙 8) | 장부 검사 |
| 크레딧 | DEM 값을 쓰면 6(b) 문구 + 6(c) 면책 문장 (`tools/town-gen/dem/ATTRIBUTION.md`) | 크레딧 화면 |
| 판정 | 실제 게임 캡처 1× → 흑실루엣 → 4×, 도현이 직접 (브리프 규칙 12) | — |

## §7 모르는 것 / 미확인

- `com.unity.splines` 2.9.0 의 **설치·컴파일(6000.3.19f1)**, 그리고 스플라인 편집 시 자동 재빌드가 되는 이벤트 구조 — 시험 안 함.
- Unity Companion License **본문의 상업 조항**, Quaternius·KayKit 라이선스 재확인, Kenney Modular Buildings 내용물.
- SplineMesh·Path-Creator·RoadArchitect·TTG·Dillo VertexPaintTool 의 **Unity 6 시험**; MarcusElg Road Creator 의 정확한 저장소.
- `SplineTown.CutFill` 의 **의존 구조 크기**(복사 난이도), `TownGenStructure` 밀도 경로와 `SplineTown` 필지 경로가 각각 어디서 쓰이는지.
- Decal Projector × `WorldSurface` 호환(DBuffer/스크린스페이스).
- Gaea Community 해상도 출처 간 불일치, GSI 측량법 승인, mxgmn/marian42 의 최근 커밋 날짜 등 일부 메타데이터.
- 교토 DEM 타일(`N34_00_E135_00`/`N35`) 내려받기와 포지타노 단면의 **고저차 숫자** — 아직 안 뽑음.
- **운전 게임의 길 제작 방식 1차 자료**(Art of Rally·Easy Delivery Co): 못 찾음.
- 산넨자카·포지타노의 **실제 건물 배치 패턴** 근거 — 이 조사에서 직접 확인하지 않았다. 규칙 4개와 수치는 전부 [추정].
- A.N.T. Landscape 의 높이맵 내보내기·Strata 의 정확한 효과.
