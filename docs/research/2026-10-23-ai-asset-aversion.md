# AI 에셋·사진 텍스처 거부감 — 근거 문서 (2026-10-23)

과제: PD 20261023-1458. 보고서(짧은 것)는 `docs/team/reports/research-20261023-1458.md`.
표시: **[확인]** 원문에서 읽음 · **[2차]** 요약/뉴스 기사로만 확인 · **[추정]** 내 해석 · **[미확인]** 찾지 못함.

---

## 1. 거부감은 어디서 나오는가

### 1-1. 규모 (비시각)
- Quantic Foundry 설문(2025-10~12, N=1,799): 85% 가 게임 속 생성형 AI 에 부정적, 63% 는 가장 부정적인 칸을 골랐다. 가장 싫어하는 용도는 **아트·음악·음향·대사·퀘스트** 같은 창작물, 동적 난이도 조절 같은 비창작 용도는 비교적 중립. 여성·논바이너리, 젊은 층, 스토리·디자인을 중시하는 게이머일수록 더 부정적. [2차] — Quantic Foundry 본문(quanticfoundry.com/2025/12/18/gen-ai/)은 이 환경에서 403 으로 못 열었다. 수치는 아래 기사 인용.
  - https://www.gamesmarket.global/quantic-foundry-most-gamers-are-negative-about-the-use-of-gen-ai-in-video-games
  - https://boingboing.net/2025/12/20/survey-finds-very-negative-attitude-toward-gen-ai-in-games.html
- 가장 자주 나오는 우려는 "저작권 침해"와 "게임들이 서로 비슷해 보이기 시작함". [2차] (같은 gamesmarket 기사)
- **공개 라벨의 비용**: Game Oracle 가 2025년 Steam 출시작 약 1만 개를 분석 — AI 공개 표기가 있는 게임은 첫 달 리뷰가 평균 약 4개, 없는 게임은 약 7개. 출시 시기·장르·퍼블리셔 지원을 보정해도 유지. **리뷰 수는 판매량의 대용치일 뿐이고 상관관계다.** [2차] https://www.digitalcitizen.life/steam-games-that-disclose-ai-use-may-sell-far-less-new-study-finds/ (같은 기사에 나온 "53%" 수치는 확인 못 해 쓰지 않는다.)

### 1-2. 실제 논란 사례 (비시각) — 공통 패턴: **임시 자리표시용 AI 가 출시본에 남음 → 발견 → 사과·교체**

| 게임 | 날짜 | 무슨 일 | 출처 |
|---|---|---|---|
| The Alters (11 bit) | 출시 2025-06-13, 해명 06-30, 패치 07-14 | 게임 속 모니터에 프롬프트 같은 AI 글자, 영상 자막에 AI 번역. 스튜디오: 임시 작업물이 실수로 남았다, "미리 알렸어야 했다". 패치 1.1.0 에서 교체. 그 사이 28만 장 이상 팔림 | https://www.fixgamingchannel.com/the-alters-ai-controversy-raises-a-bigger-steam-question/ [확인] |
| Clair Obscur: Expedition 33 | 2025-12 말 | Indie Game Awards 가 GOTY·신인상 수상을 취소. 제출 때 "생성형 AI 안 썼다"고 했는데 출시 후 임시 텍스처에 AI 썼다고 확인. 디렉터: 미완성 텍스처를 채우려 썼으나 "느낌이 안 맞아" 제거. 주최측 규정: 개발 어느 단계든 금지 | https://www.gosugamers.net/entertainment/news/77758-clair-obscur-expedition-33-stripped-of-indie-game-of-the-year-award-for-ai-usage [2차] |
| Larian — Divinity | 2025-12 공개, 2026-01 Reddit AMA | "콘셉트 아트 탐색에 AI 썼다, 출시물엔 안 들어간다" → 반발 → "출처 논란이 없도록 콘셉트 아트에서 생성형 AI 쓰지 않기로 했다". 팬 일부: "가장 창의적인 단계에 쓰면 전체가 오염된다". | https://www.videogameschronicle.com/news/holy-f-guys-were-not-replacing-artists-larian-boss-responds-to-ai-backlash · https://www.notebookcheck.net/Larian-Studios-walks-back-generative-AI-use-in-Divinity-after-fan-backlash-clarifies-future-AI-role.1201780.0.html [2차] |
| Crimson Desert (Pearl Abyss) | 출시 직후 2026-03-22 사과 | 2D 소품(그림 등)에 "실험적 생성 AI" 사용분이 출시본에 남음. 발견 하루 만에 사과, 전수 감사 약속, Steam 에 AI 표기 추가, 이후 패치로 교체. 상업적 타격은 눈에 띄지 않았다고 보도 | https://engadget.com/gaming/crimson-desert-developer-apologizes-and-promises-to-replace-ai-generated-art-183716439.html [확인] · https://www.pushsquare.com/news/2026/03/crimson-desert-dev-keeps-its-promise-replaces-ai-art-in-latest-patch [2차] |
| 1666: Amsterdam (Panache) | 2026-08 (날짜 [미확인]) | 무료 프롤로그에서 AI 초상화·그림 발견 → 초기 제작 에셋이 검수에서 빠졌다고 사과, 이후 빌드에 AI 없음 약속 | https://finalboss.io/1666-amsterdam-removes-ai-art-after-demo-backlash-what-players-should-check-next [2차] |
| ARC Raiders (Embark) | 2025-10 출시 전후 | AI 음성(TTS) 대사 → 반발 → 일부를 배우 녹음으로 교체. CEO: "전문 배우가 AI 보다 낫다". 그래도 Steam 동접 약 50만 | https://www.screenhub.com.au/news/games/arc-raiders-ai-voice-actors-replaced-2695984/ · https://hitmarker.net/news/embark-studios-defends-and-explains-its-use-of-ai-in-arc-raiders-ahead-of-launch-1561355 [2차] |

