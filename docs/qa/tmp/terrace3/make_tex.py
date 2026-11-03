"""terrace3: 보도·계단 포장 텍스처. 씬 전용(Map_TownV6_Mats/TV6_Paving.png).
사용: python -X utf8 docs/qa/tmp/terrace3/make_tex.py
- 원본 H3 (Poly Haven old_stone_wall, CC0, 이미 이음 없는 타일 텍스처) → 128×128 = 1.6 m × 1.6 m (재질 타일).
  벽 사진이지만 정면 평면 타일이라 바닥에 깔면 막돌 포장(돌 0.2~0.35 m)으로 읽힌다.
- 큰 명암만 평평하게, 밝게(+12%), 채도 35% 뺌 — 축대(TV6_RetainStone)보다 밝고 차분하게, 길(회색)과 갈리게.
"""
import pathlib
import numpy as np
from PIL import Image, ImageFilter

R = pathlib.Path(__file__).resolve().parents[4]
OUT = R / "F1/Assets/Scenes/Map_TownV6_Mats"
src = Image.open(R / "art/source/photo-picks/H/H3_old_stone_wall.jpg").convert("RGB")
W = 128
t = np.asarray(src.resize((W, W), Image.LANCZOS), np.float64)
lum = t.mean(2)
big = np.asarray(Image.fromarray(lum.astype(np.uint8)).filter(ImageFilter.GaussianBlur(20)), np.float64)
t *= ((big.mean() / np.maximum(big, 1)) ** 0.8)[..., None]
t = t * .65 + t.mean(2, keepdims=True) * .35
t = (t - t.mean()) * .8 + t.mean() * 1.12
Image.fromarray(np.clip(t, 0, 255).astype(np.uint8)).save(OUT / "TV6_Paving.png")
print("ok", t.mean())
