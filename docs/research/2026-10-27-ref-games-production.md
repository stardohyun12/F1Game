# 기준 게임 4개는 아트를 실제로 어떻게 만들었나 (리서치, 2026-10-27)

대상: ①A Short Hike · ⑥Easy Delivery Co.(EDC) · ⑫Crow Country · ⑦Jalopy.
근거: 개발자 본인 인터뷰·발표·devlog 녹취(1차). 기사·위키는 보조. 출처 번호 [n]은 맨 아래.
추정은 **[추정]**, 스샷 관찰은 **[관찰]**(`art/reference/dotlp-2026-10-27/*_1.jpg`).

> 정정: EDC 개발자는 "sgtoilet"이 아니다. **Sam C.(Sam Cameron, 닉네임 Sweaters)**, 캐나다 1인 개발, 퍼블리셔 Oro Interactive [2][5][6].

---

## 0. 한눈에

| | ① A Short Hike | ⑥ EDC | ⑫ Crow Country | ⑦ Jalopy |
|---|---|---|---|---|
| 만든 사람 | Adam Robinson-Yu 1인(프로그래머 출신). 모델 일부는 파트너 Dawn Blair·친구 Andrew Wong [9][10] | Sam C. 1인(프로그래머, 2D 픽셀 아트 경험) [2][4] | Adam Vian 1인(아티스트·디자이너). 나중에 형 Tom이 기술 지원 [12][13] | Greg Pryjmachuk 1인(Codemasters F1 디자이너 출신) [20][21] |
| 기간 | 2018-12 아트 실험 → 3개월 마감(2019-04) → 7월 Steam [1][11] | 졸업 뒤 시작 → 데모 2025-04 → 출시 2025-09 [5] | 2020년 시작, 약 4년(2024-05 출시) [14] | 2014 말 시작 → EA 2016-04 → 1.0 2018-03 [19][21] |
| 엔진 | Unity [10] | Unity [2] | Unity [12] | Unity [19] |
| 모델링 도구 | 직접 모델링(도구명 미공개). 지형은 Unity 기본 터레인 [1][10] | **Blockbench**(마인크래프트용 로우폴리 툴) [2][4] | **ProBuilder**(Unity 안). 처음엔 기본 도형만 썼다 [12][15][16] | 미확인 |
| 텍스처 | **팔레트 텍스처**: UV를 색 칸에 붙임. 세부 그림이 거의 없음 [1] | 탈것·상자·캐릭터 = **손 픽셀아트**, 환경 = **실사진 포토배싱** [2] | 대부분 물체에 텍스처. 직접 그림(방식 세부는 미공개) [17][18] | 평면 색 위주, 텍스처 최소 [관찰]·[22](작성자 미확인) |
| 렌더 해상도 | 작은 렌더 텍스처 → Point 확대. 크기는 옵션 [1][3] | **456×256** 고정 → 화면 크기로 확대 [2][7] | 카메라 셰이더가 픽셀을 줄이고 여러 왜곡 [12][16] | 일반 해상도 [관찰] |
| 빛 | 직접 쓴 **거의 unlit 셰이더** + 계단형 램프 [1] | **그림자·태양 없음**, 앰비언트 한 색 [2] | 어두운 실내, 점광 [관찰] | 평면 셰이딩 [22][관찰] |
| 안개 | 원경 소음 줄이려 안개 [1] | 안개색 = 앰비언트색 [2] | 그리기 거리 안개 언급 [13] | 하늘색 높이 안개 [관찰] |
| 포스트 | 엣지 검출(실루엣) + 색 보정(그림자에 파랑) [1] | SSAO 약간, 디더 셰이더, CRT 옵션 [2][4][7] | 복합 카메라 셰이더 하나 [12] | 없음 [관찰] |
| 레벨 | 손 조각 터레인 + 트라이플래너 자동 텍스처 [1][11] | 손 배치. 첫 프로토 맵 배치가 최종과 거의 같음 [4] | 손 모델링한 방들(ProBuilder) [15] | **절차 생성**: 타일 세트로 경로를 고름 [19] |

