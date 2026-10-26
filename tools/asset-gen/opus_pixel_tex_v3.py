"""정비소·고카트 v3 텍스처 (D-165, 기준 ①⑥⑫). PIL 만 — 노이즈·그라데이션·반복 도장 없음, 전부 손으로 정한 자리.
밀도: 건물·땅 32px/m, 카트·작은 물건 64px/m — 960×540 렌더에서 텍셀 1개 ≈ 화면 2px ≈ 1080 기준 4px
(측정: ① 5px · ⑥ 4px · ⑫ 4~5px). 큰 면은 단색에 가깝게(①), 이음매·틈·못은 1텍셀 선, 녹·기름은 각진 덩어리.
Point 필터 전제. 결과 → art/source/opus-garage-kart/tex_v3/ + tile_m.json([가로 m, 세로 m]).

    python -X utf8 tools/asset-gen/opus_pixel_tex_v3.py
"""
import json, os, random
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'art', 'source', 'opus-garage-kart', 'tex_v3')
V1 = os.path.join(ROOT, 'art', 'source', 'opus-garage-kart', 'tex')
TILE = {}


def hx(h):
    h = h.lstrip('#'); return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


class C:
    def __init__(self, w, h, pal, ppm=None):
        self.pal = [hx(p) for p in pal]; self.im = Image.new('RGB', (w, h), self.pal[0]); self.w, self.h = w, h; self.ppm = ppm

    def p(self, x, y, k):
        self.im.putpixel((x % self.w, y % self.h), self.pal[k])

    def rect(self, x0, y0, x1, y1, k):  # 끝 포함, 감싸기
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1): self.p(x, y, k)

    def stamp(self, x0, y0, rows, keys):
        for j, r in enumerate(rows):
            for i, ch in enumerate(r):
                if ch in keys: self.p(x0 + i, y0 + j, keys[ch])

    def blob(self, x0, y0, rows, k, k2=None):
        """각진 덩어리: rows 는 줄마다 (시작 오프셋, 길이) — 4px 계단으로 손으로 정한 모양. k2 = 아래 가장자리 짙은 색."""
        for j, (o, n) in enumerate(rows):
            self.rect(x0 + o, y0 + j, x0 + o + n - 1, y0 + j, k)
        if k2 is not None:
            o, n = rows[-1]; self.rect(x0 + o, y0 + len(rows), x0 + o + n - 1, y0 + len(rows), k2)


# 손으로 정한 각진 덩어리 모양(줄마다 시작·길이)
BLOB_L = [(3, 6), (1, 10), (0, 13), (0, 14), (1, 12), (2, 11), (4, 7), (6, 3)]
BLOB_M = [(2, 4), (0, 8), (0, 9), (1, 7), (3, 3)]
BLOB_S = [(1, 2), (0, 4), (0, 3)]


def save(name, c, tile=None):
    c.im.save(os.path.join(OUT, name + '.png'))
    if c.ppm: TILE[name] = [c.w / c.ppm, c.h / c.ppm]
    elif tile: TILE[name] = tile
    return c


