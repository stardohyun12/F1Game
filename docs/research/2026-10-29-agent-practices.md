# AI 에이전트 운용 실무 조사 + 우리 팀 구조 평가 — 2026-10-29

읽은 내부 자료: `docs/research/2026-10-29-efficiency-review.md`, `2026-10-28-workflow-review-claude.md`·`-codex.md`, `scripts/team.sh`, `.claude/team/config.json`·`roles/*`·`lessons/art.md`, `scripts/fb*.py/sh`, decisions-log D-190~D-204, PD 스킬(pitcrew-studio).
표기: **확인** = 원문을 직접 열어 봄. **요약만** = 검색 결과 요약만 봄. **추정** = 내 판단.

---

## 0. 결론 (먼저)

- **상주 팀 구조는 유지한다. 단, 줄인다.** PD 1 + 일꾼 4줄(Claude `dev` / Codex `art` / Claude `artlive` 짧게 / 읽기 전용 `review`). 나머지 역할은 접는다.
- 갈아엎지 않는 이유: Unity·Blender GUI·Windows 로컬이 필수라 Codex 클라우드는 못 쓴다. 단일 Claude 세션+서브에이전트는 비용을 줄이지 않는다(같은 한도 풀, 공식 문서). 도현이 원하는 "보이는 진행"은 지금 대시보드가 이미 준다.
- 진짜 문제는 구조가 아니라 **세 가지**다. ① "끝"을 모델이 판정한다 ② 긴 `--resume` 세션 + 큰 과제 ③ 일 종류와 모델이 안 맞는다.
- **가장 큰 변경 3개 (순서대로)**
  1. "끝" 판정을 하네스로 옮긴다 — 팀원 전용 settings 에 Stop 훅 + team.sh 사후 검사. 보고서·`check.sh`·`fb_judge` 통과 없이는 끝 아님.
  2. 과제마다 새 세션 + 에셋별 `PROGRESS.md` 넘김. `--resume` 기본값 폐지. 과제 = 주제 하나·25턴.
  3. 일 종류로 모델을 고정한다 — Blender·메시·파이프라인 = Codex(high), Unity C# = Sonnet, 설계·취향 = Opus(PD 표시만). 역할 8개 → 4개.

---

## A. 조사 노트 (출처 포함)

### A1. Anthropic — 긴 작업 하네스
- https://www.anthropic.com/research/harness-design-long-running-apps — **확인**.
  - 실패 둘: 컨텍스트가 차면 일관성 잃음 + "context anxiety"(한도가 가깝다고 느끼면 서둘러 마무리). **자기 평가는 관대하다** — 디자인처럼 주관 과제에서 특히.
  - 처방: **컨텍스트 리셋(새 세션) + 구조화된 넘김 파일**. 압축은 연속성은 지키지만 깨끗한 판을 못 준다.
  - **생성자와 평가자를 분리.** 평가자도 처음엔 관대하다("문제를 찾고도 별것 아니라며 통과"). 평가자 로그를 사람 판단과 대조해 프롬프트를 고치는 튜닝이 필요했다.
  - 주관 품질은 "아름다운가"가 아니라 채점 기준 + few-shot 보정. 기준 문구가 결과를 한쪽으로 끈다.
  - 작업 전 "완료 기준(sprint contract)"을 파일로 합의. 에이전트 사이 소통은 파일.
  - 모델이 좋아지면 분해 장치를 걷어낸다. **평가자는 과제가 모델 단독 능력 밖일 때만 값어치가 있다.**
  - 비용 예: 솔로 20분 $9 vs 풀 하네스 약 4시간 $124.
- https://www.anthropic.com/research/effective-harnesses-for-long-running-agents — **요약만**. 첫 세션 = 초기화(init.sh·`claude-progress.txt`·첫 커밋), 이후 세션 = 기능 하나씩 + 기록 남김.
- https://github.com/anthropics/cwc-long-running-agents (Code with Claude 2026 예제, 유지보수 안 함) — **확인**.
  - 원시 장치 셋: **Default-FAIL 계약**(모든 기준이 false 로 시작, 증거 파일을 Read 해야만 true 로 쓸 수 있게 PreToolUse 훅이 막음) / **새 컨텍스트 평가자**(Write/Edit 없는 서브에이전트) / **에이전트가 쓰는 넘김**(`PROGRESS.md` 를 쓰고 매 재시작 때 먼저 읽음 + Stop 훅으로 커밋).
  - 실행 예: `claude -p "Read PROGRESS.md and build the next unfinished feature"`. 내장 `/goal` 이 같은 루프(별도 모델이 매 턴 조건 확인)를 제공.
  - "모델이 바뀌면 하네스 부품을 하나씩 빼 보고 아직 필요한지 본다."

### A2. Claude Code 공식 문서 — best practices
- https://code.claude.com/docs/en/best-practices — **확인** (2026-10 판, Opus 5.5 기본·1M 컨텍스트).
  - 지시는 둘: 비결정(프롬프트·CLAUDE.md·스킬 — 모델이 판단) vs **결정(훅·deny 규칙 — 하네스가 강제)**. "CLAUDE.md 줄을 강조해도 계속 놓치면 그 줄은 훅이어야 한다."
  - **Stop 훅**: 끝내려 할 때 스크립트 실행, exit 2 면 stderr 를 이유로 돌려보냄. 연속 8번 막으면 풀어 줌(`CLAUDE_CODE_STOP_HOOK_BLOCK_CAP`). `onFailure: "block"` 로 훅이 죽어도 막음.
  - 무인 실행 5문: 완료 기준? 스스로 돌릴 검사? 만질 범위? 무엇이 멈추나(`--max-turns`/`--max-budget-usd`)? 결과는 어디로·누가 승인?
  - 스크립트 실행: `--bare`(훅·스킬·CLAUDE.md 끔 → 재현성), `--settings` 로 필요한 것만 명시, `--permission-mode dontAsk`(허용 목록 밖은 즉시 거절). 턴 한도에 걸리면 오류로 끝난다.
  - 서브에이전트는 "맥락 보호용"이 아니다. 쓸 곳 = 병렬, 긴 출력 격리, **안 본 리뷰어**, 도구 제한. 같은 모델에 넘긴다고 답이 좋아지지 않는다. **같은 사용 한도를 쓴다.** 단순 일은 `model: haiku`.
  - 비용: 매 요청이 대화 전체를 다시 보낸다. 관계없는 과제는 새 세션. 구독 캐시 약 1시간. "기록이 필요 없으면 새 세션이 더 싸다."
  - 그만둘 것: 대문자 IMPORTANT, "계속해" 반복, CLAUDE.md 에 구조 설명. 대신 **검사를 주고 증거(명령+출력·스크린샷)를 요구**.
  - 병렬은 worktree 로 분리(`claude -w`).
- https://code.claude.com/docs/en/subagents — **요약만**. 서브에이전트 frontmatter 에 훅을 둘 수 있다. Stop 훅은 서브에이전트에선 SubagentStop 으로 바뀐다.

### A3. Codex
- https://learn.chatgpt.com/docs/developer-commands?surface=cli — **요약만**. `codex exec`(비대화), `--json`, `codex exec resume <ID>|--last`. resume 은 원래 대화·계획·승인을 그대로 이어 간다.
- https://learn.chatgpt.com/docs/agent-configuration/agents-md.md — **요약만**. Codex 는 `~/.codex/AGENTS.md` → 저장소 루트 → 하위 폴더 순으로 `AGENTS.md` 를 합쳐 읽는다(가까운 폴더가 뒤에 와서 우선). 합계 32KiB 상한.
  → **우리 함의**: 교훈을 Codex 에 따로 주입할 필요 없이 `tools/art/AGENTS.md` 같은 폴더별 파일에 두면 자동으로 읽힌다(Codex 리뷰가 지적한 "Codex 분기엔 교훈 주입 없음"의 해결책).
- https://developers.openai.com/api/docs/guides/latest-model/gpt-5.3-codex.md — **요약만**(옛 모델 문서). 기본 medium 권장, 어려운 일은 high/xhigh. "진행 보고·계획을 먼저 말하라"는 프롬프트는 롤아웃 중간에 멈추게 할 수 있으니 빼라.
- https://help.openai.com/en/articles/20001516 · https://chatgpt.com/codex/pricing/ — **요약만**. 5시간 창 + 주간 한도. Plus 기준 GPT-6 Astra 는 5시간에 로컬 메시지 5~45개 추정(비싼 모델). 클라우드 과제는 로컬보다 더 먹는다. Pro 상위 요금제는 5시간 한도 없음.
  → **우리 함의(추정)**: Astra 는 비싸다. 기계적 단계(검사·재임포트)는 더 싼 모델로 내려도 되는지 한 번 재 볼 가치가 있다. 지금은 주간 4% 라 급하지 않다.
- Codex 클라우드 — Unity 에디터·Blender GUI·Windows 경로가 필요한 우리 일에는 맞지 않는다(**추정**, 환경이 OpenAI 관리 컨테이너).

### A4. 구독 한도
- https://support.claude.com/en/articles/11145838 · https://support.claude.com/en/articles/11049741 — **요약만**. Pro/Max 는 5시간 창 + 주간 한도, Claude 앱·Claude Code 가 **한 풀**을 공유. Max 는 Fable 주간 한도 별도.
  → 우리 함의: PD(텔레그램 Claude Code)·팀원 `claude -p`·Hermes 서브에이전트·fb_judge 의 Claude 판정자가 **전부 같은 풀**이다. 서브에이전트로 옮겨도 아낄 게 없다.

