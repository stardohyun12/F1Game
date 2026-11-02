# 세상 속 정보 + 최소 화면 UI — 1인칭 배달 게임에 가져올 것 (D-267)

2026-11-02 · PitCrew 리서치 · PD 과제 20261102-2255 · **조사. 승인 아님. 규칙·추천은 전부 "제안".** 자체 점수: 해당 없음(리서치, 그림 판 아님).
도현 질문: "아예 인게임으로 ui정보 주고 최소한의 정보만 ui로 띄우는건 어떻게 생각해? 아니면 뭔가 가져올만한 레퍼런스 있나"

- 표기: **[관찰]** 이번 세션에 Steam 공식 스토어 스크린샷을 직접 열어 본 것(게임당 7~24장 전부 훑음) · **[2차]** 위키·기자·블로거 글(개발자 말 아님) · **[개발자]** 개발자 본인 말 · **[추정]** 내 판단 · **[미확인]** 못 찾음.
- ⚠ 스토어 컷은 개발사가 고른 홍보 컷이라 HUD 가 일부러 빠졌을 수 있다. "HUD 0" 은 "그 컷에서 0" 이다. 화면 %는 **눈대중**(Lake·Firewatch 새로 잰 값) 또는 11-01 보고서 값을 가져온 것(표시함).
- 쓰지 않은 곳: gameuidatabase.com(CAPTCHA·AI 사용 금지 약관). 이미지는 저장소에 저장하지 않았다 — 내려받은 컷은 저장소 밖 임시 폴더(`%LOCALAPPDATA%\Temp\r1`)에서 훑어보기만 했고, 삭제는 권한이 거부돼 **아직 남아 있다**(저장소엔 없음, 도현이 지워도 된다). **링크만** 붙인다.
- 앞선 조사와의 관계: `docs/research/2026-11-01-ui-refs.md`(11게임 해체)·`docs/design/DELIVERY-LOOP.md`(장면별 UI 표). 거기서 이미 정한 것(D-238·240·242·243)은 **바꾸지 않는다.** 이번엔 "우리 장면 5곳에 무엇을 가져올까"에 맞췄고, Firewatch·Lake 는 이번이 처음이다.

---

## 0. 결론 3줄

1. **도현의 방향(세상 물건이 정보를 주고, 화면 UI 는 최소)은 맞다.** 단 9게임에서 그렇게 한 건 Firewatch·Jalopy 둘뿐이고(둘 다 스토어 컷 HUD 0), 배달이 본업인 Lake·Mail Time·Easy Delivery Co.는 **작은 화면 UI 를 같이 쓴다**(미니맵·키 칩·떠오르는 돈 글자). 우리 UI 1판이 이미 그쪽(화살표·[E] 칩·말풍선)이니 **이번엔 "읽을 정보"를 손에 든 종이로 옮기는 게 남은 일.**
2. **제일 값진 하나: "손이 종이를 들어 올리고, 종이 뒤로 세상이 그대로 보이는" 동작**([Firewatch 지도 컷](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/383870/ss_4b9d67ae2af0da570d03731d93b095d0203b973d.1920x1080.jpg)). 칠판 쪽지·전표·약도·일기가 전부 같은 동작 하나로 덮인다 → **"손에 든 종이" 공용 모듈 1개(M)** 가 이번 제안의 뼈대다. 둘째는 **화면 밖에서 말하는 사람 = 손에 든 무전기**(Firewatch) — 우리 대화의 빈 곳(말하는 사람이 화면 밖일 때)과 맞는다.
3. **장면별 크기 합계: S 2 · M 3 · L 0.** 화물 상태·대화(화면 밖 화자)는 S, 칠판 주문·전표/길 찾기·하루 끝은 M(공용 모듈을 쓰면 첫 장면만 M, 나머지는 S 로 줄어든다 **[추정]**). 반면교사 하나: Death Stranding 은 주문·결과가 전부 풀스크린 홀로그램 목록이라 우리 "손으로 쓴 종이"와 반대 문법이다.

---

## 1. 인게임 정보 + 최소 UI — 내 판단 (제안)

**찬성한다. 단 조건 3개.**

