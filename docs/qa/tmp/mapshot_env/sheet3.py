"""열 = OSM(없음) | 1판(같은 맵 사본 재촬영) | 2판. 행 = 5 카메라. 사용: python sheet3.py <unity 폴더>"""
import sys
from PIL import Image, ImageDraw
d = sys.argv[1]
names = ["cart_home_1", "cart_home_2", "cart_route_1", "cart_route_2", "top"]
cols = [("OSM (gray blocks)", f"{d}/%s_without.png"), ("v1 (8 buildings)", f"{d}/v1remap/%s_with.png"), ("v2 (16 houses)", f"{d}/%s_with.png")]
W, H = 640, 360
sheet = Image.new("RGB", (W * 3, (H + 18) * len(names)), (20, 20, 20))
dr = ImageDraw.Draw(sheet)
for i, n in enumerate(names):
    for j, (label, pat) in enumerate(cols):
        im = Image.open(pat % n).convert("RGB").resize((W, H), Image.LANCZOS)
        sheet.paste(im, (j * W, i * (H + 18) + 18))
        dr.text((j * W + 6, i * (H + 18) + 3), f"{n}  {label}", fill=(255, 255, 255))
sheet.save(f"{d}/sheet.png")
print("ok", sheet.size)
