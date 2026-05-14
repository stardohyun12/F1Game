# 실시간 긴장 요소 리서치 — 화물 Shock + 마을 환경 + HUD 부하

> 작성: 2026-04-25 (세션 13 이후).
> 배경: 1주차 피드백으로 "레이싱 서킷" → "마을/시골 배송"으로 컨셉 전환. 부스트(D-030 K1/K2) 철회, 화물(cargo) 긴장이 USP 축이 됨. 본 문서는 화물 Shock MVP 명세 + 마을 환경 긴장 요소 랭킹 + HUD 부하 가이드를 위한 조사/설계 문서.
> 상위 결정: D-007(아케이드+무게감), D-010(차 1종), D-030(부스트 → 철회), D-031/032(Driver Resource 철회), D-033~D-038(맵 구조).

---

## 0. 배경

도현 피드백 정리:
- "주행 중 신경 쓸 요소"가 곧 재미. 레이싱 게임의 랩타임·드리프트 점수는 PIT CREW 정체성과 거리 있음.
- 부스트·스피드 시스템은 배제. **화물 관리**는 살림.
- 마을 환경이 주는 **현실적 긴장**(보행자, 신호, 협로, 공사, 열차)을 채워야 함.

기존 시스템(Fuel/Gear/Countdown/Delivery)은 이미 MVP 골격을 구성하므로 화물 Shock는 **그 위에 얹는 "긴장 축 하나 추가"**로 설계한다. 부스트 철회로 공실이 된 "위험/보상 축"을 화물이 거꾸로 메운다: 부스트는 "빨리 가면 보상", 화물은 "빨리 가면 처벌". 감정 방향 반전.

---

## 1. 화물 Shock 명세 (MVP 박스 1종)

### A. 측정 축 — 무엇을 재는가

PIT CREW는 `ArcadeCarController`의 `Rigidbody.linearVelocity`에서 가속도 파생이 가능하다. 마을 배송 컨셉에선 **3축 중 2개만 우선**:

| 축 | 의미 | 구현 | 우선도 |
|---|---|---|---|
| **종G** (accel/decel) | 급가속·급브레이크·충돌 | `Δv_forward / Δt` | 최상 (MVP) |
| **횡G** (lateral) | 코너링 과속·급회전 | `Δv_right / Δt` 또는 `ω × v` | 상 (MVP) |
| 수직G (jolt) | 언덕·연석·방지턱 | `Δv_up / Δt` 또는 `Rigidbody` Y | 중 (맵에 경사 없으면 유예) |

**결론:** MVP는 **종G + 횡G 2축만**. 맵에 경사가 없고(H4 파킹) 방지턱 에셋도 미도입 상태이므로 수직G는 3주차 이후로 미룬다.

**누적 vs 피크:** 두 모드 병존.
- **피크(peak)** — 일정 G 이상 **0.2초 이상** 유지 시 단발 이벤트(damage tick). 충돌·급제동 포착.
- **누적(cumulative)** — 1초 이동 평균(rolling mean)이 안전선 초과 시 누적 데미지. 코너링 과속·과열 주행 전반 포착.

