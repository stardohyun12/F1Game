# PIPELINE — PitCrew(F1Game) 팀 운영 규약 (잔여분)

> **기준: `docs/team/WORKFLOW-v2.md`(D-277).** 어긋나면 그 장이 이긴다. 이 문서는 그 장에 없는 것(산출물 위치 §2 · 에디터 토큰 §4 · 모델 규격 §5 · 보고·커밋 §6 · 환경 §8 · 전역 규약 보충 §11)만 남겼다. 지워진 절은 번호만 두고 어디로 갔는지 한 줄.
> 규칙 집행은 글이 아니라 훅이 한다 — 봇 전체 경로·기다리는 검사·잠긴 파일(ProjectSettings·.mat·.unity·.prefab·차 조작 코드)은 **훅이 막음**(`.claude/hooks/README.md`), 세계 불변 조건은 과제 끝에 `scripts/world_check.sh`(`docs/world/README.md`).
> 역사: 2026-10-18 AionUi 팀 적용 · 아트팀이 Codex 로 모델링(Kenney 전용 해제) · main 직접 수정. 옛 §1·§3·§7·§9·§10 전문은 git 이력(`git show 3a1b719f:PIPELINE.md`).

## 0. 한눈 요약
- 지휘: **디렉터(도현) > PD > 일꾼.** 도현은 누구에게든 직접 말한다. 직접 지시를 받은 일꾼은 따르고 PD 에 한 줄 공유.
- 단위·단계·일꾼·3회 규칙 → WORKFLOW-v2 §1. 팀원이 읽는 것·확인 방법 → §2. 세계 모델 → §3.
- 산출물은 새 폴더가 아니라 **기존 자리**에(§2). Unity 에디터는 1개, **토큰**(§4). "완료" = 컴파일 0 · root cause 재현 검증 · 시각 확인(`no-compromise.md` §1). 보고 톤 = `communication.md`.

## 1. 팀원과 역할 → WORKFLOW-v2 §1 · `docs/team/FLOW.md` §2
**작성자≠판정자**는 그대로: 구현이 만든 것은 검증(블라인드)·도현이 판정한다. 그래픽 판정 절차 = `docs/harness/ux-reviewer-prompt.md`(의도 비공개, 스샷만 보고 예/아니오 + 결함 3개 + 10점).

## 2. 산출물 위치 (새로 만들지 말고 여기에)

| 산출물 | 위치 | 비고 |
|---|---|---|
| 카드(모든 일의 단위) | `docs/team/cards/C-NNN-*/`(card.md · evidence/ · research.md · verdict.json) | 양식 `docs/team/CARD_TEMPLATE.md` |
| 브리프 / 컨셉 | `옵시디언/GDD/overview.md`, `core-loop.md`, `decision-channels.md`, `project_rules/scope.md` | 새 브리프 쓰지 않는다 |
| 현재 상태 | `docs/CONTROL.md`, `.claude/activeContext.md` | PD 갱신. 날짜 블록 `scripts/refresh-control.ps1` |
| 기획 스펙 / 계획 | `docs/superpowers/specs/YYYY-MM-DD-<topic>-design.md`, `plans/YYYY-MM-DD-<topic>.md` | 상단에 상태(DRAFT/READY/DONE)와 수용 기준 표 |
| 코드 | `F1/` | 구현 일꾼만. 임포트 설정 포함 |
| 3D 모델 | 생성 스크립트·스펙·프리뷰 `art/models/<name>/` → 엔진 투입 `F1/Assets/Model/Custom/<category>/` | §5. Meshy 산출은 `F1/Assets/Model/AI/` |
| 2D 아트 | `art/concept`, `art/ui`, `art/sprites`, `art/_prompts` | `MANUS.md` 규칙 |
| 게이트 판정 | `docs/qa/GATE-YYYY-MM-DD-<topic>.md` | 기획·그래픽·QA 를 한 파일에 |
| 테스트 플랜 | `docs/qa/TESTPLAN-<topic>.md` | 수용 기준 → 테스트 케이스 |
| 버그 | `docs/qa/bugs/BUG-###-<title>.md` | 재현 절차 없는 버그 금지 |
| 헛짚음(AI 실수) | `옵시디언/하네스/incidents.md` | 버그와 다름. 4줄 블록 |
| 결정 | `옵시디언/notes/decisions-log.md` | D-XXX 이어서. 결정자는 도현, 원문 그대로 |
| 세션 기록 | `옵시디언/notes/session-log.md` | PD 가 이터레이션 끝에 한 블록 |
| 스크린샷 | `docs/screenshots/`(auto 캡처는 `auto/`) · 카드 증거는 `cards/<ID>/evidence/` | 게이트 증거 경로로 인용 |

다른 팀 폴더는 수정하지 않고 요청한다. 예외: 아트의 `F1/Assets/Model/Custom/` 투입, PD 의 상태 문서 갱신.

