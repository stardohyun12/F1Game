# Claude 웹 조사 v2 — U1~U5 심화

> 출처: WebSearch/WebFetch 2차 조사, 세션 10 (2026-04-22).
> v1: `claude-web-findings.md` (1차 조사) 참조. v1 본문은 수정하지 않음.
> 신뢰도: high = 공식 문서/개발자 인터뷰, med = 위키/커뮤니티 다중 소스 일치, low = 단일 소스/추정.
> 원칙: 수치는 URL 근거 필수. 못 찾은 건 UNKNOWN 그대로 남김 (추정 금지).

---

## U1. Burnout Paradise 행동별 부스트 기여 %

**결론: 정확한 퍼센티지는 여전히 비공개.** 단, 커뮤니티 리버스엔지 관찰로 "**풀바 단번 충전 트리거**"와 "**소량 증분 행동**"의 이분법은 확인됨.

| 행동 | 효과 | 정량값 | 출처 | 신뢰도 |
|---|---|---|---|---|
| Takedown (Aggression 차량) | 바 **100% 즉시 충전** | 1 바 풀충전 | [Boost Wiki](https://burnout.fandom.com/wiki/Boost) / [Aggression Boost Wiki](https://burnout.fandom.com/wiki/Aggression_boost) | med |
| Barrel Roll (모든 차량) | 바 **100% 즉시 충전** | 1 바 풀충전 | [Barrel Roll Wiki](https://burnout.fandom.com/wiki/Barrel_Roll) | med |
| Billboard 파괴 | 현재 체인 중 바 풀충전 (Stunt Run 시) | 1 바 풀충전 | [Stunt Boost Wiki](https://burnout.fandom.com/wiki/Stunt_boost) | med |
| Oncoming 주행 | 지속 시간에 비례한 **점진 충전** (초당 고정량) | 비공개 | [Boost Wiki](https://burnout.fandom.com/wiki/Boost) | low |
| Near Miss | 이벤트당 **소량 즉시 충전** | 비공개 | [Boost Wiki](https://burnout.fandom.com/wiki/Boost) | low |
| Drift | 지속 + 종료 시 총 **소량 충전** (거리/시간 비례 추정) | 비공개 | [Drift Wiki](https://burnout.fandom.com/wiki/Drift) | low |
| Air Time / Jump | 체공 시간 비례 **소량 충전** | 비공개 | [Boost Wiki](https://burnout.fandom.com/wiki/Boost) | low |
| 행동별 % 정량치 | **UNKNOWN** — Criterion 공식 미공개, 리버스엔지 문서화 없음 | - | - | - |

**찾은 축**: 부스트 충전은 **"즉시 풀충전 이벤트" (Takedown/Barrel Roll/Billboard)** vs **"지속성 점진 충전" (Oncoming/Drift/Near Miss/Jump)** 이중 구조.

**사용처 검증**: speedrunning 커뮤니티 가이드도 "Oncoming + Near Miss + 소형 Drift 혼합"으로 Burnout Chain 유지를 권장. 정량 % 없이 **"얇고 길게" vs "두껍고 드물게"** 이원화 감각 설계가 원칙.

---

## U2. Burnout Paradise 부스트 지속시간/가속률

| 수치 | 값 | 단위 | 출처 | 신뢰도 |
|---|---|---|---|---|
| Speed Boost 풀바 지속시간 | **정확히 8초** | 초 | [Speed Boost Wiki](https://burnout.fandom.com/wiki/Speed_boost) | med (위키 다중 확인) |
| Speed Boost 예외 (Hawker Mech) | 약 10초 | 초 | [Speed Boost Wiki](https://burnout.fandom.com/wiki/Speed_boost) | low |
| 속도 증가율 (%) | **UNKNOWN** — 차량/부스트 타입별 가변, 공개 수치 없음 | - | - | - |
| 150 MPH 오디오 임계 | 150 MPH 초과 시 BGM 왜곡 버전으로 전환 (체감 가속 연출용) | MPH | [Ben Minto 인터뷰](https://designingsound.org/2017/08/31/burnout-a-sound-design-retrospective-with-ben-minto/) | high |
| Infinite Gear 모델 | 최고 RPM 점근 접근 (절대 도달 X) — 영속 가속감 연출 | - | [Ben Minto 인터뷰](https://designingsound.org/2017/08/31/burnout-a-sound-design-retrospective-with-ben-minto/) | high |
| Aggression Boost 바 용량 | 최대 **3× 일반 용량** 누적 가능 | 배수 | [Aggression Boost Wiki](https://burnout.fandom.com/wiki/Aggression_boost) (v1) | med |

**핵심 발견**: **풀바 = 8초**는 여러 위키/포럼 소스에서 일관된 수치. PIT CREW 부스트 설계의 상한선 레퍼런스로 활용 가능.
**미해결**: 속도 증가율 (%)은 여전히 어디에도 공개 수치 없음. Ben Minto 인터뷰조차 "심리적 가속감 우선, 실제 속도-사운드 매핑 생략"이라 명시.

---

## U3. FH5 Auto / Manual / Manual Clutch 비율

| 항목 | 값 | 출처 | 신뢰도 |
|---|---|---|---|
| 공식 Playground Games 텔레메트리 | **UNKNOWN** — 미공개 | - | - |
| 포럼 "Great Debate" 폴 수치 | **0 voters** (페이지 캐시에 결과 미노출) | [Forza Forum Poll](https://forums.forza.net/t/the-great-debate-auto-vs-manual-a-poll/718830) | - |
| Speedrun.com WR 트랜스미션 | **FH4/FH5 전 WR = Manual + Clutch** (100%) | [Speedrun.com FH5 Forum](https://www.speedrun.com/forza_horizon_5/forums/h5p01) | med |
| Auto vs Manual+Clutch 격차 | 자동은 단변속이 조기라 파워밴드 못 씀. 격차는 "모든 차에 존재하지만 차량별 가변" | [FH4/FH5 Forum 토론](https://forums.forza.net/t/unrealistic-manual-vs-automatic-gearing-advantage/82924?page=3) | low |
| 커뮤니티 체감 | "자동 사용자 많아서 놀랐다" (포럼 반응) — 게임패드 사용자 대다수 Auto/Manual no-clutch | [Forza Poll 스레드](https://forums.forza.net/t/the-great-debate-auto-vs-manual-a-poll/718830) | low |
| 기본값 (신규 플레이어) | Automatic (v1에서 확인) | [Kotaku](https://kotaku.com/before-you-start-five-settings-to-change-in-forza-hori-1848004522) | med |

**해결/미해결**:
- **정량 비율 UNKNOWN** (공식 없음, 커뮤니티 폴도 voter 0).
- **정성 사실 확인**: 캐주얼 플레이어 = Auto 기본, 하드코어/WR = Manual+Clutch. 중간층 = Manual no-clutch가 패드 최적.
- **레퍼런스 활용**: "게임패드 사용자 기본 Auto, 토글 인센티브 주면 Manual로 이행"이라는 FH5 디자인 패턴은 PIT CREW에 그대로 이식 가능.

---

## U4. F1 게임 연료 축약률

| 항목 | 값 | 출처 | 신뢰도 |
|---|---|---|---|
| 실제 F1 레이스당 연료 | 110 kg | v1 참조 | high |
| 실제 F1 랩당 평균 연료 | 1.6~1.9 kg (Monza/Spa 3.0~3.2, Monaco 2.2~2.4) | [Michelin Simulation](https://simulation.michelin.com/canopy/technical-articles/fuel-saving-secrets-why-f1-cars-conserve-fuel-while-f2-cars-go-all-out) / [F1 Briefing](https://f1briefing.com/fuel-load-vs-lap-time-f1-performance-analysis/) | high |
| 실제 F1 연료 유량 규정 | 100 kg/h (2,200회/초 모니터링) | [F1 Briefing](https://f1briefing.com/fuel-load-vs-lap-time-f1-performance-analysis/) | high |
| F1 게임 레이스 길이 옵션 | **3랩(35%*) / 5랩 / 25% / 50% / 100%** | [F1 23 커뮤니티](https://steamcommunity.com/app/2108330/discussions/0/4035852164013622434/) | med |
| 100% 레이스 연료 시뮬 | **실제와 1:1 비율** (플레이어 설정 Average Fuel Burn이 실제 1.6~1.9 kg/랩과 동일) | [EA Forum F1 22](https://forums.ea.com/discussions/-/-/8412554) | med |
| 5랩 / 25% 레이스 | 연료 투입량 플레이어가 수동 설정 — **랩당 소비율은 100%와 동일** (단순 선형 축약) | [EA Forum F1 22](https://forums.ea.com/discussions/-/-/8412554) / [SimRacing Setup F1 23](https://simracingsetup.com/f1-23/f1-23-race-strategy-guide/) | med |
| Fuel Mix 모드 | F1 2021+ 레이스 중 Standard 고정 (party mode 규정 반영). 연습에서만 Lean 가능 | [RacingGames.gg F1 2021](https://racinggames.gg/article/f1-2021-fuel-mix-how-to-change-fuel-mix-set-fuel-levels-more) | med |
| Fuel Mix 수치 차이 (%) | **UNKNOWN** — Codemasters 미공개 | - | - |
| 게임 연료 축약률 | **시간 단위로는 축약 없음** (5랩 → 실제와 동일 연비) / **거리 단위로는 5랩=5랩 실거리 축약** | - | med (추론) |

**핵심 발견**: Codemasters F1 게임은 **"물리 시뮬 축약"이 아니라 "레이스 길이 축약"** 방식. 5랩 모드든 100% 레이스든 랩당 연비는 동일 (1.6~1.9 kg). 즉 **실제 F1 90분 레이스를 5분으로 압축하는 튠은 존재하지 않음**. 실시간 랩 = 실시간 연료 소비.

**PIT CREW 시사점**: F1 공식 게임은 "연료를 줄이려면 랩 수를 줄인다"는 해법을 택함. PIT CREW는 5~15분 런이므로 **연료 소비율을 ~18배 가속** (110 kg = 90분 → 5분 = 6.1 kg/분) 식의 공격적 축약이 필요. Codemasters를 레퍼런스로 삼기엔 사실상 시뮬레이터임.

---

## U5. FH5 기어 시프트 연출 (사운드/비주얼)

| 항목 | 값 | 출처 | 신뢰도 |
|---|---|---|---|
| Lead Audio Designer | Fraser Strachan (Playground Games) — Turn 10 아님 | [Gameshub FH5 인터뷰](https://www.gameshub.com/news/features/forza-horizon-5-lead-audio-designer-fraser-strachan-7360/) | high |
| 신규 차량 레코딩 | 320+ 차량 신규 녹음 (기존 DB 교체) | [Gameshub](https://www.gameshub.com/news/features/forza-horizon-5-lead-audio-designer-fraser-strachan-7360/) | high |
| 오디오 엔진 | **Granular Synthesis** (엔진 회전 1개를 수천 단위 샘플로 분할 후 실시간 재조립) | [Jalopnik FH5](https://www.jalopnik.com/heres-why-forza-horizon-5-sounds-so-good-straight-from-1847343451/) / [A Sound Effect](https://www.asoundeffect.com/forza-5-game-audio/) | high |
| 오디오 프레임레이트 | **90 FPS 상응** (비주얼 프레임레이트보다 높음 — 반응성 ↑) | [Jalopnik FH5](https://www.jalopnik.com/heres-why-forza-horizon-5-sounds-so-good-straight-from-1847343451/) | high |
| 기어 시프트 전용 샘플 | **Gear Cracks / Limiter Samples / Idles**를 차량별 별도 추출 후 Granular 엔진에 공급 | [A Sound Effect](https://www.asoundeffect.com/forza-5-game-audio/) | high |
| 차량 녹음 지시 | "기어 다운시프트 → 업시프트 빠르게" 명시적 요청 (공격적 테이크오프 캡처) | [A Sound Effect](https://www.asoundeffect.com/forza-5-game-audio/) / [Tonebenders 팟캐스트](https://tonebenderspodcast.com/184-forza-horizon-5-with-fraser-strachan/2/) | high |
| 오디오 파일 구조 | 5채널 .wav (엔진 / 배기 / 흡기 / 인테리어 / 추가) | [Tonebenders 팟캐스트](https://tonebenderspodcast.com/184-forza-horizon-5-with-fraser-strachan/2/) | high |
| Audible Upgrades | 부품 튜닝 시 실제 부품 소리 변경 연동 | [A Sound Effect](https://www.asoundeffect.com/forza-5-game-audio/) | high |
| DSP/컴프레션 | **의도적 최소화** (Dynamic 유지 — 압축·리미터 벗김) | [A Sound Effect](https://www.asoundeffect.com/forza-5-game-audio/) | high |
| Ray-Traced Audio | 환경 반사 적용 (엔진음 동적 변화) | [Gameshub](https://www.gameshub.com/news/features/forza-horizon-5-lead-audio-designer-fraser-strachan-7360/) | med |
| 카메라 셰이크 연동 | 옵션 존재 (G-force 기반) — 시프트 전용 강화 연출은 **UNKNOWN** | [Forza Forum](https://forums.forza.net/t/camera-shake-for-increased-sense-of-speed/708232) | low |
| 업시프트 시 RPM 드롭 그래프 | **UNKNOWN** — 공개 문서 없음 | - | - |
| HUD 플래시 등 시각 큐 | **UNKNOWN** — 개발 문서 공개 안 됨 | - | - |

**핵심 발견**: FH5 기어 시프트 연출의 본질은 **"대단한 개별 연출이 아니라, 상위 Granular 엔진이 시프트 순간을 물리적으로 재현"**. 즉 **전용 VFX/샘플 재생이 아니라 엔진 사운드의 연속 재계산이 시프트를 표현**. 이것이 Turn 10 Motorsport의 "고정 샘플 조합" 방식과 FH5의 결정적 차이.

**카메라 / HUD 시프트 전용 연출**: 공개 문서 어디에도 명시 없음. 커뮤니티에서 관찰된 건 "업시프트 시 RPM 바가 내려갔다가 다시 오른다" 정도 (표준 레이싱게임 UX).

---

## PIT CREW 적용 제안 (v1 Key Takeaways 보강)

| # | 원본 U | 구체 수치 | PIT CREW 이식안 |
|---|---|---|---|
| K1 | U1 | Takedown/Barrel Roll = 바 100% 즉시 충전, Oncoming/Near Miss/Drift = 소량 점진 | PIT CREW 부스트는 **"완수 이벤트 100% 충전" (배달 완료/완벽 정차) + "위험 주행 점진 충전" (드리프트/근접 회피)** 이중화. 구체 % 수치 비공개는 업계 관행이므로 감각 튜닝 허용. |
| K2 | U2 | Speed Boost 풀바 = 8초 | PIT CREW 풀 부스트 = **6~10초 범위**에서 튠. 8초는 "체감적 적정" 레퍼런스. 5~15분 런에서 런당 ~3~6회 발동이 자연스러움. |
| K3 | U3 | WR 100% = Manual+Clutch, 캐주얼 = Auto | PIT CREW **기본값 Auto**, Manual 토글에 인센티브 (부스트 충전율 +10%). 클러치는 안 넣어도 됨 (패드 UX 희생 비해 이득 작음). |
| K4 | U4 | F1 공식 게임도 실제와 1:1 시뮬 (랩 수만 축약) | PIT CREW는 **18배 이상 가속 필요** (110 kg/90분 → ~1 탱크/5~15분). "실제를 따라간다"는 발상 버리고 **"한 런 내 의미 있는 결정이 2~4번 발생하는 주기"**로 튠. |
| K5 | U5 | FH5 = Granular 연속 재생 / Turn 10 = 샘플 조합 | PIT CREW Kenny 에셋+아케이드 스케일이므로 **샘플 조합이 정답**. 업시프트 전용 연출 (엔진 피치 순간 드롭 + 0.1초 카메라 흔들림 + HUD 기어 숫자 플래시) 3종 세트가 저비용 고효율. |

---

## 여전히 미해결 (U1~U5 중 못 찾은 것)

| # | 항목 | 다음 조사 후보 |
|---|---|---|
| U1-a | 행동별 % 정량치 | PS3/PS4 메모리 덤프 기반 리버스엔지 (Discord speedrun 채널, GitHub BP 리서치 레포지토리 직접 접속 필요 — 웹검색으로는 한계) |
| U2-a | 부스트 속도 증가율 (%) | Burnout 팬 커뮤니티 private 문서, 또는 EA 사내 자료 (실질 접근 불가능) |
| U3-a | Auto/Manual 사용 정량 비율 | Playground Games 공식 컨퍼런스 발표 기다림. 또는 Twitch API 기반 스트리머 설정 집계 (커스텀 스크래핑 필요) |
| U4-a | Fuel Mix Lean/Standard 소비율 차이 (%) | Codemasters EGO 엔진 유출 문서 / F1 모딩 커뮤니티 디스어셈블리 (현재 웹 공개본 없음) |
| U5-a | 시프트 시 카메라 셰이크/HUD 플래시 정량 | Turn 10 GDC 2024 오디오 세션 (공개 시) / FH5 Nexus Mods 디컴파일 문서 추가 발굴 필요 |

**솔직한 평가**: U2 (풀바 8초)와 U5 (Granular + Gear Cracks 샘플 구조)는 이번 2차로 **사실상 해결**. U1/U3/U4는 **정량 수치 접근 자체가 막혀있어 정성 설계 원칙으로 수렴**하는 게 현실적.

---

## 충돌/주의 플래그

1. **U4에서 "실제와 1:1" vs v1의 "Soft 타이어 50% 축약" 모순 아님** — 타이어는 물리 모델 자체가 축약 (wear rate 파라미터), 연료는 시간 시뮬 축약 없이 랩 수로만 축약. Codemasters의 축약 철학이 항목별로 다름.
2. **U5 "FH5는 Turn 10 아님"** — v1 질문에 "Turn 10 GDC" 언급됐으나 **FH5는 Playground Games**. Turn 10은 Forza Motorsport 담당. 두 스튜디오의 오디오 접근법이 다름 (Playground = Granular, Turn 10 = 샘플 조합).
3. **U1 "Takedown = 풀바 충전"이지만 차량 타입별 다름** — Aggression 타입만 Takedown으로 풀충전, Speed 타입은 Takedown으로 바 전량 소진 시 Burnout Chain 발동 방식. PIT CREW 이식 시 "보상 방식"만 참고하고 분기 로직은 단순화 필요.
