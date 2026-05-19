# 성공 사이클 회고 — 차 박힘 root cause (2026-05-19, 세션 27)

> AI 하네스 평가용 case study. **표층 fix 5번 실패 → 도현 메타 교정 → 룰 박음 → 한 사이클 만에 root cause 잡음**.
> 같은 문제에 대한 사이클 비교로 룰의 효과 측정 가능한 사례.

---

## 1. 문제

차가 도로(아스팔트)에 묻혀서 운전됨. 도현이 manual play로 반복 보고.

## 2. 실패한 사이클들 (룰 박기 전, 세션 26~27)

| # | 시점 | AI 진단 | 적용 fix | 결과 |
|---|---|---|---|---|
| 1 | 세션 26 | raycast hit y=2.108 (사실 checkpoint collider) | car.Y = 2.2 | 박힘 |
| 2 | 세션 26 후속 | raycast hit y=0.17 (Track) | car.Y = 0.3 | 박힘 |
| 3 | 세션 27 초 | raycast hit y=0.808 (사실 Race=차 자신) | car.Y = 1.5 | 박힘 |
| 4 | 세션 27 중 | raycast hit y=3.01 (역시 차 자신) | car.Y = 2.5 | 박힘 |
| 5 | 세션 27 후 | subagent: "roadMask `#`=비도로" (사실 반대) | startCell (12,10)→(13,10), car.Y=0.5 | 박힘 — 차가 굴뚝 안 |

**공통 실패 패턴**:
- 매번 raycast 한 번 → 그 수치로 spawn Y 결정
- 매번 raycast가 무엇을 hit하는지 자체가 다른데 그건 무시
- 시각 확인(screenshot) 한 번도 안 함
- subagent 진단(거꾸로 해석)을 검증 없이 채택
- 같은 종류 fix(spawn Y 변경)를 5번 반복

## 3. 메타 교정 (도현)

> "뭔가 목표가 주어지면 어떠한 방식으로든 그걸 해결해야지 너의 단계에서 타협하려고 하면 안됨 타협하는건 나의 역할이지 너는 목표를 수행하는 역할임 그러기 위해서 하네스 엔지니어링 측면에서 내가 말한대로 행동하게 엔지니어링 해봐"

핵심: AI가 자체적으로 "이 정도면 됐다" 또는 "안 되면 다음에" 타협 패턴. 도현이 검증·해결 부담 떠안음. 이걸 룰로 강제해야.

## 4. 박은 룰 (`project_rules/no-compromise.md` 7항목)

CLAUDE.md 자동 로드 등록. 다음 세션부터 자동 발효.

1. **fix 완료의 정의** = compile + 재현 검증 + 시각 screenshot 셋 다 통과
2. **"안 됨" 보고 금지** — 우회·분해
3. **도구 점검 의무** — 새 문제 진입 시 `unity-cli list` + screenshot 필수
4. **표층 fix 2회 실패 = root cause 진단 강제 전환**
5. **진단 분해** — 큰 dispatch 1회 → 작은 5회
6. **물어볼 것 vs 결정할 것** 구분
7. **commit·답변 직전 7항목 체크리스트**

## 5. 성공 사이클 (룰 박은 직후, 같은 세션 내)

| 단계 | 행동 | 결과 |
|---|---|---|
| 1 | `unity-cli list` 점검 | 9개 명령 확인 — `screenshot`이 안 쓰던 도구로 발견 |
| 2 | 차 hierarchy + BoxCollider 측정 (작은 exec 5번) | wheels visual local Y=0.300, BoxCollider 바닥 local 0.086 — wheels이 collider보다 0.086m 아래 |
| 3 | 도로 raycast (RaycastAll) | hit 2개 — Ground(y=0) + Race(차 자신). 도로 tile에 collider 없음 |
| 4 | 도로 tile 252개 Y 측정 | 모두 y=0.05 (RaiseRoadTiles +5cm 적용된 상태) |
| 5 | screenshot 3장 | 시각으로 차 wheels 도로 안 박힌 모습 직접 확인 |
| 6 | Fix 2 layer 동시 | 도로 tile y=0.05 → 0 + 차 자식 7개 localY +0.086 |
| 7 | 검증 — screenshot 다시 | 차 wheels로 도로 표면 sit, 박힘 X |

**핵심 차이점 (실패 vs 성공)**:

| 항목 | 실패 사이클 | 성공 사이클 |
|---|---|---|
| 도구 점검 | 안 함 | `unity-cli list` 사전 점검 |
| 시각 확인 | 0회 | screenshot 3장 |
| 진단 granularity | raycast 1회 → 결론 | 5단계 측정 (hierarchy / collider / 자식 local Y / 도로 raycast / 도로 tile Y) |
| Root cause 인식 | 단일 layer (spawn Y) | 2 layer (wheel-collider offset + 도로 +5cm) |
| Subagent 결과 검증 | 무비판 채택 | 거꾸로 해석 발견 후 revert |
| Fix 회수 | 5번 표층 | 1번 (한 commit, 2 layer 동시) |

## 6. 룰의 작동 메커니즘

도현 교정 → 룰 = "내가 자체 판단으로 우회·타협하는 경로 차단".

- §1 "셋 다 통과" 강제 → screenshot 빼먹기 X
- §3 "도구 점검 의무" → `unity-cli list` 안 보면 안 됨
- §4 "2회 실패 = 진단 강제" → 같은 종류 fix 5번 반복 차단점 박힘
- §5 "분해" → 큰 exec timeout 대신 작은 명령 여러 번

행위 변화가 자체 동기에서 X — 룰이 강제 트리거. 도현 부담 ↓.

## 7. 평가 항목 (사후, 형 검토용)

- [ ] 룰 §1~7 다음 세션에 자동 발효 확인
- [ ] 차 박힘 진짜 사라졌나 (도현 manual play)
- [ ] 다른 표층 fix 패턴 (예: UI 박힘·spawn 충돌)에 룰 적용 시 같은 효과?
- [ ] 룰 위반 발생 시 자체 검출 메커니즘 (체크리스트 §7)이 진짜 작동?

## 8. 참조

- 룰: `project_rules/no-compromise.md`
- Commits: `25b30ba` 룰 박음 / `80f0ff1` startCell revert / `5357577` PackingCard prefab fix / `0c3eb7a` 차 박힘 2 layer fix
- 메모리: `[[feedback-root-cause-layered-diagnosis]]`
