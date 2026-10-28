# artlive 1653: 정비소 소품 아틀라스 garage_props.png(256x512, 64px/m) + 게시판 garage_festival.png(128x128 = 2m, 픽셀 글씨)
# 재료는 전부 CC0 사진(Poly Haven) 결 → 64px/m 로 줄임. 빛·그림자·AO 를 굽지 않음. 공구 모양은 손으로 찍은 픽셀(AI 아님).
# python -X utf8 art/source/q2-garage/props_1653.py
import random
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageOps
R = Path(__file__).resolve().parents[3]; E = R / 'art/export/q2-garage'
PH = R / 'art/source/car-v01/photos'
WOOD = R / 'art/textures/2026-10-24-kp/sources/wood/wood_plank_wall_diff_1k.png'
FONT = R / 'art/_review/2026-10-23-course-ui/fonts/Galmuri11.ttf'
rnd = random.Random(1653)

def photo(p, box, size):  # 사진 일부 → 목표 픽셀 수(64px/m)로 줄임
    return Image.open(p).convert('RGB').crop(box).resize(size, Image.LANCZOS)
def recolor(im, rgb, amp=1.0):  # 사진 밝기 결만 남기고 색은 칠 색으로(녹 색을 지움)
    g = ImageOps.grayscale(im); px = list(g.getdata()); m = sum(px) / len(px)
    out = Image.new('RGB', im.size)
    out.putdata([tuple(max(0, min(255, int(c * (1 + amp * (v / m - 1))))) for c in rgb) for v in px]); return out

A = Image.new('RGB', (256, 512), (128, 128, 128))
# ── 페그보드 (0,0)-(208,84) = 3.25m x 1.31m: 하드보드 결(나무 사진 밝기) + 6cm 간격 구멍
peg = recolor(photo(WOOD, (0, 0, 1024, 414), (208, 84)), (176, 138, 96), 0.55)
dp = ImageDraw.Draw(peg)
for y in range(2, 84, 4):
    for x in range(2, 208, 4): dp.point((x, y), fill=(84, 62, 44))
metal = recolor(photo(PH / 'rusty_metal_02_diff_1k.jpg', (300, 300, 556, 556), (64, 64)), (158, 160, 164), 0.5)
handle = recolor(photo(WOOD, (500, 0, 756, 256), (64, 64)), (150, 98, 58), 0.6)
grip_r = recolor(photo(PH / 'rusty_painted_metal_diff_1k.jpg', (0, 0, 256, 256), (64, 64)), (176, 52, 40), 0.35)
grip_y = recolor(photo(PH / 'rusty_painted_metal_diff_1k.jpg', (0, 0, 256, 256), (64, 64)), (206, 160, 52), 0.35)
def put(x, y, src): peg.putpixel((x, y), src.getpixel((x % 64, y % 64)))
def rect(x0, y0, x1, y1, src):
    for y in range(y0, y1):
        for x in range(x0, x1): put(x, y, src)
def hook(x, y): dp.point((x, y), fill=(60, 60, 64)); dp.point((x, y + 1), fill=(60, 60, 64))
# 공구(세로로 걸림, 위쪽이 머리). 크기 = 실물 cm x 0.64
def wrench(x, y, L):  # 콤비 렌치: 위 고리, 2px 자루, 아래 벌어진 입
    hook(x + 2, y - 2); rect(x, y, x + 5, y + 4, metal); peg.putpixel((x + 2, y + 1), peg.getpixel((x + 2, y - 4))); peg.putpixel((x + 2, y + 2), (84, 62, 44))
    rect(x + 1, y + 4, x + 4, y + L - 4, metal); rect(x, y + L - 4, x + 5, y + L, metal); rect(x + 2, y + L - 3, x + 3, y + L, peg.copy() if False else handle) if False else None
    for yy in range(y + L - 2, y + L): put(x + 2, yy, Image.new('RGB', (64, 64), (176, 138, 96)))
