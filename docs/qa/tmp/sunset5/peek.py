# 사용: python docs/qa/tmp/sunset5/peek.py out.png a.png b.png ... → 세로로 쌓아 2배(점확대)
import sys
from PIL import Image
ims = [Image.open(p).convert('RGB') for p in sys.argv[2:]]
w = Image.new('RGB', (max(i.width for i in ims), sum(i.height + 8 for i in ims)))
y = 0
for im in ims: w.paste(im, (0, y)); y += im.height + 8
w.resize((w.width * 2, w.height * 2), Image.NEAREST).save(sys.argv[1])
