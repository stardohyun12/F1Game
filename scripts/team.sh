#!/usr/bin/env bash
# 상주 팀원에게 과제를 준다 — 역할별 세션을 이어서(--resume) 돌린다.
# 사용: bash scripts/team.sh <programmer|qa|art> "<과제>"   (과제에 [EDITOR] 가 있으면 에디터 잠금)
#   programmer·qa = Claude Code, art = Codex CLI
# 설정: .claude/team/config.json (대시보드에서 편집) — 환경 변수 TEAM_MODEL/TEAM_BUDGET_USD/TEAM_MAX_TURNS 가 있으면 우선
# 보기: bash scripts/team-watch.sh (실시간) / bash scripts/team-status.sh (요약)
set -u
cd "$(dirname "$0")/.." || exit 1
ROLE="${1:-}"; TASK="${2:-}"
{ [ -z "$ROLE" ] || [ -z "$TASK" ]; } && { echo "usage: team.sh <role> \"<task>\""; exit 2; }
ROLEFILE=".claude/team/roles/$ROLE.md"
[ -f "$ROLEFILE" ] || { echo "FAIL: 역할 파일 없음 $ROLEFILE"; exit 2; }

mkdir -p .claude/team/sessions .ai_logs/team docs/team/reports
# 역할별 설정(대시보드에서 편집): .claude/team/config.json — 환경 변수가 있으면 그게 우선.
cfg() { python -c "import json,sys
try: print(json.load(open('.claude/team/config.json',encoding='utf-8')).get('$ROLE',{}).get('$1','') or '')
except Exception: print('')" | tr -d '\r'; }
C_MODEL=$(cfg model); C_EFFORT=$(cfg effort); C_BUDGET=$(cfg budget); C_TURNS=$(cfg turns)
SIDFILE=".claude/team/sessions/$ROLE.id"
LIVE=".ai_logs/team/$ROLE.live.log"
RESULT=".ai_logs/team/$ROLE-last.json"
LOCK=".claude/team/editor.lock"
STATE=".claude/team/sessions/$ROLE.state"

HOLD_LOCK=0
if printf '%s' "$TASK" | grep -q '\[EDITOR\]'; then
  if [ -f "$LOCK" ] && [ "$(cat "$LOCK")" != "$ROLE" ]; then
    echo "FAIL: 에디터 잠금을 $(cat "$LOCK") 가 갖고 있음 — 끝난 뒤 다시"; exit 3
  fi
  echo "$ROLE" > "$LOCK"; HOLD_LOCK=1
fi
cleanup() { [ "$HOLD_LOCK" = 1 ] && rm -f "$LOCK"; echo "idle $(date +%H:%M)" > "$STATE"; }
trap cleanup EXIT

STAMP=$(date +%Y%m%d-%H%M)
PROMPT="[PD 과제 $STAMP]
$TASK

보고 파일: docs/team/reports/$ROLE-$STAMP.md"
echo "working $(date +%H:%M) $(printf '%s' "$TASK" | tr '\n' ' ' | head -c 80)" > "$STATE"
printf '\n[%s] %-10s | ===== 새 과제 =====\n[%s] %-10s | 📋 %s\n' "$(date +%H:%M:%S)" "$ROLE" \
  "$(date +%H:%M:%S)" "$ROLE" "$(printf '%s' "$TASK" | tr '\n' ' ' | head -c 200)" >> "$LIVE"

if [ "$ROLE" = "art" ]; then
  CX=(); [ -n "${C_MODEL:-}" ] && CX+=(-m "$C_MODEL"); [ -n "${C_EFFORT:-}" ] && CX+=(-c "model_reasoning_effort=\"$C_EFFORT\"")
  # ---- Codex (첫 과제에만 역할 파일을 앞에 붙인다; 이후 resume 이 기억) ----
  # workspace-write 샌드박스는 Blender 실행 등을 승인 대기로 막는데 비대화형이라 그대로 실패한다.
  # → danger-full-access + approval never, 통제는 역할 파일 규칙(커밋 금지·승인 전 F1/Assets 금지)으로.
  if [ -s "$SIDFILE" ]; then
    codex exec resume "$(cat "$SIDFILE")" --json --skip-git-repo-check "${CX[@]}" \
      -c sandbox_mode='"danger-full-access"' -c approval_policy='"never"' "$PROMPT" < /dev/null \
      2>>".ai_logs/team/$ROLE.err" | python scripts/team_log.py "$ROLE" "$LIVE" "$RESULT"
  else
    FULL="$(cat "$ROLEFILE")

$PROMPT"
    codex exec --json --skip-git-repo-check "${CX[@]}" -C "$(pwd -W)" -s danger-full-access -c approval_policy='"never"' "$FULL" < /dev/null \
      2>>".ai_logs/team/$ROLE.err" | python scripts/team_log.py "$ROLE" "$LIVE" "$RESULT"
    python -c "import json;print(json.load(open('$RESULT',encoding='utf-8')).get('session_id') or '')" | tr -d '\r' > "$SIDFILE"
  fi
else
  # ---- Claude Code ----
  if [ -s "$SIDFILE" ]; then SESS=(--resume "$(cat "$SIDFILE")")
  else SID=$(python -c "import uuid; print(uuid.uuid4())"); echo "$SID" > "$SIDFILE"; SESS=(--session-id "$SID"); fi
  # --setting-sources user: 프로젝트 훅(stop 게이트·창 깜빡임 알림)은 팀원 세션에선 끈다 — 도현 알림 폭주 방지.
  claude -p "$PROMPT" "${SESS[@]}" \
    --model "${TEAM_MODEL:-${C_MODEL:-sonnet}}" ${C_EFFORT:+--effort "$C_EFFORT"} \
    --append-system-prompt-file "$ROLEFILE" \
    --setting-sources user \
    --permission-mode acceptEdits \
    --allowedTools "Read" "Edit" "Write" "Glob" "Grep" "TodoWrite" \
       "Bash(bash scripts/ucli-http.sh:*)" "Bash(bash scripts/regression-check.sh:*)" \
       "Bash(git status:*)" "Bash(git diff:*)" "Bash(git log:*)" "Bash(git checkout:*)" \
       "Bash(ls:*)" "Bash(cat:*)" "Bash(grep:*)" "Bash(sleep:*)" "Bash(python:*)" "Bash(date:*)" "Bash(wc:*)" \
    --disallowedTools "Bash(git commit:*)" "Bash(git push:*)" "Bash(rm -rf:*)" \
    --max-turns "${TEAM_MAX_TURNS:-${C_TURNS:-80}}" --max-budget-usd "${TEAM_BUDGET_USD:-${C_BUDGET:-5}}" \
    --output-format stream-json --verbose 2>>".ai_logs/team/$ROLE.err" \
    | python scripts/team_log.py "$ROLE" "$LIVE" "$RESULT"
fi

python - "$RESULT" <<'EOF'
import json,sys
try: d=json.load(open(sys.argv[1],encoding="utf-8"))
except Exception as e: print("결과 파싱 실패:", e); sys.exit()
c=d.get("cost_usd"); print(f"[{d['role']}] ok={d.get('ok')}" + (f" cost=${c:.2f}" if c else ""))
print((d.get("final") or "")[-1500:])
EOF
