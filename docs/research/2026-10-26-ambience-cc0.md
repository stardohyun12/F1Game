# 환경음 CC0 후보 목록 (PD 과제 20261026-0131)

> 지난 보고 `2026-10-26-map-gap-and-tools.md` §2-3, §3-1 순위 2 #2 의 첫 과제. **목록만 만들었다 — 다운로드 0개.** PD 가 고른 뒤 받는다.
> 증거 라벨: **[확인]** = Freesound 소리 페이지를 직접 열어 라이선스 줄·길이·형식·설명을 읽음 / [검색] = 검색 목록에서만 봄(길이·라이선스 줄 미확인) / [계산] / [추정] / [미확인].
> 생성 AI 소리: 후보에서 전부 제외(브리프 규칙 1). 목록에 든 소리는 전부 사람이 녹음했거나 사람이 만든 소리다.

## 결론 3줄

1. **자연음(파도·바람·갈매기·밧줄·먼 엔진)은 CC0 로 바로 채워진다.** 소리 페이지 34개를 열어 라이선스가 모두 `Creative Commons 0` 임을 확인했다. 길이는 대부분 1~7분이라 루프 편집 재료로 충분하다.
2. **사람 소리(웅성·스쿠터·라디오·가짜 중계)는 CC0 로 채워지지만 "그 동네 소리"가 아니다.** 시장 웅성 후보는 프랑스·영국 녹음이고, 방송·음악이 나오는 라디오는 CC0 에서 못 찾았다(`radio announcer commentary` 0건). 이 층은 도현 폰 녹음을 권한다(§3).
3. **Sonniss 는 CC0 가 아니다** — 재판매·AI 학습 금지 조항이 있고 약관이 2026-08-27 에 2.0 으로 바뀌었다. 목록에서 따로 두고 도현 판정을 구한다(§4).

---

## §0 방법과 한계

