# 세션 로그 (Session Log)

> **이 파일은 세션간 인수인계 문서.**
> 매 세션 끝에 한 블록 추가. 새 세션은 이 파일의 마지막 블록부터 읽고 시작.
>
> 영상 기반(앤트로픽 패턴): 컨텍스트가 바닥나거나 조기 종료되는 것을 방지.

---

## 템플릿

```
## 세션 N — YYYY-MM-DD (HH:MM)
- **목표:** [오늘의 한 줄 목표]
- **한 일:**
  - [성과 1]
  - [성과 2]
- **다음 세션 할 일:**
  - [다음 작업 1]
  - [다음 작업 2]
- **관련 문서:** [경로 1, 경로 2]
- **메모:** [특이사항, 막힘, 결정, 배운 점]
- **컨텍스트 상태:** [~N% 사용 / 남은 여유 / 새 세션 필요 여부]
```

---

## 세션 1 — 2026-04-18

- **목표:** `notes/open-questions.md`의 Q1~Q10 미정 사항 해소.
- **한 일:**
  - Q1~Q10 전부 확정 (D-006 ~ D-015)
  - 스코프 개입 2회 (Q1 카메라 전환 → 단일 뷰, Q3 도시형 오픈 → 스테이지형)
  - `dev-plan/week1.md` 상단에 확정 전제 조건 섹션 추가
  - `notes/ideas-parking.md`에 콕핏 뷰 Post-MVP 항목 추가
- **다음 세션 할 일:**
  - 하네스 정비 (CLAUDE.md 슬림화, 세션 관리 파일 만들기, git init)
  - 또는 Day 1 직접 진입 (Unity 6 + Kenny 에셋 세팅)
- **관련 문서:** `notes/decisions-log.md` D-006~D-015, `notes/open-questions.md`, `dev-plan/week1.md`
- **메모:** 도현이 Q1(A↔C 전환)과 Q3(도시형 오픈)에서 스코프를 넓히려 함 → "시각적 풍성함"에 약한 패턴 의심. 향후 같은 유형 질문 나오면 선제 방어.
- **컨텍스트 상태:** 세션 종료, 새 세션 권장.

---

## 세션 2 — 2026-04-18 (저녁)

- **목표:** 하네스 엔지니어링 6단계 중 1~3단계를 PIT CREW에 적용.
- **한 일:**
  - 하네스 영상(6단계 로드맵) 자막 기반 감사 — CLAUDE.md 172줄 초과, 세션 인수인계 부재, 기능 목록 부재, git 미커밋 확인.
  - **CLAUDE.md 슬림화:** 172 → 77줄 (영상 권장 60~100 범위). 프로젝트 개요·개발 배경·영감 게임·폴더 구조·버전 히스토리는 GDD/overview.md로 이동 또는 삭제.
  - **세션 관리 파일 3종 신규:** `notes/session-log.md` (이 파일), `dev-plan/feature-list.md` (week1~4 기능 전체 체크박스), `notes/session-opener.md` (세션 오프닝/종료 템플릿).
  - **MCP 정리 가이드:** `notes/mcp-setup.md` 작성. PIT CREW 기간 꺼둘 MCP 목록(notion, blender, meshy-ai, Chrome, calendar 등) 명시.
  - **git 세팅 준비:** `.gitignore` (Unity 표준) + `notes/git-setup.md` 작성. 샌드박스에서 `.git` 권한 문제로 init 직접 불가 → 도현 터미널에서 실행하도록 명령어 세트 제공.
  - **F1/ Unity 프로젝트 발견:** 도현이 과제 동안 Unity 6 URP 템플릿으로 F1 프로젝트 생성. Day 1 첫 번째 항목 완료. CLAUDE.md에 F1/ 경로 반영, feature-list/week1 체크박스 갱신.
- **다음 세션 할 일:**
  - **도현이 터미널에서 git 가이드 실행** (`notes/git-setup.md` 0~4단계).
  - Day 1 잔여: Kenny Car Kit + Road Tiles 임포트, 테스트 차량 배치.
  - 또는 Day 2 `ArcadeCarController.cs` 초안 작성 (Cowork가 선제 지원 가능).
- **관련 문서:** `CLAUDE.md`, `dev-plan/feature-list.md`, `notes/mcp-setup.md`, `notes/git-setup.md`, `notes/session-opener.md`, `GDD/overview.md`
- **메모:**
  - 하네스 영상 1~3단계 핵심: 지도만 주기 / 도구 최소화 / 인수인계 문서. 이 세 가지가 적용됨.
  - 4~6단계(검증 파이프라인, 멀티에이전트, 유지보수)는 MVP 완성 후 도입.
  - 샌드박스 제약: 마운트 폴더의 `.git` 직접 조작 불가. Cowork에서 git은 가이드만 제공하고 실행은 도현 터미널.
- **컨텍스트 상태:** 세션 종료. 다음 세션은 새로 시작 권장.

---

## 세션 3 — 2026-04-19