**읽는 법 [추정]**: 반감의 큰 몫은 "AI 를 썼다"가 아니라 **"숨겼고 들켰다"** 에서 온다(The Alters "알렸어야 했다", Clair Obscur 제출 때 "안 썼다"). 반면 흥행 자체는 대체로 안 꺾였다(Crimson Desert·ARC Raiders·The Alters 28만 장). 인디는 그만한 완충이 없다 — Game Oracle 수치가 그 방향.

### 1-3. Steam 규정 (비시각)
- 2026-01 개정: 개발 보조 도구(코드 도우미 등)는 공개 면제. **"마케팅·콘셉트 에셋부터 게임 내 콘텐츠까지" 플레이어가 보는 AI 생성물은 공개**. 한 번의 체크박스로 목록화. 표기는 스토어 "About This Game" 에 보임. [2차] https://gigazine.net/gsc_news/en/20260120-steam-updates-ai-disclosure-guidelines (Valve 원문 `store.steampowered.com/news/group/4145017/view/3862463747997849618` 은 본문이 안 읽혀 못 봄 [미확인])
- 사전 생성(pre-generated)과 실시간 생성(live-generated)을 따로 공개한다는 설명은 법률 블로그발 [2차, 확인 필요] https://blog.promise.legal/ai-game-assets-copyright-steam-disclosure-2026/
- 집행 강도: 어떤 글은 "자발적·Valve 중재 없음", 다른 글은 "실제 집행" — **충돌, 해소 못 함**.
- 타 스토어(Epic·PS·Xbox·Switch)는 비슷한 공개 규정 없음 [2차]. Tim Sweeney 는 Steam 라벨이 소규모 개발사를 해친다고 비판.
- **[미확인] "플레이어에게 안 보이는 내부 콘셉트 아트"가 공개 대상인지** — Valve 문구에 "concept assets" 가 들어 있어 애매. 우리 규칙 1 이 걸린 지점이라 PD 가 Valve 문서 원문으로 확인할 것.

### 1-4. 시각적 원인 ("AI 같다"로 읽히는 것)
**솔직한 한계**: 게이머가 AI 를 *어떤 시각 단서로* 알아보는지 측정한 설문·연구는 못 찾았다. 아래는 개발자·교육 글의 경험칙과 위 사례에서 발견된 실물이다. [2차 / 추정]

