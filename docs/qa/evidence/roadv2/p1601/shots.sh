#!/usr/bin/env bash
# usage: shots.sh <tag>  → 세 코멘트 카메라로 찍고 p1601/<tag>_<id>.png 로 복사
cd "$(dirname "$0")/../../../../.."
TAG="${1:-x}"
for i in 230810-268e 230939-4143 123628-4100; do
  bash scripts/fb_shot.sh "$i" 2>&1 | tail -1
  f=$(ls tools/viewer/feedback/shots/*"$i"_after.png | head -1)
  cp "$f" "docs/qa/evidence/roadv2/p1601/${TAG}_${i#*-}.png"
done
