#!/bin/bash
# programmer-20261030-1314: fb_geom 8건 + fb_shot 3건
cd /d/Github/F1Game
cp docs/qa/tmp/roadv2_2b_build.txt docs/qa/evidence/roadv2/roadv2_full_1314.log
for i in 45c3 268e 7da7 453e 4143 66ad 4100 0c17; do
  echo "== $i"
  python -X utf8 scripts/fb_geom.py $i --radius 40 2>&1 | tail -4
done
mkdir -p docs/qa/evidence/roadv2/full2
for i in 230939-4143 123628-4100 123604-0c17; do
  echo "== shot $i"
  bash scripts/fb_shot.sh $i 2>&1 | tail -3
done
