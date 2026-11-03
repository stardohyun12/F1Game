#!/usr/bin/env bash
cd /d/Github/F1Game
until [ "$(bash scripts/ucli-http.sh exec @docs/qa/tmp/jump_status.cs | tail -1)" = "DONE" ]; do sleep 4; done
cat docs/qa/evidence/jump/jump.txt
