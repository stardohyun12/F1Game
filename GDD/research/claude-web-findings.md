# Claude 웹 조사 결과 — 레이싱 게임 수치 근거

> **출처:** WebSearch/WebFetch 기반, 세션 9 (2026-04-22) 1차 수집.
> **용도:** 도현 NotebookLM 요약본과 교차검증 → GDD/systems/*.md 수치 재정립.
> **신뢰도:** high = 공식 문서/규정, med = 커뮤니티·커뮤니티 위키·리뷰어, low = 단일 소스·유튜브 해설.
> **UNKNOWN 처리:** 수치를 찾지 못한 경우 추측 금지. NotebookLM 단계 또는 2차 웹검색 과제로 이관.

---

## 1. Burnout Paradise 부스트 시스템

| 수치 | 값 | 단위 | 출처 | 신뢰도 |
|---|---|---|---|---|
| 충전 기여 행동 | Oncoming / Near Miss / Drift (주축) + Jump / Takedown / Stunt (보조) | 종류 | [Fandom](https://burnout.fandom.com/wiki/Boost) | med |
| 행동별 % 기여도 | UNKNOWN — Criterion 비공개 | - | - | - |
| 풀차지 지속 시간 | UNKNOWN — 간접 단서 "논스톱 10초" 챌린지 존재 | 초 | [GameFAQs](https://gamefaqs.gamespot.com/ps3/935291-burnout-paradise/answers/22058) | low |
| 속도 증가율 | UNKNOWN — 공개 수치 없음 (차량/부스트 타입별 가변) | % | - | - |
| Speed Boost 조건 | 부스트 바 100% 충전 필요, 사용 중 reckless driving 시 Burnout Chain | - | [Fandom](https://burnout.fandom.com/wiki/Speed_boost) | med |
| Aggression Boost 조건 | 부스트 바 일부만 있어도 발동, takedown/구조물 파괴 시 추가 충전 (최대 3× 용량) | 배수 | [Fandom](https://burnout.fandom.com/wiki/Aggression_boost) | med |
| Stunt Boost 조건 | 부스트 바 일부만 있어도 발동, drift/jump/stunt 시 추가 충전 | - | [Fandom](https://burnout.fandom.com/wiki/Stunt_boost) | med |
| Burnout Chain 조건 | Speed 차량 한정. 부스트 전량 소진 + 소진 전 다시 풀 충전 → 바 리필 반복 | - | [Fandom](https://burnout.fandom.com/wiki/Burnout_(driving_skill)) | med |
| 최대 체인 기록 | 20× (도전과제 "Boosting Around the World") | 배수 | [TrueAchievements](https://www.trueachievements.com/a249486/boosting-around-the-world-achievement) | high |
| 차량 타입 3종 요약 | Speed=최고속·저내구 / Aggression=둔중·고내구·준무한 부스트 / Stunt=경쾌·저내구 | - | [Fandom](https://burnout.fandom.com/wiki/Boost) | med |

---

## 2. F1 타이어 & 연료 (실제 + 게임)

| 수치 | 값 | 단위 | 출처 | 신뢰도 |
|---|---|---|---|---|
| 실제 Soft-Medium 델타 | 0.6~1.0 (극단 1.2) | 초/랩 | [PlanetF1](https://www.planetf1.com/news/pirelli-surprised-tyre-time-gaps) | high |
| 실제 Soft-Hard 델타 | ~1.5 (극단 1.8) | 초/랩 | [Autosport](https://www.autosport.com/f1/news/f1-tyres-what-are-the-compounds-and-what-do-they-mean/10344284/) | high |
| 컴파운드 단계별 일반 델타 | 0.75~1.0 | 초/랩/단계 | [LAS Motorsport](https://las-motorsport.com/f1/dictionary/formula-1-dictionary-tire-compound/17010/) | med |
| 실제 Soft 수명 | 10~15 | 랩 | [FlowRacers](https://flowracers.com/blog/how-long-do-f1-tires-last/) | high |
| 실제 Hard 수명 | 25~60 | 랩 | [FlowRacers](https://flowracers.com/blog/how-long-do-f1-tires-last/) | high |
| F1 23 게임 Soft 수명 | 5~8 (대부분 트랙) | 랩 | [Steam Forum](https://steamcommunity.com/app/1692250/discussions/0/3453717519634744737/) | med |
| F1 23 게임 Hard 수명 | 트랙별 극단 가변 (Qatar 8랩 50% 마모) | 랩 | [Steam Forum](https://steamcommunity.com/app/2108330/discussions/0/3812908758679474011/) | med |
| 게임 마모 모델 | 물리 시뮬 아닌 "트랙별 wear rate 파라미터" | - | [Steam Forum](https://steamcommunity.com/app/2108330/discussions/0/3812908758679474011/) | med |
| 실제 레이스당 연료 규정 | 110 | kg | [LAS Motorsport](https://las-motorsport.com/f1/blog/fuel-consumption-in-formula-1-numbers-and-factors/17185/) | high |
| 실제 랩당 평균 연료 | 1.6~1.9 | kg/랩 | [LAS Motorsport](https://las-motorsport.com/f1/blog/fuel-consumption-in-formula-1-numbers-and-factors/17185/) | med |
| 실제 100km당 연료 | 35~55 | L/100km | [LAS Motorsport](https://las-motorsport.com/f1/blog/f1-fuel-consumption-in-l-100km-real-figures/17215/) | med |
| 무게 10kg당 랩타임 페널티 | 0.3~0.4 | 초/랩 | [CarXplorer](https://carxplorer.com/how-much-fuel-is-in-a-f1-car/) | high |
| 팀 언더필 관행 | 110kg에서 5~15kg 적게 탑재 | kg | [LAS Motorsport](https://las-motorsport.com/f1/blog/how-much-fuel-does-an-f1-car-hold-per-race/17224/) | med |
| 게임 연료 축약률 | UNKNOWN — Codemasters 공식 수치 미공개 | - | - | - |

---

## 3. Forza Horizon 5 기어/보조

| 수치 | 값 | 단위 | 출처 | 신뢰도 |
|---|---|---|---|---|
| Auto vs Manual 비율 | UNKNOWN — 공식 폴 0 voters, 정량 통계 없음 | % | [Forza Forum](https://forums.forza.net/t/the-great-debate-auto-vs-manual-a-poll/718830) | - |
| 기어 옵션 종류 | Automatic / Manual / Manual w/ Clutch | 3종 | [Fandom](https://forza.fandom.com/wiki/Driver_Assists) | high |
| Driving Line 옵션 | Full (가속 파랑/감속 빨강) / Braking Only / Off | 3단계 | [Fandom](https://forza.fandom.com/wiki/Driver_Assists) | high |
| Braking 옵션 | Assisted / ABS On / ABS Off | 3단계 | [Fandom](https://forza.fandom.com/wiki/Driver_Assists) | high |
| Steering 옵션 | Assisted / Simulation (Simulation은 휠 권장) | - | [Fandom](https://forza.fandom.com/wiki/Driver_Assists) | med |
| 신규 플레이어 기본값 | Driving Line=Full, Braking=Assisted, Transmission=Automatic, Steering=Normal | - | [Kotaku](https://kotaku.com/before-you-start-five-settings-to-change-in-forza-hori-1848004522) | med |
| Camera Motion Effects | 가속 zoom + G-force 쉐이크 (토글 가능) | - | [Forza Forum](https://forums.forza.net/t/screen-shake-setting/535308) | med |
| FOV 조정 | 차량/카메라별 슬라이더 제공 | - | [Forza Forum](https://forums.forza.net/t/camera-shake-for-increased-sense-of-speed/708232) | med |
| 기어 시프트 사운드 연출 | UNKNOWN — Turn 10 GDC 공개 자료에서 미검색 | - | - | - |
| 차량별 기어 단수 분포 | UNKNOWN — 실차 기반(5~8단 혼재), FH5 전수 통계 없음 | - | - | - |

---

## Key Takeaways — PIT CREW 적용

1. **부스트 충전 행동은 3축 + 보조로 단순화.** Burnout조차 Oncoming/Near Miss/Drift 3종이 주축. PIT CREW는 3축(예: 드리프트 / 정시주행 / 배달 완수)으로 압축. **수치 % 비중은 비공개**가 업계 관행 → 감각 튜닝이면 됨.

2. **Burnout Chain 패턴이 리스크-리워드 핵심.** 풀부스트 소진 전 재풀충전 = 체인. PIT CREW 이식안: "부스트 소진 전 배달 박스 무결성 80% 이상 유지 시 보너스 충전". 최대 20× 체인은 장인 목표로 적정.

3. **F1 실제 Soft-Hard 델타 ~1.5s/랩. 5~15분 런에서는 과장 필요.** PIT CREW 타이어 3등급 델타를 **랩당 1.5~2초 차이(~8%)**로 설정해야 체감됨. 0.5~1초 차이는 무시 가능 존.

4. **Codemasters도 실제를 ~50% 축약 (Soft 10~15랩 → 게임 5~8랩).** PIT CREW는 더 공격적 축약 권장: **Soft 타이어 2~4분 주행으로 소진**. 실제 배달 거리 환산은 무의미, "한 런 내 의미 있는 결정이 발생하는가"가 기준.

5. **연료 페널티 10kg당 0.3~0.4s/랩은 실수치.** PIT CREW 화물 무게 3등급 설계 시 **무게 단계당 가속/최고속 -2~5%**가 현실 기반 근거. 기존 GDD 화물 개념과 부합.

6. **FH5 기본값 = Automatic + Full Driving Line + Assisted Brake.** 배달 게임 기본 세팅으로 그대로 채택. **Manual/클러치는 언락식 고급 옵션 + 보상(부스트 충전 +10% 등)**으로 두면 Burnout 리스크-리워드와 결합 가능. → 세션 9의 "기어 자동/수동 토글" 결정에 근거 제공.

---

## 미해결 (NotebookLM 또는 2차 조사 과제)

| # | 항목 | 어디서 찾을 가능성? |
|---|---|---|
| U1 | Burnout 행동별 % 기여도 | YouTube 리버스엔지 영상, 개발자 인터뷰 (Criterion GDC 과거 세션) |
| U2 | Burnout 부스트 풀차지 지속시간(초)·속도 증가율(%) | 스피드런 커뮤니티(speedrun.com), 커뮤니티 리버스엔지 글 |
| U3 | FH5 Auto/Manual 비율 | Speedrun.com 상위 랭커 통계, Twitch 스트리머 설정 분석 |
| U4 | F1 게임 연료 시뮬 축약률 | Codemasters 개발 블로그, GDC 2019~2023 Codemasters 세션 |
| U5 | FH5 기어 시프트 사운드/연출 디테일 | Turn 10 사운드팀 발표 (GDC), Audio Directors 인터뷰 |

---

## 세션 10 할 일 체크리스트

- [ ] 도현 NotebookLM 3개 노트북(Burnout/Forza/F1) 요약본 받기 → `notebooklm-summaries/*.md`
- [ ] 양측 결과 충돌 항목 플래그
- [ ] U1~U5 중 NotebookLM이 커버한 항목 확인
- [ ] GDD 6개 문서 일괄 업데이트 (tires/fuel/cargo/core-loop + gears/boost 신설)
- [ ] D-029 기록 (`notes/decisions-log.md`)
