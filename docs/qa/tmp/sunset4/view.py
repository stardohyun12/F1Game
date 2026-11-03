# 사용: python docs/qa/tmp/sunset4/view.py out.png a.png [b.png ...] [--x2]  → 세로로 쌓기(점확대 옵션)
import sys
from PIL import Image
args = [a for a in sys.argv[1:] if a != '--x2']; x2 = '--x2' in sys.argv
out, files = args[0], args[1:]
ims = [Image.open(f).convert('RGB') for f in files]
w = max(i.width for i in ims); h = sum(i.height + 4 for i in ims)
b = Image.new('RGB', (w, h)); y = 0
for im in ims:
    b.paste(im, (0, y)); y += im.height + 4
if x2: b = b.resize((b.width * 2, b.height * 2), Image.NEAREST)
b.save(out)
