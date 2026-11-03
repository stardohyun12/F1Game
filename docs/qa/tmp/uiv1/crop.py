"""4× 확대 조각(Nearest): python crop.py <png> <x> <y(위=0)> <w> <h> <out>"""
import sys
from PIL import Image
src, x, y, w, h, out = sys.argv[1], *map(int, sys.argv[2:6]), sys.argv[6]
im = Image.open(src).convert("RGB").crop((x, y, x + w, y + h))
im.resize((w * 4, h * 4), Image.NEAREST).save(out)
