#!/bin/bash
# 1943: 본선 합성 1바퀴 (Play 중). 결과 SessionState bl.drive
cd /d/Github/F1Game
bash scripts/ucli-http.sh exec 'UnityEditor.SessionState.SetString("bl.road","main"); UnityEditor.SessionState.SetString("bl.drive",""); return "ok";'
bash scripts/ucli-http.sh exec @docs/qa/tmp/bl-drive.cs
for i in $(seq 1 18); do
  python -c "import time;time.sleep(15)"
  r=$(bash scripts/ucli-http.sh exec 'return UnityEditor.SessionState.GetString("bl.drive","");')
  if [ -n "$r" ]; then echo "$r"; exit 0; fi
done
echo "timeout"
