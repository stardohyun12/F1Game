# F1Game AI 팀 게임 제작 워크플로우 — 로컬 증거 인계

## 핵심 요약

1. **원격에서 보았던 5월 스냅샷과 현재 로컬은 다르다.** `[이번에 재현]` 로컬 `main` HEAD는 `906f8994e9533f9953f775d292831f8f8586bf8c`(2026-10-13 18:56:44 +09:00). 로컬에 저장된 `origin/main`은 여전히 `964deb7df570ac8485fee0f94bd7bf669b9d2e7b`이며 로컬이 5커밋 앞선다. 워크플로우·게임 코드에 미커밋 변경이 많고 PitCrew 핵심 구현 다수가 미추적이다. HEAD만 복제하면 조사한 상태가 재현되지 않는다.
2. **실제 게임 제작은 작동한 부분이 있으나 완전 자동 제작 파이프라인으로 입증되지는 않았다.** `[로그 확인]` Claude의 작업 배정·그래픽/검증 서브에이전트 호출, Codex의 Blender 생성기 작성, Unity 설치·주행·배달 스모크 결과가 있다. 사람이 스타일·채택·작업 범위를 정했고, 도구 호출 실패·사용 한도·아트 재작업도 기록돼 있다. 플레이어 빌드, 성능, 전체 게임 수용 조건까지 통과한 근거는 이번 사례에 없다.
3. **검증의 실행과 판정은 별개다.** `[이번에 재현]` 회귀 스크립트는 빈 출력, 전부 SKIP, 검사 수 부족, `PASS`를 출력한 비정상 종료를 성공 종료로 처리했다. `[코드 확인]` Stop 훅은 여전히 커밋 문구·파일 존재·파일 시각을 확인하며 코드/빌드/실행 증거를 강하게 연결하지 않는다. PitCrew의 Claude 경로는 프로젝트 훅 자체를 비활성화한다.
4. **문서상 역할 분리가 모든 경로에 강제되지는 않는다.** `[로그 확인]` 대표 게임 세션에는 graphic-gate 12회, verifier 15회, web-search-agent 4회가 실제 호출됐지만 design-gate 호출은 없었다. `[코드 확인]` 팀 사이클은 개발·QA 팀장을 다르게 요구하나, 단일 목표는 같은 담당자의 별도 검토 턴이다. QA의 코드 수정 금지는 프롬프트 규칙이며 실제 QA 대화 중 하나는 `full` 권한이다. 총 시간·금액 상한은 확인되지 않았다.
5. **아트 경로는 이미 여러 갈래다.** `[로그 확인]` OpenAI 이미지 생성은 콘셉트/목업에 사용됐다. 게임용 결과는 Blender Python → FBX·메시 JSON → Unity 프리팹·씬 → 스모크로 이어진다. Blender MCP의 실제 사용은 미확인이다. `[이번에 재현]` 운영 runtime PID 19020은 응답하지만 현재 디스크에 있는 `runtime-identity` API는 404다. 로컬 구현과 운영 반영 상태도 구분해야 한다.

## 조사 기준·범위·읽는 방법

- 조사일: **2026-10-17, Asia/Seoul**. 증거 수집은 대략 19시대에 수행했으며 정확한 항목별 UTC 수집 시각은 `evidence_manifest.json`에 있다. 모든 아래 사례의 표시 시각은 특별한 표기가 없으면 KST이다.
- 기준 HEAD: `906f8994e9533f9953f775d292831f8f8586bf8c`. 조사 중 브랜치·HEAD는 바꾸지 않았다. 소스 발췌별 SHA-256·변경 상태를 함께 보존했다. 로그와 DB는 살아 있는 자료이므로 수집 시점이 조금씩 다르며 원자적 저장소 전체 스냅샷은 아니다.
- 범위: `D:/Github/F1Game`의 게임 프로젝트 `F1/`, `.claude/`, `project_rules/`, `scripts/`, `tools/asset-gen/`, `tools/studio-dashboard/`, 관련 로컬 실행 로그. **ExulMundi는 조사 중심에서 제외했다.**
- 수행: 읽기 전용 Git/파일/DB/프로세스 조사, 버전 조회, PitCrew GET 2건, 가짜 Unity 연결 도구를 사용한 격리 재현 5조건. **제품·설정 수정, 게임 실행, Unity/Blender 편집 요청, 유료 모델 호출, 재시작, commit/push는 하지 않았다.** Blender는 `--version`만 실행했다.
- `[코드 확인]`: 현재 코드·설정·문서에서 읽은 내용. 문서의 선언은 구현 보장과 별도로 표시한다.
- `[로그 확인]`: 실행 기록을 읽은 사실. 에이전트가 쓴 판정은 판정 기록일 뿐이며, 명령 결과와 구분한다.
- `[이번에 재현]`: 이번 조사에서 실제 명령/격리 시험으로 얻은 결과. `[추정]`: 증거에서 도출한 해석. `[미확인]`: 접근·기록·검증이 부족한 사항.
- 증거 링크의 번호는 `evidence/NN-...`이다. 발췌 안의 줄 번호는 **원본 파일 줄 번호**다. JSONL은 원본 파일·행·호출 ID 또는 세션 ID를 보존한다. 개인정보와 사용자 홈 이름은 `<USER>`로 치환했다. 인증 파일·원본 DB·전체 대화·게임 에셋은 첨부하지 않았다.

## 1. 현재 저장소와 실행 환경

### 저장소·변경·의존성

| 항목 | 확인 결과 | 근거/한계 |
|---|---|---|
| 저장소/remote | F1Game, `https://github.com/stardohyun12/F1Game.git` | `[이번에 재현]` [01](evidence/01-repository.json) |
| 현재 브랜치/HEAD | `main`, `906f8994e9533f9953f775d292831f8f8586bf8c` | 같은 증거 |
| 마지막 커밋 | 2026-10-13 18:56:44 +09:00, 해안마을 소품·배치 밀도 417→2053 | 커밋 제목은 결과 주장; 실제 범위는 아래 사례와 구분 |
| 원격 비교 | 캐시된 `origin/main`, `origin/session27-map-expansion`, `origin/HEAD` 모두 `964deb7…`; 비교 `0 5` | fetch/실시간 GitHub 조회는 안 했다. 현재 원격이 여전히 같은지는 미확인 |
| 스테이징 | 없음 | `git diff --cached --stat` 빈 결과 |
| 작업 트리 | 수정 133개, 삭제 1개, 미추적 9,711개(이번 조사 폴더 제외) | 조사 당시 상태. 미추적 수는 워크플로우만의 수가 아니며 산출물도 포함. 관련 경로 목록은 01의 `workflow_changes` |
| 다른 worktree | 임시 `vibe-kanban/worktrees/f85f-f1game-pitcrew/F1Game`, 브랜치 `vk/f85f-f1game-pitcrew`, HEAD `af7ca8c0fd7dc67d8abe918ffb9f0b31674d4ce9` | 존재 확인만. 그 worktree의 실행·수정 이력을 이 보고서 주 작업과 합산하지 않음 |
| 공유 하네스 | `CLAUDE.md`가 별도 `harness-core`를 참조 | 저장소 내부, `D:/Github/harness-core`, 사용자 홈 하위에서 찾지 못함. 다른 위치 존재 여부 미확인 |
| 전역 의존성 | 사용자 Claude/Codex 설정·로그·로그인, MCP 설정, Claude agent SDK 가상환경, Unity connector, Blender 실행 파일 | 저장소만으로 재현 불가. 인증 내용은 수집하지 않음 |

