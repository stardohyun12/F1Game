# 다른 택배·배달 게임은 포장을 어떻게 만드나 (과제 15, 2026-11-04)

도현 원문: "다른 택배 게임들은 어떻게 포장 만드는지도 봐봐".
우리 방식(비교 기준): **2D 작업대 화면 + 물리 시뮬** — 상자 소·중·대 고르기 → 뽁뽁이·종이 롤에서 한 장 뜯기 → 떨어뜨려 넣기 → 테이프 → 흔들면 안의 것이 실제로 움직이고, 그 흔들림 양이 화물의 시작값(주행으로 이어짐).

출처 규칙: 공식만(Steam 스토어 본문·스크린샷 = Valve `appdetails` API 로 직접 읽음, 개발사 공식 가이드·PlayStation 공식 블로그). 비공식(커뮤니티 토론·리뷰)에서만 확인된 것은 **(비공식)** 로 표시하고 근거로 세지 않았다. 이미지는 링크만.

---

## 한눈에

| 게임 | 포장이 게임 안에 있나 | 방식 | 화면 | 결과 돌아오는 길 |
|---|---|---|---|---|
| Ship, Inc. | **있음(핵심)** | 2D 탑다운 작업대, 주문서 대조·상자 크기·완충재·스티커 | 2D 보드 | 벌금·시간 보너스, 파손 |
| Parcel Problems | **있음(핵심)** | 1인칭 3D 물리, 접기·넣기·테이프·라벨 10초 반복 | 1인칭 3D | 할당량 실패 = 끝 |
| Parcel Works (출시 전) | 있음 | 창고 3D, 주문 찾기→넣기→라벨 | 3D | 시간 보너스 |
| Death Stranding | **적재(=포장의 자리)** | 메뉴에서 짐의 위치·무게·균형, 자동 정리 버튼 | UI 패널(3D 몸에 반영) | 균형 나쁘면 넘어져 파손, 평가 하락 |
| Easy Delivery Co. | 없음(실은 것이 그대로 짐칸 물리) | 짐칸에 올려놓기, 뒷문 닫기 | 1인칭/3인칭 3D | 떨어지면 주워 다시 싣기 |
| Moving Out | 없음(운반 자체가 물리) | 들고·던지고·부수기 | 3D 탑다운 | 시간 기록 |
| Unpacking | 반대 방향(꺼내 놓기) | 블록 맞추기 퍼즐 | 2D 보드 | 타이머·점수 없음, "제자리 아님" 표시만 |
| Wilmot's Warehouse | 없음(창고 정리) | 밀어서 쌓기·분류 | 2D 탑다운 | 별(Performance Stars) |
| Parcel Corps | 없음 | — (자전거 배달·트릭) | 3인칭 3D | 배달 시간 |
| Mail Time | 없음 | — (편지 꺼내 전달) | 3인칭 3D | 뱃지 |
| Lake | 없음 | — (트럭에서 우편물 꺼내 전달) | 3인칭 3D | 이야기 |
| Parcel Delivery Simulator | 없음(경영) | — | 3D 타이쿤 | 돈 |

**포장 자체가 플레이인 게임은 Ship, Inc.·Parcel Problems 둘뿐**이고, 둘 다 "포장 → 운반"이 이어지지 않는다(포장 끝 = 점수). 포장 결과가 그 뒤 이동에 물리로 이어지는 건 Death Stranding(적재→걷기)뿐이다.

---

## 게임별 5줄

