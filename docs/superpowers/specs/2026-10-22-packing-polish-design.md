# 포장 미니게임 다듬기 설계 — 손맛·연출 + 판단 깊이 (프로토타입 11/5)

상태: **초안(PD 검토 전)** · 근거 D-070(3D 포장), D-117(핵심 재미 먼저), D-119(제한 시간 = 주문판부터), D-120(핵심 재미 있음)
범위: 코드 변경 없음. 정적 판독만으로 쓴 문서이며 Play로 확인한 것은 없다(§1.4 "의심"은 실행 확인 전).

> 목표 한 줄: **포장 30초가 "칠하기"가 아니라 "시계를 보며 어디를 먼저 막을지 고르는 피트스탑"으로 느껴지게.**
> 새 시스템은 만들지 않는다. 이미 있는 WrapState·흡수·시계·AudioCueBus 위에 보이게 하고, 연결하고, 숫자를 고친다.

---

## 1. 현재 흐름 (파일:줄 근거)

### 1.1 진입과 시계
| 단계 | 하는 일 | 근거 |
|---|---|---|
| 주문판 열림 | `Time.timeScale = 0`, 프로토타입이면 `StartRunClock()` (90s 가설) | `Order/RunController.cs:125-138`, `:75` |
| 시계 | Board/Routing/Packing 단계엔 `unscaledDeltaTime`으로 `Elapsed` 증가, 0이 돼도 출발 시점까지는 안 끝냄 | `RunController.cs:375-385` |
| 포장 열기 | `OrderBoard` → `Services.Packing.Show(pickedOrders)` | `Order/OrderBoard.cs:448` |
| 카운트다운 | Packing 단계에도 표시, 남은 시간별 패널 스프라이트만 바뀜(소리·맥박 없음) | `Order/CountdownHUD.cs:77-105` |

→ **포장 중 timeScale = 0**. 포장 연출(트윈·코루틴)은 전부 `unscaledDeltaTime` / `WaitForSecondsRealtime` 여야 한다.

### 1.2 포장 단계 (Wrapping)
| 요소 | 동작 | 근거 |
|---|---|---|
| 작업대 | Y=200 공간에 카메라·키라이트 생성, FOV 35 | `Packing/PackingScreen.cs:205-242`, `:104-114` |
| 화물 생성 | 주문 1~3개 → 카탈로그 모델(계란·유리·활어·케이크) 또는 **Box는 기본 Cube 폴백** + `WrapState`·`WrapPainter` 부착 | `PackingScreen.cs:847-876`, `Resources/CargoModels/CargoModelCatalog.asset`(타입 1~4만) |
| 물건 전환 | ◀▶, 제목에 ●/○/✓(커버 50%↑) | `PackingScreen.cs:637-668` |
| 입력 | WASD 회전(unscaled), R 리셋, 1/2/3 재료, 좌클릭 드래그 = 스트로크, 휠 = 폭 | `Packing/PackingScreenInput.cs:112-144` |
| 스트로크 | Begin: 표면 raycast → `WrapRibbon` 생성(폭 = `stripWidthUV×4` m), Update: 모서리 적응 분할 + UV 이음새 판정 + RT 블릿 + 겹침 grid로 리본 솟음, End: 커버율 갱신 콜백 | `Packing/WrapPainter.cs:161-182`, `:184-231`, `:250-258`, `:128-159` |
| 상태 저장 | 256² RT, R/G/B = 버블/폼/하드케이스 채널, 칠하면 1 | `Packing/WrapState.cs:17-25`, `WrapPainter.cs:281-297` |
| 비용 | 드래그 길이 × 폭 × 256² × `costPerPixel` 누적 → 우측 패널 실시간 | `WrapPainter.cs:299-300`, `PackingScreen.cs:952-964` |
| 커버율 | **UV 픽셀 중 칠한 비율**(RT ReadPixels), 스트로크 끝날 때만 | `WrapState.cs:44-56`, `PackingScreen.cs:935-941` |
| 재료 수치 | Bubble 0.03/흡수 0.25, Foam 0.07/0.55, Hardcase 0.18/0.85 | `Resources/WrapMaterials/*.asset` |
| 사운드 | Cue 4종(MaterialSelect·StrokeStart·StrokeEnd·Confirm)은 발행되지만 **핸들러가 전부 no-op** | `Audio/AudioManager.cs:105-109`, `WrapPainter.cs:181,254` |

