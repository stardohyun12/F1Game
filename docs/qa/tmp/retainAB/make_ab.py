"""retainAB: 축대 돌 텍스처 두 안(A 도트 / B 건물 방식). 같은 사진(H1) 원본. 씬에 쓰지 않고 PNG 로만 낸다.
사용: python -X utf8 docs/qa/tmp/retainAB/make_ab.py
- 둘 다 2.5 m × 1.875 m 한 장(현재 재질 타일링 0.40×0.53 그대로) = 80×60 px → 32 px/m (check_rules.py 'texel' 기대값 = 세계 32).
- A: 큰 덩어리(사진을 55% 만 잘라 돌 ≈ 0.65 m) → 메디안 → 16~24색 팔레트 → 외톨이 1px 제거. 팔레트 = house_home .gpl 중 사진 돌색에 가장 많이 걸리는 20색(+ 흙길 색 2).
- B: house_home 표면 규칙 — 큰 그늘만 남기고(사진 큰 기울기 평탄화) 결 명도 ±10% → 그 집 palette.gpl 로 4단 재양자화.
"""
import pathlib, re
import numpy as np
from PIL import Image, ImageFilter

R = pathlib.Path(__file__).resolve().parents[4]
OUT = R / "docs/qa/tmp/retainAB"
W, H = 80, 60

pal = np.array([[int(x) for x in m.groups()] for m in
                (re.match(r"\s*(\d+)\s+(\d+)\s+(\d+)\s+c\d+", l) for l in
                 (R / "art/export/house-home/palette.gpl").read_text().splitlines()) if m], np.float64)

def nearest(img, palette):
    flat = img.reshape(-1, 3)
    d = ((flat[:, None, :] - palette[None, :, :]) ** 2).sum(-1)
    return d.argmin(1).reshape(img.shape[:2])

def clean_specks(idx, rounds=2):
    """상하좌우 4칸 중 같은 색이 하나도 없는 1px 를 이웃 최빈색으로."""
    idx = idx.copy()
    for _ in range(rounds):
        p = np.pad(idx, 1, mode="wrap")
        nb = np.stack([p[:-2, 1:-1], p[2:, 1:-1], p[1:-1, :-2], p[1:-1, 2:]])
        lone = ~(nb == idx).any(0)
        for y, x in zip(*np.nonzero(lone)):
            vals, cnt = np.unique(nb[:, y, x], return_counts=True)
            idx[y, x] = vals[cnt.argmax()]
    return idx

