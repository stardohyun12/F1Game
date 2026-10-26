"""정비소·고카트 도트 텍스처 (D-163 도트 기반 로우폴리). PIL 만 — 사진·노이즈·그라데이션·AI 없음.
16px = 1m. 한 장 3~6색. 녹·얼룩·테이프·포스터는 아래 ASCII 도장(sprite)을 손으로 정한 자리에 찍는다.

    python -X utf8 tools/asset-gen/opus_pixel_tex.py
"""
import os
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'art', 'source', 'opus-garage-kart', 'tex')
PPM = 16


def hx(h):
    h = h.lstrip('#'); return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


class C:
    def __init__(self, w, h, pal, base=0):
        self.pal = [hx(p) for p in pal]; self.im = Image.new('RGB', (w, h), self.pal[base]); self.w, self.h = w, h

    def p(self, x, y, k):
        if 0 <= x < self.w and 0 <= y < self.h: self.im.putpixel((x % self.w, y % self.h), self.pal[k])

    def rect(self, x0, y0, x1, y1, k):  # 끝 포함
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1): self.p(x, y, k)

    def stamp(self, x0, y0, rows, keys):  # rows: 문자열 줄, keys: 문자→색 번호('.'=건너뜀)
        for j, r in enumerate(rows):
            for i, ch in enumerate(r):
                if ch in keys: self.p(x0 + i, y0 + j, keys[ch])


RUST_A = ['.rr.', 'rRRr', '.rRr', '..r.']
RUST_B = ['rr..r', 'rRrrR', '.rRR.', '..r..']
DRIP = ['r', 'R', 'r', 'r', '.', 'r']
OIL = ['..oo..', '.oOOo.', 'oOOOOo', '.oOOo.', '..o...']
OIL_S = ['.oo', 'oOo', '.o.']


