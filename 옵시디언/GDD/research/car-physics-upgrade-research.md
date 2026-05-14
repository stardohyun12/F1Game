# 차량 물리 업그레이드 리서치 — "바람에 갈대" 탈출

> 세션 14 후반 도현 피드백("흔들리는 게 바람에 흔들리는 갈대 같다")에 대한 GitHub/Unity Discussions 조사. 2026-04-25.

## 1. 문제 + 제약

차량이 가볍고 즉각적이라 관성이 없어 보인다. 제약 고정: **D-007**(아케이드+약간 무게감), **D-019**(풀 시뮬/WheelCollider/슬립커브 기각), **D-040**(마을 배송 톤 — 트럭의 묵직함+반응성). 추가로 `realtime-tension-research.md` §1의 **화물 Shock 0.6g/0.7g 경고선은 g값이 프레임 노이즈로 안 튀어야 성립** → 관성 정리는 2주차 Shock MVP의 **선결 조건**이지 폴리싱이 아님.

## 2. 오픈소스 비교 (3~5개)

| 리포 | ★ | Lic | Unity | 핵심 | 차용 | 부적합 |
|---|---|---|---|---|---|---|
| [SergeyMakeev/ArcadeCarPhysics](https://github.com/SergeyMakeev/ArcadeCarPhysics) | 446 | MIT | 2022+ | raycast 서스펜션+Ackermann+다운포스+in-flight stabilizer | 스피드 커브·스태빌라이저 개념 | 4각 raycast가 D-019 스펙 근접 |
| [**benmcinnes/ArcadeVehiclePhysics**](https://github.com/benmcinnes/ArcadeVehiclePhysics) | 199 | MIT | 2018.4 | 바퀴 raycast+**별도 visual rig**가 side speed로 roll, accel로 pitch (물리 무영향) | **핵심 레퍼런스 — 물리 rig ≠ 비주얼 rig** | Unity 6 API 재확인 필요 |
| [Saarg/Arcade_Car_Physics](https://github.com/Saarg/Arcade_Car_Physics) | 124 | MIT | 2020+ | WheelCollider+AnimationCurve+드리프트 | AnimationCurve 튜닝 패턴 | WheelCollider 코어 — D-019 위반 |
| [adrenak/tork](https://github.com/adrenak/tork) | 447 | MIT | 2020 | TorkWheel+Ackermann+안티롤바 add-on | 속도별 조향(이미 적용), 안티롤바 visual-only 차용 | 고속 리얼리즘 미지원 |
| [hayden-donnelly/vehicle-physics](https://github.com/hayden-donnelly/vehicle-physics) | 28 | MIT | 2018.1 | 단순 raycast 서스펜션 | 구조 참고만 | 규모/유지보수 이슈 |

**결론:** 1순위 **benmcinnes** — visual rig 분리가 D-019 아래 "기울임"을 얻는 유일한 안전책. 나머지는 개념 발췌용.

## 3. Discussions/Forum 패턴 10개

1. **Center of mass 차체 아래** (y=-0.3~-1.0). *현재 적용 중.*
2. **접지 drag↑ / 공중 drag↓** 분기. *미적용 (airDrag 단일값).*
3. **입력 SmoothDamp 0.08~0.14s** — raw 즉시 반영 금지. ***완전 미적용, 핵심 누락.***
4. **AnimationCurve 비선형 응답**. *이미 turn/brake 커브 적용.*
5. **AddForceAtPosition squat/dive**. *미적용. [Unity 6 버그 리포트](https://discussions.unity.com/t/addforceatposition-physics-broken-in-unity-6/1594092) 재현 검증 필요.*
6. **자식 Transform에 lean lerp** (Rigidbody 회전 무손상). *미적용, benmcinnes 그대로.*
7. **공중 thrust/torque 급감** — `isGrounded=true` 하드코딩 복원 필요.
8. **Mass는 무게감 안 줌** — accelForce=mass*maxSpd/accelTime 선형 재계산이라 체감 동일. 공식은 이미 올바름.
9. **조향 = input smoothing + MoveRotation 조합**. 드리프트 분기는 올바른 구조, smoothing만 추가.
10. **Deadzone + input^1.5**. PlayerInput Processor로 처리.

"바람에 갈대" = **3 + 6 + 2** 복합 결핍.

## 4. 우선순위 매트릭스

| 기법 | 효과 | 난이도 | 채택 |
|---|---|---|---|
| 입력 SmoothDamp | 5 | 1 | **TOP 1** |
| Visual body rig (자식 Transform) | 5 | 2 | **TOP 2** |
| 접지별 drag + 저속 가속 커브 | 4 | 1 | **TOP 3** |
| Deadzone + ^1.5 input | 3 | 1 | TOP 3 부록 |
| AddForceAtPosition squat/dive | 3 | 3 | 파킹 (Unity 6 버그 검증 후) |
| Engine RPM 확장 | 3 | 2 | 선택 (사운드 별도) |
| Raycast 4-corner 서스펜션 | 4 | 5 | **기각 (D-019)** |
| Mass 대폭↑ / Steering lag 0.3s | 1 | 1 | 이미 철회/과도 |

## 5. "갈대" 해결 TOP 3 (근거)

**TOP 1 — 입력 SmoothDamp (steerLag ≈ 0.10~0.14s)**
[Seena Burns](https://seenaburns.com/2018/03/02/car-controller/): "MoveRotation은 responsive하지만 real-car처럼 안 느껴진다." MoveRotation 버리면 드리프트 분기가 깨지므로 **raw input만 smoothing**. 스틱을 휙 꺾어도 yaw가 0.1초 뒤 반응 → "입력-반응 사이의 질량"이 태어남. "갈대" 해결 ~60%. 보너스: 화물 횡G 커브가 깔끔해져 Shock 감지 안정.

**TOP 2 — Visual body rig (자식 Transform lerp)**
benmcinnes의 "물리 rig ≠ 비주얼 rig" 패턴. 차체 메시를 자식 Transform에 두고 가속/조향 신호로 localRotation lerp. **Rigidbody 회전 불변** → 드리프트/MoveRotation 보존, D-019 준수. 매핑: 가속→squat(~2°), 제동→nose dive(~3°), 조향×속도norm→roll(~5°). 경량감 체감 ~40% 감소, 물리 회귀 0.

**TOP 3 — 접지별 drag + 저속 스로틀 램프**
[Doofah tutorial](https://www.doofah.com/tutorials/unity/bouncy-vehicle-tutorial/): 접지 drag 3~4 / 공중 0.1 분기. `throttleResponseBySpeed` 커브(저속 0.6x → 0.2 normSpeed에서 1x 램프)로 "밟자마자 튀는" 느낌 제거. 전제: `isGrounded=true` 하드코딩을 실제 Raycast로 복원 (2주차 Shock 선결 작업과 겸함).

## 6. 구현 스케치 (1주 내)

**6-1. 입력 SmoothDamp — 1~2h**
필드: `steerInputLag=0.10, throttleInputLag=0.08`. 상태: `Vector2 smoothedInput, inputVelocity`. FixedUpdate 진입에서 `Vector2.SmoothDamp`(y축 개별 권장). 이후 ApplyThrottle/ApplySteering이 smoothed 사용. **결정 포인트:** `SteerInput`/`ThrottleInput` 프로퍼티를 **raw와 smoothed 둘 다 노출** — FuelSystem은 raw(역치 안정), FollowCamera/CarVisualLean은 smoothed(리듬 일치).

**6-2. CarVisualLean.cs — 2~3h**
신규 컴포넌트. 필드: `Transform visualRoot, ArcadeCarController car, float maxPitchDeg=3, maxSquatDeg=2, maxRollDeg=5, leanLerpSpeed=8`. LateUpdate에서 `targetPitch = -accel * (accel>0 ? maxSquatDeg : maxPitchDeg)`, `targetRoll = -car.SteerInput * maxRollDeg * speedNorm`, `visualRoot.localRotation = Quaternion.Lerp(...)`. 씬 세팅: Race 프리팹 아래 빈 `VisualRoot` 추가, 차체 메시를 자식으로 이동. **Collider는 VisualRoot에 두면 안 됨** (Rigidbody 루트만). **CargoBox는 VisualRoot 밖** 유지 — 충돌판정이 기울기에 딸려가면 버그. **카메라 중복 주의:** FollowCamera maxRoll=5가 이미 있음 → 차체 5° + 카메라 5° = 과함. 도현과 Play 테스트 후 **카메라 roll 2~3° 축소** 또는 **차체 roll만 사용** 택1.

**6-3. 접지별 drag + 스로틀 커브 — 1~2h**
필드: `groundedLinearDamping=0.3, airborneLinearDamping=0.05`, `throttleResponseBySpeed = (0→0.6, 0.2→1.0, 1→1.0)`. FixedUpdate 초기 `rb.linearDamping = isGrounded ? grounded : airborne`. ApplyThrottle에 curve.Evaluate(speedNorm) 추가 곱. 전제: `isGrounded` 하드코딩 제거, `Physics.Raycast(pos+up*0.1, down, groundCheckDistance, groundLayer)` 복원. Ground 레이어 세팅 선행.

## 7. 건드리지 말 것

- **ApplySteering의 `IsDrifting==false` MoveRotation 경로** — 드리프트 yaw 튜닝 직결. 입력만 smoothed 교체, 수식 보존.
- **`handbrakeAction.IsPressed()` 폴링** — 세션 4 release 버그 수정 경로. 유지.
- **FuelSystem/GearSystem 연동 프로퍼티** (`CurrentSpeedMps`, `ThrottleInput`, `MaxSpeedMps`) — 시그니처 변경 금지. `ThrottleInput` smoothed 전환 시 FuelSystem 역치 흔들림 → **raw/smoothed 양쪽 프로퍼티 노출**, FuelSystem은 raw.
- **`accelForce = mass * maxSpeedMps / accelTime`** — RecalculateForces 공식.
- **Race 프리팹의 CargoBox 부착 위치** — Rigidbody 루트 직계 유지.
- **EngineAudio 속도 기반 pitch/volume** — 프로퍼티만 지키면 무관.

## 8. 핵심 3줄 요약

1. "갈대" ~70%는 **입력 즉시 반영 + 시각 피드백 부재**의 복합. mass/drag 장난 아니라 **SmoothDamp 입력 + Visual body lean rig** 한 세트가 필요.
2. benmcinnes의 **물리 rig ≠ 비주얼 rig** 패턴이 D-019 아래 기울기를 얻는 유일 안전책 — Rigidbody 회전 무손상으로 자식 Transform만 lerp.
3. 업그레이드는 **2주차 Shock MVP 선결 조건** — 0.6g/0.7g 경고선이 프레임 노이즈에 오작동하지 않으려면 관성 정리가 먼저다.

---

**참고 링크 (추가):** [Unity — Mathf.SmoothDamp](https://docs.unity3d.com/ScriptReference/Mathf.SmoothDamp.html) · [Unity Discussions — Arcade Car Physics Q&A](https://discussions.unity.com/t/how-to-make-arcade-car-physics/761969) · [Discussions — Rigidbody Heavier](https://discussions.unity.com/t/making-rigidbody-movement-feel-heavier/649855) · [Discussions — Not Slippery](https://discussions.unity.com/t/how-to-make-rigidbody-movement-not-slippery/148493) · [Medium — Ground Hugging Vehicles](https://medium.com/thefloatingpoint/ground-hugging-vehicles-in-unity-3d-50115f421005) · [Forza Tuning Guide](https://forzatune.com/guide/the-fully-updated-forza-tuning-guide/) · [Art of Rally 물리 피드백](https://steamcommunity.com/app/550320/discussions/0/2957167122134093270/).
