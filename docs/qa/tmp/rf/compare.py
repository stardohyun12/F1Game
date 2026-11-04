# 비교판: 전 | A | B | valley_peaks_1(축소) 가로 4칸, 1× 그대로(확대·필터 없음). 레퍼런스만 640×360 칸에 맞춰 축소.
import sys
from PIL import Image, ImageDraw
root = "D:/Github/F1Game/"
ev = root + "docs/qa/evidence/art-room-flat/"
names = [("before", ev + "before.png"), ("A", ev + "A.png"), ("B", ev + "B.png"), ("ref valley_peaks_1", root + "world/taste/good/valley_peaks_1.jpg")]
W, H, LBL = 640, 360, 14
board = Image.new("RGB", (W * 4, H + LBL), (20, 20, 20))
d = ImageDraw.Draw(board)
for i, (label, path) in enumerate(names):
    im = Image.open(path).convert("RGB")
    if im.size != (W, H):
        s = min(W / im.width, H / im.height)
        im = im.resize((max(1, round(im.width * s)), max(1, round(im.height * s))), Image.LANCZOS)
    board.paste(im, (i * W + (W - im.width) // 2, LBL + (H - im.height) // 2))
    d.text((i * W + 4, 1), label, fill=(230, 230, 230))
board.save(ev + "compare.png")
# 4× 확대(B 문 모서리·책상) — 판정 순서 1× → 흑실루엣 → 4× 용
b = Image.open(ev + "B.png").convert("RGB").crop((100, 150, 260, 240))
b.resize((b.width * 4, b.height * 4), Image.NEAREST).save(ev + "B_crop4x.png")
print("ok", board.size)
