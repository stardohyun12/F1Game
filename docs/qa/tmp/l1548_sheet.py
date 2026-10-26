# 1548: look-3way 장면 3 × 안 4 → sheet.png(칸 960 폭, 칸 위 안 이름) + 장면별 큰 판(2×2, 1280 칸)
from PIL import Image, ImageDraw, ImageFont
import os, sys
D = os.path.join(os.path.dirname(__file__), '..', 'evidence', 'look-3way')
NAMES = ['0 Current', '1 Clean', '2 Painterly', '3 Pixel-Soft']
SCENES = [('a_car', '(a) 해안도로 차 시점 · 카메라 B'), ('b_street', '(b) 상점가 인도 사람'), ('c_coast', '(c) 위에서 본 해안 비탈')]
f = ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf', 30)
W, H, LB = 960, 540, 44
sheet = Image.new('RGB', (W * 4, (H + LB) * 3), (24, 24, 24)); d = ImageDraw.Draw(sheet)
for r, (key, title) in enumerate(SCENES):
    big = Image.new('RGB', (1280 * 2, (720 + LB) * 2), (24, 24, 24)); bd = ImageDraw.Draw(big)
    for c, nm in enumerate(NAMES):
        im = Image.open(os.path.join(D, f'{key}_{c}.png')).convert('RGB')
        y = r * (H + LB)
        sheet.paste(im.resize((W, H), Image.LANCZOS), (c * W, y + LB))
        d.text((c * W + 12, y + 6), f'{nm}   {title}' if c == 0 else nm, fill=(255, 255, 255), font=f)
        bx, by = (c % 2) * 1280, (c // 2) * (720 + LB)
        big.paste(im, (bx, by + LB)); bd.text((bx + 12, by + 6), f'{nm}   {title}', fill=(255, 255, 255), font=f)
    big.save(os.path.join(D, f'big_{key}.png'))
sheet.save(os.path.join(D, 'sheet.png'))
print('ok')
