"""배경 건물 손칠 아틀라스 (Look 6 규칙 8, BUILDINGS.md 6). PIL 만 쓴다 — 사진·AI 이미지 없음.

칸마다 512×512 이음매 없는(타일링) 그림을 스크립트로 칠하고 2048×1024 아틀라스로 묶는다.
벽·지붕·덧문·문은 밝은 중성색으로 칠해 두고 색은 건물마다 곱한다(Unity MPB `_Tint` / Blender 객체 속성).
창 속·처마 띠·차양·바닥은 색을 박아 둔다.

붓 얼룩 규칙:
  - 큰 부드러운 면 먼저(저주파 2옥타브) → 가로로 누운 붓 자국(타원 얼룩) → 마지막 블러로 1px 점 제거.
  - 얼룩 크기: 4 m 칸 512px 에서 40~90px = 0.3~0.7 m → 길 높이 15~30 m 거리에서 화면 폭 1~2%.
  - 진폭: 휘도 std 6~9(텍스처) — 화면에선 면 음영과 합쳐 std 10~18 목표(지브리 20~40 의 절반).
  - 1px 반점(3×3 중앙값에서 휘도 ±12 튐) ≤ 0.3% — 아래 check() 로 칸마다 잰다.

    python -X utf8 tools/asset-gen/paint_atlas.py [--out art/source/town-buildings-v0/tex] [--seed 7]
"""
import argparse, json, os, random
from PIL import Image, ImageChops, ImageDraw, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
S = 512                      # 칸 한 변 px
TILE_M = 4.0                 # 칸 한 변이 덮는 실제 크기(m) — 벽·지붕·바닥 UV 는 m/4

# Look 6 고정 팔레트(규칙 2) 중 여기서 박는 색
SHADE_TEAL = (0x4d, 0x6b, 0x78)
WIN_DEEP = (0x2e, 0x33, 0x31)     # 창 속 = 짙은 그늘(같은 계열 짙은 갈색-청록)
EAVE_BROWN = (0x4a, 0x3a, 0x30)   # 처마 밑 작은 그늘 = 짙은 갈색(규칙 4)
PAVE = (0xa8, 0x9c, 0x86)
AWN_CREAM = (0xe6, 0xd9, 0xbd)
AWN_RED = (0xa8, 0x5c, 0x5e)


def noise(size, cells, seed, wrap=True, aspect=1.0):
    """이음매 없는 저주파 잡음(L, 평균 128). cells = 한 변 격자 수, aspect>1 이면 가로로 늘어짐."""
    rnd = random.Random(seed)
    cx, cy = max(2, int(cells / aspect)), max(2, cells)
    small = Image.new('L', (cx, cy))
    small.putdata([rnd.randint(0, 255) for _ in range(cx * cy)])
    big = Image.new('L', (cx * 3, cy * 3))
    for i in range(3):
        for j in range(3):
            big.paste(small, (i * cx, j * cy))
    big = big.resize((size * 3, size * 3), Image.BICUBIC).crop((size, size, size * 2, size * 2))
    return big.filter(ImageFilter.GaussianBlur(size / cells / 4))


def mix_noise(size, parts, seed):
    """[(cells, 무게, aspect)] 를 128 중심으로 합친 L 이미지."""
    acc = Image.new('L', (size, size), 128)
    for k, (cells, w, asp) in enumerate(parts):
        n = noise(size, cells, seed * 31 + k, aspect=asp)
        n = n.point(lambda v, w=w: int(128 + (v - 128) * w))
        acc = ImageChops.add(acc, n, 1.0, -128)
    return acc


def strokes(size, n, seed, rw=(40, 90), rh=(10, 24), dv=(-14, 14)):
    """가로로 누운 붓 자국(타원) — 랩어라운드로 칠해 이음매 없음. L, 평균 128."""
    rnd = random.Random(seed)
    lay = Image.new('L', (size, size), 128)
    d = ImageDraw.Draw(lay)
    for _ in range(n):
        x, y = rnd.uniform(0, size), rnd.uniform(0, size)
        w, h = rnd.uniform(*rw), rnd.uniform(*rh)
        v = 128 + rnd.randint(*dv)
        for ox in (-size, 0, size):
            for oy in (-size, 0, size):
                d.ellipse((x + ox - w / 2, y + oy - h / 2, x + ox + w / 2, y + oy + h / 2), fill=v)
    return lay.filter(ImageFilter.GaussianBlur(7))


def tint(base, lum, amp, warm=0.0):
    """평균색 base 위에 휘도 잡음 lum(L,128 중심) × amp/64 를 더한다. warm>0 이면 밝은 곳이 살짝 따뜻."""
    chans = []
    for c, val in enumerate(base):
        k = amp / 64.0 * (1.0 + (warm if c == 0 else -warm if c == 2 else 0))
        chans.append(lum.point(lambda v, val=val, k=k: max(0, min(255, int(val + (v - 128) * k)))))
    return Image.merge('RGB', chans)


