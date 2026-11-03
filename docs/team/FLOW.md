# PitCrew 팀 흐름 — 기계 쪽 정의 (2026-11-03) · 기준: `docs/team/WORKFLOW-v2.md`

> 결정 D-271(도현 진행 신호 2026-11-03): 「어차피 고치면서 아트·리서치로 개선을 해 나가야 하는데 그걸 없이 개선해야 한다는 것 자체가 말이 안 돼서, 위의 방식대로 팀을 구성해서 해 보자」 → 아트·리서치는 멈추지 않되 **기능 카드에 묶여 흐름 안에서** 돈다. 옛 「상주 팀(역할 10개·대기열 4줄)」 대체.
> 원칙·단위·단계·팀원 방식·도현이 보는 것·PD 턴 → WORKFLOW-v2 §0~§5. 이 문서는 **기계가 읽는 정의와 도구**만: 상태 파일·칸 조건·증거 묶음·넘김 기록·대기열·명령. 기계 정의 `docs/team/flow.json`, 도구 `scripts/card.py`, 카드 양식 `docs/team/CARD_TEMPLATE.md`.
> 절 번호는 역할 파일(`roles/verify.md` 등)이 가리키므로 유지.

## 1. 왜 바꿨나 (기록)
- 커밋 파일 중 docs/qa 33%·art 29%·게임 코드 1.6%, 같은 대상 재작업(장면 A 18회·집 17회). 역할 10개여도 에디터 잠금으로 게임 쪽은 한 줄. 효과가 확인된 다중 에이전트는 「만든 과정을 모르는 검증자」 하나 + 기계 PASS/FAIL(조사 B).
- 근거 조사 `D:/Github/assistant/배움/AI개발-워크플로-조사/종합.md`, 점검 `D:/Github/assistant/배움/F1-작업-점검-2026-11-02.md`.

## 2. 일꾼 5개 — 상태 파일·별칭 (역할 정의 → WORKFLOW-v2 §1)
| 일꾼 | 상태 파일 | 합친 옛 역할 |
|---|---|---|
| **PD**(Hermes 대화) | `sessions/pd.state` | (그대로) |
| **구현** `build` — 카드 하나를 그레이박스부터 통합까지 | `build.state` | programmer · feature · mapgen |
| **검증** `verify` — 블라인드: 코드·작업자 설명 안 보고 수용 기준 + 증거 묶음만, 「재밌나」는 판정 안 함 | `verify.state` | qa |
| **아트** `art` — 엔진 = `config.json` `art.engine`(codex/claude), 과제 태그 `[CLAUDE]`/`[CODEX]`/`[STUDIO]` | `art.state` | art · art2 · artui · artlive · mapshot |
| **리서치** `research` — 카드의 질문에만 → 카드 폴더 `research.md` | `research.state` | research |

- 대기열 루프는 그 일꾼의 state 가 working 이면 다음 과제를 안 집는다. 본 에디터가 필요한 일(build·증거 묶음)은 `editor.lock` 으로 한 줄.
- 역할 프롬프트 `.claude/team/roles/{pd,build,verify,art,research}.md`. 옛 10개는 `roles/archive/2026-11-03/`, 옛 세션은 `sessions/archive/2026-11-03/`(로컬), 옛 config 값은 `config.json` `_archive_2026-11-03`.
- `team.sh` 별칭: `programmer|feature|mapgen → build`, `qa → verify`, `art2|artui|artlive|mapshot → art`(artlive = 스튜디오 변형). 옛 도구(`team-fix.sh`)도 그대로 돈다.
- **되돌리기**: 옛 역할 파일을 `roles/` 로 옮기고 `team.sh` 맨 위 별칭 `case` 한 줄을 지우면 이전과 같다(대기열 줄 이름은 `team_queue.sh` `case`).

