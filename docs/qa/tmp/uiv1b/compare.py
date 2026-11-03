# 비교판: 왼쪽 = 1판(15칸 격자), 오른쪽 = 이번 판(방향 표시만 1.5배). 같은 프레임(시간 정지)에서 찍음. 1x 전체 + 위 가운데 3x 확대.
from PIL import Image, ImageDraw
P = "docs/qa/evidence/ui-v1b/pairs/"
OUT = "docs/qa/evidence/ui-v1b/compare.png"
rows = []
for i in range(3):
    a = Image.open(P + "drive%d_v1.png" % i).convert("RGB")
    b = Image.open(P + "drive%d_v1b.png" % i).convert("RGB")
    box = (240, 0, 400, 56)
    za = a.crop(box).resize((480, 168), Image.NEAREST)
    zb = b.crop(box).resize((480, 168), Image.NEAREST)
    W = a.width + b.width + 10
    row = Image.new("RGB", (W, a.height + 4 + 168 + 14), (30, 30, 30))
    row.paste(a, (0, 0)); row.paste(b, (a.width + 10, 0))
    row.paste(za, (80, a.height + 4)); row.paste(zb, (a.width + 10 + 80, a.height + 4))
    d = ImageDraw.Draw(row)
    d.text((4, 4), "v1 (15px grid)", fill=(255, 255, 0)); d.text((a.width + 14, 4), "now (x1.5, 23px grid)", fill=(255, 255, 0))
    rows.append(row)
H = sum(r.height for r in rows) + 8 * (len(rows) - 1)
out = Image.new("RGB", (rows[0].width, H), (30, 30, 30))
y = 0
for r in rows:
    out.paste(r, (0, y)); y += r.height + 8
out.save(OUT)
print(out.size)
