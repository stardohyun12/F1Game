# 에이전트 스튜디오 팀 — Phase 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** PM 직원 1명(별도 터미널 상주) + 스튜디오 보드(파이프라인·직원명단)를 세워, "PM이 병렬로 다음 일을 미리 깔면 내가 작업 끝낼 때 다음 게 대기 0으로 준비되는가"라는 가설을 싸게 검증한다.

**Architecture:** 상시 켜진 건 2개(메인=나, PM=별도 인스턴스). 소통은 직접 대화 대신 공유 파일 버스(`backlog.md`, `docs/studio/protocol.md`, `activity.jsonl`). PM은 페르소나+메모리 파일로 매 세션 부활. 게이트는 기존 것 그대로. 효과는 성공/kill 기준으로 판정.

**Tech Stack:** Markdown(페르소나/규약), Python 표준 라이브러리(대시보드 server.py, 의존성 0), 바닐라 JS(패널).

**참조 스펙:** `docs/superpowers/specs/2026-05-26-agent-studio-team-design.md`

**이미 완료(커밋 b4c5169):** `backlog.md`(루트), `server.py /api/backlog` 파서 + `nextReady`, `pages/pipeline.html`(gitignore, 디스크 존재).

---

## File Structure

- `docs/studio/protocol.md` — 소통 파일 버스 규약 + 계급별 라우팅 룰 (Create)
- `docs/studio/roles/pm.md` — PM 페르소나 (별도 인스턴스 부팅 시 로드) (Create)
- `docs/studio/memory/pm.md` — PM 장기 메모리(추려진, 작게 유지) (Create)
- `docs/studio/BOOT-PM.md` — PM 인스턴스 부팅 절차 + 시작/종료 의례 (Create)
- `docs/studio/phase1-eval.md` — Phase 1 성공/kill 기준 + 측정 로그 (Create)
- `tools/studio-dashboard/server.py` — `/api/roster` 라우트 추가 (Modify)
- `tools/studio-dashboard/pages/roster.html` — 직원 명단 패널 (Create, gitignore)

각 파일 하나의 책임만: 규약 / 페르소나(누구) / 메모리(아는 것) / 부팅(어떻게 켜나) / 평가(됐나) / 데이터(roster) / 화면(roster).

---

### Task 1: 소통 규약 문서 (protocol.md)

**Files:**
- Create: `docs/studio/protocol.md`

- [ ] **Step 1: 디렉터리 + 규약 파일 작성**

아래 내용 그대로 작성:

```markdown
# 스튜디오 소통 규약 (파일 버스)

> 에이전트끼리 직접 대화하면 요약 한 통으로 압축돼 디테일이 샌다.
> 그래서 대화 대신 **파일에 적어 넘긴다.** 인스턴스가 달라도 파일은 안 사라짐.

## 누가 어느 파일로 소통하나

| 파일 | 소유 | 쓰는 사람 | 읽는 사람 |
|---|---|---|---|
| `backlog.md` (루트) | PM | PM | 전원 |
| `docs/CONTROL.md` | PM/공용 | PM, 메인 | 도현 |
| `tools/studio-dashboard/activity.jsonl` | hook 자동 | hook | 대시보드 |
| 디스패치 브리프 (작업당 1장, `docs/studio/briefs/<slug>.md`) | PM | PM | 메인 |
| 게이트 판정 | 게이트 | 게이트 서브 | 메인 |

## 디스패치 브리프 형식 (작업당 1장)

```
# <작업 제목>
- 현상: (지금 무엇이 문제/목표인가)
- 원인 가설: (PM이 추정하는 root cause, 있으면)
- 완료 조건: (무엇이 되면 끝인가 — 측정 가능하게)
- 관련 파일/메뉴: (메인이 바로 찾게)
- 게이트: 화면? 흐름? (어느 게이트 타는지)
```

## 계급별 라우팅 (모든 변경이 풀코스 타지 않음)

| 변경 종류 | 경로 |
|---|---|
| 오타·docs·hook-only | 메인 혼자 |
| 한 시스템, 화면 안 건드림 | 메인 + 기획 게이트 |
| 화면 / 배선 / 새 시스템 | 풀코스: PM 브리프 → 메인 구현 → 게이트 둘 → QA 실플레이 → 커밋 |

## 게이트 판정 형식

`PASS` 또는 `FAIL` + 좌표(어디가 왜). 느낌("좀 어색") 금지 — 구체 위치.

## 불변 규칙

- 게이트(판정자)는 컨텍스트0 = 매번 백지 서브에이전트. 메모리 로드 금지.
- 제안자(PM·리드)는 메모리 로드 O.
- Unity 에디터 1개 → Play는 직렬. 게이트 서브는 Play 금지.
- "좋다" 사인은 도현만. 에이전트는 "안 깨짐"까지.
```

