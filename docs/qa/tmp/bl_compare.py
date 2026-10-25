"""v1 ↔ v2 같은 지점 좌우 비교 시트 → docs/qa/evidence/bayloop-v2/compare.png (01·02·05·06)."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]
V1, V2 = ROOT / "docs/qa/evidence/bayloop-v1", ROOT / "docs/qa/evidence/bayloop-v2"
SHOTS = ["01_harbor_straight", "02_shop_street_climb", "05_slope_village", "06_coastal_straight"]
W, H, LBL = 960, 540, 28
font = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 18)
sheet = Image.new("RGB", (W * 2, (H + LBL) * len(SHOTS)), (24, 24, 24))
d = ImageDraw.Draw(sheet)
for r, s in enumerate(SHOTS):
    y = r * (H + LBL)
    for c, (folder, tag) in enumerate([(V1, "v1 (폴백 건물)"), (V2, "v2 (아트 건물)")]):
        im = Image.open(folder / f"{s}.png").convert("RGB").resize((W, H), Image.NEAREST)
        sheet.paste(im, (c * W, y + LBL))
        d.text((c * W + 8, y + 4), f"{s} — {tag}", fill=(235, 235, 235), font=font)
out = V2 / "compare.png"
sheet.save(out)
print(out, sheet.size)
