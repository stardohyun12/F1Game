# PIPELINE — PitCrew(F1Game) AionUi 팀 운영 규약

> AionUi 팀 모드로 이 저장소에서 일하는 모든 팀원(Claude·Codex)이 작업 전에 읽는 문서.
> 우선순위: `CLAUDE.md`와 `project_rules/*`가 최상위. 이 문서는 그것들을 "팀으로 어떻게 나눠 수행하는가"로 번역한다.
> AionUi 어시스턴트 규칙(범용)과 충돌하면 **이 문서가 이긴다.**
> 2026-10-18 도현 결정: AionUi 팀 적용 · 아트팀이 Codex로 모델링(Kenney 전용 해제) · main 직접 수정.

## 0. 한눈 요약

- 지휘: **디렉터(도현) > 총괄 PD > 각 팀.** 도현은 어느 팀에게든 직접 말한다. 직접 지시를 받은 팀은 따르고 PD에 한 줄 공유.
- `project_rules/roles.md`의 강제 규칙 "코딩은 기획·그래픽 게이트 통과 전 커밋 불가"가 **팀 구조**로 집행된다.
- 산출물은 새 폴더가 아니라 **기존 자리**에 쓴다 (§2). 새 폴더는 `docs/qa/` 하나.
- Unity 에디터는 1개. **에디터 토큰**(§4)을 가진 팀만 unity-cli·Play·씬 저장.
- "완료" = `no-compromise.md` §1: 컴파일 0 · root cause 재현 검증 · 시각 확인. 셋 다.
- 보고 톤 = `communication.md`: 한국어, 한 문장, 표, 직설.

## 1. 팀원과 역할 (roles.md 매핑)

| roles.md 역할 | AionUi 팀원 | 백엔드 | 담당 | 막는 것 |
|---|---|---|---|---|
| 배분·기록·보고 | 게임 총괄 PD (팀장) | Claude | 태스크 보드, 에디터 토큰 관리, `CONTROL.md`·`activeContext.md`·`session-log.md` 갱신, 도현 보고. 코드·에셋 제작 안 함 | — |
| 기획 에이전트 | 게임 기획팀 | Claude | 스펙(`docs/superpowers/specs`), **기획 게이트**(흐름·주변 무결성·빠진 단계), `scope.md` 집행(새 아이디어→`ideas-parking.md`) | ①② |
| 코딩 | 게임 프로그래밍팀 | Claude | `F1/` 구현. 판정 금지. 게이트 PASS 전 커밋 금지 | — |
| 그래픽 에이전트 + 모델러 | 게임 아트팀 | Codex | **그래픽 게이트**(`docs/harness/ux-reviewer-prompt.md` 절차, 컨텍스트 0), 룩·톤·팔레트 방향, UI/HUD 목업, **3D 모델 제작**(§5) | ③ |
| 회귀 하네스·Play 검증 | 게임 QA팀 | Codex | `scripts/regression-check.sh` 실행, 실제 Play 시나리오, 버그 리포트(`docs/qa/bugs`), QA 게이트 문서 | ③ |
| 조사 | 게임 리서치팀 (2026-10-20 도현 요청) | Claude | 오픈소스·에셋·레퍼런스·도현 링크 조사, 라이선스, 넣는 비용 가설. `옵시디언/research/`. 다운로드·설치·코드 금지(조사만) | — |
| (선택) 2번째 코딩 | 게임 프로그래밍팀 [Codex] | Codex | 병렬 구현 또는 Claude 사용량 소진 시 | — |

①②③ = roles.md의 도현 진단 3종. **작성자≠판정자**: 프로그래밍이 만든 것은 기획·아트·QA가 판정하고, 아트가 만든 모델은 QA(규격·폴리)와 기획(용도)이 판정하며 룩은 그래픽 게이트와 도현이 판정한다.

## 2. 산출물 위치 (새로 만들지 말고 여기에)

