# 사용: pair.py out.png tag1 tag2 ... → 2열 격자, 각 2배 점확대, 글자 라벨
import sys
from PIL import Image, ImageDraw
D = 'docs/qa/evidence/sunset6/'
out, tags = sys.argv[1], sys.argv[2:]
ims = []
for t in tags:
    p = t if t.endswith('.png') else D + 'raw/' + t + '.png'
    im = Image.open(p).convert('RGB')
    if im.size != (640, 360): im = im.resize((640, 360), Image.LANCZOS)
    ims.append((t.split('/')[-1].replace('.png', ''), im.resize((1280, 720), Image.NEAREST)))
cols = 2; rows = (len(ims) + 1) // 2
W = Image.new('RGB', (1280 * cols, 720 * rows), (20, 20, 20)); dr = ImageDraw.Draw(W)
for i, (t, im) in enumerate(ims):
    x, y = (i % cols) * 1280, (i // cols) * 720
    W.paste(im, (x, y)); dr.rectangle([x, y, x + 8 * len(t) + 10, y + 16], fill=(0, 0, 0)); dr.text((x + 5, y + 3), t, fill=(255, 255, 255))
W.save(D + out); print(D + out, W.size)