- [ ] **Step 2: 규약 커버리지 확인**

Read `docs/studio/protocol.md` 하고 스펙 §4(소통)·§5(계급)와 대조: 5개 파일 모두 명시 / 3개 계급 모두 명시 / 브리프 형식 존재. 빠진 항목 있으면 추가.

- [ ] **Step 3: Commit**

```bash
git add docs/studio/protocol.md
git commit -m "feat(studio): 소통 파일 버스 규약 + 계급별 라우팅 (Phase1 T1)"
```

---

### Task 2: PM 페르소나 (roles/pm.md)

**Files:**
- Create: `docs/studio/roles/pm.md`

- [ ] **Step 1: PM 페르소나 작성**

```markdown
# 페르소나 — 프로듀서/PM

너는 PitCrew 스튜디오의 PM이다. 별도 터미널 인스턴스로 상주한다.

## 정체성
- 너는 backlog의 소유자다. "다음 뭐"를 끌고 가는 사람.
- 너는 구현하지 않는다(메인=리드 프로그래머가 함). 너는 Unity를 만지지 않는다.
- 너는 판정하지 않는다(게이트가 함). 너는 조율한다.

## 매번 시작할 때 (부활 의례)
1. `docs/studio/memory/pm.md`(네 메모리) 읽기.
2. `.claude/activeContext.md` + `docs/CONTROL.md`(프로젝트 상태) 읽기.
3. `git log --oneline -10`(최근 한 일) + `backlog.md`(현재 큐) 읽기.
4. 위를 종합해 backlog를 갱신: 완료된 것 내림, 새 우선순위 올림.

## 핵심 일 (파이프 충전)
- 메인이 지금 작업 A를 하는 동안, 너는 B·C·D의 **디스패치 브리프를 미리 써둔다**
  (`docs/studio/briefs/<slug>.md`, 형식은 protocol.md). 목표: 메인이 A 끝낼 때
  다음 작업이 brief:ready 상태로 대기 0.
- backlog 항목에 `brief: ready`는 브리프 파일이 실제로 존재할 때만 붙인다.
- 도현 부재 시: 네가 능동적으로 다음 우선순위를 정해 brief까지 깔아둔다("어느 거?" 묻지 말고).

## 우선순위 판단 기준 (이 프로젝트)
- 북극성: 2026-06-24 Steam 데모 vertical slice.
- 도현의 함정: 스코프 부풀림 → 새 시스템보다 "데모에 필요한가" 우선.
- 화면/시각 결정은 도현 전속 → 너는 후보를 좁히되 "좋다"를 정하지 않는다.

## 끝낼 때
- backlog 갱신 + 네 메모리(`memory/pm.md`)에 남길 가치 있는 것만 추려 기록.
- 메모리는 작게 유지(원문 덤프 금지, 결정·우선순위 근거만).

## 금지
- 직접 구현/커밋(메인 영역). Unity 조작. 시각 "좋다" 판정. backlog 없이 즉흥 지시.
```

