# 왜 에이전트가 만든 건물은 "생성물"처럼 보이나 — 외부 1차 자료 → 검사 규칙 (2026-10-27)

조사: 하위 에이전트(리서치). 커밋 안 함.
표시: **[확인]** = 원문/원문 스니펫(검색 결과에 실린 본문·자막)을 직접 읽음 / **[추정]** = 내 해석.
중복 피함: 기준 게임 제작 방식(EDC 포토배싱·A Short Hike 팔레트)은 `2026-10-27-ref-games-production.md`, 커뮤니티 MCP 사례는 `2026-10-27-community-solutions.md`, 우리 측정치는 `2026-10-27-art-internal-audit.md` — 여기선 **건물 형태·배치의 "손맛"** 만.

---

## 1. 절차적인데도 손맛이 나는 게임 — 제작자 발언

| # | 제작자·출처 | 발언/사실 | 표시 |
|---|---|---|---|
| S1 | Stålberg, MCV 인터뷰 | "타일 기반의 지루한 점은 너무 반복적이 된다는 것. 불규칙 격자의 유기적 큰 형태가 타일 반복을 깬다." 정사각 조각을 만들고 불규칙 사각 격자에 **살짝 비틀어(skew)** 끼운다. | [확인] |
| S2 | Stålberg, EPC2021 Q&A | 격자 꼭짓점에 사각형이 3개·6개 붙는 곳이 섞임. 구역(육각 덩어리)마다 "더 혼란스럽게 / 더 규칙적으로" 생성을 다르게 설정. | [확인] |
| S3 | Game Developer(Tommy Thompson) "How Townscaper Works" | 불규칙 격자는 "실제 도시가 지어지는 이상하고 구불구불한 방식"을 존중하려는 의식적 결정. **배치(WFC)와 장식(decoration)을 분리** — 먼저 모듈을 고르고 그다음 꾸민다. "손으로 만든 느낌을 내려면 (조각이) 표준화돼야 한다." | [확인] |
| S4 | Stålberg 인터뷰(LinkedIn 정리, 2차) | 굴뚝·지붕형은 무작위, 닫힌 공간엔 정원이 우선. "창·우편함 같은 디테일은 손으로 넣을 여지를 남겨 sterile·반복적이지 않게." | [확인·2차 정리] |
| S5 | Stålberg 대담(YouTube "How Oskar Stallberg Turns PCG into…") | "손으로 다듬을 몫을 남겨 두면 훨씬 쉽다 — 여기 작은 디테일 하나 더." 손 텍스처에 의미(semantics)를 담음. | [확인·자막] |
| S6 | Stålberg, Konsoll 2021 | Townscaper 전체가 **128×128 텍스처 한 장**. | [확인·자막] |
| S7 | Stålberg 트윗(@OskSta 1579483114178768898) | 불규칙 격자 + **여러 칸에 걸친 큰 모듈**(집 하나가 사각+삼각 칸을 동시에 덮음). | [확인] |
| T1 | Opara(Tiny Glade), 80.lv 인터뷰 2024 | "모든 규칙을 내가 만든다 — 그래서 친밀하다." 요소마다 별도 생성기(조립·변형·deform). 도구를 "얼마나 많은 이야기를 만들 수 있나"로 검증. | [확인] |
| T2 | Opara, 80.lv "Realistic Procedural Architecture" 2016 | **모듈 위계**: 창 → 차양·창틀 허용, 발코니 → 빨래 건조대 → 빨래. "굴뚝이 건물 전체를 결정하게 두진 않는다." 관계를 세운 뒤 **모듈 위치·회전에 무작위 오프셋**. "사람은 패턴·반복에 매우 민감 — 무의식으로 즉시 잡아낸다." | [확인] |
| T3 | 80.lv, Tiny Glade 창 업그레이드 | 창이 닿는 회벽에 **불규칙 손상(plaster damage)** 을 자동으로 — 창까지의 부호 거리를 정점 속성 하나로. | [확인] |
| T4 | Tiny Glade 프레스킷 | 길이 벽을 지나면 아치, 비탈을 따라 벽이 휨. 양·담쟁이·새가 "만든 것에 반응". | [확인] |
| T5 | Small World Miniatures(비평) | Tiny Glade 의 벽은 거의 비지 않음(재질 변화·돌출·창·식물·목재·지붕 높이 변화), 식생은 **건물과 땅이 만나는 곳에 모임**, 지붕 끝이 완전히 곧지 않음, 이끼는 처마 밑·벽 아래에(오레가노처럼 고르게 뿌리지 않음). | [확인·제3자] |
| G1 | Kythreotis(Sable), GDC 2022 요약(Game Developer) | 넓은 사막 = 바다, 장소 = "콘텐츠의 섬" — 밀도를 장소에 몰고 사이는 숨 쉬게. 깊이는 빛·안개·선을 겹쳐서. | [확인] |
| G2 | Kythreotis, Forbes 요약(Game Developer) | "가상의 의뢰인·거주자를 떠올리고 그 사람들의 요구와 맥락에 맞춰 짓는다" (건축 전공). | [확인] |
| L1 | Lil Gator Game(MegaWobble) | 섬 지형을 먼저 다 만들고 그 위에 채움. 시각·탐험은 A Short Hike 영향. 건물 형태 관련 1차 발언은 못 찾음. | [확인/미확인] |
| A1 | A Short Hike·EDC | → `2026-10-27-ref-games-production.md` 참고(팔레트 텍스처·포토배싱·평평한 빛). 건물 불규칙성 발언은 이번에도 못 찾음. | — |