def tin(name, pal, seed_rust):
    """함석 64×48(4m×3m): 2px 마다 골, 1m 마다 이음매+리벳, 아래 녹 띠는 열마다 손으로 정한 높이."""
    c = C(64, 48, pal)  # 0 바탕 1 골 밝음 2 이음매 3 녹 4 녹 짙음
    for x in range(64):
        if x % 4 == 1: c.rect(x, 0, x, 47, 1)
        if x % 16 == 15: c.rect(x, 0, x, 47, 2)
    for x in (15, 31, 47, 63):
        for y in (3, 15, 27, 39): c.p(x - 1, y, 2)
    band = [2, 3, 3, 2, 1, 1, 2, 4, 5, 4, 2, 2, 1, 0, 0, 1, 2, 3, 2, 2, 3, 3, 4, 3, 2, 1, 1, 1, 2, 2, 3, 6,
            5, 3, 2, 1, 1, 0, 1, 2, 2, 2, 3, 2, 1, 1, 2, 3, 4, 4, 3, 2, 1, 1, 1, 2, 3, 3, 2, 2, 1, 1, 2, 2]
    for x, b in enumerate(band[seed_rust:] + band[:seed_rust]):
        for y in range(48 - b, 48): c.p(x, y, 4 if y > 46 - b // 2 else 3)
    for x, y in ((13, 4), (29, 16), (45, 28), (61, 4)):  # 리벳 아래 녹물
        c.stamp(x, y, DRIP, {'r': 3, 'R': 4})
    c.stamp(22, 20, RUST_A, {'r': 3, 'R': 4}); c.stamp(52, 9, RUST_B, {'r': 3, 'R': 4})
    c.save = lambda: c.im.save(os.path.join(OUT, name + '.png')); return c


def planks(name, pal, patches):
    """판자벽 64×48: 판 높이 4px(25cm), 판마다 이음 자리 다름, 못 2개, 덧댄 판은 다른 색."""
    c = C(64, 48, pal)  # 0 나무 1 밝은 결 2 틈 3 못 4 바랜 칠 5 새 판자
    joints = [9, 41, 23, 55, 3, 35, 17, 49, 29, 61, 13, 45]
    for i in range(12):
        y = i * 4
        c.rect(0, y + 3, 63, y + 3, 2)
        j = joints[i]; c.rect(j, y, j, y + 2, 2); c.p(j + 1, y + 1 + (i % 2), 3)  # 못 하나(이음 옆, 줄마다 높이 다름)
        c.rect((j + 7) % 64, y + 1, (j + 7) % 64 + 5, y + 1, 1)
    for x0, y0, w in ((4, 8, 12), (30, 8, 9), (40, 28, 14), (6, 36, 8)):  # 옛 칠 남은 자리
        c.rect(x0, y0, x0 + w, y0 + 2, 4); c.p(x0 + w // 2, y0 + 1, 0); c.p(x0 + 2, y0 + 2, 0)
    for x0, row in patches:  # 덧댄 새 판자
        y = row * 4 + 1; c.rect(x0, y, x0 + 10, y + 3, 5); c.p(x0, y + 1, 3); c.p(x0 + 10, y + 1, 3); c.p(x0, y + 2, 3); c.p(x0 + 10, y + 2, 3)
    c.save = lambda: c.im.save(os.path.join(OUT, name + '.png')); return c


def floor():
    c = C(64, 64, ['#b8ae9c', '#a59c8b', '#8a8274', '#6c6760', '#55524c'])  # 콘크리트·금·기름
    for x in range(0, 64, 32):
        c.rect(x, 0, x, 63, 1)
    c.rect(0, 31, 63, 31, 1)
    for x, y in ((5, 9), (6, 10), (7, 10), (8, 11), (9, 11), (10, 12), (40, 44), (41, 45), (41, 46), (42, 47), (43, 47)):
        c.p(x, y, 2)
    for x, y in ((14, 18), (44, 12), (26, 46)): c.stamp(x, y, OIL, {'o': 3, 'O': 4})
    for x, y in ((50, 30), (8, 54), (33, 6)): c.stamp(x, y, OIL_S, {'o': 2, 'O': 3})
    for y in range(64):  # 타이어 자국 두 줄
        if y % 6 < 4: c.p(20, y, 1); c.p(29, y, 1)
    return c


def grass():
    """풀밭 64×64: 자연은 진하게(Look 6) — 바탕 올리브, 밝은 풀 포기, 짙은 그늘 포기, 꽃 점 몇 개."""
    c = C(64, 64, ['#66784a', '#7d8c56', '#4a6248', '#e0cfb0', '#c98a5e'])
    tufts = ((4, 6), (19, 3), (33, 11), (50, 5), (9, 22), (27, 25), (44, 20), (58, 28), (14, 40), (31, 37), (48, 44),
             (6, 55), (22, 58), (39, 52), (56, 60), (61, 12))
    for k, (x, y) in enumerate(tufts):
        c.stamp(x, y, ['.1.', '1.1'] if k % 2 else ['1.1', '.1.'], {'1': 1}); c.stamp(x + 3, y + 2, ['2', '2'], {'2': 2})
    for x, y, k in ((12, 13, 3), (40, 30, 4), (25, 48, 3), (54, 50, 3), (3, 34, 4)): c.p(x, y, k)
    return c


def ground():
    c = C(64, 64, ['#c9b994', '#b8a782', '#d8caa6', '#9c9070', '#8f9a5e'])  # 흙길·자갈·풀
    for x, y in ((3, 4), (17, 9), (40, 3), (55, 14), (9, 26), (30, 21), (47, 33), (60, 44), (21, 40), (36, 55), (5, 50), (51, 58)):
        c.rect(x, y, x + 1, y, 1); c.p(x, y - 1, 2); c.p(x + 1, y + 1, 3)
    for x, y in ((10, 60), (12, 59), (13, 61), (58, 2), (60, 3), (25, 31), (26, 30)): c.p(x, y, 4)
    return c


def leaf():
    """나무 덩어리 32×32: 짙은 초록 바탕 + 밝은 잎 뭉치 + 가장 짙은 속 그늘(색 있는 그늘 #2c4a40)."""
    c = C(32, 32, ['#5d7a52', '#8f9a5e', '#2c4a40', '#748c58'])
    for k, (x, y) in enumerate(((2, 2), (12, 4), (23, 1), (6, 13), (18, 15), (27, 12), (1, 24), (13, 26), (24, 25))):
        c.stamp(x, y, ['.11.', '1111', '.13.'] if k % 2 else ['11.', '113', '.1.'], {'1': 1, '3': 3})
        c.stamp(x + 2, y + 4, ['22', '.2'], {'2': 2})
    return c


def tire():
    c = C(32, 8, ['#3d3e40', '#2b2c2f', '#56575b'])
    for x in range(0, 32, 2): c.rect(x, 2, x, 5, 1)
    c.rect(0, 0, 31, 0, 2); c.p(7, 3, 2); c.p(23, 4, 2)
    return c


def drum(name, base, band, hi):
    c = C(32, 16, [base, band, hi, '#7a5236', '#5a3e2c'])
    c.rect(0, 3, 31, 3, 1); c.rect(0, 11, 31, 11, 1); c.rect(0, 0, 31, 0, 1); c.rect(0, 15, 31, 15, 1)
    c.rect(4, 1, 5, 14, 2)
    c.stamp(14, 12, RUST_B, {'r': 3, 'R': 4}); c.stamp(25, 6, RUST_A, {'r': 3, 'R': 4}); c.stamp(9, 4, DRIP, {'r': 3, 'R': 4})
    c.save = lambda: c.im.save(os.path.join(OUT, name + '.png')); return c


def drum_top():
    c = C(16, 16, ['#8d8a82', '#6a6862', '#4e4c48', '#a8a59c'])
    c.rect(2, 2, 13, 13, 0); c.rect(0, 0, 15, 1, 1); c.rect(0, 14, 15, 15, 1)
    c.rect(10, 4, 11, 5, 2); c.p(4, 10, 3); c.stamp(5, 6, OIL_S, {'o': 1, 'O': 2})
    return c


def wood():
    c = C(16, 16, ['#8a6a48', '#a3815a', '#5e4632', '#3f3226'])
    for y in (3, 7, 11, 15): c.rect(0, y, 15, y, 2)
    c.rect(2, 1, 7, 1, 1); c.rect(9, 5, 14, 5, 1); c.rect(1, 9, 5, 9, 1); c.rect(8, 13, 13, 13, 1); c.p(12, 1, 3); c.p(3, 13, 3)
    return c


def metal():
    c = C(16, 16, ['#6c7476', '#848c8c', '#4c5254', '#8a5a3c'])
    c.rect(0, 0, 15, 0, 1); c.rect(0, 15, 15, 15, 2); c.p(5, 7, 3); c.p(6, 7, 3); c.p(11, 11, 3)
    return c


def parts():
    """선반 위 물건 측면: 깡통·상자·부품 — 16px 칸마다 하나."""
    c = C(64, 16, ['#5e4632', '#b4584a', '#d8c690', '#5f7f96', '#8f9a5e', '#3d3e40'])
    c.rect(0, 0, 15, 15, 1); c.rect(0, 5, 15, 9, 2); c.rect(0, 0, 15, 0, 5)  # 빨간 깡통+흰 띠
    c.rect(16, 0, 31, 15, 3); c.rect(16, 0, 31, 2, 2); c.p(20, 8, 2); c.p(21, 8, 2); c.p(22, 8, 2)  # 파란 상자
    c.rect(32, 0, 47, 15, 4); c.rect(32, 7, 47, 7, 0); c.rect(38, 2, 41, 4, 2)  # 풀색 상자
    c.rect(48, 0, 63, 15, 2); c.rect(48, 12, 63, 15, 0); c.stamp(52, 3, ['.55.', '5..5', '5..5', '.55.'], {'5': 5})  # 종이 상자+부품
    return c


POSTER = [
    'cccccccccccccccc',
    'crrrrrrrrrrrrrrc',
    'cr22rrr2rr222rrc',
    'crrrrrrrrrrrrrrc',
    'cccccccccccccccc',
    'cccccbbbbbbccccc',
    'ccccbbbbbbbbcccc',
    'cccbbbbbbbbbbccc',
    'cbbbbbbbbbbbbbbc',
    'cbbkkbbbbbbkkbbc',
    'cckkkkccccckkkkc',
    'ccckkcccccccckkc',
    'cccccccccccccccc',
    'cyyyyyyyyyyyyyyc',
    'cyy2222y22y22yyc',
    'cyyyyyyyyyyyyyyc',
    'cccccccccccccccc',
    'cc77777777cc77cc',
    'cccccccccccccccc',
    'cc777777cc7777cc',
    'cccccccccccccccc',
    'cccc33333333cccc',
    'cccc3333333ccccc',
    'cccccccccccccccc',
]


def poster(name, swap):
    keys = {'c': 0, 'r': 1, '2': 2, 'b': 3, 'k': 4, 'y': 5, '7': 6, '3': 1}
    c = C(16, 24, ['#e6dcc0', swap[0], '#f2ead6', swap[1], '#2e3436', '#d8b45a', '#9b8f78'])
    c.stamp(0, 0, POSTER, keys)
    c.p(15, 23, 6); c.p(14, 23, 6); c.p(15, 22, 6)  # 귀퉁이 말림
    c.save = lambda: c.im.save(os.path.join(OUT, name + '.png')); return c


def record_board():
    """기록 판 32×16(2m×1m): 칠판 + 분필 기록 줄·작대기 셈."""
    c = C(32, 16, ['#3f5a4c', '#6b4f36', '#d8dccc', '#7f9488'])
    c.rect(0, 0, 31, 0, 1); c.rect(0, 15, 31, 15, 1); c.rect(0, 0, 0, 15, 1); c.rect(31, 0, 31, 15, 1)
    for row, n in ((3, 7), (6, 4), (9, 9), (12, 2)):
        c.rect(3, row, 9, row, 3)
        for i in range(n): c.rect(12 + i * 2 + (i // 5), row - 1, 12 + i * 2 + (i // 5), row + 1, 2)
    c.rect(3, 2, 8, 2, 2)
    return c


SLOT_ICONS = {
    'car': ['.rrr.', 'rrrrr', 'k...k'],
    'wrench': ['g.g', 'ggg', '.g.', '.g.'],
    'flag': ['yyyy', 'yyy.', 'yyyy', 'k...', 'k...'],
}


def slot_board():
    """「차·실력·응원」 빈 칸 3개 보드 32×16: 나무판 + 아이콘 + 비어 있는 크림색 칸(나중에 스티커가 들어감)."""
    c = C(32, 16, ['#a3815a', '#5e4632', '#efe6cc', '#b4584a', '#9aa0a0', '#d8b45a', '#2e3436'])
    c.rect(0, 0, 31, 0, 1); c.rect(0, 15, 31, 15, 1)
    keys = {'r': 3, 'k': 6, 'g': 4, 'y': 5}
    for i, ic in enumerate(('car', 'wrench', 'flag')):
        x = 2 + i * 10
        c.stamp(x + 2, 2, SLOT_ICONS[ic], keys)
        c.rect(x, 8, x + 7, 13, 1); c.rect(x + 1, 9, x + 6, 12, 2)
    return c


def pegboard():
    """공구 판 32×16: 구멍 점 + 걸린 공구 그림자(렌치·망치·톱·드라이버)."""
    c = C(32, 16, ['#b89a72', '#9a7e5a', '#3f4446', '#6c7476', '#b4584a'])
    for y in range(1, 16, 3):
        for x in range(1, 32, 3): c.p(x, y, 1)
    c.stamp(2, 2, ['.3.', '333', '.3.', '.3.', '.3.', '.3.', '333'], {'3': 3})  # 렌치
    c.stamp(7, 2, ['3333', '3333', '.2..', '.2..', '.2..', '.2..', '.2..'], {'3': 3, '2': 2})  # 망치
    c.stamp(13, 2, ['22222222', '33333333', '3333333.', '333333..'], {'2': 2, '3': 3})  # 톱
    c.stamp(24, 2, ['4', '4', '4', '3', '3', '3', '3'], {'3': 3, '4': 4})  # 드라이버
    c.stamp(27, 2, ['.4.', '4.4', '4.4', '.4.', '.3.', '.3.'], {'3': 3, '4': 4})  # 줄자
    return c


def kart_body():
    """카트 차체 32×32: 바랜 빨강, 칠 벗겨진 쇠, 회색 테이프 수리, 녹 점."""
    """카트는 작아서(면 하나 0.3~0.6m = 5~10px) 16px(1m) 한 장에 8px 칸마다 자국 하나씩 들어가게."""
    c = C(16, 16, ['#a85a4c', '#bf7262', '#9aa0a0', '#6e4434', '#d0ccba', '#8f8c80'])
    c.rect(0, 0, 15, 0, 1); c.rect(0, 8, 15, 8, 1)  # 판 접힌 자리 밝은 줄
    c.stamp(1, 2, ['.22', '222', '.2.'], {'2': 2})  # 칠 벗겨진 쇠
    c.rect(9, 3, 14, 4, 4); c.rect(9, 4, 14, 4, 5)  # 테이프
    c.stamp(3, 11, ['44', '44', '45', '45'], {'4': 4, '5': 5})  # 세로 테이프
    c.stamp(10, 11, ['2.3', '223'], {'2': 2, '3': 3})  # 긁힘+녹
    c.p(6, 6, 3); c.p(14, 14, 3)
    return c


STICKER = [
    '.wwwwww.',
    'wwkkkkww',
    'wwwwwkww',
    'wwwwkwww',
    'wwwkwwww',
    'wwwkwwww',
    'wwwwwwww',
    '.wwwwwg.',
]


def sticker():
    c = C(8, 8, ['#b4584a', '#efe6cc', '#2e3436', '#c8c6b6'])
    c.stamp(0, 0, STICKER, {'w': 1, 'k': 2, 'g': 3})
    return c


def crate():
    c = C(16, 16, ['#c9a36e', '#a8834f', '#6b4f36', '#4f6a6a'])
    for y in (4, 9, 15): c.rect(0, y, 15, y, 2)
    c.rect(0, 0, 0, 15, 1); c.rect(15, 0, 15, 15, 1)
    c.stamp(5, 5, ['3.3', '.3.', '3.3'], {'3': 3})  # 스텐실 표
    c.p(2, 2, 2); c.p(13, 2, 2); c.p(2, 12, 2); c.p(13, 12, 2)
    return c


def strap():
    c = C(8, 8, ['#7a6a46', '#5e5036', '#a8a28a'])
    c.rect(0, 0, 0, 7, 1); c.rect(7, 0, 7, 7, 1); c.rect(2, 3, 5, 4, 2)
    return c


def seat():
    c = C(16, 16, ['#3e4f5a', '#516570', '#2a363e', '#c8c6b6'])
    c.rect(0, 0, 15, 1, 1); c.rect(3, 6, 12, 6, 2); c.rect(3, 11, 12, 11, 2)
    c.rect(9, 3, 12, 4, 3); c.p(13, 3, 3)  # 터진 자리 테이프
    return c


def lamp():
    c = C(8, 8, ['#4f6a6a', '#6d8a88', '#f2dc9a', '#fff0c0'])
    c.rect(0, 0, 7, 1, 1); c.rect(0, 6, 7, 7, 2); c.rect(2, 7, 5, 7, 3)
    return c


def window():
    """옆벽 창 32×16(2m×1m): 나무 틀 + 4칸 유리(짙은 청록) + 깨진 칸 판자로 막음."""
    c = C(32, 16, ['#6b4f36', '#2f4a50', '#4c6e72', '#c9a36e', '#3f3226'])
    c.rect(1, 1, 30, 14, 1)
    for x in (8, 16, 23): c.rect(x, 1, x, 14, 0)
    c.rect(1, 7, 30, 7, 0)
    for x0 in (2, 9, 17, 24): c.rect(x0, 2, x0 + 2, 2, 2)
    c.rect(24, 8, 30, 10, 3); c.rect(24, 11, 30, 13, 3); c.p(25, 9, 4); c.p(29, 12, 4)  # 막은 판자
    return c


def sea():
    c = C(32, 32, ['#2f6f88', '#3f8fa6', '#5aa4b8'])
    for x, y in ((2, 3), (14, 8), (25, 2), (7, 15), (20, 19), (28, 26), (11, 27)):
        c.rect(x, y, x + 2, y, 1); c.p(x + 1, y - 1, 2)
    return c


def stone():
    c = C(32, 16, ['#cfc4ac', '#b3a88f', '#8f8672', '#e0d6bd'])
    for y in (0, 7, 15): c.rect(0, y, 31, y, 2)
    for x, yy in ((6, 1), (17, 1), (27, 1), (2, 8), (12, 8), (23, 8)): c.rect(x, yy, x, yy + 5, 2)
    c.rect(1, 1, 4, 1, 3); c.rect(13, 9, 16, 9, 3)
    return c


def main():
    os.makedirs(OUT, exist_ok=True)
    tex = {
        'tin_wall': tin('tin_wall', ['#7d998c', '#93ad9e', '#4f6a64', '#a0623e', '#6e4130'], 0),
        'tin_roof': tin('tin_roof', ['#a85c5e', '#bb7369', '#6e3e40', '#8a5236', '#5e3426'], 11),
        'planks': planks('planks', ['#b08a5e', '#c49f70', '#6b4f36', '#3f3226', '#7f9aa6', '#d2b080'], [(18, 5), (44, 10), (2, 2)]),
        'planks_dark': planks('planks_dark', ['#8a6a48', '#a3815a', '#4f3a28', '#2e241c', '#6d8a88', '#c9a36e'], [(30, 3), (8, 9)]),
        'floor': floor(), 'ground': grass(), 'dirt': ground(), 'tire': tire(), 'leaf': leaf(),
        'drum_red': drum('drum_red', '#a85646', '#7a3b30', '#c06e5a'),
        'drum_blue': drum('drum_blue', '#5f7f96', '#435e72', '#7898ac'),
        'drum_top': drum_top(), 'wood': wood(), 'metal': metal(), 'parts': parts(),
        'poster_a': poster('poster_a', ('#b4584a', '#4f6a6a')), 'poster_b': poster('poster_b', ('#4f6a6a', '#b4584a')),
        'record': record_board(), 'slots': slot_board(), 'pegboard': pegboard(),
        'kart_body': kart_body(), 'sticker': sticker(), 'crate': crate(), 'strap': strap(), 'seat': seat(),
        'lamp': lamp(), 'window': window(), 'sea': sea(), 'stone': stone(),
    }
    for n, c in tex.items():
        if hasattr(c, 'save') and callable(c.save) and c.save.__name__ == '<lambda>': c.save()
        else: c.im.save(os.path.join(OUT, n + '.png'))
        ncol = len(set(c.im.getdata()))
        print(f'{n:12s} {c.w}x{c.h}  colors {ncol}')


if __name__ == '__main__':
    main()
