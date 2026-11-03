"""Play: 앉기(psit) 직후 0.25 s 간격 5 s 동안 착수 소리 상태 기록 → evidence/sunset8/splash_log.txt"""
import subprocess, sys, time, pathlib
here = pathlib.Path(__file__).parent
out = here.parents[2] / "qa/evidence/sunset8/splash_log.txt"
out.parent.mkdir(parents=True, exist_ok=True)
lines = []
def run(f):
    r = subprocess.run([sys.executable, str(here / "run.py"), f], capture_output=True, text=True, encoding="utf-8")
    return r.stdout.strip()
lines.append("psit: " + run("psit.cs"))
t0 = time.time()
while time.time() - t0 < 6.0:
    lines.append(f"+{time.time() - t0:4.2f}s " + run("splash.cs"))
out.write_text("\n".join(lines) + "\n", encoding="utf-8")
print("\n".join(lines))
