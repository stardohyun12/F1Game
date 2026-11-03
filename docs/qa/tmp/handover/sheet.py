# 연속 프레임 시트: 인자 = 출력 png, 입력 png 들(순서대로). 3열, 각 칸 640x360 + 이름 띠.
import sys
from PIL import Image, ImageDraw
out, ins = sys.argv[1], sys.argv[2:]
W, H, B = 640, 360, 22
cols = 3
rows = (len(ins) + cols - 1) // cols
sheet = Image.new("RGB", (W * cols, (H + B) * rows), (20, 20, 20))
d = ImageDraw.Draw(sheet)
for i, p in enumerate(ins):
    im = Image.open(p).convert("RGB").resize((W, H), Image.NEAREST)
    x, y = (i % cols) * W, (i // cols) * (H + B)
    sheet.paste(im, (x, y + B))
    d.text((x + 6, y + 5), f"{i + 1}. {p.replace(chr(92), '/').split('/')[-1]}", fill=(230, 230, 230))
sheet.save(out)
print(out, sheet.size)