- **목표:** Day 2 차량 물리 MVP 진입 — "race.fbx가 키보드 입력으로 평면 위를 움직이게 만든다."
- **한 일:**
  - 상태 감사: Kenny Car Kit + city-kit-roads는 이미 임포트됨 (Day 1 체크리스트 4/5 사실상 완료). 스코프 밖 에셋 4종(commercial, industrial, suburban, train-kit)도 함께 임포트된 것 확인.
  - **D-018 결정:** 스코프 밖 에셋 4종 유지 (도현 명시 선택). 전제: 1주차 씬/프리팹 참조 금지. `notes/decisions-log.md`에 기록.
  - **ArcadeCarController.cs 작성:** `F1/Assets/Scripts/ArcadeCarController.cs`. Rigidbody 기반, InputSystem Move 액션 재활용, 최고속 60km/h, 0→60 4.5s, 사이드 그립 감쇠, 속도비례 조향 AnimationCurve. WheelCollider 미사용(D-007 1주차 전제).
  - `dev-plan/feature-list.md`, `dev-plan/week1.md` Day 2 체크박스 업데이트.
- **다음 세션 할 일:**
  - 도현: Unity 에디터에서 `race.fbx`를 `Race.prefab`으로 만들고 Rigidbody + BoxCollider + ArcadeCarController 부착, PlayerInput 연결(기본 맵 `Player`), SampleScene에 Plane + Race 배치 후 Play 테스트.
  - Play 테스트에서 튜닝 피드백 수집 → 필요 시 인스펙터 수치 조정(accelTime, turnResponseBySpeed 커브, sideGripDamping).
  - OK면 Day 3 진입: `FollowCamera.cs` + Road Tiles 테스트 트랙.
- **관련 문서/코드:** `F1/Assets/Scripts/ArcadeCarController.cs`, `notes/decisions-log.md` D-018, `dev-plan/week1.md` Day 2.
- **메모:**
  - Unity 6 API 사용: `rb.linearVelocity` (구 `velocity` deprecated).
  - `centerOfMass`를 낮게 설정(y=-0.3)해서 뒤집힘 감소. 실제 차체 크기에 맞게 도현이 튜닝 필요.
  - 도현 패턴 재확인: 선행 에셋 임포트에서 스코프 밖 4종까지 쓸어담음. "시각적 풍성함" 유혹 관찰됨. D-018 전제를 방어선으로 삼아야 함.
- **컨텍스트 상태:** 세션 진행 중. Day 2 Play 테스트 결과 받아서 같은 세션 내에서 튜닝 가능.

### 세션 3 이어서 — Day 2 디버깅 (22:00~22:20)
- **증상:** Play 후 W키 눌러도 차가 거의 안 움직임(미세한 떨림만).
- **진단 과정 (멀티 에이전트 활용):**
  - 유니티 관리팀 + 코딩팀 서브에이전트 병렬 분석 → "isGrounded 의심" 가설 제시.
  - `isGrounded = true` 강제 후 재테스트, 디버그 로그 추가(`OnMove`, `Throttle`).
  - **확정 원인:** OnMove 입력 정상 수신, 370N 추력 정상 가해짐. 그러나 `forwardSpeed ≈ -0.0001`로 속도가 안 붙음 → **정지 마찰(μ=0.6)이 추력(370N)보다 큼**(588N). 마찰에 묶여 있었음.
- **수정:**
  - `Assets/GroundPhysics.physicsMaterial` 생성, friction 0.15로 설정, Ground MeshCollider에 할당 (Editor 스크립트 `CreateGroundPhysicsMaterial.cs`).
  - `ArcadeCarController.accelTime`: 4.5s → 2.5s (아케이드 반응성).
  - `Rigidbody.mass`: 1200 → 100 (아케이드 튜닝 용이성).
  - 디버그 로그 제거, `isGrounded = true` 하드코딩 유지(1주차 평지 전제). 2주차 이후 Ground 레이어 분리로 제대로 복원.
- **방향성 결정:** Velocity 직접 제어(A) vs 물리 기반(B) 갈림길에서 **B(물리 기반) 선택**. 이유: 5주차 드리프트 기획 + GDD "무게감" 요구 → 물리 시뮬 필수.
- **다음 세션 할 일:**
  - Play 테스트 재확인(차가 60km/h 근처까지 제대로 가속되는지, 조향 반응).
  - 튜닝 피드백 수집 후 Day 3 진입: `FollowCamera.cs` + Road Tiles 테스트 트랙.
  - Ground 레이어 분리해서 `isGrounded` 물리 체크 복원(Day 3~4 중).
- **메모:**
  - **팀 구조 아이디어:** 기획/코딩/유니티관리/피드백 4팀 구성 논의. 오늘은 유니티+코딩 2팀 병렬 디버깅으로 원인 빠르게 추적. 반복 튜닝 구간에서 유효함 확인.
  - 서브에이전트가 MCP 도구(play_game, get_unity_logs 등)에 접근 가능함을 확인.
- **컨텍스트 상태:** 세션 종료.

---

## 세션 4 — 2026-04-20

