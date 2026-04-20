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
