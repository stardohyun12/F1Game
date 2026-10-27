# 과제 0236: 시점마다 그림자 켬 | 그림자 끔 | 켬+붓질 나란히(각 960×540) + 켬/끔 픽셀 차이(그림자 증거)
from PIL import Image, ImageDraw, ImageChops
import os
d = os.path.join(os.path.dirname(__file__), "../evidence/scene-a7b")
for v in ["house", "street", "garage"]:
    ims = [Image.open(os.path.join(d, f"{v}_{s}.png")).convert("RGB") for s in ("on", "off", "paint")]
    df = ImageChops.difference(ims[0], ims[1]).convert("L"); h = df.histogram(); n = sum(h)
    mean = sum(i * c for i, c in enumerate(h)) / n; big = sum(h[20:]) / n
    out = Image.new("RGB", (2880, 570), (20, 20, 20)); dr = ImageDraw.Draw(out)
    for i, (im, lab) in enumerate(zip(ims, ["shadows ON", "shadows OFF", "shadows ON + painterly"])):
        out.paste(im.resize((960, 540), Image.NEAREST), (i * 960, 30)); dr.text((i * 960 + 10, 8), lab, fill=(255, 255, 255))
    out.save(os.path.join(d, f"{v}_trio.png"))
    print(v, "on-off mean diff %.1f /255, px changed >20: %.1f%%" % (mean, big * 100))
