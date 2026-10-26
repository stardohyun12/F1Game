# rows/*.png + meta.json → art/thirdparty/contact.png (팩별 줄, 왼쪽 라벨, 물건 이름)
import json, os, sys
from PIL import Image, ImageDraw, ImageFont
D = os.path.dirname(os.path.abspath(__file__))
meta = json.load(open(os.path.join(D, 'rows', 'meta.json'), encoding='utf8'))
try:
    F = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 15); FB = ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf', 18)
    FS = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 11)
except Exception:
    F = FB = FS = ImageFont.load_default()
LW = 300; PAD = 10; LBL = 46
rows = []
for r in meta:
    im = Image.open(r['file']).convert('RGB')
    rows.append((r, im))
W = LW + max(im.width for _, im in rows) + PAD
H = sum(im.height + LBL + PAD for _, im in rows) + 60
sheet = Image.new('RGB', (W, H), (235, 233, 228))
d = ImageDraw.Draw(sheet)
d.text((10, 10), '무료 CC0/CC-BY 로우폴리·PSX 팩 일람 — 모든 줄 같은 축척 32px/m(정사영), 빨간 막대 = 사람 1.7m. 원래 단위 그대로(* = 크기 보정한 것)', fill=(20, 20, 20), font=FB)
y = 50
for r, im in rows:
    d.rectangle([0, y - 4, W, y + im.height + LBL], outline=(180, 178, 170))
    d.text((10, y + 4), r['label'], fill=(10, 10, 10), font=FB)
    d.text((10, y + 30), f"{len(r['labels'])}개 · {im.width / 32:.0f}m 폭", fill=(80, 80, 80), font=F)
    sheet.paste(im, (LW, y))
    d.text((LW + r['bar'][0] - 10, y + im.height + 2), '1.7m', fill=(200, 0, 0), font=FS)
    last = -999; alt = 0
    for name, px, py in r['labels']:
        alt = alt + 1 if px - last < 70 else 0
        last = px
        d.text((LW + px - 20, y + im.height + 2 + (alt % 3) * 13), name[:22], fill=(30, 30, 90), font=FS)
    y += im.height + LBL + PAD
out = os.path.join(os.path.dirname(D), 'contact.png')
sheet.save(out)
print(out, sheet.size)
