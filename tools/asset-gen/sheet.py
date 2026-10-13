"""PNG 여러 장을 라벨 달아 한 장(격자)으로 붙인다. 도현 판정용.

사용:
    python -X utf8 tools/asset-gen/sheet.py --out sheet.png --cols 4 \
        "참고 이미지=cake_ref.png" "Meshy 원본 54k=cake_01_shaded.png" ...
"""
import argparse, os
from PIL import Image, ImageDraw, ImageFont

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--cols", type=int, default=4)
    ap.add_argument("--cell", type=int, default=512)
    ap.add_argument("items", nargs="+", help='"라벨=경로"')
    a = ap.parse_args()
    cell, pad, label_h = a.cell, 12, 40
    try:
        font = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 22)
    except Exception:
        font = ImageFont.load_default()
    items = []
    for it in a.items:
        label, path = it.split("=", 1) if "=" in it else (os.path.basename(it), it)
        items.append((label, path))
    rows = (len(items) + a.cols - 1) // a.cols
    W = a.cols * (cell + pad) + pad
    H = rows * (cell + label_h + pad) + pad
    sheet = Image.new("RGB", (W, H), (245, 245, 245))
    d = ImageDraw.Draw(sheet)
    for i, (label, path) in enumerate(items):
        r, c = divmod(i, a.cols)
        x = pad + c * (cell + pad); y = pad + r * (cell + label_h + pad)
        try:
            im = Image.open(path).convert("RGB")
            im.thumbnail((cell, cell))
            bg = Image.new("RGB", (cell, cell), (255, 255, 255))
            bg.paste(im, ((cell - im.width) // 2, (cell - im.height) // 2))
        except Exception as e:
            bg = Image.new("RGB", (cell, cell), (220, 200, 200))
            ImageDraw.Draw(bg).text((10, 10), f"없음: {e}", fill=(120, 0, 0), font=font)
        sheet.paste(bg, (x, y))
        d.rectangle([x, y, x + cell - 1, y + cell - 1], outline=(200, 200, 200))
        d.text((x + 4, y + cell + 8), label, fill=(30, 30, 30), font=font)
    sheet.save(a.out)
    print("OK", a.out, sheet.size)

if __name__ == "__main__":
    main()