def painted_surface(base, seed, amp=7.0, warm=0.08, n_strokes=46):
    lum = mix_noise(S, [(5, 1.0, 1.0), (11, 0.6, 1.6)], seed)
    lum = ImageChops.add(lum, strokes(S, n_strokes, seed + 5), 1.0, -128)
    im = tint(base, lum, amp, warm)
    return im.filter(ImageFilter.GaussianBlur(1.2))


def wall(seed):
    """중성 밝은 회반죽(곱하기용). 아래 0.6 m 은 조금 어둡게 — 벽 밑 때(텍스처 v 0 = 땅에서 반복이라 칸 아래 15%만)."""
    im = painted_surface((236, 232, 224), seed, amp=8.0)
    g = Image.new('L', (1, S))
    g.putdata([int(255 - max(0, (y - S * 0.85) / (S * 0.15)) * 22) for y in range(S)])
    g = g.resize((S, S))
    return ImageChops.multiply(im, Image.merge('RGB', (g, g, g)))


def roof(seed):
    """기와 줄 띠만(0.35 m 간격 = 45px). 띠 위쪽 밝고 아래쪽 어둡게, 띠마다 명도 흔들림. 기와 한 장씩은 안 그린다."""
    rnd = random.Random(seed)
    band = 45
    rows = Image.new('L', (1, S))
    vals = []
    shifts = [rnd.randint(-6, 6) for _ in range(S // band + 2)]
    for y in range(S):
        r, t = divmod(y, band)
        t /= band
        v = 140 + shifts[r] + (10 - 26 * t if t < 0.8 else -16 - 40 * (t - 0.8) / 0.2 * 0.5)
        vals.append(int(v))
    rows.putdata(vals)
    rows = rows.resize((S, S)).filter(ImageFilter.GaussianBlur(2.5))
    lum = ImageChops.add(rows, mix_noise(S, [(4, 0.9, 2.5), (9, 0.5, 3.0)], seed), 1.0, -128)
    lum = ImageChops.add(lum, strokes(S, 30, seed + 9, rw=(60, 140), rh=(8, 18), dv=(-10, 10)), 1.0, -128)
    return tint((230, 226, 220), lum, 9.0, 0.05).filter(ImageFilter.GaussianBlur(1.0))


def shutter(seed):
    """덧문(중성, 곱하기용): 가로 루버 6줄을 부드러운 띠로 + 테두리 조금 어둡게."""
    im = painted_surface((228, 228, 224), seed, amp=6.0, n_strokes=20)
    d = ImageDraw.Draw(im, 'RGBA')
    for i in range(14):
        y = 40 + i * 32
        d.rectangle((40, y + 18, S - 40, y + 28), fill=(60, 60, 60, 46))
    d.rectangle((0, 0, S - 1, S - 1), outline=(70, 70, 70, 70), width=26)
    return im.filter(ImageFilter.GaussianBlur(3))


def window(seed):
    """창: 밝은 테(벽색에 곱해도 밝게) + 짙은 속. 위 1/4 은 더 어둡게(인방 그늘), 아래 창턱은 밝게."""
    im = Image.new('RGB', (S, S), (228, 222, 210))
    inner = Image.new('L', (1, S))
    inner.putdata([int(255 - 70 * max(0, 1 - y / (S * 0.35))) for y in range(S)])
    deep = Image.new('RGB', (S - 96, S - 120), WIN_DEEP)
    gl = painted_surface(WIN_DEEP, seed, amp=5.0, warm=0.0, n_strokes=10).crop((0, 0, S - 96, S - 120))
    deep = ImageChops.multiply(gl, Image.merge('RGB', [inner.resize((S - 96, S - 120))] * 3))
    im.paste(deep, (48, 40))
    d = ImageDraw.Draw(im, 'RGBA')
    d.rectangle((S // 2 - 8, 40, S // 2 + 8, S - 80), fill=(150, 140, 125, 150))   # 가운데 문살
    d.rectangle((30, S - 80, S - 30, S - 50), fill=(245, 240, 230, 255))           # 창턱
    d.rectangle((48, S - 50, S - 48, S - 34), fill=(90, 80, 70, 120))              # 창턱 밑 작은 그늘
    return im.filter(ImageFilter.GaussianBlur(2.2))


def door(seed):
    """나무 문(중성, 곱하기용): 세로 판 3장 + 위쪽 그늘."""
    im = painted_surface((214, 210, 204), seed, amp=7.0, n_strokes=16)
    d = ImageDraw.Draw(im, 'RGBA')
    for x in (S // 3, 2 * S // 3):
        d.rectangle((x - 6, 0, x + 6, S), fill=(40, 40, 40, 60))
    d.rectangle((0, 0, S, 60), fill=(30, 30, 30, 90))
    return im.filter(ImageFilter.GaussianBlur(3))


def eave(seed):
    """처마 띠: 위쪽(처마 바로 밑)이 가장 짙고 아래로 풀리는 짙은 갈색."""
    g = Image.new('L', (1, S))
    g.putdata([int(255 * (0.55 + 0.45 * (y / S) ** 1.4)) for y in range(S)])
    base = painted_surface(EAVE_BROWN, seed, amp=4.0, warm=0.0, n_strokes=14)
    lift = Image.new('RGB', (S, S), (120, 104, 92))
    m = g.resize((S, S))
    return Image.composite(base, lift, m.point(lambda v: 255 - int((v - 140) * 1.9) if v > 140 else 255))


def awning(seed):
    """차양: 세로 줄무늬(크림/바랜 적) 6줄, 아래 끝 그늘. 줄 경계는 붓으로 번짐."""
    im = Image.new('RGB', (S, S), AWN_CREAM)
    d = ImageDraw.Draw(im)
    w = S // 6
    for i in range(0, 6, 2):
        d.rectangle((i * w, 0, i * w + w, S), fill=AWN_RED)
    lum = mix_noise(S, [(5, 1.0, 0.5)], seed).point(lambda v: int(128 + (v - 128) * 0.15))
    im = ImageChops.add(im, Image.merge('RGB', [lum] * 3), 1.0, -128)
    d = ImageDraw.Draw(im, 'RGBA')
    d.rectangle((0, S - 70, S, S), fill=(40, 30, 30, 70))
    return im.filter(ImageFilter.GaussianBlur(3.5))


def pave(seed):
    return painted_surface(PAVE, seed, amp=7.0, warm=0.05, n_strokes=40)


CELLS = [  # (이름, 함수, 열, 행) — 아틀라스 4×2
    ('wall', wall, 0, 0), ('roof', roof, 1, 0), ('shutter', shutter, 2, 0), ('window', window, 3, 0),
    ('door', door, 0, 1), ('eave', eave, 1, 1), ('awning', awning, 2, 1), ('pave', pave, 3, 1),
]


def speck_pct(im):
    """surface_noise.py 와 같은 1px 반점 정의: 3×3 중앙값에서 휘도 ±12 넘게 튄 픽셀 %."""
    L = im.convert('L')
    diff = ImageChops.difference(L, L.filter(ImageFilter.MedianFilter(3)))
    h = diff.histogram()
    return 100.0 * sum(h[13:]) / (L.size[0] * L.size[1])


def lum_std(im):
    L = im.convert('L').filter(ImageFilter.GaussianBlur(1))
    h = L.histogram(); n = sum(h)
    m = sum(i * c for i, c in enumerate(h)) / n
    return (sum(c * (i - m) ** 2 for i, c in enumerate(h)) / n) ** 0.5


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--out', default=os.path.join(ROOT, 'art', 'source', 'town-buildings-v0', 'tex'))
    ap.add_argument('--seed', type=int, default=7)
    a = ap.parse_args()
    os.makedirs(a.out, exist_ok=True)
    atlas = Image.new('RGB', (S * 4, S * 2))
    rects, report = {}, {}
    for k, (name, fn, cx, cy) in enumerate(CELLS):
        im = fn(a.seed * 100 + k)
        im.save(os.path.join(a.out, f'{name}.png'))
        atlas.paste(im, (cx * S, cy * S))
        # Unity UV(왼쪽 아래 원점): u0, v0, u1, v1
        rects[name] = [cx / 4, 1 - (cy + 1) / 2, (cx + 1) / 4, 1 - cy / 2]
        report[name] = dict(speck=round(speck_pct(im), 3), lumStd=round(lum_std(im), 1))
    atlas.save(os.path.join(a.out, 'atlas.png'))
    meta = dict(cell_px=S, tile_m=TILE_M, rects=rects, check=report,
                note='wall/roof/shutter/door 는 중성색 — 건물별 _Tint 곱하기. 칸마다 이음매 없음.')
    with open(os.path.join(a.out, 'atlas.json'), 'w', encoding='utf8') as f:
        json.dump(meta, f, ensure_ascii=False, indent=1)
    for n, r in report.items():
        print(f"{n:8s} speck {r['speck']:.3f}%  {'O' if r['speck'] <= 0.3 else 'X'}   lumStd {r['lumStd']}")


if __name__ == '__main__':
    main()
