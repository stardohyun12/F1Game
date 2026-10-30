"""terrace5: 축대 돌 텍스처 B 방식(retainAB/make_ab.py 의 B) + PD 8b98 진단 3.
사용: python -X utf8 docs/qa/tmp/terrace5/make_wall_tex.py
- 한 장 = 20 m(벽 길이) × 4 m(벽 밑동부터 높이) = 640×128 px → 32 px/m (B 와 같은 밀도). 세로는 반복 안 함(벽 최고 3.04 + 묻힌 0.3 < 4 m).
- 결: B 와 같은 사진 결 명도 ±10%, house_home palette.gpl 4단. 줄눈(가장 어두운 단)만 팔레트에서 한 단 더 진하게.
- 반복 깨기: 벽 길이 방향 큰 명도 얼룩(주기 20/10/6.7 m 사인 합 ±7%)을 양자화 전에 곱함 → 같은 4색 안에서 어두운 돌이 몰린 구간/밝은 구간.
- 밑동 흙빛: 밑동(v=0, 땅 = 0.3 m)에서 0.3~0.85 m 사이를 흙색 2단으로 번지게(울퉁 경계, 1px 외톨이 없음).
"""
import pathlib, re
import numpy as np
from PIL import Image, ImageFilter

R = pathlib.Path(__file__).resolve().parents[4]
OUT = R / "F1/Assets/Scenes/Map_TownV6_Mats/TV6_RetainStoneB.png"
PX = 32
TW, TH = 80, 60            # B 한 장 2.5 × 1.875 m
W, H = 640, 128            # 20 m × 4 m

pal = np.array([[int(x) for x in m.groups()] for m in
                (re.match(r"\s*(\d+)\s+(\d+)\s+(\d+)\s+c\d+", l) for l in
                 (R / "art/export/house-home/palette.gpl").read_text().splitlines()) if m], np.float64)

def nearest1(c):
    return pal[((pal - np.asarray(c, np.float64)) ** 2).sum(1).argmin()]