## 3. 단계(보드 칸) — 들어오는/나가는 조건 (단계 순서 → WORKFLOW-v2 §1)
| 단계 id | 칸 | 주인 | 들어오는 조건 | 나가는 조건 |
|---|---|---|---|---|
| plan | 기획 | PD | 카드 만듦 | 수용 기준·범위·잠금·시작 장면이 다 참 |
| research | 리서치 | research | 카드에 「리서치 질문」 | `research.md` 씀 → 기획으로(PD 가 카드 고침) |
| greybox | 그레이박스 | build | PD 가 넘김 / 검증 불통과 | 과제 끝 → 증거 묶음 기계 PASS |
| verify | 검증 | verify | 증거 묶음 기계 PASS | `verdict.json` PASS → 도현 한 판 / FAIL → 그레이박스(불통과 +1) |
| dohyun | 도현 한 판 | 도현 | 검증 PASS | 통과 → 아트 또는 통합, 수정 → 그레이박스, 버림 → 완료(버림 기록) |
| art | 아트 | art | 도현 통과 + 카드에 「아트」 | 납품 → 통합 |
| integrate | 통합 | build | 아트 납품 / 아트 불필요 | 과제 끝 → 증거 묶음 기계 PASS |
| final_verify | 검증(통합) | verify | 기계 PASS | PASS → 완료 / FAIL → 통합(불통과 +1) |
| done | 완료 | — | | |
| blocked | 막힘 | PD(또는 도현) | 일꾼 과제 보류 · 3회 규칙 | PD 가 원인 처리 후 알맞은 칸으로 |

- 허용되는 넘김은 `flow.json` `transitions` 만 — `card.py move` 가 그 밖의 넘김을 거부한다.

## 4. 증거 묶음 (카드당 하나 — 스크린샷 대량 생산 대신)
하네스가 찍는다(작업자가 고른 사진이 아니라): `bash tools/evidence/bundle.sh <ID> after` → `docs/team/cards/<ID>-*/evidence/`.
| 항목 | 어떻게 | 기계 판정 |
|---|---|---|
| 로그 WARNING/ERROR | 클립 동안 `Application.logMessageReceived` 로 경고·에러 수·첫 12줄 + 끝난 뒤 콘솔 에러 | `log_ok` = 에러 0 |
| Time.frameCount 증가 | 클립 시작·끝 frameCount(멈춘 화면도 사진은 찍힌다 — Unity unity-agent-plugin playmode-verification-loop) | `frames_ok` = 초당 2프레임 이상(멈춤 검출 — 캡처가 에디터를 6~13fps 로 늦추므로 성능 판정 아님) |
| 8~15초 클립 | 기본 10초, 초당 12장. Unity Recorder 는 -batchmode 에서 녹화 안 됨 → GUI 에디터 Play 중 `EditorApplication.update` 콜백에서 저해상도 화면(`LowResRenderTest.Present`, 없으면 Main Camera)을 PNG 로 받아 ffmpeg 으로 `clip.mp4` | — |
| 검증용 한 장 | `strip.png` = 클립을 시간 순 8칸(검증 일꾼은 영상을 못 보므로) | — |
| 전/후 사진 1쌍 | `before/shot.png`(작업 전 PD 가 `bundle.sh <ID> before`) · `after/shot.png`(클립 마지막 장) | — |
| 검증 판정 1줄 | 검증 일꾼이 `verdict.json` | PASS/FAIL |
- 순서: 시작 장면(`start_place` → `DebugWindow.GoPlace`) → 카드 폴더 `start.cs`(구현 일꾼이 쓰는 10초 시연 스크립트) → 클립.
- `evidence.json` 의 `machine` = PASS(프레임·로그 둘 다) / FAIL. 기계 FAIL 이면 검증 일꾼을 부르지 않고 바로 구현으로(불통과 +1).
- 과제 앞뒤의 세계 스냅숏·불변 조건 검사는 `docs/world/README.md`.

## 5. 3회 규칙 → WORKFLOW-v2 §1
기계 쪽: 기계 FAIL·검증 FAIL 합산이 카드 머리말 `fails`, 3 이면 `card.py` 가 자동으로 「막힘」+주인 도현. PD 는 다음 판 대신 2지선다 1개.

## 6. 넘김 기록 (기계가 읽는 형식)
- `docs/team/handoffs.jsonl` — 한 줄 = 한 번 넘김. 스튜디오 화면이 이걸 읽는다.
```json
{"ts":"2026-11-03T01:40:12+09:00","card":"C-001","from":"build","to":"verify","stage_from":"greybox","stage_to":"verify","note":"증거 묶음 기계 PASS(…)","evidence":"docs/team/cards/C-001-repair-one/evidence.json","verdict":null,"fails":0}
```
  필수 키: `ts`(ISO, +09:00) · `card` · `from` · `to`(일꾼 id 또는 dohyun) · `stage_from` · `stage_to` · `note`. 선택: `evidence` · `verdict`(PASS/FAIL) · `fails`. 같은 단계 안의 「대기열에 과제 넣음」도 `stage_from == stage_to` 로 남는다.
