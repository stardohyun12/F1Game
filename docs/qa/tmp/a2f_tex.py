# 과제 1110 — A2f 텍스처(A2d 기반, ① 질감 마무리). 결과 = Assets/Art/SceneA2/GenF/
#  길: 따뜻한 회색 + 큰 얼룩 3단 + 바랜 중앙 점선 + 가장자리 흙먼지 3단 (u 0..1 = 길 폭 5 m, v 0..1 = 길이 20 m — 메시 UV 는 build 에서 맞춤)
#  풀: 밝은 연두~황록 3단 큰 얼룩(이음매 없이 반복) / 마른 풀 띠: 길가 1.8 m 띠, 바깥쪽 들쭉날쭉 컷아웃
#  나무·드럼·타이어: 원본 색을 크게 뭉갠 위에 밝기 3단 계단 / 원경 지붕: 주황 2단
# 값 전부 가설. 1px 반점을 늘리지 않도록 모든 얼룩은 저해상도 장(場)을 부드럽게 키운 뒤 3단으로 자른다.
import os, random
from PIL import Image, ImageFilter, ImageStat
A = 'F1/Assets/Art/SceneA2/'; OUT = A + 'GenF/'
os.makedirs(OUT + 'Gen', exist_ok=True); os.makedirs(OUT + 'kit', exist_ok=True)

def field(W, H, gx, gy, seed):
    """이음매 없는 부드러운 얼룩 장(0~255). gx×gy 칸 난수를 3×3 로 깔고 키운 뒤 가운데만."""
    rnd = random.Random(seed); s = Image.new('L', (gx, gy)); s.putdata([rnd.randrange(256) for _ in range(gx * gy)])
    t = Image.new('L', (gx * 3, gy * 3))
    for i in range(3):
        for j in range(3): t.paste(s, (i * gx, j * gy))
    return t.resize((W * 3, H * 3), Image.BICUBIC).crop((W, H, 2 * W, 2 * H))

def mix(a, b, w): return Image.blend(a, b, w)
def band3(v, lo, hi): return 0 if v < lo else (1 if v < hi else 2)

