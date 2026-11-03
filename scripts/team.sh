#!/usr/bin/env bash
# 구현 일꾼 호출 (카디널 D-281, docs/CARDINAL.md §5). 보통은 scripts/run_task.sh 가 부른다.
# 사용: bash scripts/team.sh build "<과제 글(브리프)>"   (옛 이름 programmer·feature·mapgen 도 build 로 받음)
#   태그: [EDITOR] 본 에디터 잠금 · [OPUS] 그 과제만 opus · [NOLIMIT] 턴 제한 없음
#   환경: TEAM_MODEL(기본 sonnet) TEAM_EFFORT(high) TEAM_BUDGET_USD(15) TEAM_MAX_TURNS(0=무제한) RUN_DRY=1(claude 대신 echo)
#   세션: .claude/team/sessions/build.id 가 있으면 --resume (run_task.sh 가 같은 과제 파일이면 유지, 다르면 새로 만든다)
# 옛 v1(역할 5개·config.json·대시보드·장부·fb_autofix·체크리스트 첨부)은 archive/harness-v1/scripts/team.sh.
set -u
cd "$(dirname "$0")/.." || exit 1
ROLE="${1:-}"; TASK="${2:-}"
{ [ -z "$ROLE" ] || [ -z "$TASK" ]; } && { echo "usage: team.sh build \"<task>\""; exit 2; }
case "$ROLE" in programmer|feature|mapgen|build) ROLE=build;; *) echo "FAIL: 일꾼은 build 하나다(카디널 §2). 받은 값: $ROLE"; exit 2;; esac
ROLEFILE=".claude/team/roles/$ROLE.md"
[ -f "$ROLEFILE" ] || { echo "FAIL: 역할 파일 없음 $ROLEFILE"; exit 2; }

mkdir -p .claude/team/sessions .ai_logs/team docs/team/reports
C_MODEL="${TEAM_MODEL:-sonnet}"; C_EFFORT="${TEAM_EFFORT:-high}"; C_BUDGET="${TEAM_BUDGET_USD:-15}"; C_TURNS="${TEAM_MAX_TURNS:-0}"
printf '%s' "$TASK" | grep -q '\[OPUS\]' && C_MODEL=opus
SIDFILE=".claude/team/sessions/$ROLE.id"
LIVE=".ai_logs/team/$ROLE.live.log"
RESULT=".ai_logs/team/$ROLE-last.json"
LOCK=".claude/team/editor.lock"
STATE=".claude/team/sessions/$ROLE.state"
COSTFILE=".claude/team/sessions/$ROLE.cost"

HOLD_LOCK=0
if printf '%s' "$TASK" | grep -q '\[EDITOR\]'; then
  # 도현이 Unity 기획 창(PitCrew/기획 창)을 쓰는 동안은 기다린다 — 창이 editor.lock 에 "director <time>" 을 30초마다 씀
  dlock_fresh() { [ -f "$LOCK" ] && grep -q '^director' "$LOCK" && [ $(( $(date +%s) - $(stat -c %Y "$LOCK") )) -lt 60 ]; }
  W=0; while dlock_fresh && [ $W -lt 1800 ]; do [ $W = 0 ] && echo "대기: 도현이 Unity 기획 창 사용 중"; sleep 15; W=$((W+15)); done
  dlock_fresh && { echo "FAIL: 도현 기획 창 30분 넘게 사용 중 — 나중에 다시"; exit 3; }
  [ -f "$LOCK" ] && grep -q '^director' "$LOCK" && rm -f "$LOCK"
  if [ -f "$LOCK" ] && [ "$(cat "$LOCK")" != "$ROLE" ]; then echo "FAIL: 에디터 잠금을 $(cat "$LOCK") 가 갖고 있음 — 끝난 뒤 다시"; exit 3; fi
  echo "$ROLE" > "$LOCK"; HOLD_LOCK=1
fi
cleanup() { [ "$HOLD_LOCK" = 1 ] && rm -f "$LOCK"; echo "idle $(date +%H:%M)" > "$STATE"; }
trap cleanup EXIT

STAMP=$(date +%Y%m%d-%H%M)
REPORT="docs/team/reports/$ROLE-$STAMP.md"
PROMPT="[PD 과제 $STAMP]
$TASK

보고 파일: $REPORT (4줄: 한 것 / 확인 / 증거 / 남은 것 — CARDINAL §4)"
[ -f "$REPORT" ] || printf '# %s %s\n> 하네스가 만든 뼈대. 구현이 채운다.\n## 1. 한 것\n## 2. 확인 방법\n## 3. 검증 증거\n## 4. 남은 이슈\n' "$ROLE" "$STAMP" > "$REPORT"
REPORT_MD5=$(python -c "import hashlib,sys;print(hashlib.md5(open(sys.argv[1],'rb').read()).hexdigest())" "$REPORT")
echo "working $(date +%H:%M) $(printf '%s' "$TASK" | tr '\n' ' ' | head -c 80)" > "$STATE"
printf '%s' "$TASK" > ".claude/team/sessions/$ROLE.task"
printf '\n[%s] %-10s | ===== 새 과제 =====\n[%s] %-10s | 📋 %s\n' "$(date +%H:%M:%S)" "$ROLE" \
  "$(date +%H:%M:%S)" "$ROLE" "$(printf '%s' "$TASK" | tr '\n' ' ' | head -c 200)" >> "$LIVE"

