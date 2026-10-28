#!/usr/bin/env bash
# 묶인 메시 수 검사(읽기만 함). 사용: bash tools/art/bin/bundled.sh <건물>   (도현 "여러 오브젝트 묶지마")
cd "$(dirname "$0")/../../.." || exit 1
case "$1" in
  depot) F=art/export/q3-depot/q3_depot.fbx ;;
  garage) F=art/export/q2-garage/garage_v5_combo.fbx ;;
  postoffice) F=art/export/q4-postoffice/q4_postoffice.fbx ;;
  room) F=art/export/q1-room/room.fbx ;;
  house_home) F=art/export/house-home/house_home.fbx ;;
  *) echo "건물 이름?"; exit 1 ;;
esac
"${BLENDER:-D:/Tools/Blender/blender.exe}" -b --factory-startup -P tools/art/count_bundled.py -- "$F" "docs/qa/tmp/bundled_$1.json" 2>&1 | grep '\[bundled\]'
