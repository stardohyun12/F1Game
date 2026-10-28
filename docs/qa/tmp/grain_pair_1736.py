# artlive 1736: 1차(전) | 2차(후) — 원래 지점 · 1m · 5m 세 줄, 원래 크기(720 Nearest 그대로). + 1m 가운데 4배 확대 줄
import numpy as np
from PIL import Image, ImageDraw
E = 'docs/qa/evidence/'
for asset, spot, out in [('room', 'desk', 's02_pair_desk.png'), ('garage', 'drum', 's06_pair_drum.png'), ('garage', 'tire', 's06_pair_tire.png')]:
    rows = []
    for d in ('', '_1m', '_5m'):
        a = Image.open(f'{E}studio-{asset}/c_g1736before_{spot}{d}.png').convert('RGB'); b = Image.open(f'{E}studio-{asset}/c_g1736after_{spot}{d}.png').convert('RGB')
        ch = (np.abs(np.asarray(a, int) - np.asarray(b, int)).sum(-1) > 6).mean(); rows.append((d or '_orig', a, b, ch))
    _, a1, b1, _ = rows[1]; box = (480, 270, 800, 450)  # 1m 가운데 320x180 → 4배
    z = [im.crop(box).resize((1280, 720), Image.NEAREST) for im in (a1, b1)]
    W = 1280 * 2 + 8; im = Image.new('RGB', (W, 720 * 4 + 24), (20, 20, 20)); g = ImageDraw.Draw(im)
    for i, (d, a, b, ch) in enumerate(rows + [('_1m x4', z[0], z[1], 0)]):
        y = i * 728; im.paste(a, (0, y)); im.paste(b, (1288, y))
        g.text((10, y + 8), f'1st pass {d[1:]}', fill=(255, 255, 0)); g.text((1298, y + 8), f'2nd pass (1736) {d[1:]}' + (f'  changed {100 * ch:.0f}%' if ch else ''), fill=(255, 255, 0))
    im.save(f'{E}studio-{asset}/{out}'); print(out, [f'{r[0]} {100 * r[3]:.0f}%' for r in rows])
