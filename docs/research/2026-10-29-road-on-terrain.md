# 지형 위에 길 깎아 까는 법 — 우리 게임 안 (D-203)

작성: 리서치팀 · 2026-10-29 · 씬·Unity 수정 없음(읽기만) · 측정 코드 `docs/research/road_on_terrain_measure.py`

## 결론 (3줄)
1. 지금 깨지는 이유는 **길 띠(리본)와 지형이 4~6 cm 차이로 거의 같은 높이에 따로 놓여서**다. 닿는 순간 지형이 삐져나오거나 묻힌다. 측정: 차길 5,803 m 중 **311 m(5.4%)**, 걷는 길까지 합치면 13,086 m 중 **2,227 m(17%)**.
2. 업계 표준은 "지형을 길에 맞춰 **한 연산자**로 깎고 메운다 + 가장자리는 기울기 제한 경사로 이음"이다. 길 따로 지형 따로 얹는 방식은 표준이 아니다.
3. 제안 1안: **원뿔 깎기**(지형을 길 높이 위 2:1 · 아래 3:1 사이로 끼운다) + **길 주변 1 m 격자** + **높이차 1.2 m 초과는 석축**. 건물 테라스(D-203)도 같은 연산자로 합친다. 파이썬 시뮬: 311 m → 114 m(3~5 cm 이내까지 합치면 더 줄일 여지는 층 겹침 처리에 달림).

---

## 1. 지금 맵에서 깨진 곳 (측정)

![차길 10cm↑ 자리](2026-10-29-road-on-terrain-map.png)
범례: 회색 = 정상 차길 · **빨강 = 차길 가장자리에서 지형이 길보다 10 cm↑** · 주황 = 걷는 길 10 cm↑ · 연회색 = 걷는 길 정상 · 파랑 네모 = 도현 스샷 자리 ±45 m.
스샷: [`unity-Map_TownV6-20261029-032740-45c3.png`](../../tools/viewer/feedback/shots/unity-Map_TownV6-20261029-032740-45c3.png)

**숫자 하나: 길 가장자리에서 지형이 길보다 10 cm 이상 높은 구간 = 2,227 m (전체 길의 17.0%). 차가 다니는 길만 311 m (5.4%), 연속 구간 약 82개.**

| 종류 | 길이 m | 10cm↑ 가장자리 m | 비율 | 한복판이 묻힘(10cm↑) m |
|---|---|---|---|---|
| main(큰길) | 957 | 13 | 1.4% | 6 |
| village(마을길) | 4,846 | 298 | 6.1% | 79 |
| alley(골목) | 3,893 | 898 | 23.1% | 431 |
| stairs(계단) | 3,390 | 1,018 | 30.0% | 455 |
| 합계 | 13,086 | 2,227 | 17.0% | 971 |

**측정 조건(근사):** 현재 씬이 쓰는 지형 메시 `Mesh_TownV6_Terrain_d203.asset`(393×393, 2 m 격자)와 길 메시 `Mesh_ROAD_*.asset` 310개(지형 상자 ±390 안)를 읽었다. 길마다 중심선 점의 왼·오른 모서리에서 지형 높이(삼각형 보간)를 빼고, 10 cm 이상이면 그 점이 맡은 길이(앞뒤 점 간격 절반씩)를 더했다. 다른 길 띠가 덮은 점(갈림목 겹침)은 뺐다. 길 종류·폭은 `docs/map/town-v6b.json`. 이전 지형(`_loopfix`)으로 재도 같은 값(±1 m) — D-203 테라스는 길 근처를 건드리지 않았다.

### 차길 311 m 의 원인 (측정 분류)
| 원인 | m | 뜻 |
|---|---|---|
| 자연 지형 가로 경사 ≥ 20% | 104 | 비탈에서 위쪽 가장자리가 묻힘 (스샷 "비탈에서 한쪽이 땅 아래") |
| 겹친 다른 층 | 95 | 다른 차길이 12 m 안에서 0.4 m↑ 높이 차로 지나감 → 깎기가 서로 덮어씀 |
| 기타(미규명) | 109 | 아래 "추정" 참고 |
| 같은 높이 갈림목 | 3 | |

