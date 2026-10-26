#!/bin/bash
# 2111: 8초 연속 프레임 한 벌 (Play 중). 사용: bash docs/qa/tmp/l5c-run.sh before|after|after360
cd /d/Github/F1Game
U="bash scripts/ucli-http.sh exec"
$U "UnityEditor.SessionState.SetString(\"l5c.tag\",\"$1\"); UnityEditor.SessionState.SetString(\"bl.road\",\"main\"); UnityEditor.SessionState.SetInt(\"bl.cap\", 20); return \"tag $1\";"
$U @docs/qa/tmp/h0404-lapprep.cs
python -c "import time;time.sleep(1.5)"
$U @docs/qa/tmp/l5c-drive.cs
$U @docs/qa/tmp/l5c-frames.cs
for i in $(seq 1 40); do
  python -c "import time;time.sleep(5)"
  r=$($U 'return UnityEditor.SessionState.GetString("l5c.done","");')
  if [ -n "$r" ]; then echo "$r"; break; fi
done
$U 'UnityEditor.SessionState.SetString("l5c.driveId","stop"); var car=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); typeof(ArcadeCarController).GetField("moveInput", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(car, Vector2.zero); return "drive stopped, pos " + car.transform.position;'
