# PitCrew 팀 흐름 — 단계 · 일꾼 5개 · 카드 · 증거 묶음 (2026-11-03)

> 결정: **D-271 도현 진행 신호 2026-11-03** — 「어차피 고치면서 아트·리서치로 개선을 해 나가야 하는데 그걸 없이 개선해야 한다는 것 자체가 말이 안 돼서, 위의 방식대로 팀을 구성해서 해 보자」.
> 아트·리서치는 멈추지 않는다. 대신 **모든 아트·리서치는 기능 카드에 묶여 흐름 안에서 돈다.**
> 이 문서가 옛 「상주 팀(역할 10개·대기열 4줄)」 운영을 대체한다. 기계용 정의 = `docs/team/flow.json`, 도구 = `scripts/card.py`, 카드 양식 = `docs/team/CARD_TEMPLATE.md`.
> 근거 조사: `D:/Github/assistant/배움/AI개발-워크플로-조사/종합.md`(A 실제 게임팀 · B AI 게임 개발 확인 루프 · C 보이게 하기), 점검 `D:/Github/assistant/배움/F1-작업-점검-2026-11-02.md`.

## 1. 왜 바꾸나 (한 줄씩)
- 커밋이 건드린 파일 중 docs/qa 33%·art 29%·게임 코드 1.6%, 같은 대상 재작업(장면 A 18회·집 17회) — 테스트·버그에 시간을 쓰는데 성과가 안 보였다.
- 역할 10개·대기열 4줄이지만 Unity 에디터 잠금으로 게임 쪽은 결국 한 줄 — 화살표만 많고 「누가 무엇을 누구에게 넘기나」가 안 보였다.
- 효과가 확인된 다중 에이전트는 거의 「만든 과정을 모르는 검증자」 하나, 그보다 강한 건 기계 PASS/FAIL(조사 B).

## 2. 일꾼 5개 (+ 도현)
| 일꾼 | 상태 파일 | 하는 일 | 합친 옛 역할 |
|---|---|---|---|
| **PD** (프로듀서, Hermes 대화) | `sessions/pd.state` | 기능 카드 작성(수용 기준), 순서, 넘김 확인, 막힘, 도현 판정 묶음. 방향 논쟁은 도현에게 2지선다 1개 | (그대로) |
| **구현** `build` | `build.state` | 카드 하나를 그레이박스부터 통합까지 끝까지 | programmer · feature · mapgen |
| **검증** `verify` | `verify.state` | 블라인드: 코드·작업자 설명을 보지 않고 카드 수용 기준 + 증거 묶음만 보고 통과/불통과 + 근거 프레임. 「재밌나」는 판정 안 함 | qa |
| **아트** `art` | `art.state` | 카드에 묶인 아트만. 엔진 = `config.json` `art.engine`(codex/claude), 과제 태그 `[CLAUDE]`/`[CODEX]`/`[STUDIO]` | art · art2 · artui · artlive · mapshot |
| **리서치** `research` | `research.state` | 카드에 달린 질문(레퍼런스·자료)에만 답함 → 카드 폴더 `research.md` | research |
| 도현 (디렉터) | — | 「도현 한 판」 칸: 직접 해 보고 통과/수정/버림, 방향 2지선다 | — |

- **동시 진행 = 일꾼마다 카드 1장.** 대기열 루프가 그 일꾼의 state 가 working 이면 다음 과제를 안 집는다. 본 에디터가 필요한 일(build·증거 묶음)은 `editor.lock` 으로 한 줄.
- 역할 프롬프트 `.claude/team/roles/{pd,build,verify,art,research}.md`. 옛 10개는 `.claude/team/roles/archive/2026-11-03/`, 옛 세션 id·state·config 는 `.claude/team/sessions/archive/2026-11-03/`(로컬), 옛 config 값은 `config.json` 의 `_archive_2026-11-03`.
- `team.sh` 는 옛 이름도 받는다(별칭): `programmer|feature|mapgen → build`, `qa → verify`, `art2|artui|artlive|mapshot → art`(artlive = 스튜디오 변형). 그래서 `team-fix.sh` 같은 옛 도구도 그대로 돈다.
- **되돌리기**: 옛 역할 파일을 `roles/` 로 다시 옮기고 `team.sh` 맨 위 별칭 `case` 한 줄을 지우면 이전과 같다(대기열 줄 이름 매핑은 `team_queue.sh` `case`).

