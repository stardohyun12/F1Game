"""큰길 자리 후보 찍어 보기: python -I tryroad.py "x z pitch yaw" ..."""
import subprocess, sys, time, pathlib
here = pathlib.Path(__file__).parent
for c in sys.argv[1:]:
    x, z, p, y = c.split()
    r = subprocess.run([sys.executable, str(here / "run.py"), "mode.cs", "DUSK=1", "OLD=false", "SHOT=road", f"RX={x}", f"RZ={z}", f"RP={p}", f"RY={y}"], capture_output=True, text=True, encoding="utf-8")
    print(c, "->", r.stdout.strip()[:160], flush=True)
    time.sleep(1.5)
    subprocess.run([sys.executable, str(here / "run.py"), "grab.cs", f"P=r_{x}_{z}_{y}", "N=1", "IV=0"], capture_output=True)
    time.sleep(1.5)
