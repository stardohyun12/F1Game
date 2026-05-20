#!/usr/bin/env bash
# UserPromptSubmit hook — 도현 피드백(불만) 키워드 감지 (game-craft-upgrade ⑥)
# 도현이 "이거 이상해 / 안 됨 / 왜 이래" 같은 불만을 주면,
# Claude에게 "incident.md에 stub 기록 + 어떤 점검이 있었으면 잡았을지 적어라" 리마인더 주입.
# 목적: 같은 실수 반복 방지 — 피드백을 흘려보내지 않고 패턴으로 누적.

set -euo pipefail

# stdin JSON에서 prompt 추출 (jq 없을 수 있으니 grep fallback)
INPUT=$(cat)
PROMPT=""
if command -v jq >/dev/null 2>&1; then
  PROMPT=$(echo "$INPUT" | jq -r '.prompt // empty' 2>/dev/null || true)
fi
if [ -z "$PROMPT" ]; then
  # fallback: "prompt":"..." 대충 추출
  PROMPT=$(echo "$INPUT" | grep -o '"prompt"[[:space:]]*:[[:space:]]*"[^"]*"' | head -1 || true)
fi

# 불만/버그 키워드 (한글). 너무 흔한 단어는 피함.
KEYWORDS='이상해|이상함|안 ?됨|안 ?돼|안 ?나와|안 ?떠|안 ?보여|왜 ?이래|왜 ?안|버그|깨[졌짐]|망가|먹통|이상하게|어색|박[혀힘]|기울'

if echo "$PROMPT" | grep -qiE "$KEYWORDS"; then
  cat <<'EOF'
{
  "hookSpecificOutput": {
    "hookEventName": "UserPromptSubmit",
    "additionalContext": "[⑥ 피드백 감지] 도현이 문제·불만을 보고한 것으로 보임. fix 진행과 별개로: (1) 옵시디언/하네스/incidents.md에 4줄 stub 기록 (증상 / 추정원인 / 어떤 자동점검이 있었으면 미리 잡았을까(Missing-heuristic) / 패턴번호). (2) 같은 패턴 incident 3건+ 누적이면 룰 즉시 발효 검토 (session.md B트랙). 이건 흘려보내지 말 것 — 자체 발견 못 한 케이스라 가장 값진 학습 재료."
  }
}
EOF
fi

exit 0