---

## 1. A Short Hike (Adam Robinson-Yu)

| 항목 | 내용 | 출처 |
|---|---|---|
| 출발점 | "그림·모델링 실력이 부족하다. 대신 VFX·포스트 경험이 있어서 큰 픽셀로 작은 세계를 그려 보기로 했다." | [1] |
| 픽셀 방식 | 세계를 **작은 렌더 텍스처**에 그리고, 둘째 카메라가 그것을 **Point 필터로 확대**. GB Camera 플러그인에서 배움. AA 없음 | [1][3] |
| 픽셀의 효과 | "모델을 단순하게, 더 많이, 더 빨리 만들 수 있었다. 세부는 플레이어 상상이 채운다." | [1] |
| 팔레트 | 캐나다 순상지 **가을 사진에서 색을 직접 뽑아** 팔레트를 만듦 | [1][11] |
| 셰이더 | **거의 unlit 커스텀 셰이더**. 빛 램프를 계단 모양으로 만들고 전체를 흰쪽으로 올려 원래 팔레트 색에 가깝게. "그림자·텍스처가 평평하지 않으면 픽셀 화면이 지저분해진다." | [1] |
| 텍스처 | UV를 **팔레트 텍스처의 색 칸**에 펼쳐 붙임 → 텍스처 그리기 시간 절약, 팔레트 교체로 색 실험 | [1] |
| 포스트 순서 | 맨 화면은 원경이 "지저분" → ①안개 ②엣지 검출(원경 실루엣) ③색 보정(그림자에 파랑). 엣지 효과는 필터 실험 중 우연히 발견 | [1] |
| 지형 | Unity 기본 터레인으로 조각. **트라이플래너 셰이더**가 경사에 따라 텍스처 자동 선택. 해변·길은 터레인 페인트, 경계는 날카롭게 수정 | [1][10] |
| 레벨 구성 | 움직임을 시험하며 언덕을 즉흥으로 조각 → 랜드마크 목록(실제 가 본 곳: Mount Pilchuck 화재 감시탑) → 고도별 구역 지도 | [11] |
| 인력 | 대부분 혼자. 일부 모델은 Dawn Blair·Andrew Wong. 음악 Mark Sparling | [9][10] |
| 기간 | 아트 방향은 2018-12에 끝냄. 펀딩 뒤 **3개월**에 출시(알파·베타·RC 월 단위) | [1][8] |
| 재사용 | 이전 RPG 에셋·스크립트·툴(Yarn Spinner, Cinemachine, InControl) 재사용 | [1][10] |

**[관찰]** ①_1: 텍스처 무늬는 거의 없다. 넓은 색면 + 2~3단 명암 + 부드러운 외곽선. 디테일은 소품 수(통나무·돌·울타리·표지판·꽃)에서 나온다.

## 2. Easy Delivery Co. (Sam C.)

