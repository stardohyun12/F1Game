# 세션 프로토콜

## 세션 시작 (자동 — SessionStart hook이 체크리스트 주입)

1. `notes/session-log.md` 마지막 블록 (지난 세션 한 일).
2. `.claude/activeContext.md` (대기 결정·이번 세션 목표·힌트).
3. `dev-plan/feature-list.md` (위치 확인).
4. 오늘 목표 1줄 확정 후 진입.

## 세션 종료 (수동 + Hook 보조)

1. `.claude/activeContext.md` 갱신:
   - 이번 세션 한 일 (요약 1~3줄)
   - 다음 세션 목표·힌트
   - 대기 중 결정 (D-XXX)
2. `notes/session-log.md`에 한 블록 추가 (한 일 / 다음엔 / 메모).
3. 중요한 결정은 `notes/decisions-log.md`에 D-XXX로 기록.
4. 기능 단위 git 커밋 (한 번에 하나).

## Unity 화면 확인 시 — Coplay MCP 필수
씬 시각·Play·컴파일 확인은 항상 `mcp__coplay-mcp__*` 사용. 정적 분석으로 결과 추측 금지.