- **목표:** Day 3 마무리 + Day 5 드리프트 + Day 4 튜닝/사운드 진입.
- **한 일:**
  - **Day 3 트랙 배치 완료:** `BuildTestTrack.cs` 커브 회전/직선 방향 디버깅 (rot 0, 출구 -X 방향). 직선 8 + 커브 1 + 직선 4 연결 확인.
  - **Day 5 드리프트 구현 (멀티 에이전트 병렬):**
    - 코딩팀: `ArcadeCarController`에 `IsDrifting` 프로퍼티, 드리프트 중 사이드 그립 15%/조향 1.35×/가속 0.4× + 종방향 드래그(`driftLongitudinalDrag`) + 최고속 제한(`driftMaxSpeedKmh=42`) 추가. Handbrake 액션(Space / Gamepad B) 추가. OnHandbrake 메시지 방식이 release 못 잡아서 `InputAction.IsPressed()` 폴링으로 전환.
    - 유니티관리팀: `DriftVfxController`, `Editor/SetupDriftVfx.cs`, Race 아래 SkidMark_L/R (TrailRenderer) + Smoke_L/R (ParticleSystem) 4개 자식 + 컴포넌트 연결.
    - 파티클 보라색 이슈 → URP/Particles/Unlit 머티리얼(`DriftSmoke.mat`, `DriftSkid.mat`) 생성 (`Editor/FixDriftVfxMaterial.cs`). playOnAwake=true + emission.enabled 토글로 정상 작동.
  - **D-019 (F1 리얼리즘 요청 재확인):** 도현 "F1처럼" 요청 → D-007 아케이드 방침 유지. Tier A(수치 튜닝)만 수용, Tier B(기어/다운포스/ERS/슬립스트림/서스펜션) 파킹, Tier C(풀 시뮬) 기각. 플랜 `C:\Users\stard\.claude\plans\happy-puzzling-raccoon.md`.
  - **Tier A 튜닝 적용:** `ArcadeCarController`에 `airDrag=0.35`, `brakeStrengthBySpeed` 커브(저속 1.0/고속 0.6), `angularDamping=2.5` 추가. `turnResponseBySpeed` 커브 끝값 0.35→0.2 (코드 기본값만, 기존 직렬화는 에디터 Reset 필요). `brakeDecel` 12→6.
  - **FollowCamera 튜닝:** maxFov 75→80, 코너 롤 ±5° (`SteerInput` 프로퍼티 읽음, 속도 20% 이상에서만).
  - **D-020 (엔진 사운드 외부 예외):** Kenny에 엔진 음원 없음 확인 → 차량 엔진 루프 1개 한정 외부 허용. Sonniss GDC 유료화 + Coplay `generate_sfx` 401(Coplay 계정 미로그인) 확인 후 → 도현이 AI 서비스로 직접 생성한 `EngineLoop.mp3` 도입.
  - **EngineAudio 시스템:** `Assets/Scripts/EngineAudio.cs` 작성 (속도비례 pitch 0.7→2.1, volume 0.35→0.9). Race에 AudioSource+EngineAudio 추가. 클립 연결 (`Assets/Sound/EngineLoop.mp3`). `LICENSE.txt` 증빙.
- **다음 세션 할 일:**
  - Play 테스트: Tier A 튜닝 + 드리프트 + 엔진 사운드 전체 체감. 수치 조정 필요 시 Inspector에서.
  - Day 6 (트랙 확장 + 30분 재미 테스트) 또는 Day 4 남은 튜닝 반복 판단.
  - Race 인스턴스의 `turnResponseBySpeed` 커브 Inspector에서 Reset (코드 기본값 0.2 반영).
- **관련 문서/코드:**
  - `F1/Assets/Scripts/ArcadeCarController.cs`, `FollowCamera.cs`, `EngineAudio.cs`, `DriftVfxController.cs`
  - `F1/Assets/Editor/BuildTestTrack.cs`, `SetupDriftVfx.cs`, `FixDriftVfxMaterial.cs`, `RefreshAndLinkEngineClip.cs`
  - `F1/Assets/Sound/EngineLoop.mp3`, `LICENSE.txt`
  - `notes/decisions-log.md` D-019, D-020
  - 플랜: `C:\Users\stard\.claude\plans\happy-puzzling-raccoon.md`
- **메모:**
  - 도현 "F1 리얼리즘" 요청이 세션 내 2회 반복됨 (시뮬 → "살짝 섞기"). 패턴 3번째 (세션 1 Q1 카메라, 세션 3 에셋 쓸어담기에 이어). D-019 방어선 작동 확인.
  - 집중력 저하 신호 한 번 (엔진 사운드 탐색 중). 세션 종료 유도 + AI 생성 루트로 간단화.
  - 코플레이 generate_sfx는 계정+크레딧 필요 — 앞으로 "쉽게 쉽게" 옵션 제시할 때 유료 가능성 미리 점검.
- **컨텍스트 상태:** 세션 종료.

---

## 세션 5 — 2026-04-22