`[코드 확인]` 주요 미커밋 영역은 `.claude/activeContext.md`, `verifier.md`, `stop.sh`, `project_rules/asset.md`, `roles.md`, `lofi_pilot.py`, `CoastalTownDensity/Installer/PropsBuilder`, `LofiPilotBuilder`, 씬·재질·FBX, 대시보드 코드다. `hub.py`, `runtime.py`, 웹/시험/프로세스 정리 도구 등은 미추적 상태가 포함된다. [01](evidence/01-repository.json)의 전체 관련 목록을 우선한다. 이 보고서를 올리는 것과 제품 변경 전체를 올리는 것은 별개다.

### 설치·설정값과 관측값

| 대상 | 설치/설정 | 실제 사용 근거·제약 |
|---|---|---|
| Unity | `F1/ProjectSettings/ProjectVersion.txt`: **6000.3.19f1**, revision `7689f4515d75`; 실제 프로젝트 `D:/Github/F1Game/F1` | `[이번에 재현]` PID 3536의 실행 경로도 6000.3.19f1. 저장 descriptor는 해당 프로젝트·8090·ready. descriptor만으로 현재 Editor 응답까지 보장하지 않으며 이번에는 Editor에 요청하지 않음 |
| Unity HTTP connector | 패키지 `com.youngwoocho02.unity-cli-connector` **0.3.18**, PackageCache suffix `fbc0d1057e5f`; manifest는 Git URL | 게임 로그에 8090 `/command` 호출 결과 존재. 전역 `unity` MCP와 이 HTTP connector를 같은 제품/버전으로 합치지 말 것 |
| 전역 Unity MCP | Claude `unity-editor-mcp`, Codex `unity` 설정에 command `unity`; 로컬 실행 파일 버전 리소스 `1.0.0` | Claude 게임 세션에서 `mcp__unity-editor-mcp__editor_status` 3회 관측. 패키지/배포판의 의미 있는 CLI 버전은 미확인. PATH의 `unity-cli`는 발견하지 못함 |
| Claude Code | `claude --version`: **2.1.293**; 대시보드 venv `claude-agent-sdk` **0.2.165** | 게임 상위 세션 모델 `claude-fable-5-1`; 하위 로그에 `claude-sonnet-5-5`/`claude-opus-5-5`. 명칭을 다른 모델로 임의 치환하지 않음 |
| Codex | `codex --version`: **0.154.0**; 전역 `gpt-6-astra`, effort `medium` | 아트 작업 로그도 `gpt-6-astra`, medium, approval `never`, sandbox `workspace-write`, network_access `false`를 기록. 대시보드의 별도 full 세션과 구분 |
| Blender | `D:/Tools/Blender/blender.exe --version`: **5.2.2 LTS** | 게임 생성기 실행 기록 있음. Blender MCP 설정/호출은 조사한 전역 설정·대표 세션에서 찾지 못함 |
| OpenAI 이미지 도구 | 저장소 프로그램 설치 항목이 아니라 호스트 도구 `tools.image_gen__imagegen` | 콘셉트·목업 생성 호출과 이미지 반환 관측. 게임 에셋 자동 통합을 뜻하지 않음 |
| ComfyUI/Hunyuan | `hy3d_run.py`·README, Claude 전역 comfyui MCP 설정 존재 | 최근 채택된 마을 키트가 이 경로로 생성됐다는 실행 근거는 확보 못함. README의 처리 시간/닫힌 표면 예상은 측정 결과로 재사용하지 않음 |

근거: [02](evidence/02-project-rules.txt), [10](evidence/10-environment-config.json), [13](evidence/13-codex-session-evidence.json), [14](evidence/14-claude-session-evidence.json), [16](evidence/16-installed-tools.json), [18](evidence/18-live-readonly-observation.json), [21](evidence/21-image-generation-history.json).

`[코드 확인]` Claude 전역 설정은 permission default `auto`, effort `medium`, `CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS` 설정됨이다. Codex 전역 파일에 approval/sandbox 키가 없더라도 실제 세션 정책은 별도 전달될 수 있다. 환경변수 존재 여부는 [10](evidence/10-environment-config.json)에 이름과 boolean으로만 남겼다. **현재 조사 프로세스 환경은 이미 실행 중인 다른 에이전트의 환경을 대변하지 않는다.**

`[로그 확인]` 대시보드에는 표시명·선택 모델·관측 모델이 다른 예가 있다. `리서치 · Haiku`의 선택값은 `sonnet`, 관측 모델은 `claude-sonnet-5-5`다. `기획`의 선택값 `fable`과 저장된 actualModel `claude-sonnet-5-5`도 다르다. 이는 저장된 최신 응답 모델 필드이며, 과거 모든 턴의 모델 또는 현재 선택값 실행 결과로 단정하면 안 된다. `verifier.md`는 현재 haiku지만 activeContext는 sonnet으로 적고 있고 실제 10/13 하위 로그에도 sonnet이 있다.

### “최근 이틀 자율 실행”을 어디까지 말할 수 있나

`[로그 확인]` [11](evidence/11-dashboard-records.json)의 `last_48h_start_utc`부터 수집 시각까지를 명시적으로 잘라 메시지 수를 집계했다. 이는 **10/15 저녁부터 10/17 저녁까지 약 48시간의 관측 창**이며, 무인 연속 실행 시간은 아니다. `user` 역할에는 자동 팀 보고·사이클 프롬프트도 들어가므로 사람 개입량으로 세지 않았다.

정확한 시작/종료가 저장된 최근 사이클은 다음과 같다([20](evidence/20-cycle-timestamps.json)).

| 실행 ID | 기록된 기간(KST) | 범위/결과 |
|---|---|---|
| `b93a5e6db6cd426db8736da761a37357` | 10/15 23:26:12–23:39:47 | PitCrew 소통/효율, research→build→QA→build→QA, done |
| `6977b17e83e04af294666e02b05c5d2b` | 10/16 00:35:23–00:49:04 | PitCrew 팀 작업 방식 점검, 같은 5단계, done |
| `cc0edf661b5448c6b0459843e7b8c57d` | 10/16 15:24:17–15:49:11 | PitCrew 자체 업그레이드, 3단계, done |
| `22b1564244ad4baab83cef03d5aead37` | 10/16 20:02:49 시작; 완료 시각 없음 | UI/UX·게임 제작 도구 목표, 11개 build 단계, 수집 당시 paused |