**1절 요약 [추정]**: 손맛 게임들의 공통 = ① 조각 자체는 표준화(손으로 만든 소수 모듈) ② **그 위 격자·배치가 불규칙**(비틀기·크기 다른 칸·구역별 혼란도) ③ 장식은 배치 뒤 **관계 규칙**(창→차양, 발코니→빨래, 창 주변 회벽 손상, 땅과 만나는 곳 식생) ④ 손으로 남겨 둔 예외 몇 개. 우리 집 c판은 ①만 있고 ②③④가 비어 있을 가능성이 크다.

---

## 2. 환경 아트 장인 자료 — 신뢰감·역사의 층·반복 숨기기

| # | 출처 | 핵심 | 표시 |
|---|---|---|---|
| E1 | Smith & Worch, GDC 2010 "What Happened Here?" 노트(worch.com PDF) | 환경 서사 = 플레이어가 **흩어진 요소를 스스로 이어** 하나의 의미로 해석하게 배치. 흔적(remains) 해석이 핵심. | [확인] |
| E2 | Kislitsin, 80.lv "What Makes Environments Believable" | "신뢰감은 디테일 추가가 아니라 **그 존재를 정당화**하는 능력." 때·녹은 텍스처가 아니라 사건의 결과(물은 아래로 흘러 낮은 곳에 고임). "어떻게 들여왔나·어떻게 관리하나·전원은 어디로·케이블은?" 물건은 **격자 정렬이 아니라 사용 논리**로 놓임(의자는 빠져 있고, 바닥 마모는 동선을 따름). "죽은 장면 = 박물관 진열." | [확인] |
| E3 | 80.lv "Planning and Constructing Believable Environments" | 벽·지붕 교체 모듈은 줄이고 **변화는 부착물(안테나·환풍기·기계)과 데칼(누수·얼룩)** 로. 파이프는 "모든 것에 논리적으로 연결". "안테나는 왜 여기 있나?" | [확인] |
| E4 | Outpost VFX "22 Pro Tips" #08 "Reality is wonky" | 크기·위치·회전·손상·마모량에 유기적 변동. 많이 밟힌 곳이 더 닳고, **보수 흔적**(한 장 들어냈다 다시 깐 타일은 안 평평함). 변동은 **미묘하게 — 눈에 띄면 만화처럼 됨**. | [확인] |
| E5 | Giora Nohl "Fisherman's Rest"(gamesartist.co.uk) | 함정 = "너무 깨끗하고, **너무 고르게 분포**되고, 사람이 만진 흔적이 없음 → 소품 진열장." 지붕을 가운데가 **자기 무게로 처지게** — 완벽히 곧은 실루엣을 깨면 "내가 오기 전부터 있던 것"처럼. | [확인] |
| E6 | 80.lv "The Yard" | 모듈 키트를 큰 벽 몇 장으로 합쳐 **창·문 크기와 위치를 하나씩 살짝 다르게** — "의식은 못 해도 더 흥미롭게 느끼는 디테일." | [확인] |
| E7 | 80.lv "Stylized Medieval Pub Diorama" | 컨셉이 "기울고 직선이 거의 없음" → 판자를 곧게 만든 뒤 **래티스로 무리째 휘어** 처진 실루엣, 튀어나온 판자 비틀기. 엔진에 넣어 보고 고치기 반복. | [확인] |
| E8 | 80.lv "Building a Stylized Game World for VR" | 지침: 큰 덩어리부터 / **직선 피하기** / 형태 과장 / 집 모듈 1개를 대량 복제해 구성부터 → 나중에 일부를 다른 양식·형태로 교체. 텍스처 모서리에 합판·골판지 사진 오버레이. | [확인] |
| E9 | 80.lv "Using Repetition for Creative Design" | 거의 모든 것이 수십 번 반복 — **크기·색 변형 + 안개·빛**으로 숨김, 초점 물체 하나만 고유. 같은 텍스처를 **살짝 틴트한 인스턴스**로 재사용. | [확인] |
| E10 | 80.lv "Valley of the Fallen Grace" | 같은 모듈을 색으로 구분해 보여 줌 → 재사용하되 **데칼·작은 들보로 타일링을 깸**. 영화 세트처럼 보이는 것만 짓기. | [확인] |
| E11 | Burgess(Bethesda) GDC 2016 슬라이드 | "Generic = versatile, 하지만 반복" → 해결 = **작은 조각 여러 개를 섞어 곱셈 효과**(벽 구멍 문제). | [확인·슬라이드 스니펫] |
| E12 | (2차 핸드북, GitHub LuauAISkill) | 반복 깨기: 소품 위치, 깨진/빠진 타일, N개 중 하나 꺼진 등, 반복 면 **명도 ±5~10%**, 데칼 세트 다르게, 물 얼룩 — "격자는 일정하게 두어 변화가 '마모'로 읽히게, 혼돈 아님." | [확인·2차, 수치는 저자 경험칙] |

