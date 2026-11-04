# 낮·노을·밤 세 장을 같은 카메라로 찍고 잰다. 사용: bash docs/qa/tmp/rli/three.sh <폴더(리포 기준)> <접두어>
D=$1; P=$2
for t in "day 0 0" "dusk 1 0" "night 0 1"; do
  set -- $t
  printf '%s %s' "$2" "$3" > docs/qa/tmp/rli/set.txt
  bash scripts/ucli-http.sh exec @docs/qa/tmp/rli/set.cs
  printf '%s/%s%s.png' "$D" "$P" "$1" > docs/qa/tmp/rli/shot.txt
  bash scripts/ucli-http.sh exec @docs/qa/tmp/rli/shot.cs
done
python -X utf8 docs/qa/tmp/rli/measure.py "$D/${P}day.png" "$D/${P}dusk.png" "$D/${P}night.png"