이것은 **도구 개발 이력**이다. 이를 게임 콘텐츠가 이틀간 자동 생산됐다는 근거로 쓸 수 없다. 게임 제작을 깊이 추적한 Claude 세션 `a3e29587-ae78-41dd-a321-ea4500e45403`의 로그 범위는 **10/12 18:49:57–10/14 20:33:54**다. 대시보드 구현 관련 Claude 세션 `971b1c4a-…`는 10/15 02:51:51–10/17 09:48:16 기록이 있다. 어느 범위도 휴식·대기·사람 입력을 제거한 실제 자율 작업 시간으로 측정되지 않았다. `[미확인]` 사용자가 말한 “최근 이틀”의 정확한 시작/종료와 어떤 목표를 뜻하는지는 추가 지정이 필요하다.

## 2. 문서·구현·실행을 구분한 에이전트 구조

### 역할 표

| 역할 | 실행 도구/모델 | 진입 명령·설정 | 읽기/쓰기 권한 | 입력 | 출력 | 호출 조건 | 완료 조건 |
|---|---|---|---|---|---|---|---|
| 사람 | 직접 대화·화면 판단 | 브리프/결정 기록 | 프로젝트 선택·승인 | 원하는 게임/한 줄 기준 | 스타일·채택·범위 결정 | 새 과제/판단 대기 | 명시적 수용; 자동 판정 아님 |
| 게임 상위 작업자/배정자 | Claude Code; 관측 `claude-fable-5-1` | `CLAUDE.md`, activeContext, roles; 기존 CLI 세션 | 실제 Bash/Write/Edit 사용 | 사용자 요청·결정·상태 | 브리프, 코드, Agent 호출, 통합 | 사람이 작업 요청 | 문서상 외부 게이트 통과; 실제 예외 있음 |
| 아트/구현 작업자 | Codex CLI `gpt-6-astra`, medium | `.ai_logs/codex-building-*.log`의 브리프 입력; `art/_briefs/` | 관측 workspace-write, never; 파일 공유 | 범위·보호 파일·아트 기준 | Python/C#·FBX·JSON·캡처 | 상위 작업자가 브리프 전달 | 검사·스모크·요약; 외부 아트 합격과 별개 |
| design-gate | 설정 sonnet | `.claude/agents/design-gate.md` | Read/Glob/Grep/Bash/Write 허용, 규칙상 Bash 읽기만 | diff + 사람 기준 | 결정/범위 판정 + 게이트 기록 | 문서상 시스템/씬 변경 후 | 규칙상 게이트 판정; 대표 게임 세션 실제 호출 0 |
| graphic-gate | 설정 sonnet, 실제 sonnet 계열 | `.claude/agents/graphic-gate.md` | Read/Glob/Grep/Write; 규칙상 Unity/Blender/Bash 금지 | 캡처·무엇인지 한 줄 | 단점 3개, 점수, 다음 개선 1개, 게이트 행 | 화면/에셋 변경 | 7/10 이상 PASS; 실제 12회 호출 |
| verifier | 현재 설정 haiku; 과거 관측 sonnet | `.claude/agents/verifier.md` | Bash/Read/Glob/Grep/Write; 규칙상 제품 코드 수정 금지 | 대상 변경·현재 씬 | compile/회귀/예외 수 + 게이트 행 | 시스템/씬 변화 후 | compile 0·새 FAIL 0·예외 0; 기존 FAIL 별도 허용 |
| 조사자 | web-search-agent, 하위 로그 opus 계열도 존재 | 게임 상위 Agent 호출 | 검색/읽기 도구; 일부 Bash | 도시/레벨 설계 질문 | 자료·비교 | 필요시 분담 | 상위 작업자 수용; 실제 4회 |
| PitCrew 팀장·팀원 | Claude SDK / Codex app-server / 추가 ACP 경로 | `hub.py:delegate`, team/agent DB 설정 | 대화별 read/work/full | 목표·이전 단계·팀 보고 | 응답·도구 이벤트·위임·다음 보고 | 팀장 delegate/큐 또는 사이클 | 프로세스 턴 종료 + 보고 판정 |
| PitCrew QA | 별도 팀장; 실제 Sonnet full, Codex read 예 있음 | `cycle_prompt`, `start_cycle` | 프롬프트는 “파일 고치지 마세요”; 권한은 대화 설정 | 개발 보고·기준 | 판정·기준별 근거 문구 | build 후 | 파서가 통과/기준충족 인정 |
| PitCrew 단일 목표 검토 | **같은 owner의 별도 턴** | `goal_stage`, `goal_advance` | owner 권한 동일 | 목표 원문·작업 보고 | 완료 검토 | `상태: 완료 후보` | 파서 통과 → done |

근거: [04](evidence/04-hooks-gates.txt), [05](evidence/05-hub-orchestration.txt), [11](evidence/11-dashboard-records.json), [14](evidence/14-claude-session-evidence.json), [17](evidence/17-supplement-code.txt). 관측 호출 수는 중복 tool_use ID를 제거했으며 “모델 결제 호출 수”와 같지 않다.

### 확인한 호출 흐름과 명령

`[로그 확인]` 게임 사례의 순서는 **사람 기준 → Claude가 브리프/작업 분리 → Codex 또는 Claude가 Blender/C# 제작 → Unity 설치·스모크 → graphic-gate와 verifier → 사람 채택/다음 수정**이다. graphic-gate와 verifier는 `run_in_background:true`로 수 초 간격에 호출된 기록이 있다. Unity를 쓰는 verifier와 그림을 읽는 리뷰의 병렬화가 관측되지만, 전체 게임 제작 DAG를 자동 실행하는 고정 스케줄러로 입증된 것은 아니다.

`[코드 확인]` PitCrew의 별도 실행 구조:

```text
start.ps1 → watchdog.py → web_host.py(웹), runtime.py --serve(실행 서비스)
웹 /api/hub/* → runtime.forward → Hub → 대화별 Run 스레드
  Codex: codex … app-server --listen stdio://
         thread/start 또는 thread/resume → turn/start → 이벤트 저장
  Claude: ClaudeSDKClient(ClaudeAgentOptions(...resume=session, fork_session=False))
팀장 delegate → 대상 대화 queue → 실행 완료 후 원래 팀장에게 보고
팀 사이클: research → build → qa → 필요시 build
단일 goal: 같은 owner build → 같은 owner review
```

위 명령/함수는 소스에 실제 존재한다. 이번 조사에서는 시작하지 않았다. 생성기 파일의 실행 예시는 Blender `-b -P tools/asset-gen/lofi_pilot.py -- --root D:/Github/F1Game`이며, 현재 생성기는 `--root`만 받고 seed **413을 코드에 고정**한다. `--seed` 옵션은 없다. Unity에는 실제 `LofiPilotBuilder.BuildBuildingPrefabs()`와 `CoastalTownSmoke.Run()`이 있다. `scripts/ucli-http.sh`는 **status/exec/editor play·stop/console만** 구현한다. 브리프의 `menu`나 오래된 문서의 `editor refresh --compile`을 이 대체 스크립트가 지원한다고 가정하면 안 된다. 실제 Codex 사례는 PATH/인용 실패 후 같은 connector `/command`로 PowerShell JSON POST를 사용했다.

