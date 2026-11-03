# 흔들기 연속 프레임(<scenario>_shake_NN.png) → 한 장 시트(5열). 사용: python -I packbench_sheet.py <evidence 폴더> <scenario>
import glob, os, sys
from PIL import Image, ImageDraw

d, sc = sys.argv[1], sys.argv[2]
fs = sorted(glob.glob(os.path.join(d, f"{sc}_shake_*.png")))
if not fs:
    sys.exit("no frames")
ims = [Image.open(f).convert("RGB") for f in fs]
w, h = ims[0].size
tw = 320; th = int(h * tw / w)
cols = 5; rows = (len(ims) + cols - 1) // cols
sheet = Image.new("RGB", (cols * tw, rows * th), (0, 0, 0))
dr = ImageDraw.Draw(sheet)
for i, im in enumerate(ims):
    x, y = (i % cols) * tw, (i // cols) * th
    sheet.paste(im.resize((tw, th), Image.NEAREST), (x, y))
    dr.text((x + 4, y + 4), f"{sc} t={i * 0.5:.1f}s", fill=(255, 255, 0))
out = os.path.join(d, f"{sc}_shake_sheet.png")
sheet.save(out)
print(out, len(ims), "frames")
