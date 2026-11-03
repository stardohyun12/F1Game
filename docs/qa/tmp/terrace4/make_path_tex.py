"""terrace4: 흙길 가장자리 텍스처만 다시 굽는다(축대 돌 TV6_RetainStone.png 는 손대지 않음 — A/B 비교 중).
사용: python -X utf8 docs/qa/tmp/terrace4/make_path_tex.py
3차 f4 의 '털 같은 지글거림' = 흙/풀 경계에 화소마다 넣은 잡음(rng.normal .07)과 알갱이 점. 길 폭이 1 px 안팎으로 보이는 거리에서 반짝인다.
→ 화소 잡음을 빼고 길이 방향 저주파 흔들림(2·5 마디)만 남김. 경계 띠는 조금 넓게(.16/.30 → .22/.30). 흙 안 결은 4 px 이상 덩어리로만.
"""
import pathlib
import numpy as np
from PIL import Image, ImageFilter

R = pathlib.Path(__file__).resolve().parents[4]
OUT = R / "F1/Assets/Scenes/Map_TownV6_Mats"


def ss(e0, e1, x):
    k = np.clip((x - e0) / (e1 - e0), 0, 1); return k * k * (3 - 2 * k)


PW, PH = 32, 128
terr = np.array([.62, .66, .48]); dirt = np.array([.58, .47, .33])
center = dirt / terr  # 재질 틴트(= 풀 틴트) 곱하면 흙색
v = np.arange(PH) / PH
wob = .07 * np.sin(2 * np.pi * (2 * v + .13)) + .05 * np.sin(2 * np.pi * (5 * v + .61))
rng = np.random.default_rng(210)
blot = np.asarray(Image.fromarray((rng.random((PH, PW)) * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(3)), np.float64) / 255
blot = (blot - blot.mean()) / max(blot.std(), 1e-6)  # 흐린 덩어리(반복 끝 이음은 v 방향 4 m 에 한 번)
img = np.zeros((PH, PW, 3))
for j in range(PH):
    for i in range(PW):
        u = (i + .5) / PW
        d = abs(u - .5) * 2
        side = wob[j] if u < .5 else wob[(j + 37) % PH]
        k = ss(.50 + side - .22, .50 + side + .30, d)
        img[j, i] = (center * (1 - k) + np.ones(3) * k) * (1 + .03 * blot[j, i] * (1 - k))
img[:, 0] = img[:, -1] = 1.0
Image.fromarray(np.clip(img * 255, 0, 255).astype(np.uint8)).save(OUT / "TV6_TerracePathEdge.png")
print("ok", (OUT / "TV6_TerracePathEdge.png").stat().st_size)
