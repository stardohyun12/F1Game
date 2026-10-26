# BuildBayLoop 가 읽는 JSON 필드 vs town-v6b(=v6osm) 비교

기준: F1/Assets/Editor/BuildBayLoop.cs (+ .Lived.cs, .Hilltop.cs). 파싱은 `Json.Parse` 후 딕셔너리 캐스트.
**없는 키를 읽으면 KeyNotFoundException/InvalidCast 로 빌드가 통째로 죽는다**(TryGetValue 가 아닌 곳). 아래 "죽음/기본값/건너뜀" 은 그 결과.
v6osm 실제 키: meta, pieces, terrain, sea, roads, buildings, garage, mart, stops, boundary, edits, fun, metrics
(roads 1개 키: id kind width roughness closed knots interp source / 건물 1399채 = poly+y+piece+osm_way / 제어점 1521개, step 20)

## 1. 로딩 (Load, BuildBayLoop.cs:185-216)
- terrain.seaFloor / cliffWidth / power / softening (189) — 필수(죽음). v6: 없음(method/step/blend 만). 옛 값 seaFloor -6, cliffWidth 10, power 3, softening 6, method "idw".
  → 변환기가 이 4개를 terrain 에 넣거나, C# 에서 기본값 처리.
- terrain.controlPoints [[x,z,y]...] (191) — 필수. v6 있음, 같은 순서 [x,z,y]. OK. 단 옛 것은 수십~수백 개 IDW, v6 는 20m 격자 1521개 + 길 점 전부 추가(213) → Idw() 가 점마다 전체 순회(261) 라 격자 2m x 200x264 칸 x 수만 점 = 매우 느림. 격자 보간(method:'grid')으로 바꿔야 함.
- terrain.beaches [[cx,cz,rx,rz]...] (193) — 필수(죽음). v6 없음 → 빈 배열 `[]` 로 넣으면 됨(선택 성격).
- sea.landPolygon [[x,z]...] (195) — 필수(죽음). v6: 없음(sea.level 만). 해안선 다각형이 없으면 LandDist 가 못 돌아 바다/해변 높이 처리(302-321)가 불가.
  → 변환기가 지형(높이<=0) 등고선에서 육지 다각형을 뽑아 내보내야 함.
- sea.level (144, 1510, 1913) — 필수. v6 있음(0). OK.
- meta.version (215) — 필수(로그용). v6 있음(6). OK.
- roads[] (197) — 필수. 키별:
  - id string (199) 필수, kind string 필수, width float 필수. v6 OK.
  - kind 쓰임: Car = main|village|shortcut (45). stairs/alley 는 비차길. v6 의 main/village/alley/stairs 와 맞음.
  - roughness (200) 선택, 없으면 0.3(stairs 0). v6 있음.
  - closed (201) 선택, 기본 false. v6 전부 false 로 확인한 건 아님(헤드만).
  - terrainFollows (202) 선택, 기본 true. false 면 제어점에 안 들어감(213). v6 alley/stairs 에 있음 — 호환.
  - overWater (203) 선택(잔교). v6 없음 → 기본 false.
  - knots [[x,z,y]...] (204) 필수. 읽을 때 (x,y,z)로 뒤집어 Vector3 (x, 높이, z). v6 형식 [x,z,y] 동일. OK.
  - features[] (205) 선택. v6 없음 → 점프/돌담/랜드마크 없음. 문제없음.
  - start.join / end.join (206-209) 선택. v6 는 `out:'start'|'end'` 라 의미 다름(여기선 무시됨).
  - widthProfile / crossSection (210-211) 선택. 없으면 폭 고정, 어깨 main 0.8 / village 0.4 (46), 인도 띠·연석 없음.
- 길 샘플링 (87-89): knots 를 Unity Splines `BezierKnot` 으로 넣고 knots>2 면 **AutoSmooth**(Catmull-Rom 비슷한 부드러운 곡선), 2점이면 Linear. `Sample(sp, 1f, closed)` 로 1m 간격.
  v6 는 OSM 원본 꺾은선(interp:'linear')이라 AutoSmooth 가 꼭짓점 사이를 둥글게 깎음(코너가 안쪽으로 파고듦) → 조밀한 knots 면 오차 작지만 교차로 모서리는 변함. 대안: 항상 Linear 로 넣고 1m 선형 샘플.
