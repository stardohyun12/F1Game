# 세션 프로토콜 [project]

> unity-cli 섹션은 Unity 프로젝트 공통 → universal 승격 후보.
> 현재 경로(`C:/Users/...`)가 환경 특정이라 project 유지.

## 세션 시작 (자동 — SessionStart hook이 체크리스트 주입)

1. `옵시디언/notes/session-log.md` 마지막 블록 (지난 세션 한 일).
2. `.claude/activeContext.md` (대기 결정·이번 세션 목표·힌트).
3. `옵시디언/dev-plan/feature-list.md` (위치 확인).
4. 오늘 목표 1줄 확정 후 진입.

## 세션 종료 (수동 + Hook 보조)

1. `.claude/activeContext.md` 갱신:
   - 이번 세션 한 일 (요약 1~3줄)
   - 다음 세션 목표·힌트
   - 대기 중 결정 (D-XXX)
2. `옵시디언/notes/session-log.md`에 한 블록 추가 (한 일 / 다음엔 / 메모).
3. 중요한 결정은 `옵시디언/notes/decisions-log.md`에 D-XXX로 기록.
4. 기능 단위 git 커밋 (한 번에 하나).
5. **하네스 패스 (B 트랙)** — `옵시디언/창업/harness-usage-log.md`에 한 블록.

## B 트랙 — 하네스 (게임 작업과 나란히, 매 세션)

> A 트랙(게임) 작업이 B 트랙(하네스)의 재료. 두 트랙 동시 진행.

**게임 작업 중 (A 트랙이 트리거):**
- 헛짚음 발생 → 즉시 `옵시디언/하네스/incidents.md`에 4줄 블록 기록.
- 같은 패턴 incidents 3건+ → 해당 패턴 "검증됨" → draft-rule을 `project_rules/` 또는 `CLAUDE.md`에 **즉시 발효** (D-071: 형 검토 안 기다림, 사후 검토).
- 기존 10패턴에 없는 새 종류 → `옵시디언/하네스/patterns/` 새 파일.

**하네스 평가 (B 트랙 = "다른 팀", 게임 흐름 안 막음):**
- 발효된 규칙이 실제로 도움 되는지·잘못된 건 없는지 평가·정리·수정.
- 형 검토는 여기에 사후로 들어옴.
- A 트랙(게임 작업)은 평가를 기다리지 않음 — 켜고 계속 간다.

## Unity 검증 — unity-cli (Editor 켠 채로) 필수

씬/컴파일/물리/로그/Play 검증은 항상 `unity-cli`(`C:/Users/stard/AppData/Local/unity-cli/unity-cli.exe`)로 한다. **Editor 켜둔 상태에서 실시간 통신** — Editor 닫기 요청 금지, batch 사용 금지.

설치: 바이너리 + Unity 패키지 `com.youngwoocho02.unity-cli-connector` (이미 manifest 등록). Editor 열면 자동으로 포트 8090에 HTTP 서버 뜸.

자주 쓰는 패턴:
- 상태: `unity-cli status`
- 콘솔: `unity-cli console --type error` / `--type warning` / `--type log` (콤마 조합 가능, default는 셋 다)
- C# 즉시 실행: `unity-cli exec "return ...;"` (return으로 결과 받기)
- 메뉴 트리거: `unity-cli menu "PitCrew/Fix/Diagnose Floating"`
- **재컴파일 (블록 끝까지)**: `unity-cli editor refresh --compile` ← 이거 쓰면 sleep + 포트 변경 fail 안 남
- Play 테스트: `unity-cli editor play --wait` → 측정 → `editor stop`
- 스크린샷: `unity-cli screenshot --view game --output shot.png`
- 도움말: `unity-cli list` / `unity-cli <cmd> --help`

**금지 패턴**: `exec "AssetDatabase.Refresh()"` + sleep + retry. → 항상 `editor refresh --compile` 한 줄로.

규칙:
- "Editor 닫아줘" 요청 금지 — unity-cli는 Editor 켜둘 때만 작동.
- 정적 분석으로 Unity 결과 추측 금지 — 반드시 unity-cli로 검증.
- 진단/수정 스크립트는 `Assets/Editor/_Recovery/`에 두되, [MenuItem] 노출 권장 (`unity-cli menu`로 호출).
- 폐기됨: Coplay MCP (`mcp__coplay-mcp__*`) / Unity batch mode (`-batchmode -executeMethod`).