## 3. 이터레이션 루프와 게이트 → WORKFLOW-v2 §1(단계) · `docs/team/FLOW.md` §3~§5(칸·증거 묶음·3회 규칙)
남는 원칙 하나: 불통과는 수정 후 **재판정**이다. 판정자가 "이 정도면" 하고 넘기지 않는다(`no-compromise.md`).

## 4. 에디터 토큰 (Unity 에디터 1개)

- unity-cli(`scripts/ucli-http.sh`) · Play · 씬/프리팹 저장 · 임포트 확인은 **토큰을 가진 일꾼만**. 토큰 = `.claude/team/editor.lock`(대기열이 build 과제에 준다, `[EDITOR]` 과제 한 번에 하나). 도현이 기획 창을 열면 `director` 가 쓰여 팀원이 기다린다.
- 토큰 없는 일꾼은 정적 작업만: 문서, 코드 읽기, 스크립트·모델 생성 코드 작성, 스샷 판정. 아트의 임포트·프리뷰 렌더는 토큰 필요 → PD 에 요청.
- "급해서 잠깐" 금지. 에디터 상태가 꼬이면 전원이 멈춘다. (토큰 없는 Play 는 아직 훅이 안 막는다 — 글 규칙.)
- 토큰 반납 세 줄: 컴파일 상태 / Play 중인지 / 저장 안 한 씬 변경 유무.
- 에디터는 `scripts/open-editor.ps1` 로만 띄운다(WMI). `Start-Process` 금지. 재시작은 PD 승인 뒤.

## 5. 아트·모델링 파이프라인 (2026-10-18 Kenney 전용 해제)

**정책.** 아트 일꾼이 모델을 **만든다**(카드에 묶여서만 — WORKFLOW-v2 §1). Kenney 는 기준 룩이자 1차 라이브러리이지 상한이 아니다. `asset.md`의 "커스텀 모델 금지"는 폐기됐다.

**제작 순서 (D-106).** 제작 계획 한 장(무엇을·어떤 참고로·어떤 방식·몇 종·어디에) → 도현 "이대로/바꿔" → 원화(시안 여러 장) → 도현 승인 → Blender 등으로 모델링 → 미리보기 → 도현 적용 승인. 도현 답 없이 다음 단계로 넘어가지 않는다. 최신 아트 규칙은 `docs/design/ART-RULES.md` 가 이긴다.

**제작 방식 우선순위.**
1. **코드 생성 로우폴리** — Blender `bpy` → glTF/FBX, 또는 Unity Editor 스크립트로 `Mesh` 생성 후 프리팹화. 결정적·재생성 가능·폴리 예산 통제.
2. **Meshy text-to-3D** — `scripts/meshy-generate.ps1` + `CargoDecimator`. 유기적 형태에 한정(깎으면 깨지는 전례 있음).
3. 외부 무료 로우폴리 팩 — 라이선스 확인 후(`art/thirdparty/INDEX.md`).

**규격** (`asset.md` 계승).
- 단위 m, Kenney 타일 스케일과 맞춤, 피벗 바닥 중심, Y-up.
- 폴리 예산: 화물 ≤ 2,000 tris(`HarnessChecks.CheckCargoPolyBudget`). 건물·소품은 스펙에 예산 명시.
- 머티리얼: 플랫 셰이딩, 프로젝트 팔레트와 `Apply Warm Grade` 결과에 어울릴 것. 공유 `.mat` 수정은 훅이 막음 — 씬·폴더 전용 사본으로.
- 네이밍: `category_name_v01`(예: `cargo_eggcarton_v01`).

**위치.** `art/models/<name>/`에 `generate.*`, `SPEC.md`(용도·치수·예산·레퍼런스), `preview_*.png`. 엔진 투입물은 `F1/Assets/Model/Custom/<category>/`. `.glb`는 LFS.

**검증.** side-by-side 렌더 + 이음매 클로즈업, 톱다운+사이드뷰 두 축, 폴리 수 측정값을 보고에 숫자로. 임포트·씬 배치 확인은 토큰 보유자. 룩은 그래픽 게이트 + 도현.

**출처·라이선스.** 재료는 CC0/PD 만(AI 생성 금지 — 체크리스트 1), 출처는 `art/provenance.csv`(소리는 `F1/Assets/Audio/**/SOURCES.md`), OSM 은 `docs/map/ATTRIBUTION.md`.

**2D.** 컨셉·UI·스프라이트는 `MANUS.md` 폴더 규칙. 프롬프트 기록은 `art/_prompts/YYYYMMDD-용도.md`.

## 6. 완료 보고와 커밋

**완료 보고 4요소** (팀 메시지와 도현 보고 공통).
1. 산출물 경로
2. 확인 방법 (명령·메뉴·파일을 그대로 복붙 가능하게)
3. 검증 증거 (회귀 결과 수치, 스샷 경로, 폴리 수, 컴파일 상태)
4. 남은 이슈 / 판단 요청

