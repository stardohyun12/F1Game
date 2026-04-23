# 맵 디자인 리서치 (세션 11 — 2026-04-23)

> 3 방향 병렬 웹조사 결과. 업계 포스트모템 / 학술 이론 / 아트 디렉션.
> 놀라운 수렴성 — 3개 전부 동일한 프레임(**Lynch 5요소 + 랜드마크 가시성 + 구역 분할**)으로 귀결.

---

## 0. 공통 결론 (3 조사 수렴)

| 명제 | 업계 근거 | 학술 근거 | 아트 근거 |
|---|---|---|---|
| **랜드마크 부재가 현재 맵의 1차 문제** | Burnout "모든 코너에 게임플레이" / Forza 바이옴당 시그니처 | Lynch 5요소 / decision point 앞 랜드마크 기억률 최고 | 스카이라인 위계가 도시감의 80% |
| **구역(district) 분할 미정의가 2차 문제** | Forza 11 바이옴, Burnout 5 디스트릭트 | Parish-Müller 밀도 구배 원칙 | 용도별 군집이 단조로움 해소 |
| **양식화(stylized)가 인디의 유일한 선택지** | Crazy Taxi·Hotshot Racing·Art of Rally | — | 사실적 방향 = 에셋 양 싸움 (0원 불가) |
| **타일 배치 레이아웃은 "정보 설계"이지 폴리싱 아님** | — | Totten "non-digital prototyping" | — |

**→ CLAUDE.md 규칙 3("비주얼 폴리싱은 4주차 이후")의 예외 근거 확보.** 맵 구조(Lynch 5요소)는 지금 결정해야 함. 포스트프로세싱 값은 4주차까지 파킹해도 OK (하지만 30분짜리 설정은 ROI 압도적).

---

## 1. Lynch 5요소 PIT CREW 체크리스트

Kevin Lynch *The Image of the City* (1960) — 도시 인지의 표준 프레임. Wessel (2011)이 디지털 도시에도 유효함을 검증. Jesus Machina "5 Ingredients of Game Town Design"에서 게임 타운에 직접 번역.

| 요소 | 현재 맵 상태 | 강화 방법 (13×9 타일 내) |
|---|---|---|
| **Path** (주행 경로) | 도로 타일 = 존재. 전부 동일 스타일. | 주도로 1~2개를 시각 차별화 (2차선 확장 or 노면 색). 골목과 위계 분리. |
| **Edge** (경계) | 없음 — 맵이 "끝" 없이 페이드아웃. | 외곽을 강/철로/절벽/해안 타일로 둘러 "끝" 명시. 5~15분 런에서 "돌아갈 곳" 감각. |
| **District** (구역) | 미정의 — 건물이 무작위 섞임. | 3~4구역 분할 (다운타운/상업/교외/공업). 구역별 건물 **팔레트 분리**만으로도 효과 대부분. |
| **Node** (결절) | 교차로만 존재, 기능 없음. | 중앙 광장/로터리 1개를 픽업 허브로 지정. 주문보드 위치와 물리적으로 일치. |
| **Landmark** (랜드마크) | 없음. skyscraper 5종 있지만 미활용. | 주요 교차로 3~5개에 고유 랜드마크 1개씩. **decision point 앞에 있을 때 기억률 최고** (학술). |

---

## 2. 업계 포스트모템 인사이트

### Burnout Paradise (Criterion)
- "모든 코너에 게임플레이" — 교차로 자체를 콘텐츠화 (스매시 게이트·이벤트).
- 이벤트 시작점은 분산, **종점은 8개로 집중** → 플레이어가 맵을 학습하고 내비게이션이 경쟁 우위.
- "If you can see it, you can drive it" (Big Surf Island 원칙).

**PIT CREW 적용:** 배달 종점을 4~6곳으로 고정 → **경로 학습이 곧 전략** (USP ② 결정 밀도와 직결).

### Forza Horizon 5 (Playground Games)
- 11 바이옴 = 한 국가에 세계 압축. 바이옴당 시그니처 랜드마크.
- 입지 선정 4대 기준: 상징 도로·생태계·차 문화·계절.

**PIT CREW 적용:** 11은 과함. **3~4 마이크로 바이옴**으로 축약 — 다운타운(skyscraper 5종) / 상업 / 교외 / 항만.

### Crazy Taxi
- 아케이드판 = 루프 구조 (길 헤매지 않고 쉴 새 없이).
- 위치·목적지 고정, 등장만 랜덤.
- 팁 = 스턴트 → 고정 경로 안에서 **표현의 자유도**가 반복 플레이 지탱.

**PIT CREW 적용:** 가장 직접적. 주문보드가 Crazy Taxi의 "고정 아쉬움" 한계를 극복. 픽업/드랍 지점 고정 + 조합 랜덤.

---

## 3. 학술 핵심 발견

### Spatial Cognition (Spence & Feng 2010, Brunec et al. 2023)
- 반복 플레이 = **egocentric(순서 암기) → allocentric(전역 지도)** 전환. 2~3회차에 "머릿속 지도" 형성.
- **GPS/미니맵 과의존은 allocentric 형성 방해** (Nature Sci Rep 2022 VR 드라이빙).
- 랜드마크는 **교차로 직전** 위치에서 기억률 최고.

**PIT CREW 적용:**
- 미니맵을 구역 경계만 표시, 세부 도로 숨김 → 플레이어가 맵 학습하도록 설계.
- 모든 주요 교차로에 랜드마크 1개 (Lynch Node+Landmark 결합).