### 동시성·권한·재개·상한

- `[코드 확인]` Hub의 잠금은 DB/목표 전환/실행 큐를 보호한다. 대화마다 `Run.execute` 스레드를 시작하므로 서로 다른 대화는 동시에 실행될 수 있다. 파일·Git 브랜치·Unity Editor·Blender 출력 파일의 공통 배타 잠금은 조사한 실행 경로에서 확인되지 않았다. 별도 worktree가 하나 존재해도 모든 팀원이 자동으로 worktree를 받는 구조는 아니다.
- `[로그 확인]` 게임 브리프는 Codex가 `CoastalTownDensity.cs`/`CoastalPropsBuilder.cs`를 수정하지 않고 카탈로그를 넘기도록 분담했다. 보호 파일의 전후 해시 기록이 있다. 이것은 특정 작업의 준수 증거이며 런타임 권한 차단은 아니다. `verifier`만 Play한다는 문서와 달리 Codex 자체 스모크도 Play를 사용했다. 동시에 실행됐다는 충돌 증거까지는 확보하지 못했다.
- `[코드 확인]` Codex는 저장된 `session`으로 `thread/resume`, 반환 ID가 다르면 오류; Claude는 `resume`, `fork_session=False`, 결과 ID를 확인한다. DB에는 세션 ID가 실제 저장돼 있다. 원본 세션 재개와 별도 하위 에이전트 기록 읽기는 구분된다. 이번에는 실제 AI 재개를 호출하지 않았다.
- `[코드 확인]` `Run.execute`가 done/error/interrupted와 도구 미확인 상태를 저장한다. `recover_cycles`는 끊긴 단계를 일시정지시키며 자동으로 명령을 재전송하지 않는다. `watchdog`은 서비스 재시작을 계속 시도하고 2~30초 범위의 대기를 둔다. CLI RPC의 60/120초, proxy의 125초, 시작 대기의 35초 등은 **해당 요청/시작 제한**이지 전체 작업 시간 상한이 아니다.
- `[코드 확인]` 위임은 깊이 4, 체인 20건, 한 턴 도구 8건 제한이 있다. 체인 카운터는 메모리 값이다. 팀 사이클의 유한 rounds는 1~3이지만 `untilDone`이면 회차 제한을 적용하지 않고 단일 goal은 기본 untilDone이다. 같은 정규화 보고 3회 연속이면 정지하는 규칙이 있으나 의미상 진전·비용 제한을 보증하지 않는다. **전체 목표의 시간·토큰·금액 상한을 강제하는 코드/SDK 옵션은 이 경로에서 확인하지 못했다.** 비용 저장은 비용 제한과 다르다.
- `[코드 확인]` 팀 사이클은 build/qa **팀장 ID가 같으면 거절**한다. 그러나 팀원 중복·동일 파일 권한·동일 사람 모델 검증의 독립성까지 강제하지 않는다. 단일 goal은 애초에 같은 owner다. Claude read는 일부 도구를 거절하고 Codex read는 read-only sandbox지만, QA라고 자동 read 권한이 되는 것은 아니다.
- `[코드 확인]` UI의 실행 상태 일부는 프로세스 종료·도구·SDK/RPC 이벤트에서 온다. 목표 성공/기준 충족은 에이전트의 `판정:`·`기준 N:`·상태 줄을 파싱한다. 근거 문구/미검증 표현을 검사하지만 실제 검사 파일·코드 해시를 독립적으로 실행·대조하는 판정기는 아니다. **“done”은 게임 수용 조건의 객관적 증명과 같지 않다.**
- `[이번에 재현]` 운영 runtime-health는 PID 19020으로 응답했고 runtime-identity는 404였다. 현재 디스크의 API·보호 장치가 그 프로세스에 적용됐다고 말할 수 없다. 기존 원시 기록을 남기고 운영 전환은 하지 않았다.

## 3. 대표 게임 작업 3건의 처음부터 끝까지

### 사례 1 — 마을 밀도: Codex 한도 실패 → Claude 수행 → 그래픽 불합격 상태의 커밋

**요청/수용 조건.** `[코드 확인]` [22 브리프](evidence/22-density-brief.txt): 따뜻한 로파이 해안 결을 유지하고 “살고 있는 동네”로 만들기. 건물당 소품 약 5개, 도로 디테일, 구역 톤 통일, 주행 스모크 SUCCESS, 기존 CargoPolyBudget 1 FAIL을 제외한 회귀, 같은 위치 캡처. 출발점 `21c77820…`, 차량 물리/카메라/배달 시스템 변경 금지.

**호출/실패.** `[로그 확인]` Codex 세션 `01a1348e-5347-7141-b79a-36e26758d4a4`는 **10/13 18:18:29.287–18:18:30.518** 범위이고, `.ai_logs/codex-town-density-2026-10-13.log:15-16`에 사용 한도 오류 2줄이 있다. 세션에 제작 도구 호출이 없고 토큰 최종 계수도 없다. 같은 실패가 두 줄 출력됐다고 재시도 2회를 뜻하지 않는다. 성공률 분모에서 빼면 안 되는 실패 실행이다.

**수행/수정.** `[로그 확인]` `art/source/coastal-town/README.md`는 Codex 대신 Claude가 수행했다고 기록한다. `lofi_pilot.py` 소품 16종, `CoastalPropsBuilder.cs`, `CoastalTownDensity.cs`, `CoastalTownInstaller.cs`, 프리팹·아틀라스·씬을 바꿨다. 후속 3차는 현재 작업 트리에 더 있으므로 HEAD diff만으로 이 실행의 변경을 추정하지 말 것.

**검증/리뷰/통합.** `[로그 확인]` Claude 상위 세션의 graphic-gate 호출 `toolu_01URANFi58rBFTwExLhHDSgG`(18:34:43), verifier `toolu_01DJRKdJ95PgGVyhBpSpF3cY`(18:34:48). 게이트 행 3–4는 graphic **5/10 FAIL**, verifier PASS(회귀 **12/1/0**, 기존 CargoPolyBudget 허용), 스모크 SUCCESS, 2053 배치를 보고한다. **18:56:44 커밋 `906f8994…`은 실제 존재한다.** 커밋 본문도 FAIL을 숨기지 않고 **“비교 기록용 커밋”이며 완료가 아니라고 명시**한다([23](evidence/23-commit-and-protected-files.json)). 따라서 완료를 거짓 보고했다고 단정할 사례는 아니다. 다만 “외부 그래픽 게이트 통과 전엔 커밋 불가”가 시스템에서 절대적으로 강제되는 것도 아니다. 별도 예외의 승인 주체는 미확인이다.

