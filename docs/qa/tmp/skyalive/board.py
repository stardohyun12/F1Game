"""raw/ s_* (1초×10) · g_* (0.1초×30) → evidence/sky-alive/ 에 시트·GIF·첫/끝 차이판 + stats.json
 차이 = 첫 장과 마지막 장(9초 뒤) 밝기 차 > 6 인 픽셀을 빨강으로(나머지는 흐린 흑백). 하늘 칸 = 위 0..SKY_ROWS 줄(앉은 시점은 수평선 위)."""
import json, pathlib
from PIL import Image, ImageDraw
root = pathlib.Path(__file__).parents[4]
R = root / "docs/qa/evidence/sky-alive/raw"
O = root / "docs/qa/evidence/sky-alive"
SKY_ROWS = {"seat": 120, "road": 110}
TH = 6

def luma(im): return im.convert("L")

def diff_img(a, b):
    la, lb = luma(a).load(), luma(b).load(); w, h = a.size
    out = Image.new("RGB", (w, h)); po = out.load(); bg = luma(b).load()
    ch = 0
    for y in range(h):
        for x in range(w):
            d = abs(la[x, y] - lb[x, y])
            if d > TH: po[x, y] = (255, 40, 40)
            else: g = 40 + bg[x, y] // 3; po[x, y] = (g, g, g)
    return out

def changed(a, b, rows):
    la, lb = luma(a).load(), luma(b).load(); w, _ = a.size
    n = sum(1 for y in range(rows) for x in range(w) if abs(la[x, y] - lb[x, y]) > TH)
    return n / (w * rows)

stats = {}
rows = []
for tod in ("aft", "dusk"):
    for shot in ("seat", "road"):
        row = []
        for ver in ("old", "new"):
            tag = f"{tod}_{shot}_{ver}"
            fr = [Image.open(R / f"s_{tag}_{i:02}.png").convert("RGB") for i in range(10)]
            # 10장 시트(2줄×5, 반 크기)
            w, h = fr[0].size; sw, sh = w // 2, h // 2
            sheet = Image.new("RGB", (sw * 5 + 8, sh * 2 + 2), (255, 255, 255))
            for i, f in enumerate(fr): sheet.paste(f.resize((sw, sh), Image.NEAREST), ((i % 5) * (sw + 2), (i // 5) * (sh + 2)))
            sheet.save(O / f"sheet_{tag}.png")
            d = diff_img(fr[0], fr[-1]); d.save(O / f"diff_{tag}.png")
            per_sec = [changed(fr[i], fr[i + 1], SKY_ROWS[shot]) for i in range(9)]
            stats[tag] = {"sky_changed_first_last": round(changed(fr[0], fr[-1], SKY_ROWS[shot]), 4),
                          "sky_changed_per_1s_median": round(sorted(per_sec)[4], 4)}
            g = [Image.open(R / f"g_{tag}_{i:02}.png").convert("RGB") for i in range(30)]
            g[0].save(O / f"gif_{tag}.gif", save_all=True, append_images=g[1:], duration=100, loop=0)
            row += [fr[0], fr[-1], d]
        rows.append((f"{tod}_{shot}", row))
# 비교판: 줄마다 지금(첫·끝·차이) | 새 판(첫·끝·차이), 반 크기
w, h = 320, 180
board = Image.new("RGB", (w * 6 + 5 * 3 + 12, (h + 16) * len(rows)), (255, 255, 255))
dr = ImageDraw.Draw(board)
for r, (name, ims) in enumerate(rows):
    y = r * (h + 16)
    dr.text((2, y + 2), f"{name}   NOW: first | +9s | diff          NEW: first | +9s | diff", fill=(0, 0, 0))
    for i, im in enumerate(ims):
        x = i * (w + 3) + (12 if i >= 3 else 0)
        board.paste(im.resize((w, h), Image.NEAREST), (x, y + 14))
board.save(O / "compare.png")
json.dump(stats, open(O / "stats.json", "w"), indent=1)
print(json.dumps(stats, indent=1))
