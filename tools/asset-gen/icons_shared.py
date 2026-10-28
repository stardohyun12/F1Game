"""택배 보관소·우체국 공용 아이콘 아틀라스 icons.png (256×256, 컷아웃 알파). 사진 아님·AI 아님 — 인쇄물(간판 픽토그램·스티커·시계판·달력·전표)을 PIL 로 직접 그림.
    python -X utf8 tools/asset-gen/icons_shared.py   → art/export/q3-depot/icons.png, art/export/q4-postoffice/icons.png
칸(px, 왼쪽 위 기준): parcel 0,0,128,128 · clock 128,0,64,64 · fragile 192,0,64,32 · slip 192,32,32,48 · calendar 128,64,48,64 · ticket 176,64,48,24
"""
from pathlib import Path
from PIL import Image, ImageDraw
R = Path(__file__).resolve().parents[2]
CELLS = {'parcel': (0, 0, 128, 128), 'clock': (128, 0, 64, 64), 'fragile': (192, 0, 64, 32), 'slip': (192, 32, 32, 48), 'calendar': (128, 64, 48, 64), 'ticket': (176, 64, 48, 24)}
CREAM, DARK, KR_T, KR_L, KR_R, RED, WHITE, GRAY = (237, 227, 200), (58, 46, 40), (217, 168, 102), (176, 122, 64), (142, 94, 48), (192, 57, 43), (246, 243, 236), (150, 146, 138)
im = Image.new('RGBA', (256, 256), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
# 택배 픽토그램: 크림 판 + 끈 묶인 상자(위·왼·오른 면 크라프트 3톤) + 짙은 끈·매듭. 20m 에서 「상자」로 읽히게 큰 덩어리만.
d.rounded_rectangle((2, 2, 125, 125), 14, fill=CREAM, outline=DARK, width=6)
d.polygon([(64, 20), (106, 40), (64, 60), (22, 40)], fill=KR_T)
d.polygon([(22, 40), (64, 60), (64, 108), (22, 88)], fill=KR_L)
d.polygon([(64, 60), (106, 40), (106, 88), (64, 108)], fill=KR_R)
for a, b in (((43, 30), (85, 50)), ((85, 30), (43, 50))): d.line([a, b], fill=DARK, width=6)
d.line([(85, 50), (85, 98)], fill=DARK, width=6); d.line([(43, 50), (43, 98)], fill=DARK, width=6)
d.ellipse((54, 31, 64, 41), outline=DARK, width=4); d.ellipse((64, 31, 74, 41), outline=DARK, width=4)
d.line([(22, 40), (64, 60), (106, 40)], fill=DARK, width=2); d.line([(64, 60), (64, 108)], fill=DARK, width=2)
# 벽 시계
x, y = 128, 0; d.ellipse((x + 3, y + 3, x + 60, y + 60), fill=CREAM, outline=DARK, width=4)
for k in range(12):
    import math; a = k * math.pi / 6; r0 = 21 if k % 3 else 18
    d.line([(x + 32 + math.cos(a) * r0, y + 32 + math.sin(a) * r0), (x + 32 + math.cos(a) * 25, y + 32 + math.sin(a) * 25)], fill=DARK, width=2)
d.line([(x + 32, y + 32), (x + 21, y + 24)], fill=DARK, width=3); d.line([(x + 32, y + 32), (x + 44, y + 15)], fill=DARK, width=2)
# 깨지기 쉬움 스티커(빨강 바탕 + 흰 잔)
x, y = 192, 0; d.rectangle((x, y, x + 63, y + 31), fill=RED); d.rectangle((x + 2, y + 2, x + 61, y + 29), outline=WHITE, width=1)
d.polygon([(x + 8, y + 6), (x + 22, y + 6), (x + 19, y + 15), (x + 11, y + 15)], fill=WHITE); d.line([(x + 15, y + 15), (x + 15, y + 23)], fill=WHITE, width=2); d.line([(x + 10, y + 24), (x + 20, y + 24)], fill=WHITE, width=2)
for i in range(4): d.rectangle((x + 28, y + 8 + i * 5, x + 28 + (28 if i % 2 == 0 else 20), y + 10 + i * 5), fill=WHITE)
# 전표
x, y = 192, 32; d.rectangle((x, y, x + 31, y + 47), fill=WHITE, outline=GRAY)
for i in range(4): d.line([(x + 4, y + 8 + i * 7), (x + 27 - (i % 2) * 8, y + 8 + i * 7)], fill=GRAY, width=2)
for i in range(11): d.line([(x + 5 + i * 2, y + 38), (x + 5 + i * 2, y + 44)], fill=DARK if i % 3 else GRAY, width=1)
# 달력
x, y = 128, 64; d.rectangle((x, y + 4, x + 47, y + 63), fill=WHITE, outline=GRAY); d.rectangle((x, y + 4, x + 47, y + 17), fill=RED)
for i in (12, 34): d.rectangle((x + i, y, x + i + 2, y + 8), fill=DARK)
for r in range(5):
    for c in range(6): d.rectangle((x + 4 + c * 7, y + 22 + r * 8, x + 7 + c * 7, y + 25 + r * 8), fill=RED if (r, c) == (2, 4) else DARK if c < 5 else RED)
# 보관소는 번호표를 안 쓴다 → 번호표 없이 저장(번호표 칸이 전표 아래 절반 행 64~79 를 덮어 바코드가 가려짐, 2058). 우체국은 그대로(같은 바이트).
im.save(R / 'art/export/q3-depot/icons.png'); print('icons', 'art/export/q3-depot (번호표 없음)')
# 번호표 표시기(검은 판 + 빨간 숫자 블록 0 7)
x, y = 176, 64; d.rectangle((x, y, x + 47, y + 23), fill=DARK)
for rect in ((6, 4, 18, 6), (6, 17, 18, 19), (6, 4, 8, 19), (16, 4, 18, 19), (26, 4, 40, 6), (37, 4, 40, 19)): d.rectangle((x + rect[0], y + rect[1], x + rect[2], y + rect[3]), fill=RED)
# 우체국 전표(2251): 최종 88px 아틀라스 크기로 직접 그린 11×17 2색(종이·잉크) 비트맵. 회색 중간톤이 글씨 가장자리 AA 40% 를 만들던 것.
# 88 로 줄일 때 Nearest 가 고르는 원본 픽셀 = 그 목표 픽셀의 비트맵 값이 되게 원본 32×48 칸을 채운다(새 색·번짐 0).
TGT = 88; SLIP = ['DDDDDDDDDDD',
                  'D.........D',
                  'D.DDDDDDD.D',
                  'D.........D',
                  'D.DDDDD...D',
                  'D.........D',
                  'D.DDDDDDD.D',
                  'D.........D',
                  'D.DDDD....D',
                  'D.........D',
                  'D.........D',
                  'D.D.DD.D..D',
                  'D.D.DD.D..D',
                  'D.D.DD.D..D',
                  'D.........D',
                  'D.........D',
                  'DDDDDDDDDDD']
x, y, w, h = CELLS['slip']; tx0, ty0 = x * TGT // 256, y * TGT // 256
for sy in range(y, y + h):
    for sx in range(x, x + w):
        bx = min(max(sx * TGT // 256 - tx0, 0), 10); by = min(max(sy * TGT // 256 - ty0, 0), 16)
        im.putpixel((sx, sy), DARK + (255,) if SLIP[by][bx] == 'D' else WHITE + (255,))
for out in ('art/export/q4-postoffice',): im.save(R / out / 'icons.png'); print('icons', out)
