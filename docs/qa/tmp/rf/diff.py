import sys
from PIL import Image, ImageChops
a = Image.open(sys.argv[1]).convert("RGB"); b = Image.open(sys.argv[2]).convert("RGB")
d = ImageChops.difference(a, b).convert("L")
px = list(d.getdata())
n = sum(1 for v in px if v > 8)
print("changed px", n, "of", len(px), "max", max(px))
if len(sys.argv) > 3:
    d.point(lambda v: 255 if v > 8 else 0).save(sys.argv[3])