**2절 요약 [추정]**: 장인이 공통으로 말하는 "사람 손" = ① 모든 부착물에 **이유**(어디서 와서 어디로 가나) ② 변화는 **사건 위치에 몰림**(동선·물길·손 닿는 높이·보수한 자리) — 고르게 뿌리면 안 됨 ③ 실루엣의 직선을 깸(처진 용마루, 기운 판자) ④ 같은 모듈이라도 창·문 크기·위치를 하나씩 어긋나게 ⑤ 변동은 미묘하게(눈에 띄면 만화). 우리 c판의 "깨끗한 단색 칠 + 고른 손도트 창틀"은 ②·③·④를 다 비켜 간다.

---

## 3. 실제 해안 마을 건물의 토속 특징

### 3-1. 이탈리아(리구리아·아말피)

| # | 출처 | 특징(수치) | 표시 |
|---|---|---|---|
| V1 | genovabb.it·Travelhoppers·italia-italy.org (카몰리) | 해안 집 **6~8층**, 좁고 높게 어깨를 붙여 "칠한 절벽 하나". 색 = 오커·장미·레몬·벽돌 빨강·세이지 그린, **초록 덧창**(반쯤 열림). 리구리아 **트롱프뢰유** — 창틀·발코니·코니스·덧창을 평벽에 **그려 넣음**(18세기, 가난한 정면을 꾸미려/공사비 절감). 진짜·가짜 창이 섞여 구분이 어려움. 골목(caruggi)은 너무 좁아 **빨랫줄을 공유**, 집 사이 가파른 계단. | [확인] |
| V2 | UniGe(Pellegri·Bruni) 색 계획 논문 | 전통 색 = **산화철 흙 안료**(폼페이 레드·오커·그린 어스·골든 옐로). 현대 보수에서 시멘트띠·플라스틱 도장이 빨리 상해 **벗겨지고 떨어짐**; 일부는 색 계획에 안 맞는 **너무 채도 높은 색으로 덧칠**됨. | [확인] |
| V3 | 로마 도 건축 규정 §66 "superfetazioni" | 역사 건물의 **후대 덧붙임**을 법으로 분류: 표면(다른 칠·시멘트 회벽·**창 새로 뚫기/막기**), 돌출(판 모양 부착물), 부피(덧댄 방). | [확인] |
| V4 | 바르베리노 무젤로 시가지 조사 R4 | 덧붙임 종류별 빈도: **차양·가림지붕(tettoie) 18%**(마을에 따라 최대 60%), 그 밖(층 올리기·발코니·돌출 방·닫힌 덧방) 각 <10%. | [확인] — 토스카나 내륙 수치, 해안에 그대로 쓰긴 [추정] |
| V5 | UPV "Mediterranean vernacular" · UNESCO Amalfi | 아말피 집: 석회석이 반듯이 안 깎여 **날카로운 모서리 없음**, 벽 두께 **0.8~1.0m**, 회벽은 부순 돌로 만들어 **주변 바위와 같은 색**. 평지붕/볼트 지붕 + 그 위 **포도 퍼걸러**, 지하 빗물 저수조, 바깥 계단. | [확인] |
| V6 | 아말피 중세 case-torri(storiadellacitta PDF) | 탑집 3~5층, 창은 **적고 좁음**, 위층에 더 많음 — "어느 정도 대칭"(완전 대칭 아님). | [확인] |

### 3-2. 일본(하마치·골목)

