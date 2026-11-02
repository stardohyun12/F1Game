#!/usr/bin/env bash
# 대기열 루프(10/31, 도현 ㄱㄱ): 줄이 비면 다음 과제를 자동으로 가져간다 — 밤 줄이 끝나고 14시간 놀던 것 방지.
# 사용: bash scripts/team_queue.sh <editor|studio|verify|research>   (줄마다 하나, scripts/start_queues.ps1 이 띄움)
# 줄 = 일꾼(2026-11-03 팀 재편, docs/team/FLOW.md): editor → build(본 에디터 한 줄) · studio → art(Claude, F1Studio) ·
#   verify → verify(블라인드 검증, 에디터 안 씀) · research → research. Codex 아트는 scripts/art_queue.sh(같은 art 일꾼 — 동시에 1장).
#   feature 줄(사본 에디터)은 build 일꾼과 합쳐져 은퇴 — 일꾼마다 카드 1장.
# 과제 넣기: .claude/team/queue/<줄>/NN-이름.task 파일만 쓰면 됨(번호 순). 카드 과제는 `python scripts/card.py dispatch <ID>` 가 씀.
# 카드 과제(이름에 C-NNN)가 끝나면 card.py advance(증거 묶음 → 다음 단계 → 다음 일꾼 대기열) / 보류면 card.py hold(막힘).
# 모델(D-223): 과제 첫 줄 [OPUS] 면 opus, 아니면 config 기본 sonnet. 카드 아닌 과제만 실패 시 [OPUS] 로 1회 자동 승격.
# 결과 처리: COMPILE-BROKEN / FAILED-NOREPORT 면 커밋하지 않고 queue/<줄>/hold/ 에 남김(PD 확인).
cd D:/Github/F1Game || exit 1
export GIT_TERMINAL_PROMPT=0 GIT_ASKPASS= SSH_ASKPASS= GCM_INTERACTIVE=never  # 창 없는 실행에서 GitHub 로그인 창 금지(D-268)
PY=/c/Users/rtx40/AppData/Local/Programs/Python/Python313/python.exe
LINE=$1; Q=.claude/team/queue/$LINE; S=.claude/team/sessions
case $LINE in
  editor) ROLE=build; CALL=build;;
  studio) ROLE=art; CALL=artlive;;
  verify) ROLE=verify; CALL=verify;;
  research) ROLE=research; CALL=research;;
  feature) echo "feature 줄은 2026-11-03 팀 재편으로 은퇴(build 일꾼 한 줄) — docs/team/FLOW.md"; exit 0;;
  *) echo "줄 이름?"; exit 1;;
esac
# 줄마다 하나만(중복 실행 = 같은 과제 두 번·에디터 충돌): PID 파일 잠금
PIDF=.claude/team/queue/$LINE.pid
if [ -s "$PIDF" ] && kill -0 "$(cat "$PIDF")" 2>/dev/null; then echo "queue/$LINE 이미 실행 중 PID $(cat "$PIDF")"; exit 0; fi
echo $$ > "$PIDF"
mkdir -p "$Q/done" "$Q/hold"
log(){ echo "[$(date '+%m/%d %H:%M')] queue/$LINE $*" | tee -a .ai_logs/team/queue.log; }
commit(){
  git checkout -- F1/Assets/UI/Fonts 2>/dev/null
  for p in F1/Assets/Editor F1/Assets/Scripts F1/Assets/Art/Look F1/Assets/Art/Library F1/Assets/Audio F1/Assets/Scenes/Map_TownV6.unity F1/Assets/Scenes/Map_TownV6_Mats docs/map docs/qa/evidence docs/team/reports docs/team/cards docs/team/handoffs.jsonl docs/team/board.json docs/research F1Studio/Assets/Art $(ls -d art/_review/$(date +%Y-%m-%d)-* 2>/dev/null) tools/asset-gen tools/viewer/feedback .claude/team/ledger.jsonl; do git add "$p" 2>/dev/null; done
  git commit -qm "$1 [skip-verifier]" 2>/dev/null && { timeout 900 git push -q 2>&1 | grep -E "rejected"; log "커밋 $(git log --oneline -1)"; $PY -X utf8 scripts/notify_done.py $LINE "$NAME" ok "$(git rev-parse --short HEAD)" >/dev/null 2>&1; }; }
