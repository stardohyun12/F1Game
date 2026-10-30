#!/usr/bin/env bash
# pg1501: .cs 고친 뒤 새로고침 → 컴파일 끝 대기 → 컴파일 실패면 멈춤 → 맵 전체 v2 빌드. 로그 = $1
set -e
cd "$(dirname "$0")/../../.."
OUT=${1:?log path}
PY="python -X utf8 tools/art/bin/roadv2_codex.py"
$PY --command refresh --params '{"force":false}' >/dev/null
sleep 6
for i in $(seq 1 90); do
  r=$($PY --code 'return (UnityEditor.EditorApplication.isCompiling||UnityEditor.EditorApplication.isUpdating) ? "busy" : (UnityEditor.EditorUtility.scriptCompilationFailed ? "failed" : "ok");' 2>/dev/null || echo busy)
  [ "$r" = "ok" ] && break
  [ "$r" = "failed" ] && { echo "COMPILE FAILED"; bash scripts/ucli-http.sh console --type error | head -20; exit 1; }
  sleep 2
done
$PY --file tools/art/bin/roadv2_build.cs.txt --out "$OUT" >/dev/null
grep -E "검사:|흡수|6 m 초과|입구 재검사|그래프:" "$OUT"
