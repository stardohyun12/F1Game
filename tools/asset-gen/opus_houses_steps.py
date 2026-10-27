"""집 텍스처에서 명암 단 수 재기 (과제 20261027-1516). Blender 파이썬(numpy) 으로:
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_houses_steps.py -- f g
벽 텍셀(cls == wall)에서 1% 넘게 쓴 색 → 밝기 순으로 늘어놓고 밝기 차 ≤14 인 이웃은 한 단(= 얼룩 한 칸 옆 색)으로 묶은 수 = 명암 단.
보이는 색 = 숨은 면 뺀 텍셀의 서로 다른 색 수(간판·소품 포함).
"""
import bpy, sys, os, json, numpy as np
R = 'D:/Github/F1Game/'; V = sys.argv[sys.argv.index('--') + 1:]
NAMES = ['house_narrow_a', 'house_narrow_b', 'house_shop', 'house_slope', 'house_warehouse', 'house_cafe']
WALL, HID = 1, 0  # opus_houses.py ROLES: hidden 0, wall 1
out = {}
for v in V:
    for n in NAMES:
        im = bpy.data.images.load(R + f'art/export/scene-a4-houses-{v}/{n}.png'); w, h = im.size
        a = np.empty(w * h * 4, np.float32); im.pixels.foreach_get(a); a = np.round(a.reshape(h, w, 4)[..., :3] * 255).astype(int)
        cls = np.load(R + f'art/models/opus_houses_{v}/{n}_cls.npy')[::-1]  # 저장할 때 위아래 뒤집어 둠 → Blender 픽셀(아래→위)과 맞춤
        key = a[..., 0] * 65536 + a[..., 1] * 256 + a[..., 2]
        vis = np.unique(key[cls != HID]); wk, wc = np.unique(key[cls == WALL], return_counts=True); wk = wk[wc >= .01 * wc.sum()]
        Lw = sorted(.299 * (k >> 16) + .587 * ((k >> 8) & 255) + .114 * (k & 255) for k in wk)
        steps = 1 + sum(1 for x, y in zip(Lw, Lw[1:]) if y - x > 14) if Lw else 0
        out.setdefault(n, {})[v] = dict(visible_colors=int(len(vis)), wall_colors=int(len(wk)), wall_L=[round(x) for x in Lw], light_steps=steps)
        print('STEPS', v, n, out[n][v], flush=True)
json.dump(out, open(R + 'art/_review/2026-10-27-houses-g/steps.json', 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
