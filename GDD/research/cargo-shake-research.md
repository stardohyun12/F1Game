# Cargo Shake 리서치 — "갈대 탄성"에서 "트렁크 안 관성"으로

> 세션 14 후반, 2026-04-25. CargoShakeSimulator의 물리적 느낌 개선.
> 관련: `GDD/research/realtime-tension-research.md` (Shock G 공식).

---

## 1. 현재 구현과 "갈대" 진단

현재 `F1/Assets/Scripts/Cargo/CargoShakeSimulator.cs`는 LateUpdate에서 매 프레임 `CargoBox.SignedLongG / SignedLatG`를 읽어 박스의 `localPosition`·`localRotation`을 `restPos`·`restRot` 기준으로 **Lerp**한다. G가 0에 수렴하면 박스는 즉시 rest로 부드럽게 복귀한다. 이 구조는 스프링-댐퍼 위에 매달린 갈대와 수학적으로 동일하다 — G가 "바람", rest가 "뿌리", Lerp가 "탄성 복원력". 도현의 "갈대 같다"는 피드백은 정확한 진단이다. 실제 트렁크 박스는 (a) 정지 마찰을 넘겨야 움직이기 시작하고, (b) 일단 슬라이드하면 관성으로 계속 밀리다가 운동 마찰로 감속하며, (c) 벽에 부딪히면 그 위치에 고정되어 더는 움직이지 않는다. 즉 **rest로 돌아가는 복원력이 없다** — 화물은 "정지 상태의 어딘가"로 수렴하지, "중앙"으로 수렴하지 않는다. 현재 구현에는 (a) 정지 마찰 임계, (b) 관성 메모리, (c) rest 복귀 제거 이 셋이 모두 빠져 있다.

---

## 2. 구현 패턴 비교표

| 패턴 | 작동 원리 | 장점 | 단점 | 난이도 |
|---|---|---|---|:-:|
| **A. Rigidbody + 트렁크 벽 Collider** | 박스 = 독립 Dynamic Rigidbody. 트렁크 벽 4개 = Kinematic Rigidbody, `MovePosition`으로 차량 따라감. 박스는 PhysX 관성 + 충돌로 자연스럽게 미끄러지다 벽에 부딪혀 멈춤. Physic Material의 static/dynamic friction이 정지/운동 마찰 그대로 담당. | 물리 엔진이 모든 일을 해줌. 회전·굴러감 등 고차 효과도 자동. "진짜 박스" 느낌 최상. | CargoSim이 y=-500 외딴 위치라 하이브리드 레이어 분리 필요. Rigidbody 수가 FixedUpdate 부하. 큰 차량 가속 시 터널링 가능성. 차량이 바깥 월드 좌표계로 움직이지 않는 "스테이지"라 트렁크 `MovePosition`은 실제 차량 Rigidbody의 `linearVelocity`를 베껴 적용해야 함. | 4 |
| **B. Kinematic + Custom physics state** | 박스 `isKinematic=true`. 자체 `Vector3 velocity` 필드 관리. FixedUpdate: `accel = G * 9.81; velocity += accel * dt`. 매 프레임 Coulomb 마찰 적용 — `speed < μ_s*g*dt` 이면 정지 간주, 아니면 `velocity -= velocity.normalized * μ_k*g*dt`. AABB clamp로 벽 튕김 (`velocity *= -bounciness`). | 완벽한 격리 (레이어/충돌 매트릭스 불필요). 튜닝 파라미터 명확 (μ_s, μ_k, AABB 크기, 반발계수). CargoSim y=-500 구조 유지. 성능 초경량. rest 복귀 없음 → 갈대 느낌 완전 제거. | 회전은 별도 로직 필요 (기울기는 G 기반 단순 Lerp로 충분). 3D 충돌 감응 (롤링, 다른 박스) 없음 — MVP 단일 박스라 무관. | 3 |
| **C. 현재 Lerp 개선 (히스테리시스 + 누적)** | 정지 마찰 임계 추가: `abs(G) < 0.3` 이면 target 갱신 금지 (동결). G가 임계 이상이면 target을 **누적 오프셋**으로 이동 (rest 대신 `currentTarget += G*dt*k`). maxPosOffset으로 AABB clamp. rest 복귀 완전 제거 — G 해제 시 현재 위치 유지. | 기존 구조 최소 수정. Rigidbody 추가 없음. 1~2시간 작업. | "관성"이 물리적으로 정확하지 않음 (velocity 개념 없고 target만 누적). 벽 bounce 없음 (clamp에 붙어버림). 쌓이는 느낌은 있지만 여전히 조금 인공적. | 2 |
| **D. Verlet / Mass-spring-damper** | 박스를 파티클 노드망으로 구성. 매 스텝 `x' = x + (x - x_prev) + a*dt^2` + 스프링/댐퍼 제약. | 소프트바디 표현 가능 (천 박스, 액체). | MVP에 overkill. 튜닝 난이도 극대. 1주 이상. | 5 |
| **E. Rigidbody + SpringJoint** | 박스 = Dynamic Rigidbody, 차량 한 지점에 SpringJoint 연결. 스프링 tension/damper 조정. | Unity 네이티브, 설정 간편. | **갈대 그 자체** — 스프링의 복원력이 본질이라 rest 복귀를 증폭. 현재 문제와 동일한 성격. | 2 |

