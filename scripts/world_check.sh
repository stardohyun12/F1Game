#!/usr/bin/env bash
# D-275 세계 모델 1층 — 불변 조건 검사. 에디터(에디트 모드)에서 WorldInvariants.Check() 를 받아 stdout + docs/world/INVARIANTS.log 에 시각과 함께 추가.
# 사용: bash scripts/world_check.sh [과제이름]    → 위반(첫 줄 FAIL)이면 exit 1, OK 면 0, 에디터 못 닿음·Play 중이면 exit 2(판정 보류 — 보류 처리 안 함).
# team_queue.sh 가 build 과제 끝(커밋 전)에 부른다. 조건 목록·추가법은 docs/world/README.md.
cd D:/Github/F1Game || exit 1
LOG=docs/world/INVARIANTS.log; mkdir -p docs/world
TASK="${1:-}"
U="bash scripts/ucli-http.sh"
RES=$(timeout 120 $U exec 'if (UnityEditor.EditorApplication.isPlaying) return "SKIP: Play 중"; if (UnityEditor.EditorApplication.isCompiling) return "SKIP: 컴파일 중"; return WorldInvariants.Check();' 2>&1)
HEAD1=$(printf '%s' "$RES" | head -1)
STAMP="[$(date '+%Y-%m-%d %H:%M')]${TASK:+ $TASK}"
case "$HEAD1" in
  "OK "[0-9]*)   RC=0;;
  "FAIL "[0-9]*) RC=1;;   # ucli 의 "FAIL: request error" 는 여기 안 걸림(→ SKIP)
  *)     RC=2; RES="SKIP(에디터 응답 없음/오류): $(printf '%s' "$RES" | head -2 | tr '\n' ' ' | cut -c1-300)";;
esac
{ echo "$STAMP $(printf '%s' "$RES" | head -1)"; printf '%s\n' "$RES" | tail -n +2 | sed 's/^/    /'; } >> "$LOG"
printf '%s\n' "$RES"
exit $RC
