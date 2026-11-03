"""8판 비교 보드: 행 = 6판 / 7판 / 8판 / A Short Hike, 열 = 왼끝 / 기본 시선 / 오른끝. 전부 640x360 1배.
+ strip8.png: 12 s 연속 6장(2.4 s 간격, 1배) · zoom8.png: 반짝임·갈매기·등대 4배(Nearest)"""
import pathlib, json
from PIL import Image, ImageDraw, ImageFont
R = pathlib.Path(__file__).parents[4]
E6, E7, E8 = R / "docs/qa/evidence/sunset6", R / "docs/qa/evidence/sunset7", R / "docs/qa/evidence/sunset8"
font = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 18)
W, H, G, TOP = 640, 360, 6, 28

def fit(p):
    im = Image.open(p).convert("RGB")
    s = max(W / im.width, H / im.height)
    im = im.resize((round(im.width * s), round(im.height * s)), Image.LANCZOS if s < 1 else Image.NEAREST)
    x, y = (im.width - W) // 2, (im.height - H) // 2
    return im.crop((x, y, x + W, y + H))

def grid(rows, cols, out, label_w=150):
    board = Image.new("RGB", (cols * W + (cols + 1) * G + label_w, len(rows) * (H + TOP) + (len(rows) + 1) * G), (24, 24, 28))
    d = ImageDraw.Draw(board)
    for r, (name, paths, caps) in enumerate(rows):
        y = G + r * (H + TOP + G)
        d.text((G, y + TOP + H // 2 - 10), name, fill=(235, 235, 235), font=font)
        for c, p in enumerate(paths):
            x = label_w + G + c * (W + G)
            if caps[c]: d.text((x, y + 2), caps[c], fill=(200, 200, 200), font=font)
            if p is not None: board.paste(fit(p), (x, y + TOP))
    board.save(out); print(out.name, board.size)

grid([
    ("6판", [E7 / "raw/v6_yL.png", E6 / "play/finB_a_fmid.png", E7 / "raw/v6_yR.png"],
     ["왼끝 -35° (편집 캡처)", "기본 시선 (Play)", "오른끝 +35° (편집 캡처)"]),
    ("7판", [E7 / "play/p7_yL_fmid.png", E7 / "play/p7_y0_fmid.png", E7 / "play/p7_yR_fmid.png"],
     ["왼끝 -30° (Play)", "기본 시선 (Play)", "오른끝 +35° (Play)"]),
    ("8판", [E8 / "raw/left.png", E8 / "raw/s1.png", E8 / "raw/right.png"],
     ["왼끝 -30° (Play)", "기본 시선 (Play)", "오른끝 +35° (Play)"]),
    ("A Short Hike 예시", [None, R / "art/reference/dotlp-2026-10-27/a-short-hike_2.jpg", None], ["", "참고 (16:9 로 자름)", ""]),
], 3, E8 / "board8.png")

rows = [json.loads(l) for l in (E8 / "strip.jsonl").read_text(encoding="utf-8").splitlines() if l.strip()]
def cap(j): return f"{j['tag']} t={j['t']:.1f}s 등대{'켜짐' if j['lamp'] else '꺼짐'} 반짝{j['glint']} 갈매기{'O' if j['gulls'] else '-'} 배x{j['boatX']}"
grid([("12초 ①", [E8 / f"raw/s{i}.png" for i in range(3)], [cap(rows[i]) for i in range(3)]),
      ("12초 ②", [E8 / f"raw/s{i}.png" for i in range(3, 6)], [cap(rows[i]) for i in range(3, 6)])], 3, E8 / "strip8.png", 110)

# 4배 확대: (파일, 중심 x, y, 반폭, 반높이, 이름) — 좌표는 snap JSON(뷰포트) × 640/360
def vp(s): x, y = map(float, s.split(",")); return round(x * W), round(y * H)
z = []
for t in ("s2", "s4"):
    j = next(r for r in rows if r["tag"] == t); bx, by = vp(j["bobber"]); z.append((E8 / f"raw/{t}.png", bx, by - 6, 40, 22, f"{t} 찌 · 반짝임 {'켜짐' if j['glint'] else '꺼짐'} 4×"))
z.append((E8 / "raw/s0.png", 323, 54, 60, 30, "s0 갈매기 3마리 중 둘(35 m) 4×"))
z.append((E8 / "raw/s5.png", 522, 98, 40, 22, "s5 등대 켜짐 4×"))
z.append((E8 / "raw/s3.png", 522, 98, 40, 22, "s3 등대 꺼짐 4×"))
z.append((E8 / "raw/s1.png", 412, 98, 70, 40, "s1 해 둘레 빛 2단 4×"))
tiles = []
for p, x, y, hw, hh, name in z:
    im = Image.open(p).convert("RGB").crop((x - hw, y - hh, x + hw, y + hh))
    im = im.resize((im.width * 4, im.height * 4), Image.NEAREST); tiles.append((im, name))
ZW = max(t[0].width for t in tiles); ZH = sum(t[0].height + TOP + G for t in tiles) + G
zb = Image.new("RGB", (ZW + 2 * G, ZH), (24, 24, 28)); d = ImageDraw.Draw(zb); y = G
for im, name in tiles:
    d.text((G, y + 2), name, fill=(220, 220, 220), font=font); zb.paste(im, (G, y + TOP)); y += im.height + TOP + G
zb.save(E8 / "zoom8.png"); print("zoom8.png", zb.size)