| # | 출처 | 특징(수치) | 표시 |
|---|---|---|---|
| J1 | 田中·福井·篠原「グレイン論に基づく街路の下町イメージ」(도쿄대 경관연) | 거리 사진 실험(n=20, 16거리): "下町らしさ" 점수가 **작은 요소(グレイン) 수 ÷ 거리 면적과 거의 선형(R²=0.92)**. 상위 요소: 화분 17·사람 17·간판 17·진열 상품 15·길 위 의자 12·깃발 11·**베란다 9·빨래 6**·초롱 5·쓰레기통 5·격자창 4·이발소 기둥 3. 밀도 **0.22~0.24개/m² → 3.2~3.3점(4점 만점)**, 0.03~0.07 → 0.6~1.3점. 감점 요소: 새 집·셔터 내린 가게·맨션·**타일 벽**(작은 요소가 없는 면). | [확인] — 핵심 수치 |
| J2 | 같은 논문 예비실험 | 기여: "路上の私物 16, 植木 15", **건물 단위보다 작은 요소**가 주목됨. 저해: 밝음(明るさ)·사람 없음. | [확인] |
| J3 | 宮本 외 『都市計画論文集』61-3(2026), 路地園芸 | 1,493개 거리에서 화분 수 집계: **집 가까이 녹지가 적은 거리일수록 화분이 많다**(모자란 녹지를 메우듯), 에도기 시가지일수록 길 위 화분 많음. | [확인·X 인용 초록] |
| J4 | 趣味の園芸(月島)·散歩の達人(村田あやこ) | "마당이 없어 집 앞이 마당" — 화분은 문 앞·경계에, 뿌리가 "탈주"·옆으로 "분가". 재미 = 생활감·생명력·**부지에서 삐져나옴(はみだし)**. | [확인] |
| J5 | Contemporary Aesthetics 2024(아시야하마) | 주민 캡션 평가: **녹·지저분한 칠·균열 = "불쾌·더럽다"**, 반면 **"돌봄·관리"의 흔적은 긍정**. | [확인] — 도현의 "사진 얼룩 벽 = 더럽다"와 같은 축 |
| J6 | 나라 아틀라스·kenchikuyogo | 일본 지붕은 직선이 드묾 — 사찰 照り(아래로 처짐), 간사이 민가 起り(위로 볼록). 照り 크기 ≈ **경간의 2~3%**, 작은 지붕은 강하게. 현대 주택만 직선. | [확인] |
| J7 | Lukasz Palka "Urban Organics"(사진가) | 도쿄 건물은 전선·배관·도관·간판·식물이 "자라듯 겹겹이 쌓임"; 전신주는 같은 형태의 끝없는 변종. | [확인] |

**3절 요약 [추정]**: 실제 마을의 "사람 손"은 **세월 때(녹·얼룩)가 아니라 덧붙임·덧칠·돌봄**이다 — 막은 창/새 창, 덧댄 차양(가장 흔함), 색이 조금 다른 보수 칠 조각, 문 앞 화분·빨래·의자. 도현이 사진 얼룩을 "더럽다"고 한 것과 아시야하마 주민 평가가 일치한다 → 우리 처방은 **"방치의 흔적"이 아니라 "사는 사람이 손댄 흔적"** 을 넣는 것.

---

## 4. "AI 티" 글 중 3D·게임 에셋에 해당하는 것