### Procedural City (Parish & Müller 2001)
- 순수 격자는 "도시감" 안 남. **밀도 구배**가 핵심 — 중심부 블록 작고 외곽 큼.
- 블록 크기 2~3종 섞기 권장.
- 교차로 유형 다양성(T·4방·로터리)이 경로 계획 의미 부여.

**PIT CREW 적용:** 13×9 중앙 3×3을 "downtown"(작은 블록·고밀도·랜드마크), 외곽을 큰 블록(공업/창고, 긴 직선). **5분 런은 downtown 내부, 15분 런은 외곽 왕복**으로 자연 분리.

### Architectural Level Design (Totten 2018)
- **Sightline 우선**: 진행 방향 랜드마크 1개 이상 항상 보임.
- **Affordance**: 도로 폭·노면 색이 속도 암시 (좁은 골목 = 감속).
- Non-digital prototyping → 종이로 시선축 검증 후 엔진.

---

## 4. 아트 디렉션 — 즉시 적용 액션 (ROI 순)

| 순위 | 작업 | 시간 | 효과 |
|---|---|---|---|
| 1 | **URP Global Volume** — Bloom(0.8, thr 0.9) + Vignette(0.3) + Tonemapping ACES + Color Adjustments(post-exp +0.3, Sat +10) | 30분 | 스크린샷이 바뀜. |
| 2 | **Directional Light 변경** — 각도 (50,-30,0), warm #FFE4B5, Intensity 1.3. Ambient Gradient 모드. | 20분 | 입체감/무드. |
| 3 | **Fog 켜기** — Linear, 70~220m, 색을 Skybox 하단과 동일. | 15분 | 맵 끝 격자 티 사라짐. |
| 4 | 건물 변이 3종 — Y-yaw 랜덤 + scale.y 지터(0.9~1.3) + MaterialPropertyBlock 색 시프트 | 1~2시간 | 동일 프리팹 반복 티 감소. |
| 5 | 디테일 프롭 pass — 가로수/가로등/벤치 인도 타일당 1개 | 반나절 | 도시감의 80%. **4주차로 파킹 권장.** |

**레퍼런스 타이틀:** Art of Rally / Hotshot Racing / Inertial Drift / Race Condition.

---

## 5. PIT CREW 권고 — 세션 11 스코프

### 채택 (지금 진행 가능)
**"Lynch 5요소 완성 + 3~4 구역 밀도 구배 + 랜드마크 3~5개"** — `TrackLayout` SO 재배치로 달성 가능. 신규 시스템 X.

### 채택 (5분 설정)
**URP Volume + Fog + Lighting 프리셋 1종** — 30분 세팅. CLAUDE.md 규칙 3 예외 근거: 조사 결과가 "타일 배치와 동일한 ROI"라 명시.

### 파킹 (4주차 이후)
- 디테일 프롭 pass (가로수/가로등 인도 배치)
- 건물 높이 지터·컬러 변이 (`BuildingPlacer` 확장)
- 미니맵 allocentric 설계
- 주도로/골목 위계 (타일 메타데이터 확장 = 세션 7 시행착오 재발 위험)

---

## 6. 출처

### 업계
- [Game Developer: Burnout Paradise Legacy](https://www.gamedeveloper.com/design/devs-reflect-on-the-impact-and-legacy-of-i-burnout-paradise-i-)
- [Top Gear: Forza Horizon Map Design](https://www.topgear.com/car-news/gaming/secrets-behind-a-forza-horizon-world-map-playground-games-spills-all)
- [Crazy Taxi — Wikipedia](https://en.wikipedia.org/wiki/Crazy_Taxi_(video_game))
- [The Level Design Book — Wayfinding](https://book.leveldesignbook.com/process/blockout/wayfinding)

### 학술
- [Parish & Müller 2001 — Procedural Modeling of Cities (PDF)](https://cgl.ethz.ch/Downloads/Publications/Papers/2001/p_Par01.pdf)
- [IAC: Making Sense of a Place — Lynch](https://www.theiaconference.com/sessions/making-sense-of-a-place-lessons-from-kevin-lynchs-the-image-of-the-city-and-urban-design/)
- [ArtStation: 5 Ingredients of Game Town Design](https://www.artstation.com/blogs/jesus_machina/RL1vR/02-the-5-ingredients-of-game-town-design)
- [Spence & Feng — Video Games and Spatial Cognition](https://journals.sagepub.com/doi/10.1037/a0019491)
- [Nature Sci Rep — GPS in VR driving](https://www.nature.com/articles/s41598-022-11124-9)
- [Routledge: Totten — Architectural Approach to Level Design 2nd ed.](https://www.routledge.com/Architectural-Approach-to-Level-Design-Second-edition/Totten/p/book/9780815361367)

### 아트
- [GamingBolt: Art of Rally Interview](https://gamingbolt.com/art-of-rally-interview-art-style-development-and-more)
- [80lv: Stylized Low-Poly 3D Scenes in Unity](https://80.lv/articles/learn-to-create-stylized-low-poly-3d-scenes-in-unity-with-this-environment)
- [LMHPOLY: URP Lighting & Post-Processing](https://www.lmhpoly.com/tutorials/unity-urp-tutorial-lighting-and-post-processing)
- [NatureForge URP Post Processing Devlog](https://emaceart.itch.io/free-low-poly-meadows/devlog/1449692/devlog-1-unity-demo-improvements-urp-post-processing)