- **목표:** 속도감 튜닝 + 맵 구성 (Day 6 본작업).
- **한 일:**
  - **속도감 튜닝 (Tier A 연장):**
    - FollowCamera `baseFov` 60→55, `maxFov` 80→90 (FOV 델타 확대로 가속 체감↑).
    - ArcadeCarController `maxSpeedKmh` 60→90, `accelTime` 2.5→3.2 (실체감 향상).
    - 도현 "기어 시스템" 재제안 → D-019 방어선 4번째 작동, `ideas-parking.md`에 재검토 조건(Day 7 판정 통과 후 + 구체 피드백) 명시하고 파킹.
  - **Coplay MCP 도입:** 세션 초반 "도구 없이 맵 짜기 불가" 판단 → Coplay MCP로 `capture_scene_object`, `place_asset_in_scene`, `get_game_object_info`, `execute_script` 활용. 씬 스크린샷 보고 판단하는 루프 확립.
  - **맵 구성 (Day 6):** `BuildTestTrack.cs` 확장 — U-턴 레이아웃 (직선 10 + 커브 + 직선 8 + 커브 + 직선 10), `DecorateTrack.cs` 신규 (저지벽 양측 + 갠트리 표지판 + 출발선 콘).
  - **Kenny 타일 방향 디버깅 (대규모):** 여러 차례 시행착오 끝에 `road-straight.fbx`의 **자연 도로 축이 local +X**임을 확인 (내내 local +Z로 잘못 가정). 이로 인한 버그 3건 모두 수정:
    1. 커브 커넥터 방향 오인 (세션 3 로그 "rot 0 출구 -X" 오답 확인) → 커브 1 rot=180, 커브 2 rot=90.
    2. 직선 타일이 도로 축과 스택 방향 수직이라 타일 이음매마다 크로스워크 띠 발생 → 스타트 rot=90, 중간 rot=0, 복귀 rot=90으로 수정.
    3. 저지벽 긴 축(local Z)이 도로 축과 수직이라 도로 가로지르게 배치 → 방벽 회전 `tileRot * Euler(0,90,0)` 추가.
  - DecorateTrack 좌표계 정리: `sideDir = tile.forward` (도로 수직), `alongDir = tile.right` (도로 축).
  - **D-021 (도로 에셋 교체 파킹):** Kenny city-kit-roads가 도시 교차로용이라 타일마다 크로스워크 mesh 박혀있음이 구조적 한계. 현재 rot로 "옆면" 처리해 문제 없지만, 4주차 폴리싱에서 Kenny racing-kit 또는 커스텀 Plane 재검토. `ideas-parking.md`에 기록.
- **다음 세션 할 일:**
  - **도현: Play 테스트 전 인스펙터 Reset 2곳:**
    - Race → ArcadeCarController (maxSpeed 90, accelTime 3.2 반영)
    - Main Camera → FollowCamera (FOV 55/90 반영)
  - Play 테스트 — Tier A 튜닝 + 새 맵 전체 체감.
  - 느낌 OK면 Day 6 나머지 (30분 플레이 재미 판정) + Day 7 (1주차 체크포인트).
  - 커밋 (현재 미커밋 상태): BuildTestTrack, DecorateTrack, ArcadeCarController, FollowCamera, ideas-parking, session-log.
- **관련 문서/코드:**
  - `F1/Assets/Editor/BuildTestTrack.cs`, `DecorateTrack.cs`
  - `F1/Assets/Scripts/ArcadeCarController.cs`, `FollowCamera.cs`
  - `notes/ideas-parking.md` (기어 시스템 재검토 조건 + D-021 도로 에셋 교체)
- **메모:**
  - **Coplay MCP가 맵 작업의 판세를 완전히 바꿈.** 에셋 이름만 보고 추측하던 루프 → 스크린샷 보고 확인하는 루프. 이전 세션들은 Coplay 붙어있어도 잘 안 썼는데, 앞으로 비주얼/씬 작업은 무조건 Coplay 선행.
  - **도현 패턴 5번째 관찰:** "기어 시스템" 재제안. D-019 방어선 작동. ideas-parking에 재검토 조건 명시해서 다음 반복에 방어 근거 축적.
  - **세션 3 로그 오류 확인:** "rot 0 출구 -X 방향" 단언이 틀렸음 (실제 rot=0 커넥터는 +Z와 +X). 원인은 시각적 착시 — 도로 surface가 인접 타일과 겹쳐 보여서 "연결된 것처럼" 인식. 앞으로 에셋 방향 판단은 반드시 Coplay 캡처로 검증.
  - **도현의 집요함이 이번엔 플러스로 작용.** "도로 아직 이상함" 3~4번 반복해서 강조 → 결국 근본 버그(local +X 가정 오류) 발견. 보통 스코프 방어해야 할 타이밍이지만, 이번 건은 진짜 버그였음. 사용자 피드백 신중히 듣기.
- **컨텍스트 상태:** 세션 종료. 다음 세션은 Play 테스트부터 시작.

---

## 세션 6 — 2026-04-22 (저녁)

