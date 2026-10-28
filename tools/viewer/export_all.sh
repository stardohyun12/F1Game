#!/usr/bin/env bash
# FBX(art/export) -> tools/viewer/assets/*.glb + manifest.json. 인자로 이름을 주면 그것만: bash tools/viewer/export_all.sh depot
"${BLENDER:-D:/Tools/Blender/blender.exe}" --background --factory-startup --python "$(cd "$(dirname "$0")" && pwd -W 2>/dev/null || pwd)/export_glb.py" -- "$@"
