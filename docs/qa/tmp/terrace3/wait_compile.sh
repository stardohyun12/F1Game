#!/bin/bash
# terrace3: Refresh + 컴파일 끝날 때까지 대기 (ready.cs 가 False|True 를 낼 때까지)
bash scripts/ucli-http.sh exec @docs/qa/tmp/terrace2/refresh.cs 2>&1 | tail -2
for i in $(seq 1 24); do
  sleep 5
  r=$(bash scripts/ucli-http.sh exec @docs/qa/tmp/terrace3/ready.cs 2>&1 | tail -1)
  echo "$i $r"
  case "$r" in *"False|True"*) exit 0;; esac
done
exit 1