실세계 배송 accelerometer 연구에서 `1g` 대 `100g`가 정상 bump 대 drop 이벤트를 가르는 기준이다 ([enDAQ](https://blog.endaq.com/transportation-vibration-monitoring-what-really-happens-during-shipment)). 아케이드 차량은 이보다 유연하게 `0.3g~1.5g` 대역을 튜닝 타겟으로 잡는다. 참고로 일반 승용차 코너링 한계가 `1g`, 중형트럭 SRT(정적 롤오버) 한계가 `0.35g` ([OSHA](https://www.osha.gov/training/library/powered-industrial-trucks/app-a), [Fire Apparatus Magazine](https://www.fireapparatusmagazine.com/fire-apparatus/rollover-threshold-and-g-force/)).

**시간 윈도우:** FixedUpdate 50Hz에서 `Time.fixedDeltaTime=0.02s`. 0.2초 윈도우는 10 프레임 rolling buffer로 실측.

### B. 임계/패널티 — 숫자

**게임 감각 목표:** 정상 주행에서는 **보이지 않지만**, 급가속/급제동/과속 코너에서는 **확실히 느껴진다**. Easy Delivery Co.가 화물 페널티를 무의미하게 둬서 "배달감" 희석이라 비판받았다 ([GamingTrend](https://gamingtrend.com/reviews/easy-delivery-co-review-low-poly-9-5/)). PIT CREW는 반대로 가야 한다.

**수치 시작값 (세션 14 Day 1 튜닝 타겟):**

| 지표 | 안전 | 경고 | 데미지 | 파손 |
|---|---|---|---|---|
| 종G (정면/후면) | < 0.3g | 0.3~0.6g | 0.6~1.2g | > 1.2g |
| 횡G (코너) | < 0.4g | 0.4~0.7g | 0.7~1.0g | > 1.0g (0.5s 이상) |
| 충돌 속도 | < 10 km/h | 10~20 | 20~40 | > 40 |

**HP 모델 vs 단일 실패:** Hard Truck: 18 Wheels of Steel는 `1%` 데미지만으로도 `$0` 보상 ([Steam Guide](https://steamcommunity.com/sharedfiles/filedetails/?id=3104711021)) — 너무 가혹해 반면교사. ETS2는 `damage + wear` 통합 `15%` 임계부터 경고 ([Truck Sim Wiki](https://truck-simulator.fandom.com/wiki/Damage)) — 더 유연하고 런 중 회복 불가한 점이 긴장 유지.

**MVP 채택:** 박스 HP 100으로 시작. 단일 실패 아님.

| Integrity | 결과 | 배달 보상 |
|---|---|---|
| 100~80 | "완벽 상태" | 100% + 타임 보너스 |
| 80~50 | "경미 손상" | 80% |
| 50~20 | "큰 손상" | 50% |
| 20~1 | "거의 전파" | 20% |
| 0 | "전파" | 0% (배달 실패 아님 — 빈손 배달 가능) |

**데미지 산정 공식 (시작값):**

```
종G_damage   = max(0, (peakG - 0.6) * 10)   # 0.6g 초과분만
횡G_damage   = max(0, (peakG - 0.7) *  8)
충돌_damage  = max(0, (impact_kmh - 10) * 0.8)
누적_damage  = rolling_excess * 0.5 / sec     # 안전선 초과 면적
```

**밸런스 주의:**
- 박스 1종 MVP에선 **"빨리 가면 조금 찢김" vs "느리게 가면 지루"** 간 최적점이 튜닝 대상. 기준: Run 5~15분 동안 보통 주행 시 Integrity **80 유지**, 공격 주행 시 **50 내외**, 안전 주행 시 **95+**.
- 너무 관대하면(현재 Easy Delivery Co. 수준) 시스템 유명무실. 너무 가혹하면(현재 Hard Truck 수준) 짜증·리롤 유도.
- **배달 실패 조건 ≠ Integrity 0.** Integrity 0도 배달은 되지만 보상 0. Run 실패는 시간 초과 시만. Driver Resource(D-032 철회)의 "게임오버 없음" 원칙 계승.

### C. 시각/청각 피드백

**원칙:** 화면 중앙은 주행(USP ③ 운전감). 경고는 **가장자리 + 오디오**. "카메라 셰이크는 레이싱 안티패턴" 주의.

| 단계 | 시각 | 청각 | 기타 |
|---|---|---|---|
| 경미 (80~50) | HUD Integrity 바 노랑, 박스 머티리얼 dim tint | 삐걱 소리 (루프) | — |
| 중 (50~20) | Integrity 바 주황, 박스 스크래치 decal | "쾅"(피크 시 1회) | — |
| 위험 (20~1) | 빨간 Integrity + 화면 가장자리 빨강 펄스(0.5초) | 유리/나무 깨지는 SFX | HUD 경고 아이콘 깜빡임 |
| 전파 (0 도달) | 박스 모델 교체(찌그러짐), 연기 파티클 미세 | 큰 파손 SFX 1회 | HUD "CARGO TOTALED" 2초 표시 |

**카메라 셰이크 — 제한적 사용:**
- 충돌(impact > 30km/h)만 0.15초 amplitude 0.3 짧게. 일반 G 초과에는 사용 금지 (Dirt Rally 리서치: HUD·셰이크 과다 시 눈을 끈다 [Steam 스레드](https://steamcommunity.com/app/310560/discussions/0/490124466471047930/)).
- Gear 시프트 연출의 `0.1s` 카메라쉐이크(D-030 K5)와 동일 레벨 이하 유지.

**박스 모델 변화 (Kenney 에셋):**
- Kenney suburban/commercial에 `prop-box`·`prop-cargo` 류 소형 프롭 존재. MVP는 MaterialPropertyBlock tint로 dim만. 찌그러짐 모델은 4주차 폴리싱.
- Kenney city-kit-industrial에 `crate`·`pallet` 프롭 있음 → 박스 1종 MVP 후보. 세션 14 임포트 대상.

### D. 밸런스 레퍼런스

**1) Euro Truck Simulator 2 (ETS2)** ([Truck Sim Wiki](https://truck-simulator.fandom.com/wiki/Damage), [Steam Guide 1.50](https://steamcommunity.com/sharedfiles/filedetails/?id=3113174054))
- 트레일러·화물 데미지 계수 분리. 운동에너지 `E_k = ½mv²` 제곱 비례.
- 핵심 숫자: **truck→trailer 데미지 전달 계수 `0.1`**. 15% 누적 시 HUD 경고.
- **PIT CREW 추정치 이식:** 충돌 damage = `0.5 × v²`(km/h)² × 0.01 시작. `10km/h` 충돌 시 `~0.5` 데미지(무시 수준), `30km/h` 충돌 시 `~4.5` 데미지(경미), `60km/h` 충돌 시 `~18` 데미지(명확히 느껴짐).

**2) Hard Truck: 18 Wheels of Steel** ([Steam Guide](https://steamcommunity.com/sharedfiles/filedetails/?id=3104711021))
- Cup 아이콘 = 1% 데미지도 $0. Glass = 파손 시 자동 실패. **극단적 응징, 반면교사.**
- **PIT CREW 비채택:** 실패 판정을 이처럼 엄격히 하면 로그라이트와 상극. 화물 HP 스케일 + 부분 보상이 PIT CREW 톤에 맞음.

**3) Death Stranding — Balance** ([GameSpot](https://www.gamespot.com/articles/death-stranding-inventory-tips-how-your-inventory-works-and-how-to-best-manage-your-weight/1100-6471199/), [PowerPyx](https://www.powerpyx.com/death-stranding-how-to-balance-cargo/))
- 60kg 초과 시 좌우 스틱(L2+R2)로 기울기 상쇄. 실시간 입력 대응이 곧 화물 보호.
- **PIT CREW 이식 난도:** 2스틱 요구 UX는 아케이드 배송 톤과 부조화. 대신 **"횡G 경고 → 속도 자발 감소"**가 유사 감정선. 보조 입력 스틱 도입은 파킹.
- 핵심 차용: **"기울기 상태를 지속적으로 보여준다"**. PIT CREW HUD Integrity 바 밑에 `실시간 종G + 횡G` 미니 표시.

**4) Crazy Taxi** ([Grokipedia](https://grokipedia.com/page/Crazy_Taxi_\(video_game\)))
- 급제동·드리프트에 팁 **가산**. 화물 손상 페널티 없음. 운전사 거리낌 → 승객 콤보.
- **PIT CREW 반면교사:** 같은 드라이빙 감각을 쓰면서 **감정 방향 반대**가 가능. Crazy Taxi = "더 거칠게", PIT CREW = "더 조심해서". 아케이드 컨트롤 감각 차용 + 페널티 감정은 반전.

**5) Jalopy** ([evo](https://www.evo.co.uk/reviews/21069/jalopy-review-the-slowest-driving-game-youll-ever-play), [Steam 포럼 1](https://steamcommunity.com/app/446020/discussions/0/135509412989661240/))
- 부품 내구도 각자. 타이어 1~2 체크포인트면 교체. 마모 과도 → "단조롭다"는 비판 있음.
- **PIT CREW 교훈:** 자원 1종(박스 HP)이 **많은 결정에 영향을 주어야** 의미 있음. 단순 마모 타이머는 무의미.

**요약 (수치 추정 매트릭스):**

| 상황 | 종G | 횡G | 충돌속도 | 예상 데미지/단위 | PIT CREW 튠 |
|---|---|---|---|---|---|
| 정속 주행 | <0.2g | <0.3g | — | 0 | 안전 |
| 정상 브레이크 | 0.3~0.5g | — | — | 0~0.5 | 안전 |
| 급제동 (풀브레이크) | 0.7~1.0g | — | — | 1~4 | 경고 |
| 정상 코너 (40km/h 90°) | — | 0.4~0.6g | — | 0~0.5 | 안전 |
| 공격적 코너 (60km/h 90°) | — | 0.8~1.2g | — | 1~4 | 경고 |
| 저속 충돌 (연석) | — | — | 5~10km/h | 0~1 | 무시 |
| 중속 충돌 (건물 긁힘) | — | — | 20~30km/h | 4~10 | 경미 |
| 고속 충돌 | — | — | 60km/h | 15~30 | 명확 |

---

## 2. 마을 환경 긴장 요소 랭킹

**평가 축:**
- **기여도** (1~5): "실시간 주행 관리" 재미 기여
- **Kenney 가용성** (○/△/×): 해당 에셋 존재 여부
- **MVP 난이도** (1~5, 낮을수록 쉬움)
- **HUD 부하**: 전용 HUD 추가 필요 여부

| 요소 | 설명 | 기여도 | Kenney | 난이도 | HUD |
|---|---|---|---|---|---|
| **신호등 + 정지선** | 교차로 규칙, 적색 무시 → 충돌 위험↑ | **5** | ○ (light-square 등) | 3 | 지시 아이콘 (경량) |
| **보행자 무단횡단** | 랜덤 트리거, 치면 페널티 + 화물 Shock | **5** | △ (Kenney character pack 별도 필요) | 4 | 선택 (경고 팝업) |
| **NPC 트래픽** | waypoint 순회, 추돌 시 화물/페널티 | **4** | ○ (car-kit 여분 6~8종) | 4 | — |
| **공사 구역** | 콘/배리어 슬라럼, 우회 강제 | **4** | ○ (construction-cone/barrier/light) | 2 | — |
| **열차 건널목** | 대기 → 타이밍 = 경로 선택 | **4** | ○ (train-kit — D-018 잔류분!) | 3 | 경고등 아이콘 |
| **협로/골목** | 폭 제한, 건물 긁힘 위험 | 3 | ○ (road-straight-half, 기존 도로) | 2 | — |
| **제한속도 구역** | 스쿨존 등, 초과 시 페널티 | 3 | △ (sign-highway 있음) | 2 | 속도 경고 |
| **다리/터널** | 협폭 + 어두움 | 2 | ○ (road-bridge, bridge-pillar) | 3 | — |
| **빗길/젖은 노면** | 그립 감소 | 2 | × (셰이더 필요) | 5 | — |

### 상위 5 선정 + 근거

**① 신호등 + 정지선 (기여도 5)**
- **정당성:** 교차로가 이미 맵에 11개(crossroad/intersection, 세션 13 기준). 재활용 최대.
- **구현:** 교차로 cell에 light prefab 배치 + `TrafficLight` MonoBehaviour (phase 사이클 G/Y/R). 적색 진입 시 플레이어에 "위반" 이벤트 → **화물 Shock 페널티 직접 주진 않음** — 대신 NPC 트래픽 충돌 확률↑ (간접). 이게 "신호 안 지키면 왜 손해인지" 자연스러운 인과.
- **Kenney 에셋:** `light-square`(0.24×0.60×0.24m), `light-square-cross`(4-way T 교차용) — kenny-roads-catalog.md 존재 확인됨.
- **HUD:** 차량 전방 전용 아이콘(하단 중앙) — 적/황/녹 단순 색 스텝. 모양 확실히 구분 (접근성).

**② NPC 트래픽 (기여도 4, 실제 재미 순위로는 2위)**
- **정당성:** 신호등 + 트래픽 **세트**. 단독 신호등은 무의미, NPC가 있어야 신호 규칙이 살아있음. Crazy Taxi급 충돌 회피가 실시간 관리의 주축.
- **구현:** 간소 waypoint AI. `road-straight/crossroad` cell 위 trackpath. 차량 5~8대 순회. 추돌 시 화물 Shock 자동 트리거(충돌속도 기반, 섹션 1 D).
- **Kenney 에셋:** `kenney_car-kit` 세단/왜건 여분 존재 (현재 F1 1종만 사용).
- **HUD:** 전용 불필요. 시각/청각(경적 SFX)으로 충분.
- **주의:** AI 주행 로직 품질이 난이도를 결정. 완벽 회피는 스코프 초과. 단순 "cell 단위 정속 이동 + 신호 정지" 수준. 교차로 대기 로직만 잘 짜면 충분.

**③ 보행자 무단횡단 (기여도 5, 실제 구현 우선순위는 낮음)**
- **정당성:** "치면 안 되는 규칙"이 긴장의 정수. 마을 컨셉 핵심.
- **구현 제약:** Kenney `city-kit-*`에는 character 없음. `kenney_character-kit`(별도 팩) 필요 → D-005 원칙 애매화.
- **완화책:** **스프라이트 billboard 보행자 + 단순 LookAt 이동**으로 폴리곤 최소. 또는 Kenney의 `kenney_toy-car-kit`에서 사람 모형 재활용.
- **결론:** 2주차 **보류**, 3주차 확장. NPC 트래픽으로 1차 긴장 충족.

**④ 공사 구역 (기여도 4)**
- **정당성:** 저비용 고보상. cone/barrier는 충돌 이벤트를 자연스레 생성.
- **구현:** `BuildingCatalog`에 `O` (Obstacle) 풀 신설. 몇 cell에 콘 배치. 차량 충돌 시 Shock 발동(저강도).
- **Kenney 에셋:** `construction-cone/barrier/light` 모두 카탈로그 확인됨.
- **HUD:** 전용 불필요.
- **합성:** NPC 우회로(보다 좁은 공간) + 경광등 조명 효과로 시각 긴장까지 덤.

**⑤ 열차 건널목 (기여도 4, 유보)**
- **정당성:** "기다림"이 다른 긴장과 질적으로 다른 자원(시간). 계획 단계(D-003)와 연결.
- **구현 제약:** 열차 주기적 이벤트. `kenney_train-kit`(D-018 잔류 에셋!) 재활용 가능. 타이밍 미니게임.
- **난이도:** 중. 건널목 cell(`road-straight` 확장 마스크) + `TrainEvent` 타이머 + 열차 모델 이동.
- **우선순위 유예 근거:** 본 세션 코어 루프 안정화 후. 2주차 MVP에는 부담.

### 최종 우선순위 (MVP 기준)

**1주 내:** 신호등(교차로 prefab 배치 + 단순 사이클) + 공사 콘 배치(시각만 — 충돌 페널티 연동 없이도 시각 긴장)
**2주 내:** NPC 트래픽 waypoint + 신호 대기 로직. 충돌 시 Shock 연동.
**3주 이후:** 보행자(billboard) + 열차 건널목.
**v2+:** 빗길/젖은 노면(셰이더), 제한속도 구역.

---

## 3. HUD 부하 가이드

### 인지부하 기준 — 숫자

**Miller의 "7±2 법칙"** ([AND Academy](https://www.andacademy.com/resources/blog/ui-ux-design/cognitive-load/))은 인터페이스 요소 **동시 인지 한계를 5~9**로 본다. 다만 게임 HUD는 **지속 관찰이 아닌 주기적 체크**가 맞으므로 **실제 동시 쓸 수 있는 상시 HUD는 `3~5`**로 좁혀짐. Mario Kart 8, Dirt Rally(HUD off 플레이가 성적↑ [Steam](https://steamcommunity.com/app/310560/discussions/0/490124466471047930/)) 사례 모두 상시 HUD를 최소화.

### PIT CREW 현 상태 감사

현재 6개 HUD:
1. **FuelHUD** (연료)
2. **GearHUD** (기어)
3. **CountdownHUD** (시간 남음)
4. **DeliveryHUD / DeliveryProgressHUD** (남은 배달 수)
5. **OrderBoard** (런 시작 전 전용 — 실주행 중 숨김)
6. **ResultPanel** (정산 전용 — 실주행 중 숨김)

**주행 중 상시 노출:** 실질 4개 (Fuel/Gear/Countdown/Delivery). **임계 내.** 추가 여유 1~2개. → 화물 Integrity + 신호등 상태 아이콘 **2개까지** 추가 가능, 그 이상은 통합/변경.

### 경보 계층 (3단)

| 단계 | 시각 | 청각 | 목적 |
|---|---|---|---|
| 정상 | 상시 HUD 색 정상 | 없음 | 의식하지 않아도 됨 |
| 주의 | 해당 HUD 노랑/주황 | 저비용 beep 1회 | 시선 유도만 |
| 위험 | HUD 빨강 펄스, 가장자리 vignette | 반복 경고음 | 즉시 반응 강제 |

**원칙 (레이어 교차로 관리):**
- 동시에 "위험" 3개 이상 발생 금지. 게임 플로우 조정으로 분산.
- 가장자리 빨간 플래시(Integrity 위험용)는 **한 번에 한 HUD만**. 복수 동시 발생 시 우선도 큰 쪽만 플래시.

### 시선 분산 설계 — 역할 분리

**중앙 (주행 안전):** 차량 + 도로. HUD 최소화.

**하단 중앙 (핵심 상태):** Countdown + Gear (동일 row 배치). 크기 중.

**좌측 하단 (자원):** FuelHUD + **신규 Integrity HUD**. 같은 row. "내 소비 자원" 묶음.

**우측 하단 (진행/외부):** DeliveryHUD + **신규 TrafficLight 아이콘** (교차로 접근 시만 동적으로 표시). "지도/세계" 묶음.

**가장자리 (경보):** 위험 발생 시만 vignette/flash. 상시 요소 없음.

### 현 HUD 판정

**유지:** Fuel, Gear, Countdown, Delivery — 필수. 통합 없음.
**추가 OK:** Integrity(화물) **상시**, TrafficLight **동적(교차로 접근 시만)**.
**통합 후보 (4주차):** Gear + Countdown을 하단 중앙 단일 stripe로 묶기. 화면 시야 확보.

### 현재 6 HUD는 과잉인가? — 판정

**아니다. 단 배치 정리 필요.** 도현 "실제 게임 같은 태" 요구는 **배치 설계**로 충족 가능. 새 HUD 추가보다 **위계·그룹화**가 우선.

**추가 작업 제안 (1주 내):**
- [ ] UIRoot 아래 4 zone 그룹화 (Resource/State/Navigation/Alert)
- [ ] Integrity HUD 신규 생성 — 좌측 하단 Fuel 옆
- [ ] TrafficLight 아이콘 — 우측 하단 Delivery 옆, 동적 표시
- [ ] Alert 레이어 분리 — 모든 경보는 Canvas 최상단 overlay

---

## 4. MVP 우선순위 — 1주 / 2주 / v2

### 1주 (세션 14~16, ~일주일 내)

**목표:** 화물 Shock MVP + 신호등 + 공사 구역. 긴장 축 1개 확실히 완성.

1. **CargoBox.cs (박스 HP + 센서)**
   - 종G/횡G 측정 (FixedUpdate 10프레임 buffer)
   - 데미지 공식 적용 (섹션 1 B)
   - `OnIntegrityChanged` 이벤트
2. **IntegrityHUD.cs**
   - 좌측 하단 9-slice bar. FuelHUD 옆.
   - 80/50/20 단계 색 변화. 위험 시 펄스.
3. **TrafficLight Prefab + TrafficLightController**
   - 교차로 11곳 중 주요 4곳(Downtown 중심)에 배치.
   - 단순 3단 사이클 (G:6s / Y:2s / R:5s).
4. **공사 구역 시각 배치**
   - BuildingCatalog에 `O` = cone/barrier 풀.
   - 맵에 2~3 cell 분산 배치. 충돌 시 Shock 이벤트 연동(공용 CargoBox.OnImpact).

**검증 기준 (1주 끝):** 안전 주행 시 Integrity 90+, 평소 주행 80~90, 공격 주행 50~70. 도현 피드백에서 "화물이 찢기는 걸 느낌" 코멘트.

### 2주 (세션 17~21)

**목표:** NPC 트래픽 — 마을 감각 완성.

5. **NpcCar.cs (waypoint AI)**
   - road-cell 중앙 path. 5~8대 순회.
   - 신호등 정지 로직 (접근 5m 이내 + 적색 → 속도 0).
6. **충돌 페널티 연동**
   - 플레이어 차 vs NPC 충돌 속도 기반 Shock 데미지.
   - NPC 추돌 SFX (경적).
7. **공사 구역 확대**
   - 세션 14에서 시각만 배치된 공사 구역에 실제 충돌 콜라이더 확실히.
   - 경광등 점등.

**검증 기준 (2주 끝):** 신호 대기 + 추월 회피 행동이 자연 발생. "느긋한 배달이 의미 있다".

### v2 (MVP 이후, 5주차~)

- 보행자 billboard + 치임 페널티
- 열차 건널목 이벤트 (kenney_train-kit 재활용)
- 제한속도 구역 + 표지판 (sign-highway)
- 빗길/셰이더 — 파킹 유지
- 화물 3종(Shock/Spin/Tilt), 경고 UI (cargo.md 기존 계획)
- 카메라 세로G 트리거 (경사·언덕 H4 이관 시 자연 포함)

---

## 5. 다음 세션 착수 Top 3

**1) CargoBox + IntegrityHUD 구현 (최우선)**
- 박스 HP 단일, 종G+횡G 2축, 9-slice HUD bar.
- 파일: `F1/Assets/Scripts/Cargo/CargoBox.cs`, `IntegrityHUD.cs`.
- 데미지 공식 섹션 1 B 시작값 적용. 도현 플레이로 1일차 튠.

**2) TrafficLight 프로토타입 (1교차로만)**
- Downtown 중심 교차로 1개에 `light-square-cross` prefab 배치.
- `TrafficLightController` (G/Y/R 사이클). 상태 query API.
- NPC 트래픽 없이도 "적색에 서면 규칙 있는 마을 같다" 감각 선제 확보.

**3) UI 4-Zone 재정리 (배치만, 1~2시간)**
- 기존 Fuel/Gear/Countdown/Delivery를 Resource(좌하) / State(하중) / Navigation(우하) 그룹화.
- 세션 14 Integrity HUD 추가 준비 겸. "실제 게임 같은 태" 도현 요구 직접 반응.

---

## 핵심 3줄 요약

1. **화물 Shock는 종G+횡G 2축, 박스 HP 100, 0.6g/0.7g 경고선**으로 MVP 시작 — 부스트 폐기로 비운 "위험/보상 축"을 감정 반전(빨리 가면 처벌)로 메움.
2. **마을 긴장 상위 5: 신호등 · NPC트래픽 · 보행자 · 공사구역 · 열차** — Kenney 자산 가용성 기준으로 신호등+공사+NPC는 MVP, 보행자·열차는 v2.
3. **현 6 HUD는 과잉 아님, 배치 정리가 답** — Resource/State/Navigation/Alert 4존 위계 + Integrity 상시 + TrafficLight 동적 추가 여유 1~2개 남김.

---

## 출처

- [Euro Truck Simulator 2 — Damage system (Truck Sim Wiki)](https://truck-simulator.fandom.com/wiki/Damage)
- [ETS2 Damage System Explained v1.50 (Steam Guide)](https://steamcommunity.com/sharedfiles/filedetails/?id=3113174054)
- [Death Stranding — How to Balance Cargo (PowerPyx)](https://www.powerpyx.com/death-stranding-how-to-balance-cargo/)
- [Death Stranding Inventory Tips (GameSpot)](https://www.gamespot.com/articles/death-stranding-inventory-tips-how-your-inventory-works-and-how-to-best-manage-your-weight/1100-6471199/)
- [Hard Truck: 18 Wheels of Steel — Basics to Trucking (Steam)](https://steamcommunity.com/sharedfiles/filedetails/?id=3104711021)
- [Crazy Taxi (Grokipedia)](https://grokipedia.com/page/Crazy_Taxi_\(video_game\))
- [Jalopy review (evo)](https://www.evo.co.uk/reviews/21069/jalopy-review-the-slowest-driving-game-youll-ever-play)
- [Jalopy durability discussion (Steam)](https://steamcommunity.com/app/446020/discussions/0/135509412989661240/)
- [Easy Delivery Co. review — Low-poly 9-5 (GamingTrend)](https://gamingtrend.com/reviews/easy-delivery-co-review-low-poly-9-5/)
- [Package shipment accelerometer data (enDAQ)](https://blog.endaq.com/transportation-vibration-monitoring-what-really-happens-during-shipment)
- [Lateral Acceleration in Cornering (ScienceDirect)](https://www.sciencedirect.com/topics/engineering/lateral-acceleration)
- [Heavy vehicle stability SRT (OSHA)](https://www.osha.gov/training/library/powered-industrial-trucks/app-a)
- [Rollover Threshold and G-Force (Fire Apparatus Magazine)](https://www.fireapparatusmagazine.com/fire-apparatus/rollover-threshold-and-g-force/)
- [Cognitive Load in UX (AND Academy)](https://www.andacademy.com/resources/blog/ui-ux-design/cognitive-load/)
- [DiRT Rally turning off HUD (Steam)](https://steamcommunity.com/app/310560/discussions/0/490124466471047930/)
- [Video Game HUD & UI Design (Sunstrike Studios)](https://sunstrikestudios.com/en/HUD_design_in_games)
- [Kenney Game Assets All-in-1 (itch)](https://kenney.itch.io/kenney-game-assets)