- [ ] **Step 2: 페르소나 자기모순 점검**

Read 후 확인: "구현 안 함" + "판정 안 함" + "backlog 소유" 일관 / protocol.md의 브리프 경로(`docs/studio/briefs/`)와 일치 / 시작·끝 의례가 부활 모델(파일 로드)과 일치.

- [ ] **Step 3: Commit**

```bash
git add docs/studio/roles/pm.md
git commit -m "feat(studio): PM 페르소나 — backlog 소유, 구현/판정 금지 (Phase1 T2)"
```

---

### Task 3: PM 메모리 시드 (memory/pm.md)

**Files:**
- Create: `docs/studio/memory/pm.md`

- [ ] **Step 1: 추려진 메모리 시드 작성**

```markdown
# PM 메모리 (추려진 — 작게 유지)

> 원문 로그 금지. 결정·우선순위 근거·교훈만. 커지면 가지치기.

## 현재 우선순위 근거 (2026-05-26)
- 데모까지 ~4주. 맵 아트가 그래픽 게이트 6/10에서 정체 → 도현 시각 방향 대기 중인 항목 많음.
- 자체 진행 가능한 것(코드·수치·데이터)을 위로, 도현 시각 결정 필요한 것은 후보만 준비.

## 도현 영역 (내가 못 정하는 것)
- 시각 톤/룩 결정, 자산 선택, 기획 우선순위 변경, 새 시스템 추가.
- → 이건 brief에 "도현 결정 필요" 표시하고 후보만 정리.

## 교훈
- (여기에 운영하며 쌓인 교훈을 한 줄씩 추가)
```

- [ ] **Step 2: 크기 확인**

`wc -l docs/studio/memory/pm.md` → 40줄 미만 유지(시드 단계). 넘으면 추려라.

- [ ] **Step 3: Commit**

```bash
git add docs/studio/memory/pm.md
git commit -m "feat(studio): PM 메모리 시드 (Phase1 T3)"
```

---

### Task 4: PM 부팅 절차 (BOOT-PM.md)

**Files:**
- Create: `docs/studio/BOOT-PM.md`

- [ ] **Step 1: 부팅 가이드 작성 (도현이 별도 터미널에서 따라할 것)**

```markdown
# PM 인스턴스 부팅 (별도 터미널)

도현: 터미널 하나 더 열어 PitCrew 폴더에서 Claude Code 실행 후, 아래를 첫 메시지로 붙여넣기.

---
너는 PitCrew 스튜디오의 PM이다. `docs/studio/roles/pm.md`(네 페르소나)와
`docs/studio/protocol.md`(소통 규약)를 먼저 읽고 그 정체성으로 행동하라.
그다음 페르소나의 "매번 시작할 때(부활 의례)" 4단계를 수행하고, backlog 갱신 + 다음
작업 1~2개의 디스패치 브리프를 `docs/studio/briefs/`에 깔아라. 구현·Unity·판정은 금지.
---

## 메인(나)과의 핸드셰이크
- PM이 brief를 깔면 backlog 항목에 `brief: ready`가 붙고, 파이프라인 패널 배지가 초록.
- 메인은 작업 시작 시 `backlog.md` 대기 1순위 + 해당 brief를 읽고 착수.
- 충돌 방지: backlog.md는 PM이 쓰고 메인은 읽기만(완료 표시는 메인이 커밋 후 PM이 반영).
```

- [ ] **Step 2: 절차 검증 (dry-run 점검, 실제 인스턴스 없이)**

확인: 붙여넣기 프롬프트가 페르소나+규약 둘 다 로드 지시하나 / brief 경로가 protocol.md·pm.md와 일치하나 / backlog 쓰기 주체(PM)와 읽기 주체(메인) 분리 명시됐나.

- [ ] **Step 3: Commit**

```bash
git add docs/studio/BOOT-PM.md
git commit -m "feat(studio): PM 부팅 절차 + 메인 핸드셰이크 (Phase1 T4)"
```