1. **상시 화면 면적은 4% 안.** 11-01 조사에서 11게임 상시 HUD 가 1.7~4.2%였고, 이번 Lake 의 상시 HUD(둥근 미니맵 + 주소 한 줄)도 눈대중 2~3%였다. "읽는 물건"은 **켠 사람이 부를 때만** 크게 떠도 된다(Jalopy 지도책 55.8% — 11-01 값, Firewatch 지도 눈대중 20~25%). 내려놓으면 0%.
2. **세상에 둔 정보는 읽혀야 한다.** [Jalopy 계기판 컷](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/446020/ss_57bdff00bda596a7049f293e7909c99bf851dbac.1920x1080.jpg)은 어두워서 바늘 숫자가 안 읽힌다 **[관찰, 컷 한 장 기준]**. 세상 속 종이는 "들면 확대"를 같이 만든다.
3. **60번 반복을 견뎌야 한다.** 방학 28일에 평일 20일 × 하루 2~3건이면 약 60번(DELIVERY-LOOP §0). Firewatch 는 지도·나침반을 **매번 직접 꺼내게** 해서 일부러 불편하게 만들었다는 해석이 있다 **[2차: [Game Developer 재게시 글](https://www.gamedeveloper.com/design/how-firewatch-s-ui-enhances-player-immersion), 개발자 인용 없음]** — 우리는 배달이 반복이라 **"꺼내기 동작은 한 건당 1번"** 으로 줄인다 **[추정]** (칠판에서 쪽지를 집으면 그 종이가 곧 전표가 된다).

⚠ **개발자 본인이 UI 이유를 쓴 글은 못 찾았다.** 검색한 곳: Firewatch(Campo Santo)·Jalopy(Minskworks)·Lake(Gamious)·Death Stranding(Kojima Productions)·Mail Time·EDC. 찾은 건 EDC 개발자 인터뷰 1건([80.lv](https://80.lv/articles/inside-the-making-of-easy-delivery-co), UI 이야기는 거의 없음)과 Short Hike 개발 글([PlayStation Blog](https://blog.playstation.com/2021/08/05/crafting-a-tiny-open-world-a-look-behind-the-scenes-at-the-creation-of-a-short-hike/), 11-01 때 연 것). **나머지 칸은 화면 관찰이다.**

---

## 2. 게임별 해체 (① 세상 물건 ② 화면에 남긴 것 ③ 대화 ④ 우리 장면에 가져올 것)

> 컷 번호는 `store_item_assets` 스크린샷 순번(00부터). 링크는 §4 모음.

### Firewatch — [Steam](https://store.steampowered.com/app/383870/) · 컷 7장 (02·04·05 인용)
- ① **[관찰]** **손에 든 무전기**(02·05: 화면 왼쪽 아래, 눈대중 6~8%, 손+기기) — 상대 목소리가 올 때 꺼내는 물건. **종이 지도 + 나침반**(04: 두 손으로 지도를 들고 오른손에 나침반, 눈대중 20~25%) — **지도 뒤로 산과 하늘이 그대로 보인다.** [2차: [Game Developer 재게시 글](https://www.gamedeveloper.com/design/how-firewatch-s-ui-enhances-player-immersion)] 나침반은 방향 볼 때마다 직접 꺼낸다.
- ② **[관찰]** 7장 전부 화면 UI 0. [2차] 상호작용 마커와 무전 답장 아이콘은 가끔 쓴다(개발자 글 아님, 컷에는 안 보임).
- ③ [2차] 상대(Delilah)는 무전기 목소리, **선택지만 화면 글자**. 스토어 컷에 대사 장면 없음 **[미확인]**.
- ④ **전표·약도 = 04 의 "손이 든 종이, 뒤로 세상이 보임". 화면 밖 화자(라디오·전화) = 02 의 "손에 든 무전기".**

### Jalopy — [Steam](https://store.steampowered.com/app/446020/) · 컷 16장 (01·02·03·04·10·11 인용)
- ① **[관찰]** 부품 설명 = 부품 옆에 뜨는 **스프링 노트 한 장**(01). 속도·연료 = **계기판 바늘**(02). 경로 = **지도책 펼침**: 왼쪽 DDR 지도, 오른쪽 **손글씨 페이지 "날짜 · 구간 · Route 1/2/3"**(04). 주문 = **카탈로그 책 + 오른쪽 주문서 종이**(11: 합계 줄, Cancel / ORDER). 상점은 카운터에 가격 간판(10) + **내 지갑 속 지폐 "300"**(10). 주유소 간판에 **시계 "01:00"**(05). 길 위 **큰 지명 표지**(03).
- ② **[관찰]** 16장 모두 화면에 얹은 HUD 가 없다. 읽을 때만 물건이 커진다(지도책 55.8%, 수첩 11.7%, 계기판 3.5% — 11-01 값).
- ③ 대사 컷 없음. 상점 주인은 말 없이 서 있다(10·11) **[관찰]**.
- ④ **칠판 주문 = 11 의 카탈로그+주문서 / 하루 끝 = 04 오른쪽 손글씨 페이지 / 정산 = 10 의 지갑 속 지폐 / 길 = 03 큰 표지.**

### Easy Delivery Co. — [Steam](https://store.steampowered.com/app/3293010/) · 컷 9장 (01·02·03·05 인용)
- ① **[관찰]** 화물 = **트럭 짐칸의 나무 상자**(01). 배달하면 **상자가 문 앞 바닥에 놓인다**(03). 도로 옆 표지판(05).
- ② **[관찰]** 03: 상자 곁에 **"+ $9.82 / + $2.32" 흰 도트 글자**가 떠오른다(11-01 값 2.4%). 평소 HUD 는 컷에 없다.
- ③ **[관찰]** 02: 가게 장면에서 **박스 없이 흰 도트 글자 두 줄**(화면 왼쪽 위, 인물 쪽).
- 주문 받기·목적지 표시: **[미확인]** — 스토어 컷·위키·80.lv 인터뷰 어디에도 없다. 플레이 영상 확인 필요.
- [개발자] "it all has to read well on the made-up 456x256 resolution screen" · 패키지는 "simple pixel art textures" · UI 도 Aseprite 로 만든다 — [80.lv](https://80.lv/articles/inside-the-making-of-easy-delivery-co).
- ④ **화물 상태·배달 = 01·03 의 짐칸 실물 상자 → 문 앞에 내려놓기. 정산 글자는 대상 곁에서 몇 초만.**

### A Short Hike — [Steam](https://store.steampowered.com/app/1055540/) · 컷 7장 (03 인용)
- ① 이 컷들에선 정보를 세상 물건으로 옮긴 흔적을 못 봤다(표지판 정도) **[미확인]**.
- ② **[관찰]** 6장 HUD 0.
- ③ **[관찰]** 03: 크림 말풍선 + **말하는 사람 쪽 꼬리**, 굵은 도트 글자 한 줄, 이름표 없음. 11-01 에서 격자 맞춤 확인.
- ④ **대면 대화 = 03 문법. 우리 구현(머리 위 말풍선)과 같은 계열이라 바꿀 이유 없음.**

### Death Stranding Director's Cut — [Steam](https://store.steampowered.com/app/1850570/) · 컷 24장 (00·03·20·21 인용) — 반면교사
- ① **[관찰]** **화물이 등 위에 실물로 쌓여 있다**(00·03). 박스에 **"FRAGILE" 큰 인쇄**(20).
- ② **[관찰]** 21: 위 가운데 **홀로그램 타이머**(11-01 값 2.8%). 11-01 에서 확인: 주문·결과는 **풀스크린 홀로그램 목록**, 거리·무게는 상시 HUD.
- ③ 스토어 컷에 대사 장면 없음 **[미확인]**.
- [2차, 검색 요약만 — 원문 열기 실패] PC Gamer 는 속편이 "여러 주문을 한 번에 내는 요약 화면"을 넣었다고 쓴다([링크](https://www.pcgamer.com/games/action/11-little-death-stranding-2-improvements-that-made-me-say-hell-yeah)) — 첫 작품은 주문마다 완료 화면을 눌러 넘겼다는 뜻으로 읽히지만 **원문 인용은 못 했다 [미확인]**.
- ④ **가져올 것: 20 의 상자 표면 라벨(취급 주의를 상자 자체가 말함). 안 가져올 것: 홀로그램 목록·타이머.**

### Mail Time — [Steam](https://store.steampowered.com/app/1607240/) · 컷 9장 (01·04 인용)
- ① **[관찰]** 보상 = **엽서 종이**(04: "dear player, Congrats! … with love, Mail Scouts", YAY 버튼, 풀스크린). NPC 머리 위 **노란 "!"**(01)이 "할 말 있음". [2차] 편지를 **받는 사람 손에 직접** 건넨다(주소 없이 찾아다님, [mkaugaming 리뷰](https://www.mkaugaming.com/all-review-list/mail-time-nintendo-switch-review/) 1건).
- ② **[관찰]** 세계 컷 HUD 0(11-01).
- ③ **[관찰]** 01: 하단 가운데 큰 크림 상자 + **오른쪽 위 초록 이름 알약 + 가운데 아래 [E] 칩**(11-01 값 19.6%).
- ④ **하루 끝 보상 = 04 엽서(종이가 보상). "!" = 대화 가능 표시.** 큰 하단 상자는 우리 "아래 검은 띠 거부"(D-237)에 가까워 안 가져온다 **[추정]**.

### Lil Gator Game — [Steam](https://store.steampowered.com/app/1586800/) · 컷 7장 (01·03 인용)
- ① **[관찰]** 메뉴가 **스프링 노트**(01: 가위·크레용·스티커, 물건 설명이 노트 한 장) — 풀스크린이지만 **물건 문법**.
- ② **[관찰]** 01 왼쪽 위 파란 알약 "314 / −100"(돈) + 아래 버튼 안내. 세계 컷 HUD 0.
- ③ **[관찰]** 03: 하단 큰 주황 테두리 상자 + 왼쪽 위 이름 탭 + 오른쪽 아래 잎 모양 "다음"(11-01 값 19.1%).
- ④ **하루 끝 일기·일시정지 = 01 노트 문법**(이미 DELIVERY-LOOP §3 에 있음).

### Lake (Gamious) — [Steam](https://store.steampowered.com/app/1118240/) · 컷 12장 (02·03·05·07 인용) — 배달 소재, 1인칭 아님(3인칭 걷기)
- ① **[관찰]** **집에 번호가 붙은 우편함**(05: 우편함 "100" 옆에 선 주인공, 화면 아래 주소 글자도 "100 New St") — 도착은 **세상 물건(번호)** 으로 확인. 택배는 **양손에 든 상자**로 보인다(07). [2차 [Wikipedia](https://en.wikipedia.org/wiki/Lake_(video_game))] 택배는 밴 뒤에서 꺼내 현관까지 들고 가고, 편지는 메일 가방에서 우편함으로.
- ② **[관찰]** 걷기·운전 중 **왼쪽 아래 둥근 미니맵 + 옆에 주소 한 줄**(02·05·07·09, 눈대중 2~3%), **오른쪽 아래 키 안내 한 줄**("DELIVER MAIL (A)" 05, "TALK TO GIRL (A)" 09, 눈대중 0.5% 미만). [2차 [Wikipedia](https://en.wikipedia.org/wiki/Lake_(video_game))] 하루는 우체국에서 시작해 정해진 수만큼 배달하고 우체국으로 돌아온다 — **우리 하루 구조와 같다.**
- ③ **[관찰]** 03·04·06·08·11: **박스 없이 글자 두 줄 + 화자 이름(주황 작은 글자)** 이 화면 하단 가운데(눈대중 3~4%). 인물은 3D 로 연기.
- ④ **도착 확인 = 05 의 집 번호/문패.** 미니맵은 우리 D-242 가 숨긴 쪽이라 **가져오지 않는다**(대안으로만 §3 B). 하단 두 줄 자막은 "아래 검은 띠 거부"(D-237)와 가까워 **비추천 [추정]**.

### TOEM — [Steam](https://store.steampowered.com/app/1307580/) · 컷 8장 (04 인용)
- ① **[관찰]** 03: 할 일을 **카드 한 장**(Community Card, 별 칸·체크 목록)으로 — 풀스크린. 카메라 화면도 풀스크린.
- ② **[관찰]** 세계 컷 HUD 0.
- ③ **[관찰]** 04: 검은 말풍선 + 흰 대문자 한 줄 + 끝에 **"…" 다음 표시**, 아래 꼬리.
- ④ **전체 문법은 안 맞는다(풀스크린 카드).** "…" 다음 표시만 참고 가능.

---

## 3. 우리 장면별 추천 1안 (제안 — 결정은 도현)

> 기준: ① D-238·240·242·243 은 그대로 ② 상시 화면 면적 4% 안 ③ **세상 물건 먼저** ④ 크기 S = 반나절 안 / M = 하루 안 / L = 이틀 이상 **[추정]**. 간판·종이 글자 내용은 **도현이 정한 것만**(체크리스트 7) — 아래 레퍼런스 속 글자는 "읽는 방식" 참고일 뿐 우리 문구가 아니다.

### A. 칠판 주문 (지금: 칠판 없음, 사장이 말로 줌) — **M**
- **1안**: 정비소 벽 칠판에 **핀으로 꽂힌 종이 쪽지 몇 장**. 가까이 가면 [E] 칩이 쪽지 곁에 뜨고(D-240, 이미 있음), 하나 집으면 **손이 들어 올리는 종이**(= 전표)가 된다. 쪽지가 곧 전표라서 칠판→전표→길이 한 물건으로 이어진다.
- 레퍼런스: [Jalopy 11 카탈로그+주문서](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/446020/ss_d8e75e6d26cb1483b6d592c86f32ad7dbe8a6117.1920x1080.jpg)(종이 주문서가 화면 오른쪽에 큼직하게 읽힌다) · [Jalopy 10 상점 카운터](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/446020/ss_34a7d3670e7c83125e1d24b9e0ad1deaa0ab02cd.1920x1080.jpg)(가격 간판·카운터 = 사는 곳이 곧 메뉴).
- 안 가져올 것: DS 의 홀로그램 주문 목록.

### B. 전표 / 길 찾기 (지금: 위 가운데 화살표 + Tab 전표, 전표엔 주문 이름 한 줄) — **M**
- **1안**: Tab 전표를 **Firewatch 04 동작**으로 — 손이 종이를 들어 올리고 뒤로 도로가 보인다. 종이에 **받는 사람 · 손그림 약도 · 약속 시각**(DELIVERY-LOOP §3 에 이미 "더해야 한다"고 적힌 것). 화살표는 **D-243 그대로 유지.** 도착 확인은 세상 물건: **문 앞 집 번호/문패**(Lake 05) + 갈림길 **큰 길 표지**(Jalopy 03).
- 레퍼런스: [Firewatch 04 지도](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/383870/ss_4b9d67ae2af0da570d03731d93b095d0203b973d.1920x1080.jpg) · [Lake 05 우편함 번호](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1118240/ss_af22184a81aeef2f08008f1e40a22a07a0b5dd85.1920x1080.jpg) · [Jalopy 03 지명 표지](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/446020/ss_c257a26e8786b2c6342d71878b7bd8b31468136f.1920x1080.jpg).
- 대안(비추천): Lake 식 상시 미니맵+주소 한 줄(눈대중 2~3%) — 옛 미니맵을 숨긴 D-242 와 반대 방향.

### C. 화물 상태 (지금: 막대 숨김, 위험할 때 "!", 짐칸 흔들림·소리) — **S**
- **1안**: **이미 정한 것을 유지** + 하나 추가: **상자 표면에 인쇄된 취급 라벨**(DS 20 "FRAGILE" 큰 인쇄 방식). 포장 단계에서 붙이는 라벨이 주행·건넬 때 **상자에서 그대로 읽힌다** → 화면 UI 없이 "이 상자는 조심" 이 전달된다. 짐칸 상자는 실물로 보인다(EDC 01·DS 00).
- 레퍼런스: [DS 20 FRAGILE 라벨](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1850570/ss_4342fab99e35c9c50e01f866b676d3ab324d4015.1920x1080.jpg) · [EDC 01 짐칸 상자](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/3293010/cf5ed0c38426b0b7d592fd7d5d9006043e2fa948/ss_cf5ed0c38426b0b7d592fd7d5d9006043e2fa948.1920x1080.jpg) · [DS 00 등짐](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1850570/ss_f64a1140651ff5af30eb63bb6e5b41753d00a98e.1920x1080.jpg).
- 안 가져올 것: DS 21 의 상시 타이머(2.8%) — 타이머 없음(D-238) 결정과 충돌.

### D. 대화 (지금: 머리 위 말풍선 + 이름 탭 + 한 글자씩, **빈 곳 = 말하는 사람이 화면 밖일 때**) — **S**
- **1안**: **대면 대화는 현재 구현 유지**(Short Hike 03 과 같은 계열). **화면 밖 화자(라디오·전화)는 손에 든 무전기**를 들어 올리고 말풍선이 그 기기 위에서 나오게 한다(Firewatch 02·05). 방 아침 라디오 한 줄(DELIVERY-LOOP §3)과 같은 문법이라 하나로 덮인다.
- 레퍼런스: [Firewatch 02 무전기](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/383870/ss_570bc87288b5ab4e235ba270e1d2ae30312d5b82.1920x1080.jpg) · [Short Hike 03 말풍선](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1055540/ss_c9c8a8e6062511c5ae9259c68c66754cd09ae971.1920x1080.jpg) · [Mail Time 01 "!"](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1607240/ss_52dd14af168fcbf08340c0e3b6d3364202b18840.1920x1080.jpg)(말 걸 수 있음 표시).
- 안 가져올 것: Lake·Lil Gator·Mail Time 의 하단 큰 상자/자막(아래 띠 계열, D-237 이력).

### E. 하루 끝 (지금: 없음) — **M**
- **1안**: 방 책상 **일기장 펼침**(풀스크린). 그날 배달 한 줄씩 + 번 돈, 벽 달력 한 칸 지우기(DELIVERY-LOOP §3 와 같음). 읽는 방식은 **Jalopy 04 오른쪽 페이지**(날짜 머리글 + 구간 목록을 손글씨로)에 가깝게, 보상 순간은 **Mail Time 04 엽서처럼 종이**로.
- 레퍼런스: [Jalopy 04 지도책](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/446020/ss_409936bbd4b9af630112ed85b695f64c1bc7ddad.1920x1080.jpg) · [Mail Time 04 엽서](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1607240/ss_cf2f52ab53d4450c056f7bda453c0b4fa6dbf088.1920x1080.jpg) · [Lil Gator 01 노트](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1586800/ss_1d59c0b307971f6d839c6659784b799d0264910c.1920x1080.jpg).
- 정산 돈은 문 앞에서 **EDC 03 처럼 대상 곁 도트 글자 몇 초**([링크](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/3293010/287ddbe79e43b36d1ab74111609212f8cc7aeb64/ss_287ddbe79e43b36d1ab74111609212f8cc7aeb64.1920x1080.jpg)) — 1판 결정은 이미 같은 방향(DELIVERY-LOOP §3 "결과").

### 한 장 요약
| 장면 | 1안 한 줄 | 크기 |
|---|---|---|
| 칠판 주문 | 핀 꽂힌 종이 쪽지를 집으면 그게 전표 | M |
| 전표·길 찾기 | 손이 든 종이(뒤로 도로 보임) + 집 번호·큰 표지, 화살표는 D-243 | M |
| 화물 상태 | 상자 표면 취급 라벨 + 짐칸 실물 + 기존 "!" | S |
| 대화 | 대면은 유지, 화면 밖 화자는 손에 든 무전기 | S |
| 하루 끝 | 일기장 손글씨 펼침 + 달력 한 칸 | M |

**공용 모듈 1개(손에 든 종이: 들어 올림 → 뒤 세상 보임 → 확대 읽기 → 내려놓기)** 를 먼저 만들면 칠판·전표·일기가 같은 동작을 쓴다 **[추정]**. 그 모듈이 사실상 이번 제안의 첫 "M" 이다.

---

## 4. 스크린샷 링크 모음 (공식 Steam 스토어 컷, 눌러서 보기 — 저장 안 함)

| 게임 | 컷 | 보는 곳 |
|---|---|---|
| Firewatch | [02](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/383870/ss_570bc87288b5ab4e235ba270e1d2ae30312d5b82.1920x1080.jpg) · [04](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/383870/ss_4b9d67ae2af0da570d03731d93b095d0203b973d.1920x1080.jpg) · [05](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/383870/ss_634d9b9352169125aec4cbf8b7834a18f9992eeb.1920x1080.jpg) | 02·05 왼쪽 아래 무전기 / 04 든 지도 뒤로 풍경 |
| Jalopy | [01](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/446020/ss_1c701a26aa706b8d3cd1171e1dbe2188ee571cca.1920x1080.jpg) · [02](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/446020/ss_57bdff00bda596a7049f293e7909c99bf851dbac.1920x1080.jpg) · [03](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/446020/ss_c257a26e8786b2c6342d71878b7bd8b31468136f.1920x1080.jpg) · [04](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/446020/ss_409936bbd4b9af630112ed85b695f64c1bc7ddad.1920x1080.jpg) · [10](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/446020/ss_34a7d3670e7c83125e1d24b9e0ad1deaa0ab02cd.1920x1080.jpg) · [11](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/446020/ss_d8e75e6d26cb1483b6d592c86f32ad7dbe8a6117.1920x1080.jpg) | 01 노트 / 02 계기판(어두움) / 03 지명 표지 / 04 지도책+손글씨 / 10 지갑·가격 간판 / 11 카탈로그+주문서 |
| Easy Delivery Co. | [01](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/3293010/cf5ed0c38426b0b7d592fd7d5d9006043e2fa948/ss_cf5ed0c38426b0b7d592fd7d5d9006043e2fa948.1920x1080.jpg) · [02](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/3293010/2df6e79b65c499cd45590df8b1cbf0a705a2a6a8/ss_2df6e79b65c499cd45590df8b1cbf0a705a2a6a8.1920x1080.jpg) · [03](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/3293010/287ddbe79e43b36d1ab74111609212f8cc7aeb64/ss_287ddbe79e43b36d1ab74111609212f8cc7aeb64.1920x1080.jpg) · [05](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/3293010/ef75d56aced607ac06e8f01bb4eee91c22574149/ss_ef75d56aced607ac06e8f01bb4eee91c22574149.1920x1080.jpg) | 01 짐칸 상자 / 02 상자 없는 도트 글자 / 03 상자+떠오르는 돈 / 05 도로 표지판 |
| Lake | [02](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1118240/ss_e2c573336f048433b0efd8164de9f503cb3da21c.1920x1080.jpg) · [03](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1118240/ss_73e331a194896b53a9071d85cba842de4260d0b1.1920x1080.jpg) · [05](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1118240/ss_af22184a81aeef2f08008f1e40a22a07a0b5dd85.1920x1080.jpg) · [07](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1118240/ss_8f4f42babeee8afda20b877d6355e570f23f442b.1920x1080.jpg) | 02·05 왼쪽 아래 미니맵+주소 / 03 하단 자막 / 05 우편함 번호 / 07 든 상자 |
| A Short Hike | [03](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1055540/ss_c9c8a8e6062511c5ae9259c68c66754cd09ae971.1920x1080.jpg) | 말풍선+꼬리 |
| Mail Time | [01](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1607240/ss_52dd14af168fcbf08340c0e3b6d3364202b18840.1920x1080.jpg) · [04](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1607240/ss_cf2f52ab53d4450c056f7bda453c0b4fa6dbf088.1920x1080.jpg) | 01 "!"+하단 상자 / 04 엽서 |
| Death Stranding DC | [00](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1850570/ss_f64a1140651ff5af30eb63bb6e5b41753d00a98e.1920x1080.jpg) · [03](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1850570/ss_d47bde2e349606b3ef1f641e2d8fb7ccf1adba77.1920x1080.jpg) · [20](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1850570/ss_4342fab99e35c9c50e01f866b676d3ab324d4015.1920x1080.jpg) · [21](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1850570/ss_c01508116ce0765f35fbfc4f115cd58e24d15819.1920x1080.jpg) | 00·03 등짐 / 20 FRAGILE 라벨 / 21 상단 타이머(안 가져옴) |
| Lil Gator Game | [01](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1586800/ss_1d59c0b307971f6d839c6659784b799d0264910c.1920x1080.jpg) · [03](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1586800/ss_f08de3f83f02e4eb0bc0c6d716d3cbe90df85498.1920x1080.jpg) | 01 스프링 노트 메뉴 / 03 하단 큰 상자 |
| TOEM | [04](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1307580/ss_764193b7918c2da9db2122b4e8f361f1b7d33fc1.1920x1080.jpg) | 말풍선 + "…" |

링크 형식은 Steam 스토어가 쓰는 `shared.akamai.steamstatic.com/store_item_assets/steam/apps/<앱ID>/…` 이고 이번 세션에 전부 내려받아 열렸다. Steam 이 CDN 주소를 바꾸면 깨질 수 있으니 안 열리면 각 게임 Steam 페이지의 스크린샷 순번으로 본다.

---

## 5. 프로젝트 규칙 제안 (전부 제안 — 도현 승인 전엔 확정 아님)

1. **읽을 정보는 "손에 든 종이" 하나로 통일.** 칠판 쪽지·전표·약도·일기가 같은 동작(들어 올림 → 뒤 세상 보임 → 확대 → 내려놓기). 새 읽기 UI 를 장면마다 따로 만들지 않는다.
2. **화면 밖 화자 = 손에 든 무전기.** 대면은 지금 말풍선 그대로.
3. **화물 상태는 상자 표면(라벨)과 짐칸 실물로.** 막대·숫자 상시 표시 없음(11-01 규칙 5 와 같음). 정확한 값은 전표 뒷면.
4. **도착은 세상 물건으로 확인**(집 번호·문패). 화살표는 D-243 그대로.
5. **꺼내기 동작은 한 건당 1번.** 칠판에서 집은 쪽지가 곧 전표 — 같은 종이를 다시 꺼내지 않게.
6. **하단 큰 대화 상자·하단 자막 띠는 쓰지 않는다**(D-237 이력, Lake·Lil Gator·Mail Time 이 쓰는 문법).
7. **새 종이 글자(칠판·전표·일기)의 내용은 도현이 정한 것만.** 이 보고서 속 게임 글자는 "읽는 방식" 참고일 뿐이다.
8. **만들기 전 비교 컷**(체크리스트 14·17): "종이 든 컷"은 같은 카메라로 ① 종이 없음 ② 종이 있음 두 장을 찍어 **종이 뒤로 도로가 보이는지·종이 면적**을 같이 본다. 이 제안이 안 됐다면 실패했을 검사 = "전표 연 채 한 프레임에서 종이 바깥 도로가 보이는가", 깨뜨릴 만한 곳 = "주행 중 Tab 전표가 화살표를 가리나".

---

## 6. 모르는 것

- **EDC 의 주문 받기·목적지 표시**: 스토어 컷·위키·80.lv 에 없다 → 플레이 영상 확인 필요.
- **Firewatch 지도·나침반을 꺼내는 데 걸리는 시간, 반복 피로**: 컷으로는 못 안다. 우리 60번 반복에서 "손에 든 종이"가 질리는지는 **회색 상자 프로토로 시험해야 안다** [추정].
- **운전 중에 종이를 읽을 수 있는지**: Jalopy 지도책·Firewatch 지도가 정차 중 동작인지 **[미확인]**.
- **개발자 UI 의도**: EDC(80.lv)·Short Hike(PS Blog) 외엔 못 찾았다(Firewatch·Jalopy·Lake·DS·Mail Time 모두). 이번 칸은 대부분 화면 관찰이다.
- **Lake·Firewatch 화면 %**는 눈대중이다(좌표 박스로 재지 않음). 상한 4% 와 비교할 땐 ±1%p 오차로 본다.
- **DS2 요약 주문 화면**: PC Gamer 글 원문을 못 열었다(검색 요약만).
- **스토어 컷 편향**: HUD 가 빠졌을 수 있다. Lake 처럼 컷에 HUD 가 그대로 나온 게임이 있어 "컷에 안 보임 = 없음" 으로 단정하지 않았다.