| 산출물 | 위치 | 비고 |
|---|---|---|
| 브리프 / 컨셉 | `옵시디언/GDD/overview.md`, `core-loop.md`, `decision-channels.md`, `project_rules/scope.md` | 이미 있음. 새 브리프 쓰지 않는다 |
| 현재 상태 | `docs/CONTROL.md`, `.claude/activeContext.md` | PD가 이터레이션마다 갱신. 날짜 블록은 `scripts/refresh-control.ps1` |
| 기획 스펙 / 계획 | `docs/superpowers/specs/YYYY-MM-DD-<topic>-design.md`, `plans/YYYY-MM-DD-<topic>.md` | 기존 명명 유지. 상단에 상태(DRAFT/READY/DONE)와 수용 기준 표 |
| 코드 | `F1/` | 프로그래밍팀만. 임포트 설정 포함 |
| 3D 모델 | 생성 스크립트·스펙·프리뷰 `art/models/<name>/` → 엔진 투입 `F1/Assets/Model/Custom/<category>/` | §5. Meshy 산출은 기존 `F1/Assets/Model/AI/` |
| 2D 아트 | `art/concept`, `art/ui`, `art/sprites`, `art/_prompts` | `MANUS.md` 규칙 그대로. 아트팀도 같은 폴더 |
| 게이트 판정 | `docs/qa/GATE-YYYY-MM-DD-<topic>.md` | 기획·그래픽·QA 세 섹션을 한 파일에 |
| 테스트 플랜 | `docs/qa/TESTPLAN-<topic>.md` | 수용 기준 → 테스트 케이스 |
| 버그 | `docs/qa/bugs/BUG-###-<title>.md` | 재현 절차 없는 버그 금지 |
| 헛짚음(AI 실수) | `옵시디언/하네스/incidents.md` | 버그와 다름. AI가 못 잡은 사례 4줄 블록 |
| 결정 | `옵시디언/notes/decisions-log.md` | D-XXX 번호 이어서. 결정자는 도현 |
| 세션 기록 | `옵시디언/notes/session-log.md` | PD가 이터레이션 끝에 한 블록 |
| 스크린샷 | `docs/screenshots/` (auto 캡처는 `auto/`) | 게이트 증거 경로로 인용 |

다른 팀 폴더는 수정하지 않고 요청한다. 예외: 아트팀의 `F1/Assets/Model/Custom/` 투입, PD의 상태 문서 갱신.

## 3. 이터레이션 루프와 게이트

```
목표      도현+PD. CONTROL.md "지금 하는 일 / 막힌 것", activeContext 우선순위에서 고른다. 새 시스템은 scope.md 체크리스트.
  ↓
스펙      기획팀 → docs/superpowers/specs/. 경험 목표 · 규칙 · 수치(가설 표시) · 수용 기준(측정 가능) · 범위 밖.
  ↓
제작      프로그래밍팀 F1/  ∥  아트팀 모델·UI (§5)
  ↓
자동검사  QA팀(토큰) regression-check.sh + 관련 HarnessChecks. 기존 FAIL(예: CargoPolyBudget)은 "기존"으로 명시.
  ↓
게이트    기획 게이트(기획팀) + 그래픽 게이트(아트팀, 화면 변경 시) + QA 게이트(QA팀) → docs/qa/GATE-*.md
  ↓
커밋      프로그래밍팀. 세 게이트 PASS 메시지가 태스크에 붙어 있어야. body에 Verified-By: 실제 증거.
  ↓
판정      PD가 도현에게 보고(§6 형식) → 도현 플레이/스샷 판정 → 결정은 decisions-log D-XXX.
  ↓
기록      PD: CONTROL.md · activeContext.md · session-log.md 갱신. 다음 목표로.
```

