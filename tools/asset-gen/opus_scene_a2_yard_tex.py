"""장면 A2 마당 땅 — 사진 기반 바퀴 자국·기름 얼룩(0.1~0.3m) 원본 텍스처 (아트2, 과제 0525). PIL 만.
    python tools/asset-gen/opus_scene_a2_yard_tex.py
마당 = x −10~10, y −9~4.5 m (카트 원점, +y 마을 쪽) → 32텍셀/m = 640×432, 반복 없는 한 장.
  바탕: V1b ground.png·dirt.png(8m 타일, ambientCG CC0 사진에서 만든 것)를 낮은 주파수 마스크로 섞어 반복을 숨김.
        마스크 = Ground081 사진 밝기를 크게 흐린 것.
  바퀴 자국: 카트(바퀴 간격 1.1m·폭 0.18m)가 정비소(−y)에서 길(+y)로 나간 자국 여러 겹 + 옆 주차 자리로 도는 호.
        자국 안의 무늬 = Rubber004 사진 밝기(고무 결) → 어둡게 곱함.
  얼룩: Ground081 사진의 가장 어두운 3% 를 덩이로 잡아 주차 자리·드럼 옆에만 남김(0.1~0.8m) + Gravel022 의 어두운 돌을 0.1m 점으로.
결과는 V5 굽기 전 원본(SourceUV 용). 굽기·32색은 opus_scene_a2_far.py 가 한다.
"""
import json, math, os, random
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageOps

R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
J = lambda *p: os.path.join(R, *p)
OUT = J('art', 'source', 'scene-a2-yard'); os.makedirs(OUT, exist_ok=True)
D, X0, X1, Y0, Y1 = 32, -10, 10, -9, 4.5
W, H = int((X1 - X0) * D), int((Y1 - Y0) * D)
px = lambda x, y: ((x - X0) * D, (Y1 - y) * D)  # 위 = +y(마을 쪽)
random.seed(25)


