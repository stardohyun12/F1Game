# PitCrew 팀 — 입구 (2026-11-03 기준)

> 도현 진행 신호 2026-11-03 (D-271): 「어차피 고치면서 아트·리서치로 개선을 해 나가야 하는데 그걸 없이 개선해야 한다는 것 자체가 말이 안 돼서, 위의 방식대로 팀을 구성해서 해 보자」
> 스튜디오 화면·하루 2번 묶음 메시지 = D-272.

## 도현이 보는 곳
| 어디 | 무엇 |
|---|---|
| PC — http://localhost:8765 | **스튜디오 화면** 한 장: 지금 목표·D-day·비용 / 단계 흐름 / 일꾼 5개가 무슨 카드를 어디까지 / 도현 판정 대기(영상 + 질문 1개 + 통과·수정·버림) / 최근 게임 화면 / 막힘 / 넘김 기록. `/classic` = 일꾼 실시간 로그·설정, `/graph` = Hermes 칸반 그래프 |
| 폰 — 텔레그램 F1 탭 | **하루 2번 묶음**(13시·21시): 목표 1줄 + 판정 카드 최대 3장(영상 + 질문) + 막힘 1줄 + 비용 1줄. 답은 「C-001 통과」/「C-001 수정: 한 줄」/「C-001 버림」 |
| 즉시 알림(🚨) | 4가지만: 판정이 급함 · 과제(빌드) 실패 · 같은 카드 반복 실패 · 비용 한도 근접 |

## 팀
- 일꾼 5개: **PD**(Hermes 대화) · **구현** build · **검증** verify(블라인드) · **아트** art · **리서치** research. 옛 10역할(programmer·qa·art2·artlive·artui·mapgen·mapshot·feature…)은 **보관됨** — `.claude/team/roles/archive/2026-11-03/`.
- 모든 일은 카드: 기획 → (리서치) → 그레이박스 → 검증 → 도현 한 판 → (아트) → 통합 → 검증(통합) → 완료, 막힘 따로. 같은 카드 3번 불통과 → 막힘 → 도현 2지선다.
- 규칙 `FLOW.md` · 기계 정의 `flow.json`(목표·단계·한도·묶음 시각) · 카드 양식 `CARD_TEMPLATE.md` · 카드 `cards/C-NNN-*/` · 넘김 기록 `handoffs.jsonl` · 스냅숏 `board.json`.

## 파일
| 파일 | 하는 일 |
|---|---|
| `scripts/card.py` | 카드 만들기·옮기기·일꾼에게 보내기·보드·기록 |
| `scripts/team_queue.sh`, `art_queue.sh` | 일꾼 대기열(카드 자동 넘김) — `start_queues.ps1` 이 Hermes 밖에서 띄움 |
| `tools/evidence/bundle.sh` | 증거 묶음(10초 클립·8칸 strip·전/후) |
| `scripts/team_dashboard.py` + `studio_page.html` | 스튜디오 화면(8765) + 워치독 + 텔레그램 루프 |
| `scripts/tg_digest.py` | 묶음 메시지·즉시 알림 (`now`·`dry`·`alerts`) |

옛 문서: `.claude/team/README.md`(상주 팀 표 — 보관됨), `BOARD.md`(옛 게시판).