실행하지 않은 것은 실행하지 않았다고 쓴다. "됐습니다" 한 줄 보고는 PD 가 돌려보낸다. 완료 보고는 10줄 이내, 메시지는 완료·막힘·질문 셋만(질문은 모아서 선택지와 함께).

**PD → 도현 보고.** → WORKFLOW-v2 §5(상태 먼저 → 결과 → 질문). 도현 판단이 필요한 보고는 첫 줄 `[결정] <한 줄>`. 소리 알림 `scripts/ask-dohyun.ps1`(킬가르도 캐릭터, 2026-10-19/20)은 도현 확인이 필요한 보고에만 — 텔레그램은 WORKFLOW-v2 §4(하루 2번 묶음 + 긴급 4종).

**토큰 절약 (2026-10-20 도현 승인).** `.claude/activeContext.md`는 지금 상태 한 화면만, 지난 기록은 `옵시디언/notes/`. 큰 작업(하루 넘게) 전에 반나절짜리 시안으로 방향 확인. 단순 확인·문서 대조는 낮은 모델로. 세션은 WORKFLOW-v2 §2(같은 카드 이어감, 카드 바뀌면 새 세션).

**커밋.** 작업 단위, `type(scope): 설명`, body 에 `Verified-By:` 실제 검증 내용(`no-compromise.md` §1). 끝에 자동: 컴파일 0 · 불변 조건 검사(위반이면 커밋 안 됨) · 증거 묶음(WORKFLOW-v2 §2). **main 직접 커밋**(2026-10-18). `[skip-verifier]`는 문서·정리 커밋에만.

## 7. 훅 → `.claude/hooks/README.md`
`stop.sh`는 **전역** 컴파일 에러를 보고 턴을 막는다 — 컴파일 깨진 채 턴을 끝내면 다른 일꾼까지 막힌다. Codex 팀원은 Claude 훅을 타지 않고 `AGENTS.md`를 읽는다.

## 8. 환경

| 항목 | 값 |
|---|---|
| OS / 셸 | Windows 11 / PowerShell 5.1(한글 리터럴 깨짐 주의: `scripts/refresh-control.ps1` 주석) · 팀 스크립트는 git-bash |
| Unity | 6000.3 LTS(`F1/ProjectSettings/ProjectVersion.txt` 가 기준) |
| 에디터 접근 | `scripts/ucli-http.sh exec/editor/console`(본 F1), F1Studio 는 `F1Studio/` |
| Python | 있음(`python -X utf8`) |
| Blender | `D:\Tools\Blender\blender.exe` |
| Meshy | `.env.local`의 `MESHY_API_KEY` |

## 9. 첫 이터레이션(2026-10-18 재기동 점검) — 끝남, 삭제. 현재 상태는 `docs/CONTROL.md`.

## 10. 속도·승인 규칙 (2026-10-20 도현 결정) — PD 턴 순서는 WORKFLOW-v2 §5
- **게이트 강도 두 단계.** 시안(도현이 방향을 보기 전): 눈에 띄는 것만(관통·뜬 물체·끊긴 길·바다 위 물체·컴파일·회귀), 고치기·재판정 1회만, 남은 건 목록으로. 마감(방향 승인 뒤): 스펙 수용 기준 전부(허용오차 포함).
- **검증은 에디터 없이.** 생성기·씬 빌드는 측정용 데이터(JSON)·증거 묶음을 같이 내고, 검증·아트 판정은 그것으로. 에디터 토큰은 만드는 쪽만.
- **생성기(도시 알고리즘)는 짧은 반복.** 한 가지 변화를 1~2시간 안에 만들어 바로 알리고 다음으로.
- (삭제) "PD 대기 금지·AionUi 알림·대화 비우기(team_clear_agent_context)" — AionUi 전용. 지금은 대기열이 Hermes 밖에서 돌고 PD 는 `wait_next_event.sh` 로 깨어난다.

## 11. 전역 팀 규약 보충 (`~/.claude/TEAM-PIPELINE.md`)
- **킥오프는 읽을 파일 경로만**(브리프 한 장 — WORKFLOW-v2 §2). 긴 파일은 필요한 줄만. 할 일 없으면 아무것도 안 한다.
- **분야가 분명한 일은 바로 그 일꾼에게.** 다른 일꾼을 풀어 주는 일(시험 도구·규격)부터.
- **기억 파일 대응**: `docs/STATUS.md` → `.claude/activeContext.md` · `docs/decisions/` → `옵시디언/notes/decisions-log.md` · `docs/handoffs/` → `옵시디언/notes/session-log.md`(팀 넘김은 `docs/team/handoffs.jsonl`). 새 폴더 만들지 않는다.
- **측정**: `node tools/team_usage.js` — 세션별 토큰. 규칙 바꾼 뒤엔 전후 비교를 숫자로.
- (삭제) `blocked_by` 태스크 보드 — 카드·대기열로 대체(`docs/team/FLOW.md` §7).
