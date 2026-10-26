# 과제 0619 — A2c 측정 + 기준 시트 + A2b|A2c 나란히
import sys; sys.path.insert(0, 'docs/qa/tmp')
import audit_lite
from PIL import Image, ImageDraw
E = 'docs/qa/evidence/'
KEYS = ('face_uniform_frac', 'face_std_median', 'empty_cell_frac_bottom', 'val_p5_p95')
for n, p in [('A2c B', E + 'scene-a2c/b_game.png'), ('A2c D', E + 'scene-a2c/d.png'), ('A2b B', E + 'scene-a2b/b_game.png')]:
    d, _ = audit_lite.measure(n, p)
    print(n, ' '.join('%s=%.3f' % (k, d[k]) for k in KEYS))

def tile(p, lab):
    im = Image.open(p).convert('RGB'); W, H = 960, 540; s = max(W / im.width, H / im.height)
    im = im.resize((round(im.width * s), round(im.height * s)), Image.NEAREST)
    l = (im.width - W) // 2; t = (im.height - H) // 2
    im = im.crop((l, t, l + W, t + H)); ImageDraw.Draw(im).text((10, 10), lab, fill=(255, 230, 0)); return im

R = 'art/reference/dotlp-2026-10-27/'
ts = [tile(E + 'scene-a2c/b_game.png', 'A2c B'), tile(R + 'a-short-hike_1.jpg', '1 A Short Hike'),
      tile(R + 'easy-delivery-co_1.jpg', '6 Easy Delivery Co.'), tile(R + 'crow-country_1.jpg', '12 Crow Country')]
s = Image.new('RGB', (1920, 1080))
for i, t in enumerate(ts): s.paste(t, ((i % 2) * 960, (i // 2) * 540))
s.save(E + 'scene-a2c/sheet.png')
v = Image.new('RGB', (1920, 540))
v.paste(tile(E + 'scene-a2b/b_game.png', 'A2b (left)'), (0, 0)); v.paste(tile(E + 'scene-a2c/b_game.png', 'A2c (right)'), (960, 0))
v.save(E + 'scene-a2c/a2b_vs_a2c.png'); print('sheets ok')