1. Freesound 검색에 라이선스 필터를 URL 로 건다: `https://freesound.org/search/?q=<짧은 낱말>&f=license%3A%22Creative+Commons+0%22&s=downloads+desc`. **낱말은 AND 조건**이라 길게 쓰면 0건이 나온다(실측: `seagulls waves harbor loop` 0건, `seagulls` 15건 이상).
2. 검색 목록의 괄호 숫자는 길이가 **아니다** — 요약 도구가 "초"로 읽었지만 개별 페이지의 길이(예: `Further_Roman/208074` = 2:14.689)와 맞지 않았다. **길이·형식·라이선스는 개별 소리 페이지에서만 읽었다.**
3. "원문 확인" = 각 소리 페이지의 License 줄이 `Creative Commons 0` 이고 링크가 `http://creativecommons.org/publicdomain/zero/1.0/` 임을 읽은 것. 페이지는 도구 요약을 거쳐 읽었다. **CC0 1.0 법률 본문 자체는 이번에 열지 않았다** [미확인] — 지난 보고에서 Freesound FAQ 로 "CC0 = 표기 불필요·상업 가능"은 확인([FAQ](https://freesound.org/help/faq/)).
4. **루프 가능 여부**: 34개 중 설명에 "loop/seamless"를 쓴 것은 0개(`Rmutt/145721` 은 태그에만 `loop`). 그래서 루프 칸은 **[추정]** — 1분 이상 + 뚜렷한 사건(경적·큰 울음)이 적으면 "크로스페이드로 쉽게", 사건이 많으면 "반복이 들킴", 10초 미만이면 "원샷(무작위 재생)".
5. 소리를 **들어 보지는 않았다.** 한 줄 판단은 설명·태그·길이에서 낸 것이다. 고를 때는 PD·도현이 귀로 확인해야 한다.

---

## §1 층별 후보

표기: 숫자 = 길이(분:초) · 형식(표본률/비트/채널) · 내려받기 횟수. 모든 라이선스 칸은 [확인] CC0 이다(예외는 따로 표시).

### 1-1 항구 — 갈매기·파도·밧줄·배 엔진 멀리

| ID | 소리 (작가) | URL | 길이·형식 | 루프 [추정] | 한 줄 판단 |
|---|---|---|---|---|---|
| H1 | harbour, seagulls-48k.wav (Further_Roman) | [208074](https://freesound.org/people/Further_Roman/sounds/208074/) | 2:14.689 · 48k/16/스테레오 · 2.2K회 | 쉬움(2분, 사건 적음) | **기본층 1순위.** 프랑스 브르타뉴 항구, 갈매기+물소리가 한 파일. Olympus LS-11 녹음 |
| H2 | Harbor Ambience 2 (clif_creates) | [254130](https://freesound.org/people/clif_creates/sounds/254130/) | 0:26.985 · 48k/24/**모노** · 2.5K회 | 보통(27초라 반복이 들릴 수 있음) | 부두에 매인 배·찰랑이는 물·돛/깃발 펄럭임. 모노라 **부두 끝 한 점에 둘 점음원**으로 좋음 |
| H3 | Mooring Rope.wav (Rmutt) | [145721](https://freesound.org/people/Rmutt/sounds/145721/) | 1:09.295 · 44.1k/16/스테레오 · 2.3K회 | 쉬움(삐걱임 연속). 태그에 loop 있으나 편집 여부는 설명에 없음 | **밧줄 층의 유일한 후보.** 샌프란시스코만 연락선을 묶은 굵은 밧줄이 말뚝에 삐걱임 |
| H4 | Sea waves in the foreground boat engine in background.wav (GiovanniProvenzale) | [548200](https://freesound.org/people/GiovanniProvenzale/sounds/548200/) | 3:48.578 · 48k/16/스테레오 · 971회 | 쉬움(3분 49초) | **"배 엔진 멀리" 용.** 파도가 앞에 깔려 있어 파도와 따로 못 쓰고, 항구·해안 파도층을 겸함 |
| H5 | Waterfront_Seagulls (LewisEmmott5) | [732700](https://freesound.org/people/LewisEmmott5/sounds/732700/) | 0:54.988 · 48k/24/모노 · 283회 | 보통 | 갈매기 점음원/무작위 재생용. 태그가 `River` 라 바다가 아닐 수 있음 |
| H6 | Seagull single call (steaq) | [263786](https://freesound.org/people/steaq/sounds/263786/) | 0:00.395 · ogg 44.1k/스테레오 · 6.2K회 | 원샷 | 갈매기 한 번 울음. ⚠ 설명: **"더 긴 자유 이용 녹음에서 한 번을 잘라냈다"** — 원본 출처가 적혀 있지 않다. 쓰려면 이 문장을 장부 `notes` 에 인용하거나 H5 에서 직접 잘라 쓰기 |
| H7 (예비) | Baltimore Harbor with Distant Industrial Ambience (flood-mix) | [413334](https://freesound.org/people/flood-mix/sounds/413334/) | 1:33.250 · 48k/24/스테레오 · 1.4K회 | 쉬움 | 멀리 공장 웅웅이 깔림 — 마을 항구엔 무거움. 산업 느낌이 필요할 때만 |
| H8 (예비) | Havengeluid.WAV (Toetje30) | [618494](https://freesound.org/people/Toetje30/sounds/618494/) | 1:56.194 · 44.1k/16/스테레오 · 41회 | 쉬움 | 네덜란드 항구, **밤 22시 녹음**. 무드 프리셋 "창 불"(D-137) 밤 항구 후보 |

### 1-2 상점가 — 웅성·가게 종·라디오 멀리·스쿠터 지나감

| ID | 소리 (작가) | URL | 길이·형식 | 루프 [추정] | 한 줄 판단 |
|---|---|---|---|---|---|
| S1 | MARKET (SamuelGremaud) | [544338](https://freesound.org/people/SamuelGremaud/sounds/544338/) | 1:33.113 · 48k/24/스테레오 · 355회 | 쉬움 | **웅성 1순위 CC0.** 프랑스 남부 Sault 장터(Zoom H4n Pro, 방풍망). 사고파는 프랑스어가 들릴 수 있음 → 동네 정서와 맞는지 도현 귀 판정 |
| S2 | Medium Crowd Murmering (jentlemen) | [432331](https://freesound.org/people/jentlemen/sounds/432331/) | 0:57.393 · 44.1k/16/스테레오 · 7.1K회 | 쉬움 | 말이 알아들을 수 없게 뭉개진 웅성(`indistinct`). **작은 회의장 안**이라 울림이 있어 야외 상점가엔 어색 [추정] → 가게 안·시네마틱 가게 장면용 |
| S3 (예비) | 2022-11-12-binaural-london-market-ambience.wav (ilmari_freesound) | [659634](https://freesound.org/people/ilmari_freesound/sounds/659634/) | 2:24.591 · **192k**/24/스테레오 · 162회 | 쉬움 | 영국 시장. **바이노럴(헤드폰용 처리)** 이라 Unity 3D 소리 위치와 겹침 [추정]. 원본 ≈167 MB [계산: 144.6초×192000×6바이트] |
| S4 | Shop door bell.wav (775noise) | [494565](https://freesound.org/people/775noise/sounds/494565/) | 0:07.759 · 44.1k/16/모노 · 9.3K회 | 원샷 | **가게 종 1순위.** 채소가게 문 열릴 때의 종 |
| S5 | R04-62-Shop Bell.wav (craigsmith) | [479988](https://freesound.org/people/craigsmith/sounds/479988/) | 0:13.607 · 48k/24/모노 · 222회 | 원샷 | 작은 유리/금속 종 + 부스럭. 설명이 "디지털화한 빈티지 효과음, 잡음 있을 수 있음" — S4 대안 |
| S6 | MotorCycle passby.wav (jumpbug99) | [699643](https://freesound.org/people/jumpbug99/sounds/699643/) | 0:20.409 · 44.1k/24/스테레오 · 29회 | 원샷 | 밤 교외 오토바이 한 대 지나감(MSH-6 + Zoom H5). **스쿠터가 아니라 오토바이**, 배기음 큼 |
| S7 | Thailand traffic heavy mopeds motorcycles ... passbys great detail.flac (kyles) | [177737](https://freesound.org/people/kyles/sounds/177737/) | 4:53.709 · FLAC 48k/24/스테레오 · 75회 | 원재료 | 모페드·오토바이가 좌우로 연달아 지나감. **교통량이 많아 골목엔 과함** → 지나감 1~2회씩 잘라 쓰는 원재료 |
| S8 | Vintage Radio Tuning 5.WAV (exsil) | [652596](https://freesound.org/people/exsil/sounds/652596/) | 1:50.777 · 44.1k/16/스테레오 · 774회 | 보통 | 구형 라디오로 주파수를 훑는 정적·잡음. **말·음악 없음**(설명). "가게에서 멀리 흘러나오는 라디오"로는 부족 → 라디오 방송은 폰 녹음 (§3). 정적은 시네마틱(§1-6)에 |

**"라디오 멀리" 층**: CC0 에서 방송 음성·음악 후보를 못 찾았다 — `radio announcer commentary` 검색 0건 [측정: 검색 URL]. 이 층은 CC0 후보가 없다.

### 1-3 언덕 — 바람·풀·먼 개 짖음

| ID | 소리 (작가) | URL | 길이·형식 | 루프 [추정] | 한 줄 판단 |
|---|---|---|---|---|---|
| Hl1 | Wind_grass_sheep_Ouessant.wav (nicotep) | [569321](https://freesound.org/people/nicotep/sounds/569321/) | 5:04.000 · 48k/16/스테레오 · 563회 | 쉬움 | **풀 바람 1순위.** 브르타뉴 오우에상 섬, 바람이 풀을 훑고 배경에 양 울음. 양이 거슬리면 EQ 로 못 뺌 → 목장 느낌으로 받아들일지 판정 |
| Hl2 | Wind gusts grass (fran_marenco) | [853991](https://freesound.org/people/fran_marenco/sounds/853991/) | 5:58.208 · 48k/24/스테레오 · 91회 | 쉬움 | **돌풍.** 강한 바람이 풀을 훑는 소리(Cabo Raso, 2026-01). 꼭대기에서 볼륨이 커지는 층에 |
| Hl3 | Ambience Stereo_Distant Wind In Trees And Dog (JORDI77RS) | [864277](https://freesound.org/people/JORDI77RS/sounds/864277/) | 1:08.359 · 48k/24/스테레오 · 31회 | 쉬움 | **먼 개 1순위.** 나무 사이 바람 + 멀리 개 짖음(폴란드 Wilga, 2026-07, Zoom H5). 신규 업로드라 평판 자료는 적음 |
| Hl4 | Rural atmosphere with little birds, distant waves, distant dog barking (Chris.sonido.peru) | [855887](https://freesound.org/people/Chris.sonido.peru/sounds/855887/) | 4:24.939 · 48k/24/스테레오 · 38회 | 보통(모토택시 지나감이 사건) | **"해안 언덕" 한 덩어리.** 작은 새, 먼 파도, 먼 개, 이웃 소리, 지나가는 모토택시, 마이크에 바람. 우리 장면(언덕에서 바다가 들림)에 가장 가깝지만 섞인 게 많아 **편집 필요** |
| Hl5 (예비) | dog-distant barking.wav (alberto59) | [615258](https://freesound.org/people/alberto59/sounds/615258/) | 0:33.483 · 48k/16/모노 · 157회 | 보통 | 발칸 마을 오후, **멀리 자동차·트럭 소음이 섞임** → 언덕엔 부적합 |
| — (제외) | City far away with slight wind in dry grass ... traffic hum and plane (felix.blume) | [685791](https://freesound.org/people/felix.blume/sounds/685791/) | 4:29.507 · 48k/24/스테레오 · 178회 | — | 풀바람이지만 **교통 웅웅·비행기**가 들어 있음(제목 그대로) → 제외 |

### 1-4 해안 — 파도·바람

| ID | 소리 (작가) | URL | 길이·형식 | 루프 [추정] | 한 줄 판단 |
|---|---|---|---|---|---|
| C1 | Waves, Atlantic ocean, on pebbly shore, PEI, 2011.wav (TRP) | [577326](https://freesound.org/people/TRP/sounds/577326/) | 3:05.907 · 48k/24/스테레오 · 151회 | 쉬움 | **파도 1순위.** 자갈 해변의 낱낱의 파도(Sony M10). 파도 하나하나가 분명해 속도에 따라 볼륨만 조절하는 층으로 좋음 [추정] |
| C2 | Ocean Surf Along the Coast of Maine (be-steele) | [362414](https://freesound.org/people/be-steele/sounds/362414/) | 7:19.286 · 48k/24/스테레오 · 918회 | 매우 쉬움(7분) | 메인주 바위턱에 부서지는 큰 파도(Zoom H2 내장 마이크). 가장 길어 **반복이 안 들림**. 파도가 큼 → 항구보다 해안 직선에 |
| C3 | Ocean waves crushing on the chilean shore. (felix.blume) | [155641](https://freesound.org/people/felix.blume/sounds/155641/) | 3:00.389 · **96k**/24/스테레오 · 3.6K회 | 쉬움 | 바위에 가까이 부서지는 파도. 길 가장자리에 둘 **가까운 점음원**. 원본 ≈104 MB [계산: 180.4초×96000×6바이트] → 임포트 때 줄이기 |
| C4 | Wind at Ocean shore. (Tom_Kaszuba) | [659004](https://freesound.org/people/Tom_Kaszuba/sounds/659004/) | 1:05.993 · 48k/24/스테레오 · 733회 | 쉬움 | 해안 바람(Sony PCM-D100). 지금 `WindAmbience.cs` 의 코드 합성 바람과 **겹치게 쓸지 바꿀지**는 PD 판정 (BayLoop 씬엔 그 스크립트가 아직 안 붙어 있음 — 지난 보고 §1) |

### 1-5 정비소 — 공구·셔터

| ID | 소리 (작가) | URL | 길이·형식 | 루프 [추정] | 한 줄 판단 |
|---|---|---|---|---|---|
| W1 | Air Impact Wrench (sevenbsb) | [349398](https://freesound.org/people/sevenbsb/sounds/349398/) | 0:07.017 · 44.1k/16/스테레오 · 2.8K회 | 원샷 | **공구 원샷 1순위.** 에어 임팩트 렌치의 "드르르륵"(FORTGPRO FG3200) |
| W2 | Tools Ratchet.wav (CapsLok) | [181634](https://freesound.org/people/CapsLok/sounds/181634/) | 0:20.421 · 44.1k/24/모노 · 2.8K회 | 원샷(잘라 쓰기) | 작은 차고에서 가까이 녹음한 래칫 여러 번 + 방 울림. 실내 정비소와 맞음 |
| W3 | metal shop workshop ... disc grinder Havana, Cuba 2008.flac (kyles) | [362061](https://freesound.org/people/kyles/sounds/362061/) | 4:00.115 · FLAC 48k/16/스테레오 · 487회 | 보통 | 쿠바 금속 작업장에서 그라인더로 차 부품을 가는 소리. **"배경 작업음" 베드**로 쓸 수 있으나 그라인더 소리는 날카롭고 클 것 [추정, 듣지 않음] |
| W4 | roller_shutter_la_meandre (nicotep) | [773732](https://freesound.org/people/nicotep/sounds/773732/) | 2:05.000 · **96k**/16/스테레오 · 91회 | 원샷(잘라 쓰기) | **셔터 1순위.** 큰 창고의 낡은 셔터를 손 크랭크로 올리는 소리, 삐걱임 많음. 원본 ≈48 MB [계산: 125초×96000×4바이트] |
| W5 | RollerShutter_IT.wav (uniuniversal) | [617980](https://freesound.org/people/uniuniversal/sounds/617980/) | 0:20.600 · 44.1k/**32비트**/스테레오 · 194회 | 원샷 | 이탈리아 실내 롤 셔터(햇빛 가리개). 정비소 셔터치고 **가벼움** [추정] |

### 1-6 시네마틱 — 라디오 F1 중계 느낌

**전제(PD)**: 실제 중계 녹음은 저작권 때문에 못 쓴다. CC0 검색에서도 방송·해설 음성은 0건이었다.

| ID | 소리 (작가) | URL | 길이·형식 | 한 줄 판단 |
|---|---|---|---|---|
| K1 | Vintage Radio Tuning 5.WAV (exsil) | [652596](https://freesound.org/people/exsil/sounds/652596/) | 1:50.777 · 44.1k/16/스테레오 | 정적·주파수 훑는 소리 — **무전기 배경 질감** |
| K2 | Walkie Talkie - Roger Beep (bruce965) | [321906](https://freesound.org/people/bruce965/sounds/321906/) | 0:00.650 · FLAC 44.1k/16/모노 · 3.2K회 | 말이 끝날 때 "삑". **작가가 Audacity 로 합성한 2 kHz 사인파** + 끊긴 전송 소리(설명) — 사람 목소리 없음 |
| K3 | Distant Loud Sports Car Vroom (Mr_KeybOred) | [377148](https://freesound.org/people/Mr_KeybOred/sounds/377148/) | 0:05.222 · 44.1k/16/스테레오 · 3.5K회 | 멀리 달려가는 람보르기니. **작가가 Audacity 로 리버브·압축을 입힘**(설명). "서킷 먼 배경" 용 |
| K4 | Rally_Bohemia_V2_Sportcar_Passby_Opel_Ascona_A_Distant.wav (eklensounds) | [536281](https://freesound.org/people/eklensounds/sounds/536281/) | 0:26.607 · **96k**/24/스테레오 · 122회 | 숲속 50 m 에서 녹음한 랠리카 먼 지나감. 더 '레이스' 같음. "Rally Bohemia 2" 무료 컬렉션의 일부 — **이 소리의 CC0 만 확인, 컬렉션 전체 약관은 [미확인]**. 원본 ≈15 MB [계산] |

**목소리(중계 해설) 대안** — 소리 틀 안에서 고르는 안:

| 안 | 내용 | 분류 | 판단 |
|---|---|---|---|
| **A** | **도현이 직접 녹음한 가짜 중계** — 알아들을 수 없는 웅얼이나 짧은 대사를 해설 억양으로. 우리 스크립트로 대역 통과(약 300~3400 Hz)+약한 왜곡+K1 정적을 섞어 무전 질감 | 직접(도현 소유 + 우리 코드) | **권장.** 억양이 있는 사람 목소리는 CC0 에 없다. 저작권도 AI 도 걸리지 않음 |
| B | 목소리 없이 K1(정적)+K2(삑)+K3/K4(먼 엔진)+S2(군중 웅성)만 | 직접 | 가능하나 '중계'의 억양이 없어 느낌이 약함 — 도현 판정 |
| C | 실제 F1 중계 녹음·영상에서 추출한 소리 | **금지** | PD 전제(저작권) |
| D | 합성 음성(TTS 등 AI)으로 가짜 중계 | **금지** | 브리프 규칙 1: AI 생성 파일 0 |
| E | 저작권이 풀린 옛 방송 녹음 | 미조사 | 가능성은 있으나 이번엔 안 찾음 [미확인] |

---

## §2 최소 세트 제안 (PD 가 고를 때 시작점)

**제안일 뿐이다** — 귀로 확인하고 정한다. 층당 1~3개, 합계 약 16개, 원본 합계는 대부분 수십 MB (큰 것은 96 kHz 3개: C3≈104, W4≈48, K4≈15 MB [계산]).

| 층 | 제안 | 비고 |
|---|---|---|
| 항구 | H1 기본 + H3 밧줄 + H4 먼 엔진(+예비 H2) | H1 이 갈매기를 포함하므로 H5/H6 는 불필요할 수 있음 |
| 상점가 | S4 종 + S1 웅성(또는 폰) + S6/S7 지나감(또는 폰) | 라디오는 CC0 없음 → 폰 |
| 언덕 | Hl1 풀 + Hl2 돌풍 + Hl3 먼 개 | Hl4 는 "해안 언덕" 한 덩어리로 대체 가능 |
| 해안 | C1 파도 + C4 바람 (+C2 긴 베드) | |
| 정비소 | W1 임팩트 + W2 래칫 + W4 셔터 | W3 그라인더는 귀로 판정 |
| 시네마틱 | K1 + K2 + K3 (+ 도현 목소리 A안) | |

---

## §3 도현 폰 녹음으로 바꿀 층

기준: **말·악기·방송이 들어가는 소리**는 CC0 여도 어느 나라 소리인지 드러난다. 자연음(파도·바람·갈매기)은 CC0 로 충분하다. D-127 이 소망으로 적은 소리가 바로 "오토바이, 적당히 떠드는 사람들, 어릴 때 친구들과 떠들던 소리"(`art/_briefs/2026-10-23-art-direction-v2.md:54` [확인])다.

| 층 | 권고 | 이유 |
|---|---|---|
| 상점가 웅성 | **폰 녹음 권장** (CC0 S1 은 임시) | 프랑스·영국 녹음이라 동네 말투·분위기가 다름 [추정] |
| 스쿠터/오토바이 지나감 | **폰 녹음 권장** (CC0 S6/S7 은 임시) | D-127 에 적은 그 소리. 동네 골목의 배기음·속도가 정서의 핵심 |
| 라디오 멀리 | **폰 녹음 필수** | CC0 에 방송·음악 후보 없음. 가게 라디오·TV 가 작게 새는 소리를 폰으로 |
| 시네마틱 가짜 중계 | **도현 목소리 권장** (§1-6 A) | 억양 있는 목소리는 CC0 에 없음 |
| 정비소 셔터·공구 | 선택 | W4/W1 로 충분하나 도현이 아는 정비소가 있으면 더 구체적 |
| 먼 개 짖음 | 선택 | Hl3 이 깔끔 |
| 항구·해안·언덕 자연음 | CC0 그대로 가능 | 언어가 없음. 도현이 가는 바닷가 녹음은 선택 |

**폰 녹음 팁(리서치팀 제안)**: 30초 이상, 말하는 도중 끊지 말 것, 바람막이(손으로라도 가림), 가능하면 WAV/무손실. 사람 얼굴을 녹음하는 게 아니라도 **알아들을 수 있는 대화**가 들어가면 초상·사생활 문제가 될 수 있음 [추정] → 웅얼림 수준만 쓰기.

---

## §4 Sonniss — 도현 판정 필요

목록에 파일을 올리지 않았다(번들 내부를 열어 보지 않음). 판정할 약관만 적는다.

| 항목 | 내용 | 출처 |
|---|---|---|
| 지금 번들 | **GDC 2024**: 9개 파트·23.5 GB. **GDC 2025 번들은 없다.** **GDC 2026** 은 별도 사이트(`gdc.sonniss.com`) — 7.47 GB+·17개 제작사 (2026-03-16 기사) | [gameaudiogdc 페이지](https://sonniss.com/gameaudiogdc/) [확인], [Rekkerd](https://rekkerd.org/sonniss-releases-gdc-2026-game-audio-bundle/) [2차: 크기는 기사에서만, Sonniss 페이지 미확인] |
| 허용 | 로열티 무료·상업 가능·표기 불필요·무제한 프로젝트. 게임·영상·앱 등 "interactive projects" 에 넣는 것 허용 | [약관 페이지](https://sonniss.com/gdc-bundle-license/) [확인] |
| 금지 | 소리를 **"효과음 그대로" 판매·배포 금지**(편집·재디자인본도 같음, 완성작은 제외). **AI 학습 목적 사용 금지**(서면 허가 없이). 원작자인 척 금지 | 같은 약관 [확인] |
| 약관 버전 | **v2.0, 2026-08-27 시행.** 그 전에 받은 파일은 **받은 날 공개된 약관**을 따른다 | 같은 약관 [확인] |
| CC0 와 다른 점 | 이 약관은 CC0 가 아니다 → 우리 "CC0 필터" 규칙(지난 보고 R2)과 별개로 도현 허용이 필요 | 지난 보고 §2-3 |

**도현이 정할 것**: ① Sonniss 를 소리 원천으로 허용할지. ② 허용하면 장부에 "Sonniss 번들 연도·약관 버전·폴더/파일명"을 적는다. ③ "AI 학습 금지"는 우리 AI 에이전트가 파일을 열어 분석·정리하는 것과 다르다고 읽는 것이 자연스러우나(학습=모델을 훈련) **법적 해석이 아니다** [추정] — 걱정되면 허용하지 않는다.

---

## §5 장부 기록 (`art/provenance.csv`)

현재 열: `asset_id,path,source_url,license,accessed,tool,ai_generated,sha256,notes` [측정: `art/provenance.csv` 1행]. 소리에 맞춘 한 줄 예시(**받은 뒤에** 채움 — 지금은 만들지 않았다):

```
amb_harbor_h1,F1/Assets/Sound/Ambience/harbor_h1.ogg,https://freesound.org/people/Further_Roman/sounds/208074/,CC0-1.0,<받은 날>,<편집 도구>,no,<sha256>,"Further_Roman, original 2:14.689 48k/16 stereo; crossfade loop; Freesound license line read <확인한 날>"
```

규칙: ① `source_url` 은 **소리 페이지**(검색 페이지 아님) ② `notes` 에 작가·원본 길이·편집 내용 ③ 받은 날 License 줄을 한 번 더 확인하고 그 날짜를 적는다(작가가 나중에 라이선스를 바꿀 수 있음 [추정]) ④ H6 처럼 "다른 녹음에서 잘라냄"이 적힌 것은 그 문장을 인용.

---

## §6 모르는 것

| # | 모르는 것 | 어떻게 풀까 |
|---|---|---|
| 1 | **들어 보지 않았다** — 한 줄 판단은 설명·태그·길이 기반 | PD·도현이 층당 후보를 귀로 확인 |
| 2 | 루프 가능 여부는 전부 [추정] (설명에 루프 언급 없음) | 받은 뒤 크로스페이드 시험 |
| 3 | CC0 1.0 법률 본문 직접 열람 안 함. Freesound 가 업로더의 CC0 선언을 검증하는지 모름 | H6 같은 "출처 불명" 소리는 피하고 장부에 날짜·URL 기록 |
| 4 | Rally Bohemia 2 컬렉션(K4) 전체 약관 | K4 를 쓰려면 컬렉션 페이지 확인 |
| 5 | Sonniss 번들 내부 폴더·소리 (열어 보지 않음), 2026 번들 크기의 Sonniss 측 확인 | 도현이 허용하면 그때 |
| 6 | 한국/일본 느낌의 CC0 시장·골목 녹음이 Freesound 에 있는지 — 이번엔 `crowd`, `market ambience` 만 검색 | 필요하면 `korea`·`japan`·`kyoto` 같은 낱말로 추가 검색 |
| 7 | 방송·음악이 나오는 라디오의 저작권이 풀린 옛 녹음(E안) | 필요하면 따로 조사 |
| 8 | 도현의 취향(`TASTE.md`) 미열람 — 귀로 고르는 일이라 영향이 큼 | 도현 판정 |
