# 2253: 같은 카메라 3자리 × (2차 | 3차) → docs/qa/evidence/terrain-relief3/pair.png
from PIL import Image, ImageDraw, ImageFont
import os
R = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../.."))
E = os.path.join(R, "docs/qa/evidence/terrain-relief3")
rows = [("sink1031", "도현 사진 자리(Cam_FB_sink)"), ("blockA", "블록 A (115,23)"), ("blockB", "블록 B (112,-47)")]
cols = [("v2", "2차 — 면보다 0.6 m 아래 + 1:4 둑 + ±0.8 기복"), ("v3", "3차 — 길 가장자리 높이로, 기복은 위로만")]
W, H = 960, 648
out = Image.new("RGB", (W * 2, H * 3), (30, 30, 30)); d = ImageDraw.Draw(out)
f = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 18)
for j, (n, rl) in enumerate(rows):
    for i, (t, cl) in enumerate(cols):
        im = Image.open(os.path.join(E, f"{n}_{t}.png")).convert("RGB").resize((W, H), Image.LANCZOS)
        out.paste(im, (i * W, j * H)); lab = f"{rl} · {cl}"
        d.rectangle([i * W, j * H, i * W + 14 + d.textlength(lab, font=f), j * H + 28], fill=(0, 0, 0)); d.text((i * W + 7, j * H + 3), lab, fill=(255, 255, 255), font=f)
out.save(os.path.join(E, "pair.png")); print("ok")
