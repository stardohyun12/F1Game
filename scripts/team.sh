#!/usr/bin/env bash
# 상주 팀원에게 과제를 준다 — 역할별 Claude Code 세션을 이어서(--resume) 돌린다.
# 사용: bash scripts/team.sh <programmer|qa> "<과제>"     (과제에 [EDITOR] 가 있으면 에디터 잠금)
# 환경: TEAM_MODEL(기본 sonnet) TEAM_BUDGET_USD(기본 5) TEAM_MAX_TURNS(기본 80)
set -u
cd "$(dirname "$0")/.." || exit 1
ROLE="${1:-}"; TASK="${2:-}"
[ -z "$ROLE" ] || [ -z "$TASK" ] && { echo "usage: team.sh <role> \"<task>\""; exit 2; }
ROLEFILE=".claude/team/roles/$ROLE.md"
[ -f "$ROLEFILE" ] || { echo "FAIL: 역할 파일 없음 $ROLEFILE"; exit 2; }

mkdir -p .claude/team/sessions .ai_logs/team docs/team/reports
SIDFILE=".claude/team/sessions/$ROLE.id"
LOG=".ai_logs/team/$ROLE.log"
LOCK=".claude/team/editor.lock"
MODEL="${TEAM_MODEL:-sonnet}"
BUDGET="${TEAM_BUDGET_USD:-5}"
TURNS="${TEAM_MAX_TURNS:-80}"

# 에디터 잠금
HOLD_LOCK=0
if printf '%s' "$TASK" | grep -q '\[EDITOR\]'; then
  if [ -f "$LOCK" ] && [ "$(cat "$LOCK")" != "$ROLE" ]; then
    echo "FAIL: 에디터 잠금을 $(cat "$LOCK") 가 갖고 있음 — 끝난 뒤 다시"; exit 3
  fi
  echo "$ROLE" > "$LOCK"; HOLD_LOCK=1
fi
cleanup() { [ "$HOLD_LOCK" = 1 ] && rm -f "$LOCK"; }
trap cleanup EXIT

if [ -s "$SIDFILE" ]; then
  SESS=(--resume "$(cat "$SIDFILE")")
else
  SID=$(python -c "import uuid; print(uuid.uuid4())")
  echo "$SID" > "$SIDFILE"
  SESS=(--session-id "$SID")
fi

STAMP=$(date +%Y%m%d-%H%M)
PROMPT="[PD 과제 $STAMP]
$TASK

보고 파일: docs/team/reports/$ROLE-$STAMP.md"

echo "===== $STAMP $ROLE ($MODEL) =====" >> "$LOG"
echo "$TASK" >> "$LOG"

# --setting-sources user: 프로젝트 훅(stop 게이트·창 깜빡임 알림)은 팀원 세션에선 끈다 — 도현 알림 폭주 방지.
claude -p "$PROMPT" "${SESS[@]}" \
  --model "$MODEL" \
  --append-system-prompt-file "$ROLEFILE" \
  --setting-sources user \
  --permission-mode acceptEdits \
  --allowedTools "Read" "Edit" "Write" "Glob" "Grep" "TodoWrite" \
     "Bash(bash scripts/ucli-http.sh:*)" "Bash(bash scripts/regression-check.sh:*)" \
     "Bash(git status:*)" "Bash(git diff:*)" "Bash(git log:*)" "Bash(git checkout -- :*)" \
     "Bash(ls:*)" "Bash(cat:*)" "Bash(grep:*)" "Bash(sleep:*)" "Bash(python:*)" "Bash(date:*)" "Bash(wc:*)" \
  --disallowedTools "Bash(git commit:*)" "Bash(git push:*)" "Bash(rm -rf:*)" \
  --max-turns "$TURNS" --max-budget-usd "$BUDGET" \
  --output-format json > ".ai_logs/team/$ROLE-last.json" 2>> "$LOG"
RC=$?

python - "$ROLE" ".ai_logs/team/$ROLE-last.json" >> "$LOG" <<'EOF'
import json,sys
role,path=sys.argv[1],sys.argv[2]
try: d=json.load(open(path,encoding="utf-8"))
except Exception as e: print(f"[{role}] 결과 파싱 실패: {e}"); sys.exit()
print(f"[{role}] {d.get('subtype')} turns={d.get('num_turns')} cost=${d.get('total_cost_usd',0):.2f}")
print(d.get("result",""))
EOF
tail -n 25 "$LOG"
exit $RC
