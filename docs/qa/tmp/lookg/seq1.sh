#!/bin/bash
# Play 진입 → 집 앞·항구·언덕 × 새 오후/새 노을
set -e
cd /d/Github/F1Game
R="python -X utf8 docs/qa/tmp/lookg/run.py"
bash scripts/ucli-http.sh editor play --wait
sleep 4
$R dusk.cs DUSK=0
$R cap.cs TAG=aft_home POSE=cur
$R dusk.cs DUSK=1
$R cap.cs TAG=dusk_home POSE=cur
$R cap.cs TAG=dusk_harbor POSE=300,12,-350,8,20
$R cap.cs TAG=dusk_hill POSE=88,44,50,36.92,135
$R dusk.cs DUSK=0
$R cap.cs TAG=aft_harbor POSE=300,12,-350,8,20
$R cap.cs TAG=aft_hill POSE=88,44,50,36.92,135
bash scripts/ucli-http.sh console