| 항목 | 내용 | 출처 |
|---|---|---|
| 출발점 | "아티스트보다 프로그래머다. 내가 할 수 있는 것, 가장 빠른 것 안에서 일했다. 그래야 코딩으로 돌아갈 수 있다." | [2] |
| 형상 원칙 | "**폴리곤은 최대한 적게, 디테일은 텍스처로.**" 변형품 만들기도 쉬워짐 | [2] |
| 도구 | **Blockbench**로 모델링+텍스처(픽셀 텍스처를 모델 위에 바로 그림). 환경은 **Photoshop 포토배싱**, 최근엔 Aseprite로 전환. Aseprite로 게임 속 텍스처를 즉석 수정 | [2][4] |
| 사진 텍스처 (위키 주장 검증) | **사실.** 본인 발언: "트럭·상자·캐릭터는 귀여운 픽셀아트 텍스처, **환경은 실제 사진 텍스처**." 대비가 아늑함을 살린다고 함 | [2] |
| PSX 에셋 팩 사용? | **증거 없음.** 본인이 모델·텍스처 거의 전부를 했다고 말함("all the 3D modeling, or at least most") | [4] |
| PSX 필터 | 상용 필터 언급 없음. 직접 쓴 "작은 디더 셰이더" + 저해상도 렌더 | [4] |
| 해상도 | "만들어 낸 **456×256** 화면에서 읽혀야 한다." 내부 해상도 456×256 → 업스케일(유저 확인·불만 스레드) | [2][7] |
| 빛 | 그림자·햇빛 없음. **앰비언트 한 색**이 시간대로 변함. **안개 = 같은 색** → 흰 눈은 배경에 저절로 녹음. 입자는 앰비언트보다 조금 밝게/어둡게 | [2] |
| 명암 | "물체 명암은 **텍스처 안에 그려 넣는다**." SSAO만 살짝 | [2] |
| 제약 철학 | "오래전 가상의 엔진에서 만든다고 상상." PICO-8 같은 자기 제약 | [2] |
| 작업 순서 | 기능마다 종이 설계 → 바로 모델·텍스처 → 구현. **처음부터 완성도 있게**, 형상은 거의 다시 안 고침, 고칠 건 텍스처에서 | [2] |
| 레퍼런스 | 체계적 수집 거의 없음. 외출 때 사진, 빠른 이미지 검색, "감" | [2] |
| 캐릭터 | 팔다리 없는 모델 → 리깅 없이 Unity에서 스케일·이동으로 애니. 고양이 텍스처를 NPC가 공유 | [4] |
| 레벨 | 첫 프로토 맵 배치가 최종 Mountain Town과 거의 같음 | [6] |
| 인력 | 1인. 이식은 외부 팀 | [2] |

**[관찰]** ⑥_1: 건물은 거의 상자. 벽·창은 사진에서 온 부드러운 얼룩. 화면 블록이 크다(≈4px/블록 @1080). 하늘·원경이 안개 한 색. 전선·전신주가 구도를 나눈다.

## 3. Crow Country (Adam Vian, SFB Games)

| 항목 | 내용 | 출처 |
|---|---|---|
| 출발점 | 취미 프로젝트. 6개월간 형에게도 안 알림. 팬데믹 공백에 약 1년 혼자 | [16] |
| 모델링 | "**Unity 안 ProBuilder로 전부 모델링.**" 처음엔 모델링 툴 없이 **구·캡슐·원기둥·큐브 기본 도형**만 → 주인공이 장난감처럼 보이는 이유 | [12][15][16] |
| 가장자리 | "PS1 게임처럼 **버텍스 스무딩**을 많이 써 모서리를 부드럽게." (Reddit, SFB 계정 SFBDim) | [15] |
| 핵심 기법 | **카메라에 복합 셰이더 하나**: 픽셀을 줄이고 여러 왜곡. **모든 모델링·텍스처를 이 셰이더를 켠 채로** 작업 → 최종 모습을 늘 보며 만듦 | [12] |
| 우연한 발견 | 환경에 "디테일을 잔뜩" 넣은 뒤 픽셀 셰이더를 켜니 **FF7 프리렌더 배경처럼 보였다** → 그 뒤 USP로 고정 | [16][17] |
| 텍스처 | "환경은 꽤 디테일하고 **대부분 물체에 텍스처**가 있다. 카메라 필터와 합쳐 프리렌더 느낌." 본인: "모델링은 쉽고 빠르다. **텍스처링이 가장 어렵고 지루하다.**" | [17][18] |
| 텍스처 출처 | 사진/손그림 여부는 1차 자료에서 못 찾음 **[미확인]** | — |
| 인력·기간 | 2020~2024, 사실상 1인(아트·디자인·Playmaker 비주얼 스크립트). Tom = 저장·최적화·현지화. 음악 Ockeroid | [12][13][14] |
| 화면 구성 | 실내 공간이 **검은 허공에 뜬 상자 디오라마**(FF7에서 가져옴) | [13] |

