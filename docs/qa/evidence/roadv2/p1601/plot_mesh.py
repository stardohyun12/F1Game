"""dump_area 결과(RoadsV2 메시)를 위에서 + 원본 노선 중심선. usage: plot_mesh.py dump.json out.png"""
import json, sys, hashlib
from PIL import Image, ImageDraw
x0, x1, z0, z1 = [float(a) for a in sys.argv[3:7]] if len(sys.argv) > 6 else (215, 310, -125, -25)
S = float(sys.argv[7]) if len(sys.argv) > 7 else 9
W, H = int((x1 - x0) * S), int((z1 - z0) * S)
img = Image.new('RGB', (W, H), (235, 240, 225)); dr = ImageDraw.Draw(img, 'RGBA')
def P(x, z): return ((x - x0) * S, (z1 - z) * S)
for gx in range(int(x0) // 10 * 10, int(x1), 10): dr.line([P(gx, z0), P(gx, z1)], fill=(210, 210, 210)); dr.text(P(gx + .3, z1 - 1), str(gx), fill=(120, 120, 120))
for gz in range(int(z0) // 10 * 10, int(z1), 10): dr.line([P(x0, gz), P(x1, gz)], fill=(210, 210, 210)); dr.text(P(x0 + .3, gz + 1.5), str(gz), fill=(120, 120, 120))
rows = json.load(open(sys.argv[1], encoding='utf-8'))
if isinstance(rows, str): rows = json.loads(rows)
for r in rows:
    v = r['v']; t = r['t']
    dark = 'Main' in r['mat'] or 'main' in r['mat'].lower() or 'Asphalt' in r['mat']
    h = int(hashlib.md5(r['name'].encode()).hexdigest()[:6], 16)
    base = (70, 70, 80) if dark else (200, 180, 120)
    col = tuple(min(255, max(0, base[i] + ((h >> (i * 8)) & 63) - 32)) for i in range(3)) + (200,)
    for i in range(0, len(t), 3):
        a, b, c = v[t[i]], v[t[i + 1]], v[t[i + 2]]
        dr.polygon([P(a[0], a[2]), P(b[0], b[2]), P(c[0], c[2])], fill=col)
    cx = sum(q[0] for q in v) / len(v); cz = sum(q[2] for q in v) / len(v)
    dr.text(P(cx, cz), r['name'].replace('ROAD_V2', '').replace('camo_', 'c').replace('ridge_up_', 'R')[:30] + ' ' + r['mat'][:10], fill=(160, 0, 0))
src = json.load(open('docs/map/town-v6b.json', encoding='utf-8'))['roads']
for r in src:
    k = r['knots']
    if not any(x0 <= q[0] <= x1 and z0 <= q[1] <= z1 for q in k): continue
    dr.line([P(q[0], q[1]) for q in k], fill=(0, 0, 255, 160), width=1)
for n, p in {'268e': (277.2, -66.2), '4143': (246.7, -54.6), '4100': (264.9, -56.8)}.items():
    q = P(*p); dr.ellipse([q[0] - 6, q[1] - 6, q[0] + 6, q[1] + 6], outline=(255, 0, 0), width=3); dr.text((q[0] + 8, q[1] - 6), n, fill=(255, 0, 0))
img.save(sys.argv[2])
print('objects', len(rows), sorted(set(r['mat'] for r in rows)))