def hammer(x, y, L):  # 쇠 머리 9x3 + 나무 자루 2px
    hook(x + 4, y - 2); rect(x, y, x + 9, y + 3, metal); rect(x + 7, y - 1, x + 9, y, metal); rect(x + 3, y + 3, x + 5, y + L, handle)
def screwdriver(x, y, L, grip):  # 손잡이 3px(빨강·노랑 플라스틱) + 1px 쇠 대
    hook(x + 1, y - 2); rect(x, y, x + 3, y + 7, grip); rect(x + 1, y + 7, x + 2, y + L, metal)
def spanner(x, y, L):  # 몽키 스패너: 머리 6x5 + 홈 + 자루
    hook(x + 3, y - 2); rect(x, y, x + 6, y + 5, metal); rect(x + 2, y, x + 4, y + 2, Image.new('RGB', (64, 64), (176, 138, 96))); rect(x + 2, y + 5, x + 4, y + L, metal)
def saw(x, y):  # 톱: 나무 손잡이 7x6(구멍) + 쇠날 30px, 아래 톱니
    hook(x + 3, y - 2); rect(x, y, x + 7, y + 6, handle); dp.point((x + 3, y + 2), fill=(84, 62, 44)); dp.point((x + 4, y + 2), fill=(84, 62, 44))
    for i in range(30):
        h = 6 - i // 8
        rect(x + 7 + i, y, x + 8 + i, y + h, metal)
        if i % 2 == 0: dp.point((x + 7 + i, y + h), fill=(110, 112, 116))
# 배치(불규칙 간격·크기). 벤치 위쪽 가운데에 몰리고 양끝은 듬성
x = 9
for kind in ['wrench', 'wrench', 'hammer', 'screwdriver', 'wrench', 'saw', 'screwdriver', 'spanner', 'wrench', 'hammer', 'screwdriver', 'spanner', 'wrench']:
    y = 30 + rnd.randint(-4, 6)
    if kind == 'wrench': L = rnd.choice([12, 15, 17, 20]); wrench(x, y, L); x += 6 + rnd.randint(3, 8)
    elif kind == 'hammer': hammer(x, y, rnd.choice([18, 21])); x += 10 + rnd.randint(3, 7)
    elif kind == 'screwdriver': screwdriver(x, y, rnd.choice([12, 15, 18]), rnd.choice([grip_r, grip_y])); x += 3 + rnd.randint(4, 8)
    elif kind == 'spanner': spanner(x, y, rnd.choice([16, 19])); x += 7 + rnd.randint(4, 9)
    elif kind == 'saw': saw(x, y + 2); x += 38 + rnd.randint(3, 6)
    if x > 196: break
