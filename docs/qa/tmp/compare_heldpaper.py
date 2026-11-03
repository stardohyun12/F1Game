# D-269 비교판: Firewatch 레퍼런스 옆에 우리 캡처를 나란히 (1× 640×360 그대로 + 3× 확대 크롭)
# 사용: python -I compare_heldpaper.py out.png ref.jpg "라벨=경로" ... [--crop3x 경로 x y w h]
import sys
from PIL import Image, ImageDraw, ImageFont

out, ref = sys.argv[1], sys.argv[2]
args = sys.argv[3:]
crop = None
if "--crop3x" in args:
    i = args.index("--crop3x")
    crop = (args[i + 1], *map(int, args[i + 2:i + 6]))
    args = args[:i]
panels = [a.split("=", 1) for a in args]

W, H, PAD, LAB = 640, 360, 8, 22
font = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 15)

tiles = [("Firewatch (레퍼런스)", Image.open(ref).convert("RGB").resize((W, H), Image.LANCZOS))]
for lab, p in panels:
    tiles.append((lab, Image.open(p).convert("RGB").resize((W, H), Image.NEAREST)))
if crop:
    p, x, y, w, h = crop
    c = Image.open(p).convert("RGB").crop((x, y, x + w, y + h)).resize((w * 3, h * 3), Image.NEAREST)
    c = c.crop((0, 0, min(W, c.width), min(H, c.height)))
    canvas = Image.new("RGB", (W, H), (20, 20, 20)); canvas.paste(c, (0, 0))
    tiles.append(("3× 확대 (읽기 자세 글자)", canvas))

cols = 2
rows = (len(tiles) + cols - 1) // cols
img = Image.new("RGB", (cols * (W + PAD) + PAD, rows * (H + LAB + PAD) + PAD), (32, 32, 32))
d = ImageDraw.Draw(img)
for k, (lab, t) in enumerate(tiles):
    cx = PAD + (k % cols) * (W + PAD); cy = PAD + (k // cols) * (H + LAB + PAD)
    d.text((cx, cy), lab, fill=(235, 235, 235), font=font)
    img.paste(t, (cx, cy + LAB))
img.save(out)
print(out, img.size)
