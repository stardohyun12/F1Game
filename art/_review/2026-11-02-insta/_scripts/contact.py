"""contact.py [glob] [out] — stills/*_1080x1350.png 를 한 장으로(칸 270×338, 4열). 기본 out = ../contact_sheet.png"""
import sys, pathlib
from PIL import Image, ImageDraw
root = pathlib.Path(__file__).parent.parent
pat = sys.argv[1] if len(sys.argv) > 1 else "*_1080x1350.png"
out = pathlib.Path(sys.argv[2]) if len(sys.argv) > 2 else root / "contact_sheet.png"
files = sorted((root / ("final" if (root / "final").exists() else "stills")).glob(pat))
files = [f for f in files if not f.name.startswith("test_")]
cols = 4; cw, ch = 270, 338; pad = 6; lab = 18
rows = (len(files) + cols - 1) // cols
sheet = Image.new("RGB", (cols * (cw + pad) + pad, rows * (ch + lab + pad) + pad), (24, 24, 28))
d = ImageDraw.Draw(sheet)
for i, f in enumerate(files):
    im = Image.open(f).convert("RGB").resize((cw, ch), Image.LANCZOS)
    x = pad + (i % cols) * (cw + pad); y = pad + (i // cols) * (ch + lab + pad)
    sheet.paste(im, (x, y)); d.text((x + 2, y + ch + 3), f.name.replace("_1080x1350.png", ""), fill=(230, 230, 230))
sheet.save(out); print(out, len(files), sheet.size)