# ---- 길 160×640 (예전 64px 타일과 같은 밀도: 폭 5 m = 160 px, 길이 20 m = 640 px)
W, H = 160, 640
f = mix(field(W, H, 4, 8, 1), field(W, H, 9, 18, 2), 0.35).load()
road = Image.new('RGB', (W, H)); px = road.load()
ROAD = [(124, 117, 108), (141, 133, 122), (156, 147, 133)]        # 따뜻한 회색 3단(청·보라 없음: R ≥ G ≥ B)
DUST = [(170, 152, 120), (186, 167, 130)]                         # 가장자리 흙먼지
LINE = (196, 186, 158)                                            # 바랜 중앙선(크림)
for y in range(H):
    for x in range(W):
        n = f[x, y]; c = ROAD[band3(n, 105, 160)]
        e = min(x, W - 1 - x) / W                                 # 가장자리에서 거리(폭 비율)
        e += (n - 128) / 255 * 0.06                               # 경계를 얼룩으로 흔듦(곧은 줄 대신)
        if e < 0.05: c = DUST[1]
        elif e < 0.11: c = DUST[0]
        elif e < 0.17: c = tuple((a + b) // 2 for a, b in zip(c, DUST[0]))
        if abs(x - W / 2) < 2.5 and (y % 160) < 80 and n > 70:   # 점선 2.5 m / 2.5 m, 군데군데 벗겨짐
            c = tuple((a * 2 + b * 3) // 5 for a, b in zip(c, LINE))
        px[x, y] = c
road.save(OUT + 'Gen/road_a2f.png')

# ---- 풀 1024×948 (원본과 같은 크기·같은 UV), ① 풀색 3단
W, H = 1024, 948
g = mix(mix(field(W, H, 5, 5, 3), field(W, H, 11, 11, 4), 0.4), field(W, H, 29, 27, 6), 0.3).load()   # 2차: '빈 단색 면' 지적 → 29칸 장을 섞어 경계에 작은 섬
GR = [(118, 150, 58), (146, 176, 70), (176, 198, 92)]             # 황록 그늘 / 연두 / 밝은 연두
grass = Image.new('RGB', (W, H)); gp = grass.load()
for y in range(H):
    for x in range(W): gp[x, y] = GR[band3(g[x, y], 100, 158)]
grass.save(OUT + 'Gen/grass_tile_soft.png')

# ---- 마른 풀 띠 64×256 (u 0 = 길가, 1 = 바깥), 바깥 가장자리 들쭉날쭉 알파
W, H = 64, 256
d = field(W, H, 3, 16, 5).load()
DRY = [(178, 160, 86), (196, 176, 98), (160, 168, 74)]
dry = Image.new('RGBA', (W, H)); dp = dry.load()
for y in range(H):
    for x in range(W):
        t = x / W + (d[x, y] - 128) / 255 * 0.5
        c = DRY[1] if t < 0.35 else (DRY[0] if t < 0.62 else DRY[2])
        dp[x, y] = c + ((255 if t < 0.85 else 0),)
dry.save(OUT + 'Gen/dry_strip.png')

# ---- 나무·드럼·타이어: 원본을 줄이고 큰 얼룩 색 위에 밝기 3단
def posterize(src, dst, k, blur_c, blur_l, steps=(0.74, 1.0, 1.2), ramp=None, q=(1 / 3, 2 / 3), keep=0.0):
    im = Image.open(A + src).convert('RGBA'); W0, H0 = im.size
    sm = im.resize((max(1, W0 // k), max(1, H0 // k)), Image.BOX); a = sm.getchannel('A').point(lambda v: 255 if v >= 128 else 0)
    a = a.filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.MinFilter(3))   # 닫기: 면 안 1px 구멍 메움(외곽선은 그대로)
    mean = tuple(int(v) for v in ImageStat.Stat(sm.convert('RGB'), mask=a).mean)
    rgb = Image.composite(sm.convert('RGB'), Image.new('RGB', sm.size, mean), a)   # 투명부 색을 평균으로 채워 흐림이 가장자리를 검게 안 하게
    base = rgb.filter(ImageFilter.GaussianBlur(blur_c)); lum = rgb.convert('L').filter(ImageFilter.GaussianBlur(blur_l))
    hist = sorted(v for v, m in zip(lum.get_flattened_data(), a.get_flattened_data()) if m); lo, hi = hist[int(len(hist) * q[0])], hist[int(len(hist) * q[1])]
    out = Image.new('RGB', sm.size); op = out.load(); bp = base.load(); lp = lum.load()
    for y in range(sm.size[1]):
        for x in range(sm.size[0]):
            b = band3(lp[x, y], lo, hi)
            op[x, y] = ramp[b] if ramp else tuple(min(255, int(c * steps[b])) for c in bp[x, y])
    if keep: out = Image.blend(out, rgb.filter(ImageFilter.ModeFilter(3)), keep)   # 원본 무늬(라벨·테)를 조금 남김
    out = out.filter(ImageFilter.ModeFilter(5)).convert('RGBA'); out.putalpha(a)
    out.resize((W0, H0), Image.NEAREST).save(OUT + dst)
    return lo, hi

# 나무 아틀라스는 빛이 구워져 있다(그늘 = 거의 검정, 빛 = 주황 반점). 1/4 로 줄여 반점을 덩어리로 뭉갠 뒤 밝기 3등분 → 나무마다 단풍 3단 색
#   그늘도 검정이 아니라 붉은 갈색. 나무 재질 채도·WB 는 build 에서 중립으로(이미 색이 정해져 있어 ×2.6 이면 터진다)
TREE = {1: [(128, 54, 52), (200, 96, 46), (236, 160, 70)],     # 주황
        2: [(140, 90, 50), (214, 160, 52), (242, 206, 96)],    # 노랑
        3: [(120, 44, 52), (186, 64, 46), (226, 116, 62)],     # 빨강
        5: [(134, 70, 48), (210, 124, 48), (240, 182, 80)]}    # 주황노랑
for k, rp in TREE.items(): posterize(f'Gen/autumn_tree_0{k}_soft.png', f'Gen/autumn_tree_0{k}_soft.png', 4, 1, 1.6, ramp=rp, q=(0.45, 0.8))   # 1차 캡처에서 나무가 밝은 단 하나로 납작해 그늘·중간 단을 넓힘
# 드럼: 원본 색을 뭉개면 흐린 얼룩이 돼서(1차 캡처) 나무처럼 구운 밝기만 3등분 → 드럼마다 한 색 3단
DRUM = {'drum_01': [(48, 92, 98), (66, 124, 126), (108, 156, 150)],       # 칙칙한 청록(2차: 밝은 하늘색은 '레고 블록'으로 읽힘)
        'drum_02': [(140, 60, 40), (190, 92, 50), (226, 140, 80)],       # 녹 주황
        'drum_03': [(168, 136, 68), (212, 178, 88), (240, 214, 140)]}    # 크림 노랑
for n, rp in DRUM.items(): posterize(f'kit/{n}.png', f'kit/{n}.png', 1, 0.6, 0.8, ramp=rp, q=(0.3, 0.7), keep=0.4)
for n in ('tires_01', 'tires_02'): posterize(f'kit/{n}.png', f'kit/{n}.png', 1, 1.5, 1.2, (0.85, 1.0, 1.12))

# ---- 원경 지붕 32×32: 주황 2단 줄(기와 결)
roof = Image.new('RGB', (32, 32)); rp = roof.load()
for y in range(32):
    for x in range(32): rp[x, y] = (214, 118, 58) if (y // 4) % 2 == 0 else (190, 96, 50)
roof.save(OUT + 'Gen/roof_far.png')
print('done', sorted(os.listdir(OUT + 'Gen')), sorted(os.listdir(OUT + 'kit')))