| 단서 | 근거 | 우리 프로젝트에서 |
|---|---|---|
| 깨진 글자·가짜 서명 | 모든 가이드 공통(80.lv, macmyths 등). The Alters 도 **글자** 에셋이 걸렸다 | 규칙 5 와 맞음. 포장 라벨·간판·도장 |
| 장면 안 일관성 붕괴(조명·디테일 규칙이 에셋마다 다름), 반복 패턴 | https://macmyths.com/how-to-spot-ai-generated-art-in-video-games/ · 3D 는 baked 조명·뭉개진 메시: https://blogs.brighton.ac.uk/rodrigovicente/?p=239 | 규칙 3·7 |
| 의미 없는 디테일(이유 없는 주름·마모) | 같은 가이드류 "디테일이 어떤 실제 기법/물리도 따르지 않음" | 규칙 6 |
| 스타일 평균화(광택, 누구의 것도 아닌 룩) | 위와 같음 | 기준 원화 02_warm_street 고정 |
| **가짜 픽셀**(서로 다른 격자) | 브리프 자체 가설. 외부 근거 [미확인] — 다만 제각각인 해상도 혼합이 어색하다는 건 Polycount 사례와 일치 | 규칙 3 |
| 사진 질감과 다른 스타일 혼합 | Polycount: 사진에서 가져와 부드럽게 만든 천 질감이 손그림 나무 옆에서 튄다 https://polycount.com/discussion/comment/2269849/ [2차, 개별 게시물] | 사진 텍스처를 쓸 때의 가장 큰 위험 |
| 한계 | "시각만으론 AI 여부를 확정 못 한다"는 반론도 같은 글에 있음. 잘 고르면 단서가 숨는다 | 단서 제거보다 **출처 기록** 이 더 단단 |

### 1-5. "사진 텍스처" 자체에 대한 거부감
- **[미확인]** 사진/포토스캔 텍스처에 대한 거부감을 따로 측정한 자료는 못 찾았다. 도현이 말한 반감은 (a) 생성형 AI 반감 (b) 사진을 매끈하게 쓴 저예산 에셋 플립(스타일 충돌) 두 가지가 섞인 인상일 가능성이 높다 — **[추정]**.
- 확인된 건 반대 방향 사례(아래 2번): 사진을 쓰고도 칭찬받은 게임이 있다.

---

## 2. 사진 텍스처를 쓰고도 받아들여진 사례

| 게임 | 확인된 사실 | 출처 |
|---|---|---|
| **Easy Delivery Co** (Sam C., 2025-09-18) | Unity + Blockbench. **환경 = 사진 기반 텍스처(Photoshop 포토배싱, 나중에 Aseprite), 트럭·택배 상자·캐릭터 = 픽셀아트 텍스처.** 지오메트리 최소, 디테일은 텍스처로. 구워 넣은 조명 + SSAO, 그림자·태양 없음, 앰비언트 한 색(시간대로 변함) + **안개를 앰비언트와 같은 색**. 가상 화면 **456×256** 에서 읽히게 설계. 캐릭터는 일부러 어긋난 반올림·떨림. 참고 사진은 직접 찍거나 검색. 평가: "generally favorable", RPS "just looks right" | https://80.lv/articles/inside-the-making-of-easy-delivery-co [확인] · https://en.wikipedia.org/wiki/Easy_Delivery_Co. [확인] |
| Puppet Combo 계열 | **제작 과정 자료를 못 찾음.** 커뮤니티 추측(스톡 사진을 픽셀화 플러그인으로 처리)만 있고 개발사 발언 아님 | https://forums.getpaint.net/topic/116205-puppet-combo-game-textures/ [미확인] |
| Dusk | 원본 텍스처 제작법 자료 못 찾음. (HD 리메이크는 팬 제작이라 무관) | [미확인] |
| Signalis | 저폴리 모델 + 2D 스프라이트, PS1 영감, CRT 모드 확인. **텍스처 출처(사진/손그림) 자료 없음** | https://en.wikipedia.org/wiki/Signalis [미확인] |
| Mouthwashing·Buckshot Roulette | 제작 과정 인터뷰를 못 찾음(Mouthwashing 은 *Design Works* 책이 있다 — 단서) | [미확인] |