def tin_wall():
    """함석벽 12m×4m: 바랜 세이지 큰 색면, 1m 판 이음(1텍셀) + 리벳, 아래 녹 띠는 1m 판마다 다른 높이, 녹물 3줄기, 녹 덩어리 1."""
    # 세로 8m(256) — 박공(z 3.2~5.6m)에 아래 녹 띠가 다시 나타나지 않게
    c = C(384, 256, ['#9db5a2', '#86a08e', '#b6cab5', '#b5703f', '#8c5032'], 32)
    for x in range(31, 384, 32):
        c.rect(x, 0, x, 255, 1)
        for y in (6, 38, 70, 102, 134, 166, 198, 230): c.p(x - 1, y, 2)
    rust_h = [3, 5, 2, 7, 4, 2, 6, 3, 9, 4, 2, 5]  # 판마다 녹 높이(텍셀)
    for i, hgt in enumerate(rust_h):
        for x in range(i * 32, i * 32 + 31):
            hh = hgt + (2 if (x // 4) % 3 == 1 and hgt > 3 else 0)
            c.rect(x, 256 - hh, x, 255, 3); c.p(x, 255, 4)
    for x, y, n in ((94, 135, 14), (222, 167, 10), (318, 135, 22)): c.rect(x, y, x, y + n, 3)  # 리벳 아래 녹물
    c.blob(150, 188, BLOB_L, 3, 4); c.blob(40, 70, BLOB_M, 3)
    return save('tin_wall', c)


def tin_roof():
    """함석 지붕 12m×12m: 주황 큰 색면(① 주황 지붕), 0.8m 골마다 짙은 선 + 밝은 선 1텍셀, 녹 덩어리 3, 덧댄 회색 함석 1장."""
    c = C(384, 384, ['#d27a48', '#b2603a', '#e69a64', '#8e4a30', '#a4aca2', '#7e8880'], 32)
    for x in range(0, 384, 26):
        c.rect(x, 0, x, 383, 1); c.rect(x + 1, 0, x + 1, 383, 2)
    c.blob(60, 70, BLOB_L, 3); c.blob(250, 210, BLOB_M, 3); c.blob(140, 300, BLOB_L, 3)
    c.rect(300, 40, 337, 95, 4); c.rect(300, 40, 337, 40, 5); c.rect(300, 95, 337, 95, 5)  # 덧댄 판
    for x in (302, 335):
        for y in (43, 67, 92): c.p(x, y, 5)
    return save('tin_roof', c)


def planks(name, pal, seed):
    """판자벽 12m×4m: 판 높이 8텍셀(25cm), 1텍셀 틈, 판마다 3톤 중 하나(손 순서), 이음 자리·못 줄마다 다름,
    바랜 옛 칠(청록) 큰 덩어리 1, 덧댄 새 판자 2."""
    c = C(384, 128, pal, 32)  # 0 나무 1 밝은 판 2 짙은 판 3 틈 4 못 5 옛 칠 6 새 판자
    tones = [0, 1, 0, 2, 1, 0, 2, 0, 1, 2, 0, 1, 0, 2, 0, 1]
    rnd = random.Random(seed)
    for i in range(16):
        y = i * 8; k = tones[(i + seed) % 16]
        c.rect(0, y, 383, y + 6, k); c.rect(0, y + 7, 383, y + 7, 3)
        x = rnd.randrange(0, 60)
        while x < 384:  # 이음: 2.5~4.5m 마다
            c.rect(x, y, x, y + 6, 3); c.p(x + 2, y + 2, 4); c.p(x + 2, y + 4, 4); c.p(x - 2, y + 3, 4)
            x += rnd.randrange(80, 144)
    c.blob(40, 30, [(o * 2, n * 2) for o, n in BLOB_L], 5)  # 옛 칠 28×16
    for x0, row, w in ((180, 3, 44), (300, 10, 36)):
        y = row * 8; c.rect(x0, y, x0 + w, y + 6, 6); c.rect(x0 - 1, y, x0 - 1, y + 6, 3); c.rect(x0 + w + 1, y, x0 + w + 1, y + 6, 3)
        for yy in (y + 2, y + 4): c.p(x0 + 1, yy, 4); c.p(x0 + w - 1, yy, 4)
    return save(name, c)


def floor():
    """콘크리트 12m×12m: 거의 단색, 4m 줄눈 1텍셀, 기름 얼룩 4(작업대·카트 자리, 입구 2m 안쪽만), 금 2줄."""
    c = C(384, 384, ['#c9bfa9', '#b3a994', '#8f877a', '#6f6a62', '#d6cdb8'], 32)
    for x in (128, 256): c.rect(x, 0, x, 383, 1)
    for y in (128, 256): c.rect(0, y, 383, y, 1)
    # v=y(m), 이미지 위가 v=1 → 행 = 383 - y*32. 정비소 x -5..5 → 텍셀 x = (x mod 12)*32
    for xm, ym, shape in ((-1.2, 5.0, BLOB_L), (2.6, 7.5, BLOB_M), (-3.8, 8.6, BLOB_M), (0.8, 3.2, BLOB_S)):
        c.blob(int((xm % 12) * 32), int(383 - ym * 32), shape, 3 if shape is not BLOB_S else 2)
    c.stamp(int(3.5 * 32), int(383 - 9.5 * 32), ['2...', '.2..', '.22.', '...2', '...2'], {'2': 2})
    c.stamp(int(9.0 * 32), int(383 - 4.0 * 32), ['2..', '.2.', '.2.', '..2'], {'2': 2})
    return save('floor', c)


def ground():
    """풀밭 16m×16m: 밝은 늦가을 풀 큰 색면, 짙은 풀 포기 덩어리·낙엽(노랑·주황) 몇 점 — 위치는 고정 시드로 한 번 정함."""
    c = C(512, 512, ['#8fa54e', '#76903f', '#a8b860', '#d8ae48', '#cf7438'], 32)
    rnd = random.Random(7)
    for _ in range(26):
        x, y = rnd.randrange(512), rnd.randrange(512); c.blob(x, y, BLOB_M if rnd.random() < .5 else BLOB_S, 1)
    for _ in range(14):
        x, y = rnd.randrange(512), rnd.randrange(512); c.blob(x, y, BLOB_S, 2)
    for _ in range(22):
        x, y = rnd.randrange(512), rnd.randrange(512); k = 3 if rnd.random() < .6 else 4
        c.p(x, y, k); c.p(x + 1, y, k); c.p(x + 1, y + 1, k)
    return save('ground', c)


def dirt():
    c = C(384, 384, ['#cdae7c', '#b8996a', '#dcc296', '#9a8460'], 32)
    for x in (150, 182):  # 바퀴 자국
        for y in range(0, 384):
            if (y // 12) % 5: c.p(x, y, 1); c.p(x + 1, y, 1)
    rnd = random.Random(3)
    for _ in range(30):
        x, y = rnd.randrange(384), rnd.randrange(384); c.p(x, y, 3); c.p(x + 1, y, 2)
    c.blob(40, 260, BLOB_L, 1); c.blob(290, 90, BLOB_M, 1)
    return save('dirt', c)


def leaf(name, pal):
    """나무 덩어리 2m×2m 64×64: 단색 바탕 + 밝은 잎 덩어리 3 + 짙은 속 덩어리 2(①처럼 큰 덩어리)."""
    c = C(64, 64, pal, 32)
    for x, y in ((6, 4), (36, 10), (18, 36)): c.blob(x, y, BLOB_L, 1)
    for x, y in ((40, 40), (4, 22)): c.blob(x, y, BLOB_M, 2)
    return save(name, c)


def tire():
    c = C(64, 16, ['#3d3e40', '#2b2c2f', '#5a5b5f']); c.rect(0, 0, 63, 0, 2); c.rect(0, 15, 63, 15, 2)
    for x in range(0, 64, 4): c.rect(x, 3, x + 1, 12, 1)
    return save('tire', c, [1, 1])


def drum(name, base, band, hi):
    c = C(64, 32, [base, band, hi, '#8a5232', '#5e3826', '#e8dcc0'])
    for y in (0, 7, 24, 31): c.rect(0, y, 63, y, 1)
    c.rect(8, 1, 10, 30, 2)  # 세로 하이라이트
    c.rect(26, 11, 41, 19, 5); c.rect(28, 13, 39, 13, 1); c.rect(28, 16, 35, 16, 1)  # 상표 딱지
    c.blob(46, 20, BLOB_M, 3, 4); c.rect(50, 8, 50, 18, 3)
    return save(name, c, [1, 1])


def drum_top():
    c = C(32, 32, ['#9a968c', '#77746c', '#55534e', '#b4b0a4'])
    c.rect(0, 0, 31, 1, 1); c.rect(0, 30, 31, 31, 1); c.rect(0, 0, 1, 31, 1); c.rect(30, 0, 31, 31, 1)
    c.rect(20, 7, 23, 10, 2); c.blob(6, 16, BLOB_S, 1); c.rect(8, 6, 13, 6, 3)
    return save('drum_top', c, [1, 1])


def wood():
    c = C(64, 64, ['#94714b', '#ad8a5e', '#6a4f36', '#4a3828'], 32)
    for y in range(7, 64, 8): c.rect(0, y, 63, y, 2)
    for x, y, n in ((4, 2, 14), (30, 10, 18), (10, 26, 12), (40, 42, 16), (16, 58, 20)): c.rect(x, y, x + n, y, 1)
    for x, y in ((22, 4), (50, 20), (8, 44)): c.p(x, y, 3)
    return save('wood', c)


def metal():
    c = C(64, 64, ['#727a7c', '#8d9596', '#545a5c', '#9a5e3c'], 32)
    for x, y in ((10, 12), (40, 30), (20, 50)): c.blob(x, y, BLOB_S, 3)
    c.rect(0, 0, 63, 0, 1)
    return save('metal', c)


def kart_body():
    """카트 차체 1m×1m 64×64(64px/m): 바랜 빨강 큰 색면, 접힌 모서리 밝은 선, 칠 벗겨진 쇠 덩어리 2, 회색 테이프 2, 녹 점."""
    c = C(64, 64, ['#bd5a44', '#d6785e', '#a3a8a6', '#7a4232', '#d8d2be', '#a49f8e', '#9a4836'], 64)
    c.rect(0, 0, 63, 0, 1); c.rect(0, 32, 63, 32, 1)
    c.blob(6, 6, BLOB_M, 2); c.blob(40, 44, BLOB_S, 2); c.p(44, 47, 3)
    c.rect(34, 10, 58, 14, 4); c.rect(34, 14, 58, 14, 5); c.rect(36, 11, 37, 11, 5)  # 가로 테이프
    c.rect(12, 40, 16, 60, 4); c.rect(16, 40, 16, 60, 5)  # 세로 테이프
    c.rect(24, 20, 30, 20, 6); c.rect(52, 26, 58, 26, 6); c.p(20, 54, 3)
    return save('kart_body', c)


STICKER = [
    '..wwwwwwwwwwww..',
    '.wwwwwwwwwwwwww.',
    'wwwwwwwwwwwwwwww',
    'wwwkkkkkkkkkkwww',
    'wwwkkkkkkkkkkwww',
    'wwwwwwwwwwkkkwww',
    'wwwwwwwwwkkkwwww',
    'wwwwwwwwkkkwwwww',
    'wwwwwwwkkkwwwwww',
    'wwwwwwkkkwwwwwww',
    'wwwwwwkkkwwwwwww',
    'wwwwwkkkwwwwwwww',
    'wwwwwkkkwwwwwwww',
    'wwwwwwwwwwwwwwww',
    '.wwwwwwwwwwwwgg.',
    '..wwwwwwwwwwgg..',
]


def sticker():
    c = C(16, 16, ['#bd5a44', '#efe6cc', '#2e3436', '#c8c2ae'])
    c.stamp(0, 0, STICKER, {'w': 1, 'k': 2, 'g': 3})
    return save('sticker', c, [1, 1])


def crate():
    """짐상자 32×32(fit): 판자 3장(틈 1텍셀), 테두리 각목, 스텐실 화살표, 못."""
    c = C(32, 32, ['#cfa66c', '#b08850', '#6b4f36', '#4f6a6a', '#dcbc86'])
    for y in (10, 21): c.rect(0, y, 31, y, 2)
    c.rect(0, 0, 2, 31, 1); c.rect(29, 0, 31, 31, 1); c.rect(0, 0, 31, 1, 1); c.rect(0, 30, 31, 31, 1)
    c.rect(5, 4, 20, 4, 4)
    c.stamp(12, 12, ['..3..', '.333.', '33333', '..3..', '..3..', '..3..'], {'3': 3})
    for x, y in ((4, 3), (27, 3), (4, 28), (27, 28)): c.p(x, y, 2)
    return save('crate', c, [1, 1])


def strap():
    c = C(8, 8, ['#7a6a46', '#5e5036', '#b4ae94']); c.rect(0, 0, 0, 7, 1); c.rect(7, 0, 7, 7, 1); c.rect(2, 3, 5, 4, 2)
    return save('strap', c, [1, 1])


def seat():
    c = C(32, 32, ['#3f5464', '#566e7e', '#2a3842', '#d0ccba'])
    c.rect(0, 0, 31, 2, 1); c.rect(5, 12, 26, 12, 2); c.rect(5, 22, 26, 22, 2)
    c.rect(18, 5, 25, 8, 3); c.rect(19, 8, 25, 8, 2)  # 터진 자리 테이프
    return save('seat', c, [1, 1])


def lamp():
    c = C(16, 16, ['#4f6a6a', '#6d8a88', '#f2dc9a', '#fff0c0']); c.rect(0, 0, 15, 2, 1); c.rect(0, 12, 15, 15, 2); c.rect(4, 14, 11, 15, 3)
    return save('lamp', c, [1, 1])


def sea():
    c = C(256, 256, ['#3f8fa6', '#57a6ba', '#7cc0cc'], 6.4)
    rnd = random.Random(11)
    for _ in range(40):
        x, y = rnd.randrange(256), rnd.randrange(256); n = rnd.randrange(4, 10); c.rect(x, y, x + n, y, 1); c.rect(x + 2, y - 1, x + n - 3, y - 1, 2)
    return save('sea', c)


def stone():
    c = C(128, 64, ['#d3c8b0', '#b9ae96', '#958c78', '#e2d8c0'], 32)
    for y in (0, 31, 63): c.rect(0, y, 127, y, 2)
    for x, y in ((20, 1), (64, 1), (108, 1), (40, 32), (86, 32)): c.rect(x, y, x, y + 29, 2)
    c.rect(2, 2, 14, 2, 3); c.rect(44, 34, 60, 34, 3)
    return save('stone', c)


def from_v1(name, k=2):
    """v1 장식 판(포스터·기록판·보드·창·선반 물건)은 v1 도트를 정수배 계단 확대 — 같은 그림, 텍셀만 32px/m."""
    im = Image.open(os.path.join(V1, name + '.png')).convert('RGB')
    im.resize((im.width * k, im.height * k), Image.NEAREST).save(os.path.join(OUT, name + '.png'))
    TILE[name] = [1, 1]


def main():
    os.makedirs(OUT, exist_ok=True)
    tin_wall(); tin_roof()
    planks('planks', ['#c09464', '#d0a874', '#a87e52', '#5e4430', '#3a2c20', '#86aaa4', '#e0c08c'], 0)
    planks('planks_dark', ['#9a7650', '#ac8a60', '#86663f', '#4a3626', '#2e241c', '#729490', '#cfae7c'], 5)
    floor(); ground(); dirt()
    leaf('leaf', ['#6f9a48', '#8eb455', '#4f7a44']); leaf('leaf_y', ['#d8a83e', '#ecc458', '#b08436']); leaf('leaf_o', ['#cf6e38', '#e48c4a', '#a8542e'])
    tire(); drum('drum_red', '#b45a46', '#86402f', '#cf7a62'); drum('drum_blue', '#5f87a0', '#456478', '#7ea4ba'); drum_top()
    wood(); metal(); kart_body(); sticker(); crate(); strap(); seat(); lamp(); sea(); stone()
    for n in ('poster_a', 'poster_b', 'record', 'slots', 'pegboard', 'window', 'parts'): from_v1(n)
    json.dump(TILE, open(os.path.join(OUT, 'tile_m.json'), 'w'), indent=1)
    for f in sorted(os.listdir(OUT)):
        if f.endswith('.png'):
            im = Image.open(os.path.join(OUT, f)); print(f'{f:16s} {im.size}  colors {len(set(im.getdata()))}')


if __name__ == '__main__':
    main()