### 1.3 배치 단계 (Placement) → 출발
| 요소 | 동작 | 근거 |
|---|---|---|
| 진입 | "배치하기" → 큐브를 `CargoSimRoot` 자식으로, 위에서 내려다보는 카메라, 좌/우 패널 숨김 | `PackingScreen.cs:678-685`, `:702-761` |
| 드래그 | 바닥 평면 투영 + 트렁크 안 클램프, WASD 90° 스냅 회전 | `PackingScreen.cs:767-836`, `PackingScreenInput.cs:85-110` |
| 출발 | `PackingConfirm` cue → Hide → `BeginRun(orders, items)` | `PackingScreen.cs:687-696` |
| 인계 | 비용 합산 → `UpgradeSystem.Spend`, **모든 Collider DestroyImmediate**, `MountWrappedItem` | `RunController.cs:196-222` |
| 마운트 | 플레이어 위치는 유지, **회전은 identity로 덮어씀** | `Cargo/CargoShakeSimulator.cs:97-142` (`:107`) |

### 1.4 주행 중 포장의 의미
- 화물은 트렁크 바닥에서 2D로 미끄러지고 **±X/±Z 벽**에 부딪힐 때만 흡수 조회 → `dmg = raw × (1 − absorption)` (`CargoShakeSimulator.cs:172-225`, `:262-281`).
- 흡수 = 충격 방향 면의 UV 주변 27×27 픽셀에서 **가장 비싼 재료 하나** (B>G>R) (`WrapState.cs:79-120`).
- 제동(LongG 음수) → 화물이 +Z(앞벽)로 → **앞면이 가장 자주·세게 맞는다** (`CargoShakeSimulator.cs:181`).
- 윗면·아랫면은 절대 조회되지 않는다(벽이 4개뿐).
- Spin/Tilt/누적 G 손상은 포장과 무관 (`Cargo/CargoBox.cs:251-262`) → 활어(Spin)·케이크(Tilt)는 포장해도 주 손상원이 안 줄어든다.

### 1.5 발견 (판단 깊이를 만들기 전에 알아야 할 것)
| # | 내용 | 영향 | 확신 |
|---|---|---|---|
| F1 | 인계 시 Collider 전부 파괴(`RunController.cs:215-216`) → 주행 중 `QueryAbsorption`이 MeshCollider를 못 찾아 **항상 0** 반환(`WrapState.cs:81-83`) | 포장이 손상을 전혀 안 줄일 수 있음 = 지금의 "꼼꼼히"는 돈·시간만 버림 | **의심(정적)** — `[CargoShake] 충격 흡수` 로그가 Play 중 한 번이라도 찍히는지로 확인 |
| F2 | Box 폴백 = Unity 기본 Cube → 6면이 **같은 UV 0~1**을 공유 | 한 면 칠하면 6면 다 보호, 커버율 = 한 면 비율. "어느 면을 먼저" 판단이 Box에선 무의미 | 높음(Unity 기본 큐브 UV 구조) |
| F3 | 커버율이 UV 픽셀 비율 | 카탈로그 모델은 UV 빈 공간 때문에 100% 불가, 윗/아랫면도 섞임 → 숫자가 보호와 안 맞음 | 높음 |
| F4 | 마운트가 회전을 identity로 리셋 | 배치 단계 WASD 회전이 무효(보이기만 함) | 높음 |
| F5 | 재료 카드 비용 `₩{costPerPixel×1000}/m` = ₩30/60/180, 실제 기본 폭에선 m당 ≈ ₩118/275/708 | 표시가 4배 틀림 | 높음(식 계산) |
| F6 | 재료 이름이 에셋 `displayName`(Bubble/Foam/Hardcase) 영어, 랩 폭 라벨은 고정 문자열 | 다듬기 부족 인상 | 높음 |
| F7 | 정산 "포장 피드백" 자리가 빈 문자열(Task 17 미완) | 포장 결과를 플레이어가 한 번도 못 봄 → 학습 고리 없음 | 높음 (`Order/ResultPanel.cs:391-393`) |
| F8 | 2026-05 설계 덱(`PitCrew_3D포장_설계.pptx` 5장)은 "시간 압박 없음 — calm crafting" | D-119 이후 전제와 반대. 덱 기준 연출(느긋한 공예)은 그대로 쓰지 않는다 | — |

