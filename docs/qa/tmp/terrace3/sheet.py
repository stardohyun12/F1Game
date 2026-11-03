# terrace3 시트: 위 = 2차 후(terrace2/after), 아래 = 3차 후(terrace3/after). 걷기 3컷(w*)은 3차만(아래 줄 오른쪽 끝). pairs/ 에 컷별 위아래 쌍.
from PIL import Image, ImageDraw
import os
A = "docs/qa/evidence/terrace2/after"; B = "docs/qa/evidence/terrace3/after"; OUT = "docs/qa/evidence/terrace3"
names = ["f1_high", "f2_eye", "f3_top", "f4_cart", "f5_edge", "c1_cart", "c2_cart", "c3_cart", "t1_top"]
walks = ["w1_gate", "w2_walk", "w3_alley", "f5b_edge_now"]
W, H = 427, 240
cols = 6
rows = 2 * ((len(names) + cols - 1) // cols) + 1
sheet = Image.new("RGB", (W * cols, (H + 20) * rows), (20, 20, 20)); d = ImageDraw.Draw(sheet)
os.makedirs(OUT + "/pairs", exist_ok=True)
for i, n in enumerate(names):
    ims = [Image.open(os.path.join(r, n + ".png")).convert("RGB") for r in (A, B)]
    c, band = i % cols, i // cols
    for row, im in enumerate(ims):
        y = (band * 2 + row) * (H + 20)
        sheet.paste(im.resize((W, H), Image.LANCZOS), (c * W, y))
        d.text((c * W + 4, y + H + 4), n + ("  2nd" if row == 0 else "  3rd"), fill=(230, 230, 230))
    pw, ph = 960, 540
    p = Image.new("RGB", (pw, ph * 2 + 4), (255, 0, 255))
    for row, im in enumerate(ims):
        p.paste(im.resize((pw, ph), Image.LANCZOS), (0, row * (ph + 4)))
    p.save(f"{OUT}/pairs/{n}.png")
y = (rows - 1) * (H + 20)
for i, n in enumerate(walks):
    im = Image.open(os.path.join(B, n + ".png")).convert("RGB")
    sheet.paste(im.resize((W, H), Image.LANCZOS), (i * W, y))
    d.text((i * W + 4, y + H + 4), n + "  3rd (eye 1.6 m)", fill=(230, 230, 230))
sheet.save(OUT + "/sheet.png"); print(sheet.size)
