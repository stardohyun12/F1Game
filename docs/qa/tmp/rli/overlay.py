"""mask.json 을 그림 위에 칠해 본다(바닥 초록·벽 파랑·창 벽 빨강, 전등 웅덩이 안은 노랑). 사용: overlay.py <png> <out>"""
import json, sys
from PIL import Image
m = json.load(open("D:/Github/F1Game/docs/qa/tmp/rli/mask.json"))
im = Image.open(sys.argv[1]).convert("RGB"); W, H = im.size
ov = im.copy(); px = ov.load()
for u, v, face, ld, win in m["pts"]:
    col = (255, 220, 0) if ld <= m["lampR"] else ((255, 0, 0) if win else ((0, 200, 0) if face == 0 else (0, 80, 255)))
    x0, y0 = int(u * W), int((1 - v) * H)
    for dx in range(-2, 3):
        for dy in range(-2, 3):
            x, y = x0 + dx, y0 + dy
            if 0 <= x < W and 0 <= y < H: px[x, y] = col
Image.blend(im, ov, 0.45).resize((W // 2, H // 2), Image.NEAREST).save(sys.argv[2])
