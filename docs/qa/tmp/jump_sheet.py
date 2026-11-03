"""점프 연속 프레임 시트: 4곳(행) × 6프레임(열) → docs/qa/evidence/jump/sheet.png"""
import os, sys
from PIL import Image, ImageDraw

d = "D:/Github/F1Game/docs/qa/evidence/jump/frames"
rows = [("flat_walk", "평지"), ("slope_walk", "비탈"), ("stairs_walk", "계단(집 앞)"), ("pier_walk", "부두")]
cw = 480
imgs = []
for key, _ in rows:
    r = []
    for i in range(6):
        f = os.path.join(d, f"{key}_{i:02d}.png")
        r.append(Image.open(f).convert("RGB") if os.path.exists(f) else None)
    imgs.append(r)
ref = next(im for r in imgs for im in r if im is not None)
ch = int(ref.height * cw / ref.width)
W, H = cw * 6, ch * len(rows)
sheet = Image.new("RGB", (W, H), (20, 20, 20))
dr = ImageDraw.Draw(sheet)
for y, (r, (_, label)) in enumerate(zip(imgs, rows)):
    for x, im in enumerate(r):
        if im is None:
            continue
        sheet.paste(im.resize((cw, ch), Image.NEAREST), (x * cw, y * ch))
        dr.text((x * cw + 6, y * ch + 4), f"{label} #{x}", fill=(255, 255, 0))
sheet.save("D:/Github/F1Game/docs/qa/evidence/jump/sheet.png")
print(sheet.size)
