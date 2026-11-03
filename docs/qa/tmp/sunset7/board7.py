"""7판 비교 보드: 행 = 6판 / 7판 / A Short Hike, 열 = 왼끝 / 기본 시선 / 오른끝. 전부 640x360 1배."""
import pathlib
from PIL import Image, ImageDraw, ImageFont
R = pathlib.Path(__file__).parents[4]
E6, E7 = R / "docs/qa/evidence/sunset6", R / "docs/qa/evidence/sunset7"
font = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 18)
W, H, G, TOP = 640, 360, 6, 28

def fit(p):
    im = Image.open(p).convert("RGB")
    s = max(W / im.width, H / im.height)
    im = im.resize((round(im.width * s), round(im.height * s)), Image.LANCZOS if s < 1 else Image.NEAREST)
    x, y = (im.width - W) // 2, (im.height - H) // 2
    return im.crop((x, y, x + W, y + H))

rows = [
    ("6판 (D-235 최종 B)", [E6 / "../sunset7/raw/v6_yL.png", E6 / "play/finB_a_fmid.png", E6 / "../sunset7/raw/v6_yR.png"],
     ["왼끝 -35° (편집 캡처)", "기본 시선 (Play·Nearest)", "오른끝 +35° (편집 캡처)"]),
    ("7판", [E7 / "play/p7_yL_fmid.png", E7 / "play/p7_y0_fmid.png", E7 / "play/p7_yR_fmid.png"],
     ["왼끝 -30° (Play)", "기본 시선 (Play)", "오른끝 +35° (Play)"]),
    ("A Short Hike 예시", [None, R / "art/reference/dotlp-2026-10-27/a-short-hike_2.jpg", None], ["", "참고 (6판 보드와 같은 컷, 16:9 로 자름)", ""]),
]
board = Image.new("RGB", (3 * W + 4 * G + 150, len(rows) * (H + TOP) + (len(rows) + 1) * G), (24, 24, 28))
d = ImageDraw.Draw(board)
for r, (name, paths, caps) in enumerate(rows):
    y = G + r * (H + TOP + G)
    d.text((G, y + TOP + H // 2 - 10), name, fill=(235, 235, 235), font=font)
    for c, p in enumerate(paths):
        x = 150 + G + c * (W + G)
        if caps[c]: d.text((x, y + 2), caps[c], fill=(200, 200, 200), font=font)
        if p is not None: board.paste(fit(p), (x, y + TOP))
board.save(E7 / "board7.png")
print("board7.png", board.size)