A.paste(peg, (0, 0))
# ── 등 발광 칸 (224,0)-(256,16): 따뜻한 흰색(발광이라 빛을 굽는 게 아님)
A.paste((255, 228, 172), (224, 0, 256, 16))
# ── 타이어 트레드 (0,88)-(128,120) = 2m x 0.5m: 고무 사진 + 4px 블록 홈
tread = recolor(photo(PH / 'rubberized_track_diff_1k.jpg', (0, 0, 1024, 256), (128, 32)), (52, 52, 54), 0.6)
dt = ImageDraw.Draw(tread)
for x in range(0, 128, 4):
    for y in range(3, 29):
        if (y // 4 + x // 4) % 2 == 0 and y % 4 == 0: dt.point((x, y), fill=(30, 30, 32)); dt.point((x + 1, y), fill=(30, 30, 32))
    dt.line((x + 2, 4, x + 2, 27), fill=(36, 36, 38))
A.paste(tread, (0, 88))
# ── 타이어 옆(위 뚜껑) (128,88)-(192,152): 고무 결 + 둘레 1px 띠
side = recolor(photo(PH / 'rubberized_track_diff_1k.jpg', (512, 512, 1024, 1024), (64, 64)), (58, 58, 60), 0.5)
ImageDraw.Draw(side).ellipse((8, 8, 55, 55), outline=(46, 46, 48)); ImageDraw.Draw(side).ellipse((18, 18, 45, 45), fill=(34, 34, 36))
A.paste(side, (128, 88))
# ── 드럼 뚜껑 쇠 (192,88)-(256,152)
A.paste(recolor(photo(PH / 'rusty_metal_02_diff_1k.jpg', (600, 100, 856, 356), (64, 64)), (128, 130, 132), 0.45), (192, 88))
# ── 드럼 칠 (0,156)-(128,220) 빨강, (128,156)-(256,220) 파랑 = 2m x 1m, 가로 테 2줄
for i, rgb in enumerate([(150, 52, 40), (52, 92, 132)]):
    pm = recolor(photo(PH / 'rusty_painted_metal_diff_1k.jpg', (0, 512 * i, 1024, 512 * i + 512), (128, 64)), rgb, 0.22)
    d = ImageDraw.Draw(pm)
    for y in (20, 42): d.line((0, y, 127, y), fill=tuple(int(c * 0.8) for c in rgb)); d.line((0, y + 1, 127, y + 1), fill=tuple(min(255, int(c * 1.12)) for c in rgb))
    A.paste(pm, (128 * i, 156))
# ── 벽 아래 판자 (0,224)-(256,304) = 4m x 1.25m: 나무 사진 결, 짙게(원래 planks_dark 색에 맞춤), 판자 이음 1px
pl = recolor(photo(WOOD, (0, 0, 1024, 320), (256, 80)), (104, 82, 58), 0.7)
dl = ImageDraw.Draw(pl)
for xx in range(30, 256, 88): dl.line((xx, 0, xx, 79), fill=(70, 54, 38))
dl.line((0, 0, 255, 0), fill=(70, 54, 38))
A.paste(pl, (0, 224))
A.save(E / 'garage_props.png'); print('props', A.size)

# ── 게시판 128x128 = 2m: 종이 사진 결 + 픽셀 글꼴(Galmuri11, 안티앨리어싱 끔) + 글자마다 1px 들쭉(손으로 쓴 느낌)
paper = recolor(photo(PH / 'rough_linen_diff_1k.jpg', (0, 0, 512, 512), (128, 128)), (226, 214, 186), 0.35)
d = ImageDraw.Draw(paper); d.fontmode = '1'; f = ImageFont.truetype(str(FONT), 12)
def write(x, y, s, fill, scale=1):
    for ch in s:
        if ch == ' ': x += 3 * scale; continue
        w = int(d.textlength(ch, font=f)); g = Image.new('L', (w + 2, 16), 0); gd = ImageDraw.Draw(g); gd.fontmode = '1'; gd.text((0, 0), ch, font=f, fill=255)
        if scale > 1: g = g.resize((g.width * scale, g.height * scale), Image.NEAREST)
        paper.paste(Image.new('RGB', g.size, fill), (x, y + rnd.choice([0, 0, 1])), g); x += w * scale + (1 if scale == 1 else 0)
write(5, 6, 'BAY LOOP', (66, 82, 87), 2)
write(10, 34, 'AUTUMN 26', (113, 57, 40))
for xx in range(8, 120): d.point((xx, 52 + (1 if (xx // 13) % 3 == 1 else 0)), fill=(66, 82, 87)); d.point((xx, 53 + (1 if (xx // 13) % 3 == 1 else 0)), fill=(66, 82, 87))
write(8, 60, '대회 참가 신청', (57, 61, 55))
write(8, 82, 'REGISTER HERE', (57, 61, 55))
write(8, 102, 'PIT GARAGE', (113, 57, 40))
for i in range(128):  # 손으로 그은 테두리(1~2px 들쭉)
    for (xx, yy) in [(i, 1), (i, 126), (1, i), (126, i)]: paper.putpixel((xx, yy), (115, 91, 65))
    if rnd.random() < 0.4: paper.putpixel((i, 2), (115, 91, 65))
paper.save(E / 'garage_festival.png'); print('festival', paper.size)
