#!/usr/bin/env bash
# QA 측정(docs/qa/tools/spline-*) 사본 — 방법은 그대로, 출력만 tools/town-gen/out/qa_mirror 로 (QA 폴더를 덮지 않음).
set -eu
cd "$(dirname "$0")/../../.."
for f in spline-export spline-core-check spline-house-base; do bash scripts/ucli.sh exec "$(cat tools/town-gen/qa_mirror/$f.cs.txt)" > /dev/null; done
bash scripts/ucli.sh exec 'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ComplexTrack.unity"); return "back";' > /dev/null
py -X utf8 tools/town-gen/qa_mirror/spline-measure-v3.py --input-dir tools/town-gen/out/qa_mirror --output-dir tools/town-gen/out/qa_mirror_v3 > /dev/null