---

## 2. 손맛·연출 항목 (8개)

공통 규칙: timeScale 0이므로 **unscaled 시간만**. 연출은 **시계를 멈추지 않고 입력을 막지 않는다**(블로킹 0.35s 이하). 사운드는 이미 프로젝트에 있는 `F1/Assets/Sound/kenney_interface-sounds (1)` (scratch·pluck·tick·drop·confirmation·switch·error)만 쓴다 — 새 에셋 승인 불필요.

| # | 무엇을 | 어디에 | 난이도 | 기대 효과 |
|---|---|---|---|---|
| P1 | **스트로크 소리**: 감는 중 리본 길이 3~4cm마다 짧은 `scratch`/`pluck` 틱, 피치 = 재료(버블 1.15 / 폼 1.0 / 하드 0.8) × 드래그 속도(0.9~1.2). 시작 `drop`, 끝 `tick`, 재료 선택 `switch`, 확정 `confirmation` | 새 cue `PackingStrokeTick`(value=속도) → `AudioCueBus.cs:19-31` enum 1줄; 발행 `WrapPainter.UpdateStroke`(`:224` 근처, `dragDist` 누적); `AudioManager.SubscribeCues`(`:105-109`) no-op 교체 + `PlayClip`에 pitch 인자 오버로드(`:142`). 클립 연결은 인스펙터(에디터 토큰) 또는 `Resources/Audio/Packing/`로 복사 후 `Resources.Load` | 0.5일 | 손이 "감고 있다"를 귀로 확인. 재료 차이가 소리로 갈림. 가장 싼 체감 상승 |
| P2 | **리본 재질 3종 차별**: 버블 = 반투명·강한 스펙(뽁뽁이 광택), 폼 = 불투명·무광·약간 두꺼움, 하드케이스 = 불투명 진한 색·림 약하게. 스트로크 첫 5cm 폭 0→1 이징(테이프 풀리는 느낌) | `WrapPainter.GetOrCreateRibbonMaterial`(`:107-126`) 재료별 파라미터 표, 시작 이징은 `WrapRibbon.EmitCross`(`:117-132`)에 `uAccum` 기반 폭 배율 | 0.5일 | 무엇으로 쌌는지 한눈에. 배치 단계·트렁크 CCTV에서도 구분됨 |
| P3 | **면별 보호 게이지**(평균 커버 대체): 우측 패널에 `앞(제동) / 뒤 / 좌 / 우` 4줄, 각 면 흡수율 0~85%를 재료색 바로. 60% 넘는 순간 체크 + 짧은 `tick` | `PackingScreen.BuildRightPanel`(`:466-497`) 교체, 값은 M1의 `WrapState.QuerySideAbsorption`, 갱신 `OnStrokeCompleted`(`:935-941`) | 0.5~1일 (M1 포함) | 숫자가 실제 손상 계산과 같은 것을 가리킴(F3 해소). "어디가 비었나"가 보임 |
| P4 | **완료 도장**: "배치하기" 순간 화면 중앙 `포장 완료 A/B/C` 도장이 1.4→1.0 스케일 오버슛 + 2° 기울기 + `confirmation`, 0.35s 후 배치 화면. 등급 = 4면 가중 평균(앞 40%·뒤 20%·좌우 20%씩) | `PackingScreen.OnGoClicked`(`:678-685`) → 코루틴(`WaitForSecondsRealtime`), UI는 기존 `MakeLabel`(`:523`) | 0.5일 | 피트스탑 "끝!" 순간의 마침표. 등급이 꼼꼼함의 즉시 보상 |
| P5 | **카메라 펀치·화물 찌그러짐**: 스트로크 끝에 화물 스케일 1.04→1.0(0.12s), 재료 카드 선택 시 카드 1.08 바운스, 도장 순간 작업대 카메라 FOV 35→33→35 | `WrapPainter.EndStroke`(`:250`) 이벤트 → `PackingScreen`에서 unscaled 트윈; 카드 `HighlightActiveCard`(`:621-632`) | 0.3일 | 입력마다 "먹혔다" 응답. 비용 거의 0 |
| P6 | **포장 중 시간 압박 연출**: 남은 10초부터 카운트다운 1초마다 `tick` + 텍스트 1.15 맥박, 남은 5초부터 작업대 배경색 붉게 lerp | `CountdownHUD.Update`(`:77-105`) — `lastWholeSecond` 바뀔 때; 배경은 `PackingScreen` LateUpdate에서 `packingCamera.backgroundColor` | 0.3일 | "빨리 vs 꼼꼼히"의 '빨리' 쪽을 몸으로 느끼게. 핵심 긴장 직결 |
| P7 | **표시 정리**: 재료 이름 한글(버블랩/폼/하드케이스), 카드 비용을 실제 m당 값(현재 폭 기준)으로, 랩 폭 라벨이 휠에 따라 ▬/▬▬/▬▬▬ 갱신 | 에셋 `displayName` 3개(YAML 문자열), `BuildMaterialCard`(`:388`) 식 교체, `PackingScreenInput`(`:141-143`) 후 라벨 갱신 | 0.2일 | 거짓 숫자 제거(F5·F6). "대충 만든 화면" 인상 제거 |
| P8 | **정산 "포장이 막은 손상"**: 주행 중 흡수로 줄어든 손상 누적 → 결과 화면 한 줄 `포장이 막아 준 손상 −23 (앞면 하드케이스)` | 누적 `CargoShakeSimulator.TryApplyBoxImpactDamage`(`:277` `raw-dmg`), 표시 `ResultPanel.cs:391-393` 빈 자리 | 0.5일 | 포장 → 결과 인과를 처음으로 보여 줌(F7). 다음 판 포장 판단이 학습됨 |

