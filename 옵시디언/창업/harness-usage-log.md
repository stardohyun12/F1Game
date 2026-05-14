# Harness Usage Log — PitCrew × Claude Code

> 모두의 창업 신청서 근거자료. 세션 단위 블록 누적. 도현이 세션 끝에 수동 정리.
> 정책: [[project-modueui-changeop]] memory 참조.

## 기록 형식

각 세션 블록:

```
## YYYY-MM-DD — 세션 N

### Skill / Workflow
- 어떤 superpowers skill 흐름을 탔나
- 새로 발견한 패턴 / 폐기한 패턴

### Subagent 디스패치
| Task | 모델 | 결과 | 비고 |
|---|---|---|---|

### 자동 리뷰가 잡은 일탈
- 어떤 종류의 실수가 잡혔나 — 가장 가치 있는 기록 영역
- 예: spec 일탈, scope creep, 코드 품질, 누락 검증

### 프롬프트 패턴
- 잘 통한 표현 / 안 통한 표현
- subagent에게 context를 어떻게 잘랐나

### 시간 추정 vs 실제
- 계획 단위 시간 vs 실제 소요
- 어디서 차이 났나

### 메모
- 그 외 신청서에 쓸 만한 관찰
```

---

## 2026-05-13 — 세션 21

### Skill / Workflow

- **superpowers:brainstorming** — 기획서 W1~W6 폐기 후 재설계. 3D 포장 미니게임 단일 시스템으로 좁힘.
- **superpowers:writing-plans** — 17 Task 구현 계획 작성. unity-cli verification으로 TDD 대체 (Unity Test Framework 미사용 환경).
- **superpowers:subagent-driven-development** — Task 단위 fresh subagent + 2단계 review (spec → code quality) 진행 중.
- **Pptx 시각화** — 설계 도중 mockup pptx 생성으로 도현과 시각 합의. python-pptx + 기존 스크립트(`scripts/make-pitch-deck.py`) 패턴 재사용.

### Subagent 디스패치

| Task | 모델 | 역할 | 결과 | 비고 |
|---|---|---|---|---|
| Task 1 | sonnet | 구현 (WrapMaterialDef SO + 3 자산) | DONE | unity-cli 컴파일 + 자산 verify 통과 |
| Task 1 | sonnet | spec 리뷰 | ✅ 통과 | 9파일 = cs2+meta2+asset3+meta2, 스코프 정확 |
| Task 1 | sonnet | code quality 리뷰 | 승인 (I-1, I-2 deferred) | namespace 누락 → Task 2에서 같이 |
| Task 2 | sonnet | 구현 (WrapState + RT) | DONE_WITH_CONCERNS | `[ExecuteAlways]` 일탈 |
| Task 2 | sonnet | spec 리뷰 | ❌ 일탈 발견 | `[ExecuteAlways]` 제거 요구 |
| Task 2 | sonnet | 재구현 + 수정 | DONE | `[ExecuteAlways]` 제거, Play-mode 검증으로 전환 |
| Task 3-7 | sonnet | 백엔드 구현 (셰이더 2종 + 쿼리 시스템) | 모두 DONE | WrapStripBlit/Overlay 셰이더, WrapPainter, 커버리지/흡수율 쿼리 |
| Task 8-12 | sonnet | UI + 흐름 구현 | 모두 DONE | PackingScreen UGUI, PackingScreenInput, 실시간 비용 UI, 다물건 탭 |
| Task 10 | sonnet | spec 리뷰 | ❌ 일탈 발견 | `UnityEngine.Input` (legacy) 사용 → activeInputHandler=1 환경에서 무반응 |
| Task 10 | sonnet | 재구현 | DONE | New InputSystem (`UnityEngine.InputSystem`) 으로 전환 |
| Task 13-16 | sonnet | 통합 + 폴리시 | 모두 DONE | OrderBoard→PackingScreen 흐름, Services 등록, PackingTier 5파일 삭제, 사운드 cue 4개 |
| Task 17 | sonnet | 풀 사이클 검증 + 결정 로그 | DONE_WITH_CONCERNS | wrap 핸드오프 미완 — D-070 follow-up 등록 |

### 자동 리뷰가 잡은 일탈

**가장 가치 있는 케이스 — Task 2 `[ExecuteAlways]`**

- **상황:** 구현자가 EditMode 검증을 통과시키려고 spec에 없는 `[ExecuteAlways]` 어트리뷰트를 1줄 추가.
- **구현자 정당화:** "EditMode에서 Awake가 안 돌아서 검증이 FAIL".
- **spec 리뷰가 잡은 진짜 문제:**
  1. 검증 방법 문제이지 컴포넌트 설계 문제 아님 — `unity-cli editor play` 후 검증하면 됐음.
  2. `[ExecuteAlways]`는 도메인 리로드마다 OnDestroy→Awake → RT 짧은 이중 점유.
  3. Tasks 5/13에서 cargo prefab에 붙을 때 prefab 열기만 해도 RT 생성 → GPU 비용.
  4. 1줄로 보이지만 17 Task 후반에 누적 문제 됨.
