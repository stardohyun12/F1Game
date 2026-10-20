# AGENTS.md — Codex 팀원용 (PitCrew / F1Game)

이 저장소는 AionUi 팀 모드로 운영된다. Codex로 실행되는 팀원(게임 아트팀, 게임 QA팀, 선택 프로그래밍팀)은 작업 전에 아래를 순서대로 읽는다. Claude 팀원은 `CLAUDE.md`가 같은 내용을 자동 로드한다.

1. `PIPELINE.md` — 팀 운영 규약(§11 = 전역 `~/.claude/TEAM-PIPELINE.md` 보충: 메시지 3종·10줄·토큰, 에디터는 `scripts/open-editor.ps1`). 역할, 산출물 위치, 게이트, 에디터 토큰, 모델링 파이프라인.
2. `CLAUDE.md` §0·§10과 거기 나열된 `project_rules/*.md` — 상위 규칙 (scope, no-compromise, asset, session, communication, roles).
3. `docs/CONTROL.md`, `.claude/activeContext.md` — 현재 상태.

## 꼭 지킬 것

- 한국어. 도현에게 가는 보고는 한 문장·표·직설 (`project_rules/communication.md`).
- unity-cli · Play · 씬 저장은 **에디터 토큰**을 받았을 때만 (`PIPELINE.md` §4). 토큰 없으면 정적 작업만.
- 완료 보고 4요소: 산출물 경로 · 확인 방법 · 검증 증거 · 남은 이슈. 실행 안 한 것은 안 했다고 쓴다.
- 그래픽 게이트는 `docs/harness/ux-reviewer-prompt.md` 절차: 의도를 듣지 않고 스샷만 보고 예/아니오 + 결함 3개 + 10점.
- 모델 제작 규격·위치는 `PIPELINE.md` §5. 투입은 `F1/Assets/Model/Custom/`, 임포트 확인은 토큰 보유자에게 요청.
- `F1/` 코드 등 다른 팀 폴더는 직접 수정하지 않고 요청한다.
- 디렉터(도현)가 직접 지시하면 즉시 따르고, 내용을 PD에게 한 줄 공유한다.
