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
# 과제에 [OPUS] 가 있으면 그 과제만 opus (기본은 config 의 sonnet — Claude 구독 한도 절약, 10/29)
printf '%s' "$TASK" | grep -q '\[OPUS\]' && C_MODEL=opus
SIDFILE=".claude/team/sessions/$ROLE.id"
LIVE=".ai_logs/team/$ROLE.live.log"
RESULT=".ai_logs/team/$ROLE-last.json"
LOCK=".claude/team/editor.lock"
STATE=".claude/team/sessions/$ROLE.state"

HOLD_LOCK=0
# artlive 는 건물 스튜디오 전용 프로젝트 F1Studio 의 에디터만 쓴다(ucli-http.sh·build_asset.py·studio_cap.sh 가 UCLI_PROJ 를 읽음) → 메인 에디터 잠금 불필요
[ "$ROLE" = "artlive" ] && export UCLI_PROJ="D:/Github/F1Game/F1Studio"
if [ "$ROLE" != "artlive" ] && printf '%s' "$TASK" | grep -q '\[EDITOR\]'; then
  # 도현이 Unity 기획 창(PitCrew/기획 창)을 쓰는 동안은 기다린다 — 창이 .claude/team/editor.lock 에 "director <time>" 을 30초마다 씀
  dlock_fresh() { [ -f "$LOCK" ] && grep -q '^director' "$LOCK" && [ $(( $(date +%s) - $(stat -c %Y "$LOCK") )) -lt 60 ]; }
  W=0; while dlock_fresh && [ $W -lt 1800 ]; do [ $W = 0 ] && echo "대기: 도현이 Unity 기획 창 사용 중"; sleep 15; W=$((W+15)); done
  dlock_fresh && { echo "FAIL: 도현 기획 창 30분 넘게 사용 중 — 나중에 다시"; exit 3; }
  [ -f "$LOCK" ] && grep -q '^director' "$LOCK" && rm -f "$LOCK"   # 60초 넘게 안 갱신된 director 잠금 = 창은 열렸지만 자리 비움
  if [ -f "$LOCK" ] && [ "$(cat "$LOCK")" != "$ROLE" ]; then
    echo "FAIL: 에디터 잠금을 $(cat "$LOCK") 가 갖고 있음 — 끝난 뒤 다시"; exit 3
  fi
  echo "$ROLE" > "$LOCK"; HOLD_LOCK=1
