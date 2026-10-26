"""장면 A2 카트 — V5 방식(V1b 텍스처 × 구운 빛·AO → 공용 32색 → unlit) + 화면 크기 확인 (아트2, 2026-10-27 과제 0511).
입력: art/export/scene-a-v1b-photobash/kart_v1b.fbx
  = garage-kart-v5/kart_v5 와 같은 형태에 V1b 텍스처·UV 를 입힌 것. 같은 형태인지 tris·bbox 로 비교해 kart_check.json 에 남긴다.
굽기: 아트1 scene_a_v5_bake/quantize 와 같은 해·하늘·AO 식·공용 팔레트(scene-a-v5-combo/palette32.gpl)를 쓴다.
  - 카트만 따로 굽는다. 바닥판은 반사광·AO 가림용이고 내보내지 않는다.
  - 고유 BakedUV 는 1m = 32텍셀이다.
출력: art/export/scene-a2-kart/kart_a2.fbx + kart_a2_baked32.png
미리보기: art/_review/2026-10-27-scene-a2/kart_cam_*.png, kart_34.png, kart_size.json
    D:/Tools/Blender/blender.exe -b --factory-startup --python tools/asset-gen/opus_scene_a2_kart.py
"""
import bpy, csv, datetime, hashlib, json, math, os
import numpy as np
from mathutils import Vector, Matrix
from bpy_extras.object_utils import world_to_camera_view

R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
J = lambda *a: os.path.join(R, *a)
SRC = J('art', 'export', 'scene-a-v1b-photobash', 'kart_v1b.fbx')
REF = J('art', 'export', 'garage-kart-v5', 'kart_v5.fbx')
OUT, REV, WORK = J('art', 'export', 'scene-a2-kart'), J('art', '_review', '2026-10-27-scene-a2'), J('art', 'models', 'opus_scene_a2_kart')
PAL = J('art', 'export', 'scene-a-v5-combo', 'palette32.gpl')
DENS, GAP, AW = 32, 6, 1024
TRUCK = dict(w=round(330 / 1920, 3), h=round(360 / 1080, 3), cy=round(755 / 1080, 3))  # ⑥ EDC_2 트럭, 1920×1080 에서 px 로 잼
# 이름: (카트 뒤 거리 m, 높이 m, 세로 FOV°, 보는 점 = 카트 앞 m, 보는 점 높이 m). A = 지시값(프로그래머 0429 와 같은 보는 점)
CAMS = {'A': (5.6, 2.8, 64, 6, 1.0), 'B': (4.2, 2.2, 64, 6, .9), 'C': (3.4, 1.8, 60, 6, .8)}
for d in (OUT, REV, WORK): os.makedirs(d, exist_ok=True)


def load(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=path)
    obs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    bpy.ops.object.select_all(action='DESELECT')
    for o in obs: o.select_set(True)
    bpy.context.view_layer.objects.active = obs[0]
    bpy.ops.object.parent_clear(type='CLEAR_KEEP_TRANSFORM')
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for o in [o for o in bpy.context.scene.objects if o.type != 'MESH']: bpy.data.objects.remove(o, do_unlink=True)
    return obs


def stats(obs):
    P = [o.matrix_world @ v.co for o in obs for v in o.data.vertices]
    lo = [min(p[k] for p in P) for k in range(3)]; hi = [max(p[k] for p in P) for k in range(3)]
    return dict(tris=sum(len(f.vertices) - 2 for o in obs for f in o.data.polygons), lo=[round(v, 3) for v in lo], hi=[round(v, 3) for v in hi])


ref = stats(load(REF))
obs = load(SRC); src = stats(obs)
same = ref['tris'] == src['tris'] and all(abs(a - b) < .02 for a, b in zip(ref['lo'] + ref['hi'], src['lo'] + src['hi']))
json.dump(dict(kart_v5=ref, kart_v1b=src, same_shape=same), open(os.path.join(REV, 'kart_check.json'), 'w'), indent=1)
print('KART_CHECK', ref, src, same, flush=True)
sc = bpy.context.scene
z0 = src['lo'][2]

