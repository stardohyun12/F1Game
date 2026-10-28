#!/usr/bin/env bash
# 뷰어용 glb 다시 뽑기(전체 또는 건물 하나). 사용: bash tools/art/bin/viewer_export.sh [room|garage|depot|postoffice]
cd "$(dirname "$0")/../../.." || exit 1
if [ -n "${1:-}" ]; then D:/Tools/Blender/blender.exe --background --factory-startup --python tools/viewer/export_glb.py -- "$1" 2>&1 | grep -E "export_glb|Error" ; else bash tools/viewer/export_all.sh 2>&1 | grep -E "export_glb|Error"; fi