def tileable(t):
    hh, ww = t.shape[:2]
    r = np.roll(t, (hh // 2, ww // 2), (0, 1))
    yy, xx = np.mgrid[0:hh, 0:ww]
    def ss(e0, e1, x):
        k = np.clip((x - e0) / (e1 - e0), 0, 1); return k * k * (3 - 2 * k)
    m = ss(0, .14, np.minimum(xx / ww, 1 - xx / ww)) * ss(0, .14, np.minimum(yy / hh, 1 - yy / hh))
    return m[..., None] * t + (1 - m[..., None]) * r

def load(frac):
    src = Image.open(R / "art/source/photo-picks/H/H1_round_stone_wall.jpg").convert("RGB").crop((0, 0, 1600, 1130))
    cw, ch = int(1600 * frac), int(1130 * frac)
    src = src.crop((0, 0, cw, ch)).resize((W * 4, H * 4), Image.LANCZOS)  # 4배 해상도에서 평탄화
    t = np.asarray(src, np.float64)
    lum = t.mean(2)
    big = np.asarray(Image.fromarray(lum.astype(np.uint8)).filter(ImageFilter.GaussianBlur(40)), np.float64)
    t *= ((big.mean() / np.maximum(big, 1)) ** 0.8)[..., None]  # 큰 햇빛 기울기 평평하게(= 빛 빼기)
    return t

# ---------------- A ----------------
WA, HA = 160, 120                                          # A 는 5 m × 3.75 m 한 장(같은 32 px/m) — 반복 줄이려고 B 의 2배
W0, H0 = W, H; W, H = WA, HA
tA = load(1.0)
im = Image.fromarray(np.clip(tA, 0, 255).astype(np.uint8)).filter(ImageFilter.MedianFilter(9)).resize((W, H), Image.BOX)
tA = np.asarray(im, np.float64)
tA = tileable(tA)
tA = tA * .55 + tA.mean(2, keepdims=True) * .45            # 채도 45% 뺌(파랑 돌이 튀지 않게)
tA = tA + np.array([8, 2, -10])                             # 살짝 따뜻하게(집·흙 쪽)
# 팔레트 20색: 사진 돌색이 house_home .gpl 의 어느 색에 가장 많이 걸리는가
palL = pal[pal.mean(1) > 70]      # 거의 검정 제외(줄눈은 따로 한 색)
idx0 = nearest(tA, palL)
vals, cnt = np.unique(idx0, return_counts=True)
top = vals[np.argsort(-cnt)[:17]]
palA = np.vstack([palL[top], [[148, 120, 84], [176, 160, 120], [78, 64, 52]]])      # + 흙길 색 2(맵 지형 쪽) + 줄눈 1
idxA = clean_specks(nearest(tA, palA))
outA = palA[idxA].astype(np.uint8)
Image.fromarray(outA).save(OUT / "A_stone.png")
print("A colors", len(np.unique(idxA)), "palette", len(palA))

# ---------------- B ----------------
W, H = W0, H0
tB = load(1.0)
tB = np.asarray(Image.fromarray(np.clip(tB, 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.2)).resize((W, H), Image.BOX), np.float64)
tB = tileable(tB)
lum = tB.mean(2)
ratio = lum / lum.mean()
base = tB.reshape(-1, 3).mean(0)
base = base * .70 + base.mean() * .30                         # 채도 30% 뺌(현재 재질과 같게)
dev = np.clip(ratio - 1, -1, 1)
s = .10 / max(np.percentile(np.abs(dev), 95), 1e-6)            # 결 명도 ±10% 로 맞춤
mod = 1 + dev * s
mod = np.clip(mod, .88, 1.12)
col = base[None, None, :] * mod[..., None]
# 4단: 줄눈(어두움) / 어두운 돌 / 중간 / 밝은 돌 — 레벨 색은 house_home .gpl 에서 가장 가까운 색
levels = [.88, .95, 1.04, 1.12]
lv = np.digitize(mod, np.percentile(mod, [14, 46, 82]))
palB = np.array([pal[nearest((base * l)[None, None, :], pal)[0, 0]] for l in levels])
idxB = clean_specks(lv)
outB = palB[idxB].astype(np.uint8)
Image.fromarray(outB).save(OUT / "B_stone.png")
print("B base", base.round(0), "palette", palB.tolist(), "level share", np.bincount(lv.ravel(), minlength=4) / lv.size)

# 캡 색(갓돌): 현재 (.86,.83,.76)에 가장 가까운 그 팔레트 색
cur = np.array([.86, .83, .76]) * 255
capB = pal[nearest(cur[None, None, :], pal)[0, 0]]
capA = palA[nearest(cur[None, None, :], palA)[0, 0]]
print("capA", capA.tolist(), "capB", capB.tolist())
(OUT / "caps.txt").write_text(f"{capA[0]/255:.4f} {capA[1]/255:.4f} {capA[2]/255:.4f}\n{capB[0]/255:.4f} {capB[1]/255:.4f} {capB[2]/255:.4f}\n")

# 미리보기: 3×3 타일 ×6 확대(최근접)
cur_png = Image.open(R / "F1/Assets/Scenes/Map_TownV6_Mats/TV6_RetainStone.png").convert("RGB")
def sheet(imgs):
    tiles = []
    for im in imgs:
        w, h = im.size
        a = Image.new("RGB", (240, 180))
        for i in range(0, 240, w):
            for j in range(0, 180, h): a.paste(im, (i, j))
        tiles.append(a.resize((a.width * 2, a.height * 2), Image.NEAREST))
    out = Image.new("RGB", (sum(t.width for t in tiles) + 10 * (len(tiles) - 1), tiles[0].height), (30, 30, 30))
    x = 0
    for t in tiles: out.paste(t, (x, 0)); x += t.width + 10
    return out
sheet([cur_png.resize((80, 60), Image.LANCZOS), Image.open(OUT / "A_stone.png"), Image.open(OUT / "B_stone.png")]).save(OUT / "tex_preview.png")
