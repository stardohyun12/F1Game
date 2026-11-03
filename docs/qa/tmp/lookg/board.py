"""look-global 비교판: 행 = 자리, 열 = [지금 낮 | 새 오후 | 새 노을]. 2배 최근접 확대. 없는 칸은 회색."""
import sys, pathlib
from PIL import Image, ImageDraw
raw = pathlib.Path("D:/Github/F1Game/docs/qa/evidence/look-global/raw")
out = pathlib.Path("D:/Github/F1Game/docs/qa/evidence/look-global")
rows = sys.argv[1].split(",")          # 예: home,harbor,hill
cols = [("day", "지금 낮"), ("aft", "새 오후"), ("dusk", "새 노을")]
name = sys.argv[2] if len(sys.argv) > 2 else "board.png"
if len(sys.argv) > 3: cols = [(c, c) for c in sys.argv[3].split(",")]   # 열 접두어 바꾸기(예 base9,dusk,aft)
S = 2; W, H = 640 * S, 360 * S; pad = 4
img = Image.new("RGB", (W * 3 + pad * 2, H * len(rows) + pad * (len(rows) - 1)), (40, 40, 40))
for r, spot in enumerate(rows):
    for c, (pre, _) in enumerate(cols):
        p = raw / f"{pre}_{spot}.png"
        x, y = c * (W + pad), r * (H + pad)
        if p.exists():
            img.paste(Image.open(p).convert("RGB").resize((W, H), Image.NEAREST), (x, y))
        d = ImageDraw.Draw(img); d.rectangle([x, y, x + 150, y + 22], fill=(0, 0, 0)); d.text((x + 4, y + 5), f"{pre}_{spot}", fill=(255, 255, 255))
img.save(out / name)
print(out / name, img.size)
