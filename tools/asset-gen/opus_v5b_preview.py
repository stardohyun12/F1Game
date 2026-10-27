"""정비소 실내 전후 렌더 + 집 6채 전후 측정 (아트2, 과제 1248). 아트1 unlit-final.blend 는 읽기만(저장 안 함).
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_v5b_preview.py
"""
import bpy, json, math, os
import numpy as np
from mathutils import Vector

R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')); J = lambda *p: os.path.join(R, *p)
O = J('art', 'models', 'garage_scene_a_v5_combo'); E = J('art', 'export', 'scene-a-v5-combo'); OUT = J('art', 'export', 'scene-a-v5b-garage')
REV = J('art', '_review', '2026-10-27-interior-fix'); os.makedirs(REV, exist_ok=True)

# ---------- 집 6채 전후 측정 (보이는 픽셀, 32색 뒤) ----------
ROLES = ['hidden', 'wall', 'roof', 'glass', 'frame', 'door', 'shutter', 'rail', 'slab', 'awning', 'sign', 'stone', 'ac', 'prop', 'flat', 'soffit']
res = {}
for nm in ('house_narrow_a', 'house_narrow_b', 'house_shop', 'house_slope', 'house_warehouse', 'house_cafe'):
    row = {}
    for tag, d, w in (('before', 'scene-a4-houses', 'opus_houses'), ('after', 'scene-a4-houses-b', 'opus_houses_b')):
        im = bpy.data.images.load(J('art', 'export', d, nm + '.png')); W, H = im.size
        a = np.array(im.pixels[:], np.float32).reshape(H, W, 4)[:, :, :3] * 255; cls = np.load(J('art', 'models', w, nm + '_cls.npy'))[::-1]
        L = a @ np.array((.299, .587, .114)); vis = cls != 0
        r = dict(p5_visible=round(float(np.percentile(L[vis], 5)), 1), under40_pct=round(float((L[vis] < 40).mean() * 100), 3),
                 wall=round(float(L[cls == 1].mean()), 1), wall_rgb=[round(float(v)) for v in a[cls == 1].mean(0)])
        for role in ('prop', 'glass', 'soffit'):
            m = cls == ROLES.index(role)
            if m.any(): r[role] = round(float(L[m].mean()), 1); r[role + '_p5'] = round(float(np.percentile(L[m], 5)), 1)
        # 벽 픽셀 중 회녹(G>R) 비율
        wp = a[cls == 1]; r['wall_greenish_pct'] = round(float((wp[:, 1] > wp[:, 0]).mean() * 100), 2)
        row[tag] = r
    res[nm] = row; print('H', nm, row, flush=True)
json.dump(res, open(os.path.join(REV, 'houses_before_after.json'), 'w', encoding='utf-8'), indent=1, ensure_ascii=False)

# ---------- 정비소 전후 렌더 ----------
bpy.ops.wm.open_mainfile(filepath=os.path.join(O, 'unlit-final.blend')); sc = bpy.context.scene
sc.render.resolution_x, sc.render.resolution_y = 960, 540
tex = {g: [n for m in bpy.data.materials if m.use_nodes and m.name.startswith(g) for n in m.node_tree.nodes if n.type == 'TEX_IMAGE'] for g in ('garage', 'dirt')}
cam = sc.camera
cj = json.load(open(J('art', '_review', '2026-10-27-scene-a-v5-combo', 'camera.json')))['game']
VIEWS = dict(game=(cj['location'], cj['target'], cj['vertical_fov']), inside=((.6, -4.5, 1.7), (.4, 7.0, 1.0), 58))
for tag, d in (('before', E), ('after', OUT)):
    for g, ts in tex.items():
        img = bpy.data.images.load(os.path.join(d, g + '_baked32.png'), check_existing=False)
        for t in ts: t.image = img
    for v, (loc, tgt, fov) in VIEWS.items():
        cam.location = loc; cam.data.sensor_fit = 'VERTICAL'; cam.data.angle_y = math.radians(fov)
        cam.rotation_euler = (Vector(tgt) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
        sc.render.filepath = os.path.join(REV, f'garage_{v}_{tag}.png'); bpy.ops.render.render(write_still=True)
print('PREVIEW OK', {g: len(t) for g, t in tex.items()}, flush=True)
