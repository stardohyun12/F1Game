"""j4 작업용 — 고른 사진에 10% 격자를 그은 축소본(좌표 고르기용). PIL 만.  python tools/asset-gen/opus_houses_j4_grid.py"""
import os
from PIL import Image, ImageDraw
S = 'art/source/photo-picks/'; O = 'art/_review/2026-10-27-houses-j4/_work/'; os.makedirs(O, exist_ok=True)
PICKS = ['A/A2_apricot_facade_burano', 'B/B1_shutters_greengrocer', 'C/C1_blue_louvered_urbino', 'C/C2_green_shutters_liguria',
         'D/D1_rice_shop_yoyogi', 'D/D2_soba_shop_noren', 'E/E2_clay_roof_tiles_02', 'E/E3_clay_roof_tiles_03']
for p in PICKS:
    im = Image.open(S + p + '.jpg').convert('RGB'); W, H = im.size; k = 640 / max(W, H); t = im.resize((int(W * k), int(H * k)))
    d = ImageDraw.Draw(t)
    for i in range(1, 10):
        x = t.width * i // 10; y = t.height * i // 10
        d.line([(x, 0), (x, t.height)], fill=(255, 0, 255)); d.line([(0, y), (t.width, y)], fill=(255, 0, 255))
        d.text((x + 2, 2), str(i), fill=(255, 255, 0)); d.text((2, y + 2), str(i), fill=(255, 255, 0))
    t.save(O + p.split('/')[1][:2] + '_grid.png'); print(p, W, H)
