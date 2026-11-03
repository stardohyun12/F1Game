# .claude/hooks — Claude Code 훅

| 훅 | 이벤트 | 하는 일 |
|---|---|---|
| `guard-pre.py` | PreToolUse (Bash·Edit·Write·MultiEdit·Read) | **막음**(D-270/D-276): Bash 에 봇 전체 경로(`OnFootRouteBot.Begin/Run`·`BeginFirstOrder`·`route_feature2`·`RegressSweep.Run`), `sleep 100+`·`until [ -f … ]; do sleep` 대기 루프 / Edit·Write 가 잠긴 경로(`F1/ProjectSettings/`, `F1/Assets/**/*.mat`, `ArcadeCarController.cs`, `DriveFeel.cs`, `*.unity`, `*.prefab`)를 건드릴 때 / **D-277** 팀원 세션(`TEAM_ROLE` 있음)이 규칙 문서(`PIPELINE.md`·`AGENTS.md`·`project_rules/*.md`·`docs/harness/dohyun-checklist.md`·`docs/team/FLOW.md`·`WORKFLOW-v2.md`)를 Read 하거나 cat/head/grep 할 때(브리프 한 장에 다 있다). 막은 것은 `.ai_logs/team/guard.log` 한 줄. |
| `guard-post.py` | PostToolUse (Edit·Write·MultiEdit) | `F1/Assets/**/*.cs` 수정 뒤 중괄호 짝·`using UnityEngine;` 누락만 경고(additionalContext, 막지 않음). |
| `session-start.sh`·`session-end.sh`·`stop.sh`·`activity-log.py`·`flash.ps1`·`feedback-detect.sh` | 기존 | 로그·대시보드·알림용(막지 않음). |

푸는 법(환경변수, 그 과제 하나에만): `TEAM_ALLOW_BOT=1` 봇 전체 경로 허용(backlog 90 회귀 과제) · `TEAM_UNLOCK=1` 잠긴 경로 수정 허용.
등록 위치: PD 세션은 `.claude/settings.json`, 팀원 세션(team.sh, `--setting-sources user` 라 프로젝트 훅을 안 읽음)은 `.claude/team/guard.settings.json` 을 `--settings` 로 붙임 — 둘을 같이 고친다.
시험: `echo '{"tool_name":"Bash","tool_input":{"command":"sleep 600"}}' | python .claude/hooks/guard-pre.py` → deny JSON 이 나오면 막힘, 아무것도 안 나오면 통과.