## 3. 단계(보드 칸)
```
기획 → (리서치, 필요 시) → 그레이박스 → 검증 → 도현 한 판 → (아트) → 통합 → 검증(통합) → 완료
                                                               + 막힘
```
| 단계 id | 칸 | 주인 | 들어오는 조건 | 나가는 조건 |
|---|---|---|---|---|
| plan | 기획 | PD | 카드 만듦 | 수용 기준·범위·잠금·시작 장면이 다 참 |
| research | 리서치 | research | 카드에 「리서치 질문」 | `research.md` 씀 → 기획으로(PD 가 카드 고침) |
| greybox | 그레이박스 | build | PD 가 넘김 / 검증 불통과 | 과제 끝 → 하네스 증거 묶음 기계 PASS |
| verify | 검증 | verify | 증거 묶음 기계 PASS | `verdict.json` PASS → 도현 한 판 / FAIL → 그레이박스(불통과 +1) |
| dohyun | 도현 한 판 | 도현 | 검증 PASS | 도현 답: 통과 → 아트 또는 통합, 수정 → 그레이박스, 버림 → 완료(버림 기록) |
| art | 아트 | art | 도현 통과 + 카드에 「아트」 | 납품 → 통합 |
| integrate | 통합 | build | 아트 납품 / 아트 불필요 | 과제 끝 → 증거 묶음 기계 PASS |
| final_verify | 검증(통합) | verify | 기계 PASS | PASS → 완료 / FAIL → 통합(불통과 +1) |
| done | 완료 | — | | |
| blocked | 막힘 | PD(또는 도현) | 일꾼 과제 보류 · 3회 규칙 | PD 가 원인 처리 후 알맞은 칸으로 |

- 허용되는 넘김은 `flow.json` `transitions` 만 — `card.py move` 가 그 밖의 넘김을 거부한다.
- **카드에 안 묶인 아트·리서치·도구 작업은 만들지 않는다.** 필요하면 먼저 카드에 「아트」「리서치 질문」 절을 쓴다.

## 4. 증거 묶음 (카드당 하나 — 스크린샷 대량 생산 대신)
하네스가 찍는다(작업자가 고른 사진이 아니라): `bash tools/evidence/bundle.sh <ID> after` → `docs/team/cards/<ID>-*/evidence/`.
| 항목 | 어떻게 | 기계 판정 |
|---|---|---|
| 로그 WARNING/ERROR 검사 | 클립 동안 `Application.logMessageReceived` 로 경고·에러 수·첫 12줄 + 끝난 뒤 콘솔 에러 | `log_ok` = 에러 0 |
| Time.frameCount 증가 | 클립 시작·끝 frameCount, 실제로 바뀐 프레임 수(멈춘 화면도 사진은 찍힌다 — Unity 공식 unity-agent-plugin playmode-verification-loop) | `frames_ok` = 10초에 ≥100프레임 |
| 8~15초 클립 | 기본 10초, 초당 12장. Unity Recorder 는 -batchmode 에서 녹화 안 됨(공식 Known issues)이라 쓰지 않는다 → GUI 에디터 Play 중 `EditorApplication.update` 콜백에서 도현이 보는 저해상도 화면(`LowResRenderTest.Present`, 없으면 Main Camera)을 PNG 로 받아 ffmpeg 으로 `clip.mp4` | — |
| 검증용 한 장 | `strip.png` = 클립을 시간 순 8칸(검증 일꾼은 영상을 못 보므로) | — |
| 전/후 사진 1쌍 | `before/shot.png`(작업 전 PD 가 `bundle.sh <ID> before`) · `after/shot.png`(클립 마지막 장) | — |
| 검증 판정 1줄 | 검증 일꾼이 `verdict.json` | PASS/FAIL |
- 순서: 시작 장면(`start_place` → `DebugWindow.GoPlace`) → 카드 폴더 `start.cs`(구현 일꾼이 쓰는 10초 시연 스크립트) → 클립.
- 요약 `evidence.json` 의 `machine` = PASS(프레임·로그 둘 다) / FAIL. 기계 FAIL 이면 검증 일꾼을 부르지 않고 바로 구현으로 되돌린다(불통과 +1).

