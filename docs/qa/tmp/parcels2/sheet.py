# 0029: 필지 2판 전/후 7쌍 + 비탈 근접 4장 → 한 장(왼쪽 전 = 1판 · 오른쪽 후 = 2판)
from PIL import Image, ImageDraw, ImageFont
d = "D:/Github/F1Game/docs/qa/evidence/parcels2/"
pairs = [("dohyun", "도현 캡처 같은 각도"), ("8b98", "도현 카메라 8b98"), ("main_kart", "큰길 카트 높이"), ("garage", "차고 앞"),
         ("top_all", "맵 전체 위에서"), ("top_home", "집 둘레 위에서"), ("slope_eye", "비탈 마을 눈높이")]
close = [("C1096_stepped", "C 단차 집 C1096"), ("C602_stepped", "C 단차 집 C602"), ("B666_pad", "B 깎은 자리 B666"), ("B943_pad", "B 깎은 자리 B943")]
W, H, T = 800, 450, 28
rows = len(pairs) + 2
sheet = Image.new("RGB", (W * 2, (H + T) * rows), "white"); dr = ImageDraw.Draw(sheet)
f = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 18)
def put(path, x, y, label):
    sheet.paste(Image.open(path).convert("RGB").resize((W, H)), (x, y + T))
    dr.text((x + 8, y + 4), label, fill="black", font=f)
for i, (n, label) in enumerate(pairs):
    for j, tag in enumerate(["before", "after"]):
        put(f"{d}{n}_{tag}.png", j * W, i * (H + T), f"{label} — {'전 (1판)' if tag == 'before' else '후 (2판)'}")
for k, (n, label) in enumerate(close):
    put(f"{d}{n}_after.png", (k % 2) * W, (len(pairs) + k // 2) * (H + T), f"{label} — 후 (2판, 위 16 m)")
sheet.save(d + "sheet.png"); print(d + "sheet.png", sheet.size)
