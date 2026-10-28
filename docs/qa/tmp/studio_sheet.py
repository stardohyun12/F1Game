# studio_cap.sh 의 sheet 부분만: python -X utf8 docs/qa/tmp/studio_sheet.py <에셋> <번호> <설명>
import sys, glob, os
from PIL import Image, ImageDraw
a, n, d = sys.argv[1], int(sys.argv[2]), sys.argv[3]
out = f"docs/qa/evidence/studio-{a}"
cams = ["road", "front", "34", "close"]
cur = [os.path.join(out, f"s{n:02d}_{d}_{c}.png") for c in cams]
prev = []
for c in cams:
    g = sorted(glob.glob(os.path.join(out, f"s{n-1:02d}_*_{c}.png"))) if n > 0 else []
    prev.append(g[0] if g else None)
def lab(im, t):
    dr = ImageDraw.Draw(im); dr.rectangle([0, 0, 8 * len(t) + 12, 22], fill=(0, 0, 0)); dr.text((6, 5), t, fill=(255, 255, 255)); return im
W, H = 1280, 720
rows = 2 if any(prev) else 1
sheet = Image.new("RGB", (W * 4, H * rows), (40, 40, 40))
for i, c in enumerate(cams):
    if rows == 2 and prev[i]: sheet.paste(lab(Image.open(prev[i]).convert("RGB"), os.path.basename(prev[i])), (W * i, 0))
    sheet.paste(lab(Image.open(cur[i]).convert("RGB"), os.path.basename(cur[i])), (W * i, H * (rows - 1)))
p = os.path.join(out, f"s{n:02d}_sheet.png"); sheet.save(p); print("sheet", p, sheet.size)
