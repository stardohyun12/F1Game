# 과제 1525 측정: 같은 게임 컷 b_game(A4e vs A5)에서 집·나무·길·풀·소품 크롭 → 텍셀 크기 px·색 수·명암 단계 수 + crops.png, a4e_vs_a5.png
# 텍셀 px = 가로로 같은 RGB 가 이어지는 길이(run) 중앙값·평균, 720 렌더 해상도 기준(1080 캡처를 2/3 nearest 로 되돌려 셈)
# 색 수 = 크롭 넓이 0.5% 이상 차지하는 RGB 수 (전체 고유색 수는 괄호)
# 명암 단계 = 위 색들의 휘도(0.299R+0.587G+0.114B)를 정렬해 8 이내 간격은 한 단계로 묶은 수
import sys, statistics
from PIL import Image, ImageDraw
R = 'D:/Github/F1Game/docs/qa/evidence/'
A, B = Image.open(R + 'scene-a4e/b_game.png').convert('RGB'), Image.open(R + 'scene-a5/b_game.png').convert('RGB')
CROPS = [('house', (1220, 440, 1430, 640)), ('tree', (1480, 180, 1880, 600)), ('road', (760, 880, 1100, 1060)),
         ('grass', (0, 560, 280, 690)), ('props', (1180, 700, 1600, 980))]

def measure(im):
    W0, H0 = im.size; r7 = im.resize((W0 * 2 // 3, H0 * 2 // 3), Image.NEAREST)  # 720 렌더 해상도로 되돌려 run 을 셈
    w, h = r7.size; px = r7.load(); runs = []
    for y in range(h):
        n = 1
        for x in range(1, w):
            if px[x, y] == px[x - 1, y]: n += 1
            else: runs.append(n); n = 1
        runs.append(n)
    w, h = im.size; cols = im.getcolors(w * h); tot = w * h
    big = [c for k, c in cols if k >= tot * 0.005]
    ls = sorted(0.299 * r + 0.587 * g + 0.114 * b for r, g, b in big); steps = 0; last = -99
    for l in ls:
        if l - last > 8: steps += 1
        last = l
    return statistics.median(runs), sum(runs) / len(runs), len(big), len(cols), steps

out = []
for nm, box in CROPS:
    a, b = measure(A.crop(box)), measure(B.crop(box))
    out.append((nm, a, b))
    print(f'{nm:6s} A4e run med {a[0]:.1f} avg {a[1]:.1f} colors {a[2]} ({a[3]}) steps {a[4]} | A5 run med {b[0]:.1f} avg {b[1]:.1f} colors {b[2]} ({b[3]}) steps {b[4]}')

# crops.png: 줄마다 요소, 왼쪽 A4e / 오른쪽 A5, 각 크롭 2배(nearest)
S = 2; rows = []
for nm, box in CROPS:
    w, h = box[2] - box[0], box[3] - box[1]; k = min(S, 840 / w)
    ca, cb = [im.crop(box).resize((int(w * k), int(h * k)), Image.NEAREST) for im in (A, B)]
    rows.append((nm, ca, cb))
W = max(r[1].width for r in rows) * 2 + 30; H = sum(r[1].height + 26 for r in rows)
C = Image.new('RGB', (W, H), (30, 30, 30)); d = ImageDraw.Draw(C); y = 0
for (nm, ca, cb), (_, a, b) in zip(rows, out):
    d.text((6, y + 6), f'{nm}  A4e: run {a[1]:.1f}px colors {a[2]} steps {a[4]}', fill=(240, 240, 240))
    d.text((W // 2 + 6, y + 6), f'A5: run {b[1]:.1f}px colors {b[2]} steps {b[4]}', fill=(240, 240, 240))
    C.paste(ca, (0, y + 26)); C.paste(cb, (W // 2 + 15, y + 26)); y += ca.height + 26
C.save(R + 'scene-a5/crops.png')

# a4e_vs_a5.png: 위 b_game, 아래 house_fish — 왼쪽 A4e / 오른쪽 A5 (반 크기)
ims = [[Image.open(R + f'scene-{s}/{f}').convert('RGB').resize((960, 540), Image.NEAREST) for s in ('a4e', 'a5')] for f in ('b_game.png', 'house_fish.png')]
V = Image.new('RGB', (1930, 1110), (30, 30, 30)); d = ImageDraw.Draw(V)
for r, pair in enumerate(ims):
    for c, im in enumerate(pair): V.paste(im, (c * 970, 30 + r * 540 + r * 0))
d.text((8, 8), 'A4e (houses e, old props)', fill=(255, 255, 255)); d.text((978, 8), 'A5 (houses g, kit-style props/bush)', fill=(255, 255, 255))
V.save(R + 'scene-a5/a4e_vs_a5.png')
