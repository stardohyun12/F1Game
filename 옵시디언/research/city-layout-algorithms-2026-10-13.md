# 도시 배치 알고리즘 후보 — index.opensource 조사 (2026-10-13)

> 도현: "도시 구성을 실제 같이 하는 알고리즘이 있을 텐데 instagram.com/index.opensource 에 쓸만한 거 많다". 조사는 web-search 에이전트, 검산·요약은 Claude.

## index.opensource 가 뭔가 (확인됨)
- 소개 "Creative open source for sustainable makers", 팔로워 약 6.6만, 운영 @hyun2xyz. 사이트 indexopensource.com 의 `data/index.json`(2026-10-07) 을 직접 받아 읽음 — 오픈소스 412개를 창작 도구로 분류한 색인. 1위 분류 기하·구조(115), 지도·환경 20.
- **도시 배치용은 거의 없다.** 대부분 펜 플로터·타이포·d3·유체·AI 이미지. 닿는 건 4개: MapGenerator(릴스 `instagram.com/p/DeFVMzjufBg`), WaveFunctionCollapse(`/reel/Dd3qztFuiCl`), MarkovJunior(`/reel/Dd146JnPmbU`), mapgen4(지형). 목록에만: NDWFC, ConvChain, Lindenmayer, ngraph.path, prettymaps.

## 후보 (라이선스·언어는 gh api 로 확인)
- **probabletrain/mapgenerator** — LGPL-3.0 · TS · 1.4k★ — 텐서장 도로 곡선 + 블록 + 건물 바닥. 그림은 제일 "진짜 도시". 곡선이라 6m 칸에 올리면 계단, 포팅량 큼. (작가는 Keir/ProbableTrain, Chris Kennelly 아님 — 내 전제 오류)
- **mxgmn/WaveFunctionCollapse** — MIT · C# · 25k★ — 타일 격자. 격자엔 맞지만 도로 연결 보장 없음.
- **BorisTheBrave/DeBroglie** — MIT · C# · 526★ — WFC + 제약(PathConstraint 로 도로 끊김 금지, FixedTile 로 바다·중심 고정). Unity DLL 로 바로. 인접 규칙 짜는 데 시간, 결과가 무작위할 위험.
- **mxgmn/MarkovJunior** — MIT · C# · 8.4k★ — "이 무늬를 저 무늬로" 규칙 반복. 도시 예제 없음, XML 학습 비용.
- **t-mw/citygen** — MIT · TS · 252★ — Parish&Müller 식 도로 성장(전체 목표+지역 제약). 핵심 루프 짧아 C# 포팅 150~200줄. 자유 각도라 격자 반올림 필요.
- josauder/procedural_city_generation(MPL, Python) 참고용. marian42·selfsame 의 Unity 3D WFC 는 과함. mapgen4(Apache) 는 해안선 아이디어만.

## 이 프로젝트용 Top 3 (40×25칸 안팎, 항구 가장자리·다운타운 중심·공업 외곽, 도로 전부 연결)
1. **직접 구현: 구역 → 블록 반복 이등분 → 끊긴 곳 잇기** (외부 리포 없음, 약 170줄, 하루)
   - ① zoneMask: 한쪽 끝 3~4줄 바다/항구, 중앙 원 다운타운, 반대편 공업 (40줄)
   - ② 큰 사각형을 반으로 자르기 반복, 자른 선이 도로. 다운타운은 3~4칸 블록까지, 공업은 8~10칸에서 멈춤 (60줄) — **"구역마다 블록 크기가 다르다"가 진짜 도시 느낌의 80%**
   - ③ 해안도로 1줄 + 대각 간선 1~2 (30줄)
   - ④ BFS 연결 확인, 끊긴 조각은 가장 가까운 도로로 직선 연결 + 기존 TrackConnectivityVerifier (40줄)
   - ⑤ buildingMask 는 구역표: 다운타운 H/C, 공업 F/W/T, 항구 W
2. **DeBroglie** — 문자 하나=타일, PathConstraint·FixedTile. 우리 쪽 연결부 50~100줄.
3. **citygen 포팅** — 도로 끝에서 줄기 뻗고 교차하면 붙임. 각도 0/90° 묶고 6m 반올림. 골목 느낌은 1보다 낫고 구역 제어는 직접.

## 결정 대기
- 어느 길로 갈지는 도현. 추천은 1(되돌리기 쉽고 연결·구역을 손에 쥠). 받은 데이터 사본: scratchpad `idx/` (세션 한정).


---

