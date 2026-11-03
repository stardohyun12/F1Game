# terrace4 시트: 위 = 3차 후(terrace3/after), 아래 = 4차 후(terrace4/after). 같은 카메라 13컷 + 마당 눈높이 2컷(y*, 4차만 맨 아래 줄). pairs/ 에 컷별 위아래 쌍.
# 주의: 3차 컷은 cameraType=SceneView 로 찍혀 f1_high 등에 UIRoot 남색 판이 있다(4차는 Game).
from PIL import Image, ImageDraw
import os
A = "docs/qa/evidence/terrace3/after"; B = "docs/qa/evidence/terrace4/after"; OUT = "docs/qa/evidence/terrace4"
names = ["f1_high", "f2_eye", "f3_top", "f4_cart", "f5_edge", "f5b_edge_now", "c1_cart", "c2_cart", "c3_cart", "t1_top", "w1_gate", "w2_walk", "w3_alley"]
extra = ["y1_yard_door", "y2_yard_garden"]
W, H = 427, 240
cols = 7
bands = (len(names) + cols - 1) // cols
rows = 2 * bands + 1
sheet = Image.new("RGB", (W * cols, (H + 20) * rows), (20, 20, 20)); d = ImageDraw.Draw(sheet)
os.makedirs(OUT + "/pairs", exist_ok=True)
for i, n in enumerate(names):
    ims = [Image.open(os.path.join(r, n + ".png")).convert("RGB") for r in (A, B)]
    c, band = i % cols, i // cols
    for row, im in enumerate(ims):
        y = (band * 2 + row) * (H + 20)
        sheet.paste(im.resize((W, H), Image.LANCZOS), (c * W, y))
        d.text((c * W + 4, y + H + 4), n + ("  3rd" if row == 0 else "  4th"), fill=(230, 230, 230))
    pw, ph = 960, 540
    p = Image.new("RGB", (pw, ph * 2 + 4), (255, 0, 255))
    for row, im in enumerate(ims):
        p.paste(im.resize((pw, ph), Image.LANCZOS), (0, row * (ph + 4)))
    p.save(f"{OUT}/pairs/{n}.png")
y = (rows - 1) * (H + 20)
for i, n in enumerate(extra):
    im = Image.open(os.path.join(B, n + ".png")).convert("RGB")
    sheet.paste(im.resize((W, H), Image.LANCZOS), (i * W, y))
    d.text((i * W + 4, y + H + 4), n + "  4th (yard eye 1.6 m)", fill=(230, 230, 230))
sheet.save(OUT + "/sheet.png"); print(sheet.size)
