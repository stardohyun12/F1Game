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
