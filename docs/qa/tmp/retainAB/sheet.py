"""retainAB 시트: 세로 = 카메라, 가로 = 현재|A|B. python -X utf8 docs/qa/tmp/retainAB/sheet.py"""
import pathlib
from PIL import Image, ImageDraw, ImageFont
R = pathlib.Path(__file__).resolve().parents[4]
E = R / "docs/qa/evidence/retainAB"
OUT = R / "art/_review/2026-10-30-retain-AB"
OUT.mkdir(parents=True, exist_ok=True)
tags = [("cur", "현재 (돌 사진 그대로)"), ("A", "A 도트 32px/m · 20색 · Point"), ("B", "B 건물 방식 · 결 ±10% · house_home 4단")]
rows = [("c1_cart", "c1_cart"), ("c2_cart", "c2_cart"), ("f1_high", "f1_high"), ("z1_close", "근접 z1 (축대+집)")]
CW, CH, LH = 640, 360, 26
font = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 17)
sheet = Image.new("RGB", (CW * 3 + 8 * 2, (CH + LH) * len(rows) + 8 * (len(rows) - 1)), (24, 24, 24))
for r, (cam, rl) in enumerate(rows):
    for c, (tag, tl) in enumerate(tags):
        im = Image.open(E / tag / f"{cam}.png").convert("RGB").resize((CW, CH), Image.LANCZOS)
        x, y = c * (CW + 8), r * (CH + LH + 8)
        d = ImageDraw.Draw(sheet)
        d.text((x + 6, y + 3), f"{rl} · {tl}", fill=(235, 235, 235), font=font)
        sheet.paste(im, (x, y + LH))
sheet.save(OUT / "sheet.png")
# 근접 2컷 원본 크기(z1, z2) 가로 3칸 — 픽셀 결 확인용
for cam in ("z1_close", "z2_close"):
    w = Image.new("RGB", (1280 * 3 + 16, 720 + LH), (24, 24, 24)); d = ImageDraw.Draw(w)
    for c, (tag, tl) in enumerate(tags):
        x = c * (1280 + 8); d.text((x + 6, 3), f"{cam} · {tl}", fill=(235, 235, 235), font=font)
        w.paste(Image.open(E / tag / f"{cam}.png").convert("RGB"), (x, LH))
    w.save(OUT / f"{cam}_full.png")
print("ok")
