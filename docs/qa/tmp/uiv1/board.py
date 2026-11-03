"""지금 UI | UI 1판 비교판 — 왼쪽 예전 캡처(기능 2 증거), 오른쪽 이번 캡처(360 RT 를 Nearest 3배)."""
from PIL import Image, ImageDraw, ImageFont
F2 = 'docs/qa/evidence/feature2/'
V1 = 'docs/qa/evidence/ui-v1/'
rows = [
    ('운전', F2 + 'f2_05_hud_target.png', V1 + 'drive_01.png'),
    ('운전 · 전표', F2 + 'f2_05_hud_target.png', V1 + 'drive_slip.png'),
    ('[E] 칩', F2 + 'f2_02_boss_prompt.png', V1 + 'chip_1_boss.png'),
    ('[E] 칩 · 카트', F2 + 'f2_04_beside_cart.png', V1 + 'chip_2_cart.png'),
    ('대화', F2 + 'f2_03_talk_2.png', V1 + 'talk_offscreen_05.png'),
]
CW, CH = 960, 540
font = ImageFont.truetype('F1/Assets/UI/Fonts/Galmuri/Galmuri11.ttf', 24)
LH = 40
sheet = Image.new('RGB', (CW * 2 + 12, LH + len(rows) * (CH + LH)), (18, 18, 18))
d = ImageDraw.Draw(sheet)
d.text((10, 8), '지금 UI', font=font, fill=(255, 255, 255))
d.text((CW + 22, 8), 'UI 1판 (도트 화면 안)', font=font, fill=(255, 255, 255))
for i, (label, a, b) in enumerate(rows):
    y = LH + i * (CH + LH)
    d.text((10, y + 8), label, font=font, fill=(255, 220, 120))
    ia = Image.open(a).convert('RGB').resize((CW, CH), Image.BILINEAR)
    ib = Image.open(b).convert('RGB').resize((CW, CH), Image.NEAREST)
    sheet.paste(ia, (0, y + LH)); sheet.paste(ib, (CW + 12, y + LH))
sheet.save(V1 + 'compare_now_vs_v1.png')
print(sheet.size)
