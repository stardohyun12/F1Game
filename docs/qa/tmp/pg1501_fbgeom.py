import subprocess, sys
for i in ["45c3", "268e", "7da7", "453e", "4143", "66ad", "4100", "0c17"]:
    r = subprocess.run([sys.executable, "-X", "utf8", "scripts/fb_geom.py", i, "--radius", "40"], capture_output=True, text=True, encoding="utf-8")
    out = (r.stdout + r.stderr).strip().splitlines()
    print(f"== {i} rc={r.returncode}")
    print("\n".join(l for l in out if not l.startswith("  ok")))
