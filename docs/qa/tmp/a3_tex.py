# 과제 1124 — A3 텍스처. 결과 = F1/Assets/Art/SceneA3/Gen/
#  R2 텍셀 통일(1 m ≈ 32 텍셀): 나무는 A2f 와 같은 3단 색을 쓰되 줄이는 비율을 '32 텍셀/m' 에 맞춤(A2f 는 1/4 → 16/m 블록이라 화면에서 따로 놂)
#   나무 실측 텍셀/m(a3_probe.cs, 원본 1024×8192): 01 66.8 · 02 64.3 · 03 53.6 · 05 46.4 → 저장 크기 = 원본 × 32/실측
#   마른 풀 띠 10.5/m → 3배 해상도로 다시 만듦. 하늘 512×128(구름 한 텍셀 ≈ 화면 9 px) → 4096×1024(≈ 1.1 px), 구름 경계는 텍셀 단위 그대로
#  M1·M2 바위: 따뜻한 회색 3단 64² (2 m 한 장 = 32/m). 값 전부 가설.
import os, random, math
from PIL import Image, ImageFilter, ImageStat, ImageDraw
A = 'F1/Assets/Art/SceneA2/'; OUT = 'F1/Assets/Art/SceneA3/Gen/'
os.makedirs(OUT, exist_ok=True)

def field(W, H, gx, gy, seed):
    rnd = random.Random(seed); s = Image.new('L', (gx, gy)); s.putdata([rnd.randrange(256) for _ in range(gx * gy)])
    t = Image.new('L', (gx * 3, gy * 3))
    for i in range(3):
        for j in range(3): t.paste(s, (i * gx, j * gy))
    return t.resize((W * 3, H * 3), Image.BICUBIC).crop((W, H, 2 * W, 2 * H))
def band3(v, lo, hi): return 0 if v < lo else (1 if v < hi else 2)

def posterize(src, dst, scale, blur_c, blur_l, ramp, q):
    im = Image.open(A + src).convert('RGBA'); W0, H0 = im.size
    sm = im.resize((max(1, round(W0 * scale)), max(1, round(H0 * scale))), Image.BOX); a = sm.getchannel('A').point(lambda v: 255 if v >= 128 else 0)
    a = a.filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.MinFilter(3))
    mean = tuple(int(v) for v in ImageStat.Stat(sm.convert('RGB'), mask=a).mean)
    rgb = Image.composite(sm.convert('RGB'), Image.new('RGB', sm.size, mean), a)
    lum = rgb.convert('L').filter(ImageFilter.GaussianBlur(blur_l))
    hist = sorted(v for v, m in zip(lum.get_flattened_data(), a.get_flattened_data()) if m); lo, hi = hist[int(len(hist) * q[0])], hist[int(len(hist) * q[1])]
    out = Image.new('RGB', sm.size); op = out.load(); lp = lum.load()
    for y in range(sm.size[1]):
        for x in range(sm.size[0]): op[x, y] = ramp[band3(lp[x, y], lo, hi)]
    out = out.filter(ImageFilter.ModeFilter(5)).convert('RGBA'); out.putalpha(a)
    out.save(OUT + dst); return out.size

TREE = {1: [(128, 54, 52), (200, 96, 46), (236, 160, 70)], 2: [(140, 90, 50), (214, 160, 52), (242, 206, 96)],
        3: [(120, 44, 52), (186, 64, 46), (226, 116, 62)], 5: [(134, 70, 48), (210, 124, 48), (240, 182, 80)]}
DENS = {1: 66.8, 2: 64.3, 3: 53.6, 5: 46.4}
for k, rp in TREE.items():
    s = 32 / DENS[k]; f = s / 0.25                                  # A2f(1/4) 대비 몇 배 해상도인가 → 흐림도 같은 배수(덩어리 크기는 m 단위로 같게)
    print('tree', k, posterize(f'Gen/autumn_tree_0{k}_soft.png', f'autumn_tree_0{k}_r2.png', s, 1 * f, 1.6 * f, rp, (0.45, 0.8)))