- **목표:** 드리프트/주행 튜닝 → 맵 + 배달 시스템 도입 → 도로 정확성 확보.
- **한 일:**
  - **드리프트 구조적 버그 수정:** `ApplySteering`이 `MoveRotation`으로 차 방향을 매 프레임 강제 회전 → 사이드슬립 자체가 성립 안 함 = "브레이크 느낌"의 근본 원인. 드리프트 중에는 `AddTorque`로 회전(차체와 속도 벡터 분리), 진입 순간 yaw impulse, `angularDamping`을 0.3으로 낮춤. 그립 0.02, longDrag 0, throttleScale 0.9, maxSpeed 90 (속도 제한 실질 해제).
  - **주행 반응성 튜닝:** `accelTime` 3.2→2.8, `turnResponseBySpeed` 끝 0.2→0.35, `angularDamping` 2.5→1.5. 도현 피드백: "주행 자체 재미 없음" → 맵 + 목적 추가 후 재검증 결정.
  - **TileMapAuto 시스템 (D-022):** 셀별 수동 회전(TileMapTrack) 방식 폐기. 그리드 마스크 5줄 + 인접 셀 자동 추론으로 직사각형 루프 트랙 자동 생성. 직선/코너 회전 매핑 메타데이터를 `RotationFor` dict에 보관.
  - **road-curve native size 발견 (D-021 정정):** 도로 폭 어긋남 + 흰 띠가 crosswalk 마킹 한계가 아니라 **road-curve.fbx의 native size가 2×2 (직선은 1×1)**여서 동일 TileScale로 깔면 코너만 8×8 유닛으로 깔리는 mesh 크기 불일치였음. `MeasureTiles.cs`로 측정 → `NativeSize` dict로 정규화 → 도로 폭 정확히 일치. D-021 (도로 에셋 교체) 파킹 해제, city-kit-roads 유지.
  - **에셋 도입 워크플로우 (D-023):** 신규 키트 첫 단계로 `MeasureTiles` 측정 + `RotationFor`/`NativeSize` 등록 의무화.
  - **배달 시스템 MVP:** `DeliveryPoint` (Trigger zone), `DeliveryManager` (순서 추적, 시작→1→2→3→복귀), `DeliveryHUD` (상단 N/3 + 화면 가장자리 화살표), `SetupDeliverySystem.cs` (한 메뉴로 자동 배치 + 차량 출발 셀 정렬).
  - **검증 도구 정리 (`_Recovery/`):** `RebuildAndCleanup.cs`, `TopDownView.cs`, `TopDownCloseup.cs`, `MeasureTiles.cs`. **시각 검증은 무조건 톱다운 정사영** (isometric 두 번 오판함).
- **다음 세션 할 일:**
  - **도현: Race → ArcadeCarController Reset** (드리프트 새 필드 반영 — 안 하면 새 동작 안 함). Main Camera FollowCamera도 함께.
  - **Play 테스트** — 드리프트 미끄러짐 + 배달 루프(0/3 → RETURN → COMPLETE) + 새 주행 튜닝 체감.
  - 결과 OK면 **Building 시스템 (Phase 1)**: ScriptableObject 외부화 + `BuildingDef` + `BuildingPlacer` (suburban + commercial 키트 1×1 footprint, 인접 도로 보고 entry 자동 회전).
  - 흰 띠 폴리싱은 4주차로 미룸.
- **관련 문서/코드:**
  - `F1/Assets/Scripts/ArcadeCarController.cs` (드리프트 재설계)
  - `F1/Assets/Editor/TileMapAuto.cs` (메타데이터 시스템 핵심)
  - `F1/Assets/Editor/SetupDeliverySystem.cs`
  - `F1/Assets/Scripts/Delivery*.cs`
  - `F1/Assets/Editor/_Recovery/*.cs` (디버그/검증 도구)
  - `notes/decisions-log.md` D-021 (정정), D-022, D-023
- **메모:**
  - **시행착오 6번 분석:** 코너 회전 매핑을 머리로 추론하다 6번 틀림. 원인은 (1) Unity Y euler CW vs CCW 헷갈림 (2) base 출구 방향 가정이 틀림 (3) isometric 캡처에서 시각 착시. **해결: 톱다운 정사영 + 메타데이터 한 번 검증 후 영구 사용**. 도로 흰 띠 진단도 추측(crosswalk)에서 측정(MeasureTiles)으로 옮겼더니 진짜 원인(native size 2x2) 발견.
  - **도현 패턴 6번째 — "건물도 메타데이터 시스템으로"** 요청. 스코프 부풀림 신호 의심했으나 도현이 "MVP 구성에 필요"라 명시 + 합리적 근거(앞으로 어차피 써야) → 진행 결정. Phase 1만 (footprint 1×1, suburban+commercial 2개 키트). Phase 2(가변 footprint)는 다음.
  - **Coplay MCP가 다시 결정적 역할:** `capture_scene_object` 톱다운 + `execute_script` mesh 측정 + `check_compile_errors`. 이게 없었으면 today's 6번 시행착오가 60번 됐을 것.
- **컨텍스트 상태:** 세션 종료. 다음 세션은 Play 테스트 + Building 시스템 Phase 1.

---

## 세션 7 — 2026-04-22

