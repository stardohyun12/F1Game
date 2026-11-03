# 2356: 필지 전/후 5쌍 → 한 장(왼쪽 전 = OSM 회색 · 오른쪽 후 = 필지 덩어리)
from PIL import Image, ImageDraw, ImageFont
d = "D:/Github/F1Game/docs/qa/evidence/parcels1/"
names = [("8b98", "도현 카메라 8b98"), ("main_kart", "큰길 카트 높이"), ("garage", "차고 앞"), ("top120", "120 m 위에서"), ("yard", "마당 눈높이")]
W, H, T = 800, 450, 28
sheet = Image.new("RGB", (W * 2, (H + T) * len(names)), "white"); dr = ImageDraw.Draw(sheet)
f = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 18)
for i, (n, label) in enumerate(names):
    for j, tag in enumerate(["before", "after"]):
        im = Image.open(f"{d}{n}_{tag}.png").convert("RGB").resize((W, H))
        sheet.paste(im, (j * W, i * (H + T) + T))
        dr.text((j * W + 8, i * (H + T) + 4), f"{label} — {'전 (OSM 회색)' if tag == 'before' else '후 (필지 회색 덩어리)'}", fill="black", font=f)
sheet.save(d + "sheet.png"); print(d + "sheet.png", sheet.size)