빼 둔 것: 파티클(종이 부스러기), 손 모델/롤 애니메이션, 미니뷰 부활(도현이 폐기 지시), 포장 BGM.

---

## 3. 메커니즘 깊이 후보 (제한 시간 안에서 의미 있는 판단만)

판정 기준: ① 30초 안에서 **고를 거리**가 생기는가 ② 주행에서 **결과가 보이는가** ③ 새 시스템 없이 기존 WrapState/흡수 위에 얹히는가.

### M1. 면별 보호 = "앞면부터" (추천 ★)
- **플레이어 판단**: 시간이 없다 → 제동 때 맞는 **앞면**만 하드케이스로 두껍게? 4면 다 버블로 얇게? 좌우(코너)까지 갈까?
- **이미 있는 것**: 손상은 이미 면별(±X/±Z)로 계산됨(§1.4). 플레이어가 그걸 모를 뿐 — 보여 주기만 하면 판단이 생긴다.
- **구현 위치**
  - `WrapState.QuerySideAbsorption(Vector3 localDir)` — 기존 `QueryAbsorption`(`:79-94`)을 아이템 중심 + 로컬 축으로 감싼 얇은 래퍼. 4방향 × 스트로크 끝 1회.
  - 화물 로컬 +Z에 "앞 ▲" 표식(작은 쿼드/TMP 3D) — WASD로 돌려도 화물과 같이 돈다. 마운트가 회전을 identity로 되돌리므로(F4) **로컬 +Z = 트렁크 앞**이 그대로 성립.
  - 주문/화물 정보 한 줄: "제동 때 앞면을 맞음" (`CargoTypePresets.DriveTip` 옆).
