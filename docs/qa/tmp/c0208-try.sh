#!/bin/sh
# usage: sh docs/qa/tmp/c0208-try.sh A 1.2,1.2,1.2 0.65
echo "$1 $2 $3" > docs/qa/tmp/c0208-set.txt
bash scripts/ucli-http.sh exec @docs/qa/tmp/c0208-set.cs >/dev/null
TAG=$(bash scripts/ucli-http.sh exec @docs/qa/tmp/c0208-tint.cs)
echo "$TAG"
CAP=$(bash scripts/ucli-http.sh exec @docs/qa/tmp/c0208-cap.cs)
T=$(echo "$CAP" | sed -n 's/^cap \([^ ]*\) .*/\1/p')
E=docs/qa/evidence/car-look-v1
R=art/_review/2026-10-25-car-candidates
echo "-- threequarter"
python -X utf8 -W ignore docs/qa/tmp/c0208_metric.py $R/$1_threequarter.png $E/${T}_threequarter.png $E/${T}_threequarter_mask.png | grep -v "^all"
echo "-- overlay"
python -X utf8 -W ignore docs/qa/tmp/c0208_metric.py $R/$1_overlay.png $E/${T}_overlay.png $E/${T}_overlay_mask.png