- **목표:** Building Phase 1 + 복잡한 트랙 제작 + 도로 연결성·재발 방지.
- **한 일:**
  - **도로 연결성 근본 수정 (D-024):** `road-curve.fbx`(native 2×2, 좁은 원호) → `road-bend.fbx`(native 1×1, 직선 폭 일치)로 교체. 세션 6 D-021 "native size 정규화로 해결" 결론이 반쪽 진단이었음 — 외곽 bounds만 맞추고 아스팔트 stripe 자체 폭은 맞추지 못해 curve-straight 이음매마다 도로 폭이 절반으로 줄어드는 단차 존재. `road-bend`는 `road-straight`와 같은 스타일 한 쌍.
  - **Layout SO 외부화 + 건물 자동 배치 Phase 1 (D-025):**
    - `BuildingDef` / `BuildingCatalog` / `TrackLayout` ScriptableObject 추가 (`F1/Assets/Scripts/Layout/*.cs`).
    - `BuildingPlacer`(에디터): 2-layer 마스크(road + building), 인접 도로 우선순위 기반 회전, (c,r) 해시 결정론적 프리팹 선택, FootprintMargin 0.8.
    - `SetupLayoutAssets`: Kenny suburban 18 + commercial 10 1×1 후보를 측정값 기반으로 `BuildingDef.asset` 28개 + `BuildingCatalog.asset` + `DefaultTrackLayout.asset` 일괄 생성.
    - `BuildFromLayoutMenu`: 한 메뉴로 도로+건물 재빌드 + SceneView 톱다운 자동 프레이밍.
  - **복잡 씬 (`ComplexTrack.unity`):** 9×7 더블 루프(가운데 가로 through road + T교차로 2). `SetupComplexScene` 메뉴 한 방에 레이아웃 SO + 씬 + 카메라/라이트/ground 세팅. 35 타일 + 22 건물.
  - **건물 땅 속 박힘 버그 진단 및 수정 (D-026):** `Quaternion.FromToRotation(+Z, -Z)`가 Y축 180이 아닌 X축 180을 리턴해 건물 절반이 뒤집혀 땅 아래 박힘. 톱다운에선 정상처럼 보여 발견 못함. 사이드 캡처로 즉시 발견. `atan2(x,z)`로 Y-yaw만 직접 계산하도록 교체.
  - **재발 방지 시스템 (D-026):** `BuildingPlacer.VerifyPlacements()` — euler X/Z 1° 초과 시 "flipped/tilted", bounds.min.y < −0.05 시 "sunk" 경고. 빌드 로그에 `"all verified"` 또는 `"N FAILED"` 명시.
  - **도로 연결성 컨텍스트 개선:** `TileMapAuto.AdjustStraightRotation`. 각 직선 타일의 인접 이웃이 straight인지 검사 후, 비(非)straight(커브/T) 쪽으로 크로스워크 edge가 오지 않도록 rot 0↔180 / 90↔270 플립. T-교차로 주변 단절 느낌 제거.
  - **CLAUDE.md 업데이트:** "에셋/맵 자동 배치 작업 규칙" 섹션 추가 — side-by-side 렌더 의무 + Y-yaw 직접 계산 + post-build verification + Layout SO 사용 규칙.
  - **차량 배치:** SampleScene의 Race 인스턴스를 `Assets/Prefabs/Race.prefab`으로 만들고(`SetupRaceInScene`), ComplexTrack의 startCell(1,0)에 배치 + Main Camera에 FollowCamera 연결. startYawDeg 90으로 교정해 도로 방향 정렬.
  - **후반 버그 수정 2건:**
    1. **FollowCamera target/car None** — `private [SerializeField]` 필드를 `GetField()` 기본 호출로 접근해 리플렉션 실패(에러 없이 무시). `SerializedObject + FindProperty` 방식으로 교체. target=Race, car=Race 할당 확인.
    2. **Ground 물리 머티리얼 로드 실패** — 세션 3에서 생성한 `Assets/GroundPhysics.physicsMaterial`이 Unity 6에서 `DefaultAsset` 타입으로만 인식돼 `PhysicsMaterial`로 로드 안됨(확장자 호환 이슈 추정). `Assets/GroundPhysicsMat.physicMaterial`로 신규 생성(friction 0.15) 후 SetupRaceInScene/SetupComplexScene 경로 업데이트. ComplexTrack Ground에 할당 완료. SampleScene은 영향 없을 가능성 높지만 필요 시 도현이 수동 할당.
- **다음 세션 할 일:**
  - **ComplexTrack 씬 열고 Play 테스트 첫 스텝.** 기대 동작: WASD로 차 이동, 카메라가 따라옴, 배달 HUD 표시. 안 움직이면 콘솔 확인 → 예상 원인들:
    - Race 인스턴스에 최신 드리프트/Tier A 필드 미반영 → Inspector에서 ArcadeCarController Reset
    - Ground collider에 물리 머티리얼 미할당 → GroundPhysicsMat 수동 드래그
    - PlayerInput actions 연결 끊김 → InputSystem_Actions.inputactions 재할당
  - OK면 더블 루프 + T교차로 + 배달 루프(4 포인트) 주행 체감 + 30분 플레이.
  - 필요 시 DefaultTrackLayout(SampleScene)도 `PitCrew/Build Everything From Layout`으로 새 시스템 재빌드. SampleScene의 Race 인스턴스는 이제 Race.prefab 인스턴스로 연결됨 — 수정하면 프리팹까지 영향.
  - OK면 Building Phase 2(가변 footprint 2×1 등) 또는 Day 7 1주차 체크포인트.
  - **커밋 필요(미커밋, 큰 덩어리):**
    - `F1/Assets/Scripts/Layout/*.cs`
    - `F1/Assets/Editor/{BuildingPlacer, BuildFromLayout, SetupLayoutAssets, SetupComplexScene, SetupRaceInScene, TileMapAuto}.cs`
    - `F1/Assets/Editor/_Recovery/*` (디버그/셋업 스크립트 다수)
    - `F1/Assets/Data/*` (28 BuildingDefs + BuildingCatalog + 2 TrackLayouts)
    - `F1/Assets/Prefabs/Race.prefab`
    - `F1/Assets/ComplexTrack.unity`
    - `F1/Assets/GroundPhysicsMat.physicMaterial` (신규)
    - `F1/Assets/SampleScene.unity` (Race 프리팹 연결 변경)
    - `CLAUDE.md`, `notes/decisions-log.md`, `notes/session-log.md`, `dev-plan/feature-list.md`
    - 구 `F1/Assets/GroundPhysics.physicsMaterial`은 유지 (참조 없지만 제거는 도현 판단).
