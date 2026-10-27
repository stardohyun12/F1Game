"""j3 진단 — board·sign 역할 면을 법선 방향별로 묶어 개수·넓이·월드 범위. Blender -b -P"""
import bpy, bmesh, numpy as np
R = 'D:/Github/F1Game/'; SRC = R + 'art/export/scene-a4-houses-j2/'; WK = R + 'art/models/opus_houses_j/'
ROLES = ['hidden', 'wall', 'roof', 'glass', 'frame', 'door', 'shutter', 'rail', 'slab', 'awning', 'sign', 'stone', 'ac', 'prop', 'flat', 'soffit', 'board', 'plank', 'tin']
for n in ['house_narrow_a', 'house_shop', 'house_warehouse']:
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=SRC + n + '.fbx')
    o = [o for o in bpy.data.objects if o.type == 'MESH'][0]; mw = o.matrix_world; R3 = mw.to_3x3()
    im = bpy.data.images.load(SRC + n + '.png'); W, H = im.size; cls = np.load(WK + n + '_cls.npy')[::-1]
    bm = bmesh.new(); bm.from_mesh(o.data); uvl = bm.loops.layers.uv[0]; G = {}
    for f in bm.faces:
        u = np.array([l[uvl].uv[:] for l in f.loops]) * (W, H) - .5; c = u.mean(0)
        r = ROLES[cls[int(np.clip(round(c[1]), 0, H - 1)), int(np.clip(round(c[0]), 0, W - 1))]]
        if r not in ('board', 'sign'): continue
        nv = tuple(np.round((R3 @ f.normal).normalized(), 1)); P = [mw @ v.co for v in f.verts]
        g = G.setdefault((r, nv), [0, 0., [9e9] * 3, [-9e9] * 3, 0])
        g[0] += 1; g[1] += f.calc_area(); g[4] = max(g[4], float((u.max(0) - u.min(0)).max()))
        for p in P: g[2] = [min(a, b) for a, b in zip(g[2], p)]; g[3] = [max(a, b) for a, b in zip(g[3], p)]
    for k, g in G.items(): print('G', n, k, g[0], round(g[1], 2), np.round(g[2], 2).tolist(), np.round(g[3], 2).tolist(), 'maxuvspan', round(g[4], 1), flush=True)