**왜 Easy Delivery Co 는 받아들여졌나 (사실 + 추정)**
- 사실: ① 해상도를 낮은 가상 화면에 맞춰 *전체* 를 같은 격자로 통일 ② 환경 사진과 소품 픽셀아트를 **역할로 분리**(사람이 만지는 물건 = 손으로 그린 픽셀) ③ 조명을 앰비언트 한 색+같은 색 안개로 단순화 → 에셋별 조명 차이가 없다.
- 추정: 사진이 "사진처럼" 읽히기 전에 해상도·색으로 뭉개져 질감 재료로만 남는다. 이건 우리 브리프 규칙 2·3 과 같은 방향이지만, **EDC 가 팔레트 양자화·64px 를 했다는 근거는 없다**(80.lv 기사는 팔레트·디더를 다루지 않는다고 확인됨).

---

## 3. AI 를 쓰면서 반감을 피한 사례·방법

**솔직한 결론: "AI 를 쓰고도 반감이 거의 없었던" 인디 사례는 이번 조사에서 못 찾았다.** 찾은 건 전부 반감이 났고 되돌린 사례다. 따라서 아래는 사례가 아니라 *사례들이 알려주는 반대 방향 교훈*이다.

| 교훈 | 근거 |
|---|---|
| 자리표시용을 출시본에서 못 걸러내는 게 가장 흔한 사고 | The Alters·Crimson Desert·1666·Clair Obscur 4건 모두 |
| 사과문 구조가 통한 경우: 즉시 인정 + 전수 감사 + 교체 일정 + 표기 추가 | Crimson Desert (https://www.pushsquare.com/news/2026/03/crimson-desert-dev-keeps-its-promise-replaces-ai-art-in-latest-patch 에서 약속 이행 보도) |
| "AI 는 탐색용, 결과물엔 없다"는 설명은 *창작 초반* 에 쓴 것도 오염으로 읽는 사람이 있다 | Larian 이 결국 콘셉트 아트에서도 철회 |
| 숨기는 것이 AI 사용 자체보다 큰 문제 | The Alters 공식 "알렸어야 했다", Clair Obscur 의 제출 시 진술 불일치 |
| 질적으로 못하면 *스스로* 빼는 게 낫다 | ARC Raiders CEO "전문 배우가 낫다", Clair Obscur 디렉터 "느낌이 안 맞았다" |
| 공개하면 리뷰 수 손해 가능성 | Game Oracle (상관관계) |

---

## 4. 이 프로젝트에 적용

### 4-1. 브리프 `art/_briefs/2026-10-23-art-direction-v2.md` "AI 에셋 느낌을 없애는 제작 규칙" 7개 검토

| # | 판정 | 이유(근거) | 제안 |
|---|---|---|---|
| 1 | **고칠 것** | 방향은 맞다. 그러나 실제 사고는 전부 "쓰지 않기로 한 것이 임시로 들어갔다가 안 빠짐"이다. Larian 은 *콘셉트 아트까지* 포기. 또 Valve 문구에 concept assets 가 있어 내부 원화도 공개 대상일 수 있다([미확인]). | "AI 이미지는 원화·구도까지만" 뒤에 추가: **빌드 폴더에 AI 파생 파일 0 + 원화가 스토어·트레일러·SNS 에 나가면 공개** |
| 2 | **고칠 것** | "PS1/Easy Delivery Co 계열이 실제로 쓰는 방식"은 부분만 사실. EDC 는 *환경만* 사진, 상자·트럭은 **손 픽셀아트**. 32색 양자화는 EDC 근거 없음(우리 선택일 뿐). 또 CC0 라이브러리 안에 AI 생성물이 섞였는지 확인하는 항목이 없다 | 문구를 "EDC 는 환경=사진 포토배시, 소품=손 픽셀"로 정정. "도현이 찍은 사진"만 확실히 출처 보장. CC0 는 사이트·URL·다운로드일을 기록 |
| 3 | 유지 | 서로 다른 해상도·스타일 혼합이 어색하다는 실례(Polycount), EDC 의 단일 가상 해상도와 같은 방향 | 단, 텍셀 밀도 약 32px/m 가 EDC 방식인지 근거 없음 → "우리 기준"이라고만 표기 |
| 4 | 유지 | "실측" 자체는 외부 근거는 없으나 해가 없음 | — |
| 5 | 유지 + 보강 | 깨진 글자는 모든 가이드의 1번 단서, The Alters 도 글자였다 | 라벨·간판·도장 문구 목록을 사람이 쓰고 폰트로만 렌더 — 텍스처 PNG 안에 글자가 *굽혀 들어가는지* 검사 |
| 6 | 유지 | 이유 없는 디테일이 단서라는 가이드와 일치 | 마모 위치 목록을 에셋 메타에 적기 |
| 7 | **더할 것** | 검사가 전부 "픽셀 규격"인데 실제 사고는 **출처**. 규격은 통과해도 AI 파일이 섞여 들어갈 수 있다 | 아래 추가 항목 |

**더할 규칙 (제안)**
- **8. 출처 장부**: `art/provenance.csv` — 에셋 ID · 질감 원천(CC0 URL+날짜 / 직접 촬영 / 스크립트 생성) · 만든 도구 · AI 사용 여부. 빌드 전 장부에 없는 텍스처가 있으면 실패.
- **9. 출시 전 전수 감사**: 출시 빌드의 모든 텍스처가 장부에 있고 AI 칸이 "아니오"인지 확인(Crimson Desert 가 사후에 한 일을 사전에).
- **10. 공개 결정은 도현**: Steam 콘텐츠 설문에서 쓸 문구를 미리 정해 둔다. 숨기는 쪽이 더 비싸다는 게 사례들의 공통점.
- **11. 같은 조명 규칙**: EDC 처럼 조명·안개 한 규칙으로 통일 → 에셋별 조명 차이 제거.

### 4-2. 질감 원천 추천 — **CC0 사진 (ambientCG / Poly Haven) → 64px 축소·공용 팔레트 양자화**

비교:

| 후보 | 장점 | 단점 |
|---|---|---|
| **CC0 사진** | 출처 깨끗(둘 다 CC0, 상업 이용·출처 표시 불필요), AI 공개 불필요, 실제 불규칙성이 있어 "고르게 뿌린 노이즈" 단서를 피함, EDC 환경 방식과 같은 계열, 스크립트화 쉬움 | 스타일 충돌 위험(Polycount) → 양자화·Point 로 완화. 라이브러리에 AI 가 섞였는지 장담 못 함 |
| 손으로 칠한 64px | 가장 안전한 "사람이 만졌다" 신호, EDC 는 소품에 이렇게 함 | **그릴 사람이 없다**: 도현은 학생 디렉터 1명, 3D 는 AI 가 만든다. 에셋 수십 개를 일관된 손으로 칠하려면 병목 |
| 절차적 생성 | 일관성·재현성 최고, 파일 출처 문제 없음 | 균일해져 "고르게 뿌린 노이즈"가 되기 쉬움 = 브리프가 피하려는 바로 그 AI 단서 |

**이유**: ① 우리 상황(실물 없음·그릴 사람 없음)에서 실행 가능한 유일한 "진짜 출처" ② EDC 가 환경에 쓴 방식이라 선례가 있음 ③ 글자·라벨·테이프·마모는 따로 스크립트로 얹는 규칙(5·6)과 충돌하지 않음.
**한계 [추정]**: 화물 *소품* 은 EDC 가 손 픽셀아트로 했으므로, 포장 화면의 대표 화물 몇 개(케이크 상자 등)는 나중에 도현이 직접 한두 개 손질하는 단계를 두는 걸 권한다(필수 아님).
**검증**: 기준 원화 옆에 같은 벽 하나를 사진→양자화로 만든 것과 비교해, 도현이 1× → 흑실루엣 → 4× 로 판정. 판정 전에는 확정하지 않는다.

---

## 5. 모르는 것
- 게이머가 AI 를 알아보는 **시각 단서**의 정량 연구(찾지 못함).
- 사진/포토스캔 텍스처 **단독** 에 대한 거부감 데이터.
- Puppet Combo·Dusk·Signalis·Mouthwashing 의 텍스처 제작 방식(자료 없음).
- Valve 원문에서 "내부 콘셉트 아트"가 공개 대상인지, 집행 강도.
- Quantic Foundry 본문의 용도별 정확한 %.
- ambientCG 공식 라이선스 페이지(2차 출처만 봄), CC0 사이트 내 AI 생성물 여부.
- Game Oracle 연구 원문(기사만 봄).
