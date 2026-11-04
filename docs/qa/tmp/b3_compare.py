# D-301 비교 시트: 위 = 전(2D 판), 아래 = 후(3D 작업대). 칸마다 게임 실제 화소(640x360, Nearest) = 1x.
from PIL import Image, ImageDraw, ImageFont
R = "D:/Github/F1Game/docs/qa/"
cells = [
    ("전: 상자에 넣기(2D)", R + "20261105-0204/tut-M-drop.png"),
    ("전: 감싼 물건(2D)", R + "evidence/pack-visible/wrap.png"),
    ("후: 감싼 물건을 열린 상자로", R + "evidence/pack-bench-3d/put.png"),
    ("후: 테이프 붙이는 중", R + "evidence/pack-bench-3d/tape.png"),
]
W, H, LH = 640, 360, 26
sheet = Image.new("RGB", (W * 2, (H + LH) * 2), (20, 20, 20))
try:
    font = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 18)
except Exception:
    font = ImageFont.load_default()
d = ImageDraw.Draw(sheet)
for i, (label, path) in enumerate(cells):
    x, y = (i % 2) * W, (i // 2) * (H + LH)
    im = Image.open(path).convert("RGB").resize((W, H), Image.NEAREST)
    sheet.paste(im, (x, y + LH))
    d.text((x + 8, y + 3), label, fill=(240, 240, 230), font=font)
sheet.save(R + "evidence/pack-bench-3d/compare.png")
print("saved", sheet.size)