### 코드 스케치 (10~20줄, 의사코드)

**패턴 A — Rigidbody + 벽 Collider (의사코드):**
```
// CargoSim 하위에 BoxRigidbody(Dynamic) + 벽 4개 KinematicRigidbody
// FixedUpdate in Simulator:
//   trunkVelocity = vehicle.linearVelocity (메인 월드에서 복제)
//   trunkAngularVel = vehicle.angularVelocity
//   모든 벽.MovePosition(restWallPos + trunkDelta)
// Physic Material: staticFriction=0.5, dynamicFriction=0.3, combine=Average
// Layer: CargoSimLayer 전용. Collision Matrix에서 메인 차량 레이어와 차단.
```

**패턴 B — Kinematic + Custom physics (의사코드):**
```
// FixedUpdate:
//   Vector2 G = new(cargo.SignedLatG, cargo.SignedLongG);   // x=lat, y=long (둘 다 수평면)
//   Vector2 accel = -G * 9.81f;                             // 관성력은 G의 반대
//   velocity += accel * dt;
//   float speed = velocity.magnitude;
//   float stopThreshold = mu_s * 9.81f * dt;                // 정지 마찰 임계
//   if (speed < stopThreshold && G.magnitude < mu_s) velocity = Vector2.zero;
//   else velocity -= velocity.normalized * mu_k * 9.81f * dt;  // 운동 마찰
//   position += velocity * dt;
//   // AABB clamp + bounce
//   if (position.x > maxX) { position.x = maxX; velocity.x *= -bounce; }
//   ... (4면)
//   boxTransform.localPosition = new Vector3(position.x, 0, position.y);
//   // 회전은 기존 Lerp 유지 (기울기는 갈대여도 OK)
```

**패턴 C — Lerp 개선 (의사코드):**
```
// 기존 restPos 복귀 제거. targetPos를 누적 변수로.
// FixedUpdate:
//   if (|SignedLongG| > staticThreshold) targetPos.z -= SignedLongG * k * dt;
//   if (|SignedLatG|  > staticThreshold) targetPos.x -= SignedLatG  * k * dt;
//   targetPos = clamp(targetPos, -maxOffset, +maxOffset);    // 벽
//   // G가 임계 이하면 targetPos 그대로 유지 (정지)
// LateUpdate:
//   boxTransform.localPosition = Lerp(current, targetPos, posT);  // 빠른 추종
```

---

## 3. 레퍼런스 링크