### 코드에서 보이는 구조적 이유
- 지형을 노면 **−0.5 cm**(`p.y + RoadColLift − .045f`, [`BuildBayLoop.Gray.cs:100`](../../F1/Assets/Editor/BuildBayLoop.Gray.cs))로 깎고, 보이는 길 띠는 큰길 **+6 cm**·마을길 **+4 cm**([`:129`](../../F1/Assets/Editor/BuildBayLoop.Gray.cs)) 띄운다. 여유는 **4.5~6.5 cm**뿐이다.
- 지형 높이는 "가장 가까운 1 m 샘플 점"의 높이를 그대로 쓴다([`:96-101`](../../F1/Assets/Editor/BuildBayLoop.Gray.cs)). 샘플과 정점이 최대 0.5 m 어긋나므로 15% 경사에서 **7.5 cm 오차 > 여유**다. (추정 — 경사별로 따로 검증은 안 했다. 세로경사 15%↑ 차길은 406 m, 7%.)
- 지형 격자가 2 m라 어깨(3 m)+녹임(5 m) 8 m 구간이 4칸뿐이다([`:34`](../../F1/Assets/Editor/BuildBayLoop.Gray.cs), [`:90`](../../F1/Assets/Editor/BuildBayLoop.Gray.cs)).
- 걷는 길(골목·계단)은 **깎지도 않는다** — 가장 가까운 정점 하나만 `길 − 0.1 m` 로 누른다([`:110-115`](../../F1/Assets/Editor/BuildBayLoop.Gray.cs)). 그래서 26%(1,916 m)가 삐져나온다.
- 길 띠는 폭 방향이 **평평**하다. 가로경사 20%↑ 비탈에서는 지형이 한쪽으로 기울고 띠는 안 기울어 한쪽이 묻히고 한쪽이 뜬다. 치마(0.6 m, [`:438`](../../F1/Assets/Editor/BuildBayLoop.Gray.cs))가 뜬 쪽만 가린다.
- D-203 테라스를 넣을 때도 같은 충돌이 났다: 지형을 다시 계산하자 길 정점 207개 중 **199개가 덮였다(최대 3.57 m)**. 그래서 "길 정점 4 m 안 지형은 길 관계를 넘지 않게" 묶는 땜빵을 넣었다 — [`programmer-20261029-0338.md:14`](../team/reports/programmer-20261029-0338.md). 깎는 연산자가 둘(길·테라스)이라 서로 싸운 것이다.

---

## 2. 일반적인 방식 (조사)