| 게이트 | 통과 조건 | 판정자 |
|---|---|---|
| 스펙 → 제작 | 수용 기준이 측정 가능, 아트·기술 요구 분리, scope 체크리스트 통과, 상태 READY | 기획팀 |
| 제작 → 게이트 | 컴파일 0, 변경 요약, 확인 방법, 스샷 경로 | 제작 팀 자체 보고 |
| 기획 게이트 | 게임 기본 흐름 안 깨짐, 주변 시스템 영향 확인, 빠진 단계 없음 | 기획팀 |
| 그래픽 게이트 | ux-reviewer 절차: 의도 비공개, 스샷만 보고 예/아니오 + 결함 3개 + 10점 척도 | 아트팀 |
| QA 게이트 | 회귀 PASS(기존 FAIL 예외 명시), 실제 Play 시나리오 수행, 열린 S1/S2 버그 0 | QA팀 |
| 커밋 | 위 셋 PASS, Verified-By 실제 검증 내용 | 프로그래밍팀 |
| 디렉터 | 도현 플레이 또는 스샷 판정 | 도현 |

게이트 FAIL은 수정 후 **재판정**이다. 판정자가 "이 정도면" 하고 넘기지 않는다 (`no-compromise.md`).

## 4. 에디터 토큰 (Unity 에디터 1개)

- unity-cli · Play · 씬/프리팹 저장 · 임포트 확인은 **토큰을 가진 팀만** 한다. PD가 태스크 보드에 `[EDITOR]` 접두 태스크로 토큰을 주고, 한 번에 하나만 존재한다.
- 기본 순환: 프로그래밍(구현·컴파일) → QA(회귀·Play·스샷 캡처) → 프로그래밍(수정). 아트의 스샷 판정은 토큰 불필요. 아트의 임포트·프리뷰 렌더는 토큰 필요 → PD에 요청.
- 토큰 없는 팀은 정적 작업만: 문서, 코드 읽기, 스크립트·모델 생성 코드 작성, 스샷 판정.
- "급해서 잠깐" 금지. 에디터 상태가 꼬이면 전원이 멈춘다.
- 토큰 반납 메시지에 세 줄: 컴파일 상태 / Play 중인지 / 저장 안 한 씬 변경 유무.

## 5. 아트·모델링 파이프라인 (2026-10-18 변경: Kenney 전용 해제)

**정책.** 아트팀(Codex)이 모델을 **만든다.** Kenney는 기준 룩이자 1차 라이브러리이지 상한이 아니다. `asset.md`의 "커스텀 모델 금지"는 폐기됐다.

**제작 순서 (D-106, 2026-10-19).** Codex 가 원화(분위기·형태)를 먼저 그린다 → 도현 승인 → Blender 등 실제 도구로 모델링 → 미리보기 → 도현 적용 승인. 원화 없이 바로 모델링하지 않는다.

**제작 방식 우선순위.**
1. **코드 생성 로우폴리** — Blender `bpy` 스크립트 → glTF/FBX, 또는 Unity Editor 스크립트로 `Mesh` 생성 후 프리팹화. 결정적이고 재생성 가능하며 폴리 예산을 처음부터 통제한다. Blender가 없으면 Unity Editor 경로.
2. **Meshy text-to-3D** — `scripts/meshy-generate.ps1` + `CargoDecimator`. 유기적 형태에 한정. 깎으면 깨지는 전례(세션 21 케이크) 있음.
3. 외부 무료 로우폴리 팩 — 라이선스 확인 후.

**규격** (`asset.md` 계승).
- 단위 m, Kenney 타일 스케일과 맞춤, 피벗 바닥 중심, Y-up.
- 폴리 예산: 화물 ≤ 2,000 tris (`HarnessChecks.CheckCargoPolyBudget`가 판정). 건물·소품은 스펙에 예산 명시.
- 머티리얼: 플랫 셰이딩, 프로젝트 팔레트와 `Apply Warm Grade` 결과에 어울릴 것. 텍스처 없이 버텍스 컬러/단색 머티리얼 우선.
- 네이밍: `category_name_v01` (예: `cargo_eggcarton_v01`).