**완료 근거의 한계.** 기술적 설치와 커밋은 확인했지만 아트 수용은 당시 FAIL이다. 현재 `play-verification.txt`는 나중 작업으로 바뀌었으므로 2053 배치 판의 원본 시험 로그로 취급하지 않는다. Claude 수행의 정확한 순수 작업 시간·청구 비용·사람 개입 횟수는 산출할 근거가 부족하다. 근거: [01](evidence/01-repository.json), [08](evidence/08-game-gate-record.txt), [09](evidence/09-codex-task-records.txt), [13](evidence/13-codex-session-evidence.json), [14](evidence/14-claude-session-evidence.json).

### 사례 2 — 건물 다양성: Codex 제작 → Claude 치수 통합 → 별도 게이트

**요청/범위.** `[코드 확인]` `art/_briefs/2026-10-13-building-variety.md`: 간판만 다른 상점 대신 정면에서 업종이 읽히고 집 실루엣도 달라야 함. 4차 Codex 판 채택 후 시작. 64px 아틀라스 칸·Point·2m UV·기존 RNG 순서 유지. Density/PropsBuilder는 금지 파일, 새 치수·층수·문 위치는 `building-catalog.json`으로 전달한다.

**실제 작업 순서.** `[로그 확인]` Codex 세션 **`01a134e4-dfd8-7a70-950a-c1e0c61981b1`**, **10/13 19:52:57.449–20:06:21.046**(로그 범위 약 **803.597초**, 전체 인수/리뷰 소요 시간 아님). 생성기·카탈로그·Unity 빌더/설치 선택·검증 코드와 산출물을 변경하고, Blender 생성 후 Unity 재수입·프리팹 생성·설치·스모크·캡처를 수행했다. 그 뒤 Claude가 Density의 Body/DoorX를 카탈로그에 연결했다. 카탈로그 전달은 계획만이 아니라 현재 `CoastalTownDensity.cs`에서도 읽기 구현을 확인했다.

**수정 산출물.** `tools/asset-gen/lofi_pilot.py`, 건물 검사 코드, `F1/Assets/Editor/LofiPilotBuilder.cs`, `CoastalTownInstaller.cs`, `art/source/coastal-town/building-catalog.json`, 11종 FBX·mesh.json·prefab·Blender 원본·아틀라스, `ComplexTrack.unity`, 보고/검사 JSON. 이후 다른 작업도 같은 파일을 수정했으므로 원시 로그의 작업별 설명과 보호 파일 해시를 같이 읽어야 한다.

**실제 검증 근거.** `[로그 확인]` [13](evidence/13-codex-session-evidence.json)의 해당 세션 **원본 JSONL 180행** 도구 결과에는 8개 PASS와 SUCCESS가 있고, **242행**에는 `compiling=False playing=False ... dirty=False`, Point/Uncompressed/mipmaps=False, console 0, **2752 배치·297/297 도로·1554 충돌 샘플·Shader errors=0·Issues 비어 있음**이 있다. 이것은 README의 완료 문장보다 강한 실행 근거다. [12](evidence/12-art-validation-records.json)의 geometry/FBX 비교 결과와 보호 파일 before/after 해시도 남아 있다. 이번 현재 파일 대조에서는 PropsBuilder는 당시 해시와 같고 Density는 다르다([23](evidence/23-commit-and-protected-files.json)). 후속 통합이 있으므로 당시 보호 파일 보존을 현재 전체 작업 트리 보존으로 확대하면 안 된다. 반환 이미지/캡처 경로 기록은 시각 확인의 범위를 뒷받침하지만 모든 배치가 안전하다는 의미는 아니다.

**리뷰/후속.** `[로그 확인]` 게이트 파일 13–14행: graphic **7/10 PASS**, verifier PASS. 남은 지적은 운전 시점의 상점 가독성·큰 벽면·빈 도로다. Codex 종료 보고의 “Density가 옛 치수를 쓰므로 간섭 미해결”을 Claude가 후속 치수 통합으로 다뤘다. **해당 5차 결과는 커밋하지 않았으며 현재 HEAD는 여전히 밀도 2차다.**

**측정값/한계.** Codex 최종 기록: input **2,716,008**, 그중 cached input **2,594,048**, output **19,876**, total **2,735,884**. 이는 반복 문맥을 포함한 세션 누적 계수이며 고유 문맥 크기나 청구 금액이 아니다. outer `exec` 호출 **33**, task_started/task_complete 각각 1. 내부 shell/API 수와 모델 요청 수를 33으로 단정하지 않는다. 달러 비용·사람 개입 시간은 미확인. 플레이어 빌드/FPS/드로콜/전체 충돌 조합은 검사하지 않은 것으로 기록돼 있다.

### 사례 3 — 한낮 조명 v1→v5: 반복 미감 리뷰와 실제 결함 발견의 차이

**요청/분해.** `[코드 확인·로그 확인]` activeContext의 한낮 색감·카메라 변경과 게이트 기록을 연결했다. Claude가 조명/셰이더/카메라/후처리를 수정하고 매 단계 graphic-gate와 verifier를 분리 호출했다. 이 사례의 상세 원래 사용자 수용 문장 전체는 첨부하지 않았으므로 “아말피 한낮”이라는 문서상 방향 이상으로 객관적 수용 조건을 재작성하지 않았다.

**실제 순서.** [14](evidence/14-claude-session-evidence.json)에 **10/13 21:24:37–21:49:53** 사이 v1–v5의 graphic/verifier 쌍 호출 10개가 남아 있다. 게이트 파일에는 판정 시각이 역순 또는 `22:5x`로 적힌 행도 있어, 실제 호출 순서·기간에는 JSONL timestamp를 우선한다.

**수정/검증.** Directional light, FollowCamera/레이아웃 카메라 값, URP renderScale/Point, Ground/WorldSurface 재질·셰이더, `WarmGrade.asset`, 씬 등이 대상이다. v1 graphic 6 FAIL 뒤 v2–v5는 7 PASS가 반복된다. 회귀는 계속 12/1/0이다. 그러나 v4 verifier는 **WarmGrade의 두 component 참조가 fileID 0**임을 찾아 FAIL했다. 하위 세션 `agent-af517cafd7a1d3e12.jsonl:69`의 **21:47:00** 실제 도구 출력에 두 0 참조가 있고, 후속 `agent-a19b84560ce837b0e.jsonl:36` **21:51:45**에는 두 비영 참조가 있다. [19](evidence/19-warmgrade-verifier-results.json). 후처리 적용을 눈으로만 단정하지 않고 디스크 참조를 확인한 유효한 검증 사례다.

**리뷰의 의미/완료.** `[로그 확인]` 그래픽 점수 7이 계속 나온 상태에서도 미감 수정이 이어졌고, 기술적 결함은 별도 verifier가 찾았다. `[추정]` 반복 단점 요구가 추가 수정을 유도했을 수는 있지만, 반복의 원인을 Feels-off 규칙 하나로 확정할 수 없다. 사람이 색감 조정을 원한 영향과 분리 실험이 없다. v5 verifier PASS 및 이후 야간 verifier 기록은 있으나 최신 코드/빌드 전체를 보증하지 않는다. 결과는 미커밋이며 정확한 전체 작업 시간·비용·사람 개입량은 미확인이다.