- **위험**
  - F1(흡수 0 의심)이 사실이면 보여 줘도 거짓말 → **반드시 B0 먼저**.
  - F2: Box(기본 Cube)는 6면 UV 공유 → 면별 값이 모두 같음. Box 주문이 풀에 있으면 코드로 6면 아틀라스 UV 큐브(3×2) 메시를 만들어 폴백 교체(0.3일). 카드보드 텍스처 타일이 바뀌는 정도의 부작용.
  - 배치 단계 회전(F4)이 무효라는 사실과 충돌하지 않게: 배치 WASD 회전을 막거나(추천, 1줄) 마운트가 회전을 유지하게(흡수 축 계산도 맞춰야 함 → 더 큼).

### M2. 오늘의 약한 면 (M1 위 1단 확장)
- **플레이어 판단**: 주문마다 4면 중 1면(가끔 2면)이 "금 간 면" — 그 면 맞으면 손상 ×2. 화물을 **돌려서 찾고**, 그 면을 앞으로 두지 않게 배치하거나 그 면부터 감는다. 같은 화물이라도 판마다 손이 달라짐.
- **구현 위치**: `WrapState`에 `weakSideMask`(Show 때 주문 시드로 결정, `PackingScreen.SpawnItem` `:847-876`), 표면에 빨간 테이프 띠 표시(데칼 대신 리본 1개를 코드로 미리 그림 — `WrapRibbon` 재사용), 손상 배율 `CargoShakeSimulator.TryApplyBoxImpactDamage`(`:277`) 한 곳.
- **위험**: 배치 회전이 무효(F4)인 채면 "약한 면을 옆으로 돌려 싣기" 판단이 사라짐 → 배치 회전을 살리려면 마운트·흡수 축을 같이 고쳐야 함(+0.5일). 카탈로그 모델은 면 개념이 모호(병·케이크) → "면 = 트렁크 기준 4방향"으로만 정의.

### M3. 재료 = 시간 트레이드오프 (숫자 조정 중심)
- **문제**: 지금 재료 차이는 돈뿐. 프로토타입 판 보상 대비 포장비가 작으면 늘 하드케이스가 정답 → 판단 0.
- **플레이어 판단**: 하드케이스 = 강하지만 **좁은 띠**(같은 면 덮는 데 스트로크 2~3배 = 시간), 버블 = 약하지만 **넓은 띠**(한 번에 덮음). "앞면만 하드로 좁게 vs 전체 버블로 빠르게"가 시계 위의 선택이 된다.
- **구현 위치**: `WrapMaterialDef`에 `widthMul`(버블 1.6 / 폼 1.0 / 하드 0.55 가설) 1필드, `WrapPainter.BeginStroke`(`:170`)·`DrawStrip`(`:293`)에서 폭 × widthMul. 휠 폭 조절은 유지하되 재료별 상한.
- **위험**: 숫자만으로 판단이 생기는지는 플레이로만 확인 가능. 띠가 너무 좁으면 그냥 짜증 → 첫 값은 보수적으로, 도현 1판 판정.

### M4. 테이프 고정 안 하면 리본 풀림 (보류 추천)
- **플레이어 판단**: 감은 뒤 끝을 테이프로 눌러야 고정, 안 하면 주행 중 리본이 풀려 흡수 감소.
- **구현**: 새 입력(우클릭 테이프), 리본별 고정 상태, 주행 중 풀림 시뮬, 트렁크 CCTV 표시 — **새 시스템 3개**.
- **위험**: 11/5 안에 손맛 좋게 만들기 어렵고, 시간 압박 하의 추가 동작은 "꼼꼼히"가 아니라 "귀찮음"이 되기 쉬움. → `ideas-parking`으로. M1~M3 플레이 판정 뒤 재검토.

(같은 이유로 "겹쳐 감기 = 흡수 누적"도 보류: 겹침 grid가 `WrapPainter`에만 있어 `WrapState`로 옮기는 구조 변경이 필요. M3로 같은 '두껍게 vs 넓게' 판단을 더 싸게 얻는다.)

---

## 4. 추천 순서 — 2~3일 묶음 "보이는 보호"

