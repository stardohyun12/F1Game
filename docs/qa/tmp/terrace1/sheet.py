from PIL import Image, ImageDraw
import os
root = "docs/qa/evidence/terrace1"
names = ["f1_high", "f2_eye", "f3_top", "f4_cart", "f5_edge", "c1_cart", "c2_cart", "c3_cart", "t1_top"]
W, H = 427, 240  # 1/3 of 1280x720
sheet = Image.new("RGB", (W * len(names), H * 2 + 20), (20, 20, 20))
d = ImageDraw.Draw(sheet)
for i, n in enumerate(names):
    for row, tag in enumerate(["before", "after"]):
        p = os.path.join(root, tag, n + ".png")
        if not os.path.exists(p):
            continue
        im = Image.open(p).convert("RGB").resize((W, H), Image.LANCZOS)
        y = row * (H + 20)
        sheet.paste(im, (i * W, y))
    d.text((i * W + 4, H + 4), n + "  (위 before / 아래 after)", fill=(230, 230, 230))
sheet.save(os.path.join(root, "sheet.png"))
print(sheet.size)
