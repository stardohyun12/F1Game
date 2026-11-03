# CLAUDE.md §10 고침 제안 (D-277 브리프 한 장) — PD 가 적용

하위 에이전트는 CLAUDE.md 쓰기가 막혀 있어(보호 파일) 직접 못 고쳤다. 아래를 그대로 붙이면 된다.

## 왜
CLAUDE.md §10 의 `@PIPELINE.md`·`@project_rules/*.md`·`@.claude/activeContext.md` 는 Claude Code **import** 라 팀원 세션(team.sh)에도 매 과제 자동으로 들어간다(≈3만 자). 훅(guard-pre.py)이 Read/cat 을 막아도 import 는 못 막는다 → D-277 「7만 자 → 1만 자」가 안 된다. 조건 분기는 CLAUDE.md 에서 못 하니 import 를 떼고 목록 + 문장으로 바꾼다. 도현이 직접 여는 세션은 필요할 때 목록의 파일을 열면 된다(활성 문맥 2.5k 자만 import 로 남겨도 됨).

## §10 교체본

```
## 10. 규칙 모듈 [project] — 읽는 사람에 따라 다르다 (D-277, 2026-11-03)

**팀원(역할 세션 — `scripts/team.sh` 가 띄우고 `TEAM_ROLE` 환경변수가 있음)은 브리프 한 장만 읽는다.** 과제 글(`scripts/brief.py` 출력 = 과제 + 카드 + 필수 규칙 + 세계 스냅숏 + 파일 지도 + 에디터 치트)이 시작 글의 전부이고, 아래 문서는 열지 않는다 — 규칙은 글이 아니라 훅(`.claude/hooks/guard-pre.py`)이 강제하며, 아래 문서 읽기도 훅이 막는다. 기준 한 장은 `docs/team/WORKFLOW-v2.md`(PD 용).

**도현이 직접 여는 세션(TEAM_ROLE 없음)**은 예전처럼 필요한 것을 연다(자동 로드 `@` 는 팀원 세션에도 같이 먹어 7만 자를 매 과제 읽게 하므로 뗐다 — 목록으로만):

- `PIPELINE.md` — 팀 운영 규약
- `project_rules/scope.md` · `asset.md` · `session.md` · `communication.md` · `no-compromise.md` · `REJECTED_IDEAS.md` · `roles.md`
- @.claude/activeContext.md
```

## §11 한 줄 추가
"팀 운영 규약은 `PIPELINE.md`(자동 로드)" → "팀 운영 규약은 `PIPELINE.md`. **단 team.sh 가 띄운 팀원(역할 세션)은 §10 대로 브리프만.**"

## 버전 줄
`v0.3 (2026-11-03, D-277 §10 자동 로드 해제 — 팀원은 브리프만)` 추가.
