"""원본 노선(town-v6b.json) 또는 덤프 JSON 을 위에서 본 그림. usage: plot_src.py out.png [dump.json]"""
import json, math, sys
from PIL import Image, ImageDraw
x0, x1, z0, z1 = 215, 310, -125, -25
S = 9  # px per m
W, H = int((x1 - x0) * S), int((z1 - z0) * S)
img = Image.new('RGB', (W, H), (235, 240, 225)); dr = ImageDraw.Draw(img)
def P(x, z): return ((x - x0) * S, (z1 - z) * S)
for gx in range(x0 - x0 % 10 + 10, x1, 10): dr.line([P(gx, z0), P(gx, z1)], fill=(210, 210, 210)); dr.text(P(gx + .3, z1 - 1), str(gx), fill=(120, 120, 120))
for gz in range(z0 - z0 % 10 + 10, z1, 10): dr.line([P(x0, gz), P(x1, gz)], fill=(210, 210, 210)); dr.text(P(x0 + .3, gz + 1.5), str(gz), fill=(120, 120, 120))
col = {'main': (90, 90, 90), 'village': (215, 170, 60), 'alley': (150, 200, 150), 'stairs': (150, 150, 240)}
roads = json.load(open('docs/map/town-v6b.json', encoding='utf-8'))['roads']
if len(sys.argv) > 2:
    roads = json.load(open(sys.argv[2], encoding='utf-8'))
for r in roads:
    k = r['knots']
    if not any(x0 <= q[0] <= x1 and z0 <= q[1] <= z1 for q in k): continue
    pts = [P(q[0], q[1]) for q in k]
    dr.line(pts, fill=col.get(r.get('kind'), (220, 80, 80)), width=max(1, int(r['width'] * S)), joint='curve')
for r in roads:
    k = r['knots']
    if not any(x0 <= q[0] <= x1 and z0 <= q[1] <= z1 for q in k): continue
    pts = [P(q[0], q[1]) for q in k]
    dr.line(pts, fill=(0, 0, 0), width=1)
    for q in pts: dr.ellipse([q[0] - 1.5, q[1] - 1.5, q[0] + 1.5, q[1] + 1.5], fill=(0, 0, 0))
    m = k[len(k) // 2]
    dr.text(P(m[0], m[1]), r['id'].replace('camo_', 'c').replace('ridge_up_', 'R') + ' y%.0f' % m[2], fill=(150, 0, 0))
    dr.text(P(k[0][0], k[0][1]), 'S%.1f' % k[0][2], fill=(0, 0, 200)); dr.text(P(k[-1][0], k[-1][1]), 'E%.1f' % k[-1][2], fill=(200, 0, 200))
for n, p in {'268e': (277.2, -66.2), '4143': (246.7, -54.6), '4100': (264.9, -56.8)}.items():
    q = P(*p); dr.ellipse([q[0] - 6, q[1] - 6, q[0] + 6, q[1] + 6], outline=(255, 0, 0), width=3); dr.text((q[0] + 8, q[1] - 6), n, fill=(255, 0, 0))
img.save(sys.argv[1])
