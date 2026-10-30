# terrace7: 위에서 본 그림에 2 m 격자·좌표 글자. 직교 25 px/m, 좌상단 = (218,-40). 인자: 입력 출력 [선 json]
import sys, json
from PIL import Image, ImageDraw
src, dst = sys.argv[1], sys.argv[2]
im = Image.open(src).convert('RGB'); d = ImageDraw.Draw(im)
X0, Z0, S = 218, -40, 25
def px(x, z): return ((x - X0) * S, (Z0 - z) * S)
for x in range(218, 267, 2):
    p = px(x, 0)[0]; d.line([(p, 0), (p, 900)], fill=(255, 255, 255) if x % 10 == 0 else (90, 90, 90), width=1)
    if x % 4 == 0: d.text((p + 2, 2), str(x), fill=(255, 255, 0))
for z in range(-40, -77, -2):
    p = px(0, z)[1]; d.line([(0, p), (1200, p)], fill=(255, 255, 255) if z % 10 == 0 else (90, 90, 90), width=1)
    if z % 4 == 0: d.text((2, p + 2), str(z), fill=(255, 255, 0))
if len(sys.argv) > 3:
    for line in json.load(open(sys.argv[3], encoding='utf-8')):
        pts = [px(p[0], p[1]) for p in line['pts']]
        d.line(pts, fill=tuple(line.get('color', [255, 0, 255])), width=line.get('w', 3))
im.save(dst)
