#!/usr/bin/env bash
# roadv2: 7 코멘트 캡처 + geom. 인자 = before|after
cd /d/Github/F1Game
TAG="${1:?before|after}"
mkdir -p docs/qa/evidence/roadv2/$TAG
for id in 230810-268e 230823-7da7 230848-453e 230939-4143 231007-66ad 231130-94a3 032740-45c3; do
  bash scripts/fb_shot.sh "$id" 2>&1 | tail -1
  cp tools/viewer/feedback/shots/unity-Map_TownV6-20261029-${id}_after.png docs/qa/evidence/roadv2/$TAG/$id.png 2>/dev/null
  python -X utf8 scripts/fb_geom.py "$id" --radius 40 > docs/qa/evidence/roadv2/$TAG/$id.geom.txt 2>&1
  echo "$id $(tail -1 docs/qa/evidence/roadv2/$TAG/$id.geom.txt) FAILitems=$(grep -c '  FAIL' docs/qa/evidence/roadv2/$TAG/$id.geom.txt)"
done