**[관찰]** ⑫_1: 한 화면에 표지판 4·드럼·콘·캔·하수구·차 2대. 표지판 글씨가 읽히는 정도의 텍스처. 화면 블록은 작고 색 번짐(필터)이 있다.

## 4. Jalopy (Greg Pryjmachuk, MinskWorks)

| 항목 | 내용 | 출처 |
|---|---|---|
| 인력 | 1인("works alone"). Codemasters F1 디자이너(2009~2014) 출신. 아티스트(Ruta)는 다음 작품부터 합류 | [20][21][23] |
| 기간 | 2014 말 시작, 개발 14~15개월째 EA 준비, EA 2016-04, 1.0 2018-03 | [19][21][24] |
| 레벨 | **절차 생성 경로**. 경로는 **타일 세트**에서 고르고, 생성 때 AI·노면·짐·**건물 배치**가 정해짐. 마을엔 모텔·딜러·시장이 늘 있음 | [19] |
| 지역 차이 | 나라마다 상품 브랜드·주유소 모양을 바꿔 구분 | [23] |
| 아트 방식 | "Killer7 같은 스타일, **평면 색 셰이딩과 최소한의 텍스처**, 저충실도라 빨리 넣을 수 있었다." | [22] — 검색 요약만 확인, 본문·작성자 미확인 **[추정]** |
| 비평 | 레벨 디자이너 Robert Yang: 일부 벽·길이 "특징 없이 밋밋" — "디테일 섞임이 필요하다" | [25] |
| 모델링 도구 | 1차 자료 못 찾음 **[미확인]** | — |

**[관찰]** ⑦_1: 텍스처 거의 없음. 단색 면 + 부드러운 명암 + 하늘색 높이 안개. 차만 녹·얼룩 텍스처가 있다. 원경 나무는 단순 덩어리 반복.

---

## 5. 가져올 수 있는 구체 기법

| # | 기법 | 출처 게임 | 우리 적용 |
|---|---|---|---|
| T1 | **최종 셰이더를 켠 화면에서만 모델링·텍스처** | ⑫ [12] | Blender 렌더 판정 대신 Unity 게임 카메라(720p Nearest)로 매 반복 확인 |
| T2 | **폴리곤 최소, 디테일은 텍스처(명암까지 텍스처에)** | ⑥ [2] | bpy로 형상 세부를 늘리지 말 것. 박스 + 좋은 텍스처 |
| T3 | **환경 = 실사진 포토배싱, 조작물 = 손 픽셀** 두 층 | ⑥ [2] | 건물 벽·지면은 CC0 사진 조각을 붙여 칠함. 차·화물만 픽셀 |
| T4 | **팔레트 텍스처 UV** | ① [1] | 소품·나무는 색 칸 아틀라스 하나에 UV만 배치 → 팔레트 교체로 계절·시간 |
| T5 | **사진에서 팔레트 추출** | ① [1][11] | 늦가을 해안 사진 1~2장에서 색 뽑기(이미 우리 브리프와 같은 방향) |
| T6 | **거의 unlit + 계단 램프 + 흰쪽으로 끌어올림** | ① [1] | PBR 빛 대신 평평한 빛. 색이 팔레트에서 안 벗어나게 |
| T7 | **앰비언트색 = 안개색** 하나로 묶기 | ⑥ [2] | 우리 무드값을 "앰비언트·안개 한 쌍"으로 관리 |
| T8 | **안개 → 엣지 검출 → 색 보정** 순서 | ① [1] | 원경 지저분함은 안개로 먼저 덮고 실루엣만 살림 |
| T9 | **트라이플래너 터레인**(경사로 텍스처 자동) | ① [1] | 지형 노이즈 대신 경사별 2~3 텍스처, 경계 날카롭게 |
| T10 | **버텍스 스무딩으로 모서리 부드럽게** | ⑫ [15] | bpy 박스의 날카로운 모서리를 노멀 스무딩으로 |
| T11 | **환경엔 디테일 몰빵, 캐릭터는 단순** | ⑫ [16] | 사람·차는 단순, 길가 소품 밀도를 올림 |
| T12 | **첫 판부터 완성도, 형상은 안 고치고 텍스처로 고침** | ⑥ [2] | 반복을 텍스처 레이어에서 돌림(형상 재생성 X) |
| T13 | **실제 가 본 장소 → 랜드마크 목록** | ① [11] | PLACES.md 장소 카드와 같은 방식 |