### A5. 다중 에이전트 — 실무자 정리
- https://professionaldeveloper.net/2026/08/07/multi-agent-orchestration-from-one-agent-to-many-and-when-not-to/ — **요약만**. "기본은 에이전트 하나 + 좋은 도구." Berkeley 연구의 다중 에이전트 실패 14종(책임 소재 잃음, 한 에이전트의 착오 전파, 끝났다고 착각).
- https://trembit.com/blog/multi-agent-orchestration-llm-agents/ — **요약만**. 다중이 값어치 있는 조건 셋이 **모두** 맞을 때: 진짜 독립 하위 일 / 한 컨텍스트를 넘는 정보 / **도구·권한 영역이 다름**.
- https://joseparreogarcia.substack.com/p/claude-code-agent-teams — **요약만**. 조건 넷: 컨텍스트 초과 / 도구·제약이 진짜 다른 전문화 / 병렬로 시간 절약 / **안 본 독립 비평**. 실패 모드 "환각된 소유권" — 한쪽이 다른 쪽이 확인했다고 가정.
- https://developers.redhat.com/articles/2026/10/21/scaling-multi-agent-systems-hierarchical-orchestration — **요약만**. 오케스트레이터 하나가 맡는 에이전트가 늘면 라우팅 품질이 먼저 떨어진다.
  → 우리 함의: 역할 분리 근거는 **도구 영역**(Unity 에디터 / F1Studio+Blender / 웹)과 **독립 리뷰**뿐이다. "아트2", "맵생성"처럼 같은 도구를 쓰는 역할 복제는 근거가 약하다.

### A6. 넘김 파일(handoff)
- https://dev.to/r3dtuxedo/compact-clear-or-hand-off-running-long-agent-sessions-without-burning-your-limit-3hfp — **요약만**. 자리를 뜨기 전, 캐시가 따뜻할 때 HANDOFF 를 쓰게 하고 다음은 새 세션. 넘김은 대화록이 아니라 몇백 단어. 티켓마다 progress 파일 하나.
- https://github.com/Martin-Ferreira-O/claude-handoff-kit — **요약만**. 계획(Opus) → 구현(Codex 또는 새 Claude)을 `docs/handoff/<slug>/{CONTEXT,PLAN,PROGRESS,DECISIONS}.md` 로 넘김. PROGRESS 없이 커밋하면 PreToolUse 훅이 막음.
- planning-with-files 스킬(task_plan/findings/progress.md) — **요약만**. 커뮤니티 관행으로만 참고.

### A7. VLM 판정자
- https://arxiv.org/html/2604.25235v1 "VLM Judges Can Rank but Cannot Score" — **요약만**. **순위는 맞히고 절대 점수는 못 맞힌다**. 나쁜 것은 올리고 좋은 것은 깎아 가운데로 몰림 → 애매하면 쌍대비교.
- https://arxiv.org/html/2604.17768v1 "When VLMs Judge Without Seeing" — **요약만**. 판정자가 이미지를 덜 보고 **글이 많은 쪽**을 고르는 경향. 위치·길이 편향.
- https://briankeithn.github.io/files/2025_VLM.pdf — **요약만**. 판정자 3명 평균이 1명보다 신뢰도 높음(ICC 0.72 → 0.88).
  → 우리 함의: `fb_judge.py` 의 "전|후 + 도현 원문 + 아직 보이나 YES/NO, 판정자 둘"은 연구와 맞는 방향이다(상대 판정·좁은 질문·여럿). 약점 둘: 팀원이 쓴 글이 판정자에게 들어가면 글에 끌린다(프롬프트 `{last}` 칸 점검 필요), 도현의 [넘기기]/[다시] 와 대조한 적중률을 아직 안 잰다.

