# 1935: 도현 사진 | 전(Cam_FB_sink) | 후(Cam_FB_sink) 한 줄 + B 블록 전/후 → terrain-sink/sink_pair.png
from PIL import Image, ImageDraw
import os
R = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../.."))
E = os.path.join(R, "docs/qa/evidence")
rows = [[("dohyun/sink_1031.png", "도현 사진"), ("terrain-sink/sink_before.png", "전 Cam_FB_sink"), ("terrain-sink/sink_after.png", "후 Cam_FB_sink")],
        [("terrain-sink/sinkA_wide_before.png", "A 넓게 전"), ("terrain-sink/sinkA_wide_after.png", "A 넓게 후"), None],
        [("terrain-sink/sinkB_before.png", "B (112,-47) 전"), ("terrain-sink/sinkB_after.png", "B 후"), None]]
W, H = 800, 540
out = Image.new("RGB", (W * 3, H * len(rows)), (30, 30, 30))
d = ImageDraw.Draw(out)
from PIL import ImageFont
f = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 16)
for j, row in enumerate(rows):
    for i, c in enumerate(row):
        if not c: continue
        im = Image.open(os.path.join(E, c[0])).convert("RGB"); im.thumbnail((W, H))
        out.paste(im, (i * W, j * H)); d.rectangle([i * W, j * H, i * W + 230, j * H + 22], fill=(0, 0, 0)); d.text((i * W + 6, j * H + 2), c[1], fill=(255, 255, 255), font=f)
out.save(os.path.join(E, "terrain-sink/sink_pair.png"))
print("ok")
