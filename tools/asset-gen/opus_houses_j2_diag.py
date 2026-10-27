"""j판 진단 — 면마다 UV 칸 안 텍셀이 아틀라스 바탕색(23,37,40)인 비율. 검게 보이는 면이 어디·무슨 역할인지.
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_houses_j2_diag.py -- <export 폴더 이름> <work 폴더 이름>
"""
import bpy, sys, json
import numpy as np
R = 'D:/Github/F1Game/'; a = sys.argv[sys.argv.index('--') + 1:]
EX, WK = R + f'art/export/{a[0]}/', R + f'art/models/{a[1]}/'
ROLES = ['hidden', 'wall', 'roof', 'glass', 'frame', 'door', 'shutter', 'rail', 'slab', 'awning', 'sign', 'stone', 'ac', 'prop', 'flat', 'soffit', 'board', 'tin']
out = {}
for n in ['house_narrow_a', 'house_narrow_b', 'house_shop', 'house_slope', 'house_warehouse', 'house_cafe']:
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=EX + n + '.fbx')
    im = bpy.data.images.load(EX + n + '.png'); W, H = im.size; px = np.empty(W * H * 4, np.float32); im.pixels.foreach_get(px)
    px = np.round(px.reshape(H, W, 4)[..., :3] * 255).astype(int); bg = (np.abs(px - (23, 37, 40)).sum(-1) <= 2)
    L = px @ np.array((.299, .587, .114))
    try: cls = np.load(WK + n + '_cls.npy')[::-1]
    except Exception: cls = None
    rows = []; tot_a = dark_a = 0
    for o in [o for o in bpy.data.objects if o.type == 'MESH']:
        me = o.data; uv = me.uv_layers['BakedUV' if 'BakedUV' in me.uv_layers else 0]; mw = o.matrix_world
        for f in me.polygons:
            us = np.array([uv.data[li].uv[:] for li in f.loop_indices]) * (W, H) - .5
            c = us.mean(0); pts = np.vstack([us * .7 + c * .3, c[None]])
            xi = np.clip(np.round(pts[:, 0]).astype(int), 0, W - 1); yi = np.clip(np.round(pts[:, 1]).astype(int), 0, H - 1)
            fb = bg[yi, xi].mean(); fl = float(np.median(L[yi, xi])); area = f.area * (mw.to_3x3().determinant() ** (2 / 3))
            tot_a += area
            if fb > .5 or fl < 60:
                dark_a += area; n_ = (mw.to_3x3() @ f.normal).normalized(); ce = mw @ f.center
                rl = ROLES[int(np.bincount(cls[yi, xi]).argmax())] if cls is not None else '?'
                rows.append(dict(i=f.index, area=round(area, 3), n=[round(v, 2) for v in n_], c=[round(v, 2) for v in ce], bg=round(float(fb), 2), L=round(fl), role=rl, uvspan=[round(float(v), 1) for v in (us.max(0) - us.min(0))]))
    rows.sort(key=lambda r: -r['area'])
    out[n] = dict(dark_area_pct=round(dark_a / tot_a * 100, 2), n_faces=len(rows), top=rows[:25])
    by = {}
    for r in rows: by[r['role']] = round(by.get(r['role'], 0) + r['area'], 2)
    print('DIAG', n, out[n]['dark_area_pct'], len(rows), by, flush=True)
    for r in rows[:12]: print('  ', r, flush=True)
json.dump(out, open(R + f'art/models/{a[1]}_diag.json', 'w'), indent=1)