# 1) 재질 → 빛 받는 Diffuse(원래 텍스처, SourceUV)
for m in {s.material for o in obs for s in o.material_slots if s.material}:
    m.use_nodes = True; n = m.node_tree.nodes; L = m.node_tree.links
    img = next((x.image for x in n if x.type == 'TEX_IMAGE' and x.image), None); col = None
    if img is None:
        bs0 = next((x for x in n if x.type == 'BSDF_PRINCIPLED'), None); col = tuple(bs0.inputs['Base Color'].default_value) if bs0 else (.5, .5, .5, 1)
    n.clear(); out = n.new('ShaderNodeOutputMaterial'); bs = n.new('ShaderNodeBsdfDiffuse'); L.new(bs.outputs[0], out.inputs[0])
    if img:
        t = n.new('ShaderNodeTexImage'); t.image = img; t.interpolation = 'Closest'; uv = n.new('ShaderNodeUVMap'); uv.uv_map = 'SourceUV'
        L.new(uv.outputs[0], t.inputs[0]); L.new(t.outputs[0], bs.inputs[0])
    else: bs.inputs[0].default_value = col

# 2) 고유 BakedUV — 면마다 자기 평면에 펴서 선반식으로 채움(아트1 V5 와 같은 방식, 32텍셀/m)
rects = []
for o in obs:
    me = o.data
    if not me.uv_layers: me.uv_layers.new(name='SourceUV')
    me.uv_layers[0].name = 'SourceUV'; me.uv_layers.new(name='BakedUV')
    for f in me.polygons:
        ps = [me.vertices[i].co for i in f.vertices]; u = (ps[1] - ps[0]).normalized(); v = f.normal.cross(u).normalized()
        qs = [((p - ps[0]).dot(u), (p - ps[0]).dot(v)) for p in ps]
        lo = [min(q[k] for q in qs) for k in (0, 1)]; hi = [max(q[k] for q in qs) for k in (0, 1)]
        rects.append((o, f.index, qs, lo, hi) + tuple(max(2, math.ceil((hi[k] - lo[k]) * DENS)) for k in (0, 1)))
x = y = GAP; rh = 0; placed = []
for r in sorted(rects, key=lambda r: r[6], reverse=True):
    if x + r[5] + GAP > AW: x = GAP; y += rh + GAP; rh = 0
    placed.append(r + (x, y)); x += r[5] + GAP; rh = max(rh, r[6])
AH = 2 ** math.ceil(math.log2(y + rh + GAP))
for o, fi, qs, lo, hi, w, h, x, y in placed:
    uv = o.data.uv_layers['BakedUV']; f = o.data.polygons[fi]
    for li, q in zip(f.loop_indices, qs):
        uv.data[li].uv = ((x + (q[0] - lo[0]) / max(hi[0] - lo[0], 1e-8) * w) / AW, (y + (q[1] - lo[1]) / max(hi[1] - lo[1], 1e-8) * h) / AH)
for o in obs: o.data.uv_layers.active = o.data.uv_layers['BakedUV']

# 3) 빛 — V5 와 같은 낮은 가을 해·청록 하늘. 장면에서 카트가 −10° 돌아 있던 것을 되돌려 카트 기준 해 방향을 V5 와 같게
bpy.ops.mesh.primitive_plane_add(size=12, location=(0, 0, z0)); floor = bpy.context.object
fm = bpy.data.materials.new('bake_floor'); fm.use_nodes = True; fm.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (.42, .36, .27, 1)
floor.data.materials.append(fm)
bpy.ops.object.light_add(type='SUN'); sun = bpy.context.object; sun.data.energy = 2.4; sun.data.color = (1, .83, .60); sun.data.angle = .10
sun.rotation_euler = (Matrix.Rotation(math.radians(10), 3, 'Z') @ Vector((.7, .65, -.45))).to_track_quat('-Z', 'Y').to_euler()
sc.world = bpy.data.worlds.new('w'); sc.world.use_nodes = True; bg = sc.world.node_tree.nodes['Background']
bg.inputs[0].default_value = (.30, .52, .65, 1); bg.inputs[1].default_value = .65; sc.world.light_settings.distance = 1.0
sc.render.engine = 'CYCLES'
try:
    pr = bpy.context.preferences.addons['cycles'].preferences; pr.compute_device_type = 'OPTIX'; pr.get_devices()
    for d in pr.devices: d.use = d.type == 'OPTIX'
    sc.cycles.device = 'GPU'
except Exception as e: print('GPU off', e)
sc.cycles.samples = 128; sc.cycles.use_denoising = True; sc.cycles.max_bounces = 4
sc.view_settings.view_transform = 'Standard'; sc.view_settings.exposure = 0
bk = sc.render.bake; bk.margin = 3; bk.use_clear = True; bk.use_pass_direct = bk.use_pass_indirect = bk.use_pass_color = True

