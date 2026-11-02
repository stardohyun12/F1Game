#!/usr/bin/env bash
# 대기열 루프(10/31, 도현 ㄱㄱ): 줄이 비면 다음 과제를 자동으로 가져간다 — 밤 줄이 끝나고 14시간 놀던 것 방지.
# 사용: bash scripts/team_queue.sh <editor|studio|feature>   (persist background 로 줄마다 하나)
# 과제 넣기: .claude/team/queue/<줄>/NN-이름.task 파일만 쓰면 됨(번호 순). 끝난 것은 queue/<줄>/done/.
# 모델(D-223): 과제 첫 줄 [OPUS] 면 opus, 아니면 config 기본 sonnet. 실패 시 [OPUS] 로 1회 자동 승격.
# 결과 처리: COMPILE-BROKEN / FAILED-NOREPORT 면 커밋하지 않고 queue/<줄>/hold/ 에 남김(PD 확인).
cd D:/Github/F1Game || exit 1
export GIT_TERMINAL_PROMPT=0 GIT_ASKPASS= SSH_ASKPASS= GCM_INTERACTIVE=never  # 창 없는 실행에서 GitHub 로그인 창 금지(D-268)
LINE=$1; Q=.claude/team/queue/$LINE; S=.claude/team/sessions
# 줄마다 하나만(중복 실행 = 같은 과제 두 번·에디터 충돌): PID 파일 잠금
PIDF=.claude/team/queue/$LINE.pid
if [ -s "$PIDF" ] && kill -0 "$(cat "$PIDF")" 2>/dev/null; then echo "queue/$LINE 이미 실행 중 PID $(cat "$PIDF")"; exit 0; fi
echo $$ > "$PIDF"
case $LINE in editor) ROLE=programmer;; studio) ROLE=artlive;; feature) ROLE=feature;; *) echo "줄 이름?"; exit 1;; esac
mkdir -p "$Q/done" "$Q/hold"
log(){ echo "[$(date '+%m/%d %H:%M')] queue/$LINE $*" | tee -a .ai_logs/team/queue.log; }
shot_alive(){ UCLI_PROJ=D:/Github/F1Game_shot/F1 timeout 30 bash scripts/ucli-http.sh exec 'return "ok "+UnityEditor.EditorApplication.isCompiling;' 2>/dev/null | grep -q "ok False"; }
shot_restart(){ # 사본 에디터 멈춤(가져오기 워커가 포트 쥠) → 사본 Unity 전부 끄고 다시
  log "사본 에디터 응답 없음 → 재시작"
  powershell -NoProfile -Command "Get-CimInstance Win32_Process -Filter \"Name='Unity.exe'\" | ? { \$_.CommandLine -match 'F1Game_sho' } | % { taskkill /F /T /PID \$_.ProcessId | Out-Null }"
  sleep 5; rm -f D:/Github/F1Game_shot/F1/Temp/UnityLockfile
  powershell -NoProfile -File scripts/open-shot-editor.ps1 >/dev/null 2>&1
  for i in $(seq 1 45); do sleep 20; shot_alive && { log "사본 에디터 준비"; return 0; }; done
  log "사본 에디터 재시작 실패"; return 1; }
commit(){
  git checkout -- F1/Assets/UI/Fonts 2>/dev/null
  for p in F1/Assets/Editor F1/Assets/Scripts F1/Assets/Art/Look F1/Assets/Art/Library F1/Assets/Audio F1/Assets/Scenes/Map_TownV6.unity F1/Assets/Scenes/Map_TownV6_Mats docs/map docs/qa/evidence docs/team/reports F1Studio/Assets/Art $(ls -d art/_review/$(date +%Y-%m-%d)-* 2>/dev/null) tools/asset-gen tools/viewer/feedback .claude/team/ledger.jsonl; do git add "$p" 2>/dev/null; done
  git commit -qm "$1 [skip-verifier]" 2>/dev/null && { timeout 900 git push -q 2>&1 | grep -E "rejected"; log "커밋 $(git log --oneline -1)"; /c/Users/rtx40/AppData/Local/Programs/Python/Python313/python.exe -X utf8 scripts/notify_done.py $LINE "$NAME" ok "$(git rev-parse --short HEAD)" >/dev/null 2>&1; }; }