### 1. Ship, Inc. (Rogue Duck Interactive, 2025-06-23)
- 출처: [Steam 스토어](https://store.steampowered.com/app/3496000/) · 스샷 [작업대](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/3496000/4dbae1f6f3485d63a1165a1377188ac0de6ec7ca/ss_4dbae1f6f3485d63a1165a1377188ac0de6ec7ca.1920x1080.jpg) · [상자 3개 동시](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/3496000/6c7e455b679ca1b60acd9f02122adc5bca6b9226/ss_6c7e455b679ca1b60acd9f02122adc5bca6b9226.1920x1080.jpg)
- ① 포장이 게임의 전부. 퍼즐+시간관리: "주문서대로 상자 크기 고르고, 물건을 배치하고, 봉해서 보낸다"(스토어 본문 "Select the correct box size, arrange items properly, and seal them securely").
- ② 플레이어가 고르는 것: 상자 크기(Medium/Large/XLarge 칸이 오른쪽에 줄로), 완충재(위 선반에 포장 땅콩·뽁뽁이·라벨 묶음, 남은 개수 표시), 어느 물건을 어느 주문 상자에 넣나(주문 카드에 물건 목록 3~5개). 깨지기 쉬운 물건은 따로 주의(스토어 "Fragile items require extra attention to avoid damage during transit").
- ③ 결과: 잘못 넣거나 파손이면 돈이 깎인다("Misplacing or damaging shipments could cost you"), 모드 3개(Chill 타이머 없음 / Normal / Rush Hour 시간 짧고 벌금 큼). 돈으로 사무실·도구 업그레이드.
- ④ 화면: **2D 탑다운 작업대 한 판** — 파란/초록 격자 매트 위에 열린 상자, 흩어진 물건, 주문 카드가 상자 위에 붙음, 오른쪽에 상자 크기 탭, 위에 재료 선반. 손·캐릭터는 안 보이고 마우스 커서만.
- ⑤ 가져올 것: **주문 카드가 상자에 붙어 "뭘 넣어야 하나"가 작업대 위에서 바로 읽힘** + 상자 크기 선택이 벌금과 연결. 피할 것: 화면 전체가 앱형 UI(탭·카운터·달력)라 우리 UI 규칙(세계 안 물건만, D-238~241)과 정반대.

### 2. Parcel Problems (JD, 2023-02-28, Ludum Dare 51 출신)
- 출처: [Steam 스토어](https://store.steampowered.com/app/2306850/) · 스샷 [작업대 1인칭](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/2306850/ss_43fce0ba4029902288f69f4c3abe33533c3fd1e5.1920x1080.jpg)
- ① 포장이 전부, **1인칭 3D 물리**. "Fold, Pack, Tape, Label" 네 동작을 10초마다 한 상자씩 반복하는 압박 게임("a package leaving this building every ten seconds").
- ② 고르는 것: 상자 접기, 물건 집어 넣기(물리로 쏟아져 있음), 테이프 띠 당기기, 라벨 붙이기. 물건을 빼먹고 보내는 "꼼수"도 허용("cut corners and forget items").
- ③ 결과: 할당량을 못 맞추면 끝(생존형). 실수마다 "심장이 더 뛴다"는 식의 압박 연출.
- ④ 화면: 1인칭, 작업대 위에 상자·노란 물건·테이프 롤이 물리로 널려 있고 벽 모니터에 주문명("Bread Loaf"). UI 거의 없음.
- ⑤ 가져올 것: **테이프 한 번·라벨 한 번처럼 동작 수가 4개로 고정**돼 손에 익는다(도현 지적 "작업대 위 물건 수·동작 수"와 같은 방향). 피할 것: 10초 압박 — 우리는 포장이 "피트스톱"이지 생존 게임이 아니다.

### 3. Parcel Works: Packing Simulator (Suiter Studios, 출시 예정)
- 출처: [Steam 스토어](https://store.steampowered.com/app/4806220/) (스토어 이미지에 생성 AI 사용 표기 있음 — 레퍼런스 가중치 낮게)
- ① 있음. 창고 3D: 주문 받기 → 물건 찾기 → 상자에 넣기 → 라벨 → 출고.
- ② 고르는 것: 어느 물건, 효율적 배치("pack them efficiently into boxes"), 재고 주문(컴퓨터), 커피로 속도 버프.
- ③ 결과: 제시간이면 시간 보너스 급여.
- ④ 3D 창고 걷기.
- ⑤ 가져올 것: 없음(Ship, Inc.의 3D 판). 피할 것: 재고·커피 같은 경영 요소 — 우리 범위 밖.

### 4. Death Stranding / Director's Cut (Kojima Productions)
- 출처: [Kojima Productions 공식 초보자 가이드](https://www.kojimaproductions.jp/index.php/en/death-stranding-directors-cut-beginners-guide) · [PlayStation Blog 공식 팁](https://blog.playstation.com/2019/11/07/hit-the-ground-running-with-these-day-one-death-stranding-tips/) · [DS2 PlayStation 공식 가이드](https://www.playstation.com/en-us/games/death-stranding-2-on-the-beach/death-stranding-2-on-the-beach-guide/) · 가이드 이미지 [무게·균형](https://www.kojimaproductions.jp/sites/default/files/2022-03/screensWeightAndBalance.jpg) · [자동 정리](https://www.kojimaproductions.jp/sites/default/files/2022-03/screensAutoArrangeCargo.jpg)
- ① 포장은 없고 **적재(Cargo Management)**가 그 자리. 메뉴에서 짐을 등·어깨·허리 슬롯에 배치, 무게 표시가 흰→노랑→빨강 3단. "Auto-Arrange Cargo" 버튼으로 자동 정리도 됨.
- ② 고르는 것: 어느 짐을 어디에(무게중심에 가깝게), 높이(너무 높이 쌓으면 급회전에 넘어짐 — PS Blog), 특수 화물 규칙(**"피자는 세로로 두면 파손"** — 공식 가이드).
- ③ 결과: 균형 나쁘면 걷다 넘어져 짐 파손·배달 평가 하락(DS2 가이드 "Poorly balanced cargo makes it easier to trip and fall, resulting in damage and a reduced delivery rating"). 컨테이너는 비·낙하로 닳고 수리 스프레이로 고침.
- ④ 화면: UI 패널(목록·탭)에서 배치, 결과는 3D 몸(샘이 기우뚱)에 바로 반영.
- ⑤ 가져올 것: **적재 결과가 이동 물리의 시작값이 된다**(우리 "흔들림 = 시작값"과 같은 설계) + 화물마다 한 줄 규칙(피자 세우기 금지). 피할 것: 메뉴 안에서 하는 적재 — 우리는 작업대 위 물건으로.

### 5. Easy Delivery Co. (Sam C, 2025-09-18) — 우리 렌더링 기준 게임
- 출처: [Steam 스토어](https://store.steampowered.com/app/3293010/) · 스샷 [트럭·짐칸](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/3293010/cf5ed0c38426b0b7d592fd7d5d9006043e2fa948/ss_cf5ed0c38426b0b7d592fd7d5d9006043e2fa948.1920x1080.jpg)
- ① **포장 없음.** 스토어 본문 흐름은 "Pick Up → Drive → Deliver → Explore". 받은 물건을 경트럭 짐칸에 그대로 싣는다.
- ② 고르는 것: 짐칸 어디에 올려놓나, 뒷문 닫기. (비공식 — Steam 토론·리뷰: EZ Mart 에서 덕트테이프를 사면 짐을 고정할 수 있고, 떨어진 짐은 주워서 다시 싣는다. 스토어 본문엔 없어 근거로 세지 않음.)
- ③ 결과(공식 본문 범위): "road safety not required" — 거칠게 달려도 됨을 스토어가 직접 권장. 파손 수치·등급 언급 없음.
- ④ 화면: 3D(1·3인칭), 짐이 짐칸에 물리로 놓여 보임. 포장 UI 없음.
- ⑤ 가져올 것: **포장 결과가 세계 안 물건(짐칸 위)으로 보인다** — 우리도 작업대 위 상자가 그대로 카트에 실리는 연속성. 피할 것: 포장 단계 자체를 없애는 것 — EDC 는 "편하게"가 컨셉이고 우리는 화물 긴장이 재미 기둥.

### 6. Moving Out (SMG Studio·Devm Games, 2020-04-28)
- 출처: [Steam 스토어](https://store.steampowered.com/app/996770/)
- ① 포장 없음. "physics-based moving simulator" — 가구를 들고·던지고·창문을 부수며 트럭에 싣는 것이 플레이.
- ② 고르는 것: 경로(문 대신 창문), 던질지 들지, 협동 분담.
- ③ 결과: 시간 기록·별, 파손 벌점 없음(스토어 "smash your way to the fastest times").
- ④ 3D 탑다운, 4인 소파 협동.
- ⑤ 가져올 것: **물리 자체가 웃음 — 흔들어 보기(우리 "흔들면 안의 것이 움직인다")는 결과값이 아니라 그 자체로 재미 장면**이 될 수 있다. 피할 것: 파손 무벌점 — 우리는 파손이 돈.

### 7. Unpacking (Witch Beam, 2021-11-01) — 반대 방향
- 출처: [Steam 스토어](https://store.steampowered.com/app/1135690/) · 스샷 [방](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1135690/ss_b32bfdd3f68e1f9e264bd37e10a464629ca034f8.1920x1080.jpg)
- ① 꺼내기(unpack)만. "Part block-fitting puzzle, part home decoration".
- ② 고르는 것: 물건을 어디에 놓나(선반·서랍). 상자에서 꺼내는 건 클릭 한 번.
- ③ 결과: "no timers, meters or scores" — 제자리가 아니면 빨갛게 표시만.
- ④ **2D 측면 보드(픽셀), 손·캐릭터 없음**, 물건을 끌어다 놓음.
- ⑤ 가져올 것: **물건 하나하나가 이야기(사연)를 말한다** — 우리 화물도 "누구에게 가는 무엇"이 작업대 위에서 읽혀야 한다(STORY.md 와 같은 방향). 피할 것: 점수 없는 명상형 — 우리는 흔들림 숫자가 필요.

### 8. Wilmot's Warehouse (Hollow Ponds·Richard Hogg, 2019-08-29)
- 출처: [Steam 스토어](https://store.steampowered.com/app/839870/) · 스샷 [창고](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/839870/ss_aa84b2cde78958aea65b5f7e15c0ff886aa36c92.1920x1080.jpg)
- ① 포장 없음. 창고 정리(밀기·분류·쌓기)와 "창구 열리면 빨리 찾기".
- ② 고르는 것: 분류 체계 전부 플레이어 몫("organize by color, type, … or some wild format that only you understand").
- ③ 결과: Performance Stars → 업그레이드.
- ④ 2D 탑다운, 네모 블록.
- ⑤ 가져올 것: **"내가 정한 방식"이 보상 — 작업대 위 재료를 어디에 둘지 플레이어가 정하게 두면 손에 익는 재미**. 피할 것: 추상 네모 — 우리는 실물 화물(64px/m).

### 9. Parcel Corps (Billy Goat Entertainment, 2024-10-03)
- 출처: [Steam 스토어](https://store.steampowered.com/app/2015960/)
- ① 포장 없음. 자전거 배달·트릭·시간 단축이 전부.
- ② 고르는 것: 경로·지름길·트릭.
- ③ 결과: 배달 시간·시장 점유율.
- ④ 3인칭 3D.
- ⑤ 가져올 것: 없음(포장 쪽). 참고: 의뢰인마다 배달 조건이 다름("perform tricks to stir up milkshakes") — **화물마다 '취급 조건' 한 줄**은 여기서도 재미 기둥. 피할 것: —

### 10. Mail Time (Kela van der Deijl, 2023-04-27)
- 출처: [Steam 스토어](https://store.steampowered.com/app/1607240/)
- ① 포장 없음. 편지·소포를 들고 숲을 달려 전달, "No time limits, pressure, or risk of harm".
- ②~④ 고르는 것 없음(전달 상대 찾기), 결과는 뱃지, 3인칭 3D.
- ⑤ 가져올 것: 받는 사람이 기다리는 연출("Their recipients eagerly await"). 피할 것: 화물 무해 — 우리 축과 다름.

### 11. Lake (Gamious, 2021-09-01)
- 출처: [Steam 스토어](https://store.steampowered.com/app/1118240/)
- ① 포장 없음. 우편 트럭으로 집집마다 전달, 이야기 선택이 중심("let the auto-pilot do the work").
- ②~④ 고르는 것은 대화·관계, 결과는 이야기, 3인칭 3D.
- ⑤ 가져올 것: 배달이 사람을 만나는 구실이라는 틀(우리 STORY 와 같음). 피할 것: 운전까지 자동 — 우리는 운전이 실력.

### 12. Parcel Delivery Simulator (Digital Melody, 2026-04-15)
- 출처: [Steam 스토어](https://store.steampowered.com/app/4078230/)
- ① 포장 없음(경영 타이쿤: 직원·차량·택배함).
- ⑤ 제외 — 포장 레퍼런스 아님.

(제외: Little Kitty Big City — 배달 게임 아님, 과제 지시대로 뺌.)

---

## 결론 — 우리 방식(2D 작업대 + 물리 흔들림)과 비교

### ① 우리가 빠뜨린 재미 3개
1. **"뭘 넣어야 하나"가 작업대 위에 없다.** Ship, Inc. 는 주문 카드가 상자에 붙어 있고 물건 목록을 대조하는 것 자체가 판단이다. 우리는 받은 물건 1개를 넣기만 한다 — 주문 전표(세계 안 종이)를 작업대에 같이 올려 "이 물건·이 받는 사람·주의 한 줄"을 읽게.
2. **화물마다 한 줄 규칙이 없다.** Death Stranding 의 "피자는 세워 두면 파손", Parcel Corps 의 "밀크셰이크는 흔들어서". 우리는 모든 화물이 같은 흔들림 규칙이다 — 화물 종류(SPEC 물건)마다 "세우면 X / 종이 완충재 금지 / 큰 상자 금지" 하나씩 주면 S/M/L·재료 고르기가 진짜 선택이 된다.
3. **흔들기가 결과값일 뿐 장면이 아니다.** Moving Out 은 물리 자체가 웃음이고 Parcel Problems 는 테이프 한 번·라벨 한 번의 손맛이 리듬이다. 우리 흔들기는 숫자를 재는 검사에 가깝다 — 흔들 때 소리·안에서 부딪히는 그림·"덜그럭" 한 줄 반응(NPC 가 아니라 물건 쪽)이 있어야 "해 보고 싶은" 동작이 된다.

### ② 우리가 더 나은 점
- **포장 → 주행이 한 줄로 이어진다.** 포장 게임 둘(Ship, Inc.·Parcel Problems)은 포장이 끝이고, 배달 게임들(EDC·Parcel Corps·Mail Time·Lake)은 포장이 없다. 포장 결과가 이동 물리의 시작값이 되는 건 Death Stranding 뿐인데 그것도 메뉴 패널에서 한다. 우리는 **세계 안 작업대 위 물건(노을 빛을 같이 받는)을 손으로 만지고 그 상태가 그대로 카트에 실린다** — 12개 중 없는 조합이다.
- 화면 UI 없이 물건으로만 안내하는 규칙(D-238~241)은 Ship, Inc. 식 앱 UI 보다 우리 톤에 맞고, Unpacking 처럼 물건이 이야기를 말할 자리가 이미 있다.

### ③ 추천 변경
- **S — 화물 한 줄 규칙 + 전표를 작업대에.** `world.yaml` 물건마다 `pack_rule` 한 줄(예: "세우면 깨짐", "종이 완충재 금지")을 넣고, 작업대 위에 전표 종이(세계 안 물건)로 그 한 줄과 받는 사람을 보여 준다. 규칙을 어기면 흔들림 시작값에 가산. Core 층 규칙 + 테스트 3개, 30분 과제 1개.
- **M — 흔들기를 장면으로.** 흔들 때 상자 안 물건이 벽에 닿는 순간 소리(종류별 2~3개) + 작업대 위에서 상자 자체가 살짝 들썩이는 애니, 그리고 흔든 뒤 결과를 숫자가 아니라 **상자 겉면 표식(테이프가 들떠 보임 / 멀쩡)** 으로 돌려준다. 상자 크기는 재료비로 정산에서 차감(이미 `WrapCostCalibration` 있음)해 "큰 상자 = 안전하지만 비쌈·흔들림 큼"이 읽히게. 30분 과제 2개(소리·표식 / 비용 연결).

### 보고 3줄
- 빠뜨린 재미 3: 작업대 위 전표(뭘 넣나 대조) / 화물마다 한 줄 규칙(피자 세우기 금지 식) / 흔들기가 숫자 검사라 장면(소리·반응)이 없다.
- 더 나은 점: 포장 결과가 세계 안 물건 그대로 카트에 실려 주행 물리로 이어지는 건 12개 중 우리뿐(DS 만 비슷하나 메뉴 패널).
- 추천: S = 물건마다 `pack_rule` 한 줄 + 전표를 작업대에(Core 규칙·테스트) / M = 흔들기에 소리·상자 겉면 표식 + 상자 크기 재료비 정산 연결.
