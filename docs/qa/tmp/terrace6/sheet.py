# terrace6: 위 직교 1장에 걷는 길(walk.cs 구간점)과 컷 자리(번호)를 그리고, 눈높이 6컷 + 위 1장 시트를 만든다.
from PIL import Image, ImageDraw, ImageFont
D = "D:/Github/F1Game/docs/qa/evidence/terrace6/"
def P(x, z):  # top_raw: 중심 (215,-64), 80x45 m, 20 px/m, 오른쪽=+x, 위=+z
    return ((x - 175) * 20, (-41.5 - z) * 20)
route = [(184.8, -66.16), (194.6, -68.3), (196.3, -68.6), (197.0, -76.9), (197.3, -77.9), (200, -77.4), (205, -76.5),
         (210, -75.6), (214.3, -74.9), (215.6, -74.6), (215.6, -73.7), (215.4, -65.0), (222, -64.0), (230, -63.3),
         (229.5, -58), (226, -55)]
stairs = [((196.3, -68.6), (197.0, -76.9)), ((215.6, -73.7), (215.4, -65.0))]
cuts = [("1", 184.8, -66.16), ("2", 197.6, -77.6), ("3", 206, -76.4), ("4", 215.6, -75.2), ("5", 215.6, -64.3), ("6", 231.5, -62.0)]
try:
    F = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 26); Fs = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 22)
except Exception:
    F = Fs = ImageFont.load_default()
top = Image.open(D + "top_raw.png").convert("RGB"); d = ImageDraw.Draw(top)
d.line([P(*p) for p in route], fill=(255, 230, 0), width=7, joint="curve")
for a, b in stairs:
    d.line([P(*a), P(*b)], fill=(255, 60, 200), width=9)
for n, x, z in cuts:
    cx, cy = P(x, z); d.ellipse([cx - 16, cy - 16, cx + 16, cy + 16], fill=(20, 20, 20)); d.text((cx - 7, cy - 16), n, fill=(255, 255, 255), font=F)
d.rectangle([0, 0, 760, 74], fill=(0, 0, 0))
d.text((10, 6), "노랑 = 걷는 길(대문 → 집 계단 → 보도 → 차고 계단 → 골목 → 차고 앞)", fill=(255, 230, 0), font=Fs)
d.text((10, 40), "분홍 = 돌계단 2개 · 숫자 = 눈높이 컷 자리 · 20 px = 1 m", fill=(255, 140, 220), font=Fs)
top.save(D + "top_route.png")
names = [("w1_gate.png", "1 대문 → 집 계단 쪽"), ("w2_stair_foot.png", "2 집 계단 아래 → 보도 동쪽"), ("w3_walk_mid.png", "3 보도 중간 → 차고 계단"),
         ("w4_garage_stair.png", "4 차고 계단 발치(길 건널 곳 대신)"), ("w5_alley.png", "5 계단 머리 → 골목 포장 띠"), ("w6_garage_door.png", "6 차고 입구(앞 마당)")]
W, H = 640, 360
sheet = Image.new("RGB", (W * 3, H * 2 + 900 * W * 3 // 1600), (30, 30, 30))
for i, (f, t) in enumerate(names):
    im = Image.open(D + f).convert("RGB").resize((W, H)); dd = ImageDraw.Draw(im)
    dd.rectangle([0, 0, W, 34], fill=(0, 0, 0)); dd.text((8, 2), t, fill=(255, 255, 255), font=Fs)
    sheet.paste(im, ((i % 3) * W, (i // 3) * H))
tw = W * 3; th = 900 * tw // 1600
sheet.paste(top.resize((tw, th)), (0, H * 2))
sheet.save(D + "sheet.png")
print("ok", sheet.size)