## 4. 과거 감사 A–D의 현재 유효성

| 항목 | 분류 | 현재 증거와 해석 |
|---|---|---|
| A 회귀 미실행 성공 | **부분 수정 — 핵심 결함은 현재도 존재** | fallback HTTP는 연결 descriptor 부재/요청 오류에 FAIL을 출력하므로 그 경로는 exit 1. 그러나 집계기는 exit status·빈 출력·기대 검사 수·SKIP를 성공/실패에 충분히 반영하지 않음 |
| B 검증 대신 형식 | **부분 수정 — 형식/시각 기반 한계 유지** | Editor 도달성 경고·gate mtime 추가. Verified-By 길이, Screenshot 존재 확인 유지; 코드/빌드/실행 ID 결합 없음. 대시보드 Claude 경로는 훅 비활성 |
| C 강제 비평 | **현재도 존재** | no-compromise가 단점 없으면 개선점이라도 1개 요구, Stop은 Feels-off 존재 강제. graphic-gate는 단점 3개 요구. 유효 결함 발견과 필수 단점 생성의 인과는 별도 |
| D 지침/실제 차이 | **현재도 존재** | Unity 버전/폴더, 폐기 선언된 자기검증 규칙의 auto import, verifier 모델, design-gate 호출, README의 Codex 위임 설명, 단일 목표 검토 방식, 운영 API 반영 차이 |

### A: 이번 격리 재현 상세

원본 `regression-check.sh`를 **바이트 그대로** 새 임시 폴더에 복사하고, 같은 폴더의 `ucli.sh`만 가짜로 대체했다. sleep은 즉시 반환하는 shim을 두었다. Unity/네트워크/모델은 호출하지 않았다. 실제 connector를 원본 조건으로 재현한 시험이 아니라 **집계·종료 판정에 대한 의존성 대체 시험**이다. 코드와 결과를 [15](evidence/15-regression-isolated-reproduction.json)에 남겼다.

| 가짜 연결 도구 조건 | 출력 집계 | 스크립트 exit |
|---|---|---:|
| 모든 검사: 출력 없이 exit 127 | 0 PASS / 0 FAIL / 0 SKIP | **0** |
| 모든 검사: FAIL 출력, exit 1 | 0 PASS / 13 FAIL / 0 SKIP | 1 |
| 모든 검사: SKIP 출력 | 0 PASS / 0 FAIL / 13 SKIP | **0** |
| 모든 검사: PASS 출력 후 exit 9 | 13 PASS / 0 FAIL / 0 SKIP | **0** |
| 첫 검사만 PASS, 나머지 빈 출력 | 1 PASS / 0 FAIL / 0 SKIP | **0** |

`[코드 확인]` `say()`는 문자열에 FAIL/SKIP/PASS가 있는지만 센다(`scripts/regression-check.sh:10-18`). 마지막 줄은 FAILS>0인 경우만 exit 1이다. 초기 Play/상태 전환 명령도 성공 여부를 검증하지 않는다. `ucli-http.sh:21`의 status는 저장된 port를 찾으면 성공하고 실제 HTTP 생존 확인을 하지 않는다. 따라서 Stop 훅의 “Editor 도달성” 주석도 구현보다 강하다. 실 Editor 미연결 상황은 이번에 재현하지 않았다.

### B: 결과와 현재 코드의 연결

- `[코드 확인]` `stop.sh:75-118`: 최근 **30분 커밋**만, `[skip-verifier]` 예외, Verified-By 실질 내용 **12자 이상**과 일부 공허한 단어를 검사한다. 로그의 명령·exit·수치·코드 해시를 파싱하지 않는다.
- `[코드 확인]` `stop.sh:149-176`: Screenshot 경로가 있고 파일이 존재하면 된다. 생성 시점, 이미지 내용, 실행 씬·코드 일치는 검사하지 않는다. `187-198`의 gate mtime 비교는 미커밋 `.cs` 변경 알림을 생략하는 조건이며 **실제 gate PASS를 강제하는 검사와 다르다**.
- `[코드 확인]` `.cs/.unity/.asset` dirty 조회는 `git diff`와 cached만 포함하므로 미추적 파일은 빠진다. 도달 불가 경고는 block이 아니라 additionalContext다.
- `[코드 확인]` `CoastalTownSmoke.cs:45,47`은 결과를 같은 `play-verification.txt`에 덮어쓴다. `01~04` 캡처도 같은 이름이다. `[로그 확인]` 현재 play 로그 mtime 10/13 23:45:36은 사례 2 종료 20:06 이후다. 이전 세션의 JSONL 도구 결과는 당시 근거를 남기지만 현재 파일은 동일 판 증거가 아니다.
- `[추정]` 과거 파일/긴 문구를 재사용해 통과할 여지가 있다. 실제 악용 사례를 발견했다는 주장은 아니다. 이번에는 커밋 위조나 훅 우회 재현을 하지 않았다.

### C: 결함과 개선 제안의 자동 승격

`[코드 확인]` `no-compromise.md:120-128`은 단점이 없으면 더 나아질 점이라도 쓰도록 한다. Stop은 “Feels-off 내용이 사실인지”를 확인하지 않고 줄 존재만 확인한다. graphic-gate의 3개 단점 요구와 7점 문턱은 추가적인 주관적 게이트다.

`[로그 확인]` 실제 결함에 가까운 지적은 원경 경계, 변형 간 단면/가독성, WarmGrade 빈 참조 등이다. `[미확인]` Feels-off 커밋 문구가 직접 찾아낸 결함의 전수 통계나 불필요한 수정량은 없다. 조명 v2–v5가 7점을 유지하며 수정된 것은 반복 관측이며 불필요성의 증명은 아니다.

`[코드 확인]` Feels-off 문장을 기계적으로 필수 수정 큐로 바꾸는 연결은 찾지 못했다. PitCrew는 QA가 `수정 필요`라고 판정하면 build로 돌아가고, untilDone이면 반복 상한이 없다. 따라서 **리뷰어가 제안을 필수로 판정할 때** 반복이 생길 수 있으나, 문구의 존재만으로 자동 수정되는 구조와는 구분해야 한다.

### D: 자동 로드/낡은 지침

`[코드 확인]` `CLAUDE.md:113-119`는 session/no-compromise/activeContext를 import하며 roles는 직접 import하지 않는다. activeContext가 roles를 참조한다. `session.md:38-51`의 자기 검증 3종은 그대로 로드 대상인데 roles는 이를 폐기/대체했다고 적는다. SessionStart는 CONTROL과 activeContext 전체를 추가 주입한다. 일반 Claude CLI에서 실행될 설정이지만 대시보드의 `disableAllHooks=True` 경로에는 동일하게 적용되지 않는다.