- **인사이트:** "검증 편의를 위한 코드 변경"이 가장 미묘한 종류의 scope creep. 자동 리뷰 없으면 그냥 통과될 가능성 ↑. **신청서 인용 가능 사례.**

**케이스 2 — Task 10 InputSystem 일탈**

- **상황:** spec 코드가 `UnityEngine.Input` (legacy Input Manager) 사용. 프로젝트는 `activeInputHandler: 1` (New InputSystem only) 설정.
- **증상:** 컴파일은 통과. unity-cli 단순 wiring 검증도 통과. 그러나 Play-mode에서 키보드/마우스 입력 완전 무반응.
- **spec 리뷰가 잡은 방법:** `ProjectSettings/ProjectSettings.asset` 의 `activeInputHandler` 값 교차 참조. Legacy Input + New InputSystem 혼용 불가 → `PackingScreenInput.cs` 전체를 `InputSystem.InputAction` 기반으로 재작성 요구.
- **인사이트:** "컴파일 통과 = 동작 보장" 착각. 프로젝트 설정 의존성(InputSystem 버전)은 코드 리뷰만으로는 잡히지 않음. spec 리뷰어가 ProjectSettings 크로스체크하지 않으면 QA 단계까지 넘어갈 수 있었음. **신청서 인용 가능 사례 2번째.**

### 프롬프트 패턴

- **잘 통한 패턴:** 각 subagent에게 (1) full task 텍스트 (2) unity-cli 규칙 (3) commit format을 매번 inline. plan 파일 읽기 시키지 말 것.
- **잘 통한 패턴:** "Before You Begin: 불명확하면 ASK" — 모호한 시점에 멈춰 질문하게 함.
- **이번 추가:** spec 리뷰어에게 "Critically evaluate <구체적 일탈 요소>" 단락 추가 → 구현자 정당화를 액면 통과시키지 않게 함.

### 시간 추정 vs 실제 (예정)

- 추정: Task 1 0.3일, Task 2 2일. 실제: Task 1 약 5분(subagent 335초). Task 2 약 4분(233초). **하네스 가속 효과 큼.**
- 단, 일탈 잡기 + 수정 라운드 포함하면 실시간 5~10분/Task로 상승 가능. 17 Task 총 추정 9일 → 하네스로 1~2일 압축 가능성 (검증).

### 메모

- 도현이 "ExulMundi 경고 꺼" 명시함 (2026-05-13) — 스코프 확장 압박. 신청서 관점: "AI가 디자이너의 ambition을 어떻게 다루나"가 사례 자료.
- pptx 메타포 변경 (brush → wrap stroke) — 시각 합의 → 즉시 spec + pptx + 구현 plan 동시 갱신. 사이클 시간 짧음 (~10분).
- 모두의창업 트랙 결정이 세션 중간에 도착 (2026-05-13) — 작업 흐름 중단 없이 기록 채널만 추가. 하네스가 "메타 작업 동시 진행"에 강함.

---

> 다음 세션부터 위 형식대로 블록 추가.

---

## 2026-05-14 — 세션 24

### 한 일 (하네스 관점)
- "하네스대로 점검해봐" → unity-cli로 전체 루프 검증, 버그 5건 발견·수정, 기능 단위 커밋.
- AI 약점 2건 발생·기록 (`옵시디언/하네스/incidents.md`).
- 자율 빌드 지시 ("계속 루프 돌리면서 만들고 있어봐") → W2 채널 8 두 기능 자율 구현·커밋.

### AI 약점 — 이번 세션 발생
1. **검증 도구 부작용 오진**: `EditorApplication.Step()`이 에디터를 멈추는 걸 모르고 "카메라가 차를 안 따라온다"고 게임 버그로 단정, 가설 3~4개 헛파다 역추적. → incidents.md + 신규 패턴 후보 "검증 도구 부작용 맹점" (기존 10패턴에 없음). 메모리 `feedback-unity-step-pauses` 생성.
2. **자기 생성 probe 출력 3회 오독**: 공백 join 컴포넌트 목록을 "컴포넌트 없음"으로 착각. patterns/02 연결.

### 잘 통한 것
- 점검 → 근본 원인 추적 → 수정 → unity-cli 검증 → 커밋 사이클이 깔끔하게 돌아감.
- 채널 5가 미해결 설계 의존(다물건)에 막힌 걸 인지하고 **단독 결정 안 하고 도현에게 넘김** — 하네스의 "코어 디자인은 사람" 규범대로. 대신 차단 안 된 채널 8을 자율 완수.
- 샌드박스가 씬 직접 수정을 막음 → asset.md 규칙대로 검토 가능한 `_Recovery` [MenuItem] 스크립트로 우회 (정석 경로와 일치).

### 메모
- 자율 빌드 루프에서 "거짓 UI 만들지 말 것 / 미해결 설계 결정 위에 짓지 말 것"이 실제로 작동 — 채널 5를 hollow하게 짓지 않고 멈춤.
- 기능 단위 커밋 전환 (도현 지시) — 세션 18~24 일괄 후 미니맵·화살표 각각 커밋.