- 닫힌 루프 요구: `closed` 면 스플라인 닫음. 하지만 "main 은 루프여야 한다" 는 검사는 없음. 단 **id=="main" 인 길이 반드시 있어야 함**: `roads.First(r => r.id=="main")` (127, 1625, 및 Car/Tutorial/Motion/Hilltop/Coast 에 mainR 전달) → 없으면 InvalidOperationException. v6 id 는 `tomo_59901325_0` 식이라 **main 이름의 길이 없음**(전체를 직접 확인하진 않았음, 헤드 6개는 village). 변환기가 주행 코스용 main(긴 연속 polyline)을 하나 만들거나, C# 이 mainR 을 고르도록 변경 필요. 이게 가장 큰 구조 차이.
- 갈림목 높이 맞춤 (95-126): 위계(Rank) 낮은 길이 윗 길 띠와 겹치면 높이를 맞추고 앞뒤 12m 녹임. 길 수백 x 점 수천 -> 약간 느림. 플레이엔 무관.

## 2. 지형 (BaseH 302-321, CutFill 354-418)
- 격자 상수 하드코딩: X0 -208, Z0 -240, X1 192, Z1 288, Step 2 (33), 먼 판 -960..960 x -960..1120, 16m (34). v6 는 boundary ±380 → **상수 변경 필요**(예 -400..400). 정밀 격자 400x400칸=160k 칸 x IDW 가 문제.
- 높이 = IDW(power 3, softening 6) (258-268) → `Mask2` 로 구간 ②(x -12..140, z -4..228)는 BuildHilltopSlice.HilltopHeight+30 으로 섞음(271-274, 306). **v6 에선 이 마스크를 꺼야 함**(옛 지도 좌표 하드코딩) — 필수 수정.
- 바다/육지 (277-300): InPoly(landPoly) 와 LandDist. 하드코딩 판 닫는 변(x>=195, z<=-236 등)도 옛 영역 기준 → v6 에선 수정.
  d<cliffW 이면 seaFloor 쪽으로 눌러 절벽·해저, beaches 타원 안은 해변 경사.
- CutFill: 길 선분 따라 −12cm 깎기/메우기 + Blend 6m. 상수 Blend 6 (35) vs v6 terrain.blend 45 — 읽지 않음(무시됨).
- farMasses[] (327) — FarH 에서 필수(죽음): center, height, radius | ringRadius. v6 없음 → 빈 배열 `[]` 가능(먼 산/섬 없어짐).

## 3. 건물 (Buildings 1067-, Lived.cs:22-)
- **parcels[] 사각형만 지원, 다각형 압출 없음.** 필수(1070, Lived:26, 1465). 키: x,z,w,d (1100), floors (int, 필수), frontMid [x,z] (1078, 필수), front{road,side} (1077, 필수 — road id 가 roads 와 대응, 길 따라 정렬·같은 프리팹 반복 방지·줄 후퇴에 씀), id "P###" (1178, int.Parse(Substring(1)) 라 'P숫자' 형식 필수), district (선택, 없으면 마지막 구역), facing front/back/side (선택), frontSpace (선택, 0), density·edge·walk·baseY·plinth·gapAfter·rot·section (여기선 거의 안 읽거나 선택).
- districts[] (1085, Lived:25) — 필수. id, density(dense|medium|else), types{타입:가중치}, floors, 폴리 등. 건물 타입 선택(shop_house 등 → Art/BayLoop/Buildings/<type>_*.prefab, 없으면 폴백 열)을 이 가중치로 함. v6 없음 → 구역 정의를 만들어야 함(최소 1개).
- 변환 방법(v6 buildings → parcels): poly 의 최소 외접 사각형(회전 포함)으로 x,z,w,d; 가장 가까운 길을 front.road/side, 그 길로의 수선발을 frontMid 로; floors 는 면적/y 로 추정; id=P000..; district 는 구역 폴리곤 포함 여부.
  w/d 가 폴리 대비 부정확(L자·삼각형)하고 OSM 건물은 길에 붙은 줄집이라 필지 정렬·후퇴 로직(1150-)이 다르게 동작할 수 있음. 아주 작은 건물(w<2.6) 은 틈 로직(1129)과 충돌 가능.
