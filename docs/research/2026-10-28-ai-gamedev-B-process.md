# AI 팀 운영 개선 — 1인 개발 다중 에이전트 사례 조사 (2026-10-28, B: 과정)

조사: 하위 에이전트(웹 검색 + 페이지 직접 확인). 커밋 안 함. 다른 파일 수정 안 함.
표기: [확인] = 페이지에서 직접 읽음 / [추정] = 내 판단 / [내부] = 우리 리포 파일에서 직접 봄.
중복 피함: 아트 제작 방식 비교는 `2026-10-27-art-method-beyond-code.md`, 쌍대비교 판정자 검증은 `docs/qa/judge/JUDGE.md`.

## 0. 결론 (3줄)
- 문제 대부분은 모델 능력이 아니라 **하네스(team.sh) 구조**다. Anthropic 도 같은 실패(한 번에 다 하려다 중간에 끊김, 테스트 없이 "완료")를 보고했고, 처방은 "한 과제 = 한 기능 + 진행 파일 + 새 세션"이었다 [확인: anthropic.com/research/effective-harnesses-for-long-running-agents].
- 교훈이 반복되는 이유는 교훈이 **팀원이 못 보는 곳(PD 스킬)**에 있기 때문이다. 역할 파일은 17~23줄이고 교훈 섹션이 없다 [내부: `.claude/team/roles/*.md`, grep "교훈|lesson" 0건].
- "반드시 지킬 것"은 글이 아니라 **훅(Stop hook)·스크립트**로 강제해야 한다 — 공식 문서가 그렇게 나눈다 [확인: code.claude.com/docs/en/best-practices].

---

## 1. Unity·Blender + 에이전트 워크플로

### 1-1. Unity 연결 도구 현황
- Unity 가 공식 Unity CLI 와 Claude Code 공식 플러그인을 냈다. 플러그인은 스킬 29개를 설치한다 [확인: unity.com/blog/unity-plugin-for-claude-code].
- Unity 측 조언: 컨텍스트 문서는 "한 가지 진실의 출처"만. 겹치는 문서는 토큰만 쓰고 모델을 헷갈리게 한다 [확인: discussions.unity.com/t/…/1740182].
- 같은 글: 상세 규칙은 AGENTS.md 에 넣지 말고 "○○ 할 때 이 파일 읽어라" 한 줄로 지연 로딩. 주기적으로 문서를 다듬는 "미니 리뷰"를 돌린다 [확인: 같은 URL].
- 저토큰 Unity MCP 예: 도구 묶음을 골라 켜면 52개 → 19개, 세션당 ~8.8k → ~3.1k 토큰 [확인: github.com/canakbass/unity-mcp].
- 같은 저장소 규칙: 스크린샷은 ~2k 토큰. 수치·로직 확인은 에디터 스크립트가 `Logs/report.txt` 를 쓰게 하고 그걸 읽는다. 단일 물체는 `isolate` 로 따로 렌더 [확인: 같은 URL].
- 같은 규칙: Play 중 컴파일 금지(정적 값 초기화) — 우리 스킬 Pitfall 과 같다 [확인: 같은 URL].
- CoplayDev MCP for Unity: 50개 도구·10개 묶음, 세션별로 필요한 묶음만 켜서 "프롬프트 작게, 비용 낮게". 여러 에디터 동시 라우팅 지원 [확인: coplaydev.github.io/unity-mcp].
- 외부 평: Claude Code 는 C# 은 잘 쓰지만 "에디터 안에 사는 것(직렬화·씬·프리팹)"에서 틀린다 [확인: strayspark.studio/blog/claude-code-unity-what-works-2026 검색 요약].
- 우리 적용 [추정]: 우리는 이미 ucli-http + 결과를 숫자로 읽는 방식이다. 추가로 할 것 = **측정 스크립트가 JSON 리포트를 쓰고, 에이전트는 그것만 읽게**(스샷은 최종 1~2장).

