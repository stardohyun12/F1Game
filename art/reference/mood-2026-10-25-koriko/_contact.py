import json, os, sys
from PIL import Image, ImageDraw, ImageFont
ROOT = os.path.dirname(os.path.abspath(__file__))
CAND = os.path.join(ROOT, '_cand')
m = json.load(open(os.path.join(CAND, 'meta.json'), encoding='utf-8'))
font = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 22)
prefixes = sys.argv[1].split(',')
out = sys.argv[2]
keys = sorted(k for k in m if any(k.startswith(p) for p in prefixes) and os.path.exists(os.path.join(CAND, k + '.jpg')))
cols, tw, th = 5, 320, 213
rows = (len(keys) + cols - 1) // cols
sheet = Image.new('RGB', (cols * tw, rows * th), 'black')
d = ImageDraw.Draw(sheet)
for i, k in enumerate(keys):
    im = Image.open(os.path.join(CAND, k + '.jpg')).convert('RGB')
    im.thumbnail((tw - 4, th - 4))
    x, y = (i % cols) * tw, (i // cols) * th
    sheet.paste(im, (x + 2, y + 2))
    d.rectangle([x + 2, y + 2, x + 2 + 230, y + 30], fill='black')
    d.text((x + 6, y + 2), k[-24:], font=font, fill='yellow')
sheet.save(out, quality=85)
print(len(keys), out)
