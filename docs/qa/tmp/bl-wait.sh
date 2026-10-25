#!/usr/bin/env bash
# 컴파일/리로드 끝날 때까지 대기 후 상태·에러 출력
for i in $(seq 1 80); do
  r=$(bash scripts/ucli-http.sh exec @docs/qa/tmp/bl-compiled.cs 2>/dev/null)
  case "$r" in *compiling=False*) break ;; esac
  sleep 3
done
echo "$r"
bash scripts/ucli-http.sh console