### 1-2. 자동 플레이 봇
- 봇 보고에 넣을 항목: 목표 진행, "입력은 있는데 움직임·진행이 없는 구간(softlock window)", 오류, 사용한 시드 [확인: github.com/majidmanzarpour/threejs-game-skills …/playtest-bot.md].
- 같은 문서: 모든 랜덤을 시드 RNG 로. 아니면 봇 수치는 잡음이다 [확인: 같은 URL].
- 같은 문서: 일부러 위험을 찾는 "난폭" 봇으로 실패 상태·재시도도 검증 [확인: 같은 URL].
- 우리 상황 [내부: pitcrew 스킬]: 봇 실패 수가 같은 설정에서 ±6 흔들린다. 경사 20~41% 구간에서 실패.
- 적용 [추정]: ① 봇 시작 위치·물리 시드 고정 + 3회 중앙값 ② 실패를 "경사 초과 / 길 위 장애물 / 접지 끊김 / 끼임" 4종으로 자동 분류해 JSON 으로 ③ 경사 실패는 맵(평면) 검사로 먼저 거르고 Unity 봇은 회귀용으로만.
- 경사 기준선: Unity NavMesh 도 "걸을 수 있는 최대 경사각"을 명시 값으로 둔다 [확인: docs.unity.com …/navmeshbuildsettings/agentslope]. 우리 차 등판 한계도 숫자 하나로 정하고 평면 검사기에 넣는다 [추정].

### 1-3. 씬 검증·스크린샷 회귀
- Unity Graphics Test Framework: 씬을 렌더해 "알려진 정답" 참조 이미지와 비교. 하드웨어 차이용 임계값 인자, 씬에서 테스트 자동 생성 [확인: docs.unity3d.com/Packages/com.unity.testframework.graphics@8.13].
- 적용 [추정]: 이름 붙은 카메라(`Cam_PO_*`, 이미 쓰는 방식)마다 도현 통과 컷을 참조로 저장 → 다음 판은 픽셀 차 + `audit_compare.py` 수치를 자동 첨부. "의도 안 한 변화"(공유 .mat 덮어쓰기 등)를 잡는다.

### 1-4. Blender + 에이전트
- blender-production-mcp 의 흐름: 원본 검사 → 체크포인트 → **별도 후보** 생성 → 측정 → 같은 조건 미리보기 → 검토 후 채택. 원본은 기준으로 남김 [확인: github.com/DiegoArias32/blender-production-mcp].
- 우리 결론(코드 아트 천장 5~6점, blender-mcp 도 같은 천장)은 이미 `2026-10-27-art-method-beyond-code.md` 에 있다 — 여기선 반복하지 않는다.

---

## 2. 에이전트 과제 설계 (끊김·보고 누락·교훈 반복)

### 2-1. 한 과제 = 한 기능, 진행 파일, 새 세션
- 실패 패턴 ①: 한 번에 다 하려다 컨텍스트가 바닥나 반쯤 구현된 채 끝남. ②: 나중 세션이 진행을 보고 "다 됐다"고 선언 [확인: anthropic.com/research/effective-harnesses-for-long-running-agents].
- 처방: 첫 세션이 기능 목록 + `claude-progress.txt` + init 스크립트를 만든다. 이후 세션은 **한 번에 한 기능**, 끝에 진행 파일·커밋으로 구조화된 기록 [확인: 같은 URL].
- 각 세션 시작 절차: 진행 파일·git log 읽기 → 기본 E2E 테스트로 "망가진 상태인지" 먼저 확인 → 최우선 미완 기능 하나 [확인: 같은 URL].
- 테스트 없이 완료 표시가 주요 실패. 사람처럼 직접 돌려 보라고 명시하면 크게 좋아짐 [확인: 같은 URL].
- 후속 글: 컨텍스트 리셋(새 세션)은 compaction 과 다르다. 요약은 "맑은 상태"를 주지 못해 조기 마무리("context anxiety")가 남는다 [확인: anthropic.com/research/harness-design-long-running-apps].
- 우리 상황 [내부]: team.sh 는 기본이 `--resume`(세션 이어 쓰기). research 세션 누적 $20.8, 단일 과제 61턴 "❌ 오류" [내부: `.ai_logs/team/research.live.log`].
- 적용 [추정]: **기본 = 새 세션**, `--resume` 은 "같은 과제 마무리"일 때만. 대신 역할별 `PROGRESS.md`(최근 5과제: 한 것·산출물·남은 것)를 과제 앞에 붙인다.