- `docs/team/board.json` — 넘김마다 다시 쓰는 스냅숏: 목표·단계·일꾼·카드별 단계/주인/불통과/클립·`dohyun_waiting`(최대 3).
- `.ai_logs/team/queue.log` 에도 `card C-001 greybox→verify …` 한 줄(PD 깨우기 `wait_next_event.sh` 가 봄).

## 7. 자동으로 도는 부분 (대기열)
| 줄(queue 폴더) | 루프 | 일꾼 |
|---|---|---|
| `editor` | `team_queue.sh editor` | build(본 에디터) |
| `verify` | `team_queue.sh verify` | verify(에디터 안 씀 — build 와 병렬) |
| `research` | `team_queue.sh research` | research |
| `art` | `art_queue.sh` | art(엔진 config, 기본 Codex) |
| `studio` | `team_queue.sh studio` | art(Claude, F1Studio) — 둘 다 `art.state` 를 봐서 동시 1장 |
- 과제 파일 이름에 `C-NNN` 이 있으면 끝난 뒤 루프가: (build) 증거 묶음 → `card.py advance` → 다음 일꾼 대기열에 과제 / (verify) `verdict.json` 읽고 도현 한 판 또는 구현으로 / 보류면 `card.py hold` → 막힘.
- `feature` 줄(사본 에디터)은 은퇴 — build 한 줄로 합침.
- 루프는 `scripts/start_queues.ps1`(WMI, Hermes 밖) + 작업 스케줄러 `PitCrewQueueWatchdog`(5분)가 띄운다. 정지 깃발 `.claude/team/queue/STOP`(새 과제 안 집음, 도는 건 끝까지).

## 8. PD 명령 요약
```bash
python -X utf8 scripts/card.py new "<제목>" --slug <영문> --start-place <N> [--opus]   # 카드 만듦(기획 칸)
# card.md 를 채운 뒤
bash tools/evidence/bundle.sh C-00N before                                            # 전 사진
python -X utf8 scripts/card.py move C-00N greybox --by pd --note "<한 줄>"
python -X utf8 scripts/card.py dispatch C-00N                                          # 구현 대기열에 과제
python -X utf8 scripts/card.py board | log 20                                          # 보기
# 도현 답(원문 그대로 --note)
python -X utf8 scripts/card.py move C-00N integrate --by dohyun --note "<도현 원문>"      # 통과(아트 불필요)
python -X utf8 scripts/card.py move C-00N greybox --by dohyun --note "<원문>" && python -X utf8 scripts/card.py dispatch C-00N   # 수정
```

## 9. 보이게 하기 — 기계 쪽 (D-272; 도현이 보는 것 → WORKFLOW-v2 §4)
- 화면 = `scripts/team_dashboard.py` + `scripts/studio_page.html`(5초 갱신): 목표 띠(마일스톤·D-day·카드 N/M·동시 진행/`wip_limit`·오늘 비용/`daily_budget_usd`) → 단계 흐름 → 판정 대기(최대 3장) · 일꾼 레인 · 최근 게임 화면 → 막힘 → 넘김 기록.
- 판정 버튼은 `cards/<ID>/dohyun.json` + 카드 기록 + handoffs(dohyun→pd) + queue.log 「도현 판정 끝」만 쓴다. 단계 이동은 PD 가 `card.py move` 로 — 화면이 판정을 대신 내리지 않는다.
- 카드의 「사람 확인」 절에 `묻는 질문: 「…」` 한 줄을 꼭 쓴다 — 화면·묶음 메시지가 이 줄을 질문으로 띄운다.
- 폰 = `scripts/tg_digest.py`(대시보드 프로세스 안 스레드, 60초): `digest_hours`(13·21시) 묶음 = 목표 1줄 + 판정 카드 최대 3장 + 막힘 1줄 + 비용 1줄. 즉시 알림 4가지 — 판정 급함(대기 3장 참 / 3회 막힘 / 일꾼 모두 쉬고 남은 일이 도현 답뿐 60분), 과제 실패(새 「보류」·컴파일 실패), 같은 카드 반복 실패(불통과 2+), 비용 한도 80%·100%. 밤 1~9시는 소리 없이. 나머지 자동 메시지는 D-265 조용 모드로 끔.
- 값(`wip_limit` 3, `daily_budget_usd` 40, `digest_hours`, `stall_min`, `stale_hours`, `alert_quiet_hours`)은 `flow.json` — PD 가설값, 도현이 정하면 그대로 바꾼다.
- Hermes 칸반(비서·PD 카드)은 따로 — 화면 오른쪽 위 「칸반 그래프」(`/graph`). F1 팀 내부 넘김은 handoffs.jsonl 을 화면이 직접 그린다.