- **관련 문서/코드:**
  - `F1/Assets/Scripts/Layout/BuildingDef.cs`, `BuildingCatalog.cs`, `TrackLayout.cs`
  - `F1/Assets/Editor/BuildingPlacer.cs`, `BuildFromLayout.cs`, `SetupLayoutAssets.cs`, `SetupComplexScene.cs`, `SetupRaceInScene.cs`, `TileMapAuto.cs`
  - `F1/Assets/Data/*` (28 BuildingDefs + 2 Layouts + Catalog), `Prefabs/Race.prefab`, `ComplexTrack.unity`
  - `notes/decisions-log.md` D-024/025/026, `CLAUDE.md`
- **메모:**
  - **D-021 반성:** 세션 6에서 "측정만 하면 안전"이라 믿었지만, 측정 대상이 잘못됐음(외곽 bounds ≠ 아스팔트 폭). 측정은 "무엇을 측정하는지" 명확히 해야. 교훈을 D-024/D-023 보정에 기록.
  - **도현 패턴 7번째 관찰:** "차 배치해서 다음 세션 이어가자"로 자연스러운 세션 정리 요청. 전 세션들에서의 "컨텍스트 남기기" 습관 축적 확인. 긍정적 변화.
  - **Coplay MCP 필수성 재확인:** `capture_scene_object`(톱다운+사이드) + `execute_script`(스크립트 주입) + `get_unity_logs`. 사이드 캡처 없었으면 건물 땅속 박힘 계속 몰랐을 것.
  - **auto mode와 non-auto 전환:** 복잡한 판단(에셋 교체 결정 등)은 non-auto에서 질문. 반복 반영/빌드/검증은 auto에서 빠르게. 이번 세션 둘 다 효과적으로 전환.
- **컨텍스트 상태:** 세션 종료. 다음 세션은 Play 테스트부터.

---

## 세션 8 — 2026-04-22

- **목표:** 에셋 비율 정상화 + 도시 볼륨감 확보. 플레이 테스트로 1주차 체크포인트 접근.
- **한 일:**
  - **스케일 리밸런스 (D-027):** CellSize/TileScale 4m→6m, Race 0.7x, BuildingPlacer 고정 4x → fit-to-cell(margin 0.9). 도로 침범 해소.
  - **멀티셀 건물 인프라 (D-028):** BuildingDef cellsWide/cellsDeep, BuildingCatalog K/X 2×2 풀, 점유 그리드 기반 충돌 검증, MeshCollider 자동 부착(차 통과 버그 해결).
  - **맵 확장 V1 → V2:** 9×7 단일 크로스 → **13×9 3×4 그리드** (가로 3 + 세로 4 도로, 6블록 3×3). 타일 35→63, 건물 22→36.
  - **드리프트 튜닝 2회:** gripMultiplier 0.02→0.3, yawBoost 1.5→0.8, yawTorque 15→8, sideGripDamping 6→9. "실제 차 느낌"보다 "유저가 좋다 느끼는 톤" 목표로 재조정 예정.
  - **MeasureBuildings** 메뉴 항목 추가 + 스카이스크레이퍼 5종 포함. Coplay MCP(`get_unity_logs`)로 콘솔 로그 바로 수신.
  - **RunSetupAndRebuild** 러너(Assets/Editor/_Recovery) 생성 — MCP execute_script로 씬 재생성 자동화.
- **도현 피드백 (세션 중 라이브):**
  - "주행 자체 재미는 기본 레이싱 수준" → 폴리시 필요
  - 도시 느낌 OK, 맵은 더 넓고 복잡해야 함 (반영 완료 일부)
  - 드리프트 "실제 차 느낌 아님" → 중요 X, "유저가 느꼈을 때 좋으면 됨"으로 정정
  - 맵 조금 더 복잡하게 하는 방향 선호 (세션 9에서 논의)
- **다음 세션 할 일:**
  - **세션 9 오프닝: 기획 재검토 대화** (도현 요청). 1주차 체크포인트("차 몰기 자체가 재밌는가") 판정 + 향후 방향.
  - 논의 포인트: 아케이드 드리프트 톤 설정(레퍼런스 게임), 맵 복잡도 상한, 2주차(화물) 진입 타이밍, 비주얼 폴리시 순위.
  - 판정 YES면 2주차 화물 시스템, 그럭저럭이면 Day 4~5 튜닝 재방문, NO면 스코프 재조정.
- **관련 문서/코드:**
  - `F1/Assets/Scripts/Layout/BuildingDef.cs` (cellsWide/cellsDeep 추가)
  - `F1/Assets/Editor/{BuildingPlacer, TileMapAuto, SetupLayoutAssets, SetupComplexScene, SetupRaceInScene, _Recovery/RunSetupAndRebuild, _Recovery/MeasureBuildings}.cs`
  - `F1/Assets/Prefabs/Race.prefab` (드리프트 파라미터 + scale 0.7)
  - `F1/Assets/ComplexTrack.unity` (13×9 그리드 재생성)
  - `F1/Assets/Data/Buildings/*` (2×2 K/X 풀 7종 신규)
  - `notes/decisions-log.md` D-027, D-028