**위치.** `art/models/<name>/`에 `generate.*`(스크립트), `SPEC.md`(용도·치수·예산·레퍼런스), `preview_*.png`. 엔진 투입물은 `F1/Assets/Model/Custom/<category>/`. `.glb`는 LFS.

**검증.** side-by-side 렌더 + 이음매 클로즈업, 톱다운+사이드뷰 두 축(`asset.md`), 폴리 수 측정값을 보고에 숫자로. 임포트와 씬 배치 확인은 토큰 보유자. 룩은 그래픽 게이트 + 도현.

**2D.** 컨셉·UI·스프라이트는 `MANUS.md` 폴더 규칙. 프롬프트 기록은 `art/_prompts/YYYYMMDD-용도.md`.

## 6. 완료 보고와 커밋

**완료 보고 4요소** (팀 메시지와 도현 보고 공통).
1. 산출물 경로
2. 확인 방법 (명령·메뉴·파일을 그대로 복붙 가능하게)
3. 검증 증거 (회귀 결과 수치, 스샷 경로, 폴리 수, 컴파일 상태)
4. 남은 이슈 / 판단 요청

실행하지 않은 것은 실행하지 않았다고 쓴다. "됐습니다" 한 줄 보고는 PD가 돌려보낸다.

**소리 알림 (2026-10-20 도현 요청).** 도현이 확인·입력해야 할 게 있는 보고를 끝낼 때만 PD가 `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/ask-dohyun.ps1 -Message "<한 줄>"` 를 부른다. 진행 보고·대기 알림·팀원 턴에는 부르지 않는다(Stop 훅 알림은 꺼져 있음).

**팀장 캐릭터 (2026-10-19 도현 결정).** `ask-dohyun.ps1` 은 먼저 화면 구석의 킬가르도 캐릭터(`D:\Github\GameStudio\tools\aegis-pet`)를 부른다 — 울음소리 + 블레이드폼 + 말풍선, 도현이 클릭하면 이 팀 화면이 열린다. 캐릭터를 못 부르면 예전 알림(소리 + 트레이 풍선)으로 대신한다.
- 도현의 판단·승인이 필요한 PD 보고는 **첫 줄을 `[결정] <한 줄 요약>`** 으로 쓴다. 진행 보고에는 붙이지 않는다.
- `-Message` 에는 그 첫 줄의 요약을 그대로 넣는다(말풍선에 뜬다). 한글 가능, 40자 안쪽.

**토큰 절약 (2026-10-20 도현 승인).** ① PD는 팀 메시지를 이미 읽은 것까지 다시 받지 않는다. ② `.claude/activeContext.md`는 지금 상태 한 화면만, 지난 기록은 `옵시디언/notes/`로. 시작 훅은 activeContext를 다시 넣지 않는다. ③ 큰 작업(하루 넘게) 전에 반나절짜리 시안으로 도현 방향 확인을 먼저 받는다. ④ 팀원 보고는 '결과 3줄 + 파일 경로 + 토큰 반납 3줄'로 짧게. 단순 확인·문서 대조는 낮은 모델로.

**PD → 도현 보고 형식.** 끝난 것 / 확인 방법 / 세 게이트 결과 / 도현이 판정할 것 1~3개 / 다음 후보. communication.md 톤.

**커밋.** 작업 단위, `type(scope): 설명`, body에 `Verified-By:` 실제 검증 내용(`no-compromise.md` §1). 게이트 PASS 전 커밋 금지. **main 직접 커밋**(도현 결정 2026-10-18), 브랜치는 선택. `[skip-verifier]`는 문서·정리 커밋에만.

## 7. 훅과 팀 모드 (알아둘 것)