**왜 이 묶음**: 깊이(M1)와 손맛(P1·P3·P4·P8)이 한 고리로 이어진다 — *앞면이 중요하다는 걸 보여 주고(P3·앞 표식) → 감는 손맛(P1·P2) → 완료 도장(P4) → 주행 → 정산에서 "포장이 막은 손상"(P8)*. 새 시스템 없음. M2·M3는 이 묶음 판정 뒤.

| 순서 | 작업 | 파일 | 시간 | 완료 기준(검증) |
|---|---|---|---|---|
| B0 | **흡수 실작동 확인·수정**(F1). 확인: Play에서 앞벽 충돌 시 `[CargoShake] 충격 흡수` 로그 유무. 없으면: 인계 때 MeshCollider는 남기고 `enabled` 유지 + 다른 Collider만 제거(또는 마운트 시 MeshCollider 재부착). CargoSimRoot가 차와 물리 충돌하지 않는지 같이 확인 | `RunController.cs:215-216`, `WrapState.cs:81-83` | 0.5일 | 하드케이스 앞면 / 무포장 두 판의 같은 급제동에서 손상 차이 수치(로그) |
| B1 | Box 폴백 6면 UV 큐브(F2) — 주문 풀에 Box가 있을 때만 | `PackingScreen.SpawnItem` `:860-865` | 0.3일 | Box 앞면만 칠했을 때 좌/우 흡수 0 |
| B2 | M1 + P3: `QuerySideAbsorption`, 면별 4줄 게이지, 화물 +Z "앞 ▲" 표식, 배치 단계 WASD 회전 차단(F4 정합) | `WrapState.cs`, `PackingScreen.cs:466-497,935-941`, `PackingScreenInput.cs:97-108` | 0.8일 | 앞면만 감으면 앞 바만 차오름(eval로 4값 출력) |
| B3 | P1 스트로크 소리 + 기존 cue 4종 연결 | `AudioCueBus.cs`, `AudioManager.cs:105-109,142`, `WrapPainter.cs:224` | 0.5일 | 감는 동안 틱 간격이 드래그 속도에 비례(직접 듣기 — 도현 판정 항목) |
| B4 | P4 완료 도장(등급 = 면별 가중) + P5 펀치 | `PackingScreen.OnGoClicked` `:678-685` | 0.4일 | 도장 0.35s 이내, 그동안 시계 계속 감소(Elapsed 비교) |
| B5 | P8 정산 "포장이 막은 손상" | `CargoShakeSimulator.cs:277`, `ResultPanel.cs:391-393` | 0.4일 | 결과 화면에 −N 표시, 무포장 판은 0 |
| B6 | P7 표시 정리 (+ 여유 있으면 P2 리본 재질, P6 마지막 10초) | 에셋 3개, `PackingScreen.cs:388`, `PackingScreenInput.cs:141-143` | 0.2~0.8일 | 한글 이름·실제 m당 비용 표시 |

합계 약 3.1일 (Box 주문이 없어 B1 생략 시 2.8일, B6 확장분 +0.6일). 에디터 토큰은 B0·B3(클립 연결)·전체 Play 확인에만 필요 — 다른 작업자 사용 중이면 B1·B2·B4·B5·B6 정적 구현 먼저.

**도현 판정으로 넘길 것 하나**: "포장 30초 동안 '앞면부터 감을까, 다 감을까'를 고민했나?" — 예면 M3(재료=시간) 숫자 조정, 아니면 M2(약한 면)로 판단 거리를 늘린다.

## 5. 가설 수치 (플레이 후 조정)
- 면 가중(도장 등급): 앞 0.4 / 뒤 0.2 / 좌 0.2 / 우 0.2. A ≥ 60%, B ≥ 30%.
- 스트로크 틱 간격: 리본 3.5cm. 피치 범위 0.8~1.3.
- 도장 연출 0.35s, 화물 펀치 0.12s, 마지막 10초부터 틱.
- M3 widthMul: 버블 1.6 / 폼 1.0 / 하드 0.55.