# 마른 풀 띠 192×768 (a2f_tex 와 같은 장·같은 색, 3배 해상도)
W, H = 192, 768
d = field(W, H, 3, 16, 5).load(); DRY = [(178, 160, 86), (196, 176, 98), (160, 168, 74)]
dry = Image.new('RGBA', (W, H)); dp = dry.load()
for y in range(H):
    for x in range(W):
        t = x / W + (d[x, y] - 128) / 255 * 0.5
        dp[x, y] = (DRY[1] if t < 0.35 else (DRY[0] if t < 0.62 else DRY[2])) + ((255 if t < 0.85 else 0),)
dry.save(OUT + 'dry_strip_r2.png')

# 하늘 4096×1024: a2d_sky 와 같은 그라데이션·같은 구름 배치(씨앗 7), 해상도만 8배
W, H = 4096, 1024
TOP, MID, HOR = (104, 156, 210), (150, 190, 226), (240, 226, 192)
def lerp(a, b, t): return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(3))
col = Image.new('RGB', (1, H)); cp = col.load()
for y in range(H):
    v = 1 - y / (H - 1); cp[0, y] = lerp(HOR, MID, min(1, v / 0.18)) if v < 0.18 else lerp(MID, TOP, min(1, (v - 0.18) / 0.6))
sky = col.resize((W, H), Image.NEAREST); S = 8
lit = Image.new('L', (W, H), 0); shd = Image.new('L', (W, H), 0); dl, ds = ImageDraw.Draw(lit), ImageDraw.Draw(shd)
random.seed(7)
for _ in range(9):
    cx, cv = random.uniform(0, 512), random.uniform(0.07, 0.28); w = random.uniform(14, 34); h = random.uniform(2.4, 5.0)
    for k in range(4):
        ox, oy, s = random.uniform(-w * .5, w * .5), random.uniform(-h * .4, h * .4), random.uniform(.5, 1)
        for wrap in (-512, 0, 512):
            x0 = (cx + ox + wrap) * S; yc = ((1 - cv) * (128 - 1) - oy) * S; rw, rh = w * s * S, h * s * S
            ds.ellipse((x0 - rw, yc - rh, x0 + rw, yc + rh), fill=255)
            dl.ellipse((x0 - rw, yc - rh, x0 + rw, yc + rh * 0.3), fill=255)        # 윗부분만 밝음, 아래 30 % 아래는 회청 그늘
sky = Image.composite(Image.blend(sky, Image.new('RGB', (W, H), (226, 232, 240)), 0.42), sky, shd)
sky = Image.composite(Image.blend(sky, Image.new('RGB', (W, H), (255, 250, 240)), 0.55), sky, lit)
sky.save(OUT + 'sky_r2.png')

# 바위 64² (2 m): 따뜻한 회색 3단 큰 얼룩 + 금 몇 줄
W = 64; g = Image.blend(field(W, W, 3, 3, 11), field(W, W, 7, 7, 12), 0.4).load()
ROCK = [(132, 124, 116), (160, 151, 140), (186, 178, 166)]
rock = Image.new('RGB', (W, W)); rp = rock.load()
for y in range(W):
    for x in range(W): rp[x, y] = ROCK[band3(g[x, y], 105, 155)]
rock.save(OUT + 'rock.png')
# 덤불 64² (2 m): 짙은 초록 3단(풀보다 어둡게 — 덩어리가 풀밭에서 떠 보이도록)
g = Image.blend(field(W, W, 4, 4, 21), field(W, W, 9, 9, 22), 0.4).load()
BUSH = [(70, 104, 50), (94, 130, 58), (122, 156, 70)]
bush = Image.new('RGB', (W, W)); bp = bush.load()
for y in range(W):
    for x in range(W): bp[x, y] = BUSH[band3(g[x, y], 105, 155)]
bush.save(OUT + 'bush.png')
print('done', sorted(os.listdir(OUT)))
