"""look-global 실행기: python run.py <script.cs> KEY=VAL ... → 자리표시 __KEY__ 를 바꿔 ucli exec (sunset9/run.py 사본)."""
import subprocess, sys, pathlib, re
here = pathlib.Path(__file__).parent
src = (here / sys.argv[1]).read_text(encoding="utf-8")
for kv in sys.argv[2:]:
    k, v = kv.split("=", 1)
    src = src.replace(f"__{k}__", v)
left = re.findall(r"__[A-Z0-9]+__", src)
if left:
    sys.exit("unfilled: " + ",".join(sorted(set(left))))
(here / "_run.cs").write_text(src, encoding="utf-8")
r = subprocess.run(["bash", "scripts/ucli-http.sh", "exec", src], cwd=here.parents[3], capture_output=True, text=True, encoding="utf-8")
print(r.stdout.strip(), r.stderr.strip()[-600:])
