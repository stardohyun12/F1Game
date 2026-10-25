#!/bin/bash
# hb.drive 가 채워질 때까지 기다렸다가 결과 + 정산(p-res) 출력
cd D:/Github/F1Game
for i in $(seq 1 40); do
  r=$(bash scripts/ucli-http.sh exec 'return UnityEditor.SessionState.GetString("hb.drive","");' 2>/dev/null)
  if [ -n "$r" ]; then echo "$r"; break; fi
  sleep 4
done
sleep 2
bash scripts/ucli-http.sh exec @docs/qa/tmp/p-res.cs
