#!/usr/bin/env bash
# 과제 20261102-1543 3단 검사: Play → 도착(HP 지정) → [E] 두드리기 → 인사 → 반응+돈 캡처 → 끝 → Stop
# 사용: bash docs/qa/tmp/handover/run_tier.sh <tag> <hp01> [away]
#   away 면 문 반대쪽을 보고 DebugKnock(받는 사람이 화면 밖 → 시선 이동으로 들어오는지)
set -u
cd "$(dirname "$0")/../../../.."
U="bash scripts/ucli-http.sh"
TAG=$1; HP=$2; AWAY=${3:-}
EV=docs/qa/evidence/handover
mkdir -p "$EV"
TMP=docs/qa/tmp/handover/_arrive_$TAG.cs
sed "s/__HP__/$HP/" docs/qa/tmp/handover/arrive.cs > "$TMP"
shot() { $U exec "ScreenCapture.CaptureScreenshot(\"$(pwd -W 2>/dev/null || pwd)/$EV/$1.png\"); return \"shot $1\";"; }
$U editor play --wait
sleep 6
$U exec 'Application.runInBackground = true; return "rib";'
echo "arrive: $($U exec @$TMP)"
sleep 1.5
echo "s1: $($U exec @docs/qa/tmp/handover/state.cs)"
if [ "$AWAY" = "away" ]; then
  $U exec 'var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>(); d.Player.Cam.Yaw = 0f; return "look away";'
  sleep 0.5
  echo "s_away: $($U exec @docs/qa/tmp/handover/state.cs)"
  shot "${TAG}_0_away"
  echo "knock: $($U exec 'return "knock=" + UnityEngine.Object.FindFirstObjectByType<OnFootDirector>().DebugKnock();')"
else
  shot "${TAG}_0_prompt"
  echo "knock: $($U exec 'var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>(); if (d.Prompt != "E 두드리기") return "no prompt: " + d.Prompt; d.SimInteract(); return "knocked";')"
fi
sleep 0.5
echo "s2: $($U exec @docs/qa/tmp/handover/state.cs)"
sleep 1.6
echo "s3: $($U exec @docs/qa/tmp/handover/state.cs)"
shot "${TAG}_1_greet"
sleep 0.6
$U exec 'UnityEngine.Object.FindFirstObjectByType<OnFootDirector>().SimInteract(); return "next";'
sleep 0.7
echo "s4: $($U exec @docs/qa/tmp/handover/state.cs)"
shot "${TAG}_2_reaction_money"
sleep 0.6
$U exec 'UnityEngine.Object.FindFirstObjectByType<OnFootDirector>().SimInteract(); return "end";'
sleep 2.0
echo "s5: $($U exec @docs/qa/tmp/handover/state.cs)"
shot "${TAG}_3_after"
sleep 0.5
echo "errors: $($U console --type error)"
$U editor stop
rm -f "$TMP"
