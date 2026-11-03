"""실내 도트 값 맞추기: python -I tune.py 이름 QX QY QZ 단수 안개 — 전역값 넣고 같은 카메라로 찍어 잣대 출력"""
import subprocess, sys, time, pathlib
root = pathlib.Path(__file__).resolve().parents[3]
name, qx, qy, qz, lev, fog = sys.argv[1:7]
t = (root / "docs/qa/tmp/tune_set.cs").read_text(encoding="utf-8")
for k, v in (("QX", qx), ("QY", qy), ("QZ", qz), ("LEV", lev), ("FOG", fog)):
    t = t.replace(k, v + "f")
(root / "docs/qa/tmp/_tune_now.cs").write_text(t, encoding="utf-8")
c = (root / "docs/qa/tmp/cap_measure.cs").read_text(encoding="utf-8").replace("NAME", name)
(root / "docs/qa/tmp/_cap_now.cs").write_text(c, encoding="utf-8")
u = ["bash", str(root / "scripts/ucli-http.sh"), "exec"]
print(subprocess.run(u + ["@docs/qa/tmp/_tune_now.cs"], cwd=root, capture_output=True, text=True, encoding="utf-8", errors="replace").stdout.strip().splitlines()[-1])
time.sleep(2.5)
subprocess.run(u + ["@docs/qa/tmp/_tune_now.cs"], cwd=root, capture_output=True, text=True, encoding="utf-8", errors="replace")
time.sleep(0.5)
subprocess.run(u + ["@docs/qa/tmp/_cap_now.cs"], cwd=root, capture_output=True, text=True, encoding="utf-8", errors="replace")
print(subprocess.run([sys.executable, "-I", str(root / "docs/qa/tmp/dotstats.py"), str(root / f"docs/qa/evidence/art-indoor/{name}.png")], capture_output=True, text=True, encoding="utf-8", errors="replace").stdout.strip())