`[코드 확인·로그 확인]` activeContext의 “verifier sonnet”과 현재 에이전트 설정 haiku, “다음 게이트 대기”와 이후 게이트 PASS, 구 Unity 버전/Assets 루트 설명, README의 Codex 위임 불가 설명과 현재 `codex_delegate` 구현이 어긋난다. 독립 QA·운영 적용 완료도 문서 문구만으로 확대하지 말아야 한다.

## 5. 아트 제작과 게임 통합의 실제 경로

| 도구/경로 | 현재 확인 수준 | 자동/사람 단계 |
|---|---|---|
| OpenAI 이미지 생성 | `[로그 확인]` 세션 `01a13151-…` 10/13 03시대 콘셉트·목업과 23:09 콘셉트 보드 호출/이미지 반환 | 프롬프트·변형 생성은 도구, 스타일 채택은 사람. 목업이 실제 Unity 화면이라는 증거는 아님 |
| Blender Python | `[코드 확인·로그 확인]` `lofi_pilot.py`, `coastal_formula.py`, `street_furniture.py` 등; 최근 마을 사례는 실제 실행 | 코드 수정은 에이전트, 생성/export는 CLI, 미감·채택은 사람/리뷰어 |
| Blender MCP | `[미확인]` 조사한 설정·대표 세션에서 실제 연결/호출 없음 | **도입 후보**. 이미 작동하는 Blender CLI 경로와 혼동 금지 |
| ComfyUI/Hunyuan3D | `[코드 확인]` wrapper·모델 경로 설명·전역 MCP 설정 있음 | 사용 가능한 대안 경로. 최근 채택 마을 제작에 사용됐다는 실행 근거는 없음 |
| 기존 Kenney 계열 에셋 | `[코드 확인·로그 확인]` 레이아웃/규칙/기존 에셋을 바탕으로 자체 키트를 교체·비활성화한 기록 | 신규 키트와 함께 남은 에셋·라이선스 전체 전수 검사는 하지 않음 |
| 기존 별도 이미지 생성 프로젝트 | 경로 참조만 있음 | 유지해야 할 목표로 취급하지 않음; 이번 조사 중심 밖 |

`[코드 확인·로그 확인]` 현재 입증된 경로는 다음과 같다.

```text
사람의 방향/브리프 + 콘셉트 참고
 → 에이전트가 Blender Python 수정
 → Blender -b -P: .blend + .fbx + .mesh.json + 공용 아틀라스
 → Unity AssetDatabase.Refresh/컴파일
 → LofiPilotBuilder.BuildBuildingPrefabs: FBX/JSON 대조·프리팹·충돌체
 → CoastalTownInstaller: Layout/배치
 → CoastalTownSmoke.Run: Play·3지점 이동/접지·배달·캡처·종료
 → graphic-gate / verifier / 사람 채택
```

이는 역할·파일로 연결된 실제 사례다. 생성 이미지에서 완성 게임까지 사람 없이 자동 연결되는 단일 실행 명령은 확인하지 못했다.

**계약과 추적 방식.** `[코드 확인]` 건물 브리프·빌더는 바닥 원점, Blender 전면 방향과 Unity +Z, FBX `axis_forward='-Z'`, `axis_up='Y'`, 스케일 적용, 몸통 치수·층수·문 x, 벽걸이 띠, 삼각형 예산, UV를 다룬다. 아틀라스는 512×512/64px 칸, Point·무압축·mipmap 없음, 큰 면 2m 분할이다. 빌더는 FBX/mesh.json bounds·중심·바닥·UV를 비교하고 건물 mesh에 MeshCollider를 붙인다. 소품 종류별 Collider 예외도 별도 빌더에 있다. `building-catalog.json`은 후속 배치의 치수 계약이다. 계약 범위는 **이 키트/브리프**이며 모든 게임 에셋의 중앙 공통 스키마로 강제되는 것은 아니다.

`[로그 확인]` 생성 스크립트·seed, `.blend`, `.fbx`, `.mesh.json`, brief, README, 비교용 `before_`/`codex4_`/`claude4_`, protected-files 해시가 남는다. 그러나 많은 산출물이 미추적이고 기본 캡처·검사 경로는 덮어쓴다. 현재 geometry/FBX JSON의 이름이 `building-faces`여도 후속 variety 실행에서 갱신됐으므로 이름만 보고 버전을 정하면 안 된다.

**어디가 어려웠나.** `[로그 확인]` 관측 사례에서 미감 판정·스타일 채택·치수/문 위치의 배치 연결·Unity import/컴파일·PATH/인용 문제가 손을 탔다. 4차는 옆면 장식을 늘리며 건물 삼각형이 약 534~898에서 2998~4118로 늘었고, 5차는 범위 내 예산 4500을 사용했다. 성능 측정은 없다. `[미확인]` 전체 작업의 실패 빈도/시간 계측이 없으므로 “가장 자주 실패하는 단계”를 통계적으로 순위 매길 수 없다. 이 샘플에서 가장 여러 번 관측된 재작업은 시각 품질/조명 판정이다. ChatGPT 이미지 생성이나 Blender MCP가 더 효율적이라는 말은 **비교 전 가설**이다.

## 6. 앞으로 할 비교 실험 후보 — 이번에는 실행하지 않음

**비교 대상 고정.** 현행군은 이번에 확인한 “상위 작업자 + 구현자 + graphic/verifier 등 실제 역할 분리”다. 대시보드 팀 사이클을 쓸 경우 그것을 별도 조건으로 명시한다. 경량군은 “구현자 1명 + 동일 자동 검사 + 필요한 읽기 전용 리뷰어 1명”이다. 두 군의 모델·effort·권한·도구·과제·초기 파일을 동일하게 고정해야 구조 효과를 비교할 수 있다. 모델 성능 비교는 별도 실험으로 분리한다.

**기준 커밋 주의.** 공통 기준은 `906f8994e9533f9953f775d292831f8f8586bf8c`로 명시하되, 현재 미커밋 코드가 필요한 과제는 이 보고서 manifest가 식별한 파일과 필수 산출물을 별도 격리 스냅샷으로 확보해야 한다. 이 인계 자료에는 전체 코드/에셋이 없으므로 그대로 실행 가능한 완전 백업이 아니다. 양 군의 시작 파일 해시가 같아야 한다. 원본 작업 트리의 checkout/reset은 하지 않는다.

