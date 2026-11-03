# terrace2 시트: 위 = 1차 후(terrace1/after), 아래 = 2차 후(terrace2/after). 컷별 위아래 쌍도 pairs/ 에.
from PIL import Image, ImageDraw
import os
A = "docs/qa/evidence/terrace1/after"; B = "docs/qa/evidence/terrace2/after"; OUT = "docs/qa/evidence/terrace2"
names = ["f1_high", "f2_eye", "f3_top", "f4_cart", "f5_edge", "c1_cart", "c2_cart", "c3_cart", "t1_top"]
W, H = 427, 240
sheet = Image.new("RGB", (W * len(names), H * 2 + 20), (20, 20, 20)); d = ImageDraw.Draw(sheet)
os.makedirs(OUT + "/pairs", exist_ok=True)
for i, n in enumerate(names):
    ims = [Image.open(os.path.join(r, n + ".png")).convert("RGB") for r in (A, B)]
    for row, im in enumerate(ims):
        sheet.paste(im.resize((W, H), Image.LANCZOS), (i * W, row * (H + 20)))
    d.text((i * W + 4, H + 4), n + "  (위 1차 / 아래 2차)", fill=(230, 230, 230))
    pw, ph = 960, 540
    p = Image.new("RGB", (pw, ph * 2 + 4), (255, 0, 255))
    for row, im in enumerate(ims):
        p.paste(im.resize((pw, ph), Image.LANCZOS), (0, row * (ph + 4)))
    p.save(f"{OUT}/pairs/{n}.png")
sheet.save(OUT + "/sheet.png"); print(sheet.size)