SYSFILE=".claude/team/sessions/$ROLE.sys.md"; cat "$ROLEFILE" > "$SYSFILE"
LESSON=".claude/team/lessons/$ROLE.md"; [ -f "$LESSON" ] && { echo; cat "$LESSON"; } >> "$SYSFILE"
if [ -s "$SIDFILE" ]; then SESS=(--resume "$(cat "$SIDFILE")"); export TEAM_PREV_COST=$(cat "$COSTFILE" 2>/dev/null || echo 0)
else SID=$(python -c "import uuid; print(uuid.uuid4())"); echo "$SID" > "$SIDFILE"; SESS=(--session-id "$SID"); export TEAM_PREV_COST=0; fi
TURNARG=(); if [ "$C_TURNS" != "0" ] && ! printf '%s' "$TASK" | grep -q '\[NOLIMIT\]'; then TURNARG=(--max-turns "$C_TURNS"); fi
# 규칙은 글이 아니라 훅이 막는다(.claude/hooks/guard-pre.py) — --setting-sources user 라 프로젝트 settings.json 이 안 읽히니 --settings 로 붙임
export TEAM_ROLE="$ROLE"
if [ "${RUN_DRY:-0}" = 1 ]; then
  echo "[DRY] claude -p <브리프 $(printf '%s' "$PROMPT" | wc -c)자> ${SESS[*]} --model $C_MODEL --effort $C_EFFORT ${TURNARG[*]:-} --max-budget-usd $C_BUDGET"
  printf '{"role":"%s","ok":true,"final":"[DRY] 실행 안 함","session_id":"%s","cost_usd":0}\n' "$ROLE" "$(cat "$SIDFILE")" > "$RESULT"
else
claude -p "$PROMPT" "${SESS[@]}" \
  --model "$C_MODEL" ${C_EFFORT:+--effort "$C_EFFORT"} \
  --append-system-prompt-file "$SYSFILE" \
  --setting-sources user --settings .claude/team/guard.settings.json \
  --permission-mode acceptEdits \
  --allowedTools "Read" "Edit" "Write" "Glob" "Grep" "TodoWrite" \
     "Bash(bash scripts/ucli-http.sh:*)" "Bash(bash scripts/world_check.sh:*)" "Bash(awk:*)" \
     "Bash(git status:*)" "Bash(git diff:*)" "Bash(git log:*)" "Bash(git checkout:*)" \
     "Bash(ls:*)" "Bash(cat:*)" "Bash(grep:*)" "Bash(sleep:*)" "Bash(python:*)" "Bash(date:*)" "Bash(wc:*)" \
  --disallowedTools "Bash(git commit:*)" "Bash(git push:*)" "Bash(rm -rf:*)" \
  ${TURNARG[@]+"${TURNARG[@]}"} --max-budget-usd "$C_BUDGET" \
  --output-format stream-json --verbose 2>>".ai_logs/team/$ROLE.err" \
  | python scripts/team_log.py "$ROLE" "$LIVE" "$RESULT"
fi

python -c "import json;d=json.load(open('$RESULT',encoding='utf-8'));print(d.get('session_cost_usd') or 0)" 2>/dev/null | tr -d '\r\n' > "$COSTFILE"
python - "$RESULT" <<'EOF'
import json,sys
try: d=json.load(open(sys.argv[1],encoding="utf-8"))
except Exception as e: print("결과 파싱 실패:", e); sys.exit()
c=d.get("cost_usd"); print(f"[{d['role']}] ok={d.get('ok')}" + (f" cost=${c:.2f}" if c else ""))
print((d.get("final") or "")[-1500:])
EOF
# 보고서가 뼈대 그대로면 FAILED-NOREPORT + 마지막 기록·변경 파일을 붙여 둔다(정보 유실 방지)
if [ "${RUN_DRY:-0}" != 1 ] && [ -n "$REPORT_MD5" ] && [ "$(python -c "import hashlib,sys;print(hashlib.md5(open(sys.argv[1],'rb').read()).hexdigest())" "$REPORT")" = "$REPORT_MD5" ]; then
  echo "FAILED-NOREPORT: $REPORT 뼈대 그대로 (턴 한도 추정)"
  { echo; echo "## 하네스 자동 기록 (구현이 보고서를 못 씀)"; echo '```'; awk '/===== 새 과제/{n=NR} {a[NR]=$0} END{for(k=n;k<=NR;k++) print a[k]}' "$LIVE" | grep -a -e "💬" -e "끝:" -e "🔧" | tail -8 | cut -c1-300; echo '```'; echo "변경 파일:"; git status --short | grep -v '^??' | head -20; } >> "$REPORT"
fi