### 2-2. 보고 먼저 (report-first) — 훅으로 강제
- 공식 문서: CLAUDE.md 줄은 "지침"이라 모델이 건너뛸 수 있다. 예외 없는 규칙은 훅/거부 규칙으로 [확인: code.claude.com/docs/en/best-practices].
- Stop 훅: Claude 가 턴을 끝내려 할 때 스크립트가 돌고, exit 2 면 일하러 돌려보낸다. stderr 가 이유로 전달된다 [확인: 같은 URL].
- `--max-turns` / `--max-budget-usd` 는 `-p` 에서 harness 가 강제하는 지출 한도다 [확인: 같은 URL 표].
- 서브에이전트 `maxTurns` 에 걸리면 출력이 "partial" 로 표시되고 이어서 재개할 수 있다 [확인: code.claude.com/docs/en/sub-agents].
- 우리 상황 [내부]: team.sh 는 `--setting-sources user` 로 프로젝트 훅을 끈다(알림 폭주 방지). 그래서 보고서 강제 장치가 없다.
- 적용 [추정]: ① 팀원 전용 설정 파일에 **Stop 훅 1개만**: `docs/team/reports/<role>-<STAMP>.md` 가 없거나 "검증 증거" 절이 비면 exit 2. CLI `--settings` 로 이 파일만 넘긴다(`--settings` 플래그 존재 [확인: code.claude.com/docs/en/cli-reference]). ② 과제 문장 첫 줄에 "턴 0.6×한도에서 보고서 뼈대 먼저 쓰기". ③ team.sh 가 끝난 뒤 보고서 파일이 없으면 같은 세션에 15턴 "보고서만" 자동 재실행.

### 2-3. 교훈이 실제로 적용되게
- 공식 문서: CLAUDE.md 는 짧게. 길어지면 줄이 묻힌다. "IMPORTANT" 를 붙여도 해결 안 됨 [확인: code.claude.com/docs/en/best-practices].
- 특정 작업 절차는 스킬로 — 필요할 때만 로드돼 토큰을 아낀다 [확인: code.claude.com/docs/en/costs "Move instructions from CLAUDE.md to skills"].
- 서브에이전트는 `memory`(user/project/local) 필드로 세션 간 학습 디렉터리를 가질 수 있다. `skills` 필드로 시작 시 스킬 전문을 주입한다 [확인: code.claude.com/docs/en/sub-agents].
- Voyager: 성공한 행동을 **실행 가능한 코드**로 라이브러리에 저장하고 다음 과제에서 검색해 쓴다 [확인: huggingface.co/papers/2305.16291].
- 우리 상황 [내부]: 교훈은 PD 스킬(pitcrew-studio, 수백 줄)에 쌓이고 팀원 역할 파일(17~23줄)엔 교훈 섹션이 없다. 팀원은 그 교훈을 읽지 못한다.
- 적용 [추정]:
  1. 역할별 `.claude/team/lessons/<role>.md` — **최대 15줄**, 한 줄 = "하지 마라/하라 + 왜 + 검사 명령". team.sh 가 역할 파일과 함께 `--append-system-prompt-file` 로 붙인다.
  2. 넘치면 오래된 줄을 스크립트·검사로 승격(예: "공유 .mat 변경 0" → regression-check 항목). 글 → 코드로 옮긴 교훈은 목록에서 지운다(Voyager 식).
  3. PD 가 과제를 낼 때 "이번 과제에 걸리는 교훈 번호"를 적게 하고, 보고서에 "교훈 #n 지킴/어김" 칸.

