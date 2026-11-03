#!/bin/bash
# (Play 중) 낚시 자리 앉기 → 새 노을(9판 비교) · 새 오후
set -e
cd /d/Github/F1Game
R="python -X utf8 docs/qa/tmp/lookg/run.py"
$R dusk.cs DUSK=1
python -X utf8 docs/qa/tmp/sunset9/run.py psit.cs
sleep 3
$R dusk.cs DUSK=1
$R cap.cs TAG=dusk_sit POSE=cur
$R cap.cs TAG=dusk_sit_b POSE=cur
$R dusk.cs DUSK=0
$R cap.cs TAG=aft_sit POSE=cur
bash scripts/ucli-http.sh console
