"""pg1501: .cs 고친 뒤 새로고침 → 컴파일 끝 대기 → 실패면 멈춤 → 맵 전체 v2 빌드.

  python -X utf8 tools/art/bin/roadv2_rebuild.py <로그 경로> [--no-build]
"""
import pathlib, sys, time
sys.path.insert(0, str(pathlib.Path(__file__).parent))
from roadv2_codex import request, ROOT

out = pathlib.Path(sys.argv[1])
request("exec", {"code": 'UnityEditor.AssetDatabase.Refresh(); return "ok";'})
time.sleep(6)
for _ in range(120):
    try:
        r = request("exec", {"code": 'return (UnityEditor.EditorApplication.isCompiling||UnityEditor.EditorApplication.isUpdating) ? "busy" : (UnityEditor.EditorUtility.scriptCompilationFailed ? "failed" : "ok");'})
    except Exception:
        r = "busy"
    if r == "ok":
        break
    if r == "failed":
        print("COMPILE FAILED"); print(request("console", {"type": "error", "lines": 20, "stacktrace": "none"})); sys.exit(1)
    time.sleep(2)
if "--no-build" in sys.argv:
    print("compiled"); sys.exit(0)
t = time.time()
log = request("exec", {"code": (ROOT / "tools/art/bin/roadv2_build.cs.txt").read_text(encoding="utf-8")})
out.parent.mkdir(parents=True, exist_ok=True); out.write_text(log, encoding="utf-8")
print(f"build {time.time() - t:.0f}s → {out}")
for l in log.splitlines():
    if any(k in l for k in ("검사:", "흡수", "6 m 초과", "입구 재검사", "그래프:")):
        print(l[:220])