---

## 6. 우리와 결정적으로 다른 점 5개

| # | 기준 게임 | 우리 | 왜 격차가 나나 |
|---|---|---|---|
| 1 | **사람이 눈으로 보며 손으로 고친다.** 4개 다 1인이 "감"으로 만들고 바로 고침 [2][12] | AI가 bpy 좌표를 코드로 적고 렌더 뒤 판정 | 손맛·과장은 반복 눈-손 고리에서 나옴. 코드는 "규칙을 지킨 평균"에서 멈춤 [추정] |
| 2 | **최종 화면(셰이더 켠 카메라)에서 만든다** [12][1] | Blender 렌더로 단품을 판정 | 저해상도·안개·필터가 디테일을 지우거나 살리는 걸 만드는 순간에 못 봄 |
| 3 | **디테일은 텍스처, 형상은 최소** [2][1] | 형상(지붕·창·몰딩)을 코드로 늘리고 텍스처는 절차 노이즈 | 저해상도에선 형상 디테일이 지글거림이 되고, 노이즈 텍스처는 "가짜"로 읽힘 |
| 4 | **텍스처 원천이 실물**(사진 포토배싱 ⑥, 사진 팔레트 ①, 손그림 ⑫) [1][2][17] | 텍스처 대부분이 코드 생성 | 실사진의 우연한 얼룩·때가 없음 → "AI/절차 티" |
| 5 | **빛이 단순·통제됨**(unlit·한 색 앰비언트·안개 한 색) [1][2] | URP 조명·무드 값을 전역으로 여러 번 조정 | 기준 게임은 빛을 줄여 색을 지켰다. 우리는 빛 변수가 많아 판마다 색이 흔들림 [추정] |

덧: 기준 게임들도 "아트를 못 해서 제약을 골랐다"고 말한다(①⑥⑫ 모두) [1][2][16]. 차이는 실력보다 **제약을 하나 고르고 끝까지 그 화면 안에서 만든 것**이다.

---

## 미확인으로 남은 것

- ① 모델링 툴 이름, 렌더 텍스처 정확한 크기.
- ⑫ 텍스처를 사진으로 만들었는지 손으로 그렸는지, 텍스처 해상도.
- ⑦ 모델링·텍스처 툴, 아트 원칙 본인 발언 원문(Reddit 본문 접근 차단).
- 4개 모두: 텍스처 해상도 수치(px/m)는 공개 자료 없음 → 스샷 측정으로 대체해야 함.

## Sources

