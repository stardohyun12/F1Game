# docs/qa — 게이트 판정 · 테스트 플랜 · 버그

AionUi 팀의 QA팀·기획팀·아트팀이 판정 결과를 남기는 곳. 규약은 루트 `PIPELINE.md` §3.

| 파일 | 누가 | 무엇 |
|---|---|---|
| `GATE-YYYY-MM-DD-<topic>.md` | 기획·아트·QA 공동 | 한 변경에 대한 세 게이트 판정 (템플릿 `GATE_TEMPLATE.md`) |
| `TESTPLAN-<topic>.md` | QA팀 | 스펙 수용 기준 → 테스트 케이스 (템플릿 `TESTPLAN_TEMPLATE.md`) |
| `bugs/BUG-###-<title>.md` | QA팀 | 재현 가능한 버그 하나당 파일 하나 (템플릿 `bugs/BUG_TEMPLATE.md`) |

**버그와 인시던트는 다르다.** 게임이 잘못 동작하면 여기 `bugs/`. AI가 못 잡았거나 잘못 판단한 사례는 `옵시디언/하네스/incidents.md`.
