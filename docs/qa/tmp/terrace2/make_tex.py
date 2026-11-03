"""terrace2: 축대 돌 텍스처(H1 사진, 이음 없는 타일) + 흙길 가장자리 번짐 텍스처. 씬 전용(Map_TownV6_Mats/TV6_*).
사용: python -X utf8 docs/qa/tmp/terrace2/make_tex.py
- TV6_RetainStone.png 128×96 = 2.5 m × 1.88 m (house_home 기단과 같은 축척: H1 한 장 = 2.5 m → 돌 한 개 ≈ 0.38 m)
  큰 명암(사진의 햇빛 기울기)만 흐린 판으로 나눠 평평하게. 돌·줄눈 대비는 사진 그대로.
- TV6_TerracePathEdge.png 32×128: u = 길 가로(0..1), v = 길 세로(4 m 반복). 가운데 흙 → 가장자리 흰색(= 재질 틴트 = 풀색).
  흙/풀 경계는 길이 방향 저주파 흔들림 + 알갱이 점으로 들쭉날쭉.
"""
import pathlib
import numpy as np
from PIL import Image, ImageFilter

R = pathlib.Path(__file__).resolve().parents[4]
OUT = R / "F1/Assets/Scenes/Map_TownV6_Mats"

# ---------- 돌 ----------
src = Image.open(R / "art/source/photo-picks/H/H1_round_stone_wall.jpg").convert("RGB")
src = src.crop((0, 0, 1600, 1130))  # 맨 아래 자갈 바닥 띠 뺌
W, H = 128, 96
t = np.asarray(src.resize((W, H), Image.LANCZOS), np.float64)
lum = t.mean(2)
big = np.asarray(Image.fromarray(lum.astype(np.uint8)).filter(ImageFilter.GaussianBlur(18)), np.float64)
t *= ((big.mean() / np.maximum(big, 1)) ** 0.7)[..., None]
r = np.roll(t, (H // 2, W // 2), (0, 1))
yy, xx = np.mgrid[0:H, 0:W]
def ss(e0, e1, x):
    k = np.clip((x - e0) / (e1 - e0), 0, 1); return k * k * (3 - 2 * k)
m = ss(0, .14, np.minimum(xx / W, 1 - xx / W)) * ss(0, .14, np.minimum(yy / H, 1 - yy / H))
out = m[..., None] * t + (1 - m[..., None]) * r
out = out * .7 + out.mean(2, keepdims=True) * .3  # 채도 30% 뺌(맵 로파이 색과 맞춤)
Image.fromarray(np.clip(out, 0, 255).astype(np.uint8)).save(OUT / "TV6_RetainStone.png")

# ---------- 흙길 가장자리 ----------
PW, PH = 32, 128
rng = np.random.default_rng(209)
terr = np.array([.62, .66, .48]); dirt = np.array([.58, .47, .33])
center = dirt / terr  # 재질 틴트(= 풀 틴트) 곱하면 흙색
v = np.arange(PH) / PH
wob = .07 * np.sin(2 * np.pi * (2 * v + .13)) + .05 * np.sin(2 * np.pi * (5 * v + .61))  # 반복 4 m 안에서 2·5 마디
img = np.zeros((PH, PW, 3))
for j in range(PH):
    for i in range(PW):
        u = (i + .5) / PW
        d = abs(u - .5) * 2  # 0 가운데 → 1 가장자리
        side = wob[j] if u < .5 else wob[(j + 37) % PH]
        edge = .50 + side  # 흙이 끝나는 곳(가로 절반 기준)
        k = ss(edge - .16, edge + .30, d + rng.normal(0, .07))  # 0 흙 → 1 풀
        grain = 1 + rng.normal(0, .05) * (1 - k)
        img[j, i] = (center * (1 - k) + np.ones(3) * k) * grain
img[:, 0] = img[:, -1] = 1.0  # 맨 바깥 = 풀과 같은 색
Image.fromarray(np.clip(img * 255, 0, 255).astype(np.uint8)).save(OUT / "TV6_TerracePathEdge.png")
print("ok", (OUT / "TV6_RetainStone.png").stat().st_size, (OUT / "TV6_TerracePathEdge.png").stat().st_size)
