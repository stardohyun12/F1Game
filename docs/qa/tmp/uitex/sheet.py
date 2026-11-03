"""D-258 UI 비교판: 지금 | A | B  ×  (칠판·전표·말풍선) × (오후·노을).
sheet.png  = 1× (360 격자: 게임 화면을 640×360 으로 최근접 축소 — 도트 1칸 = 1px)
sheet_3x.png = 각 요소 둘레를 360 격자에서 잘라 3배 최근접 확대.
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

EV = Path("D:/Github/F1Game/docs/qa/evidence/uitex")
OUT = Path("D:/Github/F1Game/art/_review/2026-11-02-ui-texture")
OUT.mkdir(parents=True, exist_ok=True)
ROWS = [("칠판", "d_03_board_view"), ("Tab 전표", "d_06_slip_on_foot"), ("사장 말풍선", "f2_03_talk_1")]
COLS = [("지금", "now"), ("A 세상 속", "a"), ("B 텍스처 화면", "b")]
TIMES = [("오후", "aft"), ("노을", "dusk")]
# 360 격자 좌표 (x0, y0, x1, y1) — 3× 확대 자리
CROP = {"d_03_board_view": (150, 60, 490, 300), "d_06_slip_on_foot": (0, 220, 220, 360), "f2_03_talk_1": (190, 30, 450, 200)}

try:
    FONT = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 14)
except Exception:
    FONT = ImageFont.load_default()


def grid(p):
    im = Image.open(p).convert("RGB")
    return im.resize((640, 360), Image.NEAREST)


def build(scale3):
    cells = []
    for tl, tk in TIMES:
        for rl, rk in ROWS:
            row = []
            for cl, ck in COLS:
                p = EV / tk / f"{rk}_{ck}.png"
                if p.exists():
                    im = grid(p)
                    if scale3:
                        im = im.crop(CROP[rk])
                        im = im.resize((im.width * 3, im.height * 3), Image.NEAREST)
                else:
                    w, h = ((CROP[rk][2] - CROP[rk][0]) * 3, (CROP[rk][3] - CROP[rk][1]) * 3) if scale3 else (640, 360)
                    im = Image.new("RGB", (w, h), (60, 0, 0))
                row.append((f"{tl} · {rl} · {cl}", im))
            cells.append(row)
    pad, lab = 6, 20
    cw = [max(r[i][1].width for r in cells) for i in range(3)]
    rh = [max(c[1].height for c in r) for r in cells]
    W = sum(cw) + pad * 4
    H = sum(h + lab for h in rh) + pad * (len(rh) + 1)
    sheet = Image.new("RGB", (W, H), (24, 24, 24))
    d = ImageDraw.Draw(sheet)
    y = pad
    for r, h in zip(cells, rh):
        x = pad
        for i, (t, im) in enumerate(r):
            d.text((x, y + 2), t, fill=(230, 230, 230), font=FONT)
            sheet.paste(im, (x, y + lab))
            x += cw[i] + pad
        y += h + lab + pad
    name = "sheet_3x.png" if scale3 else "sheet.png"
    sheet.save(OUT / name)
    print(OUT / name, sheet.size)


build(False)
build(True)