**Unity 포럼 / 공식 문서**
- [Independently moving Rigidbody inside a moving Rigidbody — Unity Discussions](https://discussions.unity.com/t/independently-moving-rigidbody-inside-a-moving-rigidbody/68314)
- [Rigidbodies will slide off of moving kinematic rigidbodies, fix? — Unity Discussions](https://discussions.unity.com/t/rigidbodies-will-slide-off-of-moving-kinematic-rigidbodies-fix/757829)
- [How to stop rigidbodies sliding and falling off moving platforms — John Stejskal](http://johnstejskal.com/wp/how-to-stop-rigidbodies-sliding-and-falling-off-moving-platforms-in-unity3d-and-2d/)
- [Simulating friction manually — Unity Discussions](https://discussions.unity.com/t/simulating-friction-manually/414832)
- [Collider surface friction — Unity 6 Manual](https://docs.unity3d.com/6000.0/Documentation/Manual/collider-surface-friction.html)
- [Rigidbody.MovePosition — Unity ScriptReference](https://docs.unity3d.com/ScriptReference/Rigidbody.MovePosition.html)
- [Catlike Coding — Movement / Physics](https://catlikecoding.com/unity/tutorials/movement/physics/)

**상용/오픈소스 차량 + 화물**
- [Vehicle Physics Pro — Component Guide (Solid Cargo / Liquid Cargo)](https://vehiclephysics.com/components/component-guide/) — 상용 레퍼런스. Solid Cargo는 부피/질량/위치를 파라미터로 차량 동역학에 반영. Liquid Cargo는 탱크 내 sloshing 시뮬.
- [Randomation Vehicle Physics 2.0 — GitHub](https://github.com/JustInvoke/Randomation-Vehicle-Physics) — Unity 오픈소스 차량 물리. 화물 전용은 아니지만 로컬 프레임 처리 참고.
- [ArcadeCarPhysics — SergeyMakeev/GitHub](https://github.com/SergeyMakeev/ArcadeCarPhysics) — 아케이드 차량. MVP 튜닝 철학 일치.
- [Saarg/Arcade_Car_Physics — GitHub](https://github.com/Saarg/Arcade_Car_Physics) — WheelCollider 기반.
- [BeamNG.drive — Soft-body Physics](https://www.beamng.com/game/about/physics/) — Node/Beam 구조. PIT CREW에는 과함, 원리 참고용.
- [BeamNG v0.31 — Moving Cargo 업데이트](https://racesimcentral.net/beamng-drive-v0-31-focuses-on-moving-cargo-trucks-and-vr-improvements/) — 화물 이동 시뮬 사례.

**트럭 시뮬 업계 / 디자인 참고**
- [SCS Software — ETS2 1.57 Dynamic Cargo Loading](https://blog.scssoft.com/2025/10/157-update-dynamic-cargo-loading.html)
- [Death Stranding Cargo Management Guide — Game Truth](https://www.gametruth.com/guides/death-stranding-cargo-management-guide-route-planning-and-weight-balance/) — 무게 중심/기울기 시스템.

---

## 4. PIT CREW 추천안 — **패턴 B (Kinematic + Custom physics)**

**근거:**
1. **격리 원칙 보존.** CargoSim y=-500 외딴 위치 + CCTV 구조는 세션 14 D-040 확정. 패턴 A는 Rigidbody + Collision Matrix + 레이어 분리까지 건드려야 하고, 차량 Rigidbody의 `linearVelocity`를 CargoSim 트렁크 벽에 복제하는 "이중 물리" 구조가 디버깅 지옥이 될 수 있다. 패턴 B는 G 값만 읽어 완전히 독립적으로 계산한다.
2. **"갈대" 제거 핵심 3요소가 그대로 들어감.** velocity 필드 = 관성 메모리, μ_s 임계 = 정지 마찰, AABB clamp = 벽. rest 복귀를 구조적으로 **쓰지 않는다**.
3. **튜닝 파라미터가 직관적.** `staticFriction = 0.4`, `kineticFriction = 0.3`, `maxOffset = 0.5m`, `bounce = 0.2` — 도현이 숫자 4개만 굴리면 된다.
4. **MVP 범위 준수.** 1~3일 작업. Rigidbody·Collider 추가 없음. 단일 박스라 다물체 상호작용 고려 불필요.
5. **회전은 현재 Lerp 유지.** 기울기는 G 순간값에 비례하는 단순 시각 효과라 "갈대"여도 자연스럽다 — 트렁크 안에서 박스가 기울어지는 건 실제로 질량 중심 변위에 따른 잠시 기울기니까. **문제는 position이었다.**
6. **패턴 C와의 비교.** C는 작업량 절반이지만 "velocity 개념 없음"이 결국 한계. 벽에 부딪혔을 때 bounce 없이 clamp로 붙어있는 느낌은 여전히 가짜. B는 C보다 1일 더 걸리지만 "진짜 미끄러지는" 감각의 차이가 크다.

**반대 근거 (패턴 A 선택 시):** 팀이 향후 "박스가 두 개, 서로 부딪힘" 같은 확장을 원한다면 A가 맞다. 하지만 MVP 체크리스트 규칙 1: 이 기능이 없으면 재미가 없는가? — **NO.** 단일 박스 미끄러짐으로 충분. 다중 박스는 `ideas-parking.md` 행.

---

## 5. 구현 단계 (1~3일)

**Day 1 — 코어 물리 (반나절)**
1. `CargoShakeSimulator.cs` 상단에 `Vector2 velocity`, `Vector2 posOffset` 필드 추가. `restPos` 유지하되 **복귀 Lerp 제거**.
2. `Update`/`LateUpdate` 대신 `FixedUpdate`로 이동 (물리 dt 고정이 핵심 — frame rate 독립).
3. G 입력 → 관성력 역변환: `accel = -new Vector2(SignedLatG, SignedLongG) * 9.81f`.
4. velocity 적분: `velocity += accel * dt`.
5. Coulomb 마찰 적용: 정지 조건 (`speed < μ_s*g*dt && Gmag < μ_s`) 만족 시 velocity=0, 아니면 운동 마찰로 감속.
6. posOffset += velocity * dt.
7. AABB clamp + bounce: 각 축 독립 처리 (`|offset.x| > max` 시 `offset.x = sign*max; velocity.x *= -bounce`).
8. 최종 `boxTransform.localPosition = restPos + new Vector3(offset.x, 0, offset.y)`.

**Day 1 후반 — 회전 유지 + 동결 판정 시각화**
9. 회전 Lerp 코드는 그대로. 단 maxRotDeg는 살짝 낮춰 (5~6도) "트렁크 벽에 갇힌" 제한감 강조.
10. 디버그 Inspector 필드: 현재 velocity, posOffset, isResting bool 실시간 표시. `[SerializeField]` + `[Tooltip]`.

**Day 2 — 튜닝 루프**
11. 도현이 주행하며 다음 4개 숫자 조정:
    - `staticFriction` (0.3 ~ 0.6): 얼마나 강하게 G가 걸려야 박스가 움직이기 시작하는가.
    - `kineticFriction` (0.2 ~ 0.4): 움직이면 얼마나 빨리 감속하는가.
    - `maxOffset` (0.3 ~ 0.8m): 트렁크 내부 여유 공간.
    - `wallBounce` (0 ~ 0.4): 벽에 부딪혔을 때 튕김. 0이면 "쿵" 붙음, 높으면 "탱탱".
12. 성공 기준: (a) 직진 정속 주행 시 박스 완전 정지. (b) 급브레이크 시 앞으로 쭉 밀려 벽에 부딪혀 멈춤 — **rest로 돌아오지 않음**. (c) 급코너 후 직진 재개 시 박스는 벽 쪽에 "놓여 있음".

**Day 3 — 예외·폴리싱**
13. 차량 리스폰/순간이동 감지: `posOffset`과 `velocity`를 즉시 0으로 리셋. 안 하면 박스가 CargoSim 하늘 너머로 날아감.
14. `CargoBox.Repair()` 호출 시 위치도 초기화하는 연동.
15. (선택) 벽 충돌 시 작은 사운드/카메라 쉐이크 트리거 — 하지만 이건 4주차 폴리싱. 지금은 스킵.
16. post-verification: 90초 정속 + 90초 공격 주행 후 박스가 합리적 위치에 있는지 육안 + 로그 확인. CLAUDE.md "자동 배치 post-build verification" 패턴 준수.

---

## 핵심 3줄 요약

1. **현재 "갈대" 느낌의 범인은 `restPos` 복귀 Lerp**이다 — 스프링-댐퍼 수식 그 자체라 G가 꺼지면 박스가 중앙으로 탄성 복귀한다.
2. **패턴 B (Kinematic + Custom velocity + Coulomb 마찰 + AABB clamp)** 를 추천: CargoSim 격리 유지, 1~3일 작업, 튜닝 파라미터 4개, rest 복귀 구조적 제거.
3. **구현의 정수는 "velocity를 필드로 보존 + 정지 마찰 임계 + 벽에서 멈춤"** — 이 셋만 있으면 박스는 자연스럽게 "어딘가에 놓여 있는" 물체가 된다. 회전 Lerp는 그대로 둬도 된다.