- `.claude/settings.json`의 훅은 **Claude 팀원 전원**의 세션에서 돈다 (워크스페이스 공유). SessionStart가 `CONTROL.md`·`activeContext.md`를 주입하므로 Claude 팀원은 자동으로 현재 상태를 안다.
- `stop.sh`는 **전역** 컴파일 에러를 보고 턴을 막는다. 프로그래밍팀이 컴파일 깨진 채 턴을 끝내면 다른 팀 턴까지 막힌다 → 프로그래밍팀은 턴 종료 시 항상 컴파일 클린.
- `flash.ps1` 알림은 팀원마다 턴 끝에 뜬다. 시끄러우면 Stop 훅에서 flash만 빼는 건 도현 결정.
- Codex 팀원은 Claude 훅을 타지 않고 `AGENTS.md`를 읽는다. 상태는 PD가 메시지에 담거나 `CONTROL.md`를 읽게 한다.

## 8. 환경 (노트북)

| 항목 | 값 |
|---|---|
| OS / 셸 | Windows 11 / PowerShell 5.1 (한글 리터럴 깨짐 주의: `scripts/refresh-control.ps1` 주석 참조) |
| Unity | 6000.3.19f1 (ProjectVersion 실측 2026-10-19. 원 6000.3.9f1 미설치) |
| unity-cli | `C:/Users/stard/AppData/Local/unity-cli/unity-cli.exe` |
| Python | 있음 (`py`) |
| Blender | 5.2 `D:\Tools\Blender` (activeContext 세션33 기록, 아트팀이 `--version` 재확인) |
| Meshy | `.env.local`의 `MESHY_API_KEY` |
| AionUi 어시스턴트 | 별도 저장소 `stardohyun12/GameStudio`의 `setup/SETUP.md`로 생성 |

## 9. 첫 이터레이션 = 재기동 점검

main 마지막 커밋이 2026-05-27이므로 새 기능보다 현재 상태 재측정이 먼저다.

1. **PD**: `CONTROL.md`·`activeContext.md`를 읽고 "현재 상태 재측정" 태스크 묶음 생성, 프로그래밍팀에 `[EDITOR]` 토큰.
2. **프로그래밍**: 에디터 열기, `unity-cli editor refresh --compile`, 콘솔 에러 0 확인. 토큰 반납.
3. **QA**(토큰): `bash scripts/regression-check.sh` → PASS/FAIL/SKIP 집계. 5월 기준선은 12 PASS / CargoPolyBudget FAIL. 스샷 캡처 후 토큰 반납.
4. **아트**: 그 스샷으로 그래픽 게이트 1회 → 현재 룩 점수 기준선. Blender 유무 확인.
5. **기획**: activeContext "세션 33 우선순위" 5개를 스펙 후보로 재평가, scope 체크리스트 적용.
6. **PD**: `CONTROL.md` 날짜·상태 갱신, 도현에게 "지금 어디까지 왔나 + 첫 목표 후보 3개" 보고.

## 10. 속도·승인 규칙 (2026-10-20 도현 결정)

**PD 대기 금지.** PD는 일을 맡기면 바로 턴을 끝낸다. 턴 안에서 팀원을 기다리는 루프를 돌리지 않는다(그동안 도현 메시지가 묻힘). AionUi 앱 알림은 꺼 두었다(`notification_enabled=false`, 되돌리기: `echo '{"notification_enabled": true}' | aioncore config settings patch`). 도현을 부르는 소리는 `scripts/ask-dohyun.ps1` 하나.

**게이트 강도 두 단계.**
| 단계 | 언제 | 보는 것 |
|---|---|---|
| 시안 | 방향을 도현이 보기 전 | 눈에 띄는 것만: 관통·뜬 물체·끊긴 길·바다 위 물체·컴파일·회귀 |
| 마감 | 도현이 방향을 승인한 뒤 | 스펙 수용 기준 전부(수치 허용오차 포함) |
시안 단계에서는 고치기·재판정 왕복을 1회로 제한하고, 남은 건 목록으로 붙여 도현에게 보여 준다.

