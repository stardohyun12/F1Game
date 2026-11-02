"""frames/<P>_<test>_NN.png → sheet_<test>.png : 줄 = 프리셋 A/B/C, 칸 = 0.2초 간격 12장 (같은 입력·같은 카메라)."""
import os
from PIL import Image, ImageDraw
here = os.path.dirname(os.path.abspath(__file__))
fr = os.path.join(here, "frames")
for test in ("brake", "turn35", "bump"):
    rows = []
    for p in "ABC":
        imgs = [os.path.join(fr, f"{p}_{test}_{i:02d}.png") for i in range(0, 24, 2)]
        imgs = [f for f in imgs if os.path.exists(f)]
        if imgs: rows.append((p, imgs))
    if not rows: continue
    w, h = 240, 135
    sheet = Image.new("RGB", (40 + w * 12, h * len(rows)), (20, 20, 20))
    d = ImageDraw.Draw(sheet)
    for r, (p, imgs) in enumerate(rows):
        d.text((12, r * h + h // 2), p, fill=(255, 255, 255))
        for c, f in enumerate(imgs):
            sheet.paste(Image.open(f).convert("RGB").resize((w, h)), (40 + c * w, r * h))
    sheet.save(os.path.join(here, f"sheet_{test}.png"))
    print("sheet", test, [p for p, _ in rows])
