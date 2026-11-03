"""Play 앉은 상태: 12 s 동안 2.4 s 간격 6장(snap.cs, s0..s5) + 상태 JSON → evidence/sunset9/strip.jsonl"""
import subprocess, sys, time, pathlib
here = pathlib.Path(__file__).parent
out = here.parents[2] / "qa/evidence/sunset9/strip.jsonl"
rows = []
t0 = time.time()
for i in range(6):
    while time.time() - t0 < i * 2.4:
        time.sleep(0.05)
    r = subprocess.run([sys.executable, str(here / "run.py"), "snap.cs", f"TAG=s{i}"], capture_output=True, text=True, encoding="utf-8")
    rows.append(r.stdout.strip()); print(rows[-1], flush=True)
out.write_text("\n".join(rows) + "\n", encoding="utf-8")
