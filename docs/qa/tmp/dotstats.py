"""360 프레임 도트 문법 잣대 — 오프라인(PNG). python -I dotstats.py a.png b.png ...
colors      : 고유 RGB 수
colors95    : 화면 95% 를 덮는 색 수
small_step  : 가로 이웃 밝기 차 1~6 (잔결·사진 점무늬·매끈한 그라디언트) 비율
big_step    : 가로 이웃 밝기 차 ≥7 (색면 경계·디더 교차) 비율
dither      : 좌우 또는 위아래 이웃끼리 같고 가운데만 다른 픽셀(체커) 비율
flat        : 3×3 이 전부 같은 색인 픽셀 비율(색면)
lum_levels  : 밝기 막대 32칸 중 1% 넘는 칸 수(빛·색 단 수의 대리값)
"""
import sys, json
import numpy as np
from PIL import Image

def stats(path):
    a = np.asarray(Image.open(path).convert("RGB")).astype(np.int32)
    H, W, _ = a.shape
    if H != 360:  # 기준 그림(업스케일 캡처)은 Nearest 로 360 에 맞춤
        im = Image.open(path).convert("RGB")
        im = im.resize((round(W * 360 / H), 360), Image.NEAREST)
        a = np.asarray(im).astype(np.int32); H, W, _ = a.shape
    key = (a[..., 0] << 16) | (a[..., 1] << 8) | a[..., 2]
    _, cnt = np.unique(key, return_counts=True)
    cs = np.sort(cnt)[::-1]; c95 = int(np.searchsorted(np.cumsum(cs), 0.95 * key.size) + 1)
    L = (a[..., 0] * 299 + a[..., 1] * 587 + a[..., 2] * 114) // 1000
    dx = np.abs(np.diff(L, axis=1))
    small = float(((dx >= 1) & (dx <= 6)).mean()); big = float((dx >= 7).mean())
    c = key[1:-1, 1:-1]; l = key[1:-1, :-2]; r = key[1:-1, 2:]; u = key[:-2, 1:-1]; d = key[2:, 1:-1]
    dith = float((((l == r) & (c != l)) | ((u == d) & (c != u))).mean())
    flat = np.ones_like(c, dtype=bool)
    for oy in (-1, 0, 1):
        for ox in (-1, 0, 1):
            flat &= key[1 + oy:H - 1 + oy, 1 + ox:W - 1 + ox] == c
    h, _ = np.histogram(L, bins=32, range=(0, 256))
    lv = int((h > 0.01 * L.size).sum())
    return dict(name=path.replace("\\", "/").split("/")[-1], w=W, h=H, colors=len(cnt), colors95=c95,
                small_step=round(small, 3), big_step=round(big, 3), dither=round(dith, 3), flat=round(float(flat.mean()), 3), lum_levels=lv)

for p in sys.argv[1:]:
    print(json.dumps(stats(p), ensure_ascii=False))