def tileable(t):
    hh, ww = t.shape[:2]
    r = np.roll(t, (hh // 2, ww // 2), (0, 1))
    yy, xx = np.mgrid[0:hh, 0:ww]
    def ss(e0, e1, x):
        k = np.clip((x - e0) / (e1 - e0), 0, 1); return k * k * (3 - 2 * k)
    m = ss(0, .14, np.minimum(xx / ww, 1 - xx / ww)) * ss(0, .14, np.minimum(yy / hh, 1 - yy / hh))
    return m[..., None] * t + (1 - m[..., None]) * r

# --- B 결(make_ab.py B 와 같은 단계) ---
src = Image.open(R / "art/source/photo-picks/H/H1_round_stone_wall.jpg").convert("RGB").crop((0, 0, 1600, 1130))
src = src.resize((TW * 4, TH * 4), Image.LANCZOS)
t = np.asarray(src, np.float64)
big = np.asarray(Image.fromarray(t.mean(2).astype(np.uint8)).filter(ImageFilter.GaussianBlur(40)), np.float64)
t *= ((big.mean() / np.maximum(big, 1)) ** 0.8)[..., None]
t = np.asarray(Image.fromarray(np.clip(t, 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.2)).resize((TW, TH), Image.BOX), np.float64)
t = tileable(t)
lum = t.mean(2); ratio = lum / lum.mean()
base = t.reshape(-1, 3).mean(0); base = base * .70 + base.mean() * .30
dev = np.clip(ratio - 1, -1, 1); s = .10 / max(np.percentile(np.abs(dev), 95), 1e-6)
mod = np.clip(1 + dev * s, .88, 1.12)
th = np.percentile(mod, [14, 46, 82])                       # B 와 같은 단 경계(고정값으로 저장해 큰 얼룩이 단 비율을 바꾸게)

# --- 20 m × 4 m 로 깔기 + 길이 방향 큰 얼룩 ---
reps_x, reps_y = W // TW, -(-H // TH)
full = np.tile(mod, (reps_y, reps_x))[:H, :W]
x = np.arange(W) / W * 2 * np.pi
rng = np.random.default_rng(5)
blot = sum(a * np.sin(k * x + rng.uniform(0, 6.28)) for k, a in ((1, .035), (2, .025), (3, .02)))
yy = np.arange(H)[:, None] / H * 2 * np.pi
blot2 = .015 * np.sin(2 * x[None, :] + 1.3 * np.sin(yy))     # 세로로 살짝 기울어진 얼룩
full = full * (1 + blot[None, :] + blot2)
lv = np.digitize(full, th)                                  # 0 줄눈 · 1 어두운 돌 · 2 중간 · 3 밝은 돌

levels = [.88, .95, 1.04, 1.12]
palB = [nearest1(base * l) for l in levels]
# 줄눈만 한 단 진하게: 팔레트에서 줄눈색보다 어둡고(명도 6~16% 낮음) 색상이 가까운 것
m0 = palB[0]; cand = pal[(pal.mean(1) < m0.mean() * .94) & (pal.mean(1) > m0.mean() * .84)]
mortar = cand[((cand / np.maximum(cand.mean(1, keepdims=True), 1) - m0 / m0.mean()) ** 2).sum(1).argmin()] if len(cand) else m0 * .9
palB[0] = mortar
# 밑동 흙빛 2단(흙길 색 쪽): 땅(0.3 m)~0.85 m
earth = [nearest1([124, 103, 78]), nearest1([102, 85, 64])]   # 흙빛이 도는 돌색(채도 낮게 — 주황 띠가 되지 않게)

img = np.zeros((H, W, 3))
for i in range(4): img[lv == i] = palB[i]
# PIL 행 0 = 위 = v 최대. 밑동 = 아래 행.
hgt = (H - 1 - np.arange(H))[:, None] / PX                  # 행 → 밑동부터 m
edge = .62 + .16 * np.sin(x * 3 + 1.1)[None, :] + .08 * np.sin(x * 11)[None, :] + .05 * (full - 1) / .12
dirt = hgt < edge - .14                                    # 아래: 다 흙빛
fringe = (hgt < edge) & ~dirt & (lv <= 1)                   # 위 가장자리: 줄눈·어두운 돌만 흙빛(번지는 경계)
img[dirt & (lv <= 1)] = earth[1]
img[dirt & (lv >= 2)] = earth[0]
img[fringe] = earth[1]
# 1px 외톨이 정리(같은 색 4이웃 없음 → 이웃 최빈색)
idx = np.unique(img.reshape(-1, 3), axis=0, return_inverse=True)[1].reshape(H, W)
cols = np.unique(img.reshape(-1, 3), axis=0)
for _ in range(2):
    p = np.pad(idx, 1, mode="wrap")
    nb = np.stack([p[:-2, 1:-1], p[2:, 1:-1], p[1:-1, :-2], p[1:-1, 2:]])
    lone = ~(nb == idx).any(0)
    for y, xx in zip(*np.nonzero(lone)):
        v, c = np.unique(nb[:, y, xx], return_counts=True); idx[y, xx] = v[c.argmax()]
out = cols[idx].astype(np.uint8)
Image.fromarray(out).save(OUT)
cap = nearest1(np.asarray(palB[3]) * 1.10)                  # 갓돌 띠 = 가장 밝은 돌보다 한 단 밝게(흰색 아님)
print("palette", [list(map(int, c)) for c in palB], "earth", [list(map(int, c)) for c in earth], "cap", list(map(int, cap)), "colors", len(cols))
(R / "docs/qa/tmp/terrace5/cap.txt").write_text(" ".join(f"{c/255:.4f}" for c in cap))
# 미리보기 ×3
Image.fromarray(out).resize((W * 3, H * 3), Image.NEAREST).save(R / "docs/qa/tmp/terrace5/wall_tex_preview.png")
