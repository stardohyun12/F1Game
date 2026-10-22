#!/usr/bin/env bash
# 4차: QA 최신 도구(docs/qa/evidence/spline-third 의 추출 스크립트 + docs/qa/tools/*.py)를 그대로 쓰고,
# 씬만 TownGen_Spline_v4.unity, 출력만 tools/town-gen/out/qa_v4 로 바꾼 사본. QA 폴더·도현이 보는 TownGen_Spline.unity 는 건드리지 않는다.
set -eu
cd "$(dirname "$0")/../../.."
OUT=tools/town-gen/out/qa_v4
for f in spline-export spline-core-check spline-house-base; do bash scripts/ucli.sh exec "$(cat tools/town-gen/qa_mirror/v4/$f.cs.txt)" > "$OUT/$f.exec.txt"; done
bash scripts/ucli.sh exec 'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ComplexTrack.unity"); return "back";' > /dev/null
cd docs/qa/tools
py -X utf8 spline-measure.py --input-dir "../../../$OUT" --output-dir "../../../$OUT" > "../../../$OUT/measure-output.txt"
for t in spline-disc-grade spline-radius-audit spline-third-extra spline-osm-distance spline-own-contact; do py -X utf8 $t.py "../../../$OUT" > "../../../$OUT/$t-output.txt" 2>&1 || echo "$t rc=$?"; done
