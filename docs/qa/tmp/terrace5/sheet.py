# terrace5 시트: 위 = 4차 후(terrace4/after, 8b98 은 도현 원본 스샷), 아래 = 5차 후(terrace5/after). 판정 4컷: 8b98·c1_cart·f1_high·y2_yard_garden.
# 사용: python -X utf8 docs/qa/tmp/terrace5/sheet.py <8b98 4차 png> <8b98 5차 png>
import sys, os
from PIL import Image, ImageDraw
A = "docs/qa/evidence/terrace4/after"; B = "docs/qa/evidence/terrace5/after"; OUT = "docs/qa/evidence/terrace5"
cuts = [("8b98", sys.argv[1], sys.argv[2])] + [(n, f"{A}/{n}.png", f"{B}/{n}.png") for n in ("c1_cart", "f1_high", "y2_yard_garden")]
W, H = 640, 360
sheet = Image.new("RGB", (W * len(cuts), (H + 22) * 2), (20, 20, 20)); d = ImageDraw.Draw(sheet)
os.makedirs(OUT + "/pairs", exist_ok=True)
for i, (n, a, b) in enumerate(cuts):
    ims = [Image.open(p).convert("RGB") for p in (a, b)]
    for row, im in enumerate(ims):
        y = row * (H + 22)
        sheet.paste(im.resize((W, H), Image.LANCZOS), (i * W, y))
        d.text((i * W + 4, y + H + 5), n + ("  4th" if row == 0 else "  5th"), fill=(230, 230, 230))
    pw, ph = 960, 540
    p = Image.new("RGB", (pw, ph * 2 + 4), (255, 0, 255))
    for row, im in enumerate(ims):
        p.paste(im.resize((pw, ph), Image.LANCZOS), (0, row * (ph + 4)))
    p.save(f"{OUT}/pairs/{n}.png")
sheet.save(OUT + "/sheet.png"); print(sheet.size)
