"""집 h판 시트용 — 정비소 V5(b) + 집 6채를 한 장면·같은 카메라로 (과제 20261027-1543). Blender:
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_houses_h_scene.py
렌더: unlit(Emission, Closest), 정사영 카메라 하나. 줄 = 정비소 + 집 6채(h), 같은 자리에 e판 6채로 바꿔 한 장 더.
측정(같은 식으로 정비소·집 모두): 텍셀/m = √(Σ UV 넓이(px²) / Σ 면 넓이(m²)), 면 넓이 0.05m² 넘는 면만 ·
  색 수 = 텍스처 전체 서로 다른 색 · 벽 평균 밝기 = 세운 면(|n.z|<.2, 넓이 ≥1m²)의 UV 칸 안 텍셀 밝기 평균(면 넓이 가중).
출력: art/_review/2026-10-27-houses-h/{row_h,row_e}.png, scene_measure.json
"""
import bpy, os, json, math
import numpy as np
from mathutils import Vector
R = 'D:/Github/F1Game/'; RV = R + 'art/_review/2026-10-27-houses-h/'; os.makedirs(RV, exist_ok=True)
NAMES = ['house_narrow_a', 'house_narrow_b', 'house_shop', 'house_slope', 'house_warehouse', 'house_cafe']
bpy.ops.wm.read_factory_settings(use_empty=True); sc = bpy.context.scene
PXC = {}


def pix(path):
    if path not in PXC:
        im = bpy.data.images.load(path); w, h = im.size; a = np.empty(w * h * 4, np.float32); im.pixels.foreach_get(a); PXC[path] = (im, a.reshape(h, w, 4)[:, :, :3])
    return PXC[path]


def imp(fbx, png):
    before = set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=fbx); obs = [o for o in bpy.data.objects if o not in before and o.type == 'MESH']
    im, _ = pix(png); m = bpy.data.materials.new('u_' + os.path.basename(png)); m.use_nodes = True; nt = m.node_tree; nt.nodes.clear()
    tx = nt.nodes.new('ShaderNodeTexImage'); tx.image = im; tx.interpolation = 'Closest'; em = nt.nodes.new('ShaderNodeEmission'); mo = nt.nodes.new('ShaderNodeOutputMaterial')
    nt.links.new(tx.outputs[0], em.inputs[0]); nt.links.new(em.outputs[0], mo.inputs[0])
    for o in obs:
        uvn = 'BakedUV' if 'BakedUV' in o.data.uv_layers else o.data.uv_layers[0].name
        um = nt.nodes.get('uv') or nt.nodes.new('ShaderNodeUVMap'); um.name = 'uv'; um.uv_map = uvn; nt.links.new(um.outputs[0], tx.inputs[0])
        o.data.materials.clear(); o.data.materials.append(m)
        for f in o.data.polygons: f.material_index = 0
    return obs


def measure(obs, png):
    _, a = pix(png); H, W = a.shape[:2]; L = a @ np.array((.299, .587, .114)) * 255
    uvA = wA = 0.; wl = []; ww = []
    for o in obs:
        me = o.data; uv = me.uv_layers['BakedUV' if 'BakedUV' in me.uv_layers else 0]; mw = o.matrix_world
        for f in me.polygons:
            ps = [mw @ me.vertices[i].co for i in f.vertices]; us = np.array([uv.data[li].uv[:] for li in f.loop_indices]) * (W, H)
            area = sum(((ps[i] - ps[0]).cross(ps[i + 1] - ps[0])).length / 2 for i in range(1, len(ps) - 1))
            ua = abs(sum(us[i, 0] * us[(i + 1) % len(us), 1] - us[(i + 1) % len(us), 0] * us[i, 1] for i in range(len(us)))) / 2
            if area > .05: uvA += ua; wA += area
            n = (mw.to_3x3() @ f.normal).normalized()
            if abs(n.z) < .2 and area >= 1 and ua > 4:
                x0, y0 = np.floor(us.min(0)).astype(int); x1, y1 = np.ceil(us.max(0)).astype(int)
                blk = L[max(y0, 0):min(y1, H), max(x0, 0):min(x1, W)]
                if blk.size: wl.append(float(np.median(blk))); ww.append(area)
    key = (np.round(a * 255).astype(int) @ np.array((65536, 256, 1)))
    return dict(texels_per_m=round(math.sqrt(uvA / max(wA, 1e-9)), 2), colors_texture=int(len(np.unique(key))), wall_L_mean=round(float(np.average(wl, weights=ww)), 1) if wl else None, wall_faces=len(wl))


G = R + 'art/export/scene-a-v5b-garage/'
gar = imp(G + 'garage_v5_combo.fbx', G + 'garage_baked32.png')
meas = dict(garage_v5b=measure(gar, G + 'garage_baked32.png'))
pts = np.array([list(o.matrix_world @ Vector(c)) for o in gar for c in o.bound_box]); gx1 = pts[:, 0].max(); gy0 = pts[:, 1].min()
print('GARAGE bbox', pts.min(0).round(2), pts.max(0).round(2), flush=True)
rows = {}
for v in ('h', 'e'):
    D = R + f'art/export/scene-a4-houses-{v}/'; cur = gx1 + 2.5; rows[v] = []
    for n in NAMES:
        obs = imp(D + n + '.fbx', D + n + '.png'); meas[f'{n}_{v}'] = measure(obs, D + n + '.png')
        p = np.array([list(o.matrix_world @ Vector(c)) for o in obs for c in o.bound_box]); dx = cur - p[:, 0].min(); dy = gy0 - p[:, 1].min()
        for o in obs: o.location.x += dx; o.location.y += dy
        cur += p[:, 0].max() - p[:, 0].min() + 2.0; rows[v] += obs
    print('ROW', v, round(cur, 1), flush=True)
xm = cur / 2 + (pts[:, 0].min()) / 2
cd = bpy.data.cameras.new('cam'); cd.type = 'ORTHO'; cd.ortho_scale = cur - pts[:, 0].min() + 2; cam = bpy.data.objects.new('cam', cd); sc.collection.objects.link(cam); sc.camera = cam
d = Vector((math.sin(math.radians(18)), -math.cos(math.radians(18)), .42)).normalized(); tgt = Vector((xm, gy0 + 3, 3.2)); cam.location = tgt + d * 80
cam.rotation_euler = (tgt - cam.location).to_track_quat('-Z', 'Y').to_euler()
wd = bpy.data.worlds.new('w'); sc.world = wd; wd.use_nodes = True; wd.node_tree.nodes['Background'].inputs[0].default_value = (.62, .72, .78, 1)
sc.render.engine = 'CYCLES'; sc.cycles.samples = 4; sc.cycles.use_denoising = False; sc.cycles.filter_width = .5; sc.view_settings.view_transform = 'Standard'
sc.render.resolution_x, sc.render.resolution_y = 2560, 640; sc.render.resolution_percentage = 100
for v, o_v in (('h', 'e'), ('e', 'h')):
    for o in rows[v]: o.hide_render = False
    for o in rows[o_v]: o.hide_render = True
    sc.render.filepath = RV + f'row_{v}.png'; bpy.ops.render.render(write_still=True); print('RENDER', v, flush=True)
json.dump(meas, open(RV + 'scene_measure.json', 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
for k, m in meas.items(): print('M', k, m, flush=True)
