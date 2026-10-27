"""j5 진단 — narrow_b 모서리 기둥 얼룩 면 찾기. Blender -b -P. 출력 stdout."""
import bpy, bmesh, numpy as np
R = 'D:/Github/F1Game/'; SRC = R + 'art/export/scene-a4-houses-j4/'; n = 'house_narrow_b'
ROLES = ['hidden', 'wall', 'roof', 'glass', 'frame', 'door', 'shutter', 'rail', 'slab', 'awning', 'sign', 'stone', 'ac', 'prop', 'flat', 'soffit', 'board', 'plank', 'tin']
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=SRC + n + '.fbx')
o = [o for o in bpy.data.objects if o.type == 'MESH'][0]; mw = o.matrix_world
im = bpy.data.images.load(SRC + n + '.png'); W, H = im.size; a = np.empty(W * H * 4, np.float32); im.pixels.foreach_get(a); px = a.reshape(H, W, 4)[..., :3] * 255
cls = np.load(R + 'art/models/opus_houses_j/' + n + '_cls.npy')[::-1]
bm = bmesh.new(); bm.from_mesh(o.data); bm.faces.ensure_lookup_table(); uvl = bm.loops.layers.uv[0]
rows = []
for f in bm.faces:
    u = np.array([l[uvl].uv[:] for l in f.loops]) * (W, H) - .5; e = u.max(0) - u.min(0)
    if e.max() < 8: continue
    x0, y0 = np.floor(u.min(0)).astype(int); x1, y1 = np.ceil(u.max(0)).astype(int)
    blk = px[max(y0, 0):y1 + 1, max(x0, 0):x1 + 1].reshape(-1, 3); sd = float(blk.std(0).mean()); nc = len(np.unique(blk.round().astype(int) @ np.array((65536, 256, 1))))
    c = u.mean(0); r = ROLES[int(cls[int(np.clip(round(c[1]), 0, H - 1)), int(np.clip(round(c[0]), 0, W - 1))])]
    if e.min() < 20 and e.max() >= 3 * e.min() and sd > 6:
        rows.append((sd, f.index, r, e.round(1).tolist(), round(f.calc_area(), 2), [round(v, 2) for v in mw @ f.calc_center_median()], nc))
for r in sorted(rows, reverse=True)[:40]: print('D', r)
print('N', len(rows))