img = bpy.data.images.new('kart_bake', AW, AH, alpha=False); img.generated_color = (.15, .2, .2, 1)
for m in {s.material for o in obs for s in o.material_slots if s.material}:
    t = m.node_tree.nodes.new('ShaderNodeTexImage'); t.image = img; m.node_tree.nodes.active = t
bpy.ops.object.select_all(action='DESELECT')
for o in obs: o.select_set(True)
bpy.context.view_layer.objects.active = obs[0]


def grab(path):
    a = np.array(bpy.data.images.load(path).pixels[:], dtype=np.float32).reshape(AH, AW, 4)[:, :, :3]; return a


bpy.ops.object.bake(type='DIFFUSE'); p_raw = os.path.join(WORK, 'kart_baked_raw.png'); img.filepath_raw = p_raw; img.file_format = 'PNG'; img.save()
bpy.ops.object.bake(type='AO'); p_ao = os.path.join(WORK, 'kart_ao.png'); img.filepath_raw = p_ao; img.save()

# 4) 아트1 V5 와 같은 후처리: AO(.70+.30·AO)·청록 그늘·노출 +1스톱 → 공용 32색(가까운 색, 디더 없음)
arr = grab(p_raw) * 255; ao = grab(p_ao)[:, :, 0]
arr *= (.70 + .30 * ao)[:, :, None]; shade = (1 - ao) * .16; arr[:, :, 1] += shade * 24; arr[:, :, 2] += shade * 28
s = np.clip(arr / 255, 0, 1); lin = np.where(s <= .04045, s / 12.92, ((s + .055) / 1.055) ** 2.4)
lin = lin * 2.0 + ((1 - s.max(2)) ** 2)[:, :, None] * np.array([.009, .017, .019])
enc = np.clip(np.where(lin <= .0031308, lin * 12.92, 1.055 * np.maximum(lin, 0) ** (1 / 2.4) - .055), 0, 1)
pal = np.array([[int(v) for v in l.split()[:3]] for l in open(PAL).read().splitlines() if l[:1].isdigit()], dtype=np.float32) / 255
assert len(pal) == 32
flat = enc.reshape(-1, 3); idx = np.empty(len(flat), dtype=np.int32)
for i in range(0, len(flat), 65536): idx[i:i + 65536] = ((flat[i:i + 65536, None, :] - pal[None]) ** 2).sum(2).argmin(1)
q = pal[idx].reshape(AH, AW, 3)
used = set(int(v) for v in np.unique(idx.reshape(AH, AW)[[y for y in range(AH)], :]))  # 아틀라스 전체(빈 칸 포함)
out = bpy.data.images.new('kart_a2_baked32', AW, AH, alpha=False)
out.pixels.foreach_set(np.concatenate([q, np.ones((AH, AW, 1), np.float32)], 2).ravel())
p32 = os.path.join(OUT, 'kart_a2_baked32.png'); out.filepath_raw = p32; out.file_format = 'PNG'; out.save()
out = bpy.data.images.load(p32)

# 5) unlit(Emission) 재질 하나, BakedUV 만 남기고 FBX
um = bpy.data.materials.new('kart_a2_unlit'); um.use_nodes = True; n = um.node_tree.nodes; n.clear(); L = um.node_tree.links
tx = n.new('ShaderNodeTexImage'); tx.image = out; tx.interpolation = 'Closest'; em = n.new('ShaderNodeEmission'); mo = n.new('ShaderNodeOutputMaterial')
L.new(tx.outputs[0], em.inputs[0]); L.new(em.outputs[0], mo.inputs[0])
for o in obs:
    o.data.materials.clear(); o.data.materials.append(um)
    for f in o.data.polygons: f.material_index = 0
    o.data.uv_layers.remove(o.data.uv_layers['SourceUV'])
bpy.ops.object.select_all(action='DESELECT')
for o in obs: o.select_set(True)
bsx = n.new('ShaderNodeBsdfPrincipled'); L.new(tx.outputs[0], bsx.inputs['Base Color']); L.new(bsx.outputs[0], mo.inputs[0])  # FBX 엔 unlit 이 없어 diffuse 칸에 PNG 경로만
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, 'kart_a2.fbx'), use_selection=True, object_types={'MESH'}, axis_forward='-Z', axis_up='Y',
                         bake_anim=False, path_mode='COPY', embed_textures=False)
L.new(em.outputs[0], mo.inputs[0]); n.remove(bsx)

