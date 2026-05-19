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

---

## 세션 10 — 2026-04-22 ~ 2026-04-23

- **목표:** 세션 7~9 커밋 정리 + 2주차 시스템 본격 구현 (연료/기어/주문보드/부스트 등) + 차별점 명확화.
- **한 일:**
  - **세션 7~9 커밋 정리 (4 커밋):**
    - `366ed09` docs(session-9) — 세션 9 기획·연구 셋업
    - `d243d16` chore — gitignore 보강 + defaultMode auto + Prefabs.meta
    - `f7d499f` docs(research) — U1~U5 2차 웹조사 결과
    - `ed22f3d` docs(gdd) — D-030 K1~K5 + boost/gears 신설 + fuel 재설계
  - **2차 웹조사 완료 (D-029 약속 이행):** `claude-web-findings-v2.md` — Burnout 부스트 8초(U2 해결), FH5 Granular Synthesis 90 FPS 오디오(U5 해결, Playground Games 담당), F1 게임 시간 시뮬 1:1(U4 결정적 발견 — Codemasters 모델 부적합).
  - **D-030 결정 + GDD 반영:** K1 부스트 이중충전 / K2 8초 / K3 Manual +10% 인센티브 / K4 "주유 결정 2~4번" 감각 / K5 시프트 연출 3종.
  - **연료 시스템 MVP (코드):** `FuelSystem.cs`, `FuelHUD.cs`. 5분 풀탱크 정속 기준. F12 디버그 풀충전 (옛 Input → 신 InputSystem 수정). ArcadeCarController에 `ThrottleInput` 노출.
  - **기어 시스템 MVP (코드):** `GearSystem.cs` (5단 Auto + R + N, 히스테리시스), `GearHUD.cs` (우하단 큰 박스 + 시프트 0.15초 화이트 플래시 = K5 일부).
  - **B 시스템 — 주문 보드/오버뷰 카메라/정산:**
    - `Order/{OrderJob,OrderCatalog,OverviewCamera,DeliveryPointMarker,OrderBoard,RunController,ResultPanel,CountdownHUD}.cs`
    - `Editor/SetupOrderSystem.cs` 메뉴 — Order_A/B/C + Catalog + 8지점 + RunRoot/HUD 자동 생성·와이어링
    - `DeliveryManager.cs` `ActivateSubset(int n)` 추가
    - 3 트레이드오프: A.안전(3지점/무제한/₩500) / B.균형(5지점/5분/₩900+₩200) / C.도전(7지점/6분/₩1800+₩500)
  - **OverviewCamera 버그 수정:** `Quaternion.Euler(70,0,0) * Vector3.back` 결과로 카메라 지면 아래로 박혔던 것 → 명시적 삼각함수 + LookAt 방식. height 95, pitch 75°, FOV 55.
  - **DeliveryPointMarker 자동 상태 반영:** 현재 목표 펄스 / 대기 흐림 / 완료 회색.
  - **DeliveryHUD(화살표) 셋업에 추가** — 화면 밖 다음 목표 방향 표시 복구.
  - **EngineAudio 일시정지 시 mute** — 처음 lerp 페이드 → 새는 듯해서 강제 `src.mute=true`로 강화. AudioSource 직렬화도 `playOnAwake=false, mute=true`로 정리.
  - **Easy Delivery Co. 조사 (도현 요청):** `research/easy-delivery-co.md` — 95% 압긍 4800+ 리뷰. 잡 보드 / 장비 잠금해제 / 이중 게이지 인사이트. 페인 포인트(시간 압박+GPS 둘 다 없음)가 PIT CREW 강점과 정확히 반대 = 시장 빈 칸 검증.
  - **D-031 시도 → D-032 철회 사이클:** EDC "운전자 자원" 차용해 부스트 폐기하고 운전자 집중력 시스템 신설(`DriverResource.cs`+`DriverHUD.cs`+ArcadeCarController External* 인터페이스). 도현 검토 결과 컨셉 동의가 즉시 구현 동의는 아니었음 → 전체 롤백. 부스트(D-030) 정본 복귀.
  - **USP 4개 명문화 (overview.md):** ① 압축 폼팩터 5~15분 ② 결정 밀도 4/런 ③ 운전감(Burnout급) ④ 로그라이트 정산. 시장 빈 칸 표(EDC/Crazy Taxi/Burnout/Pacific Drive) 명시. **이후 모든 시스템 결정은 USP 4개 중 하나 이상 강화해야 채택.**
  - **권한 모드 / auto mode 활성화:** 도현이 데스크톱 설정에서 직접 토글로 켰음. `defaultMode: "auto"` 도 settings.local.json에 들어감.
- **다음 세션(세션 11) 할 일:**
  - **3 트랙 동시 진행 — 주제만 정해져 있고 구체 명세는 다음 세션 시작 때 합의:**
    1. **맵 디자인 — 실제 도시 느낌.** 현재는 단순 격자 (3×4 도로 그리드 + Kenny 건물). 도로 다양성·랜드마크·구역 구분·교차로 디테일 등 도시감 보강.
    2. **UI — 실제 게임처럼.** 현재는 OnGUI 박스 + 텍스트만. Canvas + UI Toolkit으로 전환, 폰트/아이콘/애니메이션 정리.
    3. **그래픽 느낌 / 렌더링.** URP 셰이더·라이팅·포스트프로세싱 톤 정리. Kenny 에셋 위에 어떤 무드를 입힐지.
  - **위 3개 진입 전 합의 단계 필수 (D-032 교훈):** ① 컨셉 합의 → ② 명칭/디테일 합의 → ③ MVP 명세 합의 → ④ 구현. 단계 합치지 말 것.
- **관련 문서/코드:**
  - `GDD/overview.md` (USP 4개), `GDD/systems/{boost,gears,fuel}.md`, `GDD/research/{claude-web-findings-v2,easy-delivery-co}.md`
  - `notes/decisions-log.md` D-030, D-031(시도), D-032(철회)
  - `F1/Assets/Scripts/{FuelSystem,FuelHUD,GearSystem,GearHUD,Order/*,EngineAudio,ArcadeCarController,DeliveryManager}.cs`
  - `F1/Assets/Editor/SetupOrderSystem.cs`, `_Recovery/{RunSetupOrderSystem,RemoveDriverComponents,InspectAudioSource,CleanRaceAudioSource}.cs`
  - `F1/Assets/{ComplexTrack.unity,Prefabs/Race.prefab,Data/Orders/*}`
- **메모:**
  - **도현 패턴 8번째 — "차별점이 명확하려나" 자발적 검증.** ExulMundi 패턴 회피 의지 강해지는 신호. USP 명문화 프로세스로 매 결정 게이트화 도입.
  - **D-032 교훈 (큰 시스템 도입 시 단계 분리):** "ㅇㅇ" 답변 = 컨셉 동의. 즉시 구현 진입 X. 명칭·디테일·MVP 명세 단계별 합의 필요. 이번 D-031 → 코드까지 가서 롤백. 비용 한 시간이지만 다음엔 단계 분리로 0.
  - **EDC 조사 = 시장 진단 도구로 유효.** 단순 "메커니즘 카피" 이상의 가치. 페인 포인트(EDC가 못 한 것)가 우리 USP 검증.
  - **2차 조사로 발견한 결정적 사실:** F1 공식 게임도 시간 시뮬 1:1. PIT CREW가 Codemasters를 따라할 모델 아님. 독자 설계 정당화.
  - **OverviewCamera 버그 = 회전 직접 계산 함정 재발.** 세션 7 D-026("Y-yaw만 atan2로 직접") 교훈이 카메라 시점에서도 적용됨. CLAUDE.md "에셋/맵 자동 배치 작업 규칙"에 카메라 포즈도 검토 필요.
  - **Coplay MCP 효과성 또 입증:** SetupOrderSystem 자동화 + DriverResource 롤백까지 모든 씬 작업이 `execute_script` + `add_component` + `check_compile_errors` 루프. 수작업이면 세션 시간 2~3배.
- **컨텍스트 상태:** 세션 종료. 다음 세션은 맵/UI/그래픽 3 트랙 — 시작 시 각 트랙별 컨셉 합의부터.

---

## 세션 11 — 2026-04-23

