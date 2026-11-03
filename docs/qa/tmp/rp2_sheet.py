# 2300: 전/후 4쌍 → 한 장(왼쪽 전 · 오른쪽 후)
from PIL import Image, ImageDraw
d = "D:/Github/F1Game/docs/qa/evidence/roadpaint2/"
names = [("8b98", "도현 카메라 8b98"), ("yard", "마당 눈높이"), ("main_kart", "큰길 카트 높이"), ("lane", "골목")]
W, H, T = 800, 450, 28
sheet = Image.new("RGB", (W * 2, (H + T) * len(names)), "white"); dr = ImageDraw.Draw(sheet)
try:
    from PIL import ImageFont; f = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 18)
except Exception:
    f = None
for i, (n, label) in enumerate(names):
    for j, tag in enumerate(["before", "after"]):
        im = Image.open(f"{d}{n}_{tag}.png").convert("RGB").resize((W, H))
        sheet.paste(im, (j * W, i * (H + T) + T))
        dr.text((j * W + 8, i * (H + T) + 4), f"{label} — {'전' if tag == 'before' else '후(길가 소품)'}", fill="black", font=f)
sheet.save(d + "sheet.png"); print(d + "sheet.png", sheet.size)