| # | 방식 | 어떻게 | 맞는 점 · 틀린 점 | 출처 |
|---|---|---|---|---|
| a | **스플라인이 지형을 깎고 메운다** | 스플라인 + 폭 + 옆 falloff 로 높이맵을 길 쪽으로 올리거나 내린다. 끝은 코사인으로 마감. | 길이 지형을 따라간다. 가장 흔함. 비탈에서 falloff 가 길면 계곡이 파인다. | [Unreal Landscape Splines](https://dev.epicgames.com/documentation/unreal-engine/landscape-splines-in-unreal-engine)(Raise/Lower·Side Falloff·End Falloff), [Unity Road Splines Procedural Tool](https://discussions.unity.com/t/road-splines-procedural-tool/1514428)(Edge Smoothness 0~1, 거리장 텍스처 256², "mesh passing through road" 오류가 해상도 낮을 때 남음), [Infinite Lands](https://d34kmbgkaszffl.cloudfront.net/blog/infinite-lands-a-look-at-its-spline-based-road-system)(A*→스플라인 단순화→주변 평탄화), Hesiod [FlatbedCarve](https://hesioddoc.readthedocs.io/en/latest/node_reference/nodes/FlatbedCarve/)(검색 요약만 확인) |
| b | **높이맵·마스크에 길을 칠하고/찍는다** | 길 중심을 흰색, 가장자리 0 으로 떨어지는 마스크를 래스터화해 길 높이와 자연 지형을 섞는다. 같은 마스크로 길 질감도 칠한다. | 한 면이라 겹침·z-fight 가 없다. 층(고가·겹침) 불가. | [gamedev.net 블로그](https://nexe.gamedev.net/blogs/entry/600044-and-more-roads)(검색 요약만, 원문 접속 실패), Unreal 같은 문서의 Layer Name 칠하기 |
| c | **길 메시 + 지형 정점 이동/구멍** | 길 아래 정점을 길 가장자리로 밀어내거나, 길 메시를 위아래로 늘린 "절단기"로 지형을 뚫는다. | 정점 이동은 "엄청 촘촘한 격자"가 필요(저폴리 간섭). 절단기는 깔끔하나 셰이더가 필요. | [Blender Artists 스레드](https://blenderartists.org/t/add-road-surface-to-terrain-mesh/594332)(검색 요약만) |
| d | **길 먼저(벡터 지형)** | 길·강을 벡터로 두고, 지형 높이·질감을 그 벡터를 래스터화해 만든다. 길은 설계상 평평. | 길이 주인공. 벡터에 높이가 없으면 지형에 종속. | [Bruneton & Neyret 2008](https://diglib.eg.org/items/6c9ac337-23f9-4d26-a584-201cc7fc3e3d), [Galin 외 2010](https://diglib.eg.org/items/0aba12a9-bdbc-43a2-982c-5d1a9a43a2ba)(최단경로로 길을 찾고 지형에 깎아 넣는다; 본문 PDF 은 시간 초과로 못 읽음 — 초록만) |
| e | **길 띠를 지형 위에 얹는다** | 우리 현재 방식. | 단순. 여유 몇 cm 로 두 면이 맞닿아 깨짐. | 이 리포 `BuildBayLoop.Gray.cs` |

**Unity 도구:** Splines 패키지는 도로 샘플은 있지만 **터레인 통합이 없다** — 길 메시와 지형 변형은 직접 스크립트로 짜야 한다([Unity 포럼](https://discussions.unity.com/t/road-system-for-unity-based-on-the-current-spline-package-and-the-terrain-system/1664437), [Splines 매뉴얼](https://docs.unity3d.com/Packages/com.unity.splines@2.8/manual/index.html)). 유료는 이름만: EasyRoads3D 등(리포가 쓰는 건 없음). 우리는 Unity Terrain 이 아니라 메시 지형이라 어차피 직접 짠다.

**사례:**
- Easy Delivery Co.: Unity, PS1풍 저폴리([Wikipedia](https://en.wikipedia.org/wiki/Easy_Delivery_Co.)). 개발자가 공개한 건 눈 쌓임 셰이더([80.lv](https://80.lv/articles/check-out-this-cozy-snowy-delivery-sim-made-with-unity))뿐 — **길을 어떻게 깔았는지는 못 찾았다.**
- Desolate Drive(인디 드라이빙): 길 만들기에 가상 텍스처 → 스플라인 메시 → 다시 가상 텍스처로 **왕복**했고, 환경 아티스트가 **교차로 모양**에 가장 공을 들였다([devlog](https://itch.io/devlog/203883/making-of-the-roads.amp)). 지형에 맞추는 법은 안 적었다.
- Cities: Skylines II: 도로 높이 옵션으로 **cut-and-fill**(깎기·메우기) 도로를 직접 지원한다([Paradox](https://paradoxinteractive.com/games/cities-skylines-ii/features/road-tools)).
- 길 단면 수치(실제 도로 표준): 메우기 3:1·깎기 2:1이 일반적으로 허용되는 가장 가파른 값([Charlotte NC](https://www.charlottenc.gov/files/sharedassets/city/v/1/growth-and-development/documents/cldsm/crosssectionguidelines.pdf)), 낮은 성토는 더 가파르게 허용([MoDOT D-66F](https://www.modot.org/media/16754)), 소로 도랑 폭 약 2 m·깊이 0.6 m([Alberta 표준도(폐지본)](https://www.alberta.ca/system/files/custom_downloaded_images/trans-dwg-cb6-2-3m51-obsolete.pdf)). 모두 검색 요약만 확인.

---

## 3. 우리 안

### 1안 (추천) — 원뿔 깎기 + 길 주변 1 m 격자 + 석축

**그림으로 (옆에서 본 단면, 한 지점):**
```
 자연 지형(비탈 위쪽)                                        자연 지형(아래쪽)
   ───────╲                                                    ╱───────
           ╲  깎기 2:1 (오른 만큼 옆으로 2 m)    메우기 3:1 ╱
   >1.2 m 면 → ┃석축(수직 벽)                      ╱
              ┃[어깨 1 m]━━ 차로 폭 w ━━[어깨 1 m]╱   ← 길 면: 좌우 평평(가로경사 0)
              ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛     ← 지형은 길 면 −3 cm
```

**연산자 한 줄(정점마다):** `e` = 길 모서리까지 거리. 지형 = 자연 지형을 `[길면 − (e−1)/3 , 길면 + (e−1)/2]` 사이로 **끼워 넣기**(clamp). `e ≤ 1 m`(어깨 안)이면 길면 −3 cm 로 고정. 자연 지형이 그 사이면 안 건드린다.
- 길면 높이는 **가장 가까운 점이 아니라 선분에 투영한 높이**(보간)를 쓴다 → 4.5 cm 여유를 갉던 ±7.5 cm 오차가 없어진다.
- 경사 한도는 **연산자가 보장**한다(깎기 50%·메우기 33% 이하). smoothstep 블렌드는 비탈에서 72%까지 가팔라진다(추정: 3×0.5 / 5 m 구간 기준, 미측정).

| 항목 | 값 | 근거 |
|---|---|---|
| 차로 폭 | `town-v6b.json` 의 `width` 그대로 (3.2~10 m) | 기존 |
| 어깨 | 1 m 평평, 길면 −3 cm | 격자 1 m 라서 2 m 격자일 때의 3 m 어깨가 필요 없다 |
| 깎기 / 메우기 | 2:1 / 3:1 | 위 도로 표준 |
| 석축 | 높이차 > 1.2 m 이면 수직 벽 띠(두께 0.3 m, 돌 재질) | D-203 도 같은 석축 필요: 집 앞·정비소 [programmer-20261029-0338.md:23](../team/reports/programmer-20261029-0338.md), :32 "다음 과제(축대 에셋)" |
| 배수로 | **안 한다** | 64 px 질감·카트 시점에서 안 읽힘(추정). 깎기 쪽 어깨에 돌 줄무늬 질감으로 대신 |
| 가로경사 | 0 (곡선 뱅크 없음) | 카트 조작 단순. 현재 리본도 평평 |
| 세로경사 | 기존 plan 의 15%(헤어핀 18%) + `SmoothProfiles` 그대로 | `docs/map/town-v6b.json` meta.note, `BuildBayLoop.Gray.cs:245-250` |
| 교차로 | `SmoothProfiles` 의 평평한 판(반경 = 큰 폭/2 + 1 m)을 "폭 큰 길"로 취급해 같은 연산자 적용 | 기존 |
| 걷는 길 | 같은 연산자, 어깨 0.3 m, 깎기·메우기 1:1 까지 | 지금은 깎지도 않음 |

**건물 테라스(D-203)와 같은 장치인가? → 예, 한 연산자로 합친다.** D-203 은 "발자국+1.5 m 평평, 바깥 8 m smoothstep"이라([programmer-20261029-0338.md:12](../team/reports/programmer-20261029-0338.md)) 길 연산자와 같은 모양인데 따로 돌아서 싸웠다. 합치는 규칙:
`Embank(발자국, 높이, 깎기비, 메우기비, 석축기준)` 하나. 길은 **선**(높이가 따라 변함), 건물은 **면**(높이 일정). 겹치면 **길 > 건물 > 자연**. 그러면 "길 정점 4 m 안 묶기" 땜빵이 필요 없다.

**시뮬 (파이썬 근사, 현재 길 모양에 적용):**
- 차길 가장자리 지형−길 ≥ 10 cm: **311 m → 114 m**, ≥ 30 cm: → 57 m.
- 남은 114 m 중 겹친 층 56 m · 기타 56 m · 같은 높이 갈림 3 m. 층은 "다른 높이 길은 소유권을 따로" 규칙이 필요하다(아래 단계 6).
- 길 변 11,607 m 중 자연 지형과 높이차 **1.2 m 초과 811 m(석축 후보)**, 0.5~1.2 m 2,653 m. 차길 단면의 60%는 0.5 m 이내라 대부분 평탄하다. **하한값이다:** `controlPoints`(8 m)가 이미 "길에 맞춰 깎은 지형"이라서 원본 SRTM 대비 실제 깎기·메우기는 더 크다.
- 1 m 격자 정점 170,063 개가 바뀜(전체 616,225).
- 가정: 길 소유권 = 가장 가까운 길 · 자연 지형 = `N0`(json controlPoints → 2 m 격자 → 3×3 평균 4번, 빌더와 같은 식).

**구현 단계 · 예상 턴 (프로그래머, 추정):**
| 단계 | 내용 | 턴 |
|---|---|---|
| 1 | 측정기(`road_on_terrain_measure.py`)를 `tools/` 로 승격 = 합격 기준 자동화 | 1 |
| 2 | `BuildBayLoop.Gray.cs:87-115` 의 carve 를 투영 높이 + 원뿔 clamp 로 교체 | 2~3 |
| 3 | 지형 격자 2 m → 1 m, 16 청크로 쪼개기(메시 asset·MeshCollider) | 2 |
| 4 | 석축 띠 메시(D-203 축대와 공용) | 2 |
| 5 | 걷는 길·건물 테라스를 같은 연산자로 이관, `_loopfix`·`_d203` 사본 폐기(빌더에 통합) | 2 |
| 6 | 겹친 층 소유권 | 1~2 |
| 7 | 캡처 3곳 before/after → 도현 판정(1× → 흑실루엣 → 4×) | 1~2 |
| 합 | | **11~15 턴(추정)** |

**리스크:**
| 리스크 | 정도 | 대응 |
|---|---|---|
| 1 m 격자: 정점 4배(61만), 에셋 약 60 MB(현재 14.8 MB × 4, 추정), 콜라이더 쿠킹 시간 미측정 | 중 | 청크로 쪼개고 길 근처만 1 m, 밖은 2 m(가장자리 틈은 스커트) — 단계 3에서 실측 |
| 지형 모양이 바뀌어 건물이 뜨거나 묻힘(OSM 건물 높이는 `CpAt` 기준) | 중 | 건물 `y0 = min(gy, minG) − 1` 기준을 새 지형으로 재계산 |
| 차가 깎기 경사(50%)를 올라탐 | 중 | 카트 한도 결정 필요(아래 "모르는 것") |
| 겹친 층 56 m | 낮음 | 단계 6 |
| 빌더를 다시 돌리면 사본 메시(`_d203`)가 사라진다 — [programmer-20261029-0338.md:34](../team/reports/programmer-20261029-0338.md) | 높음(현재) | 단계 5 에서 해결 |

### 대안 — 길을 지형에 **그린다** (리본 폐기)
- 같은 원뿔 깎기로 지형을 만든 뒤, 차길 리본(`ROAD_*`)을 없애고 **지형 면 자체가 길**이 되게 한다. 길 색은 길 마스크 텍스처(또는 정점 색)로 칠한다. 면이 하나라 z-fight·치마·콜라이더 이중이 없다. 근거: 방식 b·d(Bruneton–Neyret, Unreal 레이어 칠하기).
- 단점: `PitCrew/LofiPilot/Surface` 셰이더에 마스크 샘플 추가, `RoadSurface`·`StoneBumps`·갈림목 판·막다른 끝(`DeadEnds`) 흐름 재작성, 겹친 층 불가(고가·터널), 64 px 질감에 맞는 마스크 해상도 필요(추정: 784 m × 4~8 px/m).
- 예상 15~20 턴(추정). 1안으로 가다 리본 문제가 남을 때 후속으로 고려.

---

## 4. 프로젝트 규칙 제안 (체크리스트)
- [ ] **지형은 길을 따라간다, 반대로 하지 않는다.** 길 높이가 먼저 정해지고, 지형은 그 높이에 맞춘다.
- [ ] 길·건물 테라스는 **같은 연산자 `Embank`** 로 만든다. 우선순위 길 > 건물 > 자연.
- [ ] 지형 후처리 사본(`_loopfix`, `_d203`)을 **더 쌓지 않는다.** 빌더(`BuildBayLoop.Gray.cs`)에 넣어 재빌드로 같은 결과가 나오게 한다.
- [ ] **합격 기준(자동):** 측정기에서 차길 가장자리 지형−길 ≥ 10 cm 구간 = 0 m(겹친 층 예외 목록 별도), 한복판 묻힘 = 0 m. 지금은 311 m / 89 m.
- [ ] 단면 표준: 어깨 1 m · 깎기 2:1 · 메우기 3:1 · 1.2 m 초과는 석축 · 가로경사 0 · 배수로 없음.
- [ ] 길면과 지형 사이 여유는 **최소 3 cm 를 지형이 아래로**. 리본을 쓴다면 lift 는 0.03 이하로 통일(큰길 0.06 위계 들림은 폐기).
- [ ] 길 근처 격자 1 m 이하.
- [ ] 도현 판정은 길 가장자리 3곳(스샷 자리·북쪽 헤어핀·동쪽 비탈) 전/후를 1× → 흑실루엣 → 4× 로.

## 5. 모르는 것
- Easy Delivery Co. 의 길 구현: 공개 자료 못 찾음(검색 3회).
- Galin 2010 본문 PDF 가 시간 초과 — 단면 수치는 지자체 표준으로 대체. gamedev.net·Hesiod·Blender Artists·Cities: Skylines II·도로 표준은 **검색 요약만** 읽었다(원문 확인 전 단정 금지).
- 스샷에 동그라미 친 길의 정확한 id: 못 짚었다. 클릭 점(198.9, −77.4) 반경 45 m 안에는 큰길이 없고 마을길 30 m·계단·골목뿐이다.
- 차 한도: 깎기 50% 경사를 차가 올라타도 되는가? 지금 코드는 지형 전체가 주행 가능이다. 도현 결정.
- 차길 311 m 중 "기타" 109 m(시뮬 뒤 56 m)의 원인.
- 1 m 격자 에셋 크기·콜라이더 쿠킹 시간.
- 원본 SRTM 대비 실제 석축 길이(위 811 m 는 하한).
- 시뮬은 파이썬 근사다. C# 구현 뒤 같은 측정기로 다시 재야 확정된다.

## 파일
- 측정·시뮬 코드: [`road_on_terrain_measure.py`](road_on_terrain_measure.py) (실행 로그 [`.log`](road_on_terrain_measure.log), 요약 [`.json`](road_on_terrain_measure.json))
- 그림: [`2026-10-29-road-on-terrain-map.png`](2026-10-29-road-on-terrain-map.png)