## 2차 조사 (같은 날 밤) — 릴스 DeFVMzjufBg = ProbableTrain/MapGenerator 심층 + 추가 OSS

**릴스 확인**: indexopensource.com 색인 X026 = MapGenerator(`https://github.com/ProbableTrain/MapGenerator`, LGPL-3.0, TS, 1.5k★, 마지막 실질 커밋 2022-01). 색인에서 도시 블록 배치를 다루는 항목은 이것 하나.

**작동(docs/algorithmoverview.md)**: ① 텐서장 — 지도 위 모든 점에 직각 화살표 한 쌍. 바둑판 필드(각도)와 방사형 필드(중심)를 여러 개 놓고 거리로 감쇠(`basis_field.ts`). ② 도로 = 화살표 따라 긋기(RK4 streamline, `streamlines.ts`), 간격 `dsep`(minor 20 / major 100 / main 400), 위계 main→major→minor. ③ 물: 노이즈 선 하나로 해안·강, 물 안은 텐서 0. ④ 블록·필지: 교차점 그래프 → "계속 우회전" 다각형(`polygon_finder.ts`) → 가장 긴 변 기준 반복 분할. ⑤ 내보내기 PNG/SVG/STL/Heightmap — **JSON·GeoJSON 없음**. 원 논문 Chen et al. SIGGRAPH 2008 "Interactive Procedural Street Modeling".

**우리 6m 격자에 쓰는 법**
- (a) 데이터만(추천, 1~2일): 로컬 빌드(`npm install`→`gulp`), `main.ts` 에 `window.main=new Main()` 한 줄 + `allStreamlines/roads/coastline/seaPolygon` JSON 덤프 버튼 → 파이썬: 6m 스냅 → Bresenham(대각은 4방향 2스텝) → 짧은 막다른 가지 제거 → BFS → buildingMask/zoneMask.
- (b) C# 포팅: 도로만 TS 약 1,260줄(`tensor.ts, basis_field.ts, tensor_field.ts, integrator.ts, grid_storage.ts, streamlines.ts, vector.ts`), 블록까지 +700, 물 +243. 의존 isect/jsts/simplify-js/d3-quadtree/simplex-noise 대체 필요.
- **라이선스**: 출력 데이터만 쓰면 의무 없음. 포팅 코드는 2차 저작물 — **Editor 전용 어셈블리에 두고 roadMask 만 굽으면 배포물에 안 들어감**.

**룩 주의(중요)**: 이 도구는 수 km 도시용. 우리 맵 180×132m 에선 도로 6~9줄, 곡선 효과 거의 사라짐. 4방향 셀 래스터화 → 30° 기운 길이 "ㄱㄴㄱㄴ" 지그재그 = 추적캠에서 2셀마다 90° 꺾는 운전. Kenney 도로 키트엔 curve/bend/roundabout/split 은 있고 **45° 대각 조각은 없음**. 완화: 각도 0/90 스냅 + 모서리만 curve, 방사형 중심엔 roundabout, 또는 스플라인 도로(RoadArchitect)로 전환(타일·BFS 재설계).

**추가 OSS(라이선스 확인)**: DeBroglie(MIT C#, 구역별 타일 규칙 WFC) · ranjian0/building_tools(MIT Blender 애드온, 창·문·발코니 로우폴리 건물, 2026-09 활동) · mchrbn/unity-traffic-simulation(MIT, 웨이포인트 AI 차량+신호, 2022 멈춤) · FritzsHero/RoadArchitect(MIT, 스플라인 도로·교차로, 2026-10 활동) · t-mw/citygen(MIT) · josauder(MPL) · magnificus/Procedural-Cities(MIT, UE4 참고) · mapgen4(Apache, 해안선). 제외: watabou TownGeneratorOS(GPL-3) · Buildify(라이선스 미확인) · roadcreator(아카이브) · 보행자 OSS 없음 · 도로 마킹은 URP Decal 로 충분.

**선택지 셋(공수는 추정)**
- ① 구불구불 지중해 항구(MapGenerator 데이터): 계단 운전 위험, 곡선 살리려면 스플라인 +4~6일.
- ② 구역마다 블록 크기가 다른 격자(자체 코드, 2~3일): 다운타운 3×3 촘촘·공업 6×5 큰 블록·항구 해안길+부두 가지. 타일·검증기 그대로.
- ③ 혼합(3~4일): 비스듬한 대로 2~3줄 + 원형 교차로만 텐서장/손그림, 나머지 ②.
클론 사본: scratchpad `mg/repo`(세션 한정).
