#!/usr/bin/env bash
# 1b: 리컴파일 → RoadNetV2Build → fb_geom 7건 요약. 사용: bash run.sh <태그>
cd /d/Github/F1Game
TAG=${1:-x}; OUT=docs/qa/tmp/roadv2b/$TAG; mkdir -p $OUT
bash scripts/ucli-http.sh exec 'AssetDatabase.Refresh(); return "ok";' >/dev/null 2>&1
for i in $(seq 1 60); do
  r=$(bash scripts/ucli-http.sh exec 'return (EditorApplication.isCompiling || EditorApplication.isUpdating) ? "busy" : "idle";' 2>/dev/null)
  if echo "$r" | grep -q idle; then break; fi
  sleep 3
done
bash scripts/ucli-http.sh exec 'return BuildBayLoop.RoadNetV2Build(150,-130,330,-20,15);' > $OUT/build.txt 2>&1
grep -E "검사|그래프|지형 정점|겹침 위치|rror" $OUT/build.txt | head -8
for id in 230810-268e 230823-7da7 230848-453e 230939-4143 231007-66ad 231130-94a3 032740-45c3; do
  python -X utf8 scripts/fb_geom.py $id --radius 40 > $OUT/$id.txt 2>&1
  echo "$id: $(grep -m1 -E 'PASS|FAIL' $OUT/$id.txt)"
done