### A8. 3D·게임 아트에서 AI
- https://arxiv.org/html/2504.01786v1 BlenderGym (CVPR 2025) — **요약만**. 최신 VLM 도 초보 Blender 사용자에게 쉬운 편집에서 크게 뒤진다. **절차적 재질·지오메트리 편집이 가장 약함**. 생성 여러 개 + 검증자 선택이 품질을 올림(옛 모델 기준).
- https://www.3daistudio.com/3d-generator-ai-comparison-alternatives-guide/blender-mcp — **요약만**(업체 글, 편향 가능). Blender MCP 는 메시를 "생성"하지 않고 조작한다. 장면 조립·재질·조명·반복 작업은 강하고, 유기적 형태는 약하다. **긴 세션은 느려지고 비싸지고 한 일을 잊는다.** "작게 쪼갠 명시적 지시"가 낫다.
- https://www.mcp-for-blender.com/docs/concepts/capabilities — **요약만**. 같은 결론: 복잡한 작업은 작은 단계로.
- Tripo 블로그(https://www.tripo3d.ai/blog/explore/style-guides-for-ai-generated-3d-asset-packs 등) — **요약만, 업체 홍보**. 쓸 만한 부분: 스타일 시험 묶음 3~5개 먼저, 밀도·UV 통일, 공용 마스터 재질. (우리는 "AI 티" 거부로 생성 메시는 안 씀 — 참고만.)
- Unity 공식 MCP·AI gateway(Unity 6+) — https://unity.com/blog/unity-ai-mcp-how-to-get-started · https://unity.com/resources/what-is-unity-ai — **요약만**. Claude Code·Codex 를 에디터에 공식 연결(오픈 베타, Unity Cloud 연결·구독 필요). 우리 에디터 6000.3 이라 조건은 됨. 지금 `ucli-http.sh` 가 돌아가니 **급하지 않음**.
- 게임 "에이전트 스튜디오" 저장소(https://github.com/k1bot2026/GameDev-Team — 8 역할, https://github.com/samuk10/Agents-Game-Studios — 49 서브에이전트) — **요약만**. 역할이 많은 템플릿이 유행이지만 결과 증거(출시·지표)는 못 찾음. **검증 안 됨** — 따라 하지 않는다.

---

## B. 우리 구조를 기준과 대조

| 기준(출처) | 우리 현황 | 판정 |
|---|---|---|
| 새 세션 + 넘김 파일(A1·A2·A6) | team.sh 기본이 `--resume`. Codex 도 `exec resume`. art 입력 토큰이 과제마다 4.6M→7.3M→9.6M 로 커짐(live.log). autorotate 는 $15 넘어야 돎 | **어긋남** |
| "끝"은 하네스가 판정(A2 Stop 훅, A1 Default-FAIL) | `--setting-sources user` 로 프로젝트 훅 전부 끔. 보고서·검사는 글로만 요구. artlive 29회 중 13회 ❌, programmer 118회 중 24회 ❌(live.log `끝:` 줄) | **어긋남** |
| 턴 한도 = 멈춤 장치(A2) | `--max-turns` 있음. `--max-budget-usd` 는 실측상 안 지켜짐($10 → $50~100) | 반쯤 |
| 생성자 ≠ 평가자, 좁은 상대 판정(A1·A7) | `fb_judge.py`: 전|후 + 원문 + YES/NO, 판정자 둘 | **맞음** — 보정만 필요 |
| 다중 에이전트는 도구 영역이 다를 때만(A5) | 8 역할 중 art/art2/artlive/artui 가 같은 Blender 영역, programmer/mapgen 이 같은 영역 | **과함** |
| 서브에이전트는 비용 절약 수단 아님(A2·A4) | 같은 풀 | 바꿀 이유 없음 |
| Blender 는 작게 쪼갠 명시적 단계(A8) | `build_asset.py` 단일 파이프라인 | **맞음** |
| AI 는 배치·조립에 강하고 모델링에 약함(A8) | D-162/D-189: 사진 재료 + 키트 + 도현 배치 | **맞음** |
| 짧은 지시 파일, 강제 규칙은 훅으로(A2) | PD 스킬 SKILL.md **116KB** + 참조 58KB. 역사 서술·옛 규칙·충돌 규칙이 섞임. PD 실수 목록의 "틀린 전제"와 관련 있을 것(**추정**) | **어긋남** |
| Codex 는 AGENTS.md 계층을 자동으로 읽음(A3) | Codex 분기엔 교훈 주입 없음, 첫 과제에만 역할 파일 | **어긋남** |
| 모델-일 맞춤(A2·A3 effort) | config.json: programmer·qa·mapgen·art2 가 아직 **opus**(D-202 와 다름). Sonnet 이 Blender 메시 25턴 실패, Codex 는 $0 성공. Codex 주간 4% | **어긋남** |

---

## C. KEEP / CHANGE / STOP

### KEEP (지킨다)
1. **PD 1명 + 로컬 headless 일꾼** 구조. Unity·Blender GUI 는 로컬에만 있다. 대시보드·live.log·state 파일은 도현이 "진행이 보인다"고 느끼는 창이다.
2. **`fb.py` → `fb_shot.sh`(도현 카메라 그대로 재촬영) → `fb_judge.py`(판정자 둘, YES/NO) → 도현 [넘기기]/[다시]**. "한 번 말하면 제대로"(D-204)에 대한 맞는 방향이다. 연구(A1·A7)와 맞다.
3. **`tools/art/build_asset.py` 단일 파이프라인**과 `check.sh` 게이트. 재생성 가능 = 완료.
4. **F1Studio 분리**(에디터 둘, 잠금 분리). 같은 에디터를 두 일꾼이 쓰지 않는다.
5. **쌍대비교 판정(JUDGE.md)**, 10점 폐기. 연구가 그대로 뒷받침(A7).
6. **턴 한도로 비용 묶기.** 예산 플래그는 믿지 않는다.

### CHANGE (바꾼다) — 파일 · 변경 · 효과 · 품

| # | 파일 | 변경 | 기대 효과 | 품 |
|---|---|---|---|---|
| C1 | 새 `.claude/team/worker-settings.json` + 새 `scripts/team_stop_gate.py` + `scripts/team.sh` | `--setting-sources user` 는 두고 `--settings .claude/team/worker-settings.json` 추가. Stop 훅 = `python scripts/team_stop_gate.py`: ① `docs/team/reports/<role>-<STAMP>.md` 존재 + "검증" 절 비지 않음 ② 과제 원문에 코멘트 id 가 있으면 `tools/viewer/feedback/judge/<id>.json` PASS ③ 아트 과제면 `check.sh` 마지막 줄 PASS. 아니면 exit 2 + 빠진 것 출력. `onFailure: block`. **PreToolUse 훅**: `fb.py fixed` 를 judge json 없이 부르면 거부 | "고쳤다" 뒤 재지적 감소. 보고서 없는 끝 0 | 2~3h |
| C2 | `scripts/team.sh` (사전·사후) | 턴 한도로 끝나면 Stop 훅이 안 돌 수 있다(**미확인**). → 시작 **전에** team.sh 가 보고서 뼈대(과제 원문·끝 조건·빈 진행 표)를 직접 만든다. 끝난 뒤 뼈대 그대로면 state 를 `failed-noreport` 로 쓰고 대시보드 ⛔. Codex 도 같은 사후 검사 | PD 가 로그 뒤지는 시간 감소 | 1h |
| C3 | `scripts/team.sh` | **기본 = 새 세션.** `--resume` 은 과제 원문에 `[RESUME]` 일 때만. Codex 도 같은 규칙(`codex exec` 새 스레드). 새 세션 프롬프트 첫 줄 = "먼저 `docs/team/progress/<대상>.md` 를 읽어라" | 과제당 비용·끊김 감소. art 입력 토큰 폭증 멈춤 | 1h |
| C4 | 새 `docs/team/progress/<에셋 또는 영역>.md` (형식 1장) + `roles/*.md` 한 줄 | 에셋별 넘김 파일: 목표 / 한 것(날짜·커밋) / 열린 코멘트 id / 다음 한 걸음 / 하지 말 것. 팀원이 매 과제 끝에 갱신, C1 훅이 수정 시각 확인 | 세션을 버려도 기억이 남음. 도현도 이 파일로 진행을 봄 | 1h |
| C5 | `.claude/team/config.json` | programmer·qa·mapgen·art2 `opus` → `sonnet`. `[OPUS]` 만 opus. effort: dev `medium`, review `high` | Opus 호출 대부분 제거(D-202 를 실제로 적용) | 5분 |
| C6 | `scripts/team.sh` (라우팅) | 과제 원문에 `blender`·`bpy`·`build_asset`·`메시`·`check.sh` 가 있고 역할이 Claude 면 **시작 전에 거부**하고 "art(Codex)로" 출력. 예외 표시 `[CLAUDE-OK]` | Sonnet 25턴 헛돔 재발 방지. Codex 주간 사용률 4% → 30~60% | 30분 |
| C7 | 새 `tools/art/AGENTS.md`, 새 `F1/AGENTS.md` | `lessons/art.md`·`lessons/programmer.md` 를 폴더별 AGENTS.md 로 옮긴다(Codex 자동 로드). Claude 쪽은 team.sh 가 같은 파일을 sys.md 에 붙인다 | 교훈이 두 엔진에 같은 버전으로 감 | 30분 |
| C8 | `.claude/team/roles/` · `scripts/team_dashboard.py` · config | 역할 8 → 4: **`dev`**(programmer+mapgen 합침, Sonnet, [EDITOR]) / **`art`**(Codex — Blender·파이프라인·메시·검사 전부) / **`artlive`**(Claude, F1Studio 에디터 작업·형태 판단만, 짧게) / **`review`**(qa 를 읽기 전용 새 세션으로: Read·Grep·캡처만). `art2`·`artui`·`mapgen`·`research` 는 `roles/_archive/` 로. 리서치는 PD 의 Hermes 하위 에이전트로 | PD 라우팅 실수·세션 관리 감소 | 1h |
| C9 | `scripts/fb_judge.py` | ① 판정자에게 팀원이 쓴 설명을 넣지 않는다 — 그림 + 도현 원문만(A7 글 편향) ② 도현 [넘기기]/[다시] 때 `feedback/judge/agree.csv` 에 (판정자 결과, 도현 결과) 한 줄 → 주 1회 적중률 | 판정자 관대함을 숫자로 봄 | 1h |
| C10 | PD 스킬 `pitcrew-studio/SKILL.md` (Hermes 스킬 폴더 — **이 조사에선 안 고침**, PD 할 일) | 116KB → 20KB 이하. 남길 것 = 현재 규칙·명령·함정. 역사·옛 D-번호 서술은 `references/history.md` 로. 꼭 지켜야 하는 것은 훅·스크립트로 옮기고 스킬에서 지움 | PD 의 틀린 전제·옛 규칙 따르기 감소(**추정**) | 2h |
| C11 | 과제 작성 규칙(`references/resident-team.md`) | 과제 = **주제 하나, 25턴**. 첫 줄 = "끝 조건 1줄"(예: 코멘트 c12·c13 judge PASS + check.sh PASS). 같은 역할이 두 번 연속 턴 한도면 한도를 올리지 말고 쪼갠다 | 턴 한도 실패율 45% → 15% 이하 목표 | 0h(규칙) |

### STOP (그만둔다)
1. **`--resume` 을 기본으로 쓰기.** 긴 세션 = 비싸고, 잊고, 끊긴다(A1·A2·A8).
2. **같은 도구 영역에 역할 복제**(art/art2/artui, programmer/mapgen). 근거 없는 다중화(A5).
3. **"N턴 넘으면 보고서부터" 같은 글 지시에 기대기.** 세 번 안 지켜졌다. 훅으로(A2).
4. **Claude 에 Blender 메시 탐색 맡기기.** Codex 가 $0 에 한 일을 Sonnet 이 25턴 날렸다.
5. **`--max-budget-usd` 를 안전장치로 믿기.** 안 지켜진다.
6. **PD 가 전|후를 안 보고 "고쳤어" 전달하기.** C1 이 막지만 PD 도 본다.
7. **새 도구(뷰어·창)를 "어디서 쓸지" 안 묻고 만들기.** 웹 3판 버림.
8. **스킬에 사건 서술 쌓기.** 규칙은 짧게, 강제는 훅으로(A2).

---

## D. 구조 질문에 대한 답

**질문: 상주 팀(PD + team.sh 역할 세션 + Codex)을 유지하나, 바꾸나?**

| 안 | 장점 | 단점 | 판정 |
|---|---|---|---|
| A. 지금 그대로 | 이미 돌아감 | B 표의 어긋남 그대로 | ✕ |
| B. **줄인 상주 팀**(PD + dev/art/artlive/review, 새 세션 기본, 훅 게이트) | 도구 영역별 분리 유지, 진행 보임, Codex 한도 활용 | team.sh 손볼 일 4~6h | **추천** |
| C. 단일 Claude Code 세션 + 서브에이전트 | 설정 단순 | **같은 한도 풀**(아낄 것 없음), Codex 못 씀, 텔레그램 PD 세션이 길어져 더 비쌈, 대시보드 진행 표시 잃음 | ✕ |
| D. Codex 클라우드 과제 | 병렬, 로컬 부하 없음 | Unity 에디터·Blender GUI·Windows 경로 없음. 로컬보다 한도를 더 먹음(A3) | ✕ (코드만 만지는 리팩터 외엔) |
| E. 전부 Codex | Codex 한도 넉넉 | Unity C#·에디터 조작은 Claude 쪽 도구·훅이 갖춰져 있음. 판정자 다양성 잃음 | ✕ (아트 기계 단계만 Codex 로) |

**추천: B.** 구조는 맞다. 운영 방식이 틀렸다.

**가장 큰 변경 3개 (이 순서로):**
1. **C1+C2 — "끝"을 하네스가 판정.** 도현 불만("한 번 말하면 제대로")과 턴 한도 끊김을 같이 친다. 글 지시는 3번 실패했다.
2. **C3+C4+C11 — 새 세션 + PROGRESS 넘김 + 주제 하나 25턴.** 비용·끊김·잊음을 같이 친다.
3. **C5+C6+C8 — 일 종류별 모델 고정 + 역할 4개.** Opus 낭비와 Sonnet 헛돔을 없애고 Codex 한도를 쓴다.

**지표(2주 뒤 대조):** 도현 통과 에셋 수(지금 0) / 같은 코멘트 [다시] 비율 / 턴 한도 실패율(artlive 45%, programmer 20%) / Codex 주간 사용률(4%) / 과제당 평균 $.

---

## E. 확인 못 한 것
- 턴 한도(`--max-turns`)로 끝날 때 Stop 훅이 도는지 — 문서상 오류 종료라 **안 돈다고 가정**, C2 로 보완. 실제 시험 필요.
- `--settings` 로 준 훅이 `--setting-sources user` 와 같이 적용되는지 — **추정**(명시 주입이라 될 것). 첫 과제 전에 빈 과제로 시험.
- Codex 에 훅 기능이 있는지 — 찾지 못함. Codex 는 team.sh 사후 검사(C2)로만 막는다.
- GPT-6 Astra 대신 더 싼 Codex 모델로 기계 단계 품질이 유지되는지 — 미측정.
- 실무자 블로그(A5·A6)·논문(A7·A8) 대부분은 검색 요약만 봤다. 방향이 공식 문서(A1·A2)와 같아서 근거로 썼다.

**F 심화 조사 뒤 갱신 (2026-10-29)**
- ~~Codex 에 훅 기능이 있는지 — 찾지 못함~~ → **있다**(공식 hooks 문서, F3). Stop exit 2 로 계속시킴. 단 신뢰 승인 흐름을 비대화 `codex exec` 에서 실제로 넘기는지(`--dangerously-bypass-hook-trust`)는 **미시험**.
- 턴 한도 종료 때 Stop 훅 — 공식 문서는 "턴 한도 = 오류 종료", "API 오류 = StopFailure" 까지만 말하고 턴 한도에서 Stop 이 도는지는 명시 안 함. **여전히 미확인** → C2 를 주 게이트로(G2).
- `--settings` + `--setting-sources user` — 공식 문서상 `--settings` 는 파일 출처와 별개인 "명령줄" 층이라 적용될 것. **실험은 아직**.
- Codex 가 `-C <루트>` 일 때 하위 폴더 AGENTS.md 를 안 읽는다는 것 — 공식 문서 문구("현재 폴더에서 탐색을 멈춘다")로 판단, **우리 환경에서 `List the instruction sources you loaded` 로 확인 안 함**.
- 우리 artlive 과제가 Claude `-p` 의 10분 백그라운드 상한에 걸린 적이 있는지 — live.log 미대조.
- 우리 팀원이 `.unity`·`.prefab` 을 텍스트로 직접 고친 적이 있는지 — 미점검(G4 훅의 필요성 판단 근거).
- EditJudge-Bench 수치(AUROC 67%, 공간 편집 찍기)가 우리 판정자(Sonnet·Astra)·우리 코멘트 종류에도 맞는지 — 우리 `agree.csv` 가 없어 미측정. 연구는 GPT-4o 기준.
- EditHero 의 Opus 5.5 > Astra(medium) 차이가 우리 Blender 파이프라인 과제에도 나는지 — 미측정(연구는 부품 편집, 실행·탐색 비용은 안 봄).
- 검색 요약만 본 항목(F 제외 목록): Codex 블로그 3개, everydayUnity, Capcom 사례, Ralph 비용 사례 일부, 프론트엔드 VLM 비평가 논문, r/blender 글.

---

## F. 심화 조사 (원문 확인만)

규칙: 아래는 **원문을 web_extract 로 직접 열고** 내용을 인용·요약한 것만 셌다. 검색 요약만 본 것은 끝의 "제외" 줄에 따로 적었다(근거로 안 씀).

### F1. AI 에이전트로 게임 만든 실제 기록 (2025~2026)

- https://www.dan-malone.com/blog/ai-agents-game-godot-to-unity (2026-10-03, Crush Depth) — **확인**.
  - 코드 전부 Claude Code. 사람은 "요청 쓰기·스샷 보기·빌드 플레이"만. Godot 판 38,450줄 + Python 테스트 124개.
  - "가장 쓸모 있는 건 게임 코드가 아니라 작은 TCP 다리" — 줄당 JSON 명령 하나(상태 읽기·N프레임 진행·버튼·스샷·종료), 고정 시드 lockstep. 124개 중 92개 테스트가 다리만 쓴다 → 엔진을 바꿔도 테스트·봇·MCP 가 그대로. "에이전트로 게임 만들면 이것부터 만들어라."
  - 모델이 옛 API 를 쓴다(Unity `rb.velocity` → `linearVelocity`). 처방 둘: **고정 버전 엔진 소스에서 답하는 API 조회 도구**(`unity_api.py`) + **매 파일 편집 훅**이 옛 API 를 막고 고칠 줄을 출력. **Unity 훅은 씬·프리팹 파일 손편집도 막아 에디터 스크립트로만 바꾸게** 한다. 엔진별 "모델이 모르는 변경·함정" 문서(Unity 54줄).
  - 엔진 선택 판정: 새 에이전트들이 **눈가림**으로 키아트 + 라벨 없는 두 프레임(순서 섞음)을 봄 → Unity 12/12 승. 단 "판정자는 에이전트지 플레이어가 아니다" — 결국 **본인이 직접 플레이**하고 결정.
  - Unity 재구축 첫 판 56분, 막힘 12개(각 2~14분). worktree 마다 `Library` 5.7GB 필요 → 따뜻한 Library 복제로 새 레인 74초(차가운 임포트 223초). Unity 작업 하나가 RAM 10GB.
- https://ahmetbalaman.com/en/blog/building-a-3d-game-in-one-day-with-ai-agents-boingward-en/ (2026-10-04, BOINGWARD) — **확인**.
  - Claude Code 여러 세션 동시, 하루 148커밋. 사람 = 결정 + 폰 테스트.
  - **Unity 를 첫 시간에 버림**: "Unity 프로젝트는 한 번에 한 에디터만, 배치 빌드 느림, 바이너리 씬 + .meta 가 병렬 에이전트끼리 충돌."
  - 병렬 규칙: 세션마다 **소유 폴더**를 적어 둠, 공유 파일은 리드만 고침(나머지는 보고서에 요청), 작은 인터페이스 계약(`get_move()` 등). 동시에 전체 테스트를 돌리면 CPU 포화로 랜덤 실패 → **기계 전역 잠금으로 전체 테스트는 한 번에 하나**.
  - 규칙 파일의 가장 중요한 줄: **"돌려 보지 않은 기능은 된다고 말하지 마라. 폰에서만 확인 가능하면 정확히 그렇게 써라."**
  - 가장 쓸모 있던 테스트 = **코스 봇**(실제 물리로 모든 점프를 시도, 봇이 못 닿으면 코스가 틀림). 하지 말 것 폴더(거부된 시안 이미지)를 함께 줘서 아트 방향 논쟁을 줄임.
  - 정직한 목록: 플레이 가능 / 숨김 / 반쯤 / 미측정 / 미출시.
- https://vbgnt.com/blog/ai-coding-agent-game-development/ (2026-09, 50+ 게임, 업체 글이라 홍보 편향 있음) — **확인**.
  - 잘하는 것 = 메커닉 하나짜리 작은 완성 게임. 못하는 것 = 많은 저작 콘텐츠, 아무도 안 적은 시스템 상호작용, **손맛**("점프가 둥둥 뜨는지 모른다"). 루프 = 사람이 90초 플레이 → 무엇이 이상했는지 말함 → 수정 → 다시.
  - Unity 표: "씬·프리팹은 ID 가득한 큰 YAML, 에디터가 무거움, **보려면 MCP 다리 필요**."
  - 가장 값진 단계 = 부팅 시간·프레임·오류를 보고하는 **자동 플레이테스트**. "눈을 줘라" — 실행 화면 스샷 + 생성 3D 모델을 **라벨 붙은 여러 각도**로(뒤돌아 선 캐릭터·벽 속 카메라 잡음). 생성 모델은 방향·축척·피벗을 재서 고친다.
  - 실패 사례 둘이 우리와 직결: ① 에이전트가 멈춘 미리보기를 치우려 `pkill -f godot` → 시스템 프롬프트가 CLI 인자로 들어가 있어 **자기 프로세스까지 죽임**(exit -9, 메모리 부족처럼 보임) ② **비대화 모드 CLI 는 백그라운드 작업을 10분까지만 기다리고 턴을 끝냄** → 고아 렌더가 다음 턴 CPU 를 먹음.
  - 스킬 규칙: "모든 규칙은 한 번 맞은 버그."
- https://www.kevinlondon.com/2025/06/04/building-game-with-ai/ (2025-06) — **확인**. 비게임 개발자. MCP 로 실행·오류 수집을 붙이자 크게 나아짐("게임을 띄우지도, 됐는지도 몰랐다"). 교훈: 피드백은 **여러 개를 한 프롬프트에 묶기**(사용량 절약), "**두 번의 90%**" — 다듬기·밸런스가 처음 구현보다 오래 걸림.
- https://unity.com/blog/unity-plugin-for-claude-code (2026-09-09, Unity 공식) — **확인**. Unity CLI(Unite Seoul 발표) + 공식 Claude Code 플러그인, 스킬 29개(`/unity-cli` = 라이브 에디터에서 GameObject·씬·C# 실행, 빌드·로그; `/physics-3d-collision`, `/urp-postprocessing` 등). 목적 = "일반 에이전트가 하는 그럴듯한 추측"을 Unity 의 정답 절차로 대체. 다른 에이전트(=Codex)용은 "올해 안".

**F1 이 우리에게 뜻하는 것**
- 다섯 다 같은 말: **에이전트가 게임을 스스로 돌려 보고 결과를 기계가 판정**해야 한다. 우리는 `ucli-http.sh`·봇·`fb_shot.sh` 로 이미 반쯤 있다. 빠진 것 = (a) "돌리지 않았으면 됐다고 말하지 마라"를 훅으로 강제(C1 과 같음), (b) **Unity 씬·프리팹 YAML 손편집 차단 훅**(Malone) — 우리 팀원이 `.unity` 를 텍스트로 고친 적이 있는지 점검 필요(**추정**), (c) Unity 6000.3 API 함정 목록 + 편집 훅(옛 API 차단).
- Unity 는 병렬에 불리하다는 실무 보고가 둘(BOINGWARD·Malone). 우리의 "에디터 하나에 [EDITOR] 과제 하나 + F1Studio 분리"는 이 제약에 대한 맞는 답이다. 에디터를 더 늘리지 않는다.
- 손맛·분위기는 사람 판정(셋 다 명시). 도현 판정 자리를 줄이려 하지 말고 **도현 판정 전 단계를 기계로 거르는 것**이 맞는 방향.
- vbgnt 의 "비대화 CLI 10분 백그라운드 대기" — Blender 렌더·Unity 임포트를 백그라운드로 띄우는 과제에서 같은 끊김이 날 수 있다(**미확인**, 우리 live.log 에서 대조 필요).

### F2. 3D·게임 아트에서 AI — 실사용·벤치마크·스튜디오

- https://arxiv.org/html/2610.02298v1 EditHero (2026-10-01) — **확인**. 한 에셋을 최대 30턴 연속 부품 편집(추가·삭제·교체·재칠)하고 매 턴 정답과 비교.
  - 코드로 고치는 LLM 에이전트(현재 메시를 보고 → 코드로 이름 붙은 부품만 수정 → 렌더 확인 후 확정)가 생성형 3D 편집기보다 낫다: 나머지 보존 CC 0.95~0.98(생성형 ≤0.90), 지시 이행 IF **Opus 5.5 0.62 / Astra(medium) 0.53 / Fable 5.1 0.52 / Sol 6 0.42** (생성형 ≤0.36).
  - **삭제는 쉽다(IF 0.88~0.99), 추가·교체는 어렵다(0.10~0.49, 0.17~0.52)** — "코드로 새로 만든 형태는 크기·모양을 자주 놓친다."
  - 느리다: 편집 1건에 **1.5~6분, 모델 호출 6~16회**(중앙값). 생성형은 20~50초.
- https://arxiv.org/html/2601.11109v1 VIGA (UC Berkeley 외) — **확인**. 원샷 VLM 은 공간·물리 근거가 없어 Blender 재구성을 못 한다 → **쓰기→실행→렌더→비교→수정** 닫힌 고리 + 생성자/검증자 역할 교대 + 계획·코드 diff·렌더 이력 기억. BlenderGym 원샷 대비 +35%, 새 BlenderBench(카메라 맞추기·다단계·합성 편집 30과제) +125%. BlenderGym 은 카메라가 고정이라 "3D 추론이 2D 비교로 줄어든다"는 한계를 스스로 지적. 기억 없는 반복(BlenderAlchemy)은 합성 편집에서 일관성을 잃음.
- https://github.com/will-rice/llm-self-improvement-papers/blob/main/papers/arxiv-2610-13098v1--aabc732740d9.md VETR (2026-10-09 arXiv 사본) — **확인(초록·서론만)**. 최종 렌더만 보는 피드백은 모호하다 → 실행 기록(어떤 편집이 살아남고·사라지고·간섭했나)을 뽑아 대안 프로그램을 만들고 보존 조건을 실제 상태로 검증 → 그다음 시각 검증자. BlenderGym 280·BlenderBench 27 에서 VIGA·BlenderAlchemy 보다 낫다고 보고(수치는 본문 표, 미확인).
- https://www.reddit.com/r/ClaudeAI/comments/1ritdv6/ (Claude Code + BlenderMCP) — **확인**. "Opus 4.6 은 3D 공간 관계가 어렵다", "한 번은 완벽히 놓고, 그 지시를 기록해 다시 시키면 틀린다 → 결국 **정확한 x,y,z 를 주는 아주 구체적 연산**으로만 반복 가능했다."
- https://www.reddit.com/r/OpenAI/comments/1we95z2/ (Blender MCP + Astra) — **확인**. "코딩처럼 80% 해법인데, 코드와 달리 결과를 고쳐 쓰기 어렵다." 쿼드를 요구해도 에지 흐름이 없어 메시를 다시 짜야 함. 실루엣·블록아웃은 됨. 댓글: 이미지→모델은 Tripo 가 낫고 Astra+Blender 는 그 위에서 리깅·연출, "고개를 정면으로 돌려라" 같은 단순 변형도 실패, 애니메이션은 **참조 영상이나 시작·끝 프레임**을 주면 훨씬 낫다, 3D 작업은 effort high 가 그나마 낫고 더 올리면 느리기만 함.
- https://www.gamedeveloper.com/business/one-third-of-game-workers-use-generative-ai-but-half-think-it-s-bad-for-the-industry (GDC 2026 State of the Game Industry, 2,300명) — **확인**. 생성 AI 사용처 = 조사·브레인스토밍 81%, 일상·코드 47%; 에셋 생성 19%·절차 생성 10%·플레이어 대면 5%. 52% 가 "업계에 해롭다"(시각·기술 아트 64%).
- https://gamedevreports.substack.com/p/vaia-how-game-studios-use-ai-in-2026 (VAIA, 스튜디오 리더 101명 인터뷰, 2026 Q2~Q3) — **확인**. 97% 가 AI 를 써 봄, 쓰는 곳 = 코딩 86%·생산성 83%·**컨셉·프리프로덕션 79%**. 그런데 파이프라인 통합 깊이 자체 평가 **중앙값 3/10**.

**F2 가 우리에게 뜻하는 것**
- 우리 결론("AI 는 배치·조립·수정에 강하고 무에서 모델링에 약함")은 2026 벤치마크로도 그대로다. 더 정확히: **이미 있는 것을 지우기·옮기기·재칠은 잘 하고, 새 형태를 만드는 건 절반도 못 맞힌다.** 도현 코멘트 과제를 "삭제/이동/재칠"과 "새 형태 추가"로 나눠 앞쪽은 바로 맡기고, 뒤쪽은 사람 제작 키트·사진 부품에서 가져오게 한다.
- 3D 편집 지시 이행은 **Opus 5.5 > Astra(medium)**(0.62 vs 0.53). "형태·취향 판단은 Opus, 기계 단계는 Codex"(D-202) 배분이 수치로도 맞다. 반대로 Blender **실행·탐색·파이프라인**은 여전히 Codex 쪽(턴당 비용·한도).
- 실무자 공통 처방 = **정확한 좌표·이름 붙은 부품·렌더 확인 루프**. 우리 `build_asset.py` + 이름 붙은 낱개 오브젝트 + 고정 카메라 전/후가 정확히 이 형태다. 빠진 건 VETR 식 **"편집이 실제로 남았나"를 상태로 검사**(예: 고친 오브젝트의 transform·재질 이름 diff 를 렌더 판정 **전에** 기계로 확인).
- 편집 1건 1.5~6분·6~16호출이 연구 수치다. 우리 artlive 한 과제 15~25턴은 편집 2~3건이 상한이다 → C11 "주제 하나, 25턴"이 맞다.
- 업계도 AI 를 아트 최종물에 거의 안 쓴다(19%). 도현의 "AI 티" 거부는 업계 평균과 같은 쪽이다.

### F3. Codex CLI 실무

- https://learn.chatgpt.com/docs/agent-configuration/agents-md.md (OpenAI 공식) — **확인**(A3 의 "요약만"을 확인으로 올림). 실행마다 `~/.codex` → Git 루트 → 현재 폴더까지 폴더당 하나(`AGENTS.override.md` > `AGENTS.md`)를 이어 붙임, 가까운 폴더가 뒤 = 우선. 합계 `project_doc_max_bytes` 32KiB 넘으면 **잘림**. **현재 폴더보다 아래 폴더의 AGENTS.md 는 안 읽는다** — "Codex 는 현재 폴더에서 탐색을 멈춘다." 확인법: `codex --ask-for-approval never "Summarize the current instructions."`.
- https://learn.chatgpt.com/docs/hooks.md (OpenAI 공식) — **확인**. **Codex 에도 훅이 있다**: `PreToolUse`·`PostToolUse`·`Stop`·`SessionStart` 등, 위치 `~/.codex/hooks.json` 또는 `<repo>/.codex/hooks.json`. **`Stop` 훅이 exit 2(stderr = 이유) 또는 `{"decision":"block","reason":...}` 를 내면 Codex 는 그 이유를 새 사용자 프롬프트로 삼아 계속한다.** 함정: 비관리 훅은 **해시 기준으로 신뢰 승인 전엔 건너뜀**(`/hooks` 에서 승인, 또는 `codex exec --dangerously-bypass-hook-trust`). 프로젝트 `.codex/` 층은 프로젝트가 신뢰돼야 로드.
- https://docs.arantic.com/codex/automation (서드파티 문서, 공식 문서를 정리) — **확인**. `codex exec`: 진행은 stderr, 최종 메시지만 stdout; 기본 샌드박스 read-only; 비대화 실행은 승인 프롬프트에 답 못 함 → 승인이 필요한 동작은 **실패**; `--output-schema` 로 최종 답을 JSON 스키마에 맞춤, `-o` 로 파일에; `--json` 이벤트에 `turn.completed.usage`(입력·캐시·출력 토큰). 권장: "여러 단계 과제를 거대한 프롬프트 하나로 하지 말고 `exec resume --last` 로 단계마다 확인 가능하게." 병렬 작업은 worktree 분리.
- https://help.openai.com/en/articles/20001516-managing-usage-with-gpt-6-astra-in-work-and-codex (OpenAI 공식) — **확인**(A3 의 "요약만"을 확인으로 올림). Plus 5시간 창 추정 로컬 메시지: **Astra 5~45 / GPT-6.1 Sol 15~160 / GPT-6 Sol 15~150 / Luna 350~3,000**. 5시간·주간 둘 다 남아야 함. 공식 문구: "**Astra Low 가 Sol High 보다 나을 수 있다** — Sol High 로 잘 됐으면 Astra Low/Medium 부터." "effort 를 올려도 빠진 정보·권한은 못 채운다." Pro 100/200/500 은 5시간 한도 없음.
- https://kenimoto.dev/blog/codex-cli-vs-claude-code-7-real-tasks-31-days/ (2026-08, 같은 저장소 7티켓 31일) — **확인**. Claude Code 승: 리팩터·버그(Codex 는 재시도만 넣고 근본 원인을 놓침)·PR 리뷰(Codex 오탐 4건)·테스트 생성(Codex 41개 중 6개 동어반복). Codex 승: **기계적 이전(14곳, 11분, diff 깨끗)**, `--help` 문서 동기화(한 번에). "망설임이 값어치 있는 일 = Claude, 망설임이 지연만 더하는 일 = Codex." 결정이 없는 티켓은 Codex 가 30~50% 빠름.

**F3 이 우리에게 뜻하는 것**
- **E 항목 정정: Codex 에 훅이 있다.** C1 의 Stop 게이트를 Codex `art` 에도 같은 스크립트로 걸 수 있다(`.codex/hooks.json` 의 Stop → `python scripts/team_stop_gate.py`, exit 2). 단 첫 실행 전에 `/hooks` 로 한 번 신뢰 승인하거나 team.sh 의 codex 명령에 `--dangerously-bypass-hook-trust`.
- C7(폴더별 AGENTS.md) 함정: Codex 는 **현재 폴더까지만** 내려가 읽는다. team.sh 가 `-C <repo 루트>` 로 띄우면 `tools/art/AGENTS.md` 는 **안 읽힌다**. → art 과제는 `-C tools/art`(+ `--add-dir` 로 F1Studio) 로 띄우거나, 루트 AGENTS.md 에 "아트 교훈 = tools/art/AGENTS.md 먼저 읽기" 한 줄. 지금 루트 AGENTS.md 가 32KiB 를 넘는지도 확인(넘으면 잘림).
- Astra 한도는 Plus 기준 5시간 5~45 메시지로 가장 비싸다. 기계 단계(재임포트·check.sh·export)는 **Astra low/medium** 으로 먼저 돌리는 것이 공식 권장과 맞다(지금 high 고정). effort 는 과제 원문 태그로 고르게.
- Codex = "결정 없는 기계 일"이라는 실무 보고(kenimoto)와 우리 실측(Sonnet 25턴 헛돔 vs Codex $0 성공)이 같은 방향. Codex 에 **원인 찾기·판정**을 맡기면 증상만 덮는다는 보고도 있다 → art 과제에 "원인 진단"이 섞이면 Claude 쪽.
- `codex exec --output-schema` 로 art 과제 끝 보고를 JSON(산출 경로·check.sh 결과·남은 위반 수)으로 강제할 수 있다 → team.sh 사후 검사(C2)가 글을 파싱하지 않아도 됨.

### F4. 시각·게임 결과물 검증 고리

- https://neurips.cc/virtual/2026/loc/paris/poster/139200 EditJudge-Bench (NeurIPS 2026) — **확인**(페이지를 curl 로 받아 초록 확인). **Blender 로 렌더한 전/후 1,500쌍**, 정답은 3D 장면 값에서 나옴. 판정자를 "지시대로 고쳐졌나" **YES/NO 검증자**로 시험 — 정확히 우리 `fb_judge.py` 형식.
  - 널리 쓰는 VIEScore 틀 + GPT-4o AUROC **67.1%**, 미세조정 판정자 50.9~65.0%. **회전·형태 같은 공간 편집은 거의 찍기 수준.** 이미지·글 순서만 바꿔도 최대 13%p 흔들림.
- https://arxiv.org/html/2604.25235v1 "VLM Judges Can Rank but Cannot Score" — **확인**(A7 의 "요약만"을 확인으로 올림). 판정자 점수의 예측 구간이 미학·자연 이미지에서 점수 폭의 ~40%, 차트·수학에서 ~70%. "**순서는 맞히지만 절대 점수는 못 준다**(ranking-scoring decoupling)." 기존 벤치마크에서 5점 척도 정확 일치 32~34%. 구간 폭은 모델 크기보다 과제 모호함·정답 품질이 좌우(깨끗한 다중 주석 데이터에서 4.5배 좁음).
- https://arxiv.org/html/2604.17768v1 "When VLMs Judge Without Seeing" (Oracle AI) — **확인**(A7 의 "요약만"을 확인으로 올림). 판정자가 이미지를 거의 안 보고 **더 많이 말한 답**을 고른다. 더 자세한 쪽이 틀린 경우 정확도 **40.6~59.0% 급락**. 처방 BIRCH = 먼저 이미지로 "진실 기준"을 만들고 그와 비교 → 편향 최대 17% 감소.
- https://vbgnt.com/blog/ai-games-automated-playtesting/ (업체 글) — **확인**. QA 에이전트가 모든 빌드를 사람보다 먼저: 부팅 / 프레임 하한 / 오류 셋만. "검은 화면도 '뭔가 떴다'로 세면 안 된다." **실패는 사람이 아니라 개발 에이전트로 되돌림**(왕복 횟수 상한 있음). "자동 플레이테스트는 재미를 못 잰다 — '사람 의견을 모을 가치가 있는 상태인가'만 답한다."
- (F1 의 Malone) 엔진 비교를 **새 에이전트·눈가림·라벨 없음·순서 섞음**으로 판정, 12/12 일치에도 결정은 본인 플레이로.

**F4 가 우리에게 뜻하는 것**
- `fb_judge.py` 의 질문 "아직 보이나 YES/NO"는 EditJudge-Bench 의 바로 그 설정이다. 이 설정의 최신 측정이 **AUROC 67%, 공간 편집은 찍기**다. 우리 코멘트의 상당수는 공간 문제다(길 묻힘·건물 떠 있음·옮긴 위치·겹침). → **공간 코멘트는 VLM 판정을 주 게이트로 쓰지 않는다.** 대신 기계 검사(레이캐스트 높이차·바운딩 박스 겹침·접지 거리·transform diff)를 1차 게이트, VLM 은 "색·질감·분위기" 코멘트에만. 이게 "숫자로 고치고 화면은 안 본다" 문제의 반대편 함정이다 — **화면 판정을 VLM 에 넘기면 공간 결함은 못 잡는다.** 둘 다 필요하고 종류별로 나눈다.
- 판정자 프롬프트의 **이미지·글 순서를 고정**한다(최대 13%p). 팀원이 쓴 "고친 것" 설명은 판정자에게 주지 않는다(C9 ① — 정보 편향 40~59%p 하락 근거가 이제 확인됨).
- 판정자 결과를 도현 [넘기기]/[다시] 와 대조(C9 ②)는 필수다. 67% 수준이면 판정자 PASS 도 셋 중 하나는 틀린다 → 도현에게 "판정자 PASS" 를 "됐다"로 말하지 않는다.
- vbgnt 처럼 **실패는 팀원에게 되돌리고 PD·도현은 통과본만 본다.** 지금은 PD 가 팀원 실패를 읽고 다시 과제를 쓴다. Stop 훅(C1)이 그 왕복을 팀원 세션 안에서 끝내게 한다(상한 = Claude 8회 기본 캡).

### F5. 하네스·오케스트레이션 — 숫자 있는 것

(A1·A2 의 Anthropic 글 두 개 외)
- https://code.claude.com/docs/en/hooks.md (공식 훅 참조) — **확인**. Stop = "메인 에이전트가 응답을 끝냈을 때. **사용자 중단이면 안 돈다. API 오류면 StopFailure**(결정 권한 없음, 기록용)." 연속 이어가기 **8회 캡**, 도구를 한 번 부르면 카운트 초기화(`CLAUDE_CODE_STOP_HOOK_BLOCK_CAP`). Stop 입력에 `stop_hook_active`·`last_assistant_message`·`background_tasks`. `onFailure: "block"` 은 v2.1.295+. Windows 에서 Git Bash 유무에 따라 셸 훅 실패 동작이 달라짐 → 정책 훅엔 `onFailure: "block"` 명시. 프롬프트형 Stop 훅(`type: "prompt"`)도 있음 = 내장 `/goal`.
- https://code.claude.com/docs/en/cli-reference.md (공식) — **확인**. `--max-turns` = "한도에 닿으면 **오류로 종료**." `--max-budget-usd` = 클라이언트 추정 비용 기준, "**한도를 넘을 수 있다**", 그리고 "**`--resume` 으로 이어 가면 이전 실행 비용은 한도에 안 센다**" → 우리 "예산 플래그가 안 지켜진다"의 공식 설명.
- https://code.claude.com/docs/en/settings.md (공식) — **확인**. 우선순위: 관리 > **명령줄 `--settings`** > 프로젝트 로컬 > 프로젝트 > 사용자. `--settings` 는 user/project/local "파일 출처"와 **별개 층**이다 → `--setting-sources user` 와 함께 줘도 적용될 것(E 의 추정을 문서로 뒷받침, 실험은 여전히 필요).
- https://code.claude.com/docs/en/headless.md (공식) — **확인**. `-p` 는 기본으로 프로젝트 `.claude/settings.json` 훅을 **신뢰 대화 없이** 돌린다(`--bare` 면 안 돎; `--bare` 가 앞으로 `-p` 기본값이 될 예정). **메인 대화가 띄운 백그라운드 명령이 없으면, 10분 연속 대기 후 남은 작업을 멈추고 부분 결과를 버린다**(`CLAUDE_CODE_PRINT_BG_WAIT_CEILING_MS`). `--output-format json` 의 `total_cost_usd` 는 `--resume` 이면 **대화 전체 누적**.
- https://github.com/snarktank/ralph (Ralph 루프, 별 22k) — **확인**. 반복마다 **새 인스턴스**, 기억은 git + `progress.txt`(덧붙이기 전용 교훈) + `prd.json`(이야기마다 `passes: false→true`). 한 반복 = **가장 우선인 미통과 이야기 하나** → 검사(타입·테스트) 통과 시에만 커밋 → 교훈을 관련 `AGENTS.md` 에 추가. 크기 기준: "한 컨텍스트 창에 끝나야 한다. '대시보드 전체'·'인증 추가'는 쪼갠다." UI 이야기는 완료 기준에 "브라우저로 확인" 필수. 기본 10회 상한.
- https://stevekinney.com/writing/the-ralph-loop — **확인**. "**파일과 git 기록이 컨텍스트 창보다 나은 기억층이다.**" 반복마다 명세를 통째로 다시 읽는 낭비를 일부러 감수 = 압축·컨텍스트 부패를 피하는 값. 숫자: bash 루프의 Claude ≈ **API $10/시간**, Huntley 가 소개한 사례 $50,000 계약을 **API $297** 로 납품. $20 요금제는 한 세션이 주간 한도를 먹는다. "**반복 상한은 반드시** — 완료 약속 문자열은 행복한 경로일 뿐, 진짜 안전망은 상한." 다중 에이전트보다 단일 루프부터("비결정 마이크로서비스는 엉망").

**F5 가 우리에게 뜻하는 것**
- C1(Stop 훅) 설계를 고친다: **턴 한도·API 오류로 끝나면 Stop 훅은 안 돈다**(턴 한도는 "오류 종료", API 오류는 StopFailure). 그래서 C2(team.sh 가 사전 뼈대 + 사후 검사)가 **주 게이트**, Stop 훅은 "정상으로 끝내려 할 때"의 보조다. 순서를 C2 → C1 로 바꾼다.
- **`--resume` + `--max-budget-usd` 조합은 구조적으로 안 지켜진다**(공식). C3(새 세션 기본)을 하면 예산 플래그가 다시 의미를 갖는다 — 새 세션에선 이전 비용이 없다.
- 10분 백그라운드 대기 상한(Claude `-p`)과 vbgnt 의 같은 보고 → artlive 가 Blender 렌더·Unity 임포트를 백그라운드로 띄우면 결과가 버려질 수 있다. 팀원 과제엔 "렌더·임포트는 포그라운드로" 또는 team.sh 에 `CLAUDE_CODE_PRINT_BG_WAIT_CEILING_MS=1800000`.
- Ralph 의 `prd.json passes` = 우리 `fb` 코멘트 jsonl 의 `status`. 이미 같은 구조가 있다. 빠진 건 (a) **한 과제 = 미통과 코멘트 하나**(지금은 묶음), (b) 끝나면 교훈을 `lessons/*.md`·AGENTS.md 에 자동 덧붙임, (c) 반복 상한(같은 코멘트 3회 실패 → 도현에게).
- 비용 잣대: 루프 $10/시간이 실무 기준선. 우리 artlive 과제 $1~2/4~7분 ≈ $15/시간 — 비슷하다. 비용 자체보다 **헛도는 과제 비율(턴 한도 실패 40%)**이 문제라는 A~D 결론 유지.

### F 제외 목록 (검색 요약만 봄 — 근거로 안 씀)
- https://www.reddit.com/r/blender/comments/1syv8w2/ — 403(열기 실패).
- https://testdino.com/blog/ai-visual-regression-testing — 페이지 로딩 실패(본문 없음).
- https://openreview.net/pdf?id=gF6jmAsbpZ (VLM 비평가로 프론트엔드 3회 개선 +17.8%, 다차원 판정자 사람 일치 48.5%→69.5%) — 봇 검사 벽.
- https://github.com/danielvaughan/codex-blog (Claude 계획 → Codex 실행 라우팅), https://blakecrosley.com/blog/codex-vs-claude-code-2026, https://getknack.ai/blog/codex-vs-claude-code — 요약만.
- https://t.me/s/everydayUnity (Coplay Unity MCP 위에 에이전트가 실제 게임을 '수동 테스트'로 플레이, "토큰을 빨리 먹는다", "빌드 성공 여부는 결정적 스크립트가 말해야") — 요약만.
- https://www.linkedin.com/posts/arizing-pixel_… (토폴로지·UV·LOD·리그는 사람에게 남음), https://ziva.sh/blogs/game-development-roles (Capcom 배경 소품 수천 개 초안을 Gemini·Imagen 으로, 자기 채점 후 아트디렉터) — 요약만.
- https://dev.to/sivarampg/the-ralph-wiggum-approach… (50회 반복 $50~100) — 요약만.

---

## G. 결론 수정

**원문 27개를 열어 본 뒤: 0절 결론과 top3 는 유지한다.** 방향이 바뀐 것은 없다. 순서·세부 셋이 바뀌고, 빠진 큰 관행이 하나 있다.

### G1. 그대로인 것
- 상주 팀 유지 + 역할 축소. 근거 추가: Unity 는 에디터 하나·바이너리 씬·.meta 때문에 병렬 에이전트에 불리하다는 실무 보고 둘(F1). 우리 "에디터당 과제 하나 + F1Studio 분리"가 맞는 답이다. 에디터를 늘리지 않는다.
- top2 "새 세션 + PROGRESS + 주제 하나": Ralph(별 22k)·Anthropic 예제·Kinney 가 같은 구조. 공식 문서로 이유 하나 추가 — `--resume` 이면 예산 한도가 이전 비용을 안 센다(F5).
- top3 "일 종류로 모델 고정": EditHero 수치(3D 편집 지시 이행 Opus 5.5 0.62 > Astra 0.53)와 실무 비교(kenimoto: 결정 없는 기계 일 = Codex, 원인 찾기·리뷰 = Claude)가 같은 방향.

### G2. 바뀌는 것
1. **top1 의 순서: C2(team.sh 사전 뼈대·사후 검사)가 주 게이트, C1(Stop 훅)은 보조.** 턴 한도 종료는 "오류 종료", API 오류는 StopFailure 라 Stop 훅이 안 돈다(공식, F5). 우리 실패의 다수가 턴 한도라서 Stop 훅만으론 못 막는다.
2. **Codex 도 같은 게이트를 건다.** Codex 에 Stop 훅이 있다(공식, F3) — E 의 "훅 없음"은 틀렸다. `.codex/hooks.json` Stop → 같은 `team_stop_gate.py`(exit 2). 해시 신뢰 승인 필요 → team.sh codex 명령에 `--dangerously-bypass-hook-trust` 또는 첫 회 `/hooks` 승인.
3. **C7(폴더별 AGENTS.md)은 지금 방식으론 안 먹는다.** team.sh 가 Codex 를 `-C <저장소 루트>` 로 띄운다(76행). Codex 는 현재 폴더까지만 내려가 읽으니 `tools/art/AGENTS.md` 는 무시된다. → 아트 교훈은 **루트 `AGENTS.md`**(지금 1.6KB, 상한 32KiB)에 "아트 과제" 절로 넣거나 `-C tools/art --add-dir <루트>` 로 띄운다.
4. **fb_judge 의 역할을 좁힌다**(G3).
5. Codex effort: 기계 단계는 **Astra low/medium** 부터(공식 권장 "Astra Low 가 Sol High 보다 나을 수 있다", Plus 5시간 5~45 메시지로 가장 비쌈). high 는 과제 원문 `[HIGH]` 일 때만.

### G3. 새로 찾은 큰 관행 — "공간 결함은 기계로, 겉모습만 VLM 으로"
- 근거: EditJudge-Bench(NeurIPS 2026) — Blender 전/후 YES/NO 판정, GPT-4o AUROC 67%, **회전·형태 같은 공간 편집은 거의 찍기**. 판정자는 글이 많은 쪽에 끌린다(정확도 40~59%p 하락). Malone·vbgnt·BOINGWARD 는 모두 "되는지"를 **결정적 스크립트**(다리·봇·테스트)로 판정하고 VLM·사람은 겉모습·손맛에만 쓴다.
- 우리 상황: D-204 의 재지적(길 묻힘·옮긴 건물 묻힘)은 **공간 결함**이다. 지금 `fb_judge.py` 는 이걸 VLM 두 명에게 "아직 보이나"로 묻는다 — 연구상 가장 약한 칸이다. "숫자로만 고치고 화면을 안 본다"를 "화면을 VLM 에게만 보인다"로 바꾸면 같은 실패가 다른 모양으로 남는다.
- 처방(파일 단위):

| # | 파일 | 변경 | 품 |
|---|---|---|---|
| G3-a | 새 `scripts/fb_geom.py` | 코멘트 id → 대상 오브젝트(선택 경로·`split_from`)에 대해 `ucli-http.sh exec` 로 결정적 검사: 바닥 레이캐스트 접지 거리(\|d\|≤3cm), 지형·길 대비 묻힘(꼭짓점 중 지형 아래 비율), 이웃 바운딩 박스 겹침, 도현 `transform_delta` 적용 여부(위치 차 ≤5cm). 결과 `feedback/judge/<id>.geom.json` PASS/FAIL + 수치 | 3h |
| G3-b | `scripts/fb.py` (`fixed`) | 코멘트 원문에 공간 단어(묻힘·뜸·겹침·옮김·높이·기울·접지·끼임)가 있으면 `geom.json` PASS 가 **필수**, VLM 판정은 보조. 겉모습 단어(색·밝기·노이즈·질감·느낌)면 지금처럼 VLM 2명 | 1h |
| G3-c | `scripts/fb_judge.py` | 프롬프트 순서 고정(이미지 → 도현 원문 → 질문), 팀원 설명 제거(C9 ①), 결과에 `type: appearance` 기록. 도현 [넘기기]/[다시] 와 `agree.csv` 대조(C9 ②) | 1h |
| G3-d | `.claude/team/roles/artlive.md`·`dev.md` | "공간 코멘트 = `fb_geom.py` 수치 + 같은 카메라 전/후 둘 다, 수치만·화면만은 미완료" 한 줄 | 10분 |

### G4. 같이 하면 싼 것 (작은 관행)
- **Unity 씬·프리팹 YAML 손편집 차단 훅**(Malone): Claude 팀원 settings 의 PreToolUse(Edit/Write) 가 `*.unity|*.prefab|*.mat` 경로면 거부 → 에디터 스크립트·`ucli-http.sh` 로만. `.claude/team/worker-settings.json`(C1 과 같은 파일). 30분.
- **Unity 6000.3 API 함정 목록**: `F1/AGENTS.md` 대신 루트 `AGENTS.md`·`lessons/programmer.md` 에 "모델이 틀리는 API" 줄(예: `linearVelocity`, `FindObjectsByType`). 실측으로 나온 것만, 줄마다 `[검사]` 규칙 유지. 30분.
- **백그라운드 10분 상한**: team.sh Claude 분기에 `CLAUDE_CODE_PRINT_BG_WAIT_CEILING_MS=1800000` export(렌더·임포트 결과 버림 방지). 5분.
- **한 과제 = 미통과 코멘트 하나**(Ralph): 코멘트 묶음 과제를 주제별로 쪼개던 C11 을 더 좁힌다 — 같은 원인 코멘트끼리만 묶고 기본은 하나. 같은 코멘트 3회 실패면 다음 과제 대신 도현에게.
- **Codex 끝 보고를 JSON 으로**: `codex exec --output-schema art-report.schema.json -o <보고>.json`(산출 경로·check.sh 마지막 줄·남은 위반 수·glb 시각) → C2 사후 검사가 파싱 없이 읽음. 1h.

### G5. 바뀐 실행 순서
1. **C2 → G3(a·b) → C1** — "끝" 판정: 사후 검사가 먼저, 공간 결함은 기계 게이트, Stop 훅(Claude·Codex 둘 다)은 마지막.
2. **C3+C4+C11(+코멘트 하나)** — 새 세션·PROGRESS·작은 과제. 예산 플래그가 다시 의미를 가짐.
3. **C5+C6+C8 + Codex effort 낮추기 + 루트 AGENTS.md 아트 절** — 모델·역할 정리.

---

## H. 하네스 엔지니어링 — 공식·신뢰 출처 (PD 가 원문 직접 확인, 10/29 06:2x)

### H1. 토스 — "Harness를 통한 조직 생산성 저점 높이기" (김용성, 토스페이먼츠, 2026-02-26) — **확인**
https://toss.tech/article/harness-for-team-productivity
- 같은 모델을 써도 결과 편차가 큰 건 실력이 아니라 "LLM 을 얼마나 정교하게 제어하나"의 차이. 이걸 개인 센스에 맡기지 말고 **팀이 설계·배포하는 시스템**으로 만든다 → 저점(Floor)을 올린다.
- **Executable SSOT**: 사람이 읽으면 매뉴얼, LLM 이 읽으면 시스템 프롬프트. 플러그인이 바뀌면 모든 에이전트 행동이 즉시 바뀐다.
- 훅 = 막기만 하는 린터가 아니라 **팀 규율대로 행동을 바로잡는 안내**(예: main 커밋 시도 → feature 브랜치로).
- 슬래시 명령 하나에 최고 숙련자의 워크플로우를 담는다: `/new-feature` = 맥락 수집 → 이슈·브랜치·계획 → **사람이 계획 승인** → 구현 → PR.
- 범용 플러그인(oh-my-claudecode)은 출발점일 뿐, 도메인별 "AI가 잘하는 일 / 사람이 꼭 볼 일(HITL)"은 직접 정해야 한다.
- 스스로 "대부분 가설·방향성"이라고 밝힘 — 수치 근거는 없음.

### H2. OpenAI — "Harness engineering: leveraging Codex in an agent-first world" (2026-02-11) — **확인**
https://openai.com/index/harness-engineering/
- 5개월, 수기 코드 0줄 제품. 엔지니어당 하루 PR 3.5개. **초반이 느렸던 이유는 모델이 아니라 환경이 덜 정의돼서.**
- 사람은 방향을 정하고(steer) 수용 기준을 만들고 결과를 검증. **에이전트가 막히면 신호로 본다: 빠진 도구·가드레일·문서를 찾아 저장소에 넣는다(그 수정도 Codex 가 씀).**
- AGENTS.md 는 백과사전이 아니라 **목차**. 지식은 저장소 안 문서가 기록 원본.
- 취향·구조는 잔소리가 아니라 **불변식을 기계로 강제**: 커스텀 린터·구조 테스트. **린트 에러 메시지에 고치는 방법을 써서** 에이전트 맥락에 바로 들어가게.
- 엔트로피: 에이전트는 저장소의 나쁜 패턴도 복제한다. 매주 금요일(20%)을 "AI slop" 청소에 쓰다가 → **주기적 백그라운드 과제가 이탈을 찾아 작은 수정 PR**.

### H3. Martin Fowler(Thoughtworks) — "Harness engineering for coding agent users" (2026-04-02) — **확인**
https://martinfowler.com/articles/harness-engineering.html
- 하네스 = 가이드(행동 전, feedforward) + 센서(행동 후, feedback). 각각 **계산형**(스크립트·린터·테스트 — 결정적)과 **추론형**(리뷰 에이전트·규칙 문서 — 비결정)으로 나뉜다.
- 품질 검사는 최대한 앞쪽(왼쪽)에. 센서 신호는 LLM 이 읽기 좋게.
- 사람의 역할: 사람 입력을 없애는 게 아니라 **사람 입력이 가장 중요한 곳으로 몰아준다.**
- 하네스는 한 번 설정이 아니라 계속하는 엔지니어링.

### H4. 우리에게 뜻하는 것 — 이렇게 쓴다
지금 우리 하네스를 이 틀로 보면:
- 가이드: roles/*.md, lessons/art.md, PD 가 매번 손으로 쓰는 과제 글. ← **가장 큰 편차 원인은 PD 의 과제 글**(매번 다르게 씀, 빠뜨림). 토스 말로 "저점"이 PD 의 그날 컨디션이다.
- 센서(계산형): check.sh(아트 규칙), 봇 주행, 컴파일. **공간 결함(묻힘·뜸·겹침) 센서가 없다** → "고쳤다" 후 같은 지적 재발의 직접 원인.
- 센서(추론형): fb_judge(판정자 둘). 겉모습엔 맞지만 공간 판정엔 약함(F 절 EditJudge).
- 사람: 도현 = 방향·취향·최종 [넘기기]. 여기에만 도현 시간을 쓴다.

적용 순서(바꾼 최종안):
1. **워크플로우를 명령으로 포장(토스)** — PD 가 과제 글을 쓰지 않는다. `bash scripts/team.sh fix <코멘트id>` 하나가 고정 템플릿으로 과제를 만든다: 코멘트·스샷·thread 읽기 → 원인 한 줄을 보고서에 먼저 → 고침 → 계산형 센서 → fb.py fixed(재촬영+판정) → 같은 원인 남은 곳. 새 세션·25턴·코멘트 하나. 3회 실패면 도현에게.
2. **계산형 센서 먼저(OpenAI·Fowler)** — `scripts/fb_geom.py`(건물 접지·묻힘·길 가장자리 단차·겹침). 공간 단어 코멘트는 geom PASS 필수. check_rules·geom 의 실패 메시지에 "고치는 법" 한 줄을 넣는다.
3. **끝은 하네스가 판정** — team.sh 가 보고서 뼈대를 먼저 만들고, 끝난 뒤 센서 결과 파일이 없으면 failed. (Stop 훅은 보조)
4. **가이드는 목차로** — AGENTS.md·CLAUDE.md·PD 스킬을 목차로 줄이고 규칙은 센서로 옮긴다. 막힐 때마다 "빠진 도구·규칙"을 저장소에 추가(교훈 = 센서 하나 또는 템플릿 한 줄).
5. **청소 과제(OpenAI GC)** — 주 1회 Codex 가 라이브러리·맵 이탈(규칙 위반, 남은 임시 파일, 묻힌 물체)을 훑어 작은 수정.
- 처음부터 다 만들지 않는다(과한 구조화가 더 큰 실패 원인이라는 실무 회고 다수). 1→2→3 만 먼저, 쓰면서 늘린다.