while true; do
  T=$(ls "$Q"/*.task 2>/dev/null | sort | head -1)
  # 일꾼마다 카드 1장: 그 일꾼이 일하는 중이면 기다림. build 는 본 에디터 잠금도 봄(증거 묶음·도현 기획 창).
  if [ -z "$T" ] || grep -q '^working' "$S/$ROLE.state" 2>/dev/null || { [ $ROLE = build ] && [ -s .claude/team/editor.lock ]; }; then sleep 60; continue; fi
  NAME=$(basename "$T" .task); mv "$T" "$Q/done/$NAME.task"
  CARD=$(printf '%s' "$NAME" | grep -oE 'C-[0-9]{3}' | head -1)
  mv -f "$S/$ROLE.id" "$S/archive/$ROLE-$NAME.id" 2>/dev/null; mv -f "$S/$ROLE.claude.id" "$S/archive/$ROLE-claude-$NAME.id" 2>/dev/null   # 과제마다 새 세션(긴 세션 = 비용 증가)
  log "시작 $NAME"
  if [ $ROLE = build ]; then
    RULE="[하네스 고정 규칙 D-270] 기능 2 봇 전체 경로(방→찻집)는 돌리지 마라 — PD 가 여러 과제 끝에 한 번 따로 돌린다. 확인은 디버그 창 순간이동(DebugWindow.GoPlace)으로 고친 자리에서 바로, 캡처 1~2장. sleep 으로 기다리는 검사 금지. 과제 글에 '봇 1회'가 있어도 이 규칙이 우선."
    OUT=$(bash scripts/team.sh $CALL "$(printf '%s
%s' "$(cat "$Q/done/$NAME.task")" "$RULE")" 2>&1)   # 과제 첫 줄이 먼저(상태·대시보드 제목) — 규칙은 뒤에
  else
    OUT=$(bash scripts/team.sh $CALL "$(cat "$Q/done/$NAME.task")" 2>&1)
  fi
  rm -f .claude/team/editor.lock
  echo "$OUT" | tail -40 > "$Q/done/$NAME.out"
  if echo "$OUT" | grep -qE "COMPILE-BROKEN|FAILED-NOREPORT|ok=False"; then
    WHY=$(echo "$OUT" | grep -oE 'COMPILE-BROKEN|FAILED-NOREPORT|ok=False' | head -1)
    log "보류 $NAME ($WHY) — 커밋 안 함"
    $PY -X utf8 scripts/notify_done.py $LINE "$NAME" hold >/dev/null 2>&1
    if [ -n "$CARD" ]; then   # 카드 과제: 자동 승격 대신 막힘으로(3회 규칙은 card.py 가 셈)
      mv "$Q/done/$NAME.task" "$Q/hold/"; $PY -X utf8 scripts/card.py hold "$CARD" "$NAME $WHY" >/dev/null 2>&1; commit "card: $CARD 보류"; continue
    fi
    # D-223 자동 승격: Sonnet 과제가 실패하면 같은 과제를 [OPUS] 로 한 번 다시(이미 OPUS 면 hold)
    CHG=$(git status --porcelain -- F1/Assets docs/qa/evidence 2>/dev/null | grep -vcE 'UI/Fonts|LofiSurface')   # D-264: 일을 해 놓고 보고서만 못 쓴 경우는 승격 안 함
    if [ $ROLE = build ] && ! grep -q '\[OPUS\]' "$Q/done/$NAME.task" && [ "${CHG:-0}" -eq 0 ]; then
      { printf '[OPUS]'; cat "$Q/done/$NAME.task"; printf '
이어 하기(자동 승격): Sonnet 시도가 실패함 — 직전 보고서 docs/team/reports/ 의 최신 %s 보고서와 git status 를 먼저 보고 이어서.
' "$ROLE"; } > "$Q/00-$NAME-opus.task"
      log "승격 $NAME → [OPUS] 재시도 예약"; mv "$Q/done/$NAME.task" "$Q/done/$NAME.sonnet-fail.task"; continue
    fi
    mv "$Q/done/$NAME.task" "$Q/hold/"; continue
  fi
  commit "$LINE: $NAME"
  if [ -n "$CARD" ]; then   # 넘김: 증거 묶음(build 끝, bash 에서) → 다음 단계 → 다음 일꾼 대기열. 기록은 docs/team/handoffs.jsonl
    RC=""
    if [ $ROLE = build ]; then bash tools/evidence/bundle.sh "$CARD" after >> "$Q/done/$NAME.out" 2>&1; RC="--bundle-rc $?"; rm -f .claude/team/editor.lock; fi
    $PY -X utf8 scripts/card.py advance "$CARD" $RC >> "$Q/done/$NAME.out" 2>&1
    commit "card: $CARD $(grep -m1 '^stage:' docs/team/cards/$CARD-*/card.md | cut -d' ' -f2)"
  fi
done
