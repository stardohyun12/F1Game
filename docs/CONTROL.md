# 🎮 PitCrew 관제탑
**<!-- AUTO:status -->2026-05-22 | D-33<!-- /AUTO:status -->**

> **단일 본거지.** 세션 시작/끝에 무조건 여기부터. 나머지 문서는 전부 §4에서 링크.
> 갱신: 잘 변하는 §3·§7은 `scripts/refresh-control.ps1`이 git에서 자동. 느린 §0·§2는 마일스톤에 수동.
> 범례: ✅완료 · 🚧진행 · ⬜미착수 · ⚠이슈

---

## 0. 목표 사다리 — 왜 → 무엇 (지금 일이 데모로 어떻게 이어지나)

```
🎯 Steam 데모 (Vertical Slice) — 2026-06-24
   └ 6주: W1 맵해금✅ · W2 박스+내비✅ · W3 차고✅ · W4 정산→복원⬜ · W5 폴리시⬜ · W6 QA⬜
      └ 풀게임 모든 시스템 mechanically 작동 + 콘텐츠 1슬라이스(1도시·화물5종·부품2복원)
         └ 지금 작업 = 화물 원물화(채널4 배송물자) → "포장이 의미있는" 슬라이스 완성
```

핵심 지표: 한 런 **8~12초** (목표 5~15분 — 200~900배 갭, 미해결) · 거리 49~110m · 화물 4종 · 부품 6/6 복원가능

---

## 1. 파이프라인 — 모든 작업이 흐르는 길

```
아이디어 → 기획(spec) → 계획(plan) → 구현 → 검증 → 폴리시 → 완료
 ideas-parking  brainstorm   writing-plans  코드  ⬇게이트    Feels-off   매트릭스 ✅
                                              게이트(자기검증3종): 컴파일0 · 재현 · 시각
                                              + 씬/데이터 변경 → 검증 서브에이전트(D-073)
```
**규칙**: 새 작업은 이 길로 들어온다(즉흥 금지). 각 항목은 §3에서 현재 단계 + 다음 게이트로 추적.

---

## 2. 상태 매트릭스 — 무엇이 어느 단계 (A축 게임)

| 시스템 | 기획 | 구현 | 검증 | 폴리시 |
|---|:--:|:--:|:--:|:--:|
| 코어 루프 (계획/실행/정산) | ✅ | ✅ | ✅ | 🚧 |
| 포장 미니게임 (원물·버블랩) | ✅ | ✅ | ✅ | 🚧 |
| 트렁크 물리 (다물건 슬라이드) | ✅ | ✅ | ✅ | ⬜ |
| 차고 / F1 복원 메타 | ✅ | ✅ | ✅ | ⬜ |
| 내비 / 미니맵 / 화살표 | ✅ | ✅ | ✅ | 🚧 |
| 화물 비주얼 (원물화) | ✅ | ✅ | 🚧 | 🚧 |
| 맵 확장 (R1, 도시 17배) | ✅ | ⬜ | ⬜ | ⬜ |
| 사운드 / HUD / 온보딩 | 🚧 | 🚧 | ⬜ | ⬜ |

**결정 채널 9**: 1속도✅ · 2외형🚧 · 3맵해금✅ · 4배송물자🚧 · 5박스크기✅ · 6랩핑✅ · 7F1복원✅ · 8내비✅ · 9연료탱크⬜

---

## 3. 진행 중 — in-flight + 다음 게이트

| 작업 | 단계 | 다음 게이트 |
|---|---|---|
| 화물 원물화 (4종) | 폴리시 | 3종(계란/생선/케이크) 저폴리 재작업 — UnityMeshSimplifier로 강제 decimate |
| ~~관제탑(이 문서)~~ | ✅완료 | refresh 스크립트 + SessionStart hook 연결됨 (이 문서가 본거지) |

---

## 4. 본거지 링크 — 흩어진 것 한 곳에

- **상태/서사 로그**: `.claude/activeContext.md` (세션별 상세) · `옵시디언/notes/session-log.md`
- **기획**: `옵시디언/GDD/overview.md` · 결정 채널 `옵시디언/GDD/decision-channels.md` · 시스템 `옵시디언/GDD/systems/`
- **계획**: 데모 spec `옵시디언/dev-plan/2026-05-13-vertical-slice-design.md` · feature-list `옵시디언/dev-plan/feature-list.md`
- **결정 이력**: `옵시디언/notes/decisions-log.md` (D-XXX)
- **B축 하네스**: 룰 `project_rules/` + `CLAUDE.md` · 인시던트 `옵시디언/하네스/incidents.md` · 패턴 `옵시디언/하네스/patterns/`
- **spec/plan**: `docs/superpowers/specs/` · `docs/superpowers/plans/`
- **검수**: `docs/screenshots/asset_review/` (AssetPreviewTool 출력)

---

## 5. ⚠ 막힌 것 · 열린 결정

- **화물 3종 저폴리**: UnityMeshSimplifier 받는 중(도현이 Unity 포커스 or `!` refresh). 받으면 decimate+플랫셰이딩.
- **데모 런 길이 갭**: 8~12초 vs 5~15분 (200~900배). 맵 확장 + escalation 둘 다 필요 — 미착수.
- **열린 결정**: 맵확장 vs escalation 우선순위 · 작업대 받침 plane · TireType 3종 vs 4종(D-091).

---

## 6. B축 하네스 — 한 줄

발효 룰: 자기검증3종 · 검증서브에이전트(D-073) · 화면commit Feels-off(§10) · 자산 통합전 중립검수 ·
파이프라인 게이트(이 문서 §1). 인시던트 누적(이번 세션 3: verifier우회 / 피벗 / fidelity). 패턴 10.

---

## 7. 최근 활동 (git)

<!-- AUTO:activity -->
- `93f7f75` fix(cargo): 화물 프리팹 피벗 바닥-중심→기하 중심 (작업대 잘림 + 회전 궤도 해결)
- `4a43cc0` docs(active): 화물 4종 완성 기록 [skip-verifier]
- `a1feb87` feat(cargo): 화물 3종 추가 — 계란판/생선/케이크 (원물+버블랩)
- `b7e8e5e` refactor(cargo): 카탈로그 빌드 4종 데이터 테이블화 + meshy 스크립트 remesh 단계 추가
- `6f139b4` docs(active): 와인 파일럿 완료 + 리본 스케일 버그fix 기록 [skip-verifier]
<!-- /AUTO:activity -->

---

> **갱신 프로토콜**: 세션 끝 — ① `scripts/refresh-control.ps1` 실행(§status·§7 자동) ② §3 진행중 손보기 ③ 단계 바뀐 시스템 §2 갱신. 세션 시작 — 이 문서부터 읽기.
