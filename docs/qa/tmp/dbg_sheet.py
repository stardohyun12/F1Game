import sys, os, glob
from PIL import Image, ImageDraw, ImageFont
d = "D:/Github/F1Game/docs/qa/evidence/debug-window/"
names = sys.argv[2:]
out = sys.argv[1]
W, H = 640, 360
cols = 3
rows = (len(names) + cols - 1) // cols
sheet = Image.new("RGB", (cols * W, rows * (H + 22)), (20, 20, 20))
dr = ImageDraw.Draw(sheet)
try:
    f = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 16)
except Exception:
    f = None
for i, n in enumerate(names):
    p = d + n + ".png"
    if not os.path.exists(p):
        continue
    im = Image.open(p).convert("RGB").resize((W, H), Image.LANCZOS)
    x, y = (i % cols) * W, (i // cols) * (H + 22)
    sheet.paste(im, (x, y + 22))
    dr.text((x + 6, y + 2), n, fill=(255, 255, 255), font=f)
sheet.save(d + out)
print("saved", out, sheet.size)