### 2-4. 생성자 ≠ 판정자, 완료 기준 먼저 합의
- 일하는 에이전트와 판정 에이전트를 분리하는 것이 강한 레버 [확인: anthropic.com/research/harness-design-long-running-apps].
- 기본 상태의 Claude 는 약한 QA: 문제를 찾고도 "큰 문제 아님"으로 스스로 통과시킨다. 판정자 프롬프트를 여러 번 보정해야 했다 [확인: 같은 URL].
- 스프린트 전 "완료 계약"(무엇이 끝인지, 테스트 기준)을 생성자·판정자가 파일로 합의 [확인: 같은 URL].
- 비용: 단일 에이전트 $9 vs 전체 하네스 $200 — 품질은 하네스가 훨씬 나음 [확인: 같은 URL 표]. 그 뒤 하네스를 단순화해 부품을 줄였다 [확인: 같은 URL].
- 적용 [추정]: 과제 템플릿에 **완료 계약 3줄**(통과 숫자·캐처 경로·실패 시 행동)을 PD 가 먼저 쓴다. qa 는 그 3줄만 검사.

---

## 3. 시각 작업 평가 루프

- 다중 이미지 VLM 은 위치 편향이 있다 — 상용 모델은 처음·끝 이미지는 잘 보고 중간을 놓친다 [확인: arxiv.org/html/2503.13792v1].
- LLM 판정자는 두 응답 중 첫 번째를 선호하는 경향 [확인: 같은 URL 관련 연구 인용].
- 우리 상황 [내부: pitcrew 스킬]: 쌍대비교 + 양 순서는 이미 적용(`judge_pair.py`). 점수형은 도현 앵커와 안 맞음(0/7).
- 판정자 보정: few-shot 점수 예시로 판정자를 사용자 취향에 맞추고 반복 간 점수 표류를 줄였다 [확인: anthropic.com/research/harness-design-long-running-apps]. 단 우리 측정에선 앵커를 예시로 주면 66%로 나빠졌다 [내부] → 앵커는 판정자 **자격 시험**으로만 유지.
- 반복이 늘 좋아지진 않는다: 중간 판이 마지막보다 나은 경우가 자주 있었다 [확인: 같은 URL].
- 적용 [추정]: ① 3장 이상 비교는 금지, 항상 2장 쌍 ② 판 N 은 "직전 판"뿐 아니라 "지금까지 최고 판"과도 붙인다(최고 판 보존) ③ 쌍대비교 통과 + 장면 점검표(구성) + 참조 픽셀 차 셋을 묶어 "씬 맞음" 게이트 — 단품 게이트 대신.

---

## 4. 비용 통제

- 캐시가 가장 큰 레버. 캐시 읽기는 Opus/Sonnet 5.5 에서 입력가의 1/20 [확인: platform.claude.com/docs/en/about-claude/models/optimizing-for-cost-and-intelligence].
- 에이전트 루프는 입력의 중앙값 84% 를 캐시에서 읽는다. 80% 미만이면 캐시를 깨는 원인을 찾으라 [확인: 같은 URL].
- 기본 캐시 수명 5분 [확인: 같은 URL]. 모델 전환·시스템 프롬프트·도구 정의 변경은 캐시 전체를 무효화 [확인: code.claude.com/docs/en/prompt-caching].
- `/compact` 는 그 자체가 큰 요청. 연속성이 필요 없으면 새 세션(`/clear`)은 공짜 [확인: code.claude.com/docs/en/costs].
- 장황한 작업(테스트·로그·문서 조회)은 서브에이전트로 — 요약만 돌아온다 [확인: 같은 URL].
- 확장 사고 토큰은 출력 단가로 청구, 단순 과제엔 effort 를 낮춘다 [확인: 같은 URL].
- 구체적 프롬프트가 넓은 탐색(파일 읽기)을 줄인다 [확인: 같은 URL].
- Sonnet 이 대부분의 코딩 작업에 맞고, Opus 는 더 깊은 추론용 [확인: support.claude.com/en/articles/14552983].
- `-p --resume` 의 `total_cost_usd` 는 세션 기준으로 보고된다 — 우리가 이미 "이번/누적"을 분리한 이유와 같다 [확인: code.claude.com/docs/en/headless].
- 우리 상황 [내부]: config.json 에서 programmer·qa·art2·mapgen 이 모두 opus, 턴 60~80. research 는 sonnet + effort xhigh.
- 적용 [추정]:
  1. **qa = sonnet**(판정은 스크립트 결과 읽기 위주), research effort xhigh → high. programmer 만 opus 유지, 단순 [EDITOR] 재측정은 `TEAM_MODEL=sonnet`.
  2. 역할 파일 + 교훈 파일을 **바뀌지 않는 앞부분**으로 고정(과제마다 수정 금지) → 캐시 유지.
  3. 측정·로그 큰 출력은 파일로 쓰고 요약 줄만 읽게(1-1 의 report.txt 방식).
  4. 과제당 턴 상한을 역할별 실측 중앙값 ×1.5 로(research 실측 30~85턴).