| 과제 | 기준·범위 | 객관적 수용 조건 | 필요한 환경 | 평가 명령/수동 절차 |
|---|---|---|---|---|
| 1. 회귀 false-green 보완 | 공통 HEAD + 현재 `regression-check.sh`, `ucli*.sh` 스냅샷; shell 집계만 | 빈 출력·비정상 exit·전부 SKIP·13개 미만을 통과로 처리하지 않음. 정상 13개 PASS만 정상 완료. 명시 FAIL은 실패 | Git Bash/Python, 가짜 connector. Unity 불필요 | [15](evidence/15-regression-isolated-reproduction.json)의 5조건을 동일 fixture로 수행. 정상 13 PASS 대조군을 추가하는 것은 **미래 시험 조건**이며 이번 결과에 포함하지 않음 |
| 2. 작은 소품 1종 제작·통합 | 공통 HEAD + 현재 키트 계약/빌더; 예: 벤치 1종 | 기준 치수·바닥 원점·UV·삼각형 상한을 사전 수치로 고정, FBX/JSON 일치, 기존 8 스모크 PASS, 지정 세 시점에서 간섭 없음 | 같은 Blender 5.2.2, Unity 6000.3.19f1, 키트/씬 사본. 에디터 독점 시간 | 기존 `lofi_pilot.py` 생성 진입점과 실제 프리팹 빌더 사용. `CoastalTownSmoke.Run()` 및 고정 카메라 캡처를 수동 비교. 새 에셋을 현 코드가 자동 수용한다고 가정하지 않음 |
| 3. 카탈로그 기반 배치 간섭 1건 | 공통 HEAD + 현재 11종 catalog/Density/Installer/씬 | 지정된 건물·문·소품 조합 1건의 겹침 제거, 도로 297/297/기존 배달 성공 유지, 금지 파일 해시 유지 | Unity 사본·같은 layout/seed·같은 플레이어 | 기존 설치/validation/스모크 경로. 지정 오브젝트 bounds와 고정 시점 캡처를 비교; 실제 차 접촉은 동일 수동 절차로 평가 |
| 4. 주문보드 UI의 한 가지 가독성 수정 | 공통 HEAD + 현재 OrderBoard/UI/씬 스냅샷; 글자/배치 한 조건만 | 동일 주문 3개에서 텍스트 겹침·잘림 없음, 출발/선택 기능 유지, 지정 해상도 2개 모두 통과 | 동일 Unity, 같은 주문 fixture. GUI/시각 평가 필요 | 현재 HarnessChecks의 OrderBoardWiring + 주문→포장→주행 수동 절차. RectTransform 측정과 실제 화면 캡처 둘 다; 전체 회귀 판정은 과제1과 별개로 동일 방식 적용 |

**공통 기록 규칙(실험 제안).** 시작/종료·대기 시간, 모델 호출/중첩 도구 호출의 구분, 실제 토큰·금액의 제공 여부, 사람 결정/수정 시간, 검증 실패·timeout·미실행·최종 수용을 모두 남긴다. 실패 실행을 삭제하거나 재시도 성공으로 덮지 않는다. 같은 자동 검사와 같은 수용자를 쓰고, 아트 평가는 익명 A/B·같은 장면/카메라로 한다. 이번에는 이 비교군을 가동하거나 비용을 쓰지 않았다.

## 7. 우선순위별 다음 조치와 추가 질문

아래는 **후속 작업 제안**이며 이번 조사에서 구현·설정 변경은 하지 않았다.

| 우선순위 | 다음 조치 | 이유/필요 정보 |
|---|---|---|
| P0 | 이번 파일 해시·현재 작업 트리를 보존하고 실제 실험 기준판 지정 | HEAD/원격/미커밋/운영 프로세스가 다름. 다른 ChatGPT가 GitHub만 읽으면 현재 구조를 놓침 |
| P0 | 회귀 검사 미실행·실패·부족 수를 별도 결과로 다루고 증거-코드 연결을 검증할 후속 과제 승인 | 이번 격리 재현에서 false-green이 확인됨. 이 보고서의 판정으로 자동 수정하지 말 것 |
| P1 | “완료”가 기술 통과·아트 채택·게임 통합·배포 중 무엇인지 과제별 지정 | 그래픽 FAIL 상태 커밋, 기존 회귀 FAIL 허용, SDK 훅 비활성, 동일 owner 검토가 공존 |
| P1 | 최근 이틀의 목표 ID/시작·종료/사람 개입 기록 지정 | 도구 개발의 지속 기록을 게임 제작 자동화 시간으로 합산하면 안 됨 |
| P1 | 위 후보 중 1개를 골라 구조 비교의 환경/예산/종료 조건 고정 | 모델 우열과 구조 효과를 분리하고 실패·미실행 포함 가능 |
| P2 | 아트 후보 도구 비교는 별도 작은 과제로 수행 | 이미지 생성은 콘셉트 실사용, Blender CLI는 에셋 실사용, Blender MCP는 도입 후보. 현재 더 낫다는 측정 없음 |

외부 ChatGPT가 추가로 확인할 질문:

1. “최근 이틀 자율 실행”은 어느 날짜·goal/session ID이며, 게임 제작과 PitCrew 개발 중 어느 범위인가?
2. 기준판은 현재 미커밋 작업 트리인가, 10/13 HEAD인가? GitHub에 없는 코드·산출물 중 공유 가능한 최소 범위는 무엇인가?
3. 아트/게임의 최종 수용자는 누구이며, CargoPolyBudget 기존 FAIL 허용과 그래픽 FAIL 상태 커밋의 예외는 어떤 근거로 승인됐나?
4. 성능 목표(기기/FPS/메모리), 실제 플레이어 빌드, 수동 주행 범위에 관한 결과가 별도 위치에 있는가?
5. 모델 청구/계정 사용량과 작업별 비용을 연결한 원장이 있는가? 없다면 비용·성공률은 미확인으로 유지해야 한다.
6. Blender MCP가 다른 앱/머신에 연결돼 있다면 **비밀값을 제외한 설정 위치·버전·실행 ID**는 무엇인가? `harness-core`의 실제 경로/버전도 필요하다.

## 산출물·공유 범위

- `WORKFLOW_HANDOFF.md`: 이 문서.
- `evidence/`: 번호가 붙은 최소 발췌/집계/격리 재현. 코드 파일 전체나 인증 자료·DB·게임 에셋 전체는 제외.
- `evidence_manifest.json`: 증거별 출처·범위·확인 방법·비식별 처리·해시. 소스 SHA는 수집한 시점 기준이다.
- `collect_evidence.py`, `collect_execution_records.py`, `collect_supplement.py`: 이번 폴더 안의 수집 방법 기록. 실행 시 이 폴더의 증거를 다시 쓰므로 **인계 받은 정본 보존이 필요하면 재실행하지 말 것**. 현재 제품 파일을 수정하지 않으며 격리 재현만 수행한다.

GitHub에 공유할 최소 목록은 **이 폴더의 `WORKFLOW_HANDOFF.md`, `evidence_manifest.json`, `evidence/` 전체**다. 수집 스크립트는 방법 검토가 필요할 때 추가할 수 있다. `.runtime/`, 원본 JSONL, `.claude.json`, 인증 파일, 게임 에셋 전체, 다른 미커밋 파일은 이 조사 공유 목록에 포함하지 않는다. 경로·세션 ID도 내부 식별자이므로 공개 범위는 사용자가 정한다. 이번에는 commit/push하지 않았다.

상태: 완료 후보
