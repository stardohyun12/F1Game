#!/usr/bin/env bash
# 카디널 D-281 (docs/CARDINAL.md §5) — 과제 1개 실행. 백그라운드 루프 없음: PD 가 필요할 때 부른다.
# 사용: bash scripts/run_task.sh <과제파일.md>        RUN_DRY=1 이면 team.sh·ucli 대신 echo (순서 확인용)
# 순서: 에디터 상태 확인(Play 면 stop) → 세계 스냅숏 → 브리프 → team.sh build(같은 과제 파일이면 세션 이어감)
#       → 컴파일 에러 확인 → world_check → 통과면 커밋 `task: <이름> [skip-verifier]` + push, 실패면 커밋 없이 이유
#       → docs/NOW.md 「도현 판정 대기」 한 줄 + tg_notify 1번 → .ai_logs/task.log 한 줄
set -u
cd "$(dirname "$0")/.." || exit 1
TASKFILE="${1:-}"
[ -f "$TASKFILE" ] || { echo "usage: run_task.sh <과제파일.md>"; exit 2; }
NAME=$(basename "$TASKFILE" .md)
DRY="${RUN_DRY:-0}"
U="bash scripts/ucli-http.sh"
SESS=.claude/team/sessions; mkdir -p "$SESS" .ai_logs docs/qa
LOG=.ai_logs/task.log
NOW=docs/NOW.md
T0=$(date +%s)
say() { printf '[%s] run_task %s | %s\n' "$(date +%H:%M:%S)" "$NAME" "$*"; }
ucli() { if [ "$DRY" = 1 ]; then echo "[DRY] ucli-http.sh $*"; else timeout 60 $U "$@" 2>&1; fi; }

# 1. 에디터 상태 — Play 중이면 멈춘다(스냅숏·검사는 에디트 모드에서만). 에디터가 꺼져 있으면 그대로 진행(구현이 보고한다).
ST=$(ucli status | tail -1)
say "에디터: $ST"
if [ "$DRY" != 1 ] && ! printf '%s' "$ST" | grep -q '^FAIL'; then
  P=$(ucli exec 'return UnityEditor.EditorApplication.isPlaying ? "PLAYING" : "EDIT";' | tail -1 | tr -d '\r')
  [ "$P" = PLAYING ] && { say "Play 중 → editor stop"; ucli editor stop >/dev/null; sleep 3; }
fi

# 2. 세계 스냅숏(실패해도 진행 — 옛 WORLD.md 유지)
if [ "$DRY" = 1 ]; then echo "[DRY] world_snapshot.sh"; else bash scripts/world_snapshot.sh || say "스냅숏 실패(옛 WORLD.md 유지)"; fi

# 3. 브리프 (실패하면 과제 글 그대로)
BRIEF=$(python -X utf8 scripts/brief.py "$TASKFILE" 2>/dev/null) || BRIEF=""
[ -z "$BRIEF" ] && { say "brief.py 실패 — 과제 글 그대로"; BRIEF=$(cat "$TASKFILE"); }
say "브리프 $(printf '%s' "$BRIEF" | wc -c)자"

# 4. 구현 — 같은 과제 파일이면 세션 이어감(build.id 유지), 다른 과제면 새 세션(옛 id 는 sessions/archive/ 로)
PREV=$(cat "$SESS/build.task_name" 2>/dev/null || true)
if [ "$PREV" != "$NAME" ] && [ -s "$SESS/build.id" ]; then
  mkdir -p "$SESS/archive"; mv -f "$SESS/build.id" "$SESS/archive/build-$PREV-$(date +%Y%m%d-%H%M).id"; rm -f "$SESS/build.cost"
  say "새 과제 → 새 세션(옛 세션 $PREV 보관)"
fi
printf '%s' "$NAME" > "$SESS/build.task_name"
RUN_DRY="$DRY" bash scripts/team.sh build "$BRIEF"; RC=$?
say "team.sh rc=$RC"

