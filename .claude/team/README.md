# PitCrew 상주 팀 (Hermes 하이브리드, 2026-10-23 도현 결정)

AionUi 팀 모드 대신, **PD(Hermes 대화)** + **상주 팀원(Claude Code 세션, 역할별로 이어짐)**.

| 역할 | 누구 | 하는 일 | 결정권 |
|---|---|---|---|
| PD | Hermes (도현과 대화하는 쪽) | 배분·우선순위·커밋·도현 보고 | 방향 해석, 커밋 |
| 프로그래밍팀 | `programmer` 세션 | 구현·튜닝·버그 수정·컴파일 | 구현 방법·수치(가설로 기록) |
| 기획·QA팀 | `qa` 세션 | 흐름 무결성 판정, Play/회귀 검증, 스펙 다듬기 | PASS/FAIL 판정 (작성자 ≠ 판정자) |
| 아트팀 | (Codex 사용량 돌아오면) | 에셋 제작 | — |

## 돌아가는 방식
- PD 가 `bash scripts/team.sh <role> "<과제>"` 로 일을 준다. 팀원 세션은 **역할별로 계속 이어진다**(`--resume`) — 지난 일을 기억한다.
- 팀원은 일을 끝내면 `docs/team/reports/<role>-<YYYYMMDD-HHMM>.md` 에 보고(4요소: 산출물·확인 방법·검증 증거·남은 이슈)를 쓴다.
- 팀원끼리 넘길 일은 `docs/team/BOARD.md` 에 적는다. PD 가 읽고 다음 팀원에게 준다.
- **커밋은 PD 만.** QA PASS 뒤에.
- **Unity 에디터는 하나** → `.claude/team/editor.lock` 을 가진 팀원만 Play·씬 저장. `team.sh` 가 `[EDITOR]` 과제에 잠금을 잡고 끝나면 푼다.

## 도현이 팀원과 직접 대화하고 싶을 때
`claude -r $(cat .claude/team/sessions/programmer.id)` (프로젝트 폴더에서) — 그 팀원의 기억 그대로 대화.
진행 상황 보기: `.ai_logs/team/<role>.log`, `docs/team/reports/`.