1. A Short Hike GDC 2020 포스트모템 "Crafting A Tiny Open World" (Adam Robinson-Yu) — https://www.youtube.com/watch?v=ZW8gWgpptI8 (GDC Vault: https://www.gdcvault.com/play/1026613/Independent-Games-Summit-Crafting-A)
2. 80.lv, "Inside The Making Of Easy Delivery Co." (Sam C. 인터뷰, 2026-05-07) — https://80.lv/articles/inside-the-making-of-easy-delivery-co
3. PlayStation Blog, "Crafting a tiny open world…" (Adam Robinson-Yu, 2021-08-05) — https://blog.playstation.com/2021/08/05/crafting-a-tiny-open-world-a-look-behind-the-scenes-at-the-creation-of-a-short-hike/
4. Paper Prototypes Podcast, "Easy Delivery Co. Dev Interview" (19:00~23:30) — https://www.youtube.com/watch?v=45wW47LnQxs
5. Wikipedia, Easy Delivery Co. — https://en.wikipedia.org/wiki/Easy_Delivery_Co.
6. Sam C., "Easy Delivery Co. Devlog 1" (2025-11-25) — https://www.youtube.com/watch?v=J9s5ziLdycQ
7. Steam 토론, 내부 해상도 456×256 불만 스레드 — https://steamcommunity.com/app/3293010/discussions/0/680734492841863089/
8. Game Developer, "Finding smart shortcuts in A Short Hike postmortem" — https://www.gamedeveloper.com/design/finding-smart-shortcuts-in-a-short-hike-postmortem-unlocking-the-vault-4
9. Game Pilgrim, Adam Robinson-Yu 인터뷰 (2020-02-26) — https://gamepilgrim.com/2020/02/26/developer-interview-adam-robinson-yu/
10. Game Developer, "Road to the IGF: A Short Hike" — https://www.gamedeveloper.com/business/road-to-the-igf-adam-robinson-yu-s-i-a-short-hike-i-
11. PlayStation Blog, "The award-winning A Short Hike hits PS4" (2021-11-13) — https://blog.playstation.com/2021/11/13/the-award-winning-a-short-hike-hits-ps4-november-16/ ; 프레스킷 https://ashorthike.com/press/
12. Kakuchopurei, Adam Vian 인터뷰 (2024-06) — https://www.kakuchopurei.com/2024/06/crow-country-exclusive-interview-adam-vian-indie-sfb/
13. Patch Magazine, SFB Games 인터뷰 — https://patchmagazine.co.uk/blogs/features/interview-sfb-games-developer-of-crow-country
14. Gamer Social Club, Crow Country 개발자 인터뷰 (2024-05-03) — https://gamersocialclub.ca/2024/05/03/crow-country-developer-interview/
15. r/CrowCountry "Crow Country Artstyle" (SFBDim 답글) — https://www.reddit.com/r/CrowCountry/comments/1cijdch/crow_country_artstyle/
16. Talking Terror with SFB Games (11:50~12:40) — https://www.youtube.com/watch?v=ygTY_zgymjs ; Developer Interview(1:00~2:30) https://www.youtube.com/watch?v=rqyhu6ihcrE
17. GamesRadar, Crow Country 기사(Vian 인용) — https://www.gamesradar.com/crow-country-an-atmospheric-survival-horror-set-in-1990-is-a-haunting-love-letter-to-ps1-era-games/ ; Adventure Games Podcast(5:40~6:30) https://www.youtube.com/watch?v=4QhbkeMcbjo
18. Under The Mayo, Adam Vian 인터뷰 (22:20~24:10) — https://www.youtube.com/watch?v=pWo4BpKu7go
19. Steam 뉴스, "Jalopy Launches in Two Weeks – Route Generation" / FAQ (2016-04) — https://store.steampowered.com/news/posts/?appids=446020&enddate=1460969097
20. MinskWorks About — https://minskworks.net/about/
21. GOG, Jalopy 소개 — https://www.gog.com/en/game/jalopy ; Wikipedia https://en.wikipedia.org/wiki/Jalopy_(video_game)
22. r/IndieDev "Solo dev struggling with art/visuals" (검색 요약만 확인) — https://www.reddit.com/r/IndieDev/comments/1qf2xn1/solo_dev_struggling_with_artvisuals_how_did_you/
23. Kill Screen, "Hungary is more than just a pit stop…" — https://www.killscreen.com/hungary-just-pit-stop-jalopys-long-winding-roadtrip/
24. Rock Paper Shotgun, "Road Trip Driving Game Hac Now Called Jalopy" — https://www.rockpapershotgun.com/jalopy-hac-road-trip-game
25. Robert Yang, "Level With Me: level design in Jalopy" (38:30) — https://www.youtube.com/watch?v=Pa2C36Sor8k