fi
cleanup() { [ "$HOLD_LOCK" = 1 ] && rm -f "$LOCK"; echo "idle $(date +%H:%M)" > "$STATE"
  # Hermes 백그라운드 완료 알림이 텔레그램에 안 와서 직접 알린다 (TEAM_NOTIFY_TO 로 대상 변경, 빈 값이면 끔)
  local to="${TEAM_NOTIFY_TO:-}" # off                 
  [ -n "$to" ] && hermes send -q -t "$to" "🔔 PitCrew $ROLE 끝 ($(date +%H:%M)) — $(tail -1 "$LIVE" 2>/dev/null | cut -c1-200)
'다음' 이라고 하면 PD 가 결과 확인하고 이어서 진행" >/dev/null 2>&1 || true; }
trap cleanup EXIT

STAMP=$(date +%Y%m%d-%H%M)
PROMPT="[PD 과제 $STAMP]
$TASK

보고 파일: docs/team/reports/$ROLE-$STAMP.md"
echo "working $(date +%H:%M) $(printf '%s' "$TASK" | tr '\n' ' ' | head -c 80)" > "$STATE"
printf '%s' "$TASK" > ".claude/team/sessions/$ROLE.task"   # 워치독 자동 재시작이 원문을 쓴다
printf '\n[%s] %-10s | ===== 새 과제 =====\n[%s] %-10s | 📋 %s\n' "$(date +%H:%M:%S)" "$ROLE" \
  "$(date +%H:%M:%S)" "$ROLE" "$(printf '%s' "$TASK" | tr '\n' ' ' | head -c 200)" >> "$LIVE"

if [ "$ROLE" = "art" ] || [ "$ROLE" = "artui" ]; then
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
  EXTRA_TOOLS=""; [ "$ROLE" = "research" ] && EXTRA_TOOLS="WebSearch WebFetch"
  [ "$ROLE" = "artlive" ] && EXTRA_TOOLS="Bash(D:/Tools/Blender/blender.exe:*) Bash(/d/Tools/Blender/blender.exe:*)"   # 라이브 아트 = Blender + 에디터
  [ "$ROLE" = "art2" ] && EXTRA_TOOLS="Bash(D:/Tools/Blender/blender.exe:*) Bash(/d/Tools/Blender/blender.exe:*) Bash(\"D:/Tools/Blender/blender.exe\":*)"   # 아트 2 = Blender 필요
  COSTFILE=".claude/team/sessions/$ROLE.cost"
  # 역할 프롬프트 + 교훈 파일(.claude/team/lessons/<role>.md; art2·artlive 는 art.md)을 합쳐 붙임
  LESSON=".claude/team/lessons/$ROLE.md"; case "$ROLE" in art2|artlive) LESSON=".claude/team/lessons/art.md";; esac
  SYSFILE=".claude/team/sessions/$ROLE.sys.md"; cat "$ROLEFILE" > "$SYSFILE"; if [ -f "$LESSON" ]; then echo >> "$SYSFILE"; cat "$LESSON" >> "$SYSFILE"; fi
  if [ -s "$SIDFILE" ]; then SESS=(--resume "$(cat "$SIDFILE")"); export TEAM_PREV_COST=$(cat "$COSTFILE" 2>/dev/null || echo 0)
  else SID=$(python -c "import uuid; print(uuid.uuid4())"); echo "$SID" > "$SIDFILE"; SESS=(--session-id "$SID"); export TEAM_PREV_COST=0; fi
  # --setting-sources user: 프로젝트 훅(stop 게이트·창 깜빡임 알림)은 팀원 세션에선 끈다 — 도현 알림 폭주 방지.
  claude -p "$PROMPT" "${SESS[@]}" \
    --model "${TEAM_MODEL:-${C_MODEL:-sonnet}}" ${C_EFFORT:+--effort "$C_EFFORT"} \
    --append-system-prompt-file "$SYSFILE" \
    --setting-sources user \
    --permission-mode acceptEdits \
    --allowedTools "Read" "Edit" "Write" "Glob" "Grep" "TodoWrite" \
       "Bash(bash scripts/ucli-http.sh:*)" "Bash(bash scripts/regression-check.sh:*)" "Bash(bash tools/art/bin/check.sh:*)" "Bash(bash tools/art/bin/viewer_export.sh:*)" "Bash(bash tools/art/bin/studio_cap.sh:*)" "Bash(bash tools/art/bin/iter_cap.sh:*)" "Bash(awk:*)" "Bash(bash tools/viewer/export_all.sh:*)" "Bash(D:/Tools/Blender/blender.exe:*)" \
       "Bash(git status:*)" "Bash(git diff:*)" "Bash(git log:*)" "Bash(git checkout:*)" \
       "Bash(ls:*)" "Bash(cat:*)" "Bash(grep:*)" "Bash(sleep:*)" "Bash(python:*)" "Bash(date:*)" "Bash(wc:*)" \
    --disallowedTools "Bash(git commit:*)" "Bash(git push:*)" "Bash(rm -rf:*)" \
    ${EXTRA_TOOLS:+--allowedTools $EXTRA_TOOLS} \
    --max-turns "${TEAM_MAX_TURNS:-${C_TURNS:-80}}" --max-budget-usd "${TEAM_BUDGET_USD:-${C_BUDGET:-5}}" \
    --output-format stream-json --verbose 2>>".ai_logs/team/$ROLE.err" \
    | python scripts/team_log.py "$ROLE" "$LIVE" "$RESULT"
fi

if [ "$ROLE" != "art" ] && [ "$ROLE" != "artui" ]; then python -c "import json;d=json.load(open('$RESULT',encoding='utf-8'));print(d.get('session_cost_usd') or 0)" 2>/dev/null | tr -d '
' > ".claude/team/sessions/$ROLE.cost"; fi
python - "$RESULT" <<'EOF'
import json,sys
try: d=json.load(open(sys.argv[1],encoding="utf-8"))
except Exception as e: print("결과 파싱 실패:", e); sys.exit()
c=d.get("cost_usd"); print(f"[{d['role']}] ok={d.get('ok')}" + (f" cost=${c:.2f}" if c else ""))
print((d.get("final") or "")[-1500:])
EOF