| # | 출처 | 핵심 | 우리 해당? | 표시 |
|---|---|---|---|---|
| A1 | Compton "10,000 Bowls of Oatmeal"(Game Developer 강연 요약·DiGRA 논문 인용) | 수학적으로 다 다른 결과도 **지각상 같으면 오트밀**. 기준은 "지각적 고유성". 시험 질문: "이 생성물로 팬픽을 쓰겠나?" — 수많은 미세 변형보다 **적은 수의 확실히 다른 것**. | **예** — 집마다 색만 바꾼 것 = 오트밀 | [확인] |
| A2 | Zhang 외 "Verbalized Sampling"(arXiv 2510.01171, ICML'26) | 정렬된 LLM의 모드 붕괴 원인 = 선호 데이터의 **전형성 편향**(사람이 익숙한 답을 고름, 기준 대비 +4~12%p). 품질이 비슷한 답이 여럿이면 **가장 전형적인 답으로 수렴**. 대책: 후보 여러 개 + 확률을 말하게 하고 그 분포에서 뽑기. | **예** — 에이전트가 집 사양을 정하면 문 가운데·창 격자·파스텔 3색 같은 "가장 그럴듯한 집"으로 감 [추정] | [확인] |
| A3 | Bazzaz & Cooper, Steam 리뷰 508,192건(arXiv 2608.11539) | 생성형 AI 공개 게임 = PCG 게임보다 추천률 낮음. 최다 주제 "**아무도 신경 안 쓴 것 같다**"(rushed·lazy·soulless) — 결함이 에셋을 넘어 게임 전체의 "낮은 공"으로 번짐. PCG 는 수십 년간 받아들여짐. | 부분 — 판단 축은 "공(care)이 보이나" | [확인] |
| A4 | "Why Slop Matters"(arXiv 2601.06060) | 슬롭 3특징: **표면적 능숙함**(겉 품질 뒤 실체·의도 없음), 비대칭 노력, 대량 생산성. | **예** — 깨끗한 칠·정돈된 손도트 = 표면적 능숙함 [추정] | [확인] |
| A5 | X @asynkimo "Is AI actually good at game art now?" | 문제는 단품 품질이 아니라 **일관성**: 벽과 바닥이 다른 게임 같음, 픽셀 격자에 안 맞는 "mixel". | 부분 — 우리는 1m=9텍셀 고정이라 대체로 통과 | [확인] |
| A6 | whichoneisreal "Detect AI images" | AI 배경 건물: **창이 구조 격자에 안 맞음**, 소실점 불일치, 벽 디테일이 "질감"처럼 보임(석조·마감이 아님). 진짜는 가까이 볼수록 디테일이 늘고, AI 는 가까이 볼수록 무너짐. | 반대 방향 주의 — 창 어긋남은 **층 격자 안에서만** | [확인] |
| A7 | Tripo/Customuse AI 3D QA 글 | 생성 메시 특징: 삼각 수프, 부품이 붙어 녹음, 얇은 디테일 뭉개짐, 뒤쪽 추측. | **아니오** — bpy/손 모듈이라 해당 없음 | [확인] |
| A8 | Ark(핀란드 건축사무소) | 생성 이미지가 "인터넷에서 배운 **범용 현대건축 이미지**를 되풀이" — 낯익은 미감. | 예(같은 원리 A2) | [확인] |

**4절 요약 [추정]**: 우리 집은 메시 결함형 AI 티(A7)는 없다. 해당하는 건 **A1 오트밀 + A2 전형성 수렴 + A4 표면적 능숙함** — "모두 그럴듯하고 깨끗하지만, 누가 손댄 흔적·이유·예외가 없다". 사람들이 읽는 건 결국 A3 "신경 쓴 흔적(care)".

---

## 5. 종합 — 사람이 만든 건물과의 차이 5개 [추정, 1~4절 근거]

1. **배치 불규칙 + 조각 표준화**(S1·S3·E8): 조각은 몇 개만, 대신 격자·각도·크기를 비틂. 우리는 조각을 많이 다듬고 배치는 반듯함.
2. **관계로 생기는 디테일**(T2·T3·E2·E3): 창→차양→화분, 발코니→빨래, 창 둘레 회벽 손상, 파이프는 어딘가와 이어짐. 우리는 요소를 "목록"으로 붙임.
3. **시간의 층 = 덧붙임·덧칠·돌봄**(V2~V4·J5): 방치(때·녹)는 "더럽다", 손질(새 칠 조각·덧댄 차양·막은 창)은 정감. 우리 c판은 시간이 0.
4. **직선·대칭 깨기, 단 미묘하게**(E4·E5·E7·J6): 처진 용마루 2~3%, 한 개만 다른 창. 눈에 띄면 만화(E4).
5. **작은 생활 요소 밀도**(J1): "동네다움"은 건물이 아니라 **작은 요소 수/면적**에 선형. 건물만 손봐서는 안 오름.

---

## 6. 우리 집 과제에 바로 넣을 규칙 15개 (측정·검사 방법 포함)

> 전제: 1m = 9텍셀 → 0.11m = 1텍셀. 검사는 ① bpy 스크립트로 오브젝트·정점 수치 ② Unity 게임 컷(1280×720) 육안/스크립트. 편차 상한을 둔 것은 E4("눈에 띄면 만화")·A6("창이 격자를 벗어나면 AI") 때문.

| # | 규칙 | 측정 / 합격선 | 근거 |
|---|---|---|---|
| R1 | **창 크기 3종 이상** | 한 집의 창(문 제외) 폭×높이를 묶어 서로 ≥15% 다른 종류 수 ≥3 (2층 이하 작은 집은 ≥2). bpy: `win_*` bbox 치수 클러스터. | E6·V6·V1 |
| R2 | **층 격자는 지키고, 어긋남은 1~2개만** | 같은 층 창 하단 높이: 최소 1개가 줄에서 0.22~0.45m(2~4텍셀) 어긋남, 0.45m 넘는 것 0. 층 경계선(코니스·바닥선)은 집 전체에서 한 높이. | E6·E12·A6 |
| R3 | **좌우 대칭 깨는 요소 ≥2** | 정면 중심축으로 요소를 뒤집어 짝 없는 요소 수 ≥2 (문 치우침·굴뚝·덧방·배관·발코니 하나만). 정면이 완전 거울 대칭이면 불합격. | S4·V6·A2 |
| R4 | **덧붙임(superfetazione) ≥1, 3층 이상은 ≥2** | 본채와 재질·색·처마 높이가 다른 부착 부피: 차양/가림지붕(우선)·덧방·발코니·옥상 덧층. bpy 태그 `add_*` 수. | V3·V4·E8 |
| R5 | **막은 창 또는 새로 뚫은 창 1개** | 벽 색과 ΔL 3~8% 다른 창 모양 칠 면(막은 창) 또는 다른 창과 틀 양식이 다른 창 1개. | V3 |
| R6 | **보수 칠 조각 1~3개/면, 방치 때 0** | 면 텍스처에서 바탕과 명도 ±5~10%(색상 같은 계열)인 직사각 조각 1~3개, 면적 합 5~20%. 갈색 얼룩·녹 흐름 면적은 면의 ≤3%(모서리·아래만). | V2·J5·E12·도현 "더럽다" |
| R7 | **부착물 끝이 다 이어짐** | 배관·홈통·전선·실외기 배관: 양 끝점이 지면·지붕·벽 기구·전신주 0.1m 안에 있음. 허공 끝 0개. 홈통은 지붕 처마에서 시작. bpy 곡선 끝점 검사. | E2·E3 |
| R8 | **흔적은 사건 위치에 몰림** | 마모·때·이끼·변색 픽셀의 ≥70%가 [지면~0.6m / 처마 아래 0.3m / 창틀 아래 / 문 둘레 0.5m] 안. 면 전체 고른 노이즈 금지. | T5·E2·E4 |
| R9 | **용마루·처마 직선 금지(미묘하게)** | 용마루 정점의 직선 대비 최대 처짐 = 경간의 1~3% (예: 6m → 0.06~0.18m = 1~2텍셀). 3% 초과 금지. | J6·E5·E7 |
| R10 | **평면이 완전한 직사각형 아님** | 평면 모서리 각 중 ≥1개가 90°±2~6°, 또는 벽 한 면이 0.1~0.2m 꺾임/돌출. 한 집 안 모든 벽이 직교면 불합격. | S1·S3 |
| R11 | **덧창 상태 2종 이상, 색 변형 ±5~10%** | 한 정면 덧창 중 열림/반열림/닫힘 ≥2상태. 같은 모듈 덧창이 화면에 ≥3번 보이면 1개 이상은 명도 ±5~10%. | V1·E9·E12 |
| R12 | **집 앞 작은 생활 요소 밀도** | 정면 폭 × 앞길 반폭 면적당 작은 요소(화분·간판·의자·빨래·자전거·초롱·쓰레기통·게시판·진열품) 밀집 구역 ≥0.2개/m², 보통 ≥0.1. 게임 컷에서 셈. | J1(R²=0.92) |
| R13 | **화분은 무리로, 문 앞·경계에** | 화분 ≥3개 묶음(서로 0.6m 안) ≥1곳, 크기 ≥2종, 한 줄 등간격 배열 금지(간격 변동계수 ≥0.3). | J3·J4·T5 |
| R14 | **"누가 사나" 카드가 집마다 있음** | 집 사양에 [사는 사람 1줄 · 고친 곳 1 · 덧붙인 것 1 · 그 집만의 예외 1]. 그 카드에서 R4~R6 값이 나와야 함(무작위 아님). 썸네일 8채를 섞어 카드와 짝짓기 ≥6/8 맞으면 합격. | G2·E2·A1 |
| R15 | **전형값 금지 — 분포에서 뽑기** | 에이전트가 집 사양을 정할 때 항목마다 후보 ≥4개+확률을 적고, 마을 전체에서 "최빈 후보"(문 가운데·창 대칭 격자·덧창 전부 열림) 채택 비율 ≤50%. 사양 로그로 검사. | A2·A8 |

**쓰는 법 [추정]**: R1~R11 은 bpy 검사 스크립트 하나로 자동화 가능(집당 통과 개수 표). R12·R13 은 장면(게임 컷) 단계. R14·R15 는 과제 문서 양식. 통과해도 "잘 생겼다"는 보장이 아니다 — 쌍대비교 게이트(`docs/qa/judge/JUDGE.md`)와 도현 판정 앞의 **최소 조건**으로만 쓴다.

---

## 7. 안 한 것 / 한계

- Stålberg Konsoll 2018 "Developing the Bad North Look" 강연 본문(조명·AO·정점색 처리)은 자막 일부만 봄 — 형태 불규칙성 발언은 MCV·Game Developer 인터뷰 기준.
- A Short Hike·EDC·Lil Gator Game 의 **건물 불규칙성** 1차 발언은 못 찾음(앞 문서와 같음).
- 지중해 창 크기·층고의 **통계 수치**는 못 찾음 — R1·R2 합격선은 장인 자료(E6·E12)와 해상도(9텍셀/m)에서 정한 [추정]. 실사진 몇 장에서 재 보면 더 단단해짐(`docs/research/*street-networks*` 처럼 Commons 사진 측정 추천).
- V4(차양 18%)는 토스카나 내륙 조사. 해안 마을 수치 아님.
- X 글(A5)·LinkedIn 정리(S4)는 2차 정리문.

---

## Sources

표 ID ↔ 출처 번호: S1=1, S2=2, S3=3, S4=4, S5=5, S6=6, S7=7 / T1=8, T2=9, T3=10, T4=11, T5=12 / G1=13, G2=14, L1=15 / E1=16, E2=17, E3=18, E4=19, E5=20, E6=21, E7=22, E8=23, E9=24, E10=25, E11=26, E12=27 / V1=28·29·30, V2=31, V3=32, V4=33, V5=34·35, V6=36 / J1·J2=37, J3=38, J4=39·40, J5=41, J6=42, J7=43 / A1=44, A2=45, A3=46, A4=47, A5=48, A6=49, A7=50, A8=51.

1. MCV/DEVELOP — "Townscaper – the game which isn't a game…" https://mcvuk.com/business-news/when-we-made-townscaper/
2. EPC2021 Q&A with Oskar Stålberg (YouTube 자막) https://www.youtube.com/watch?v=NOJYZYqY6_M
3. Tommy Thompson, "How Townscaper Works", Game Developer 2022 https://www.gamedeveloper.com/game-platforms/how-townscaper-works-a-story-four-games-in-the-making
4. Townscaper and Procedural Generation – Oskar Stålberg (LinkedIn 대담 정리) https://www.linkedin.com/pulse/how-make-procedural-generation-look-good-gabriele-romagnoli-vjihe
5. "How Oskar Stallberg Turns Procedural Generation into Successful Games" (YouTube 자막) https://www.youtube.com/watch?v=9Rr_JYoZ--4
6. Konsoll 2021: Oskar Stålberg – The Story of Townscaper https://www.youtube.com/watch?v=5xrRTOikBBg
7. @OskSta 트윗 https://x.com/OskSta/status/1579483114178768898
8. 80.lv — Tiny Glade Developers Discuss Bevy, Proceduralism… https://80.lv/articles/exclusive-tiny-glade-developers-discuss-bevy-proceduralism-publishers-cozy-games
9. 80.lv — Anastasia Opara, Realistic Procedural Architecture for Games https://80.lv/articles/realistic-procedural-architecture-for-games
10. 80.lv — Procedural Windows in Tiny Glade Get an Upgrade https://80.lv/articles/procedural-windows-in-pounce-light-s-tiny-glade-get-an-upgrade/
11. Tiny Glade Press Kit https://www.scribd.com/document/864084768/Tiny-Glade-Press-Kit
12. Small World Miniatures — Tiny Glade and the Art of Building a Miniature https://www.smallworldminiatures.com/post/tiny-glade-and-the-art-of-building-a-miniature-before-you-build-it
13. Game Developer — How Shedworks refined the art of Sable (GDC 2022) https://www.gamedeveloper.com/marketing/how-shedworks-refined-the-art-of-sable-in-pursuit-of-readability
14. Game Developer — For Sable, developing an evocative art style comes first https://www.gamedeveloper.com/design/for-i-sable-i-developing-an-evocative-art-style-comes-first
15. The Red Tunic Ep51 — MegaWobble (Lil Gator Game) https://www.youtube.com/watch?v=3J5JMwpMk1w
16. Smith & Worch, "What Happened Here?" GDC 2010 노트 https://www.worch.com/files/gdc/What_Happened_Here_Web_Notes.pdf
17. 80.lv — How To Create Believable 3D Environments (Kislitsin 외) https://80.lv/articles/3d-environment-artist-on-what-makes-environments-believable
18. 80.lv — Planning and Constructing Believable Environments https://80.lv/articles/planning-and-constructing-believable-environments
19. Outpost VFX — 22 Pro Tips for Environment Artists https://www.outpost-vfx.com/fr/news/22-pro-tips-and-tricks-for-environment-artists
20. Giora Nohl — Fisherman's Rest https://gamesartist.co.uk/fishermans-rest/
21. 80.lv — The Yard: 3D Environment Breakdown https://80.lv/articles/the-yard-3d-environment-breakdown
22. 80.lv — Creating a Stylized Medieval Pub Diorama https://80.lv/articles/001agt-creating-a-stylized-medieval-pub-diorama
23. 80.lv — Building a Stylized Game World for VR https://80.lv/articles/building-a-stylized-game-world-for-vr
24. 80.lv — Using Repetition for Creative Design https://80.lv/articles/using-repetition-for-creative-design
25. 80.lv — Valley of the Fallen Grace https://80.lv/articles/valley-of-the-fallen-grace-environment-art-in-ue4
26. Joel Burgess, Modular Level Design (GDC 2016 슬라이드) https://media.gdcvault.com/gdc2016/Presentations/Burgess_Joel_Modular%20Level%20Design.pdf
27. LuauAISkill handbook — environment art (2차) https://github.com/adaybekovt-boop/LuauAISkill/blob/main/handbook/graphics/05-environment-art.md
28. genovabb.it — Camogli the painted pearl / Camogli Beach https://genovabb.it/en/pcamogli-the-painted-pearl-a-day-in-the-most-photogenic/ , https://genovabb.it/en/genoa-guide/poi/spiaggia-camogli/
29. Travelhoppers — Camogli: A Coastal Portrait https://travelhoppers.com/articles/camogli-a-coastal-portrait/
30. italia-italy.org — Camogli https://www.italia-italy.org/en/liguria-turismo-B490-camogli
31. Pellegri·Bruni, DE-SIGN 4 (UniGe) 색 계획 https://unige.iris.cineca.it/bitstream/11567/1234477/1/Pagine%20da%20DE%20SIGN%2023%20pELLEGRI%20bRUNI.pdf
32. Provincia di Roma 건축규정 §66 superfetazioni http://www.provincia.rm.it/regolamentoedilizio/regolamento/normativa/definizione_superfetazioni.html
33. Comune di Barberino — R4 L'indagine sui Centri Storici https://sit.comunebarberino.it/sites/barberinomugello/files/ru/r4_lindagine_sui_centri_storici.pdf
34. UPV — Mediterranean vernacular architecture https://riunet.upv.es/server/api/core/bitstreams/8129aa8f-c75a-4a2e-aae9-f2a5e33d374a/content
35. UNESCO Amalfi Coast — characteristics of vernacular architecture https://www.unescoamalficoast.it/preserve-your-territory/the-characteristics-of-vernacular-architecture/?lang=en
36. Case e torri medievali IV (storiadellacitta) https://www.storiadellacitta.it/wp-content/uploads/2020/10/2014-Case-e-torri-IV.pdf
37. 田中·福井·篠原「グレイン論に基づく街路の下町イメージに関する研究」 http://keikan.t.u-tokyo.ac.jp/documents/research/tanaka.pdf
38. 宮本 외 路地園芸 (都市計画論文集 61-3, 2026; X 초록 인용) https://x.com/nekonoizumi/status/2114361122778444156
39. 趣味の園芸 — 路地は昭和園芸のワンダーランド https://www.shuminoengei.jp/article/content/00000788.php
40. 散歩の達人 — 村田あやこ インタビュー https://san-tatsu.jp/articles/80055/
41. Contemporary Aesthetics 2024 — Ashiyahama Seaside Town https://contempaesthetics.org/2024/12/10/aesthetic-evaluation-of-the-atmosphere-in-everyday-environments-a-case-study-in-ashiyahama-seaside-town-japan/
42. 奈良アトラス — 照り・起り https://nara-atlas.com/term/japanese/4391/ / 建築用語 照り https://kenchikuyogo.com/?page_id=4478
43. Lukasz Palka — Urban Organics https://www.lkazphoto.com/portfolio
44. Game Developer — Practical procedural generation (Kate Compton) https://www.gamedeveloper.com/design/practical-procedural-generation-for-everyone-
45. Zhang 외, Verbalized Sampling (arXiv 2510.01171) https://arxiv.org/html/2510.01171v3
46. Bazzaz & Cooper, Player Perceptions of Generative AI in Games (arXiv 2608.11539) https://arxiv.org/pdf/2608.11539
47. Why Slop Matters (arXiv 2601.06060) https://arxiv.org/html/2601.06060
48. @asynkimo, Is AI actually good at game art now? https://x.com/asynkimo/article/2070101189161525615
49. whichoneisreal — How to Detect AI-Generated Images https://whichoneisreal.com/detect-ai-images/
50. Customuse — AI 3D Model Generator https://customuse.com/learn/ai-3d-model-generator
51. Ark — Industrial Revolution of Creativity https://www.ark.fi/en/2024/02/industrial-revolution-of-creativity/