---

### Task 5: 직원 명단 패널 (roster) — API + 화면

**Files:**
- Modify: `tools/studio-dashboard/server.py` (read_backlog 함수 아래 + do_GET 라우트)
- Create: `tools/studio-dashboard/pages/roster.html`

- [ ] **Step 1: server.py에 roster 데이터 함수 추가**

`read_backlog` 함수 정의 바로 아래에 추가:

```python
ROLES = [
    ("PM", "프로듀서", "pm"),
    ("메인", "리드 프로그래머", None),
    ("디자이너", "게임 디자이너 (Phase2)", "game-designer"),
    ("아트", "아트 디렉터 (Phase2)", "art-director"),
    ("기획게이트", "흐름·무결성 판정", None),
    ("그래픽게이트", "시각 판정", None),
    ("QA", "풀루프 검증 (Phase3)", "qa"),
    ("하네스", "운영체계 발전 (Phase3)", "harness-engineer"),
]


def read_roster():
    """직원 명단: 각 직군의 마지막 활동(activity.jsonl) + 메모리 파일 mtime."""
    mem_dir = os.path.join(PROJECT, "docs", "studio", "memory")
    acts = read_activity(200)
    out = []
    for who, role, mem in ROLES:
        last_act = next((a for a in acts if a.get("who") == who), None)
        mtime = 0
        if mem:
            mp = os.path.join(mem_dir, mem + ".md")
            if os.path.exists(mp):
                mtime = os.path.getmtime(mp)
        out.append({
            "who": who, "role": role,
            "lastText": (last_act or {}).get("text", ""),
            "lastTs": (last_act or {}).get("ts", 0),
            "memMtime": mtime,
        })
    return out
```

- [ ] **Step 2: do_GET에 라우트 추가**

`/api/backlog` 분기 바로 아래에 추가:

```python
        elif path == "/api/roster":
            self._send(json.dumps(read_roster(), ensure_ascii=False), "application/json; charset=utf-8")
```

- [ ] **Step 3: roster.html 작성**

```html
<!DOCTYPE html><html lang="ko"><head><meta charset="utf-8"><title>직원 명단</title>
<style>
 body{margin:0;font-family:system-ui,'Malgun Gothic',sans-serif;background:#1b1f24;color:#e6e1d6}
 h2{margin:0;padding:14px 18px;border-bottom:1px solid #333;font-size:17px}
 .grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(230px,1fr));gap:12px;padding:16px}
 .card{background:#23282f;border-radius:10px;padding:12px 14px}
 .who{font-size:15px;font-weight:700}
 .role{font-size:11px;color:#8a929c;margin:2px 0 8px}
 .last{font-size:12px;color:#c7ccd2;min-height:16px}
 .ts{font-size:10px;color:#6a727c;margin-top:6px}
 .on{border-left:3px solid #2e7d4f}
 .idle{border-left:3px solid #444}
</style></head><body>
<h2>👥 직원 명단</h2><div id="g" class="grid"></div>
<script>
function ago(ts){ if(!ts)return '활동 없음'; const s=Date.now()/1000-ts;
 if(s<60)return Math.round(s)+'초 전'; if(s<3600)return Math.round(s/60)+'분 전'; return Math.round(s/3600)+'시간 전'; }
async function load(){
  let r; try{ r=await (await fetch('/api/roster?_='+Date.now())).json(); }catch(e){ return; }
  const g=document.getElementById('g'); g.innerHTML='';
  r.forEach(p=>{
    const recent=p.lastTs && (Date.now()/1000-p.lastTs<600);
    const d=document.createElement('div'); d.className='card '+(recent?'on':'idle');
    d.innerHTML='<div class="who">'+p.who+'</div><div class="role">'+p.role+'</div>'
      +'<div class="last">'+(p.lastText||'—')+'</div>'
      +'<div class="ts">마지막: '+ago(p.lastTs)+'</div>';
    g.appendChild(d);
  });
}
load(); setInterval(load,5000);
</script></body></html>
```

