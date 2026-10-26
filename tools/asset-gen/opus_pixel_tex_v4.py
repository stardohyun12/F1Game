"""정비소·고카트 v4 텍스처 (내부 1280×720 → 1080 Nearest 1.5배, 기준 ①⑥⑫). v3 에서 이어서.
밀도(게임 컷 x1_game 기준으로 다시 맞춤): 건물·땅 24px/m, 카트 48px/m — 카트 7m·정비소 12m 에서 텍셀 1개 ≈ 1080 기준 4.5~5px
(기준 ① 5px · ⑥ 4px · ⑫ 4~5px). 실측은 opus_texel_probe_v4.py(체커 렌더). v3 의 32/64 도트는 그대로, 미터당 밀도만 바꿈.
v4 추가: 늦가을 마른 풀, 카트 칠 벗겨짐·스티커·녹 블록, 바깥 바닥(apron)·벽 밑 세월(흙 튐·얼룩·잡초)을 큰 블록으로.
Point 필터 전제. 결과 → art/source/opus-garage-kart/tex_v4/ + tile_m.json([가로 m, 세로 m]).

    python -X utf8 tools/asset-gen/opus_pixel_tex_v4.py
"""
import json, os, random
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'art', 'source', 'opus-garage-kart', 'tex_v4')
V1 = os.path.join(ROOT, 'art', 'source', 'opus-garage-kart', 'tex')
TILE = {}
PPM = {32: 24, 64: 48}  # v4: 그린 도트 밀도 → 실제 미터당 텍셀(720 렌더에서 기준 칸 크기에 맞춤)


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
    if c.ppm: p = PPM.get(c.ppm, c.ppm); TILE[name] = [c.w / p, c.h / p]
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
    grime(c, 7, 8, 9, seed)
    return save(name, c)


# 잡초 포기 모양(아래가 땅) — 손으로 그린 3종
WEED = [['..x...x', '.xx..xx.', '.xxx.x..', 'xxxxxx..', '.xxxxx..'],
        ['...x..', 'x..x..', 'xx.xx.', '.xxxx.', 'xxxxxx'],
        ['.x.....x', '.xx..x.x', 'xxx.xxx.', 'xxxxxxxx']]


