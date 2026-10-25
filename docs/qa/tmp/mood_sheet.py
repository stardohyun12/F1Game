"""D-137 하루 빛 4종 × 3지점 → docs/qa/evidence/bayloop-mood/sheet.png (행 = 시간대, 열 = 지점)."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

D = Path(__file__).resolve().parents[3] / "docs/qa/evidence/bayloop-mood"
MOODS = [("1morning", "아침 안개"), ("2afternoon", "가을 오후"), ("3sunset", "해 질 녘 역광"), ("4dusk", "초저녁 창에 불")]
SPOTS = [("02_shop_climb", "02 상점가 오르막"), ("04_hilltop_downhill", "04 꼭대기→바다"), ("06_coastal", "06 해안 직선")]
W, H, LBL, SIDE = 640, 360, 30, 150
font = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 20)
sheet = Image.new("RGB", (SIDE + W * len(SPOTS), LBL + H * len(MOODS)), (24, 24, 24))
d = ImageDraw.Draw(sheet)
for c, (_, sl) in enumerate(SPOTS):
    d.text((SIDE + c * W + 8, 3), sl, fill=(235, 235, 235), font=font)
for r, (mt, ml) in enumerate(MOODS):
    d.text((8, LBL + r * H + H // 2 - 12), ml, fill=(235, 235, 235), font=font)
    for c, (st, _) in enumerate(SPOTS):
        im = Image.open(D / f"{mt}_{st}.png").convert("RGB").resize((W, H), Image.LANCZOS)
        sheet.paste(im, (SIDE + c * W, LBL + r * H))
out = D / "sheet.png"
sheet.save(out)
print(out, sheet.size)
