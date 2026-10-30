import sys, glob, os
from PIL import Image, ImageDraw
d = sys.argv[1]
W, H = (int(sys.argv[2]), int(sys.argv[3])) if len(sys.argv) > 3 else (320, 180)
fs = sorted(f for f in glob.glob(d + "/*_with.png"))
cols = 4
rows = (len(fs) + cols - 1) // cols
sheet = Image.new("RGB", (W * cols, H * rows), (20, 20, 20))
dr = ImageDraw.Draw(sheet)
for i, f in enumerate(fs):
    im = Image.open(f).convert("RGB").resize((W, H), Image.LANCZOS)
    x, y = (i % cols) * W, (i // cols) * H
    sheet.paste(im, (x, y))
    dr.rectangle([x, y, x + 60, y + 14], fill=(0, 0, 0))
    dr.text((x + 3, y + 1), os.path.basename(f).replace("_with.png", ""), fill=(255, 255, 0))
sheet.save(d + "/cand_sheet.png")
print(sheet.size)