## 5. 3회 규칙
- 같은 카드(같은 대상)가 **3번 불통과**(기계 FAIL·검증 FAIL 합산, 카드 머리말 `fails`)면 `card.py` 가 자동으로 「막힘」으로 옮기고 주인을 도현으로 한다. PD 는 다음 판을 돌리지 않고 도현에게 **2지선다 1개**(근거 클립·사진, 추천 하나)로 올린다.

## 6. 넘김 기록 (기계가 읽는 형식)
- `docs/team/handoffs.jsonl` — 한 줄 = 한 번 넘김. 다음 카드의 화면(스튜디오 대시보드)이 이걸 읽는다.
```json
{"ts":"2026-11-03T01:40:12+09:00","card":"C-001","from":"build","to":"verify","stage_from":"greybox","stage_to":"verify","note":"증거 묶음 기계 PASS(…)","evidence":"docs/team/cards/C-001-repair-one/evidence.json","verdict":null,"fails":0}
```
  필수 키: `ts`(ISO, +09:00) · `card` · `from` · `to`(일꾼 id 또는 dohyun) · `stage_from` · `stage_to` · `note`(한 줄). 선택: `evidence` · `verdict`(PASS/FAIL) · `fails`. 같은 단계 안의 「대기열에 과제 넣음」도 `stage_from == stage_to` 로 남는다.
- `docs/team/board.json` — 넘김마다 다시 쓰는 스냅숏: 목표(goal)·단계·일꾼·카드별 단계/주인/불통과/클립·`dohyun_waiting`(최대 3).
- `.ai_logs/team/queue.log` 에도 `card C-001 greybox→verify …` 한 줄(PD 깨우기 `wait_next_event.sh` 가 봄).

## 7. 자동으로 도는 부분 (대기열)
| 줄(queue 폴더) | 루프 | 일꾼 |
|---|---|---|
| `editor` | `team_queue.sh editor` | build(본 에디터) |
| `verify` | `team_queue.sh verify` | verify(에디터 안 씀 — build 와 병렬) |
| `research` | `team_queue.sh research` | research |
| `art` | `art_queue.sh` | art(엔진 config, 기본 Codex) |
| `studio` | `team_queue.sh studio` | art(Claude, F1Studio) — 둘 다 `art.state` 를 봐서 동시 1장 |
- 카드 과제 파일 이름에 `C-NNN` 이 있으면, 끝난 뒤 루프가: (build) 증거 묶음 → `card.py advance` → 다음 단계 → 다음 일꾼 대기열에 과제 / (verify) `verdict.json` 읽고 도현 한 판 또는 구현으로 / 보류면 `card.py hold` → 막힘.
- `feature` 줄(사본 에디터)은 은퇴 — build 일꾼 한 줄로 합쳤다.
- 루프는 `scripts/start_queues.ps1`(WMI, Hermes 밖) + 작업 스케줄러 `PitCrewQueueWatchdog`(5분)가 띄운다.

## 8. PD 명령 요약
```bash
python -X utf8 scripts/card.py new "<제목>" --slug <영문> --start-place <N> [--opus]   # 카드 만듦(기획 칸)
# card.md 를 채운 뒤
bash tools/evidence/bundle.sh C-00N before                                            # 전 사진
python -X utf8 scripts/card.py move C-00N greybox --by pd --note "<한 줄>"
python -X utf8 scripts/card.py dispatch C-00N                                          # 구현 대기열에 과제
python -X utf8 scripts/card.py board | log 20                                          # 보기
# 도현 답
python -X utf8 scripts/card.py move C-00N integrate --by dohyun --note "<도현 원문>"      # 통과(아트 불필요)
python -X utf8 scripts/card.py move C-00N greybox --by dohyun --note "<원문>" && python -X utf8 scripts/card.py dispatch C-00N   # 수정
```
