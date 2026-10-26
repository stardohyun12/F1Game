"""2053: Look 5 → 5b 나란히(before/after) + 시간대 4종 시트. 480×270 원본을 ×2 Nearest."""
import os
from PIL import Image, ImageDraw
R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
OLD = os.path.join(R, 'docs/qa/evidence/look5-edc'); NEW = os.path.join(R, 'docs/qa/evidence/look5b')
W, H = 960, 540


def tile(path, label):
    im = Image.open(path).convert('RGB').resize((W, H), Image.NEAREST)
    d = ImageDraw.Draw(im); d.rectangle((0, 0, 8 * len(label) + 12, 22), fill=(0, 0, 0)); d.text((6, 5), label, fill=(255, 255, 255))
    return im


rows = [('a_car', 'look5'), ('b_street', 'look5'), ('c_town', 'look5')]
sheet = Image.new('RGB', (W * 2, H * 3))
for i, (k, t) in enumerate(rows):
    sheet.paste(tile(os.path.join(OLD, f'{k}_{t}_raw480.png'), f'{k} Look5 (before)'), (0, i * H))
    sheet.paste(tile(os.path.join(NEW, f'{k}_{t}_raw480.png'), f'{k} Look5b (after)'), (W, i * H))
sheet.save(os.path.join(NEW, 'before_after.png'))

for k in ('a_car', 'b_street', 'c_town'):
    s = Image.new('RGB', (W * 2, H * 2))
    for i, t in enumerate(('t1morning', 't2afternoon', 't3sunset', 't4dusk')):
        p = os.path.join(NEW, f'{k}_{t}_raw480.png')
        if not os.path.exists(p):
            p = os.path.join(NEW, f'{k}_look5_raw480.png')
        s.paste(tile(p, f'{k} {t}'), ((i % 2) * W, (i // 2) * H))
    s.save(os.path.join(NEW, f'tod_{k}.png'))
print('ok')