def tiled(path, tile_m, off=(0, 0)):
    t = Image.open(path).convert('RGB').resize((int(tile_m * D),) * 2, Image.BOX); im = Image.new('RGB', (W, H))
    for i in range(-1, W // t.width + 2):
        for j in range(-1, H // t.height + 2): im.paste(t, (i * t.width + off[0], j * t.height + off[1]))
    return im


def photo_gray(name, size_px, tile_m):  # 사진 밝기를 실제 크기(tile_m)로 줄여 깔기
    p = J('art', 'source', 'photos', 'scene-a', name, f'{name}_1K-JPG_Color.jpg')
    t = ImageOps.grayscale(Image.open(p)).resize((int(tile_m * D),) * 2, Image.BOX); im = Image.new('L', size_px)
    for i in range(0, size_px[0], t.width):
        for j in range(0, size_px[1], t.height): im.paste(t, (i, j))
    return im


V1B = J('art', 'source', 'scene-a-v1b', 'tex')
base = tiled(os.path.join(V1B, 'dirt.png'), 8)
mask = photo_gray('Ground081', (W, H), 9).filter(ImageFilter.GaussianBlur(30)); mask = ImageOps.autocontrast(mask, cutoff=2)
img = ImageChops.multiply(base, Image.merge('RGB', [mask.point(lambda v: 222 + v * 33 // 255)] * 3))  # 0.87~1.0 밝기 얼룩으로 반복 숨김

# 바퀴 자국 — 선마다 바퀴 두 줄
trk = Image.new('L', (W, H), 0); dr = ImageDraw.Draw(trk)


def tyre_pair(pts, gauge=1.1, w=.18, a=170):
    for side in (-.5, .5):
        line = []
        for i, (x, y) in enumerate(pts):
            nx, ny = pts[min(i + 1, len(pts) - 1)][0] - pts[max(i - 1, 0)][0], pts[min(i + 1, len(pts) - 1)][1] - pts[max(i - 1, 0)][1]
            L = math.hypot(nx, ny) or 1; line.append(px(x + ny / L * gauge * side, y - nx / L * gauge * side))
        dr.line(line, fill=a, width=max(2, round(w * D)), joint='curve')


for k in range(7):  # 정비소 → 길, 조금씩 어긋난 여러 번(겹친 곳이 더 진함)
    dx = random.uniform(-.35, .35); bend = random.uniform(-.5, .5); y0 = random.uniform(-9, -4); y1 = random.uniform(1, 4.5)
    n = int((y1 - y0) * 2)
    tyre_pair([(dx + bend * math.sin((y0 + t / 2 + 9) / 13 * math.pi), y0 + t / 2) for t in range(n + 1)], w=random.uniform(.12, .2), a=random.randint(110, 160))
for cx, cy, r, a0, a1 in ((-3.5, -3, 3.5, -10, 80), (3.8, -3, 3.2, 100, 185), (-1, -7, 2.4, 20, 150)):  # 옆 주차 자리로 도는 호
    tyre_pair([(cx + r * math.cos(math.radians(a)), cy + r * math.sin(math.radians(a))) for a in range(a0, a1, 6)], a=130)
fade = photo_gray('Ground037', (W, H), 5).filter(ImageFilter.GaussianBlur(10)); fade = ImageOps.autocontrast(fade, cutoff=3).point(lambda v: 90 + v * 165 // 255)
trk = ImageChops.multiply(trk.filter(ImageFilter.GaussianBlur(.7)), fade)  # 자국이 군데군데 끊겨 보이게
rub = photo_gray('Rubber004', (W, H), .6); rub = ImageOps.autocontrast(rub, cutoff=2)
dark = ImageChops.multiply(img, Image.merge('RGB', [rub.point(lambda v: int(120 + v * .35))] * 3))
img = Image.composite(dark, img, trk)

# 얼룩 — 사진의 어두운 덩이를 주차 자리·드럼 옆 근처에만
tot = W * H
G81 = ImageOps.grayscale(Image.open(J('art', 'source', 'photos', 'scene-a', 'Ground081', 'Ground081_1K-JPG_Color.jpg')))
stain = Image.new('L', (W, H), 0)
SPOT = [(-3.5, -3, 1.6, .06), (3.8, -3, 1.4, .05), (0, -6.5, 1.8, .07), (3.3, .9, .9, .05), (-3.2, .6, .8, .04), (.3, 1.5, 1.0, .03), (-1, 3.6, .7, .03), (6, 2.5, 1.2, .04)]
for x, y, r, frac in SPOT:  # 자리마다 사진의 다른 곳을 잘라 가장 어두운 frac 만 덩이로
    s = int(2 * r * D); cw = int(r * 400); cx, cy = random.randint(64, 960 - cw), random.randint(64, 960 - cw)  # 사진 1장 ≈ 2.5m, 가장자리 64px 비움
    pad = 16; win = G81.resize((1024 * s // cw,) * 2, Image.BOX); ox, oy = cx * s // cw, cy * s // cw
    win = win.crop((ox - pad, oy - pad, ox + s + pad, oy + s + pad)) if ox >= pad and oy >= pad else win.crop((ox, oy, ox + s + 2 * pad, oy + s + 2 * pad))
    hp = ImageChops.subtract(win, win.filter(ImageFilter.GaussianBlur(5)), 1, 128).crop((pad, pad, pad + s, pad + s))  # 국소 어두운 점(0.1~0.3m)
    h = hp.histogram(); acc = 0; thr = 0
    for v, c in enumerate(h):
        acc += c
        if acc > s * s * frac * 2.5: thr = min(v, 118); break
    fall = Image.new('L', (s, s), 0); ImageDraw.Draw(fall).ellipse((s * .1, s * .1, s * .9, s * .9), fill=255)
    fall = ImageChops.multiply(fall.filter(ImageFilter.GaussianBlur(s / 6)).point(lambda v: 255 if v > 140 else 0), fall)
    blob = ImageChops.multiply(hp.point(lambda v: 230 if v <= thr else 0), fall)
    a, b = px(x - r, y + r); stain.paste(ImageChops.lighter(stain.crop((int(a), int(b), int(a) + s, int(b) + s)), blob), (int(a), int(b)))
grav = photo_gray('Gravel022', (W, H), 1.2); grav = ImageOps.autocontrast(grav, cutoff=1).point(lambda v: 150 if v < 14 else 0)
stain = ImageChops.lighter(stain,ImageChops.multiply(grav, fade.point(lambda v: max(0, v - 120) * 2)))
oil = Image.new('RGB', (W, H), (70, 56, 42)); img = Image.composite(oil, img, stain.point(lambda v: int(v * .8)))
# 굽기·32색에서 올리브로 떨어지지 않게 붉은 흙 쪽으로(공용 팔레트의 (177,148,106)·(158,127,78) 근처로 가게), 대비도 조금 줄임
img = Image.merge('RGB', [c.point(lambda v, k=k: int(min(255, v * k))) for c, k in zip(img.split(), (1.0, .86, .74))])
img = Image.blend(img, img.filter(ImageFilter.GaussianBlur(1.2)), .35)
img.save(os.path.join(OUT, 'yard_ground.png'))
json.dump(dict(texels_per_m=D, area_m=dict(x=[X0, X1], y=[Y0, Y1]), size_px=[W, H],
               sources=['art/source/scene-a-v1b/tex/ground.png', 'art/source/scene-a-v1b/tex/dirt.png',
                        'ambientCG Ground081 (CC0)', 'ambientCG Rubber004 (CC0)', 'ambientCG Gravel022 (CC0)'],
               stain_pct=round(sum(i * c for i, c in enumerate(stain.histogram())) / 255 / tot * 100, 1),
               track_pct=round(sum(i * c for i, c in enumerate(trk.histogram())) / 255 / tot * 100, 1)),
          open(os.path.join(OUT, 'yard_ground.json'), 'w'), indent=1)
print('OK', W, H)
