"""측정 보조: 기준 스크린샷 일부를 잘라 50px 격자(원본 좌표)를 덮어 저장. 좌표를 눈대중이 아니라 격자에서 읽기 위함.
    python -X utf8 tools/asset-gen/opus_grid_crop.py <img> x0 y0 x1 y1 <out.png> [scale]
"""
import sys
from PIL import Image, ImageDraw, ImageFont

src, x0, y0, x1, y1, out = sys.argv[1], *map(int, sys.argv[2:6]), sys.argv[6]
sc = int(sys.argv[7]) if len(sys.argv) > 7 else 1
im = Image.open(src).convert('RGB').crop((x0, y0, x1, y1))
im = im.resize((im.width * sc, im.height * sc), Image.NEAREST)
d = ImageDraw.Draw(im); f = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 13)
for gx in range((x0 // 50 + 1) * 50, x1, 50):
    X = (gx - x0) * sc; d.line([(X, 0), (X, im.height)], fill=(0, 255, 255) if gx % 100 else (255, 0, 255), width=1)
    d.text((X + 2, 2), str(gx), fill=(255, 255, 0), font=f, stroke_width=2, stroke_fill=(0, 0, 0))
for gy in range((y0 // 50 + 1) * 50, y1, 50):
    Y = (gy - y0) * sc; d.line([(0, Y), (im.width, Y)], fill=(0, 255, 255) if gy % 100 else (255, 0, 255), width=1)
    d.text((2, Y + 2), str(gy), fill=(255, 255, 0), font=f, stroke_width=2, stroke_fill=(0, 0, 0))
im.save(out)
