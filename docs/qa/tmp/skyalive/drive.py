"""Play 앉은 상태에서: python -I drive.py quick | full | flick | ms
 quick = 조합마다 1장(튜닝용) · full = 조합마다 1초×10장 시트 + 0.1초×30장(3초 GIF) · flick = 새/옛 8초 깜빡임 · ms = 프레임 5초"""
import subprocess, sys, time, pathlib
here = pathlib.Path(__file__).parent
def run(*a):
    r = subprocess.run([sys.executable, str(here / "run.py"), *a], capture_output=True, text=True, encoding="utf-8")
    print(" ", a[0], a[1:], "->", r.stdout.strip()[:300], flush=True)

combos = [(tod, shot, ver) for tod in ("aft", "dusk") for shot in ("seat", "road") for ver in ("old", "new")]
mode = sys.argv[1]
only = sys.argv[2] if len(sys.argv) > 2 else ""
for tod, shot, ver in combos:
    tag = f"{tod}_{shot}_{ver}"
    if only and only not in tag: continue
    if mode == "ms" and shot == "road": continue
    run("mode.cs", f"DUSK={'1' if tod == 'dusk' else '0'}", f"OLD={'true' if ver == 'old' else 'false'}", f"SHOT={shot}", "RX=-10", "RZ=-83", "RP=-14", "RY=90")
    time.sleep(1.5)
    if mode == "quick":
        run("grab.cs", f"P=q_{tag}", "N=1", "IV=0"); time.sleep(1.5)
    elif mode == "full":
        run("grab.cs", f"P=s_{tag}", "N=10", "IV=1"); time.sleep(11)
        run("grab.cs", f"P=g_{tag}", "N=30", "IV=0.1"); time.sleep(4.5)
    elif mode == "flick":
        run("flick.cs", f"TAG={tag}"); time.sleep(11)
    elif mode == "ms":
        run("ms.cs", f"TAG={tod}_{ver}"); time.sleep(7)
