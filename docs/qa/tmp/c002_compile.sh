#!/usr/bin/env bash
# C-002: 스크립트 다시 컴파일하고 끝날 때까지 기다린 뒤 에러·새 코드 여부 확인
cd "$(dirname "$0")/../../.." || exit 1
U="bash scripts/ucli-http.sh"
$U exec 'UnityEditor.AssetDatabase.Refresh(); UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation(); return "req";' | tail -1
r=""
for i in $(seq 1 60); do
  sleep 2
  r=$($U exec 'return (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating) ? "busy" : "idle";' 2>/dev/null | tail -1)
  [ "$r" = "idle" ] && break
done
echo "compile: $r"
$U console --type error | tail -5
$U exec 'return typeof(OpeningSequence).GetProperty("Caption") != null ? "new code" : "old code";' | tail -1
