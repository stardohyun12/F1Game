import sys
from PIL import Image, ImageDraw
d = sys.argv[1]
names = ["cart_home_1", "cart_home_2", "cart_route_1", "cart_route_2", "top"]
W, H = 640, 360
sheet = Image.new("RGB", (W * 2, (H + 18) * len(names)), (20, 20, 20))
dr = ImageDraw.Draw(sheet)
for i, n in enumerate(names):
    for j, k in enumerate(["without", "with"]):
        im = Image.open(f"{d}/{n}_{k}.png").convert("RGB").resize((W, H), Image.LANCZOS)
        sheet.paste(im, (j * W, i * (H + 18) + 18))
        dr.text((j * W + 6, i * (H + 18) + 3), f"{n}  {'WITHOUT (OSM gray blocks)' if k == 'without' else 'WITH (8 background buildings)'}", fill=(255, 255, 255))
sheet.save(f"{d}/sheet.png")
print("ok", sheet.size)