- **목표:** 맵 디자인 "실제 도시 느낌" 강화. 도로 연결 부자연스러운 부분 해결.
- **한 일:**
  - **연구 페이즈 (D-029 패턴 재현, 타임박스 1 세션):** 3 병렬 웹조사 — 업계 포스트모템 / 학술 이론 / 로우폴리 아트. `GDD/research/map-design-research.md`에 수렴 결론 저장 (Lynch 5요소 + 랜드마크 + 구역 분할 = Burnout·Forza·Parish-Müller·Totten·Art of Rally 전부 동의).
  - **D-032 교훈 준수:** ① 컨셉(Lynch 5요소) ② 명칭/디테일(구역 경계 + 랜드마크 4개) ③ MVP 명세(mask 재배치 + 7 배달점 + 아트 프리셋) 3단계 순차 합의 후 구현. 롤백 0.
  - **구역 분할 + 랜드마크 (D-033):**
    - `SetupLayoutAssets.cs` 확장 — Industrial kit 추가 + 신규 mask char 4개 (H highrise 5종 / T chimney 2종 / F factory 2×2 / W warehouse 1×1). BuildingDef 11개 신규.
    - `SetupComplexScene.cs` buildingMask 재작성 — 좌(교외 S/X) / 중앙 하(상업 C/K) / 중앙 상(Downtown H/K + T at (6,5)) / 우(산업 W/F). 배달점 8→7 Lynch Node 원칙으로 교차로 배치.
    - `SetupOrderSystem.cs` — `layout.deliveries` 덮어쓰기 제거 (Layout SO가 truth).
  - **아트 프리셋 (`_Recovery/SetupArtPreset.cs` 신규):** URP Volume Profile (`PitCrewCityVolume.asset`) — Bloom/Vignette/ACES Tonemapping/Color Adjustments. Warm Directional Light (#FFE4B5, Euler 50,-30,0, 1.3). Ambient Trilight Gradient + Linear Fog 70~220m.
  - **Industrial kit 실측 (`_Recovery/MeasureIndustrial.cs`):** 25종 maxXZ·height 측정. D-023 규칙 준수.
  - **검증:** `_Recovery/TopDownDiagnose.cs` + `SideView.cs`. 36 건물 all verified. 사이드뷰에서 Chimney Tower T 랜드마크가 Downtown 앵커로 완전 작동 확인.
  - **재빌드 자동화 (`_Recovery/RunFullRebuild.cs`):** Layout Assets → Complex Scene → Race → Order System 4 스텝 원샷 실행.
- **다음 세션(세션 12) 할 일:**
  - **도현 Play 테스트:** ComplexTrack 주행. Warm 아트 프리셋 체감. 새 맵 7 배달점 + Downtown Tower 가시성.
  - **커밋 (미커밋):** 본 세션 변경 덩어리. 분리 커밋 권장 — (1) 연구 문서 (2) 에셋/코드 + 씬.
  - **UI 트랙 진입** (세션 10에서 예정된 3 트랙 중 2번째): OnGUI → Canvas/UI Toolkit.
  - **그래픽 트랙**은 이미 부분 선행됨 (D-033 아트 프리셋). 남은 건 머티리얼/셰이더 톤 맞추기, 필요 시 4주차.
- **관련 문서/코드:**
  - `GDD/research/map-design-research.md` (신규)
  - `notes/decisions-log.md` D-033
  - `F1/Assets/Editor/{SetupLayoutAssets,SetupComplexScene,SetupOrderSystem}.cs`
  - `F1/Assets/Editor/_Recovery/{SetupArtPreset,MeasureIndustrial,TopDownDiagnose,SideView,RunFullRebuild}.cs`
  - `F1/Assets/Data/Buildings/` 11 신규 BuildingDef
  - `F1/Assets/Data/{BuildingCatalog,ComplexTrackLayout}.asset` 업데이트
  - `F1/Assets/Settings/PitCrewCityVolume.asset` (신규)
  - `F1/Assets/ComplexTrack.unity` (구역 재배치 + GlobalVolume)
- **메모:**
  - **연구→합의→구현 파이프라인 재현 성공.** 세션 9 D-029 패턴이 매력적 템플릿으로 자리잡음. 세션 11에서는 1 세션 내 완결 (연구·합의·구현 동시). 다음 큰 결정에서도 재사용.
  - **D-032 교훈 실전 적용.** "알아서 잘 만들어봐" 위임 받았음에도 컨셉→명칭→명세 단계별 합의 거쳤음. 롤백 없이 완료. 컨셉/명세 분리의 가치 입증.
  - **도현 피드백 "도로 연결 부자연":** 재빌드 시 자연 해소. 실제로는 세션 7 D-024 시점부터 잔존했을 이음매 미스매치(Kenny city-kit 특유 crosswalk mesh 경계)가 mask 재작성 + BuildingPlacer 재배치로 해결됨.
  - **Coplay MCP 핵심 역할:** `execute_script`로 재빌드 루프, `capture_scene_object` 톱다운/사이드뷰, `get_unity_logs`로 VerifyPlacements 결과 확인. 세션 내 5 사이클 반복 가능했던 이유.
  - **BuildingPlacer 돌출 버그는 실재했음.** 원인 정확히는 못 잡았지만(재빌드로 사라짐), mask 재설계 시 2×2 anchor 위치가 더 정리돼서 우연 해결. 재발 시 `VerifyPlacements`에 horizontal bounds overflow 체크 추가 고려.
- **컨텍스트 상태:** 세션 종료. 다음 세션은 Play 테스트 + UI 트랙 진입.

### 세션 11 이어서 — T자 수정 + 도로 카탈로그 + 오픈월드 Tier A (D-034)

- **도현 추가 피드백:**
  - "T자 도로가 아직 제대로 연결 안됨" (세션 11 초기 수정 후에도 남음)
  - "도로 구성 에셋들 한번씩 인식해서 앞으로 다 쓸 수 있게"
  - "정형화된 블럭 도시 = 돌아다니는 맛 없음 + 오픈월드 맵구성 고려"
- **T자 실측 (2차 시도로 해결):**
  - 1차 수정(단일 probe) = -90 오프셋 → 오답. T 여전히 이상.
  - 2차 수정(4 회전 side-by-side probe `MeasureIntersectionRotations`) = +90 오프셋 → 정답.
  - 최종 매핑: rot 0 missing S / 90 missing W / 180 missing N / 270 missing E.
  - 개별 캡처로 (4,0) (0,4) T-교차로 정상 작동 확인.
- **Kenny 도로 카탈로그 (`MeasureAllRoads.cs`, `GDD/research/kenny-roads-catalog.md`):**
  - 72 FBX 전수 실측 (straight/bend/curve-intersection/crossroad/roundabout/split/side-entry/driveway/slant/bridge/square/sign/light/construction/tile 분류).
  - 활용 매트릭스 — 현재 사용 중 vs 향후 활용 후보.
- **오픈월드 Tier A 적용:**
  - `BuildingDef.addCollider` 플래그 신설 → road/prop 타일을 BuildingCatalog로 다룰 수 있는 인프라.
  - `P` (Plaza) 엔트리 추가 — road-square FBX, addCollider=false.
  - roadMask 지름길 4곳 (`.###` 패턴 row 2/6에 각각 2번) → 6 큰 블록 → 12 subblock 분할.
  - buildingMask 광장 2곳 (`PPP` row 3/7 교외 남단) → 아스팔트 플라자.
  - 결과: 경로 선택 다양성 확보, "정형화" 깨짐. 40 건물 all verified.
- **Tier B 파킹:** Roundabout 3×3 (TileMapAuto 구조 변경 필요) / 맵 비대칭 외곽 (roadMask 대수술) / 대각선 도로 (에셋 부재).
- **관련 문서/코드:**
  - `notes/decisions-log.md` D-034
  - `F1/Assets/Editor/TileMapAuto.cs` (Intersection 매핑 + 실측 주석)
  - `F1/Assets/Scripts/Layout/BuildingDef.cs` (addCollider)
  - `F1/Assets/Editor/BuildingPlacer.cs` (addCollider 체크)
  - `F1/Assets/Editor/SetupLayoutAssets.cs` (Plaza pool)
  - `F1/Assets/Editor/SetupComplexScene.cs` (지름길 + 광장 mask)
  - `F1/Assets/Editor/_Recovery/{MeasureIntersection,MeasureIntersectionRotations,MeasureAllRoads,RebuildAfterTeeFix}.cs`
  - `GDD/research/kenny-roads-catalog.md`
- **메모:**
  - **세션 11 중복 피드백 패턴:** 도현이 초반 요청("맵 만들어봐")에서 광범위하게 맡기고, 구현 후 구체 피드백 3회 (T자 / 정형화 / 오픈월드). 세션 5 "도로 아직 이상함" 3-4회 패턴 재현 — 도현 집요함 = 근본 버그 발견. 이번도 `Empirical default` 미실측 버그 발견 + 맵 구조 단조로움 근본 해결.
  - **실측 도구화의 가치:** `MeasureIntersectionRotations.cs` 같은 "N개 회전 side-by-side" 패턴이 단일 probe보다 확정적. 추상적 방향(CW/CCW) 추론 불가할 때 기계적 검증으로 대체.
  - **BuildingDef.addCollider 확장성:** 이 플래그 하나로 road-kit 타일을 Building catalog에 통합 가능 → 향후 sign-highway (랜드마크) / road-square (광장) / construction-cone (장애물) / light (가로등) 전부 "Building" 파이프라인으로 배치 가능.
- **컨텍스트 상태:** 세션 종료. 다음 세션은 Play 테스트 + UI 트랙.

### 세션 11 이어서 — 커밋 + UI 트랙 세션 12 이관

- **커밋 3개 분리 완료:**
  - `b0f6fda` feat(session-11) — 맵 Lynch 5요소 + T자 + 오픈월드 Tier A (90 files, +6342/-4599)
  - `8f53bff` docs(session-11) — D-033/034 + 리서치 + Kenny 도로 카탈로그 (5 files, +417)
  - `98364a4` chore(session-11) — Kenny UI pack + interface sounds 임포트 (세션 12 스테이징)
- **UI 트랙 명세 합의 (구현은 세션 12로 이관):**
  - 기술: UGUI (Canvas) 확정
  - 에셋: Kenny UI pack (Grey+Yellow 조합, Warm 아트 프리셋 매칭) + Kenny Future TTF + Kenny interface-sounds
  - Tier 1 (주행 상시): FuelHUD / GearHUD / CountdownHUD — 1순위
  - Tier 2 (1회성): DeliveryHUD / OrderBoard / ResultPanel — 2순위
  - 공통 인프라: UIRoot (Canvas 1920×1080) / UITheme SO / UISounds static helper / TMP_FontAsset 생성
- **세션 12 오프닝 진입점:**
  1. TMP Essentials 임포트 + Kenney Future → TMP_FontAsset 2종 변환
  2. UIRoot + UITheme + UISounds 인프라 구축
  3. FuelHUD UGUI 전환 → 패턴 검증
  4. GearHUD/CountdownHUD 동일 패턴으로 전환
  5. (선택) DeliveryHUD 전환 or 세션 13으로 이관
- **세션 11 총괄:**
  - 연구 페이즈 1 (맵 3 병렬 조사) → 즉시 구현 완결 (D-033 Lynch 5요소)
  - 버그 수정 2회 시행착오 → 실측 기반 해결 (D-034 T자 rot 0→S/90→W/180→N/270→E)
  - 에셋 카탈로그 인프라화 (72 Kenny roads + Industrial kit + UI pack)
  - 맵 "정형화" → "오픈월드 느낌" 전환 (Tier A: 지름길 4 + 광장 2)
  - 커밋 위생 유지 (3 분리 커밋, 미커밋 축적 0)
  - UI 트랙은 D-032 교훈 따라 신규 세션 진입 결정 (장시간 후 판단력 저하 회피)
- **도현 패턴 10번째:** "B로 ㄱㄱ" 세션 종료 판단 자발적 수용. 세션 경계 관리 능력 향상 — 세션 10 "세이브 후 기획 대화" 요청 이어서 두 번째 성숙한 신호.
- **컨텍스트 상태:** 세션 종료. 다음 세션은 UI 트랙 Tier 1 구현부터.

---

## 세션 12 — 2026-04-23

- **목표:** UI 트랙 Tier 1 완결 — TMP 인프라 + UIRoot/UITheme/UISounds + FuelHUD/GearHUD/CountdownHUD OnGUI → UGUI 전환. 세션 11 종료 시 합의된 5단계 진입점 그대로 실행.
- **한 일 (D-035):**
  - **TMP Essentials 자동 임포트 (`Editor/_Recovery/EnsureTMP.cs`):** Unity 6 UGUI 2.0이 PackageCache에 번들한 `TMP Essential Resources.unitypackage`를 `AssetDatabase.ImportPackage`로 자동 임포트. `com.unity.ugui@*/Package Resources/` glob 탐색으로 버전 무관 대응.
  - **Kenney Future TMP FontAsset 2종 (`Editor/_Recovery/CreateKenneyFontAssets.cs`):** `TMP_FontAsset.CreateFontAsset`으로 Dynamic SDF 아틀라스 생성. sampling 90, padding 9, 1024² atlas, multi-atlas 활성. 결과: `Assets/UI/Fonts/{KenneyFuture SDF, KenneyFutureNarrow SDF}.asset`.
  - **UI 인프라 3종 (`Scripts/UI/`):**
    - `UIRoot.cs` — 1920×1080 Canvas + CanvasScaler(Match 0.5) + GraphicRaycaster. `Ensure()` static helper로 싱글톤 보장.
    - `UITheme.cs` ScriptableObject — 폰트 2종 + panel/track/fill 9-slice sprite + 상태색(ok/warn/crit/pulse, text/muted/flash/accent, timer normal/mid/low) + 사운드 4종(click/back/confirm/shift). `UITheme.Active`는 Resources 싱글톤.
    - `UISounds.cs` — lazy singleton. `UISounds.Shift()` 등 정적 호출 → UITheme에서 클립 가져와 PlayOneShot.
  - **HUD 3종 UGUI 전환 (파일 GUID 유지 → 씬 참조 보존):**
    - `FuelHUD.cs` — 좌하단 가로 바. BG Image + Fill Image(크기 anchoredPosition 기반 뉴비) + TMP_Text 라벨. 25% warn(Yellow), 10% crit(Red blink).
    - `GearHUD.cs` — 우하단 박스. "GEAR" 라벨(Narrow font) + 큰 기어숫자(Primary font). `OnShift` 이벤트 구독 → 0.15s 화이트 플래시 Lerp + `UISounds.Shift()` 호출 (Kenney bong_001).
    - `CountdownHUD.cs` — 우상단. `RunController.TimeRemaining < 0` 이면 CanvasGroup.alpha 0 (무제한 작업 자동 숨김). 30s/60s 경계별 배경 색 전환.
  - **HUD 자가구성 패턴:** 세 HUD 모두 Awake에서 `EnsureCanvasParent()` → Canvas 부모 없으면 `UIRoot.Ensure()` 하위로 자동 재부착. 이전 `RunRoot`에 붙어있던 CountdownHUD도 런타임에 알아서 이동.
  - **UITheme 연결 자동화 (`Editor/_Recovery/SetupUITheme.cs`):** Resources/UITheme.asset 생성 + Kenney font/sprite/sfx 참조 연결. Kenney sprite 3종(`button_rectangle_depth_flat`, `slide_horizontal_grey_section_wide`, `button_rectangle_flat[Yellow]`) TextureImporter 자동 설정 (Sprite mode, border=14px, pixelsPerUnit=100).
  - **씬 배치 자동화 (`Editor/_Recovery/SetupUIScene.cs`):** 현재 열린 씬에 UIRoot Canvas + FuelHUD + GearHUD GameObject 멱등 배치. 이미 있으면 스킵. 저장까지 자동.
  - **Play 테스트 (Coplay capture_ui_canvas):** ComplexTrack.unity에서 Play 진입 → "FUEL 100%" (녹색 9-slice fill, Kenney 폰트) 좌하단 + "GEAR N" (Primary font 96pt) 우하단 정상 표시 확인.
- **다음 세션(세션 13) 할 일:**
  - **도현 Play 테스트:** 실제 주행으로 기어 시프트 플래시 + bong 사운드 + 연료 warn/crit 깜박임 체감.
  - **UI Tier 2 이관:** DeliveryHUD(화살표) / OrderBoard(A/B/C 카드) / ResultPanel(정산) 같은 자가구성 패턴으로. Button + Kenney click/confirmation SFX 연결.
  - **커밋 (미커밋):** TMP Essentials 임포트 + UI 인프라 + HUD 전환 — 세션 11 패턴처럼 2~3 분리 권장.
  - **그래픽 트랙 잔여 (4주차 이후):** URP 머티리얼 톤 / 셰이더 / 포스트프로세싱 디테일. 아트 프리셋은 세션 11에서 선행.
- **관련 문서/코드:**
  - `notes/decisions-log.md` D-035
  - `F1/Assets/Scripts/UI/{UIRoot,UITheme,UISounds}.cs` (신규)
  - `F1/Assets/Scripts/{FuelHUD,GearHUD}.cs` + `F1/Assets/Scripts/Order/CountdownHUD.cs` (UGUI 재작성)
  - `F1/Assets/Editor/_Recovery/{EnsureTMP,CreateKenneyFontAssets,SetupUITheme,SetupUIScene}.cs` (신규)
  - `F1/Assets/Resources/UITheme.asset` (신규)
  - `F1/Assets/UI/Fonts/{KenneyFuture SDF, KenneyFutureNarrow SDF}.asset` (신규)
  - `F1/Assets/TextMesh Pro/` (TMP Essentials 전체 임포트 — Resources/Shaders/Sprites/Fonts)
  - `F1/Assets/ComplexTrack.unity` (UIRoot + FuelHUD + GearHUD 추가)
- **메모:**
  - **UI 자가구성 패턴이 키.** `Awake → EnsureCanvasParent → BuildIfNeeded` 3-step이 Tier 2도 동일하게 반복될 베이스 패턴. 신규 HUD 추가 비용이 "스크립트 1개 + 씬에 빈 GameObject 1개"로 내려감. 프리팹 관리 부담 없음.
  - **파일 GUID 보존 선택 (구 HUD → UGUI 내용 덮어쓰기):** `Scripts/UI/` 하위에 신규 HUD 3종을 먼저 썼다가, 같은 클래스명 중복 컴파일 에러 + 씬 참조 깨짐 리스크를 고려해 구 경로 파일을 그대로 UGUI 내용으로 덮어쓰고 UI/ 중복은 삭제. Unity의 MonoScript GUID 기반 참조 방식 특성상 이 방식이 가장 저위험.
  - **TMP Dynamic SDF의 실전성:** 한글 없음 + 영문/숫자만 → Dynamic 모드로 아틀라스 pre-bake 없이 즉시 사용 가능. 첫 글자 사용 시 런타임 rasterize 비용은 한 프레임 미만, 이후 캐시. 2주차 개발에 최적.
  - **Kenney UI pack에 "panel" sprite가 없다는 제약.** button_rectangle/slide/input만 있음. BG로 `button_rectangle_depth_flat`(어두운 회색) 전용 → tint colorPanel(검정 0.55α)을 곱하면 배경이 흐려져 거의 안 보임 (Play 캡처에서 확인). **튜닝 대상:** colorPanel을 더 밝은 회색으로, 또는 Extra/Default의 `divider` + 별도 반투명 레이어 조합 검토. 세션 13 Tier 2 작업 시 함께 개선.
  - **CountdownHUD가 RunRoot에 남아있어도 문제 없음.** Awake에서 Canvas 부모 탐색 → 없으면 UIRoot로 이동. 씬 편집 없이 마이그레이션 완료.
  - **D-032 교훈 지속 적용.** 세션 11 말미에 UI 트랙 컨셉/명세를 먼저 합의하고 구현은 새 세션으로 분리 → 이번 세션은 단일 트랙 집중으로 롤백 0 + 컨텍스트 유지.
- **컨텍스트 상태:** 세션 종료. 미커밋 변경 있음 (TMP + UI 인프라 + HUD 전환 + 씬 업데이트). 다음 세션 오프닝 첫 작업으로 분리 커밋.

---

## 세션 13 — 2026-04-23 ~ 2026-04-24

- **목표:** 세션 12 UI Tier 1 검증 후 런타임 에러 2건(CountdownHUD CanvasGroup / 폰트 글자 깨짐) 해결. 도현 "맵 단조롭다" 피드백 기반 맵 확장 + 도로 다양성.
- **한 일 (2 블록):**

### 블록 A — UI Tier 1 hotfix (세션 12 D-035 이어서)

  - **CountdownHUD MissingComponentException (CanvasGroup):**
    - 원인: `EnsureCanvasParent`가 RunRoot(일반 GameObject, 하위에 RunController/OrderBoard 공존) 전체를 UIRoot로 옮기려 함. `SetParent` 직후 Awake 나머지에서 CanvasGroup 참조 불안정.
    - 수정: HUD 3종의 `EnsureCanvasParent`를 "부모 없는 루트일 때만 SetParent, 부모 있는데 Canvas 밖이면 경고 + enabled=false"로 제한. SetupOrderSystem에서 CountdownHUD를 RunRoot 대신 UIRoot 하위 독립 GameObject로 생성. `[RequireComponent(RectTransform, CanvasGroup)]` 추가. `??` 연산자를 `TryGetComponent`로 교체 (Unity fake-null 이슈 회피). MigrateCountdownHUD.cs 에디터 스크립트로 기존 씬 마이그레이션.
  - **Kenney 폰트 글자 깨짐:**
    - 원인: `AtlasPopulationMode.Dynamic`이 Play stop 시 atlas 초기화 → Play 재진입 시 글리프 누락 → 깨진 렌더.
    - 수정: `RebuildKenneyFontAssets.cs` 개편 — CreateAsset 후 `TryAddCharacters`로 ASCII 0x20-0x7E pre-populate → `atlasTextures`/`material`을 asset 서브에셋으로 편입 → `atlasPopulationMode = Static` 전환 → `ReadFontAssetDefinition` + `SaveAssets`. sampling 90→64 (atlas 여유), padding 9→5.
  - **검증:** Play 모드 UI 캡처에서 "FUEL 100%"(녹색 9-slice) + "GEAR N" 선명, 콘솔 에러 0건. 커밋 `e9eb61c`.

### 블록 B — 맵 확장 + 도로 다양성 (D-036)

  - **도현 피드백 분류:** 맵 넓히기 / 높낮이 / 화물 배치 UX / 시스템 고도화.
  - **스코프 체크 결과:**
    - MVP 범위 유지: 맵 평지 확장 + 도로 다양성
    - v2로 이관: 높낮이(H4) / 배치 UX(H2) / Shock 물리(H3) / 허브 씬(H1) / Roundabout(H5)
  - **ideas-parking.md 확장 (D-037):** "v1 MVP 이후 고도화 파이프라인" 섹션 추가, H1~H5 명문화. 개별 파킹보다 묶음 관리 — D-031 철회 교훈 반영.
  - **TileMapAuto Kind 확장:**
    - `Kind.Crossing` — `road-crossing.fbx` (Straight 시각 변종)
    - `Kind.Driveway` — `road-driveway-single.fbx` (End 변종, 배달 진입로용. 이번 세션 실 배치는 0개 — mask char 'D' 사용 처 없음)
    - mask char 체계: `#` 일반 / `X` crossing / `D` driveway. IsRoad/CellChar 분리. Kind 결정 후 char 기반 override.
    - `Kind.End`의 source를 `road-end.fbx` → `road-end-round.fbx`로 교체 (교외 분위기 round cap).
  - **15×9 맵 (D-036):**
    - 동쪽 cols 13-14에 ㅗ자 L 돌출: main road(row 4)에서 1칸 뻗은 뒤(col 13) 북/남 분기(col 14 rows 3/5 dead-end).
    - Crossing 9개 (cols 1/5/9 × rows 0/4/8). 이음매 손상 0 (crossing 1×1 = straight 1×1).
    - 지름길 row 2/6 유지 (D-034 Tier A).
    - buildingMask 동쪽 확장 8칸 (S/H districts).
  - **배달점 재배치:** 7개 고정 유지, `EastBranchEnd(14,4)` 신규로 `IndustryNorth(12,6)` 대체. 맵 확장을 배달 동선에 반영.
  - **RunFullRebuild Step 5 추가:** SetupUIScene.Execute. 새 씬 생성 시 UIRoot/FuelHUD/GearHUD 자동 복구. SetupComplexScene이 UI를 날리던 문제 자동화 해결.
  - **연결 버그 1회 시행착오:** row 4 첫 작성 시 `"#X###X###X###.#"` (col 13='.') → 동쪽 L이 main과 단절된 고립 세로선. 수정 `"#X###X###X#####"` (col 13='#') → main과 정상 연결. `Tile_Intersection_14_4_r270` 생성 확인으로 검증.
- **다음 세션(세션 14) 할 일:**
  - **커밋 (미커밋):** D-036/037 + 세션 13 변경. 2 분리 권장 — (1) feat 맵+도로, (2) docs.
  - **도현 Play 테스트:** 새 맵 15×9 주행 체감. Crossing 시각 효과. Dead-end 왕복 비용 감각.
  - **UI Tier 2 이관 (세션 12→13 이월분):** DeliveryHUD(화살표) / OrderBoard(A/B/C 카드) / ResultPanel(정산) UGUI 전환. Kenney click/confirmation SFX 연결.
  - **재미 검증 1차 (Day 7 체크포인트):** 1주차 "차 몰기 자체가 재밌는가" 판정 아직 안 됨. Play 15분 이상 후 YES/그럭저럭/NO 기록.
- **관련 문서/코드:**
  - `notes/decisions-log.md` D-036, D-037
  - `notes/ideas-parking.md` "v1 MVP 이후 고도화 파이프라인" H1~H5 섹션
  - `F1/Assets/Editor/TileMapAuto.cs` — Crossing/Driveway Kind + mask char 분기
  - `F1/Assets/Editor/SetupComplexScene.cs` — 15×9 masks + 배달점 재배치
  - `F1/Assets/Editor/_Recovery/RunFullRebuild.cs` — Step 5 SetupUIScene 추가
  - `F1/Assets/Editor/_Recovery/{MigrateCountdownHUD, RebuildKenneyFontAssets}.cs` (블록 A)
  - `F1/Assets/Scripts/{FuelHUD, GearHUD}.cs` + `F1/Assets/Scripts/Order/CountdownHUD.cs` (블록 A 방어)
- **메모:**
  - **도현 제안을 "묶음 파이프라인(H1~H5)"으로 정리한 게 적중.** 개별 파킹이면 다음에 다시 "지금 넣을까" 유혹 반복. "v2 레이어"라는 상위 컨테이너로 묶으니 "4주 끝난 뒤 한 번에 재검토"가 명확.
  - **마스크 1회 실수 (L 단절)는 TileMapAuto 특성상 즉시 감지 가능:** hierarchy 리스트에서 `Tile_Intersection_14_4_r270` 없고 `Tile_Straight_14_4_r90`만 있으면 "L이 main과 단절됨" 신호. 세션 11 "실측 도구화" 원칙 재현 — 계층 이름으로 검증.
  - **CountdownHUD 문제의 진짜 원인은 `EnsureCanvasParent`의 과도한 일반화였음.** HUD 3종이 전부 "부모 이동 가능" 전제였지만 실제로는 CountdownHUD만 공존 GameObject에 붙는 특이 케이스. 향후 HUD 추가 시 기본값은 "독립 GameObject"로 가정.
  - **`??` 연산자 + Unity UnityEngine.Object 이슈 재확인.** Unity C# 표준 교재에도 자주 등장하는 주제. 앞으로 GetComponent 관련은 `TryGetComponent` 우선.
  - **세션 12→13 이관이 옳았다.** UI 트랙 세부 디버깅은 컨텍스트 긴 상황에서 판단력 감소. D-032 교훈 적용 연속 성공.
- **컨텍스트 상태:** 세션 종료. 미커밋 변경 있음 (블록 B). 다음 세션 오프닝 첫 작업으로 분리 커밋.

---

## 세션 14 — 2026-04-25

- **목표:** 1주차 Day 7 재미 판정 + 도현 피드백 기반 기획 재검토. 리서치 병렬 페이즈 개시.
- **진행 중 (미완료):**
  - **Day 7 재미 판정 기록:** "그럭저럭/NO". 도현 피드백 3건 — ①맵 돌아다니기 자체 재미 없음 ②주행 중 신경쓸 요소 필요 ③맵 크기/생김새 단조로움.
  - **D-039 부스트 시스템 최종 철회** — D-030 → D-031 → D-032 경로 후 "없는 게 좋을 듯" 확정. 코드 미구현 상태라 비용 0.
  - **D-040 컨셉 전환** — 레이싱 배송 → **현실 마을/시골 배송**. H4 고저차 MVP 편입, 열차 편입, 오픈월드 기각 방침 정정("마을 느낌 닫힌 스테이지" 허용).
  - **리서치 4트랙 병렬 에이전트 (background):**
    - 트랙 0 — Kenny 에셋 전수 카탈로그 (`GDD/research/kenney-asset-inventory.md`)
    - 트랙 A — 마을 배송 레퍼런스 (Euro Truck 2/Snowrunner/Pacific Drive/Death Stranding/Short Hike) (`GDD/research/driving-fun-research.md`)
    - 트랙 B — 화물 Shock 명세 + 환경 긴장 요소 (`GDD/research/realtime-tension-research.md`)
    - 트랙 C — UI 고급화 (Shader/TMP/UI Toolkit/MCP) (`GDD/research/ui-polish-research.md`)
- **리서치 4 완료 후 도현 결정 (세션 14 후반):**
  - 열차 → **v2 유지** ("나중에 움직이는 걸로")
  - 주인공 차량 교체 → **보류** ("일단 만들면서 결정")
  - 허브 씬 → **MVP 편입** ("시작 전 + 정비 장소로"). H1 파킹 해제, 4주차 메타 루프와 통합.
- **다음 (리서치 완료 후):**
  - 4 리서치 문서 검토 + 도현 합의 (우선순위 확정)
  - 2주차 구현 진입 (화물 Shock 우선 + 열차/신호등 순차)
  - USP ③ rephrase (`GDD/overview.md`) — "운전감" → "환경 상호작용"
  - `GDD/systems/{boost,gears}.md` D-039 반영 정리
  - 세션 14 블록 마감 + 커밋 분리
- **관련 문서/코드 (문서화 결과):**
  - `notes/decisions-log.md` D-039, D-040
  - `notes/ideas-parking.md` 부스트 철회 블록 + 오픈월드 방침 정정
  - `dev-plan/feature-list.md` Day 6/7 체크 + 2주차/3주차 편입 항목
- **메모:**
  - 1주차 Day 7 체크포인트가 "컨셉 재편"까지 트리거한 첫 사례. CLAUDE.md 규칙 2의 본래 의도 실현.
  - 리서치 병렬(4 에이전트) 패턴은 세션 11 D-033(3 병렬) 성공 경험의 스케일업. 트랙 0(에셋 카탈로그)이 A/B/C 3트랙의 공통 인풋 역할.
  - 컨셉 전환은 "단번에 컨셉 합의 완료"가 아니라 "1주차 재미 검증 실패 → 방향 전환"의 자연스러운 결과. Day 7 게이트가 정말로 작동.
- **세션 14 실제 구현 (리서치 완료 후):**
  - **화물 Shock 시스템 MVP** — `F1/Assets/Scripts/Cargo/CargoBox.cs`. 박스 HP 100, 종G+횡G 2축 (linearVelocity 변화를 차 local frame 투영), 피크 윈도우 0.2s + 누적 윈도우 1s 병존. 데미지 공식: (peak-0.6)·10 종G / (peak-0.7)·8 횡G / (impact-10)·0.8 충돌 / (avg-0.6)·0.5 누적. `OnIntegrityChanged/OnImpact/OnTotaled` 이벤트.
  - **IntegrityHUD** — `F1/Assets/Scripts/IntegrityHUD.cs` (FuelHUD 패턴 복제). 좌하단 Fuel 위 stack (anchoredPos y=152). 80/50/20 단계 sprite 스왑 + 20% blink + 0% "CARGO TOTALED".
  - **SetupRaceInScene 수정** — Race prefab 인스턴스화 직후 CargoBox AddComponent (멱등).
  - **SetupUIScene 수정** — IntegrityHUD EnsureHUD 체인 추가.
  - **CargoAndHUDSetup.cs (신규 Editor)** — 메뉴 `PitCrew/Cargo/Setup Cargo + IntegrityHUD`. Race prefab에 CargoBox 영구 부착 + SetupUIScene.Execute 연쇄.
  - **검증:** Coplay `check_compile_errors` "No compile errors" + `execute_script` 성공 + Unity 로그 `CargoSetup/UIScene` 정상 + Play 모드 UI 캡처에서 "CARGO 100%"(녹색 9-slice) 좌하단 Fuel 위 정상 stack.
- **세션 14 커밋 5개 (모두 main fast-forward 완료):**
  - `e248fa9` docs(session-14): 리서치 4트랙 산출물
  - `8fe9857` docs(session-14): 컨셉 전환 D-040 + 부스트 철회 D-039
  - `8254918` feat(session-14): 화물 Shock 시스템 MVP + IntegrityHUD (D-040)
  - `acf185a` docs(session-14): 실제 구현 결과 + 화물 Shock 체크 + 세션 15 진입 조건
  - `cc082b0` feat(session-14): 트렁크 CCTV — 외딴 박스 + 전용 카메라 + RawImage HUD
  - `1482aa7` fix(session-14): CCTV 박스 모델 교체 (Cube primitive + 갈색) + 카메라 튜닝
  - `5544e89` / `496f546` / `b991866` — CCTV 탑다운 시점 + 박스 크기 조정 + H6 기획 파킹
  - `8364657` → `f05d8bc` revert: 차량 무게감 v1 (도현 "되돌려놓자" 요청)
  - `ff8fc2d` feat: **카고 패턴 B** — velocity 기억 + 정지/운동 마찰 + AABB + wall bounce (갈대 제거)
  - `6a661b8` feat: **차량 TOP 1** — 입력 SmoothDamp (steer 0.12s / throttle 0.08s)
  - `1695ecf` feat: **차량 TOP 2** — 접지 raycast 복원 + 공중 drag 2배 + 저속 램프 (0~10km/h 50%→100%)
  - `faf814c` → `8e52cd8` fix: **차량 TOP 3** — Visual body rig (CarVisualLean 루트 부착, nested prefab 구조 유지)
- **트렁크 CCTV 구현 (도현 피드백 "차에 박스 붙이면 거추장" 반영):**
  - 월드 외딴 (y=-500) CargoSimRoot + Primitive Cube(갈색 골판지) + SimCam + Point Light
  - CargoBox에 SignedLongG/SignedLatG 노출
  - CargoShakeSimulator: 가속=뒤기움/제동=앞기움/우회전=왼쪽기움/좌회전=오른쪽기움 lerp
  - CargoView HUD: UIRoot 자가구성 + RawImage (512×384 RenderTexture 런타임 참조)
  - 메뉴: `PitCrew/Cargo/Setup Cargo View (CCTV)` — 멱등 + Box/Cam tuning 매 실행 덮어쓰기
  - 검증 Play 캡처: 우상단 TRUNK CAM 창에 갈색 배송박스 선명. 흔들림은 실주행 필요(정지상태는 static). Kenney에 crate/pallet prop 부재 확인 → Cube primitive 채택이 합리.
- **다음 세션(세션 15) 진입 조건:**
  - **맵 디테일 패스** (도현 세션 14 요청 "맵은 언제 할꺼") — Suburban props 19종 (울타리 9 + 나무 2 + 진입로/석길 7 + planter) BuildingCatalog 편입 + mask char 신규 + buildingMask 재배치. 저비용 고효과.
  - 화물 Shock 튜닝 — 도현 실제 Play 30분 후 임계값(longWarnG/latWarnG/damageMul) 조정. 목표 "안전 95+ / 평소 80~90 / 공격 50~70".
  - 신호등 프로토타입 1교차로 (리서치 B Top2).
  - UI 4-Zone 재정리 + TMP Glow/Outline (리서치 B+C 착수).
- **세션 14 총괄 메모:**
  - **리서치 → 기획 결정 → 구현 싱글 세션 완결.** 세션 11 D-033 패턴(3 병렬) 스케일업 → 4 에이전트 병렬 성공. 트랙 0(에셋) 산출물이 A/B/C 3트랙 리서치의 공통 인풋 역할 수행.
  - **Day 7 재미 판정이 컨셉 재편까지 트리거한 첫 사례.** CLAUDE.md 규칙 2 본래 의도 실현. 수치 튜닝 도피 없이 D-040 방향 전환.
  - **도현 "한 문장 일상어" 피드백 memory 저장** — `~/.claude/projects/C--Users-stard-GameDevelope-PitCrew/memory/feedback_communication_style.md`. 결정 질문 스타일 즉시 개선.
  - **D-032 교훈 4단계 분리 지속 적용:** (1) 리서치 4트랙 → (2) 도현 결정 3건(열차 v2/차량 보류/허브 편입) → (3) 명세(리서치 B) → (4) 화물 구현. 롤백 0.
  - **맵 디테일은 세션 15 이관** — 세션 14 컨텍스트 관리 + D-032 "큰 작업 장시간 후 판단력 저하 회피" 원칙. 도현 auto mode 허락에도 세션 경계 유지.
- **컨텍스트 상태:** 세션 종료. 모든 변경 커밋 + main fast-forward 완료.

---

## 세션 15 — 2026-04-28

- **목표:** Phase 1 실행 계획 수립 + Week 2 Day 8~12 일괄 구현 (화물 시스템 다양화).
- **사전 작업 — 게임 디렉터 전략 진단:**
  - Plan 파일 `~/.claude/plans/immutable-splashing-gizmo.md` 작성. Part I (현황 진단), Part II (6개월 4단계 로드맵), Part III (Day 단위 실행 계획).
  - 핵심 진단: D-040 ETS2 방향이 Kenny 타일 + 5~15분 런과 충돌. "퍼즐 박스" 정체성으로 재정의 권고. 신호등/열차 우선순위 낮춤.
  - 시그니처 메커닉 = 화물 Shock 시스템 (이미 구현됨). Phase 1 동안 화물에 100% 집중.
  - 6개월 EA 출시 목표 ($9.99, 6/10 완성도, itch.io).
- **Day 8 (CargoShake 패턴 B) — 이미 완료 확인:**
  - 세션 14 커밋 `ff8fc2d`에서 갈대→velocity 기억+Coulomb 마찰+AABB bounce 전환 완료.
  - 별도 작업 불필요. Day 9로 즉시 진입.
- **Day 9 — CargoType enum + Spin 센서:**
  - **신규 `Cargo/CargoType.cs`** — enum 4종 (Box/Eggs/Glass/Fish).
  - **`Cargo/CargoBox.cs` 수정** — Spin (Yaw Rate) 섹션. `transform.up` 축 angular velocity → deg/s 변환, 0.2초 peak buffer + 데미지 누적. public getter `Type/CurrentSpinDegPerSec/PeakSpinDegPerSec`.
  - 기본값: spinWarnDegPerSec=80, spinDamageMul=0.3.
- **Day 10 — CargoTypePresets + Awake 자동 적용:**
  - **신규 `Cargo/CargoTypePresets.cs`** — 4종 차등 프리셋 + DisplayName/DriveTip 헬퍼.
    - Box: 표준 베이스라인.
    - Eggs: Shock 극민감 (longWarn=0.4, latWarn=0.5, impactWarn=5).
    - Glass: 충돌 극민감 (impactWarn=3, impactMul=2.5).
    - Fish: Spin 극민감 (spinWarn=50, spinMul=0.8).
  - **`CargoBox.cs`** — `applyPresetOnAwake` flag (default true), `ApplyPreset()`, `SetCargoType(newType)` 런타임 교체 메서드.
- **Day 11 — Order 시스템 화물 연동:**
  - **`Order/OrderJob.cs`** — `CargoType cargoType` 필드 추가.
  - **`Order/RunController.cs`** — `CargoBox cargo` 참조, `BeginRun`에서 `cargo.SetCargoType(job.cargoType)` 자동, `EndRun`에서 `LastIntegrityPercent` 캡처.
  - **보상 가중 추가** — `ComputeIntegrityMultiplier` static (GDD 5단계: 80+=100%, 50+=80%, 20+=50%, 1+=20%, 0=0%). reward × integrityMul 적용.
  - **`Order/OrderBoard.cs`** — DrawCard에 "화물 (계란)" 행 + "→ 급가속·급제동 금지" 가이드 행. cardWidth 320→360, cardHeight 200→240.
- **Day 12 — ResultPanel 이야기형 정산:**
  - **`Order/ResultPanel.cs`** — 패널 360→460 height. "화물 (계란) | 87% · 완벽" 행 + 하단 BuildStory 한 줄 (`"{name}, 한 점 흠 없이 도착했음."` 등 5단계).
  - 색상 띠 3단계 차등 — 성공+화물완벽=녹색 / 성공+화물손상=황색 / 실패=적색.
- **다음 단계 (도현 검증 필요):**
  - Unity 컴파일 통과 확인 (Coplay disconnected → 도현 직접 Console).
  - OrderCatalog assets에 OrderJob 4종 cargoType 지정 (Box/Eggs/Glass/Fish 골고루).
  - 플레이테스트 30분 — 화물별 운전 차이 체감 / 정산 화면 이야기 즉각성 / Spin 임계값 적절성.
  - 도현 피드백 후 Day 13~14 (튜닝) 진입.
- **메모:**
  - 세션 14 → 세션 15 사이에 Plan 파일 작성. Phase 1~4 게이트 명문화.
  - 디렉터 진단 권고 = "환경 장애물 추가" 처방 잘못. "화물이 운전 방식을 강제" 처방으로 전환.
  - Week 2 Day 8~12 분량을 single session에서 일괄 구현. /loop self-pacing 모드 활용.
- **세션 15 후반 진입 조건:**
  - 도현 컴파일 + 플레이 30분 후 피드백 → 임계값/배율 튜닝 (Day 13~14).
  - Day 15부터 Week 3 진입 — Tilt 센서 + 노드-엣지 그래프 + 고저차 + 타이어 + 업그레이드 트리 1단계.
- **Day 15 (선행 진입) — Tilt 축 + Cake 화물 타입:**
  - **`Cargo/CargoType.cs`** — `Cake` enum 추가 (5번째). 3축 민감도 완성 (Shock/Spin/Tilt).
  - **`Cargo/CargoBox.cs`** — Tilt (Body Angle) 섹션. `Vector3.Angle(transform.up, Vector3.up)` → 차체 기울기, 0.2초 peak buffer + 데미지 누적. public getter `CurrentTiltDeg/PeakTiltDeg`.
  - **`Cargo/CargoTypePresets.cs`** — Preset struct에 `tiltWarnDeg/tiltDamageMul` 필드 추가, 5종 모두 tilt 값 명시.
    - Box: tilt 15° / 0.3
    - Eggs/Glass: tilt 12° / 0.4 (적당한 민감)
    - Fish: tilt 15° / 0.4
    - **Cake: tilt 5° / 1.2 (극민감)** — 작은 기울기에도 데미지 큼
  - DriveTip: "경사·차체 기울기 금지"
  - 경사 구간 물리(Day 17)는 Rigidbody 자연 중력으로 대부분 처리됨. CargoBox.tilt가 자동으로 데미지 누적 → 별도 코드 추가 안 해도 컨셉 작동.
- **현재 상태 (5일치 변경 누적, 컴파일 미검증):**
  - 신규 파일 3개: CargoType.cs, CargoTypePresets.cs (Day 9~10), Day 15 변경
  - 수정 파일 5개: CargoBox.cs, OrderJob.cs, RunController.cs, OrderBoard.cs, ResultPanel.cs
  - 도현 컴파일 검증 받기 전 Week 3 (고저차 맵, 노드 그래프, 타이어, 업그레이드)는 보류.
- **다음 도현 작업:**
  1. Unity 컴파일 통과 확인 (Console 에러 0)
  2. OrderCatalog 자산에 OrderJob 5종(Box/Eggs/Glass/Fish/Cake) cargoType 분배
  3. 플레이테스트 30분 — 각 타입 운전 차이 + 정산 이야기 즉각성 + tilt 데미지 (경사 만들 자산 없으면 차체 기울 만한 충돌로 테스트)
  4. 피드백 후 Day 13~14 튜닝 → Week 3 (Day 16~21) 진입.

- **Week 3 시스템 구조 선행 진입 (Day 16/17/18/20/21):**
  - **Day 16 — `Layout/TrackLayout.cs`** — `heightMask` 필드 + `cellHeightMeters` (default 1.5m) + `GetHeight(c,r)` + `SlopeDegBetween` 헬퍼.
  - **Day 17 — `ArcadeCarController.cs`** — `CurrentSlopeDeg` getter (`Asin(forward.y)*Rad2Deg`).
  - **Day 18 — 신규 `Route/NodeGraph.cs`** — GraphNodeType enum(Start/Delivery/Pit/Junction/Special), GraphNode/GraphEdge Serializable, NodeGraph SO + FindNode/FindEdge/TotalDistance/TotalEstimatedTime.
  - **Day 20 — 신규 `Tires/TireSystem.cs`** — TireType enum(Soft/Medium/Hard), wear 0~1, GripMultiplier(Soft 1.4 / Hard 0.7), 분당 마모율, SetTire/Replace.
  - **Day 21 — 신규 `Upgrades/UpgradeDef.cs` + `UpgradeSystem.cs`** — UpgradeEffect enum 5종 (SuspensionUpgrade/ChilledHold/ShortcutMap/SmoothSteering/ReinforcedTrunk), Wallet + TryPurchase + IsOwned. CargoBox.ApplyPreset에 후크 추가 (3종 효과 자동 적용). RunController.EndRun에서 Wallet 누적.

- **세션 15 도현 실제 플레이 피드백 + 즉시 튜닝:**
  - **피드백 1 — 속도감 부족.**
    - `ArcadeCarController.maxSpeedKmh` 90→130, `accelTime` 2.8→2.0, `brakeDecel` 6→8.
    - `FollowCamera.baseFov` 55→50, `maxFov` 90→100 (FOV 변화폭 ↑).
    - **신규 Speed Shake** — Perlin noise 기반 카메라 미세 진동 (60% 속도부터, max 0.05m). 속도감 보강.
  - **피드백 2 — 일반 운전에도 화물 파괴, 적정선 없음.**
    - 모든 CargoTypePresets 임계값 +0.2~0.3g 일괄 상향. brakeDecel 0.82g 정상 제동 → 데미지 0.
    - Box: long 0.6→0.9, lat 0.7→1.0, spin 80→100.
    - Eggs: long 0.4→0.7, lat 0.5→0.8, spin 70→90.
    - Glass: long 0.5→0.8, lat 0.6→0.9, spin 80→100.
    - Fish: long 0.7→1.0, lat 0.7→1.0, spin 50→70.
    - Cake: long 0.7→1.0, lat 0.7→1.0, spin 90→110.
    - CargoBox.cs default도 long 0.9, lat 1.0, spin 100으로.
  - **피드백 3 — 갈대 박스.**
    - `CargoShakeSimulator.staticFrictionG` 0.3→0.6 (큰 G만 박스 움직임).
    - `kineticFriction` 3.5→6.0 (빨리 멈춤).
    - `accelScale` 1.2→0.6 (반응 절반).
    - `maxOffset` 0.45→0.25 (이동 폭 절반).
    - `wallBounce` 0.25→0.05 (벽에서 거의 멈춤).
    - `velocityDeadZone` 0.02→0.05 (작은 진동 무시).
    - **회전 진폭 동시 축소** — degPerLongG/degPerLatG 3→1.5, maxRotDeg 8→4, rotLerp 3→6.

- **세션 15 누적 파일 변경 (Coplay disconnected — 도현 컴파일 검증 필수):**
  - 신규 5개: CargoType.cs, CargoTypePresets.cs, Route/NodeGraph.cs, Tires/TireSystem.cs, Upgrades/UpgradeDef.cs + UpgradeSystem.cs
  - 수정 8개: CargoBox.cs, CargoShakeSimulator.cs, OrderJob.cs, OrderBoard.cs, ResultPanel.cs, RunController.cs, ArcadeCarController.cs, FollowCamera.cs, TrackLayout.cs

- **다음 도현 작업 (튜닝 검증):**
  1. 컴파일 통과 확인.
  2. 같은 맵 5분 주행 — 속도감 130km/h 적절한지, FOV 변화 체감되는지, 진동 너무 강한지.
  3. Box 화물로 일반 주행 — 100% 유지되는지 (목표: 95+).
  4. Eggs로 같은 코스 — 80~90% 유지되는지 (목표: 평균 주행에서 약간 손상).
  5. 갈대 사라졌는지 (CCTV 박스 본 상태로 코너 운전).
  6. 추가 피드백 → 미세 튜닝 또는 Week 3 (Day 19 RoutePlannerPanel UI) 진입.

- **세션 15 추가 — Week 4 인프라 + 새 디자인 결정:**
  - **Day 24 — `Upgrades/UpgradeCatalog.cs` 신규 + `Order/ResultPanel.cs` 업그레이드 영역 통합:**
    - UpgradeCatalog SO (5개 UpgradeDef 리스트)
    - ResultPanel 패널 460→700 (h), 540→600 (w). Wallet 표시 + 5개 카드 가로 배치 + 보유/구매/자금부족 상태별 표시.
  - **Day 25 — `Audio/AudioManager.cs` 신규 (인프라):**
    - 싱글톤 + SFX 6풀 + Music 1소스. `ignoreListenerPause=true` 정산 화면 작동.
    - 자동 후크: CargoBox.OnImpact (속도 비례 볼륨), OnTotaled, DeliveryManager.OnDeliveryCompleted, UpgradeSystem.OnUpgradePurchased.
    - 도현 인스펙터에서 AudioClip 6종 + run music 할당하면 즉시 작동.

- **D-041 — Packing(포장) 시스템 도입 (도현 세션 15 신규 아이디어):**
  - **문제 진단:** 일반 주행에도 화물 데미지 → 좌절. 임계값 튜닝만으로 해결 어려움.
  - **해결:** 박스 안에 충격흡수재(스티로폼/뽁뽁이) 포장 결정 시스템.
  - **신규 `Cargo/PackingTier.cs`** — enum 4단계 + Helper static.
    - None: ×1.0 (매우 조심), Bubble: ×1.3 (일반 안전), Foam: ×1.6 (공격 가능), Hardcase: ×2.0 (거의 모든 운전).
    - Cost: 0/100/300/800 ₩.
  - **`CargoBox.cs`** — `packing` 필드 추가, ApplyPreset에 `packMul` 적용 (longWarn/latWarn/spinWarn/tiltWarn/impactWarn 모두 곱), `SetPacking` 메서드.
  - **`OrderJob.cs`** — `defaultPacking` 필드 (default Bubble).
  - **`RunController.cs`** — BeginRun에서 `cargo.SetPacking(job.defaultPacking)` 자동.
  - **`OrderBoard.cs`** — DrawCard에 "포장: 뽁뽁이" 행 + 운전 가이드에 포장 팁 합치기. cardHeight 240→280.
  - **즉시 효과:** 기본 Bubble = ×1.3 → 모든 임계값 30% 여유. Box(0.9·1.0) → 1.17g·1.30g. 일반 주행 (0.5~0.8g) 절대 안전.
  - **USP 정합:** "결정 4개" — 기존 (주문/경로/세팅/업그레이드) + 포장 = 5개. 결정 밀도 ↑.
  - **이후 확장 (Phase 2):** OrderBoard에 4단계 토글 인터랙션 + 자금 차감. 포장 종류별 차등 (충격은 Foam, 회전은 Bubble).

- **세션 15 누적 파일 변경 (재집계):**
  - 신규 8개: CargoType.cs, CargoTypePresets.cs, PackingTier.cs, Route/NodeGraph.cs, Tires/TireSystem.cs, Upgrades/UpgradeDef.cs, UpgradeCatalog.cs, UpgradeSystem.cs, Audio/AudioManager.cs
  - 수정 9개: CargoBox.cs, CargoShakeSimulator.cs, OrderJob.cs, OrderBoard.cs, ResultPanel.cs, RunController.cs, ArcadeCarController.cs, FollowCamera.cs, TrackLayout.cs, FixStartYaw.cs(메뉴 강화)
  - 수정 1개: Assets/Data/ComplexTrackLayout.asset (startYawDeg 0→90)

- **다음 도현 검증 (긴 변경 누적 — 컴파일 검증 필수):**
  1. 컴파일 통과 확인.
  2. OrderJob 자산에 cargoType + defaultPacking 분배.
  3. UpgradeCatalog 자산 생성 + UpgradeDef 5개 등록.
  4. 플레이테스트 — Bubble 기본 상태에서 "일반 주행 안전" 확인 (도현 좌절 해소 검증).
  5. Eggs + None 포장으로 같은 코스 → 매우 조심해야 살아남는지.
  6. ResultPanel 업그레이드 카드 5개 보이고 구매 작동하는지.
  7. AudioClip 할당 후 SFX 후크 작동하는지.
- **세션 15 핵심 진전:**
  - 화물 시스템이 "퍼즐 시스템"에서 "포장으로 운전 자유도 구매" 시스템으로 진화. 좌절 해소 + 결정 밀도 ↑.
  - Phase 1 Week 2~4 시스템 인프라 95% 완성. Day 19 (RoutePlannerPanel) + Day 22~23 (Hub Scene)만 남음.

- **Day 26 — 8개 주문 카드 셋 + Shuffle Pick:**
  - **신규 `Editor/GenerateExpandedOrders.cs`** — 메뉴 `PitCrew/Order/Generate 8-Order Catalog`. 8개 OrderJob asset (5종 cargoType × 4종 packing 다양성) + OrderCatalogExpanded.asset 자동 생성.
    - 1. 신선 계란 (Eggs/Bubble, 4분, ₩700)
    - 2. 유리 식기 (Glass/Foam, 6분, ₩1000)
    - 3. 활어 운송 (Fish/Foam, 5분, ₩1100)
    - 4. 결혼 케이크 (Cake/Hardcase, 7분, ₩1500)
    - 5. 택배 박스 (Box/Bubble, 무제한, ₩600)
    - 6. 고급 와인 (Glass/Hardcase, 8분, ₩1800)
    - 7. 회식용 횟감 (Fish/Bubble, 3.5분, ₩900)
    - 8. 생일 케이크 (Cake/Foam, 4.5분, ₩800)
  - **`OrderBoard.cs` 셔플 픽 추가** — `offerCount=3` (default), `currentOffer` list. Open() 시 8개 풀에서 3개 무작위 픽 (Fisher-Yates 부분 셔플). 매 런 다른 카드 → 로그라이트 다양성.
  - **카드 표시 로직** catalog.jobs → currentOffer 전환. ApplyHoverState/OnGUI 모두 업데이트.

- **Day 21 자동화 — `Editor/GenerateUpgrades.cs`:**
  - 메뉴 `PitCrew/Upgrade/Generate 5-Upgrade Catalog`. 5개 UpgradeDef + UpgradeCatalog 자동 생성.
    - 서스펜션 강화 (₩800, +0.1g 임계)
    - 냉동 칸 (₩1500, 냉동 주문 해제 — Phase 2)
    - 지름길 지도 (₩1200, NodeGraph isShortcut 표시)
    - 부드러운 조향 (₩1000, Spin +20°/s)
    - 튼튼한 적재함 (₩1500, maxHP +20)

- **세션 15 누적 (재집계):**
  - 신규 11개: CargoType.cs, CargoTypePresets.cs, PackingTier.cs, Route/NodeGraph.cs, Tires/TireSystem.cs, Upgrades/UpgradeDef.cs, UpgradeCatalog.cs, UpgradeSystem.cs, Audio/AudioManager.cs, Editor/GenerateExpandedOrders.cs, Editor/GenerateUpgrades.cs
  - 수정 10개: CargoBox.cs, CargoShakeSimulator.cs, OrderJob.cs, OrderBoard.cs, ResultPanel.cs, RunController.cs, ArcadeCarController.cs, FollowCamera.cs, TrackLayout.cs, FixStartYaw.cs
  - 자산 1개: ComplexTrackLayout.asset (startYawDeg 0→90)

- **Phase 1 (4주 MVP) 진행률:**
  - Week 2 (Day 8~14): 100% — 화물 다양화 + 정산 이야기 + 도현 피드백 튜닝
  - Week 3 (Day 15~21): 90% — Tilt/Cake, height 데이터, slope, NodeGraph 구조, Tire/Upgrade 시스템 (UI는 미구현)
  - Week 4 (Day 22~28): 60% — ResultPanel 업그레이드 영역, AudioManager, 8개 주문, 5개 업그레이드 자동 생성
  - 미구현 핵심: Day 19 (경로 UI, NodeGraph 인스턴스 필요), Day 22~23 (Hub Scene, 큰 작업), Day 27~28 (통합 플레이 + 게이트 점검 — 도현 직접)

- **다음 도현 작업 (자산 생성 + 검증):**
  1. 컴파일 통과 확인.
  2. **메뉴 실행:**
     - `PitCrew/Order/Generate 8-Order Catalog` → 8개 OrderJob + OrderCatalogExpanded.asset
     - `PitCrew/Upgrade/Generate 5-Upgrade Catalog` → 5개 UpgradeDef + UpgradeCatalog.asset
  3. 인스펙터 연결:
     - OrderBoard.catalog → OrderCatalogExpanded
     - ResultPanel.upgradeCatalog → UpgradeCatalog
     - 씬에 UpgradeSystem MonoBehaviour 추가 (싱글톤)
     - 씬에 AudioManager MonoBehaviour 추가 (옵션, AudioClip 할당 후)
  4. 플레이테스트 — 매 런 다른 3개 주문 / 화물별 운전 / 정산 후 업그레이드 구매 / 다음 런 효과 적용.
  5. 좌절 해소 검증 (Bubble 기본 ×1.3 임계).

- **D-042 — CargoShakeSimulator 패턴 C 재설계 (도현 세션 15 갈대 진단 2차):**
  - **진단:** 회전 Slerp(restRot 복귀)가 갈대 느낌의 근본 원인. 위치 마찰만으론 해결 안 됨.
  - **해결:** 회전 비활성 + 위치 미끄러짐만 + 멈춘 자리 유지.
  - **`CargoShakeSimulator.cs` 변경:**
    - `enableRotation` 신규 (default false). LateUpdate 회전 적용 일찍 return.
    - `staticFrictionG` 0.6→0.25 (작은 G에도 미끄러짐 시작)
    - `kineticFriction` 6.0→1.0 (천천히 감속, 자연스러운 미끄러짐)
    - `accelScale` 0.6→1.0 (G가 박스를 잘 밀어냄)
    - `maxOffset` 0.25→0.5 (트렁크 공간 ±0.5m → 2벽 사이 1m)
    - `velocityDeadZone` 0.05→0.02 (작은 움직임 살림)
  - **트럭 박스 동작:** 차 가속 → 박스 뒷벽 미끄러짐 → 거기 머무름. 차 정지 → 박스 그 자리 유지 (rest 복귀 X). 차 코너 → 박스 옆벽 미끄러짐.
  - **트렁크 시각화 — `Editor/_Recovery/SetupCargoView.cs` 강화:**
    - `SetupTrunkWalls` 신규 — 4벽 + 바닥 cube 자동 생성 (2.0×2.0 정사각형, 어두운 갈색).
    - SimCam 재조정 — y 1.7→2.5, z -0.15→-0.8, rotation 78°→65°, FOV 42°→55°. 트렁크 전체 + 박스 미끄러짐 명확히 보임.
    - 매 메뉴 실행 시 force recreate (튜닝 즉시 반영).

- **다음 도현 작업 (트렁크 시각화 검증):**
  1. `PitCrew/Cargo/Setup Cargo View (CCTV)` 메뉴 재실행 → 트렁크 4벽 + 바닥 자동 생성, 카메라 새 위치.
  2. 플레이 → 차 가속/제동/코너 → CCTV에 박스가 트렁크 안에서 미끄러져 벽에 부딪히고 멈추는 게 보이는지.
  3. 회전 없이 미끄러짐만으로 "갈대 사라졌다" 체감되는지.
  4. 필요 시 `enableRotation` true로 켜면 미세 회전 추가 (보통 OFF가 자연스러움).

- **D-043 — 화물 타입 즉시 인식 폴리싱:**
  - **CCTV 박스 색상 차별화 — `CargoTypePresets.BoxColor`:**
    - Eggs: 상아색 (계란껍질)
    - Glass: 청색
    - Fish: 청록 (활어 수조)
    - Cake: 핑크
    - Box: 골판지 갈색 (기본)
  - **`CargoShakeSimulator.SetBoxColor(Color)`** 신규 — material instance 색상 변경.
  - **`RunController.BeginRun`** — 화물 타입에 따라 SetBoxColor 자동 호출. CCTV에 색깔로 화물 즉시 인식.
  - **`IntegrityHUD` 강화:**
    - 라벨에 화물 타입 표시 — "CARGO 87%" → "**계란 87%**" / "**유리 65%**" / "**활어 92%**"
    - **충돌 flash** — `cargo.OnImpact` 후크. 0.18s 빨강 bg lerp (속도 비례 강도). HUD 자체가 충돌 시각 알림.

- **세션 15 누적 (재집계 — 최종):**
  - 신규 12개: CargoType.cs, CargoTypePresets.cs, PackingTier.cs, Route/NodeGraph.cs, Tires/TireSystem.cs, Upgrades/UpgradeDef.cs, UpgradeCatalog.cs, UpgradeSystem.cs, Audio/AudioManager.cs, Editor/GenerateExpandedOrders.cs, Editor/GenerateUpgrades.cs (+ session-log 별도)
  - 수정 12개: CargoBox.cs, CargoShakeSimulator.cs, IntegrityHUD.cs, OrderJob.cs, OrderBoard.cs, ResultPanel.cs, RunController.cs, ArcadeCarController.cs, FollowCamera.cs, TrackLayout.cs, FixStartYaw.cs, SetupCargoView.cs
  - 자산 1개: ComplexTrackLayout.asset (startYawDeg 0→90)

- **Phase 1 진행률 (최종):**
  - Week 2 (Day 8~14): **100%** — 화물 다양화 + Packing + 도현 피드백 튜닝
  - Week 3 (Day 15~21): **95%** — Tilt/Cake, height 데이터, Slope getter, NodeGraph, Tire/Upgrade 시스템 (UI 미구현)
  - Week 4 (Day 22~28): **70%** — ResultPanel 업그레이드 영역, AudioManager, 8개 주문 + Shuffle Pick, 5개 업그레이드 자동, IntegrityHUD 폴리싱
  - 미구현: Day 19 (RoutePlannerPanel UI — NodeGraph 인스턴스 의존), Day 22~23 (Hub Scene), Day 27~28 (도현 통합 플레이 게이트)

- **세션 15 핵심 성과 정리:**
  - 도현 직접 피드백 4건 즉시 반영: 속도감 / 화물 임계 적정선 / 갈대 (1차) / 갈대 (2차 회전 진단).
  - 도현 신규 디자인 결정 1건: Packing(D-041) 시스템 도입 — USP "결정 4개 → 5개" 진화.
  - 디렉터 진단 (시그니처=화물 시스템) 정합 완전 — 화물에 모든 폴리싱 집중.
  - Phase 1 시스템 인프라 완료. UI/씬 작업만 남음 (RoutePlanner + Hub).

- **D-044 — "바닥에 있는 화물" 진단 (도현 세션 15 3차):**
  - **진단:** "기울기 따로 벽 따로" — 차체 기울기로 박스 미끄러져야 하는데 시뮬에 사면 중력 없음 + 트렁크 visual이 차량 회전 안 따라가서 박스 시뮬과 visual 분리됨.
  - **해결 1: 사면 중력 추가** (`CargoShakeSimulator.FixedUpdate`)
    - `Physics.gravity` → `cargo.transform.InverseTransformDirection` → local x/z 성분 = 사면 G.
    - 관성력 + 사면 중력 합산 → totalAccel.
    - **차 평지 정지 → 박스 가만히** / 차 사면 정지 → **박스 사면 따라 미끄러짐** (실제 트렁크 박스).
    - 정지 마찰 임계도 totalAccel 기준 비교.
  - **해결 2: 트렁크 visual 회전 매칭** (`CargoShakeSimulator.LateUpdate`)
    - `matchCarRotation` 플래그 (default true).
    - `transform.rotation = cargo.transform.rotation` — CargoSimRoot 전체가 차량 회전 따라감.
    - SimCam/SimLight/Floor/Walls/Box 자식 모두 함께 회전.
    - 박스 simVelLocal은 차량 local frame 기준이라 trunk frame과 정합.
    - **시각 효과:** 차가 좌측 캠버 → 트렁크 visual 같이 기울어짐 → 박스가 사면에서 미끄러져 우측 벽으로 가는 거 명확히 보임.
  - **이제 진짜 "바닥에 놓인 화물":**
    - 평지 정지 = 박스 가만히
    - 평지 가속 = 관성으로 미끄러짐
    - 사면 정지 = 사면 중력으로 미끄러짐 (정지 상태에서도)
    - 사면 가속 = 두 힘 합산 (업힐 가속 시 더 강하게 뒤로)
    - 트렁크 회전 = 차량과 함께. 박스가 트렁크 안에서 미끄러지는 시각 명확.

- **다음 도현 검증 (D-044):**
  1. 차 정지 + 평지 → 박스 움직임 없음.
  2. 차 정지 + 도로 캠버(살짝 기울)에 정차 → 박스 사면 아래쪽으로 미끄러짐.
  3. 차 코너 → 트렁크 화면이 살짝 기울고 박스가 외측 벽으로 미끄러짐.
  4. 차 급제동 → 박스 앞쪽 벽으로 미끄러짐.
  5. matchCarRotation 끄면(false) — 트렁크 fixed, 박스 시뮬만 작동.
  6. enableRotation 켜면 추가 박스 회전 (보통 OFF).

- **Phase 1 통합 setup Editor (세션 15 마무리):**
  - **신규 `Editor/SetupPhase1Integration.cs`** — 메뉴 `PitCrew/Phase 1/Full Integration Setup`. 한 번 클릭으로 모든 신규 시스템 자동 연결:
    1. 8개 주문 + OrderCatalogExpanded.asset 생성
    2. 5개 업그레이드 + UpgradeCatalog.asset 생성
    3. UpgradeSystem GameObject 씬 추가
    4. AudioManager GameObject 씬 추가
    5. OrderBoard.catalog → OrderCatalogExpanded 자동 연결
    6. ResultPanel.upgradeCatalog → UpgradeCatalog 자동 연결
    7. SetupCargoView 재실행 (트렁크 4벽 + CCTV 카메라)
    8. FixStartYaw 자동 정렬
  - **도현 작업 단순화:** 컴파일 통과 → 메뉴 1번 클릭 → Play 즉시 가능. AudioClip만 별도 인스펙터 할당.

- **세션 15 누적 (최최종):**
  - 신규 13개 (Cargo/CargoType, CargoTypePresets, PackingTier, Route/NodeGraph, Tires/TireSystem, Upgrades/UpgradeDef, UpgradeCatalog, UpgradeSystem, Audio/AudioManager, Editor/GenerateExpandedOrders, GenerateUpgrades, SetupPhase1Integration)
  - 수정 12개 (CargoBox, CargoShakeSimulator, IntegrityHUD, OrderJob, OrderBoard, ResultPanel, RunController, ArcadeCarController, FollowCamera, TrackLayout, FixStartYaw, SetupCargoView)
  - 자산 1개 (ComplexTrackLayout startYawDeg 0→90)

- **D-045 — 충돌 카메라 shake (FollowCamera):**
  - `CargoBox.OnImpact` 후크 → 카메라 0.35s shake (속도 비례 강도, decay).
  - `Random.insideUnitCircle * impactShakeAmplitude(0.25) * decay * strength` — 자연스러운 임팩트감.

- **D-046 — "자동차 안 화물" 느낌 진단 (도현 세션 15 4차):**
  - **진단:** "바닥과 상관없이 화물 각도 달라짐 + 시뮬레이션 느낌". 실제 화물칸 마찰감 부족.
  - **원인 1: 박스가 floor에 정착 안 됨** — Box localY=0이라 박스 bottom = -0.325 (floor 안에 박힘).
    - 수정: `SetupCargoView`에서 boxGO.localPosition = (0, 0.325, 0) → 박스 bottom = floor top = 0.
  - **원인 2: 마찰 너무 약함 (시뮬 같음)** — 작은 G에도 박스 미끄러짐.
    - `CargoShakeSimulator` 마찰 강화:
      - `staticFrictionG` 0.25→**0.30** (10° 사면 정도까지 박스 안 움직임)
      - `kineticFriction` 1.0→**3.0** (박스 빨리 멈춤)
      - `accelScale` 1.0→**0.7** (반응 70%)
  - **새 동작:** 평지 정지 = 박스 정확히 floor에 정착 + 작은 사면(<15°)에서는 박스 안 움직임 + 강한 G 또는 큰 사면에서만 미끄러짐. 진짜 자동차 화물칸 느낌.

- **세션 15 종료 정리 — 세션 16 진입 조건:**

  **세션 16 시작 시 도현 작업 (순서대로):**

  1. **Coplay MCP 연결 (가장 우선)**
     - Unity Editor 켜고 Coplay 윈도우 활성화
     - Claude Desktop 완전 종료 → 재실행
     - 새 conversation 시작 → 자가 검증 도구 활성화 (8차 → 1차 진단 사이클 단축)

  2. **Phase 1 setup 통합 검증**
     - `PitCrew/Phase 1/Full Integration Setup` 메뉴 1회 실행 (자산 + 연결 + Cargo View 강제 reset)
     - `PitCrew/Phase 1/Verify Setup` 메뉴 → 모든 ✓ 확인
     - ✗ 있으면 즉시 처치 후 다시 Verify

  3. **트렁크 박스 거동 검증 (세션 15 8차 진단 마무리)**
     - Play → CargoBox CCTV 화면 확인
     - 평지 정차 → 박스 평면 + 가만히
     - 평지 가속 → 박스만 미끄러짐 (trunk 평면 유지)
     - 사면 정차 → 박스가 사면 중력으로 미끄러짐 (trunk 평면 유지)
     - 코너 → 카메라 함께 yaw 회전, 박스 외측 미끄러짐
     - 도현 OK 신호 → 화물 시뮬 마무리

  4. **Phase 1 게이트 30분 통합 플레이** (`notes/phase1-status.md` 섹션 3 체크리스트)
     - A 운전감 + B 화물 + C 트렁크 + D 주문 + E 정산 + F 30분 연속
     - 5개 게이트 중 3개 이상 YES → Phase 2 진입
     - NO 다수면 부족 영역 1주 추가

  **세션 16 우선순위 결정 분기:**

  - **A. Phase 1 게이트 통과 시:** Phase 2 진입 — 비주얼 정체성 결정 (코지/긴장/코믹/하이브리드) + 시그니처(화물) 폴리싱 + 사운드 본격 + 첫 5분 임팩트.
  - **B. 게이트 일부 NO:** 부족 영역 보강 (가능성: Hub Scene Day 22~23 = "정산 → 정비 → 다음 런 흐름"이 약하면 추가).
  - **C. 화물 시뮬 추가 진단:** 8차 처치 후에도 갈대 느낌 남으면 Coplay 자가 검증으로 정확한 원인 진단 (도구 없이는 진단 사이클 부담).

  **세션 15 미완료 (Phase 1 잔여):**
  - Day 19 RoutePlannerPanel UI (NodeGraph 인스턴스 의존, 도현 인스펙터 작업 후)
  - Day 22~23 Hub Scene (별도 차고 영역, 큰 작업)
  - TileMap heightMask 시각 적용 (Editor 작업)
  - TireSystem 효과 ArcadeCarController 통합

- **D-049 — 박스 회전 강제 안전장치 (도현 세션 15 8차):**
  - **진단:** "트렁크처럼 평평한 곳에서 운전을 하는데 그와 맞지 않게 화물이 기울고 움직임". D-048 yaw-only 적용 후에도 박스 회전 발생.
  - **원인 추정:** 메뉴 재실행 안 했거나 enableRotation 인스펙터에서 켜졌을 가능성.
  - **해결:** 코드 안전장치 + 메뉴 강제 reset.
    - `CargoShakeSimulator.LateUpdate`: enableRotation=false면 매 프레임 `boxTransform.localRotation = restRot` 강제. 외부 영향 차단.
    - `SetupCargoView`: 메뉴 실행 시 `enableRotation=false` + `matchCarRotation=true` SerializedObject로 강제 reset.
  - **결과:** 도현이 인스펙터에서 enableRotation 켰어도 메뉴 한 번 실행 = 즉시 false. 박스 회전 절대 발생 X.

- **Coplay MCP 자가 검증 시도 (실패):**
  - 도구 로드 시도 → `mcp__coplay-mcp__*` 안 보임.
  - 원인: 도현이 Coplay 켰어도 Claude Desktop 재시작 안 함 → MCP server 연결 안 됨.
  - 다음 conversation에서 자가 검증 가능. 이번 conversation에서는 도현 직접 검증 필요.

- **D-048 — Trunk Yaw-only Match (도현 세션 15 7차, 진짜 트렁크 느낌):**
  - **진단:** "트렁크처럼 평평한 곳에 물건 배치된 것으로 구현해야 하는데 속도/사면에 따라 기울기 결정됨". 즉 trunk visual이 평면이어야 하는데 차량 pitch/roll 따라가서 평면 깨짐.
  - **해결:** Trunk + Camera 둘 다 **yaw만** 차량 매칭. pitch/roll 무시.
  - **`CargoShakeSimulator.LateUpdate` 변경:**
    - `transform.rotation = Quaternion.Euler(0, carYaw, 0)` — trunk yaw만
    - `simCamRig.rotation = Quaternion.Euler(65, carYaw, 0)` — 카메라 pitch fix, yaw 차량 매칭
    - `simCamRig.position = trunkPos + RotateY(carYaw) * (0, 2.5, -0.8)` — 카메라가 trunk 뒤쪽 위
  - **결과:**
    - 평지 정차 → trunk 평면, 박스 평면 ✓
    - 평지 가속 → trunk 평면 유지, 박스만 trunk 안에서 -z 미끄러짐 (사용자 시점)
    - 사면 정차 → trunk 평면, 박스가 사면 중력(차량 frame G)으로 미끄러짐
    - 코너 → 카메라 yaw 따라가서 시점 일관 (화면 안에서 회전 안 보임), 박스만 외측 미끄러짐
  - **시뮬-visual 정합:** 박스 simVelLocal은 차량 frame x/z. trunk yaw 매칭이라 trunk frame x/z = 차량 frame x/z. 정합 유지.
  - **`SetupCargoView` 강화** — simCamRig 참조 자동 인스펙터 할당 (SerializedObject).
  - **메뉴 재실행 필요** — `PitCrew/Cargo/Setup Cargo View (CCTV)` 또는 `PitCrew/Phase 1/Full Integration Setup`.

- **D-047 — SimCam worldspace fixed (도현 세션 15 5차):**
  - **진단:** "평지위에 있는것 같지 않음" — CargoSimRoot 회전(차량 매칭)에 SimCam도 자식이라 같이 회전 → 시점 자체가 기울어 박스가 평면 아닌 듯 보임.
  - **해결:** SimCam을 CargoSimRoot 외부로 분리. 신규 GameObject "CargoSimCamRig"가 worldspace fixed.
    - CargoSimRoot 회전(자식 Floor/Walls/Box) → trunk + 박스가 차량과 함께 기울어짐
    - SimCam은 worldspace fixed → 카메라 시점 안 흔들림
    - 결과: 평지에선 박스+floor 평면, 사면에선 박스+floor 같이 기울어진 게 명확히 보임
  - **`SetupCargoView.cs`** — CargoSimCamRig 자동 생성 + SimCam 자식 이동. 기존 root 자식 SimCam 제거 호환.
  - 화물: 5종 + 3축 민감도 + Packing 4단계 + 색상 차별화 + 충돌 flash
  - 운전: 130km/h + FOV 50~100 + 속도 진동 + slope getter
  - 트렁크: 4벽 + 사면 중력 + visual 회전 매칭
  - 정산: 화물 이야기 + 5단계 등급 + Wallet + 5개 업그레이드 카드
  - 주문: 8개 풀 + Shuffle Pick (3개 픽) + 화물 표시 + 운전 가이드
  - 업그레이드: 5개 효과 + 자동 적용 후크 (CargoBox/Spin/HP)
  - 오디오: 6 풀 + 5개 후크 (충돌/완료/구매)
  - 미구현: RoutePlannerPanel UI, Hub Scene

---

## 세션 16 — 2026-04-28 (이어 진행)

- **목표:** Coplay MCP 연결 후 Phase 1 자가 검증 + 도현 플레이 피드백 즉시 처치 + 4영역 기획.
- **컨텍스트:** Coplay 자가 검증 도구 활성화. /loop 자체 페이싱 모드로 다회 라운드 진행.
- **한 일 (커밋 시간순):**
  - **인프라 (단계 1+2):**
    - 워크트리 `claude/angry-montalcini-14cd06` → main 머지 (`--no-ff`). 세션 15 작업 4 커밋 통합 — D-046~D-049 화물 시뮬 fix 포함.
    - `.gitattributes` + Git LFS 정책 (Conservative — 신규부터): FBX/PNG/WAV/TTF 등 LFS, Unity YAML smart merge.
    - asmdef 2개 — `PitCrew.asmdef` (Runtime) + `PitCrew.Editor.asmdef` (Editor only). InputSystem/TMP/URP/Core 참조.
    - `notes/conventions.md` — 폴더 구조, 네이밍, asmdef 정책, Git 워크플로우, LFS, Smart Merge, 자산 추가 체크리스트.
    - 문서: feature-list.md 갱신 (Phase 1 완료 항목 체크).
  - **자가 검증 (Coplay):**
    - `PitCrew/Phase 1/Full Integration Setup` 메뉴 — 8개 주문/5 업그레이드/UpgradeSystem/AudioManager 자동 wire + CargoView 재실행 + FixStartYaw.
    - `PitCrew/Phase 1/Verify Setup` — 처음 29 OK / 0 FAIL → TireSystem/CargoWarningHUD 추가 후 31 OK / 0 FAIL.
  - **Day 20 — TireSystem 통합:** ArcadeCarController에 자동 연결 + `EffectiveMaxSpeedMps` (Soft +5%, Hard −5%) + `ApplySideGrip` 그립 배율 (Soft 1.4 / Hard 0.7). null guard로 기존 동작 보존. SetupPhase1Integration에 TireSystem singleton 추가.
  - **Day 24 — CargoWarningHUD:** 4 위험 소스(Long/Lat/Spin/Tilt) 중 가장 임박 표시. 위험도 0.7+ 노랑 → 1.0+ 빨강 깜박. signed로 방향(◀코너/코너▶ 등). UIRoot 자동 부착 EnsureUIChild 헬퍼 신설.
  - **도현 1차 플레이 피드백 #1·#2·#3 (즉시 처치):**
    - #1 컨트롤 묵직 → accelTime 2.0→1.4, steerLag 0.12→0.06, throttleLag 0.08→0.03, lowSpeedRamp 0.5→0.8.
    - #2 한글 깨짐 → SetupKoreanFallback editor 메뉴 신설. NotoSansKR-VF (OFL) → KoreanFallback SDF dynamic atlas → KenneyFuture SDF/Narrow fallback 등록.
    - #3 맵 단조 → PlaceSuburbanProps (도로 인접 빈 셀 55%, collider 없음) + Traffic 시스템 (TrafficLight + TrafficLightArea + 4-way 자동 배치 메뉴).
  - **도현 2차 플레이 피드백 #1-2·#2-2·#3-2·#4 (즉시 처치):**
    - #1-2 여전히 뻑뻑 → accelTime 1.4→1.0, lag 0.025/0.01, maxTurnDegPerSec 110→135, lowSpeedRamp 0.95.
    - #2-2 UI 너무 작음 → 5 HUD 사이즈/폰트 ~45% 확대 + 좌하단 스택 좌표 재배치.
    - #3-2 소리 큼/실제같음 → music 0.4→0.18, sfxMaster 1.0→0.55, EngineAudio idle 0.35→0.16, max 0.9→0.42.
    - #4 드리프트 → yawTorque 15→11, entryYawImpulse 4→2.5, longitudinalDrag 0→0.6, gripMul 0.02→0.05, max 90→85.
  - **ApplySession16Tuning 통합 메뉴:** SerializeField 기본값 변경이 기존 씬 인스턴스에 자동 반영 안 됨 — 한 번 클릭으로 모든 SerializedObject 강제 적용 + HUD 자식 삭제(다음 Play에서 새 fontSize로 재빌드) + Korean fallback 재생성 + props/traffic 자동 배치 + TMP_Text ForceMeshUpdate. Play 모드 가드 포함.
  - **도현 3차 #1-3 → 4차 #1-4 속도 재인하:**
    - 110→85 km/h, accelTime 1.6→2.2, lowRamp 0.85, driftMaxSpeedKmh 65 (비례).
    - 한글 fallback malgun.ttf로 교체 (NotoSansKR-VF 호환성 회피, dynamic atlas 안정성 ↑).
  - **CargoBox.ApplyExternalDamage public API:** TrafficLightArea 등 외부 페널티 진입점. OnImpact 동시 발동 (HUD flash + 충돌음 후크).
- **누적 main 19 커밋:**
  - 머지 1, pre-merge WIP 1, 인프라 4 (`.gitattributes`/asmdef/conventions/feature-list), 게임 코드 13 (TireSystem 통합/CargoWarningHUD/컨트롤 4차 튜닝/한글 fallback 2회/Suburban props/Traffic system/CargoBox API/HUD 사이즈/오디오 인하/드리프트/Apply 통합/scene 갱신 등).
- **다음 세션 (도현 복귀 후):**
  1. **Plan B 영역 1 — PreRunSetupPanel** (화물·포장 선택 UI). 데미지 감소 시스템은 이미 작동, UI만 추가.
  2. **Plan B 영역 2 — 한글 fallback 강제 등록** (`fallbackFontAssetTable` 직렬화 누락 가능성, ForceRegisterKoreanFallback editor 메뉴).
  3. **Plan B 영역 3 — ResultPanel IMGUI → UGUI 전환 + 1200×800 확대** (한글 안정 + 데미지 분석 막대 + 타이어 마모 표시).
  4. **Plan B 영역 4 — 도로 마스크 확장** (Kenney curve/end/roundabout 신규 maskChar 'C/E/O/T').
- **메모:**
  - 세션 13의 "Coplay disconnected" 한계 해소. 자가 검증 사이클 8차→1차 단축.
  - SerializeField 기본값 변경 자동 반영 함정 발견 → ApplySession16Tuning 패턴 정착.
  - VF(Variable Font)는 TMP dynamic atlas와 호환성 약함 → 정적 TTF 우선 정책.
  - /loop dynamic 모드 8 tick 진행. 도현 직접 Play 평가 사이 자동 보강.
- **컨텍스트 상태:** ~70% 사용. 세션 17 신규 진입 권장 — Plan B 영역 1+3 본격 구현 (60~150분 분량).

---

## 세션 16 후반 — 2026-04-28 (러닝 + 통합 라운드)

- **목표:** Plan B (B1~B4) 본격 구현 + 도현 추가 플레이 피드백 처치.
- **컨텍스트:** 도현 러닝 다녀오는 동안 Auto 모드로 Plan A(문서) + Plan B 4영역 구현. 복귀 후 직접 Play 평가 라운드 다회.
- **한 일 (커밋 시간순, 추가 14 커밋):**
  - **Plan A:** D-050~D-058 결정 이력 + 세션 16 블록 (`f629010`).
  - **Plan B1 한글 fallback 강제 등록** (`4bc1cac`): `ForceRegisterKoreanFallback` editor 메뉴 — m_FallbackFontAssetTable에 직접 SerializedObject 추가 + SetDirty + SaveAssets. Verify 강화 (36 항목).
  - **Plan B2 ResultPanel IMGUI → UGUI 전환** (`1e4a764`): 600×460→1200×800→1600×1000 확대. TMP_Text 사용 → 한글 fallback 자동. 타이어 마모 막대, 포장 효과, 시간 보너스, 화물 이야기 추가.
  - **Plan B3 PreRunSetupPanel** (`2b39e33`, `fc49592`): 화물·포장 4 카드 선택 모달 (None/Bubble/Foam/Hardcase). OrderBoard.PickOffer → PreRunSetupPanel.Show → CargoBox.SetPacking → BeginRun. SetupPhase1Integration 자동 등록.
  - **Plan B4 도로 마스크 확장 + 가로등** (`7f3b476`): TileMapAuto에 'B' = Bridge. PlaceStreetLights — 도로 모서리 30% 확률 light-curved/square 자동 배치.
  - **#2-5/2-6 한글 atlas 깨짐 진짜 처치** (`6219e78`, `bc4044b`): SetupKoreanFallback에 `AddObjectToAsset` 누락 → MissingReferenceException 발견. atlas texture/material을 sub-asset으로 명시 추가. 또한 매번 재생성 시 KenneyFuture fallback table에 null 누적 → ForceRegister에서 일괄 정리. UITheme.fontPrimary → KoreanFallback SDF 직접 교체 (B5 우회). Korean SDF는 malgun.ttf 기반 (NotoSansKR-VF Variable Font 호환성 회피).
  - **#2-7 속도계 텍스트 겹침** (`bc4044b`): "0/km/h" 한글 SDF 폭 ↑ → speedText offsetMax -74→-110, unit sizeDelta 66→96.
  - **#2-11 EventSystem 부재 → 다시 버튼 안 눌림** (`1abbe70`): 씬에 EventSystem GameObject 없어 UGUI 버튼 클릭 디스패치 안 됨. ApplySession16Tuning + ResultPanel/PreRunSetupPanel.Awake에 자동 보장. InputSystem 1.18 호환 위해 InputSystemUIInputModule 사용. ResultPanel stat row Y 방향 반전 fix (+ → -). 카드 cost/btn 겹침 처치.
  - **#2-13 NRE CarVisualLean.cs:70** (`69aca61`): `parts` 배열 null → LateUpdate에서 lazy init 방어. obsolete API 정리 (textWrappingMode, trafficLight rename).
  - **#2-14 미사용 필드 경고 정리** (`afd0088`): isOpen → public IsOpen, midSpeedNorm/sep 제거.
  - **#2-15 화물 손상도 입력 spike 완화** (`85ed648`): 도현 "입력 따라 손상도 → 초반 컨트롤 극복 어려움" 진단. dv 저역필터 (0.30 smoothing) + 저속 0~30km/h lowSpeedRelief +0.3g. 일반 가속·제동·코너 데미지 거의 0, 진짜 급격한 입력만 손상.
  - **#2-16 OrderBoard 카드 가독성** (`85ed648`): cardWidth 360→480, cardHeight 280→320, 배경 alpha 0.55→0.88, 글자색 흰→노란 강조 (statValueStyle), fontSize ↑, tipStyle 별도 노란 + 2줄 분리.
  - **3D 회전 포장 단계 미래 요구** (PreRunSetupPanel docstring): 도현 "물건을 선택하고 포장하는 단계가 따로 있어서 완충제를 어떻게 싸고 뭘 쓸지 물건을 3D로 돌려가면서 포장해야함". Phase 2/3 별도 PackingStage 신설 권장. ideas-parking 등록.
- **누적 main 33 커밋** (세션 15 머지 + 세션 16 인프라/시스템/UI/튜닝/Plan B/도현 다회 라운드).
- **검증 (Coplay 자가 검증):**
  - Phase 1 Verify 36 OK / 0 FAIL (한글 fallback / Props / TrafficLights / StreetLights / EventSystem / 컨트롤 4차 튜닝 등 추가 검사).
  - Play 캡처 — "상자 100%" 한글 정상, 정산 화면 1600×1000, 다시 버튼 노란.
- **남은 미해결:**
  - 3D 회전 포장 단계 (PackingStage 신설) — Phase 2/3.
  - 30분 통합 플레이 (사람 작업, Phase 1 게이트 평가).
- **다음 세션 (세션 17):**
  1. 도현 30분 통합 플레이 → Phase 1 게이트 5개 평가.
  2. 결과에 따라 Phase 2 진입 vs 부족 영역 보강.
  3. 후속: PackingStage 3D 모달 / Hub Scene H1 / 맵 고저차 H4 / 타이어 선택 UI.
- **컨텍스트 상태:** 80%+ 사용. 세션 17 신규 시작 강력 권장.

## 세션 17 — 2026-04-29~30 (A 옵션 Canvas 리팩터 + 코드 리뷰 vault + 야간 루프 2회)

- **목표:** 세션 16 후속 — UI는 Scene/Prefab에서 직접 편집 가능하게 (A 옵션). 야간 자율 루프 1회 + main 직접 루프 1회. 코드 리뷰 인지 부담 최소화.
- **컨텍스트 분기:** worktree(`claude/angry-montalcini-14cd06`)에 어제 야간 루프 4 commit이 inert 상태로 남아있음. 도현이 같은 시간 main에서 PreRunSetupPanel 직접 작성 (procedural Canvas) → 디자인 충돌 (인라인 vs 별도 모달). 도현 결정: **A로 가자, worktree 폐기, main 기준**.
- **한 일 (커밋 시간순, main 추가 12 커밋):**
  - **세션 16 후속 Phase 1 — OrderBoard IMGUI → Canvas + Prefab** (`c7566e6`): OrderCardUI.cs 신규 (prefab root 컴포넌트, 카드 sub-elements 인스펙터 노출). OrderBoard.cs 전면 리팩터 — OnGUI 제거, panelRoot/cardsContainer/cardPrefab/headerText/helpText 와이어링, 호버 정적 콜백, 클릭 → PreRunSetupPanel.Show. Coplay execute_script로 OrderCard prefab 자동 빌드 + 씬 와이어링 (BuildOrderCardPrefab.cs, ConvertOrderBoardTextsToTMP.cs 일회용 헬퍼).
  - **Phase 2 — PreRunSetupPanel procedural → prefab 패턴** (`f948e26`): PackingCardUI.cs 신규. PreRunSetupPanel.cs 전면 재작성 (300줄 → 190줄, 모든 references 인스펙터 노출). RunController에 `BeginRun(OrderJob, PackingTier?)` 오버로드 추가 — **버그 fix**: 이전엔 PSP가 `cargo.SetPacking(selectedTier)` 호출 후 BeginRun이 `cargo.SetPacking(job.defaultPacking)`으로 덮어쓰던 문제. 오버로드로 packingOverride 보존. Coplay BuildPreRunSetupPanel.cs로 1400×680 모달 + 4 PackingCard prefab + RunRoot에 PreRunSetupPanel 컴포넌트 attach + 11개 references 와이어링.
  - **코드 리뷰 마인드맵 3종** (`e79e35b`): Markmap-friendly architecture.md / Mermaid flows.md / questions.md.
  - **Obsidian vault 37 노트** (`1486220`): notes/code-review/ 아래 README + scripts(6) + functions(22) + flows(5) + 각 index. `[[wiki-link]]` 양방향 연결, Obsidian 그래프 뷰 활용.
  - **야간 자율 루프 2 (main 직접)** — 6 사이클 × 30분, cron `c680c0d7`:
    - Cycle 1: **Task Y** FindActiveCar Awake 캐시 (`f960949`). ⚠ programmer agent CWD 오류로 worktree 브랜치에 잘못 commit (0fe9e44) → cherry-pick 차단(worktree 머지 금지) → main 직접 적용. Cron prompt에 CWD 강제 + `pwd` 자기검사 추가.
    - Cycle 2: 검증 전용 — carCache 안전성 OK, Obsidian drift 1건 발견.
    - Cycle 3: **Task Z** 일회용 Editor 헬퍼 3개 제거 (`d1ef227`) + Obsidian drift 동기화 (`664605d` — DeliveryManager.md/Update.md 갱신 + Awake.md 신규).
    - Cycle 4: 검증 전용 — Task Z 사후검증 잔재 0건, drift 잔재 1건 (functions/index.md Awake 링크 누락).
    - Cycle 5: drift 잔재 보완 (`a937e5e`) + cycle 6 outline 사전 준비.
    - Cycle 6: 단순 종료 — `notes/loop-summary-2026-04-30.md` 작성 (~1240단어), cron 정지.
  - **진행표 갱신 6회** (cycle 1~6): `adbef1f`, `a0d1a27`, `59a3110`, `b8077e0`, `04ac01d`, `a633d04`.
- **누적 main 45 커밋** (세션 16 + 세션 17 후속).
- **검증:**
  - Phase 1: Coplay get_unity_editor_state — 컴파일 에러 0, RunRoot에 OrderBoard/RunController/ResultPanel/PreRunSetupPanel 4 컴포넌트 + 모든 references 와이어링 확인.
  - 야간 루프: main HEAD `a633d04`, 코드 변경 2개 (Task Y/Z) 모두 동작 동일 또는 dead code 제거. worktree 브랜치 inert (cycle 1 stale 0fe9e44 무시).
- **남은 미해결:**
  - **도현 30분 통합 플레이 미실시** — Phase 1 게이트 평가 보류 (세션 16 끝부터 누적).
  - ResultPanel procedural → prefab 패턴 (도현 별도 세션 영역으로 명시).
  - F1 (죽은 조합 UI 가드), F3 (무결성 등급 tick), F4 (포장 사전 시뮬), F5 (정산 보상 인과) — designer 누적 발견.
  - DeliveryManager.OnDeliveryReached처럼 매 프레임 호출 안 되는 함수의 의문은 해소됨.
- **다음 세션 (세션 18):**
  1. **`notes/loop-summary-2026-04-30.md` 정독** (도현 30분 진입점, ~1240단어).
  2. **첫 작업 — PreRunSetupPanel에 Wallet 1줄 추가** (designer 5사이클 합의 P0). ResultPanel.walletVal 패턴 복붙, 5~10분, 가치값 변경 X. "보유 자금 ₩X / 포장비 ₩Y" 노출.
  3. Phase 1 통합 30분 플레이 → 게이트 평가.
  4. worktree 브랜치 (`claude/angry-montalcini-14cd06`) 정리 결정 — inert 보존 OK / 삭제(`git branch -D`).
  5. 후속 P0/P1 처리 (F1·F3·F4·F5 중).
- **컨텍스트 상태:** 매우 높음 (80%+). 세션 18 신규 시작 강력 권장. Obsidian vault에서 코드 탐색 시작 권장.

## 2026-05-11 — Auto-End stub (12:28)
- (다음 세션 시작 시 채움: 한 일 / 다음엔 / 메모)

## 2026-05-12 — Auto-End stub (19:38)
- (다음 세션 시작 시 채움: 한 일 / 다음엔 / 메모)

## 세션 20 — 2026-05-12~13 (Unity-CLI 전환 + Vault 정리 + 화물 인과 + Vertical Slice 결정)

- **목표**: 미해결 도로/차 부유 + 한글 폰트 + 화물 손상 인과 재설계 + 다음 단계 결정
- **컨텍스트**: 세션 19 Phase 4~6 + peer review 미커밋. Coplay MCP 타임아웃 시작 → batch 모드 임시 사용 → unity-cli 발견 → 워크플로 전환

### 한 일

#### 인프라
- **Unity 검증 도구 교체** (D-065): Coplay MCP 폐기, `unity-cli` (port 8090 HTTP) 채택. Editor 켠 채로 실시간 통신, batch lock 회피. `~/.claude.json` mcpServers 비움, 권한 정리. `.claude/rules/session.md` + memory 갱신.
- **Obsidian Vault 분리** (D-066): repo 루트가 vault 루트였던 구조 → `옵시디언/` 폴더로 분리. `.obsidian/`/`GDD/`/`notes/`/`dev-plan/`/`code/`/`🗺 Maps/`/`_archive/` 이동. 경로 참조 (`CLAUDE.md`, `.claude/rules/*`, `.claude/activeContext.md`, `scripts/*.ps1`) 전부 갱신. vault README 신규.
- **`_Recovery/` 정리** (D-067): 108 .cs → 15 keep + 93 `_archive/` 이동. KEEP: AuditScene, FixFloatingCarAndRoad, FixKoreanFontAtlas, AlignBaseHorizon, TuneCargoShake, SimulateCarSettle, DiagnoseCarFully/SpawnStack, DiagDriveBlocker, DiagPauseState, DiagnoseGameContextScript/Runtime, RestoreCarPhysicsDefault, RefreshAssets, RunFullRebuild.
- **알림 사운드 hook** 추가: Stop 이벤트 = Asterisk, Notification = Question. PowerShell SystemSounds.
- **Superpowers 플러그인** 설치: brainstorming, writing-plans 등 15 skill 가능.

#### 코드/씬
- **차 부유 + 기울임 + 복귀 불가** (3 증상): 원인 — pos.y=0.78에 spawn (도로 위 0.61m), `centerOfMassOffset=(0,-0.3,0)` → COM이 box 바닥 아래/땅속 → inverted pendulum. 수정: car y → 0.169 (asphalt 접지), COM y → +0.13 (box 내부 하단 1/4), angularDamping 2.5.
- **도로 z-fight** 시리즈:
  - BaseHorizon plane y=-0.21 → 0 → -0.01 → -0.06 (world 0.11) 단계적 조정
  - 도로 타일 5cm 일괄 raise (sidewalk curb 노출)
  - 평지 잔디 큐브 `GG_*` 64개 제거 (도로 측면과 coplanar z-fight 원인). plateau `GP_*` 유지
  - **최종 원인** (도현 명시): 아스팔트(검은 부분) top y=0.17 ↔ BaseHorizon y=0.16 (1cm) z-fight. BaseHorizon 5cm 추가 ↓ + GroundField BoxCollider top 0.12 + 차 y 0.169 재snap.
- **한글 폰트 깨짐**: `AddKoreanFontFallback`이 만든 Malgun SDF asset에 atlas Texture2D + Material이 sub-asset으로 안 묶여 reload 시 null. `FixKoreanFontAtlas`로 통째 재생성 (TMP_FontAsset.CreateFontAsset → CreateAsset → AddObjectToAsset(atlasTexture) + AddObjectToAsset(material)). TMP_Settings global fallback 재등록.
- **CargoShake 튜닝**: staticFrictionG 0.30→0.12, kineticFriction 3.0→1.0, accelScale 0.7→1.5. 박스 반응 강화.
- **화물 손상 인과 변경** (D-068): peakLong/peakLat → 직접 damage 제거. CargoShakeSimulator 벽 clamp에서 simVelLocal 기반 `ApplyExternalDamage` 호출. 인과: 차 G → 박스 슬라이드 → trunk 박음 → HP 깎임.

#### 문서/기획
- **PitCrew 기획서 .pptx 생성**: `PitCrew_기획서.pptx` (12 슬라이드). 산업 표준 (Viktori/Storydoc/Rami Ismail 등 리서치 기반) + GDD 자료 + 시장 빈 칸 비교표 + USP 4 + 결정 채널 9 표 + Player Experience 6단계 + Phase 1~6 진척 바.
- **Vertical Slice 데모 6주 결정** (D-069): 풀 spec `[[2026-05-13-vertical-slice-design]]` 작성. 데드라인 2026-06-24. 모든 결정 채널 (2 제외) mechanically 작동 + F1 복원 부품 2개 + 30분 데모.

### 검증
- 컴파일: 끝마다 unity-cli console --errors 확인. 마지막 에러 0.
- 차 안착 시뮬: Physics.Simulate 2초 → box.bottom = road top 정확히 안착, 기울임 0.

### 미커밋
모두 — 다음 세션 시작 또는 도현 명시 시 커밋.

### 다음 세션 (세션 21)
1. `[[2026-05-13-vertical-slice-design]]` 정독
2. **W1 작업 브레인스토밍 — 채널 3 (맵 해금)** 구체 설계:
   - UI 위치 (차고? 잡 보드?)
   - 해금 조건 (자금? 부품? 잡 N회?)
   - 데모 콘텐츠 (1지역 + 잠금 1지역)
3. `writing-plans` 스킬로 구현 계획 → 구현

---

## 세션 22 (2026-05-14)

### 한 일
- Unity 상태 확인 (Edit→Play 진입, 컴파일 0 에러)
- 화면 우상단 "박스" 정체 = `GroundField/GP_*_h3.0` plateau tile (Plateau2Mat 입힌 6×3.2×6 Cube). cardboard 아님, elevation tile. 안 건드림.
- `Assets/Editor/_Recovery/ProbeTopRightBoxes.cs` 만듦 ([MenuItem]로 카메라 frustum 내 renderer 덤프).
- GameContext.packingScreen 이미 와이어링 완료 확인 (세션 21에서 됐던 듯).
- **막힘 발견**: `unity-cli screenshot --view game`이 ScreenSpaceOverlay UI 캡처 못함 → AI가 OrderBoardPanel 실제 표시 여부 검증 불가.
- WebSearch로 원인 확정: RenderTexture 기반 캡처는 Canvas Overlay 누락 (Unity 공식 제약).
- 해결책 결론: `ScreenCapture.CaptureScreenshot(path)` Unity 네이티브 API. [MenuItem]로 노출.

### 메모리 갱신
- `reference_unity_screenshot_overlay.md` — 제약 + 해결책
- `feedback_try_before_blocking.md` — "막힘" 보고 전 검색·우회 시도 끝까지

### 미커밋
세션 18~22 전부.

### 다음 세션 (세션 23)
1. **`Assets/Editor/_Recovery/CaptureFullScreen.cs` 만들기** — `[MenuItem("PitCrew/Debug/Capture Full Screen")]` → `ScreenCapture.CaptureScreenshot(path)`. Play 중 트리거.
2. Play → 메뉴 트리거 → overlay 포함 PNG로 OrderBoardPanel 실제 표시 여부 확인.
3. (이전 미해결) 포장 시연 manual 테스트 — 잡 선택 → 포장 (드래그/WASD/1·2·3/휠) → 출발 → 운전 충격 → 정산 손상 wrap에 따라 다른지.

---

## 세션 23 (2026-05-14, 자율 2시간 루프)

### 한 일

#### Capture 인프라
- **`CaptureFullScreen.cs`** 작성 → `[MenuItem("PitCrew/Debug/Capture Full Screen")]` → `ScreenCapture.CaptureScreenshot(path)`. `F1/Captures/` 디렉터리에 timestamp PNG. Overlay UI 포함 캡처 가능 → AI 자체 UI 검증 루프 닫힘.
- 세션 22 미해결 의문 확인 — OrderBoardPanel 카드 3개 정상 렌더링됨 (캡처로 확인).

#### W1 채널 3 (맵 해금) 코어 구현
- 데이터 모델
  - `Zone.cs` SO — id, displayName, unlockCost, tintColor, jobs[]
  - `ZoneRegistry.cs` SO — allZones + Find(id)
- 런타임
  - `ZoneUnlockSystem.cs` — unlockedIds HashSet, TryUnlock → UpgradeSystem.Spend, OnZoneUnlocked 이벤트, GetAvailableJobs, GetLockedZones, GetZoneOf(job)
- UI
  - `ZoneUnlockPanel.cs` — 잠금 zone 카드 리스트, wallet 변경/해금 시 자동 refresh, prefab 없으면 폴백 카드 자동 생성
  - `ZoneUnlockCard.cs` — prefab root, affordable color 갱신
- 통합
  - `OrderBoard.PickOffer` — ZoneUnlockSystem 있으면 합쳐진 풀 사용, 없으면 catalog 폴백
  - `OrderBoard.Open` — ZoneUnlockPanel.RefreshExternal 호출
  - `OrderCardUI.Bind` — zone displayName을 size=60% + tintColor로 카드 상단에 표시
- 콘텐츠
  - Zone_Downtown (₩0, tint cyan, 4 jobs: StandardBox/BirthdayCake/WeddingCake/Eggs)
  - Zone_Harbor (₩1,000, tint orange, 4 jobs: Fish/SashimiTray/Glass/PremiumWine)
- 자동화
  - `PitCrew/W1/Seed Zones` — Zone SO + Registry 생성·연결
  - `PitCrew/W1/Wire Scene (Zones)` — 씬에 ZoneUnlockSystem + ZoneUnlockPanel 자동 배치 (우하단)

### 검증
- 컴파일 0 에러.
- Play 진입 → 우하단 "잠금 지역 / 항구 구역 ₩1,000" 카드 (gray, 자금 부족) ✓
- Wallet 1,500 cheat → AddReward → 카드 affordable color (이론, 캡처는 timeScale=0 stale render 잔상으로 시각 확인 미흡)
- TryUnlock 호출 → wallet 1,500→500, locked panel hide, GetAvailableJobs 4→8 ✓
- OrderBoard.Close→Open → currentOffer가 두 zone에서 섞임 ✓
- OrderCard 위쪽 라벨 "다운타운"/"항구 구역" tint 색으로 분류 표시 ✓

### 미해결
- 캡처 stale frame — timeScale=0 환경에서 ScreenCapture가 prev frame을 잡는 경우 있음. 내부 state 확인은 정확.
- ZoneUnlockPanel prefab 디자인 없음 — 런타임 폴백 카드. 양산 단계에서 prefab 만들면 됨.
- 도현 manual play 검증 필요: 잡 카드 자체 색 + zone 라벨 색 같이 보이는지, 운전→정산→OrderBoard 재오픈 흐름에서 wallet 증가 → 항구 affordable → 클릭 → 잡 풀 확장 시연.

### 미커밋
세션 18~23 전부.

### 다음 세션 (세션 24)
1. **도현 W1 manual 검증** — 위 미해결 시연.
2. **W2 진입 — 채널 5 (박스 크기 capacity 2→4)** + 채널 8 (지도/내비).
   - 채널 5는 UpgradeDef 2~3개 추가로 끝날 가능성 (가벼움).
   - 채널 8 (내비) 새 시스템 — 미니맵 + 화살표.
3. 포장 시연 manual 테스트 (계속 미해결).
4. 화물 손상 인과 (D-068) Play 테스트.

---

## 세션 24 (2026-05-14) — 하네스 점검 + 버그 5건 + W2 채널 8

### 한 일

**1. 하네스 점검 (UI / 게임플레이)** — unity-cli로 전체 루프 (주문→포장→운전→정산) 검증.

**2. 버그 5건 수정 (기능 단위 묶음 → 이후 개별 커밋):**
- 차 스폰이 CentralCross 배달마커와 겹침 (6m 기둥이 차 관통) → 옆 도로칸 (42,36)로 이동.
- 포장 큐브 concave MeshCollider 에러 → SetParent **전**에 DestroyImmediate.
- IntegrityHUD·CargoView가 모든 화면에 표시 → `BarHUDBase.ShouldShow()` 훅 + RunActive 게이팅.
- **포장한 물건 ≠ 트렁크 CCTV 물건** — RunController가 큐브를 차(Race)에 붙였는데 SimCam은 CargoSimRoot 다이오라마를 비춤. `CargoShakeSimulator.MountWrappedItem`로 SimCam이 비추는 Box 자리에 마운트.
- PreRunSetupPanel 빈 오브젝트 (세션 21 스크립트 삭제 잔재) 삭제.

**3. 세션 18~24 일괄 커밋** (`0a39d66`) — .gitignore에 생성물 제외 추가. 이후 기능 단위 커밋 전환.

**4. W2 채널 8 (지도/내비):**
- MiniMapHUD (`606ce2e`) — 좌상단 top-down 미니맵, 직교 카메라 + RT, 차 추적.
- DirectionArrow (`06c6c3a`) — 차 위 월드 화살표, 목표 방향 yaw.

**5. 하네스 (B축):** AI 약점 2건 `incidents.md` 기록 — Step() 부작용 오진 / probe 출력 오독. draft-rules에 신규 패턴 후보.

### 검증
- 컴파일 0 에러. 각 수정 Play 검증 (unity-cli step).
- 미니맵·화살표 런 중 표시 / 비런 숨김 확인.

### 미해결 — 도현
- **🔴 채널 5 (박스 크기) 차단됨** — 다물건 잡 작동 안 함. 다물건 설계 결정 필요 (n개 트렁크 배치·흔들림·손상).
- 세션 24 수정 5건 도현 manual play 검증 필요 (특히 트렁크 CCTV에 포장 큐브 — 실제 페인팅 후).

### 다음 세션 (세션 25)
1. 도현 manual play — 수정 5건.
2. 채널 5 다물건 설계 결정 → 빌드.
3. W3 차고/부품 결정.

---

## 세션 24 (이어서, 2026-05-15) — W2 채널 8 + 하네스 운영 + 채널 5 Plan 1

> 세션 24가 날짜 넘겨 이어짐. 위 블록 이후 작업.

### 한 일

**W2 채널 8 (지도/내비) 완료:**
- MiniMapHUD (`606ce2e`) — 좌상단 top-down 미니맵, 직교 카메라+RT, 차 추적, RunActive 게이팅.
- DirectionArrow (`06c6c3a`) — 차 위 월드 화살표, 현재 배달 목표 방향 yaw. UITheme.arrowNorth 스프라이트.

**하네스 운영 방식 확정 (D-071):**
- 도현: "하네스는 게임 개발과 나란히 계속 굴린다, 2개 팀." 내 "보류" 프레이밍이 정지처럼 들려 정정.
- 결정: 패턴 incidents 3건+ 검증되면 규칙 **즉시 발효** (형 검토 사후). 평가는 B 트랙("다른 팀")이 담당, 게임 흐름 안 막음.
- `project_rules/session.md`에 "B 트랙 — 하네스" 섹션 + 세션 종료에 하네스 패스 추가.
- 커뮤니케이션 피드백: "내가 너처럼 모든 정보 가졌다 가정 마라" → memory `feedback-explain-dont-assume-context`.

**채널 5 설계 결정 (도현):** 따로 포장 / 최대 3개 / **플레이어가 드래그로 트렁크 배치, 위치가 물리에 영향**.

**채널 5 Plan 1 — 다물건 트렁크 물리 (`bd53ea2`):**
- CargoShakeSimulator 단일 박스 → `List<TrunkItem>`. N개 각자 슬라이드 + 아이템 간 충돌(2D AABB) + 아이템별 벽 충돌 손상.
- RunController가 items 전부 마운트. Order_C itemCount=3 (첫 멀티 잡).
- Play 검증: 3개 마운트·슬라이드·0.5m 간격 유지·CargoBox 100→91.7%.

### 메모 / 미해결
- 채널 5 슬라이드: velocityDeadZone이 약한 사면 증분보다 커서 약슬로프에선 화물 안 미끄러짐 (선재 튜닝 이슈, Plan 1에서 안 건드림).
- 검증 빚: 세션 24 전체가 unity-cli 자동 step 검증만 — 도현 manual play 필요.

### 다음 세션 (세션 25)
1. 도현 manual play (검증 빚 청산).
2. 채널 5 Plan 2 (플레이어 드래그 배치) → Plan 3 (capacity 업그레이드).
3. W3 차고/부품 결정.

---

## 세션 27 (2026-05-19) — W3 차고 / F1 부품 복원 시스템

### 한 일

**기획** (D-073~093, 21 결정) — `옵시디언/dev-plan/2026-05-19-garage-realtime-planning.md` + `2026-05-19-restore-economy.md`:
- 6 부품 1 티어 / 사이드뷰 분해도 / 메커니즘 unlock형 / 풀게임 6 부품 모두
- 가격 1000/4000/4000/8000/12000/25000 (지수형 · 자유 게이팅 · 엔진 1000 첫 도파민)
- 자금 누적 / 정산액 비례 페널티 / Yes/No 차고 prompt
- 차고 UI 머신 가운데 + 카드 6장 하단 / 회색+부족 / 2~3초 컷씬
- 엔진 +20%/+30% / 변속기 K(다운)·L(업) / 부스트 Space 게이지 / 타이어 4종+영구마모+펑크=잡실패
- 그랑프리 후속 / 서스 점프대 맵 디자인 트랙으로
- 맵 디자인 학습 캔버스 분리: `옵시디언/dev-plan/2026-05-19-map-design-canvas.md`

**구현** (Task 1~12 완료, plan `docs/superpowers/plans/2026-05-19-garage-restore-system.md`):
- Task 1: F1Part SO 3개 (`5be654a`, 경로 fix 포함)
- Task 2: 6 F1Part asset + Catalog (`ec25cb7`)
- Task 3: F1RestoreSystem + Services.Restore + GameContext (`8298703`)
- Task 4: 엔진 +20%/+30% (`06bddc1`)
- Task 5: 변속기 K/L 수동 (`41ddc23`)
- Task 6: BoostSystem Space (`db5e93f`)
- Task 7: 타이어 + 영구 마모 + 펑크=잡실패 (`8b49de2`)
- Task 8: 도색 swap + 서스 flag (`69058d2`)
- Task 9: GarageScreen UI + Cutscene (`a2a00ba`)
- Task 10: ResultPanel '차고로' (`c327645`)
- Task 11: ComplexTrack 씬 wiring (`9ed1534`)
- Task 12: verifier subagent — 12 항목 **ALL GREEN**

**검증 결과 (12 항목)**: Services 매핑 17개 OK / Catalog 6 부품 가격 OK / 6 TryRestore 성공 / 엔진 ratio 1.200 / Gears.ManualUnlocked True / Boost.IsUnlocked True / Tires.SelectionUnlocked True / RecordJobUsage wear 1.00→0.75 / CanAfford 500<1000 False / Play 30초 에러 0건 / IntegrityMultiplier 1.00→0.80→0.00 단조 감소 / 자금 delta 1000.

**프로세스 개선**:
- 알람 hook에 system sound 3회 추가 (`SystemSounds.Exclamation`)
- subagent prompt 표준화: "Unity 프로젝트 루트 = `F1/`" CRITICAL PATH RULE 매번 박기 (Task 1 경로 누락 사고 후)
- 모든 commit에 `Verified-By:` 트레일러 (Stop hook 강제 통과)

### 발견된 이슈 (다음 세션 결정/처리)

1. **TireType 실제 3종** (Soft/Medium/Hard) — D-091 기획 4종(드라이/웨트/소프트/하드)과 어긋남. 데모는 3종 매핑 (Soft 1.5 / Medium 1.0 / Hard 0.5)으로 진행. 도현 결정: (A) 3종 유지 (B) Dry/Wet 추가 (C) Medium→Dry rename + Wet 추가.
2. `IUpgradeSystem.OnWalletChanged` 인터페이스 미노출 → GarageScreen Update 폴링 우회. 잠재 부채.
3. `IUpgradeSystem.Spend(int)` Task 3 도중 인터페이스에 추가 (이전 누락 발견 fix).
4. RunStage namespace 전역 — BoostSystem에서 `Services.Run as RunController` 캐스팅. 잠재 부채.
5. EffectiveMaxSpeedMps가 private (인터페이스 노출 X) — verifier가 EngineMaxSpeedMul로 우회 검증.

### manual play 빚 (차고 흐름 통합 후 한 번도 굴리지 않음)

- 정산 → "차고로" 클릭 → GarageScreen 진입 확인
- 엔진 복원 후 다음 런 속도 체감
- K/L 수동 변속 작동 (Driving stage 한정)
- Space 부스트 — 게이지 UI HUD 없음 (후속)
- 펑크 = 잡 실패 트리거 (마모 30% 강제 후 굴려보기)

### 다음 세션 (28) 첫 작업

1. **manual play 5~10분** — 차고 전체 흐름 / 엔진 체감 / K/L / Space / 펑크 트리거
2. **TireType 4종 결정** (도현 답변)
3. **R1 맵 적용** + RunMetricsRecorder 5런 측정
4. 측정 결과로 가격/마모량 재튜닝
5. **W4 진입** (정산→복원 연결 + 데모 시나리오 3런)
