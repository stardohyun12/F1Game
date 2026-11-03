"""Play 앉은 상태: python pair.py <tag> <yaw> [fz=1] → raw/<tag>_9.png, raw/<tag>_10.png 와 나란히 2배 _ab_<tag>.png"""
import subprocess, sys, time, pathlib
from PIL import Image
here = pathlib.Path(__file__).parent
raw = here.parents[1] / "evidence/sunset10/raw"
tag, yaw = sys.argv[1], sys.argv[2]
fz = sys.argv[3] if len(sys.argv) > 3 else "1"
for v, v9 in (("9", "true"), ("10", "false")):
    subprocess.run([sys.executable, str(here / "run.py"), "snap.cs", f"TAG={tag}_{v}", f"V9={v9}", f"YAW={yaw}", f"FZ={fz}"])
    time.sleep(2)
a, b = Image.open(raw / f"{tag}_9.png"), Image.open(raw / f"{tag}_10.png")
w, h = a.size
im = Image.new("RGB", (w * 2 + 8, h), (255, 255, 255)); im.paste(a, (0, 0)); im.paste(b, (w + 8, 0))
im.resize((im.width * 2, h * 2), Image.NEAREST).save(here / f"_ab_{tag}.png")
print(open(raw / "snap.jsonl", encoding="utf-8").read().strip().splitlines()[-2:])
