"""python strip.py <prefix> → raw/<prefix>_y-30,_y0,_y35 캡처 후 raw/_<prefix>_strip.png"""
import subprocess, sys, pathlib
from PIL import Image
here = pathlib.Path(__file__).parent
raw = here.parents[2] / "qa/evidence/sunset7/raw"
p = sys.argv[1]
for y in (-30, 0, 35):
    subprocess.run([sys.executable, str(here / "run.py"), "cap.cs", f"TAG={p}_y{y}", f"YAW={y}", "V6=0"])
ims = [Image.open(raw / f"{p}_y{y}.png") for y in (-30, 0, 35)]
s = Image.new("RGB", (sum(i.width for i in ims) + 8, ims[0].height), (40, 40, 40)); x = 0
for i in ims: s.paste(i, (x, 0)); x += i.width + 4
s.save(raw / f"_{p}_strip.png")
