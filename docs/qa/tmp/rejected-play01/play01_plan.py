"""과제 2329: Proto_Play01 평면 캡처(plan_raw.png)에 배달점 번호·차고·큰길/지름길 표시·범례를 얹어 plan.png 로.
카메라: 직교 size 218, 중심 (5, -17), 960x540 → 월드 x = 5 ± 387.6, z = -17 ± 218.
"""
import sys
from PIL import Image, ImageDraw, ImageFont

src, dst = sys.argv[1], sys.argv[2]
im = Image.open(src).convert("RGB")
d = ImageDraw.Draw(im)
HW, HH, CX, CZ = 218 * 16 / 9, 218, 5, -17
def px(x, z):
    return ((x - (CX - HW)) / (2 * HW) * 960, (CZ + HH - z) / (2 * HH) * 540)

try:
    f = ImageFont.truetype("C:/Windows/Fonts/malgunbd.ttf", 18)
    fs = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 13)
except OSError:
    f = fs = ImageFont.load_default()

pts = {"G": (0, -210), "1": (-160, 40), "2": (130, -105), "3": (40, -40), "4": (190, 185)}
names = {"G": "차고", "1": "1 유리병", "2": "2 상자", "3": "3 계란", "4": "4 상자"}
for k, (x, z) in pts.items():
    u, v = px(x, z)
    col = (40, 90, 200) if k == "G" else ((220, 60, 50) if k in "13" else (40, 120, 220))
    d.ellipse([u - 11, v - 11, u + 11, v + 11], fill=col, outline=(255, 255, 255), width=2)
    d.text((u, v), "G" if k == "G" else k, fill=(255, 255, 255), font=f, anchor="mm")
    d.text((u + 14, v - 8), names[k], fill=(20, 20, 20), font=fs)

# 경로 이름(큰길=M, 지름길=S) — 대표 지점에 작은 글씨
tags = [((-200, -95), "1 큰길(헤어핀)"), ((-75, -110), "1 지름길(흙·턱·급경사)"), ((200, -212), "2·3 큰길(해안 직선)"),
        ((65, -165), "2 지름길(골목)"), ((-15, -100), "3·4 지름길(돌길)"), ((5, 0), "4 지름길(오르막 골목)"),
        ((-50, 75), "4 큰길(고원)"), ((140, 155), "언덕 넘어 내리막")]
for (x, z), t in tags:
    u, v = px(x, z)
    w = d.textlength(t, font=fs)
    d.rectangle([u - 2, v - 1, u + w + 2, v + 15], fill=(255, 255, 255))
    d.text((u, v), t, fill=(30, 30, 30), font=fs)

# 범례
lg = [((158, 158, 158), "해안 큰길"), ((133, 133, 133), "큰길"), ((199, 153, 82), "흙 지름길"), ((140, 97, 61), "돌길 요철"), ((178, 143, 92), "골목(벽 진회색)")]
d.rectangle([8, 8, 150, 20 + 18 * len(lg)], fill=(255, 255, 255))
for i, (c, t) in enumerate(lg):
    d.rectangle([14, 14 + 18 * i, 30, 26 + 18 * i], fill=c, outline=(0, 0, 0))
    d.text((36, 12 + 18 * i), t, fill=(0, 0, 0), font=fs)
im.save(dst)
print("ok", dst)