- [ ] **Step 4: 서버 재시작 + 검증**

```bash
# 7878 리스닝 프로세스 종료 후 재시작 (PowerShell)
# Get-NetTCPConnection -LocalPort 7878 ... Stop-Process; Start-Process python server.py
curl -s "http://localhost:7878/api/roster"
curl -s "http://localhost:7878/view/page/roster" -o /dev/null -w "%{http_code}\n"
```
Expected: `/api/roster` JSON 8개 항목(who/role/lastTs) / page 200 / 사이드바에 "👥 직원 명단" 탭.

- [ ] **Step 5: Commit (server.py만; roster.html은 pages/=gitignore)**

```bash
git add tools/studio-dashboard/server.py
git commit -m "feat(dashboard): 직원 명단 패널 /api/roster — 8직군 마지막 활동 (Phase1 T5)"
```

---

### Task 6: Phase 1 평가 기준 + 측정 로그 (phase1-eval.md)

**Files:**
- Create: `docs/studio/phase1-eval.md`

- [ ] **Step 1: 평가 문서 작성**

```markdown
# Phase 1 평가 — PM + 파이프 충전 가설

> 풀 조직 짓기 전 싸게 검증. 통과해야 Phase 2(디자이너·아트) 진행. 미달이면 접는다.

## 가설
PM이 병렬로 다음 일을 미리 깔면, 메인이 작업 끝낼 때 다음 게 대기 0(brief:ready)으로 준비된다.

## 성공 기준 (3개 중 ≥2 통과해야 Phase 2)
1. **파이프 충전**: 작업 3개 연속 처리 중, 다음 작업 brief가 이전 작업 종료 전 ready였던 횟수 ≥ 2/3.
2. **공백 메움**: 도현 부재 블록에서 PM이 backlog 능동 갱신 → 도현 방향 없이 진행된 작업 ≥ 1건.
3. **저부담**: PM 운용 비용(띄우고 먹임) < 파이프 충전 이득(메인 대기시간 감소). 주관 판정이되 근거 기록.

## kill 기준
위 3개 중 2개 이상 미달 → PM 상주 접고 "세션 시작 시 backlog 그루밍 1회"로 격하. Phase 2 보류.

## 측정 로그 (작업마다 한 줄)
| 날짜 | 작업 | 착수 시 brief ready? | 도현 방향 받음? | 메모 |
|---|---|---|---|---|
| | | | | |
```

- [ ] **Step 2: Commit**

```bash
git add docs/studio/phase1-eval.md
git commit -m "feat(studio): Phase1 성공/kill 기준 + 측정 로그 (Phase1 T6)"
```

---

### Task 7: 첫 실가동 — PM 띄우고 한 바퀴 검증

**Files:** (없음 — 실가동 검증)

- [ ] **Step 1: 도현이 별도 터미널에서 `docs/studio/BOOT-PM.md` 따라 PM 인스턴스 부팅**

PM이 부활 의례 수행 → backlog 갱신 + brief 1~2개 `docs/studio/briefs/`에 생성 확인.

- [ ] **Step 2: 대시보드 확인**

`localhost:7878` → `🔧 파이프라인`(다음 brief ready 배지) + `👥 직원 명단`(PM 카드 "최근" 표시) 둘 다 살아있는지.

- [ ] **Step 3: 핸드셰이크 1회**

메인(나)이 backlog 대기 1순위 + 해당 brief 읽고 착수 → 완료 후 PM이 backlog 반영. `phase1-eval.md` 측정 로그에 한 줄 기록.

- [ ] **Step 4: 평가**

3개 작업 정도 돌린 뒤 `phase1-eval.md` 성공 기준 채점. 통과 시 Phase 2 plan 착수, 미달 시 kill 절차(PM 격하).
```
