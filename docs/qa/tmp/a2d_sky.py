# 과제 1046 — A2d 하늘: 늦가을 오후 파랑(위) → 크림(지평선) + 흰 구름 몇 점(낮은 해상도 덩어리). u = 방위(0~1 한 바퀴), v = 고도(0 지평선 ~ 1 천정)
import random, math
from PIL import Image
W, H = 512, 128
TOP, MID, HOR = (104, 156, 210), (150, 190, 226), (240, 226, 192)
im = Image.new('RGB', (W, H)); px = im.load()
def lerp(a, b, t): return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(3))
for y in range(H):
    v = 1 - y / (H - 1)                       # 이미지 위 = 천정
    c = lerp(HOR, MID, min(1, v / 0.18)) if v < 0.18 else lerp(MID, TOP, min(1, (v - 0.18) / 0.6))
    for x in range(W): px[x, y] = c
random.seed(7)
for _ in range(9):                           # 구름: 납작한 타원 몇 개 겹침, 지평선 위 낮은 고도(v 0.06~0.3)
    cx, cv = random.uniform(0, W), random.uniform(0.07, 0.28); w = random.uniform(14, 34); h = random.uniform(2.4, 5.0)
    for k in range(4):
        ox, oy, s = random.uniform(-w * .5, w * .5), random.uniform(-h * .4, h * .4), random.uniform(.5, 1)
        for y in range(H):
            for x in range(W):
                dx = ((x - cx - ox + W / 2) % W) - W / 2; dy = (1 - y / (H - 1) - cv) * H - oy
                if (dx / (w * s)) ** 2 + (dy / (h * s)) ** 2 < 1:
                    base = px[x, y]; t = 0.55 if dy > -h * .3 else 0.42     # 아래쪽은 살짝 회청 그늘
                    px[x, y] = lerp(base, (255, 250, 240) if t > .5 else (226, 232, 240), t)
im.save('F1/Assets/Art/SceneA2/Gen/sky_a2d.png'); print('sky', im.size)
