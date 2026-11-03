#!/bin/bash
# 과제 1250: 열린 길 코멘트 8건 fb_geom --radius 40
cd /d/Github/F1Game
for i in 45c3 268e 7da7 453e 4143 66ad 4100 0c17; do
  echo "== $i"
  python -X utf8 scripts/fb_geom.py "$i" --radius 40 > "docs/qa/tmp/fbgeom_1250_$i.txt" 2>&1
  echo "exit $?"
  tail -n 12 "docs/qa/tmp/fbgeom_1250_$i.txt"
done