- 건물 y 는 사용 안 함: 높이는 H(x,z) 지형에서 얻음(baseY·plinth 도 지형 기준). v6 의 `y`는 쓰이지 않음.

## 4. 기타 지물
- openGround[] (426, 476, 980) — 필수(죽음): id, kind(plaza|quay|beach|lot|meadow|field), poly [[x,z]...], flatten bool, roughness. v6 없음 → `[]`. 광장/부두 평탄화·석축 호안(Coast) 사라짐.
- trees.points [[x,z,"Tree"|"Bush"]...] (1015) — 선택(없으면 "나무 0" 반환). v6 없음 → 나무 없음. 필요하면 생성.
- poles.points [[x,z,"StreetLamp"|"Pole"]] (1626) — **필수(죽음)**: `Get(J,"poles","points")`. 첫 점으로 main 에 투영. v6 없음 → `{"points":[]}` 필요.
- fields[] (1655, Hilltop:69) — 필수(죽음): rect [x,z,w,d], bands, id. v6 없음 → `[]`.
- stops[] (1678, 1761) — 필수. id, n(int), name, cargo, pos [x,z]. v6 있음, 같은 형태. OK. n==1 인 정류장이 튜토리얼 시작점(1763).
- mart.pos [x,z] (1072, 1688, 1704, 1912) — 필수. v6 있음(pos 225.1,-78.5). OK. MART 반경 9m 안 필지는 건너뜀(1102).
- garage — BuildBayLoop 는 읽지 않음(차 시작 위치는 main 길 기준 Car(mainR)). v6 의 garage.pos 를 쓰려면 C# 수정.
- boundary.play — 읽지 않음(MapEdge 는 하드코딩). junctions, outRoads, addressPool, fog, sections, seaLabels — 이 파일들에선 안 읽음(grep 결과 없음).
- pieces/edits/fun/metrics — 무시됨.

## 5. 종합: 필수인데 v6 에 없는 것 (빌드가 죽는 순서)
1. terrain.seaFloor/cliffWidth/power/softening (189), 2. terrain.beaches (193), 3. sea.landPolygon (195),
4. id=="main" 길 (127), 5. openGround (425), 6. farMasses (327), 7. parcels (1070) + districts (1085), 8. poles.points (1626), 9. fields (1655).

## 다음 과제에서 할 일 (제안)
1. **파이썬 변환기 한 개로 v6b → bayloop 스키마 JSON 생성** (추천). 이유: C# 은 유니티 컴파일 왕복이 느리고 BuildBayLoop 가 2274줄+부분 클래스 5개라 손대면 옛 plan(v5)도 깨질 위험. 반면 JSON 을 옛 스키마에 맞추면 C# 은 최소 변경(상수만).
   - 변환기가 채울 것: 위 9항목 기본값/빈 배열, 해안선 landPolygon(지형 0m 등고선), parcels(폴리→외접 사각형+front 매칭), districts 1~3개, 가능하면 main 길 하나(가장 긴 큰길 polyline 이어붙이기; closed:false 라 루프 아님).
2. C# 최소 수정 (변환기로는 불가): 격자 상수(X0..Z1)와 Mask2 비활성(HilltopHeight 섞기 끄기)·LandDist 판 닫는 변 하드코딩, Idw 를 격자 보간으로 교체(제어점 수만 개 순회 방지), JsonPath 를 인자/파일명으로. 길 곡선 Linear 옵션(interp 읽기).
3. 폴리곤 건물을 C# 이 직접 압출하는 안은 프리팹/폴백 건물 시스템(필지 사각형 전제)을 우회하게 되어 범위가 큼 — 1차는 보류, 외접 사각형으로 먼저 눈으로 확인.
4. 한 번 돌린 뒤 로그 "길 N개, 본선…" 과 지형 칸수로 sanity 확인 (유니티 실행은 이 조사 범위 밖).