def grime(c, kd, kw, kw2, seed):
    """벽 밑 세월: 비 맞아 튄 흙 띠(4텍셀 단위로 높이가 바뀜) + 잡초 포기 몇 개 — v4."""
    hs = [3, 4, 4, 2, 5, 6, 3, 2, 2, 4, 7, 5, 3, 3, 2, 4]
    for x in range(c.w):
        h = hs[((x // 4) + seed * 3) % len(hs)]
        c.rect(x, c.h - h, x, c.h - 1, kd)
        if h >= 5 and (x // 4) % 2: c.p(x, c.h - h - 1, kd)  # 튄 자국 한 칸 더
    for i, x in enumerate((22, 97, 140, 233, 301, 350) if seed == 0 else (60, 190, 330)):
        w = WEED[(i + seed) % 3]
        c.stamp(x, c.h - len(w), w, {'x': kw if i % 2 else kw2})


def apron():
    """정비소 앞 바깥 콘크리트 12m×4m(24px/m, 288×96): 행 r ↔ y=-r/24 m(0=문턱), 열 ↔ x=((x/12) mod 1)·288.
    밖에서 들어온 흙(아래 행), 벽 밑 잡초(x≈±5.5), 문 앞 기름 얼룩, 갈라진 금, 바퀴 자국 — 큰 블록으로."""
    c = C(288, 96, ['#c4b9a1', '#ada28c', '#8f877a', '#6f6a62', '#b59a6e', '#9c9a52', '#7d8240', '#d3c9b2'])
    col = lambda xm: int(((xm / 12) % 1) * 288)
    for x in range(288):  # 흙이 쓸려 들어온 띠: y -2.6~-4m, 4텍셀마다 높이가 다름
        h = (10, 14, 12, 18, 22, 16, 12, 20, 26, 18, 14, 10)[(x // 6) % 12]
        c.rect(x, 96 - h, x, 95, 4)
    for xm in (-1.1, 1.3):  # 들어오는 바퀴 자국(흙 묻은 2텍셀 줄, 끊김)
        x = col(xm)
        for y in range(0, 80):
            if (y // 6) % 4: c.rect(x, y, x + 1, y, 1 if y < 60 else 3)
    c.blob(col(-.4), 18, [(o * 2, n * 2) for o, n in BLOB_M], 3)  # 문 앞 기름 얼룩(큰 덩어리)
    c.blob(col(-.4) + 4, 32, BLOB_S, 2)
    c.blob(col(2.8), 44, BLOB_M, 2)
    c.stamp(col(3.4), 6, ['3...', '.3..', '.33.', '...3', '...3', '..3.', '..3.'], {'3': 3})  # 금
    c.stamp(col(-3.0), 30, ['3..', '.3.', '.3.', '..3', '..3'], {'3': 3})
    for xm, sh, k in ((-5.6, 0, 5), (-5.3, 1, 6), (5.4, 2, 5), (5.8, 0, 6), (-4.6, 2, 6)):  # 벽 밑·문턱 틈 잡초
        w = WEED[sh]; c.stamp(col(xm), 2, w[::-1], {'x': k})
    for x in range(0, 288, 1):  # 문턱 줄(경계 1텍셀)
        c.p(x, 0, 1)
    return save('apron', c, [12, 4])


def floor():
    """콘크리트 12m×12m: 거의 단색, 4m 줄눈 1텍셀, 기름 얼룩 4(작업대·카트 자리, 입구 2m 안쪽만), 금 2줄."""
    P = 24; N = 12 * P  # v4: 24px/m 로 직접(얼룩 자리가 미터로 정해져 있어서)
    c = C(N, N, ['#c9bfa9', '#b3a994', '#8f877a', '#6f6a62', '#d6cdb8', '#b59a6e'])
    c.ppm = None
    for x in (4 * P, 8 * P): c.rect(x, 0, x, N - 1, 1)
    for y in (4 * P, 8 * P): c.rect(0, y, N - 1, y, 1)
    # v=y(m), 이미지 위가 v=1 → 행 = N-1 - y*P. 정비소 x -5..5 → 텍셀 x = (x mod 12)*P
    for xm, ym, shape in ((-1.2, 5.0, BLOB_L), (2.6, 7.5, BLOB_M), (-3.8, 8.6, BLOB_M), (0.8, 3.2, BLOB_S)):
        c.blob(int((xm % 12) * P), int(N - 1 - ym * P), shape, 3 if shape is not BLOB_S else 2)
    c.stamp(int(3.5 * P), int(N - 1 - 9.5 * P), ['2...', '.2..', '.22.', '...2', '...2'], {'2': 2})
    c.stamp(int(9.0 * P), int(N - 1 - 4.0 * P), ['2..', '.2.', '.2.', '..2'], {'2': 2})
    for x in range(N):  # v4: 입구 안쪽 0~0.7m 에 들어온 흙(4텍셀마다 깊이 다름)
        h = (4, 7, 5, 10, 14, 9, 6, 12, 16, 8, 5, 3)[(x // 4) % 12]
        c.rect(x, N - h, x, N - 1, 5)
    return save('floor', c, [12, 12])


def ground():
    """풀밭 (512텍셀, 24px/m → 21m): v4 늦가을 — 바랜 황록 바탕, 마른 풀(짚색) 큰 덩어리, 아직 푸른 포기 몇 개,
    맨흙 자리, 낙엽(노랑·주황) — 위치는 고정 시드로 한 번 정함. 노이즈 아님(덩어리 수 손으로 정함)."""
    c = C(512, 512, ['#aaa45c', '#8f8a4a', '#c6b874', '#d8ae48', '#cf7438', '#8c9a4c', '#b39a64'], 32)
    rnd = random.Random(7)
    for _ in range(18):  # 마른 풀 큰 덩어리(짚색)
        x, y = rnd.randrange(512), rnd.randrange(512); c.blob(x, y, [(o * 2, n * 2) for o, n in BLOB_L], 2)
    for _ in range(22):  # 짙은 포기
        x, y = rnd.randrange(512), rnd.randrange(512); c.blob(x, y, BLOB_M if rnd.random() < .5 else BLOB_S, 1)
    for _ in range(9):  # 아직 푸른 포기
        x, y = rnd.randrange(512), rnd.randrange(512); c.blob(x, y, BLOB_M, 5)
    for _ in range(6):  # 맨흙
        x, y = rnd.randrange(512), rnd.randrange(512); c.blob(x, y, BLOB_L, 6)
    for _ in range(40):  # 낙엽 2×2 블록
        x, y = rnd.randrange(512), rnd.randrange(512); k = 3 if rnd.random() < .6 else 4
        c.rect(x, y, x + 1, y + 1, k)
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
    """v4: 몸체가 단색이라는 판정 → 칠 벗겨진 쇠(큰 덩어리 3), 녹 블록(2×2~3×3, 5개), 덧칠한 짙은 빨강 판 1,
    스티커 2(크림+파랑 줄 / 노랑 원형), 회색 테이프. 64×64 도트 = 48px/m 로 1.33m 를 덮음."""
    c = C(64, 64, ['#bd5a44', '#d6785e', '#a3a8a6', '#7a4232', '#d8d2be', '#a49f8e', '#9a4836',
                   '#3f6a8a', '#e2b948', '#8a3c30'], 64)
    c.rect(0, 0, 63, 0, 1); c.rect(0, 32, 63, 32, 1)
    c.rect(2, 36, 22, 50, 9)  # 덧칠 판(짙은 빨강, 경계 각짐)
    c.blob(4, 4, BLOB_L, 2, 5); c.blob(40, 44, BLOB_M, 2, 5); c.blob(26, 52, BLOB_S, 2)  # 벗겨진 쇠
    for x, y, s in ((10, 8, 3), (44, 47, 2), (56, 20, 2), (28, 54, 2), (6, 58, 3)): c.rect(x, y, x + s - 1, y + s - 1, 3)  # 녹
    c.rect(34, 10, 58, 14, 4); c.rect(34, 14, 58, 14, 5); c.rect(36, 11, 37, 11, 5)  # 가로 테이프
    c.rect(14, 40, 18, 62, 4); c.rect(18, 40, 18, 62, 5)  # 세로 테이프
    c.rect(26, 18, 37, 25, 4); c.rect(26, 21, 37, 22, 7); c.rect(36, 18, 37, 19, 1)  # 스티커: 크림 + 파랑 줄, 귀퉁이 떨어짐
    c.stamp(46, 28, ['.yyy.', 'yyyyy', 'yykyy', 'yyyyy', '.yyy.'], {'y': 8, 'k': 3})  # 노랑 원형 스티커
    c.rect(24, 30, 30, 30, 6); c.rect(52, 40, 58, 40, 6)
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


# ---- v4: 작은 물건(면에 맞춰 붙이는 fit 텍스처)을 절반 해상도로 다시 그림 — 체커 실측에서 카트 짐·시트·바퀴가
# 1080 기준 2~3px(기준 4~5px 보다 잘다). 상자 0.38m 에 16텍셀 ≈ 42/m → 7m 에서 약 5px.
def tire():
    c = C(32, 8, ['#3d3e40', '#2b2c2f', '#5a5b5f']); c.rect(0, 0, 31, 0, 2); c.rect(0, 7, 31, 7, 2)
    for x in range(0, 32, 4): c.rect(x, 2, x + 1, 5, 1)
    return save('tire', c, [1, 1])


def drum(name, base, band, hi):
    c = C(32, 16, [base, band, hi, '#8a5232', '#5e3826', '#e8dcc0'])
    for y in (0, 3, 12, 15): c.rect(0, y, 31, y, 1)
    c.rect(4, 1, 5, 14, 2)  # 세로 하이라이트
    c.rect(13, 5, 21, 10, 5); c.rect(14, 7, 20, 7, 1)  # 상표 딱지
    c.blob(23, 9, BLOB_S, 3, 4); c.rect(25, 4, 25, 8, 3)
    return save(name, c, [1, 1])


def drum_top():
    c = C(16, 16, ['#9a968c', '#77746c', '#55534e', '#b4b0a4'])
    c.rect(0, 0, 15, 0, 1); c.rect(0, 15, 15, 15, 1); c.rect(0, 0, 0, 15, 1); c.rect(15, 0, 15, 15, 1)
    c.rect(10, 3, 11, 4, 2); c.rect(3, 9, 4, 10, 1); c.rect(4, 3, 7, 3, 3)
    return save('drum_top', c, [1, 1])


def sticker():
    c = C(8, 8, ['#bd5a44', '#efe6cc', '#2e3436', '#c8c2ae'])
    c.stamp(0, 0, ['.wwwwww.', 'wkkkkkkw', 'wwwwwkkw', 'wwwwkkww', 'wwwkkwww', 'wwwkkwww', 'wwwkkwww', '.wwwwwg.'],
            {'w': 1, 'k': 2, 'g': 3})
    return save('sticker', c, [1, 1])


def crate():
    """짐상자 16×16(fit): 판자 3장(틈 1텍셀), 밝은 테두리 각목, 스텐실 화살표, 못."""
    c = C(16, 16, ['#cfa66c', '#b08850', '#6b4f36', '#4f6a6a', '#dcbc86'])
    for y in (5, 10): c.rect(0, y, 15, y, 2)
    c.rect(0, 0, 0, 15, 1); c.rect(15, 0, 15, 15, 1); c.rect(0, 0, 15, 0, 1); c.rect(0, 15, 15, 15, 1)
    c.rect(3, 2, 9, 2, 4)
    c.stamp(6, 6, ['.3.', '333', '.3.', '.3.'], {'3': 3})
    for x, y in ((2, 1), (13, 1), (2, 14), (13, 14)): c.p(x, y, 2)
    return save('crate', c, [1, 1])


def strap():
    c = C(4, 4, ['#7a6a46', '#5e5036', '#b4ae94']); c.rect(0, 0, 0, 3, 1); c.rect(3, 0, 3, 3, 1); c.rect(1, 1, 2, 2, 2)
    return save('strap', c, [1, 1])


def seat():
    c = C(16, 16, ['#3f5464', '#566e7e', '#2a3842', '#d0ccba'])
    c.rect(0, 0, 15, 0, 1); c.rect(2, 6, 13, 6, 2); c.rect(2, 11, 13, 11, 2)
    c.rect(9, 2, 12, 4, 3); c.rect(9, 4, 12, 4, 2)  # 터진 자리 테이프
    return save('seat', c, [1, 1])


def main():
    os.makedirs(OUT, exist_ok=True)
    tin_wall(); tin_roof()
    planks('planks', ['#c09464', '#d0a874', '#a87e52', '#5e4430', '#3a2c20', '#86aaa4', '#e0c08c', '#8a7550', '#9c9a52', '#7d8240'], 0)
    planks('planks_dark', ['#9a7650', '#ac8a60', '#86663f', '#4a3626', '#2e241c', '#729490', '#cfae7c', '#7a6646', '#9c9a52', '#7d8240'], 5)
    floor(); apron(); ground(); dirt()
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