# 6) 미리보기 — 바닥 = V1b 길 텍스처(emission), 하늘 = V5 하늘색. 카메라 A/B/C 에서 카트가 화면을 얼마나 차지하는지 잰다
sun.hide_render = True; bg.inputs[0].default_value = (.33, .52, .62, 1); bg.inputs[1].default_value = 1
bpy.data.objects.remove(floor, do_unlink=True)
bpy.ops.mesh.primitive_plane_add(size=80, location=(0, 0, z0)); gp = bpy.context.object
for lp in gp.data.loops:
    co = gp.data.vertices[lp.vertex_index].co; gp.data.uv_layers.active.data[lp.index].uv = (co.x / 4, co.y / 8)
gm = bpy.data.materials.new('ground_path'); gm.use_nodes = True; n = gm.node_tree.nodes; n.clear()
t = n.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(J('art', 'source', 'scene-a-v1b', 'tex', 'path.png')); t.interpolation = 'Closest'
e = n.new('ShaderNodeEmission'); o2 = n.new('ShaderNodeOutputMaterial'); gm.node_tree.links.new(t.outputs[0], e.inputs[0]); gm.node_tree.links.new(e.outputs[0], o2.inputs[0])
gp.data.materials.append(gm)
sc.cycles.samples = 1; sc.cycles.use_denoising = False; sc.cycles.pixel_filter_type = 'BOX'; sc.cycles.filter_width = .01; sc.render.dither_intensity = 0
sc.render.resolution_x, sc.render.resolution_y, sc.render.resolution_percentage = 1280, 720, 100; sc.render.film_transparent = False
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam; cam.data.sensor_fit = 'VERTICAL'
P = [o.matrix_world @ v.co for o in obs for v in o.data.vertices]; size = {}
for k, (dist, hgt, fov, ahead, th) in CAMS.items():
    cam.location = (0, -dist, z0 + hgt); tgt = Vector((0, ahead, z0 + th)); cam.data.angle_y = math.radians(fov)
    cam.rotation_euler = (tgt - cam.location).to_track_quat('-Z', 'Y').to_euler(); bpy.context.view_layer.update()
    V = [world_to_camera_view(sc, cam, p) for p in P]; xs = [v.x for v in V]; ys = [v.y for v in V]
    size[k] = dict(cam=dict(back=dist, height=hgt, vfov=fov, look_ahead=ahead, look_h=th), w=round(max(xs) - min(xs), 3), h=round(max(ys) - min(ys), 3),
                   cy=round(1 - (max(ys) + min(ys)) / 2, 3))
    sc.render.filepath = os.path.join(REV, f'kart_cam_{k}.png'); bpy.ops.render.render(write_still=True)
cam.location = (2.4, 2.9, z0 + 1.5); cam.data.angle_y = math.radians(40)
cam.rotation_euler = (Vector((0, 0, z0 + .4)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
sc.render.filepath = os.path.join(REV, 'kart_34.png'); bpy.ops.render.render(write_still=True)
json.dump(dict(truck_ref=TRUCK, kart=size, kart_dims_m=[round(b - a, 2) for a, b in zip(src['lo'], src['hi'])], atlas=[AW, AH],
               palette_used=len(used)), open(os.path.join(REV, 'kart_size.json'), 'w'), indent=1)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(WORK, 'kart_a2_unlit.blend'))
pv = J('art', 'provenance.csv')
keep = [l for l in open(pv, encoding='utf-8', newline='').readlines() if not l.startswith('scene_a2_kart')]
open(pv, 'w', encoding='utf-8', newline='').writelines(keep)
with open(pv, 'a', newline='', encoding='utf-8') as f:
    csv.writer(f).writerow(['scene_a2_kart_baked32', 'art/export/scene-a2-kart/kart_a2_baked32.png',
                            'art/export/scene-a-v1b-photobash/kart_v1b.fbx (scene_a_v1b_* ambientCG CC0 사진 텍스처 + opus_garage_kart_v5.py 칠 텍스처)',
                            'CC0-1.0', datetime.date.today().isoformat(), 'opus_scene_a2_kart.py', 'no', hashlib.sha256(open(p32, 'rb').read()).hexdigest(),
                            'Cycles 128 낮은 해+하늘빛 굽기 · AO(.70+.30) · 노출+1 · scene-a-v5-combo palette32 가까운 색, 디더 없음'])
print('DONE', json.dumps(size), AW, AH, flush=True)
