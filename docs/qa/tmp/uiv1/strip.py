"""strip.py out.png scale cols in1.png in2.png ... — Nearest 로 키워 격자로 붙인다(라벨 = 파일 이름)."""
import sys
from PIL import Image, ImageDraw
out, k, cols = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
ims = [Image.open(p).convert('RGB') for p in sys.argv[4:]]
w, h = ims[0].size
W, H = w * k, h * k
rows = (len(ims) + cols - 1) // cols
sheet = Image.new('RGB', (cols * W + (cols - 1) * 6, rows * (H + 18)), (20, 20, 20))
d = ImageDraw.Draw(sheet)
for i, (im, p) in enumerate(zip(ims, sys.argv[4:])):
    x, y = (i % cols) * (W + 6), (i // cols) * (H + 18)
    sheet.paste(im.resize((W, H), Image.NEAREST), (x, y + 18))
    d.text((x + 4, y + 3), p.replace('\\', '/').split('/')[-1], fill=(255, 255, 255))
sheet.save(out)
print(out, sheet.size)