while true; do
  T=$(ls "$Q"/*.task 2>/dev/null | sort | head -1)
  if [ -z "$T" ] || grep -q '^working' "$S/$ROLE.state" 2>/dev/null || { [ $ROLE = programmer ] && [ -s .claude/team/editor.lock ]; }; then sleep 60; continue; fi
  NAME=$(basename "$T" .task); mv "$T" "$Q/done/$NAME.task"
  mv -f "$S/$ROLE.id" "$S/archive/$ROLE-$NAME.id" 2>/dev/null    # 과제마다 새 세션(긴 세션 = 비용 증가)
  log "시작 $NAME"
  if [ $ROLE = feature ]; then
    shot_alive || shot_restart || { mv "$Q/done/$NAME.task" "$Q/hold/"; continue; }
    OUT=$(UCLI_PROJ=D:/Github/F1Game_shot/F1 bash scripts/team_feature.sh feature "$(cat "$Q/done/$NAME.task")" 2>&1)
  else
    RULE="[하네스 고정 규칙 D-270] 기능 2 봇 전체 경로(방→찻집)는 돌리지 마라 — PD 가 여러 과제 끝에 한 번 따로 돌린다. 확인은 디버그 창 순간이동(DebugWindow.GoPlace)으로 고친 자리에서 바로, 캡처 1~2장. sleep 으로 기다리는 검사 금지. 과제 글에 '봇 1회'가 있어도 이 규칙이 우선."
    OUT=$(bash scripts/team.sh $ROLE "$(printf '%s
%s' "$RULE" "$(cat "$Q/done/$NAME.task")")" 2>&1)
  fi
  rm -f .claude/team/editor.lock
  echo "$OUT" | tail -40 > "$Q/done/$NAME.out"
  if echo "$OUT" | grep -qE "COMPILE-BROKEN|FAILED-NOREPORT|ok=False"; then
    log "보류 $NAME ($(echo "$OUT" | grep -oE 'COMPILE-BROKEN|FAILED-NOREPORT|ok=False' | head -1)) — 커밋 안 함"
    /c/Users/rtx40/AppData/Local/Programs/Python/Python313/python.exe -X utf8 scripts/notify_done.py $LINE "$NAME" hold >/dev/null 2>&1
    # D-223 자동 승격: Sonnet 과제가 실패하면 같은 과제를 [OPUS] 로 한 번 다시(이미 OPUS 면 hold)
    CHG=$(git status --porcelain -- F1/Assets docs/qa/evidence 2>/dev/null | grep -vcE 'UI/Fonts|LofiSurface')   # D-264: 일을 해 놓고 보고서만 못 쓴 경우는 승격 안 함
    if ! grep -q '\[OPUS\]' "$Q/done/$NAME.task" && [ "${CHG:-0}" -eq 0 ]; then
      { printf '[OPUS]'; cat "$Q/done/$NAME.task"; printf '
이어 하기(자동 승격): Sonnet 시도가 실패함 — 직전 보고서 docs/team/reports/ 의 최신 %s 보고서와 git status 를 먼저 보고 이어서.
' "$ROLE"; } > "$Q/00-$NAME-opus.task"
      log "승격 $NAME → [OPUS] 재시도 예약"; mv "$Q/done/$NAME.task" "$Q/done/$NAME.sonnet-fail.task"; continue
    fi
    mv "$Q/done/$NAME.task" "$Q/hold/"; continue
  fi
  if [ $ROLE = feature ]; then
    ( cd D:/Github/F1Game_shot && git checkout -- F1/Assets/UI/Fonts 2>/dev/null; git add F1/Assets && git commit -qm "$NAME [skip-verifier]" && timeout 300 git push -q -f origin HEAD:feature/onfoot ) && log "feature 브랜치 커밋"
  fi
  commit "$LINE: $NAME"
done
