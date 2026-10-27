# 과제 0236-3: 돌담 명도 대비 낮춤 — 각 픽셀을 평균색 쪽으로 40% 당긴 사본(stone_H1_soft.png). 원본 stone_H1.png 는 그대로
from PIL import Image
import os
d = os.path.join(os.path.dirname(__file__), "../../../F1/Assets/Art/SceneA7/Tex")
im = Image.open(os.path.join(d, "stone_H1.png")).convert("RGBA"); px = im.load(); w, h = im.size
n = w * h; mean = [sum(px[x, y][c] for x in range(w) for y in range(h)) / n for c in range(3)]
K = 0.6
for y in range(h):
    for x in range(w):
        r, g, b, a = px[x, y]
        px[x, y] = tuple(int(round(mean[c] + (v - mean[c]) * K)) for c, v in enumerate((r, g, b))) + (a,)
im.save(os.path.join(d, "stone_H1_soft.png")); print("mean", [round(m) for m in mean], "size", w, h)