**QA는 에디터 없이.** 생성기·씬 빌드는 측정용 데이터(JSON)를 같이 내보낸다. QA·아트 판정은 그 데이터와 캡처로 동시에 한다. 에디터 토큰은 만드는 쪽만 쓴다.

**아트는 "만들기 전에 먼저 보여 준다" (도현 = 총책임자).** 순서:
1. **제작 계획 한 장** — 무엇을, 어떤 참고(실사·레퍼런스 게임 캡처)로, 어떤 방식(원화 생성/모델링 도구/코드 생성)으로, 몇 종을, 어디에 쓰는지. 도현이 "이대로 / 이렇게 바꿔"로 답한다.
2. 원화(시안 여러 장, 고를 수 있게) → 도현 승인·수정 지시
3. 제작 → 미리보기 → 도현 적용 승인
도현 답 없이 다음 단계로 넘어가지 않는다(asset.md 승인 순서를 계획 단계까지 앞당김).

**대화 비우기(토큰).** 조각·태스크 하나가 끝나면 PD 는 그 팀원 대화를 비운다(team_clear_agent_context). 이어지는 정보는 스펙·보고 파일에 있어야 한다. PD 자신도 큰 단락이 끝나면 activeContext 에 인계를 쓰고 `/compact`(도현 선호, 2026-10-20).

**생성기(도시 알고리즘)는 짧은 반복.** 한 번에 크게 만들지 않는다. 한 가지 변화(예: 광장 하나, 집 앉히기 하나)를 1~2시간 안에 만들어 도현에게 바로 알리고 다음으로 간다. 화면은 도현이 에디터에서 직접 본다 — PD 는 캡처를 붙이지 않고 씬 경로·볼 지점만 알린다(2026-10-20 도현). 캡처는 QA·아트 판정용으로만.

## 11. 전역 팀 규약 보충 (2026-10-20, `~/.claude/TEAM-PIPELINE.md` 반영)

전역 규약(딱지 프로젝트에서 재정의)이 이 문서의 바탕이다. 아래는 이 문서에 없던 것만 옮긴 것. 충돌하면 이 문서가 이긴다(예: 그림은 채팅에 띄우지 않고 씬 경로만 — §10).

- **메시지는 완료·막힘·질문 셋만.** 착수는 태스크를 `in_progress` 로 바꾸는 것으로 끝. 완료 보고는 10줄 이내, 마지막 줄에 쓴 토큰(대략). 질문은 모아서 선택지와 함께 한 번에.
- **분야가 분명한 일은 기획을 거치지 않고 그 팀에 바로.** 다른 팀을 풀어 주는 일(시험 도구·규격)부터 배분.
- **다음 태스크는 `blocked_by` 로 미리 건다.** 끝나는 순간 열린다. QA 는 구현 중에 테스트 플랜, 기획은 다음 스펙.
- **에디터는 `scripts/open-editor.ps1` 로만 띄운다**(WMI, 에이전트 Job 밖). `Start-Process` 금지 — 기억 비우기 때 에디터가 같이 꺼진다. 재시작은 PD 승인 뒤.
- **킥오프는 읽을 파일 경로만.** 긴 파일은 필요한 줄만(grep·머리 20줄). 할 일 없으면 아무것도 안 한다.
- **기억 파일 대응**(전역 이름 → 이 프로젝트): `docs/STATUS.md` → `.claude/activeContext.md`(압축 뒤 첫 번째로 읽기) · `docs/decisions/` → `옵시디언/notes/decisions-log.md` · `docs/handoffs/` → `옵시디언/notes/session-log.md`. 새 폴더 만들지 않는다.
- **측정**: `node tools/team_usage.js` — 세션별 토큰. 규칙 바꾼 뒤엔 전후 비교를 숫자로.