- **메모:**
  - **도현 피드백 패턴:** "실제 차 느낌 안남"을 처음에 중요한 문제로 제시 → 추가 튜닝 후 "사실 실제 차는 중요하지 않음"으로 정정. **과제: 피드백 초기에 "이게 중요해서 하는 말인가?" 한번 더 물어봐서 헛튜닝 방지.**
  - **스케일 의사결정 오갔음:** fit-to-cell(flatten) → uniform 4x(varied but overflow) → fit-to-cell with multi-cell(varied within category via cells). 정답 찾는 데 3번 왕복. 교훈: 2축(변이 vs 피팅) 트레이드오프는 사전 선언 후 결정 요청.
  - **Coplay MCP 효과성:** execute_script + get_unity_logs로 씬 재생성 루프가 세션 내 5회 이상 반복됨. 수작업이면 세션 시간 2배 필요했을 것.
- **컨텍스트 상태:** 세션 종료 유도(도현 요청: 세이브 후 기획 대화). 새 세션 권장.

---

## 세션 9 — 2026-04-22 (기획 재검토)

- **목표:** 1주차 체크포인트 판정 + 2주차 시스템 기획 정립.
- **한 일:**
  - **1주차 판정 합의:** "그럭저럭" — 코어 돌아감, 주행감 단독 튜닝은 한계 → 2주차 진입 결정.
  - **2주차 시스템 5개 상세 기획:** 기어(자동/수동 토글), 타이어 마모, 부스터 ↔ 마모 트레이드오프, 연료, 화물 Shock. HUD 4종 게이지 합의.
  - **기어 수동 제안 3번째 방어 + 절충:** 도현 "경영 감각" 프레이밍으로 재등장. 본인 "스트레스 회피" 원칙과 충돌 지적 → 자동 기본 + 수동 Post-MVP 토글로 절충.
  - **수치 근거 부족 지적 (도현):** NotebookLM 같은 도구로 사전조사하며 기획 재정립 제안 → plan mode 진입.
  - **연구 기반 재정립 플랜 확정:** `C:\Users\stard\.claude\plans\whimsical-juggling-kernighan.md`. 도현 NotebookLM + Claude WebSearch 병렬. 레퍼런스 Burnout Paradise / F1 22/23 / Forza Horizon 5.
  - **Claude 1차 웹조사 완료:** `GDD/research/claude-web-findings.md` — 3개 토픽 수치 표 + 출처 URL + Key Takeaways + 미해결 U1~U5.
  - **D-029 기록.**
- **주요 조사 인사이트:**
  - 부스트 % 비중은 업계 비공개 관행 (Criterion/Codemasters) → 감각 튜닝
  - F1 Soft-Hard 실제 델타 ~1.5s/랩. 5~15분 런은 **~8%(1.5~2초)로 과장** 필요
  - Codemasters도 실제 50% 축약 (Soft 10~15랩 → 게임 5~8랩). PIT CREW는 더 공격적 축약
  - F1 무게 페널티 10kg당 0.3~0.4s/랩 = 실수치. 화물 무게 시스템 근거 확보
  - FH5 기본값 Auto + Driving Line + Assisted Brake → 배달 게임 기본 세팅 근거
- **다음 세션(세션 10) 할 일:**
  - **도현:** NotebookLM 노트북 3개(Burnout 우선) 생성 → 요약본 export → `GDD/research/notebooklm-summaries/{burnout,forza,f1}.md`
  - **오프닝 커밋 정리:** 세션 7~8 덩어리(Layout SO, BuildingPlacer, ComplexTrack 등) + 세션 9 기획/연구 셋업 → **2개 커밋 분리**
  - NotebookLM 요약 + Claude 웹조사 교차검증 → GDD 6개 문서 일괄 업데이트 (tires/fuel/cargo/core-loop + gears/boost 신설)
  - 미해결 U1~U5 중 NotebookLM이 커버한 항목 확인, 남은 건 `open-questions.md` Q11~로 이관
- **세션 11 예정:** 검증된 수치 기반으로 2주차 구현 플랜 재작성 (plan mode 진입).
- **관련 문서:**
  - `C:\Users\stard\.claude\plans\whimsical-juggling-kernighan.md`
  - `GDD/research/claude-web-findings.md`
  - `notes/decisions-log.md` D-029
- **메모:**
  - **도현 패턴 7번째:** 기어 수동 제안 3번째. 프레이밍만 바뀜 (F1 리얼리즘 → 조작 깊이 → 경영 감각). D-029에 기록 → 연구 결과가 "아케이드=자동 기본"이면 `ideas-parking.md`에 재파킹 강화 가능.
  - **연구 페이즈 리스크:** 끝없이 파다 2주차 진입 늦어질 가능성. 타임박스 2세션 엄격 준수. 세션 11 진입 시 "연구 끝" 공동 선언 필요.
  - **연구 접근 자체는 좋은 변화.** 이전 세션까지는 수치가 "감"이었고, 이번에 처음으로 "출처"가 생김. 앞으로 수치 결정할 때 기본 루틴으로 자리잡으면 완성도 상승.
  - **미커밋 누적 커짐.** 세션 7~9 3세션 분량. 세션 10 오프닝 첫 작업으로 분리 커밋 필수.
- **컨텍스트 상태:** 세션 종료. 다음 세션은 커밋 정리 + 도현 NotebookLM 요약본 공유부터.