# 5. 컴파일 에러 확인
FAILS=()
if [ "$DRY" = 1 ]; then echo "[DRY] ucli-http.sh console --type error → (없음)"
else
  CF=$(ucli exec 'return UnityEditor.EditorUtility.scriptCompilationFailed ? "BROKEN" : "OK";' | tail -1 | tr -d '\r')
  ERRS=$(ucli console --type error | grep -c -a -i 'error' || true)
  [ "$CF" = BROKEN ] && FAILS+=("컴파일 깨짐")
  [ "$CF" != BROKEN ] && [ "$CF" != OK ] && say "컴파일 확인 불가(에디터 응답: $CF)"
  [ "${ERRS:-0}" -gt 0 ] && FAILS+=("콘솔 에러 $ERRS")
fi

# 6. 불변 검사 (exit 1 = 위반 → 실패, 2 = 에디터 못 닿음 → 보류 아님·경고만)
if [ "$DRY" = 1 ]; then echo "[DRY] world_check.sh $NAME → OK"
else
  bash scripts/world_check.sh "$NAME" > .ai_logs/world_check.last 2>&1; WC=$?
  head -1 .ai_logs/world_check.last
  [ $WC = 1 ] && FAILS+=("불변 위반: $(head -1 .ai_logs/world_check.last)")
  [ $WC = 2 ] && say "불변 검사 보류(에디터 응답 없음)"
fi

# 7. 커밋 — 통과면 F1/Assets + docs/qa 만. index.lock 이면 10초 뒤 재시도(다른 세션이 커밋 중일 수 있음)
COMMIT=""; RESULT=ok
if [ ${#FAILS[@]} -gt 0 ]; then
  RESULT=fail; say "커밋 안 함 — ${FAILS[*]}"
elif [ "$DRY" = 1 ]; then
  echo "[DRY] git add -A F1/Assets docs/qa && git commit -m 'task: $NAME [skip-verifier]' && git push"
else
  git checkout -- F1/Assets/UI/Fonts 2>/dev/null || true   # 폰트 SDF 는 Play 마다 바뀜
  for i in 1 2 3; do [ -f .git/index.lock ] && { say "index.lock 대기"; sleep 10; } || break; done
  git add -A F1/Assets docs/qa
  if git diff --cached --quiet; then say "변경 없음(커밋 없음)"
  else
    git commit -q -m "task: $NAME [skip-verifier]" && COMMIT=$(git rev-parse --short HEAD) && say "커밋 $COMMIT"
    git push -q 2>&1 | tail -1 || say "push 실패(나중에 다시)"
  fi
fi

# 8. NOW.md 「도현 판정 대기」 한 줄 + 텔레그램 1번 + task.log 한 줄
MIN=$(( ($(date +%s) - T0) / 60 ))
LINE="- $(date '+%m/%d %H:%M') $NAME — $RESULT${COMMIT:+ $COMMIT}${FAILS:+ (${FAILS[*]})} · ${MIN}분 · 보고 $(ls -t docs/team/reports/build-*.md 2>/dev/null | head -1)"
if [ -f "$NOW" ]; then
  python -X utf8 - "$NOW" "$LINE" <<'EOF'
import sys,re
p,line=sys.argv[1],sys.argv[2]
t=open(p,encoding="utf-8").read()
m=re.search(r"(## 도현 판정 대기\n)(.*?)(?=\n## |\Z)",t,re.S)
if m:
    body=m.group(2).rstrip("\n")
    body="" if body.strip() in ("(없음)","") else body
    t=t[:m.start()]+m.group(1)+(body+"\n" if body else "")+line+"\n"+t[m.end():]
else:
    t=t.rstrip("\n")+"\n\n## 도현 판정 대기\n"+line+"\n"
open(p,"w",encoding="utf-8").write(t)
EOF
fi
MSG="$([ "$RESULT" = ok ] && echo '✅' || echo '⚠️') $NAME — $RESULT${COMMIT:+ $COMMIT}${FAILS:+ (${FAILS[*]})} · ${MIN}분"
if [ "$DRY" = 1 ]; then echo "[DRY] tg_notify.py \"$MSG\""; else python -X utf8 scripts/tg_notify.py "$MSG" | tail -1; fi
printf '%s\t%s\t%s\t%s\t%s분\n' "$(date '+%Y-%m-%d %H:%M')" "$NAME" "$RESULT" "${COMMIT:--}" "$MIN" >> "$LOG"
say "끝: $RESULT"
[ "$RESULT" = ok ]
