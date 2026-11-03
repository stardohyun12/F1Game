"""360 화면 색 세기: 고유 색 수 · 주요 8색(median cut) · 4계열 분류 · 탁한 색 비율. python colors.py tag=path ... → JSON + 팔레트 그림"""
import sys, json, colorsys, pathlib
from PIL import Image, ImageDraw, ImageFont
font = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 14)

def fam(r, g, b):
    h, l, s = colorsys.rgb_to_hls(r / 255, g / 255, b / 255); hd = h * 360
    if l > 0.80 or (l > 0.68 and 35 <= hd <= 65): return "강조(흰·노랑)"
    if l < 0.16: return "실루엣(남보라·검정)"
    if 200 <= hd <= 290 and l < 0.42: return "실루엣(남보라·검정)" if s < 0.35 or hd > 235 else "물(청록·남)"
    if 160 <= hd < 235: return "물(청록·남)"
    if hd < 50 or hd >= 300: return "하늘·빛(주황·분홍)"
    return "그 밖(초록·갈색 등)"

def muddy(r, g, b):
    h, l, s = colorsys.rgb_to_hls(r / 255, g / 255, b / 255)
    return 0.18 < l < 0.6 and s < 0.12   # 중간 밝기 회색 기운 = 탁한 색

out = {}; rows = []
for a in sys.argv[2:]:
    tag, p = a.split("=", 1)
    im = Image.open(p).convert("RGB")
    if im.size != (640, 360): im = im.resize((640, 360), Image.LANCZOS)
    px = list(im.getdata()); n = len(px)
    uniq = len(set(px))
    q = im.quantize(8, method=Image.Quantize.MEDIANCUT); pal = q.getpalette()[:24]; cnt = sorted(q.getcolors(), reverse=True)
    top = [{"rgb": pal[i * 3:i * 3 + 3], "pct": round(c / n * 100, 1), "fam": fam(*pal[i * 3:i * 3 + 3])} for c, i in cnt]
    fams = {}
    for c in px: f = fam(*c); fams[f] = fams.get(f, 0) + 1
    md = sum(1 for c in px if muddy(*c))
    out[tag] = {"unique": uniq, "top8": top, "families_pct": {k: round(v / n * 100, 1) for k, v in sorted(fams.items(), key=lambda x: -x[1])}, "muddy_pct": round(md / n * 100, 1)}
    rows.append((tag, im, top))
sys.stdout.reconfigure(encoding="utf-8")
pathlib.Path(sys.argv[1]).with_suffix(".json").write_text(json.dumps(out, ensure_ascii=False, indent=1), encoding="utf-8")
for k, v in out.items():
    print(k, "unique", v["unique"], "muddy%", v["muddy_pct"], v["families_pct"])
    print("   ", [(tuple(x["rgb"]), x["pct"], x["fam"][:3]) for x in v["top8"]])
# 팔레트 그림: 행 = 판, 왼쪽 화면 1/2 축소, 오른쪽 8색 칸(면적 비율 % · 계열)
W = 320; board = Image.new("RGB", (W + 8 * 92 + 20, len(rows) * 190 + 10), (24, 24, 28)); d = ImageDraw.Draw(board)
for r, (tag, im, top) in enumerate(rows):
    y = 10 + r * 190
    board.paste(im.resize((W, 180), Image.LANCZOS), (5, y)); d.text((8, y + 2), f"{tag} 고유색 {out[tag]['unique']} · 탁한색 {out[tag]['muddy_pct']}%", fill=(255, 255, 255), font=font)
    for k, t in enumerate(top):
        x = W + 15 + k * 92; d.rectangle((x, y, x + 86, y + 120), fill=tuple(t["rgb"]))
        d.text((x, y + 124), f"{t['pct']}%", fill=(230, 230, 230), font=font); d.text((x, y + 142), t["fam"][:6], fill=(200, 200, 200), font=font)
board.save(sys.argv[1]); print("board", sys.argv[1], board.size)
