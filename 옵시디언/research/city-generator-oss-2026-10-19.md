# 도시 생성기 공개 구현 조사 — D-105 (2026-10-19, 기획팀)

> 도현 결정 D-104(첫 목표 = 마을 생성기 P2, "실제 도시처럼") · D-105(직접 구현 말고 기존 공개 구현 활용). 결정은 도현.
> [검산] = gh api / curl 로 오늘 직접 확인 · [페이지] = README·문서 본문 · [미확인] = 못 봄. 공수는 전부 **가설**(측정값 아님).
> 이전 조사 `city-layout-algorithms-2026-10-13.md` 와 겹치는 것(MapGenerator·DeBroglie·citygen·RoadArchitect)은 갱신만, 새 것 위주.
> 스크린샷 사본: `.ai_logs/osm/*.png` (roadgen.png · mapgen.png · bx_*.png). OSM 원본 응답: `.ai_logs/osm/amalfi.json`, `positano.json`.

## 0. 한 줄
"실제 도시처럼"을 문자 그대로 만족하는 길은 **진짜 아말피 해안 마을의 도로 데이터(OpenStreetMap)를 6m 칸에 눌러 담는 것**이고, 완전 절차 생성 중에선 **pboechat/roadgen(Unity C#, MIT, Parish&Müller + 지형 훅)** 이 우리 제약에 제일 가깝다. 둘 다 셀 크기(D-103)에 안 묶인다. 단, 진짜 아말피는 길의 8할이 계단이라 "그대로"는 차가 다닐 길이 60~100 조각뿐 — 보행로를 차도로 승격하는 게임 추상화 규칙이 필요하다.

## 1. 우리 제약 (판정 기준)
- 마을 = 6m 타일 격자(셀 크기는 D-103 축척 결정에 묶임 → **연속 좌표를 내놓는 후보에 가점**, 격자에 박힌 후보는 감점)
- 해안도로 = 스플라인(D-098) · 생성 규칙 셋(D-100: 언덕 안 막기 / 평지 우회로 / 꼭대기 랜드마크)
- `TrackConnectivityVerifier` 연결성(BFS, 고립 0) · 배달지 라벨 11 매핑 · 상업 배포 가능 라이선스 · Editor 전용으로 돌려 배포물에 코드 안 들어가게

## 2. 후보 (적합도 순)

### ① OpenStreetMap 실제 마을 데이터 → 6m 스냅 (osmnx MIT 또는 Overpass API 직접) — **추천 1**
- URL: https://github.com/gboeing/osmnx [검산 MIT · Python · push 2026-07-31 · ★5,876] · Overpass API https://overpass-api.de (라이브러리 없이 curl 한 줄) · C# 참고 구현 https://github.com/rwth-irt/osm2unity [검산 BSD-3 · C# · push 2026-05-11, 자전거 시뮬용 OSM→도로·건물·지형 Unity 패키지]
- 라이선스: 데이터 = **ODbL 1.0**. 상업 게임 사용 가능, **출처 표기 필수**("© OpenStreetMap contributors"). 우리가 만든 roadMask 는 "파생 데이터베이스"라 배포 시 ODbL 로 공개해야 하지만, 게임 빌드(Produced Work)는 자유. 타일 이미지는 쓰지 않는다. (osmfoundation.org/wiki/Licence)
- 오늘 실측 [검산, Overpass 응답 파일 보관]:
  - **Amalfi** 1.1×1.7km 상자: 길 566 조각. 차도 = primary 36 · residential 29 · service 22 · living_street 6 · secondary/tertiary 8 · unclassified 3 ≈ **104**. 비차도 = steps **184** · footway 208 · pedestrian 42 · path 28 = **462 (82%)**.
  - **Positano** 1.1×1.7km 상자: 길 372 조각. 차도 ≈ 60 (primary 28 · tertiary 12 · service 10 · residential 6 · unclassified 4). 비차도 = steps 128 · footway 111 · pedestrian 38 · path 33 = 310 (83%).
  - 뜻: 실제 아말피형 마을은 **차가 다니는 길이 해안도로 1줄 + 골목 몇 개**뿐. 배달 게임으로 쓰려면 pedestrian·footway 를 "좁은 차도"로 승격하고 steps 는 버리는 **승격 규칙**이 필요. 그래도 길 모양·광장·해안도로 위계·마을 윤곽은 진짜다.
- Unity 적용 경로: Python(또는 curl) 오프라인 → GeoJSON → 6m 스냅 + Bresenham(대각은 4방향 2스텝) + 짧은 막다른 가지 제거 → roadMask/zoneMask → 기존 설치기·검증기 그대로. Editor 밖 파이프라인이라 배포물 無.
- "실제 도시" 근거: 데이터 자체가 실제 도시. 스샷 불필요.
- 제약 적합: 셀 크기 독립(스냅 단계에서 결정) ✅ · 연결성은 스냅 뒤 BFS 로 보장(끊긴 조각 직선 연결) ✅ · 해안도로는 OSM primary(SS163) 폴리라인을 스플라인 입력으로 바로 ✅ · **고도는 OSM 에 없음** → Copernicus GLO-30 DEM(무료, 30m) 별도 또는 손 고도맵 · D-100 규칙 셋은 생성이 아니라 **선별**(언덕 우회로가 있는 구간을 고른다)로 만족
- 공수(가설): 파이프라인 1~2일 + 승격 규칙·배달지 자리 손보기 1일. 위험: 실제 지형은 우리 6m 칸·평지 가정과 안 맞는 곳(헤어핀·계단)이 많아 **"실제처럼 보이되 실제는 아닌" 재배치**가 결국 필요.

### ② pboechat/roadgen — Unity C#, Parish&Müller 확장 L-system + 지형 인터페이스 + 필지 — **추천 2**
- URL: https://github.com/pboechat/roadgen [검산 MIT · C# · push 2024-03-02 · ★94 · 프로젝트 Unity **5.3.5f1**(2016)]
- 내용 [페이지]: Müller 2001 기반, 교차·스냅(self-sensitivity), 인구밀도 이미지맵, 큰길/골목 위계, 쿼드트리, **ITerrain 인터페이스**(지형 비용 훅 → D-100 "언덕 비용" 넣을 자리), IAllotmentBuilder(필지→건물 자리), GlobalSeeder(결정적). 출력은 엔진 내 세그먼트·메시(파일 export 없음 → 우리가 세그먼트를 덤프).
- "실제 도시" 근거: `roadgen.png` [직접 봄] — 방사형 중심 + 격자 골목 + 외곽 간선 곡선, 밀도 따라 블록 크기 달라짐. Unity-native 후보 중 가장 도시답다. 단 평지 대도시 룩.
- 제약 적합: 연속 좌표 → 셀 독립 ✅ · 6m 스냅 필요(①과 같은 래스터 단계 공유) · 지형 훅 ✅ · 연결성은 L-system 이 스냅으로 대부분 보장, BFS 로 마감
- 공수(가설): Unity 6 컴파일 맞추기 0.5~1일(순수 C#, 미검증) + 우리 설치기 연결·래스터 1~2일 = 2~3일. 위험: 2016 코드, 커밋 13개, 이슈 1 — 유지보수 없음. 작은 마을(40×25칸)에선 파라미터 튜닝이 곧 공수.

### ③ Bixio999/ProceduralCityGenerator — Unity C#, Parish&Müller + CGA 건물 문법 + 펄린 지형
- URL: https://github.com/Bixio999/ProceduralCityGenerator [검산 MIT · C# · Unity **2021.3.2f1** · push 2022-10-04 · ★16 · 대학 과제]
- 내용 [페이지]: 확장 L-system 도로 + CGA 셰이프 문법 건물(오피스·주택 3변형) + 펄린 지형·인구맵 + 시드.
- "실제 도시" 근거: **없음** — README 그림은 논문 도식과 건물 매스뿐(`bx_*.png`, `bixio1/2.png` 직접 봄), 도시 출력 스샷이 없다 [미확인].
- 제약 적합: ②와 같음. Unity 버전이 가까워 컴파일은 더 쉬울 것(가설).
- 공수(가설): 2~3일. ②보다 스샷 증거가 없어 뒤.

### ④ ProbableTrain/MapGenerator — 텐서장 도로 + 블록 + 건물 (데이터만 쓰기)
- URL: https://github.com/ProbableTrain/MapGenerator [검산 **LGPL-3.0** · TypeScript · push 2024-05-09 · ★1,474]
- "실제 도시" 근거: `mapgen.png` [직접 봄] — 해안선·방사형 광장·격자/사선 혼합·건물 바닥까지. **스샷으론 가장 진짜 도시.**
- 제약 적합: 연속 좌표(셀 독립 ✅) · JSON export 없음 → 로컬 빌드에 덤프 한 줄 추가(이전 조사) · km 급 도시용이라 **180m 마을에선 곡선 효과가 사라지고** 4방향 셀에서 지그재그 운전(이전 조사 "룩 주의") · LGPL 은 데이터만 쓰면 의무 없음
- 공수(가설): 1~2일(데이터 경로). 작은 마을엔 과함.

### ⑤ watabou Village Generator / Medieval Fantasy City Generator — GeoJSON export 만 쓰기
- URL: https://watabou.github.io/village.html · 코드 https://github.com/watabou/TownGeneratorOS [검산 **GPL-3.0** · Haxe · push 2021-01 · ★1,944, 옛 버전]
- 라이선스: 코드 GPL 이지만 **생성된 지도는 상업 포함 자유 사용**(itch 페이지 명시, 출처 표기 권장). 작가 말: "MFCG·VG·NG 세 생성기가 같은 JSON(건물·도로·나무)을 export" [페이지]. VG 는 지형·고도(hachure)·다리·잔교 있음. JSON 에 고도가 들어가는지는 [미확인].
- "실제 도시" 근거: 중세 유럽 유기적 마을. 길 먼저 → 집 띄엄띄엄 → 밭·나무. 아말피 비탈 마을과 결이 가깝다(사진은 itch 페이지).
- 제약 적합: 연속 좌표 ✅ 셀 독립 ✅ · 연결성 BFS 마감 · 사람이 브라우저에서 시드 고르고 export 하는 **수동 한 번**(자동 재생성 불가 — "수동 왕복은 버그" 규칙과 충돌하나 1회성)
- 공수(가설): GeoJSON 파서+스냅 1일. 가장 싸다.

### ⑥ BorisTheBrave/DeBroglie — 제약 WFC (격자 네이티브)
- URL: https://github.com/BorisTheBrave/DeBroglie [검산 MIT · C# · push 2025-12-21 · ★527]
- 내용 [문서]: ConnectedConstraint(모든 길 연결 보장) · **LoopConstraint(두 갈래 이상 — D-100 ② "우회로 하나" 와 Alexander 순환 고리를 제약으로 박을 수 있음)** · FixedTile(바다·광장 고정) · Count · Border. 경로 제약은 무겁고 백트래킹 필요.
- "실제 도시" 근거: 없음 — 타일 통계일 뿐 위계·광장·중심이 안 생긴다. 규칙 설계가 곧 공수.
- 제약 적합: 6m 격자에 바로 ✅ 연결성 내장 ✅ · **셀 크기에 묶임**(감점) · 해안도로 스플라인과 별도
- 공수(가설): 1~2일. ①~⑤ 결과를 "채우는" 보조 도구로는 가치 있음(빈 블록 소품·필지 변형).

### ⑦ FritzsHero/RoadArchitect — 스플라인 도로·절차 교차로 (마을 배치 아님, 해안도로 P1 용)
- URL: https://github.com/FritzsHero/RoadArchitect [검산 MIT · C# · push **2026-10-08** · ★364 · 개발 Unity 2020.3, "ongoing but slow"]
- 내용 [페이지]: 스플라인 메시 압출, 차선·폭·각도로 교차로 자동, 다리 곡률. 코드 API 는 문서에 없음(에디터 창 워크플로) [미확인].
- 자리: D-098 해안도로의 "보이는 도로" 층. 마을 배치 후보가 아니라 여기 적어 둔다.

### ⑧ t-mw/citygen — Parish&Müller 류 도로 성장 (TS, 작음)
- URL: https://github.com/t-mw/citygen [검산 MIT(LICENSE 본문) · TypeScript · push 2026-04-08 · ★252] · 데모 http://tmwhere.com/city_generation.html · Godot 포트 있음.
- 이전 조사와 같음: 핵심 루프 짧아 C# 150~200줄 포팅. ② roadgen 이 이미 Unity C# 이라 우선순위 뒤.

## 3. 제외 (이유 한 줄)
- Obsidian-TTRPG-Community/Town-Forge [MIT · TS · push 2026-10-18] — 설정은 좋으나 **export 가 PNG 뿐**, 도로 폴리라인 없음.
- JasperMoens/VillageGenerator — 저장소 **비어 있음** [검산].
- jmespadero/cityGen — CC BY-NC-SA, 상업 불가.
- vvoovv/blosm — GPL-3(release 브랜치) · OSM→Blender 임포트. ①의 Blender 경로로는 가능하나 osmnx/Overpass 가 더 단순.
- mapbox/mapbox-unity-sdk — v2 개발 중단, Unity 6 "best-effort".
- mirrorfishmedia/ImaginaryCities [MIT · 2019] — 순수 격자, "실제 도시" 아님. MeghaS94 [MIT · 2018 · ★1], cybercritic [2016] — 사실상 죽음.
- Syomus/ProceduralToolkit [MIT · push 2024-04 · ★2,910] — 건물 생성 라이브러리, 배치 아님(D-106 아트 경로 참고용).
- magnificus/Procedural-Cities [MIT · C++ UE4] — 참고용.
- gregoryneal/Cigen [라이선스 없음] · BCuracao/Unity-City-Generator [라이선스 없음 · 2019].

## 4. 비교 (핵심 축만)
| 후보 | 라이선스 | Unity 경로 | 셀 독립 | "실제 도시" 증거 | 공수(가설) |
|---|---|---|---|---|---|
| ① OSM 실제 데이터 | ODbL(출처표기) | 오프라인→roadMask | ✅ | 실제 그 자체 | 2~3일 |
| ② roadgen | MIT | C# 그대로(Unity5→6) | ✅ | 스샷 ○ | 2~3일 |
| ③ Bixio999 | MIT | C#(2021.3) | ✅ | 스샷 ✗ | 2~3일 |
| ④ MapGenerator | LGPL(데이터만) | TS 덤프→roadMask | ✅ | 스샷 ◎(대도시) | 1~2일 |
| ⑤ watabou JSON | 지도 자유/코드 GPL | GeoJSON→roadMask | ✅ | 중세 마을 ○ | 1일 |
| ⑥ DeBroglie | MIT | C# DLL | ✗(격자) | ✗ | 1~2일 |

## 5. 추천 (기획팀 의견, 결정은 도현)
- **1순위 ①(OSM 실제 데이터)**: 이유 (a) "실제 도시처럼"을 추측이 아니라 실제 아말피·포지타노 길로 만족 (b) 셀 크기·축척(D-103)에 안 묶이고 Editor 밖에서 돌아 배포물에 코드가 없다 (c) 라이선스가 가장 명확. 전제 경고: **차도가 60~100 조각뿐**이라 보행로 승격 규칙이 설계의 핵심이 되고, 고도는 DEM 을 따로 붙여야 한다.
- **2순위 ②(roadgen)**: 이유 (a) 완전 절차라 마을 2개·시드 재생성이 공짜 (b) ITerrain 훅으로 D-100 언덕 규칙을 생성 안에 넣을 수 있음 (c) Unity C# MIT 라 포팅 없음. 전제 경고: 2016 코드·유지보수 없음, 작은 마을 파라미터 튜닝이 공수.
- 섞는 안: ①로 첫 마을(항구)·해안도로 뼈대를 실제 데이터로 깔고, ②로 두 번째 마을(언덕 위)을 생성 → 두 방식 비교 그림을 도현이 본다. D-097 때 "둘 다 만들어 비교"가 통했다.
- ⑤ watabou 는 **하루짜리 보험**: ①②가 막히면 export 한 장으로 즉시 마을 한 개.

## 6. 도현 결정 요청
1. 실제 데이터(①) vs 절차 생성(②) vs 둘 다 비교.
2. ①이면 OSM 출처 표기(크레딧 화면)와 roadMask ODbL 공개 — 받아들일지.
3. ①이면 어느 마을: Amalfi(차도 104, 평지 항구 있음) / Positano(차도 60, 거의 전부 비탈) / 다른 곳.

## 7. 한계
- 공수는 전부 가설. roadgen·Bixio999 가 Unity 6 에서 컴파일되는지 **안 돌려봄**.
- watabou VG JSON 의 고도 포함 여부, RoadArchitect 코드 API 유무 미확인.
- Overpass 는 1.1×1.7km 상자 하나씩만 셌다(마을 중심). 상자를 키우면 숫자가 변한다.
