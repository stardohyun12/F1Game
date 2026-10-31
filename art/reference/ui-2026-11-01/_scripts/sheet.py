"""게임별 번호 달린 연락판. 사용: python -I sheet.py <rawdir> <outdir> game [game ...]
각 칸 560x315, 3열. 번호는 파일 이름(00.jpg → 0)."""
import sys, os, glob
from PIL import Image, ImageDraw

raw, out = sys.argv[1], sys.argv[2]
os.makedirs(out, exist_ok=True)
W, H = 560, 315
for ga in sys.argv[3:]:
    g, _, st = ga.partition(":")  # game 또는 game:시작번호
    st = int(st or 0)
    files = sorted(glob.glob(os.path.join(raw, g, "*.jpg")))[st:st + 9]
    if st:
        g = f"{g}_{st}"
    elif g == "edc":
        files = sorted(glob.glob("art/reference/edc-2026-10-26/edc_*.jpg"))[:9]
    cols = 3
    rows = (len(files) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * W, rows * H), (30, 30, 30))
    d = ImageDraw.Draw(sheet)
    for i, f in enumerate(files):
        im = Image.open(f).convert("RGB")
        im.thumbnail((W, H), Image.LANCZOS)
        x, y = (i % cols) * W, (i // cols) * H
        sheet.paste(im, (x, y))
        n = os.path.basename(f)[:2]
        d.rectangle([x, y, x + 34, y + 22], fill=(0, 0, 0))
        d.text((x + 6, y + 4), n, fill=(255, 255, 0))
    p = os.path.join(out, f"sheet_{g}.jpg")
    sheet.save(p, quality=82)
    print(p, sheet.size, len(files))
