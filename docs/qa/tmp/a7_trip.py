# 과제 2050: 시점마다 A6d | A7 그림자 켬 | A7 그림자 끔 나란히(각 960×540, 위에 이름)
from PIL import Image, ImageDraw
import os
d = os.path.join(os.path.dirname(__file__), "../evidence/scene-a7")
for v in ["house", "street", "garage", "roof"]:
    out = Image.new("RGB", (2880, 570), (20, 20, 20)); dr = ImageDraw.Draw(out)
    for i, (s, lab) in enumerate([("a6d", "A6d baked"), ("a7on", "A7 shadows ON"), ("a7off", "A7 shadows OFF")]):
        im = Image.open(os.path.join(d, f"{v}_{s}.png")).convert("RGB").resize((960, 540), Image.NEAREST)
        out.paste(im, (i * 960, 30)); dr.text((i * 960 + 10, 8), lab, fill=(255, 255, 255))
    out.save(os.path.join(d, f"{v}_trio.png"))
print("ok")
