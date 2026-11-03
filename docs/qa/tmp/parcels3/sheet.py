# 0141: 필지 3판 비교 — 왼쪽 2판(parcels2/*_after) · 오른쪽 3판(parcels3/*_3pan), 마지막 줄 언덕 눈높이
from PIL import Image, ImageDraw, ImageFont
d2 = "D:/Github/F1Game/docs/qa/evidence/parcels2/"
d3 = "D:/Github/F1Game/docs/qa/evidence/parcels3/"
pairs = [("dohyun", "도현 캡처 같은 각도"), ("8b98", "도현 카메라 8b98"), ("main_kart", "큰길 카트 높이"), ("garage", "차고 앞"),
         ("top_all", "맵 전체 위에서"), ("top_home", "집 둘레 위에서"), ("slope_eye", "비탈 마을 눈높이")]
W, H, T = 800, 450, 28
rows = len(pairs) + 1
sheet = Image.new("RGB", (W * 2, (H + T) * rows), "white"); dr = ImageDraw.Draw(sheet)
f = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 18)
def put(path, x, y, label):
    sheet.paste(Image.open(path).convert("RGB").resize((W, H)), (x, y + T))
    dr.text((x + 8, y + 4), label, fill="black", font=f)
for i, (n, label) in enumerate(pairs):
    put(f"{d2}{n}_after.png", 0, i * (H + T), f"{label} — 2판")
    put(f"{d3}{n}_3pan.png", W, i * (H + T), f"{label} — 3판")
put(f"{d3}hill_eye_3pan.png", 0, len(pairs) * (H + T), "언덕 반지하·2단 눈높이 — 3판 (새 각도)")
sheet.save(d3 + "sheet.png"); print(d3 + "sheet.png", sheet.size)