---

## 5. 사람 디렉터를 효율적으로 루프에 넣기

- 사람 승인은 "되돌리기 어려운·밖으로 나가는 행동"에만 건다(전송·결제·계정 변경) [확인: github.com/n8n-io/skills …/n8n-ai-nodes-official/SKILL.md].
- 승인 지점에서 실행을 멈추고 상태를 보관했다가 사람 답으로 재개하는 방식이 표준 [확인: openai.github.io/openai-agents-js/guides/human-in-the-loop].
- 판정에서 사람은 측정이 못 하는 축만: Anthropic 판정자 실험도 마지막엔 저자 취향으로 보정했다 [확인: anthropic.com/research/harness-design-long-running-apps].
- 우리 상황 [내부: pitcrew 스킬]: 도현 페인트오버 루프가 가장 빨랐다(D-183). 도현 판정 대기는 `team_wait.py … 도현` 으로 이미 표시.
- 적용 [추정]: ① 도현에게 가는 것은 **"2장 중 하나 고르기" 또는 "번호 그리기"** 형식만(점수·긴 글 금지) ② 하루 1회 묶음(예: 저녁 한 장 시트: 대기 판정 N개, 각 A/B) ③ 도현 답은 원문 그대로 앵커·결함 문서에 기록 → 판정자 자격 시험 세트가 자동으로 자란다.

---

## 6. 우선순위 (효과 ÷ 수고)

| # | 개선 | 효과 | 수고 | 근거 |
|---|---|---|---|---|
| 1 | Stop 훅으로 보고서 강제 (`--settings` 로 훅 1개만) | 큼 — "보고 없이 끝" 제거 | 30분 | best-practices (Stop 훅, exit 2) [확인] |
| 2 | 역할별 교훈 파일 ≤15줄을 `--append-system-prompt-file` 로 주입, 넘치면 검사 스크립트로 승격 | 큼 — 반복 실수 | 1시간 | best-practices (CLAUDE.md 짧게·훅), Voyager [확인] |
| 3 | 기본 새 세션 + 역할별 PROGRESS.md (resume 은 마무리 때만) | 큼 — 비용·조기 마무리 | 1시간 | effective-harnesses, harness-design (리셋 vs compaction) [확인] |
| 4 | 모델 라우팅: qa·재측정 sonnet, research effort high, 고정 앞부분으로 캐시 보호 | 중~큼 — 비용 | 10분 | optimizing-for-cost, prompt-caching, support 14552983 [확인] |
| 5 | 과제 = 한 기능 + PD 가 먼저 쓴 완료 계약 3줄, qa 는 그것만 검사 | 중 — 끊김·과잉 범위 | 과제마다 2분 | effective-harnesses, harness-design (sprint contract) [확인] |
| 6 | 씬 단위 게이트: 이름 카메라 참조컷 픽셀 차 + 쌍대비교(최고 판과도) + 구성 점검표 | 중 — "단품 통과, 씬 부적합" | 반나절 | Graphics Test Framework, arxiv 2503.13792 [확인] |
| 7 | 봇: 시드 고정·3회 중앙값·실패 4종 자동 분류 JSON, 경사는 평면 검사로 선차단 | 중 — 경사 실패 진단 | 반나절 | playtest-bot.md (softlock·seed), Unity agentSlope [확인] |

남은 이슈: Claude Code 훅 페이지·CLI 참조는 본문 일부만 읽혔다(길이 제한) — `--settings` 존재는 확인, Stop 훅 JSON 형식은 best-practices 예시 기준. 적용 전 `claude --help` 로 재확인 필요.
