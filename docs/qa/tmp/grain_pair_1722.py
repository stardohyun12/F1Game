# artlive 1722: 전|후 나란히 (Unity 720 Nearest) + 바뀐 픽셀 수
import sys
import numpy as np
from PIL import Image, ImageDraw
E = 'docs/qa/evidence/'
for asset, spot, out in [('room', 'desk', sys.argv[1]), ('garage', 'drum', 's05_pair_drum.png'), ('garage', 'tire', 's05_pair_tire.png')]:
    a = Image.open(f'{E}studio-{asset}/c_g1722before_{spot}.png').convert('RGB'); b = Image.open(f'{E}studio-{asset}/c_g1722after_{spot}.png').convert('RGB')
    d = (np.abs(np.asarray(a, int) - np.asarray(b, int)).sum(-1) > 6).mean()
    im = Image.new('RGB', (a.width * 2 + 8, a.height), (20, 20, 20)); im.paste(a, (0, 0)); im.paste(b, (a.width + 8, 0))
    g = ImageDraw.Draw(im); g.text((10, 10), 'before', fill=(255, 255, 0)); g.text((a.width + 18, 10), 'after (grain 1722)', fill=(255, 255, 0))
    im.save(f'{E}studio-{asset}/{out}'); print(asset, spot, out, f'changed {100 * d:.1f}%')
