"""장면 A2 원경 지붕 무리 + 마당 땅 — V5 방식(사진 바탕 × 구운 빛·AO → 공용 32색 → unlit) (아트2, 2026-10-27 과제 0525).
원경: art/thirdparty/ 의 CC0 건물 8종(새로 모델링 안 함)을 축소·회전·조합해 18채, 비탈 계단 4단에 겹쳐 놓는다.
  - 문(door) 물체는 버리고 창(windows)은 어두운 색으로 남긴다. 원래 재질은 지우고 면 방향으로 다시 칠한다:
    위를 보는 높은 면 = 지붕(주황 기와 / 창고는 청록 함석), 평지붕 꼭대기 = 밝은 시멘트, 나머지 = 벽(바랜 크림·황토·청록).
  - 바탕 무늬는 CC0 사진(Poly Haven 기와·회벽·돌벽, ambientCG 함석·흙)의 밝기만 따서 목표색에 곱한다.
  - 굽기는 원경이라 6텍셀/m(가까운 물건 32의 1/5). 안개는 굽지 않는다(Unity 안개가 unlit 에도 걸림).
마당: opus_scene_a2_yard_tex.py 의 yard_ground.png(32텍셀/m)를 판 하나에 깔고 같은 빛으로 굽는다.
출력: art/export/scene-a2-far/{far_town.fbx, far_town_baked32.png, yard_ground.fbx, yard_ground_baked32.png, kit.json}
    D:/Tools/Blender/blender.exe -b --factory-startup --python tools/asset-gen/opus_scene_a2_far.py
"""
import bpy, bmesh, json, math, os, random, hashlib
import numpy as np
from mathutils import Vector, Matrix

R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')); J = lambda *p: os.path.join(R, *p)
TP = J('art', 'thirdparty'); OUT = J('art', 'export', 'scene-a2-far'); WORK = J('art', 'models', 'opus_scene_a2_far')
for d in (OUT, WORK): os.makedirs(d, exist_ok=True)
PAL = J('art', 'export', 'scene-a-v5-combo', 'palette32.gpl')
HH = J('art', 'source', 'hillhouse-surfaces', 'photos'); PH = J('art', 'source', 'photos', 'scene-a')
FAR_DENS, YARD_DENS, GAP, AW = 6, 32, 2, 1024
random.seed(525)
bpy.ops.wm.read_factory_settings(use_empty=True); sc = bpy.context.scene

# ---------- 1) 바탕 타일: 사진 밝기 × 목표색 (2.5m 한 장, 32텍셀/m 원본)
TILE_M = 2.5
PHOTO = dict(roof=os.path.join(HH, 'clay_roof_tiles_diff_1k.jpg'), wall=os.path.join(HH, 'worn_plaster_wall_diff_1k.jpg'),
             stone=os.path.join(HH, 'stone_block_wall_diff_1k.jpg'), tin=os.path.join(PH, 'CorrugatedSteel005', 'CorrugatedSteel005_1K-JPG_Color.jpg'),
             soil=os.path.join(PH, 'Ground037', 'Ground037_1K-JPG_Color.jpg'))
CLASS = dict(  # 이름: (사진, sRGB 목표색, 무늬 세기)
    roof_tile=('roof', (196, 104, 60), .7), roof_tile2=('roof', (176, 88, 56), .7), roof_tin=('tin', (96, 140, 136), .5),
    wall_cream=('wall', (226, 214, 184), .45), wall_ochre=('wall', (206, 162, 98), .45), wall_teal=('wall', (110, 156, 150), .45),
    flat_top=('wall', (200, 194, 176), .4), window=('wall', (40, 52, 56), .2), terrace_wall=('stone', (176, 166, 140), .6), terrace_top=('soil', (122, 128, 74), .6))
TS = int(TILE_M * 32)


def tile(cls):
    src, col, k = CLASS[cls]
    if src not in PHOTO_CACHE:
        im = bpy.data.images.load(PHOTO[src]); w, h = im.size; a = np.array(im.pixels[:], np.float32).reshape(h, w, 4)[:, :, :3]
        L = a @ np.array([.299, .587, .114], np.float32); b = w // TS
        L = L[:b * TS, :b * TS].reshape(TS, b, TS, b).mean((1, 3)); PHOTO_CACHE[src] = L / L.mean()
    d = 1 + k * (PHOTO_CACHE[src] - 1); c = np.array(col, np.float32) / 255
    px = np.clip(c[None, None] * d[:, :, None], 0, 1)
    img = bpy.data.images.new('tile_' + cls, TS, TS, alpha=False)
    img.pixels.foreach_set(np.concatenate([px, np.ones((TS, TS, 1), np.float32)], 2).ravel()); img.pack(); return img


PHOTO_CACHE = {}
MAT = {}
for cls in CLASS:
    m = bpy.data.materials.new(cls); m.use_nodes = True; n = m.node_tree.nodes; n.clear(); L = m.node_tree.links
    out = n.new('ShaderNodeOutputMaterial'); bs = n.new('ShaderNodeBsdfDiffuse'); t = n.new('ShaderNodeTexImage'); t.image = tile(cls)
    uv = n.new('ShaderNodeUVMap'); uv.uv_map = 'SourceUV'; L.new(uv.outputs[0], t.inputs[0]); L.new(t.outputs[0], bs.inputs[0]); L.new(bs.outputs[0], out.inputs[0])
    MAT[cls] = m

# ---------- 2) 바탕 건물 읽기 → 바닥 가운데 원점, 창 면 표시
BASES = {
    'house1': 'elegantcrow-retro-house-pack/models/fbx/house1.fbx', 'house2': 'elegantcrow-retro-house-pack/models/fbx/house2.fbx',
    'house3': 'elegantcrow-retro-house-pack/models/fbx/house3.fbx', 'house4': 'elegantcrow-retro-house-pack/models/fbx/house4.fbx',
    'house5': 'elegantcrow-retro-house-pack/models/fbx/house5.fbx', 'shack': 'chillydurango-shacks-sheds/Shack A.blend',
    'shed': 'chillydurango-shacks-sheds/Large Shed A.blend', 'hangar': 'valsekamerplant-urban-stacked/Buildings Prebuild/Hangar1.glb',
    'flat': 'valsekamerplant-urban-stacked/Buildings Prebuild/flat_square_concrete.glb'}
LIC = {'elegantcrow': 'CC0 (Elegant Crow Retro House Pack)', 'chillydurango': 'CC0 (chilly_durango Shacks, Sheds)', 'valsekamerplant': 'CC0 (valsekamerplant PSX Urban stacked)'}
BASE = {}
for key, rel in BASES.items():
    before = set(bpy.data.objects); p = os.path.join(TP, rel)
    if p.endswith('.fbx'): bpy.ops.import_scene.fbx(filepath=p)
    elif p.endswith('.glb'): bpy.ops.import_scene.gltf(filepath=p)
    else:
        with bpy.data.libraries.load(p) as (a, b): b.objects = a.objects
        for o in b.objects:
            if o: sc.collection.objects.link(o)
    bpy.context.view_layer.update()
    new = [o for o in bpy.data.objects if o not in before]
    bm = bmesh.new(); win = bm.faces.layers.int.new('win')
    for o in new:
        if o.type != 'MESH' or 'door' in o.name.lower(): continue
        me = o.data.copy(); me.transform(o.matrix_world); n0 = len(bm.faces); bm.from_mesh(me); bm.faces.ensure_lookup_table()
        for f in bm.faces[n0:]: f[win] = int('window' in o.name.lower())
        bpy.data.meshes.remove(me)
    for o in new: bpy.data.objects.remove(o, do_unlink=True)
    lo = Vector([min(v.co[k] for v in bm.verts) for k in range(3)]); hi = Vector([max(v.co[k] for v in bm.verts) for k in range(3)])
    bmesh.ops.translate(bm, verts=bm.verts, vec=-Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z)))
    bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4]); bmesh.ops.recalc_face_normals(bm, faces=bm.faces)  # 뒤집힌 면 → 굽기에서 검게 나옴
    me = bpy.data.meshes.new('base_' + key); bm.to_mesh(me); bm.free(); me.materials.clear()
    BASE[key] = dict(mesh=me, src=rel, size=[round(v, 2) for v in (hi - lo)], tris=sum(len(p.vertices) - 2 for p in me.polygons))

# ---------- 3) 18채 배치: 계단 4단(한 단 = 깊이 11m·높이 3.2m), 앞 단이 낮고 뒤로 갈수록 올라가며 겹친다
ROWS = [  # (바탕, 배율, 벽, 지붕)
    [('house2', .9, 'wall_cream', 'roof_tile'), ('shack', 1.3, 'wall_teal', 'roof_tin'), ('house5', .85, 'wall_ochre', 'roof_tile2'),
     ('flat', .3, 'wall_cream', 'flat_top'), ('house3', .9, 'wall_teal', 'roof_tile')],
    [('house1', .85, 'wall_ochre', 'roof_tile'), ('hangar', .8, 'wall_teal', 'roof_tin'), ('house4', .85, 'wall_cream', 'roof_tile2'),
     ('flat', .32, 'wall_ochre', 'flat_top'), ('house2', .85, 'wall_cream', 'roof_tile')],
    [('house5', .9, 'wall_cream', 'roof_tile'), ('house3', .85, 'wall_ochre', 'roof_tile2'), ('flat', .35, 'wall_cream', 'flat_top'),
     ('house1', .8, 'wall_teal', 'roof_tile')],
    [('house4', .85, 'wall_ochre', 'roof_tile'), ('shed', .7, 'wall_teal', 'roof_tin'), ('house2', .9, 'wall_cream', 'roof_tile2'),
     ('house5', .85, 'wall_ochre', 'roof_tile')]]
DEPTH, STEP, WIDTH = 11, 3.2, 76
objs, kit_items = [], []
for r, row in enumerate(ROWS):
    xs = np.linspace(-WIDTH / 2 + 8, WIDTH / 2 - 8, len(row)) + (r % 2) * 4
    for i, (key, s, wall, roof) in enumerate(row):
        b = BASE[key]; me = b['mesh'].copy(); me.transform(Matrix.Scale(s, 4))
        rot = random.choice((0, 90, 180, 270)) + random.uniform(-12, 12)
        pos = Vector((xs[i] + random.uniform(-2.5, 2.5), r * DEPTH + random.uniform(-2, 2.5), r * STEP))
        ob = bpy.data.objects.new(f'far_{len(objs):02d}_{key}', me); sc.collection.objects.link(ob)
        ob.location = pos; ob.rotation_euler = (0, 0, math.radians(rot))
        for c in (wall, roof, 'window', 'flat_top'): me.materials.append(MAT[c])
        H = max(v.co.z for v in me.vertices); wl = me.attributes.get('win')
        for f in me.polygons:
            nz = f.normal.z; cz = f.center.z
            if wl and wl.data[f.index].value: f.material_index = 2
            elif key == 'flat' and nz > .95 and cz > H * .9: f.material_index = 3
            elif nz > .25 and cz > H * .45: f.material_index = 1
            else: f.material_index = 0
        objs.append(ob)
        kit_items.append(dict(name=ob.name, source='art/thirdparty/' + b['src'], license=LIC[b['src'].split('-')[0]], scale=s, rot_z_deg=round(rot, 1),
                              pos_blender=[round(v, 2) for v in pos], wall=wall, roof=roof, wall_rgb=CLASS[wall][1], roof_rgb=CLASS[roof][1],
                              tris=sum(len(p.vertices) - 2 for p in me.polygons)))
# 단 받침(옹벽 + 흙 윗면) — 단마다 상자 하나
for r in range(len(ROWS)):
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1)
    bmesh.ops.scale(bm, vec=(WIDTH + 8, DEPTH, 12), verts=bm.verts); bmesh.ops.translate(bm, vec=(2, r * DEPTH, r * STEP - 6.02), verts=bm.verts)
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.normal.z < -.5], context='FACES')  # 밑면 버림
    me = bpy.data.meshes.new(f'terrace_{r}'); bm.to_mesh(me); bm.free()
    me.materials.append(MAT['terrace_wall']); me.materials.append(MAT['terrace_top'])
    for f in me.polygons: f.material_index = 1 if f.normal.z > .5 else 0
    ob = bpy.data.objects.new(f'far_terrace_{r}', me); sc.collection.objects.link(ob); objs.append(ob)
    kit_items.append(dict(name=ob.name, source='새로 — 아트2 단 받침 상자(옹벽 stone_block_wall + 흙 Ground037 사진)', license='CC0 사진 + 프로젝트 작성',
                          pos_blender=[2, r * DEPTH, r * STEP], tris=12))


# ---------- 4) SourceUV(월드 상자 투영, 2.5m 타일) + 고유 BakedUV(선반 채우기)
def source_uv(o, scale=1 / TILE_M):
    me = o.data; uv = me.uv_layers.new(name='SourceUV'); M = o.matrix_world
    for f in me.polygons:
        n = (M.to_3x3() @ f.normal); ax = max(range(3), key=lambda k: abs(n[k]))
        for li in f.loop_indices:
            p = M @ me.vertices[me.loops[li].vertex_index].co
            uv.data[li].uv = ((p.x, p.y) if ax == 2 else (p.y, p.z) if ax == 0 else (p.x, p.z))
            uv.data[li].uv = (uv.data[li].uv[0] * scale, uv.data[li].uv[1] * scale)


def baked_uv(obs, dens):
    rects = []
    for o in obs:
        me = o.data; me.uv_layers.new(name='BakedUV'); M = o.matrix_world
        for f in me.polygons:
            ps = [M @ me.vertices[i].co for i in f.vertices]; nrm = (M.to_3x3() @ f.normal).normalized()
            u = (ps[1] - ps[0]).normalized(); v = nrm.cross(u).normalized(); qs = [((p - ps[0]).dot(u), (p - ps[0]).dot(v)) for p in ps]
            lo = [min(q[k] for q in qs) for k in (0, 1)]; hi = [max(q[k] for q in qs) for k in (0, 1)]
            rects.append((o, f.index, qs, lo, hi) + tuple(max(2, math.ceil((hi[k] - lo[k]) * dens)) for k in (0, 1)))
    x = y = GAP; rh = 0; placed = []
    for r in sorted(rects, key=lambda r: (r[6], r[5]), reverse=True):
        if x + r[5] + GAP > AW: x = GAP; y += rh + GAP; rh = 0
        placed.append(r + (x, y)); x += r[5] + GAP; rh = max(rh, r[6])
    AH = 2 ** math.ceil(math.log2(y + rh + GAP))
    for o, fi, qs, lo, hi, w, h, x, y in placed:
        uv = o.data.uv_layers['BakedUV']; f = o.data.polygons[fi]
        for li, q in zip(f.loop_indices, qs):
            uv.data[li].uv = ((x + .5 + (q[0] - lo[0]) / max(hi[0] - lo[0], 1e-8) * (w - 1)) / AW, (y + .5 + (q[1] - lo[1]) / max(hi[1] - lo[1], 1e-8) * (h - 1)) / AH)
    for o in obs: o.data.uv_layers.active = o.data.uv_layers['BakedUV']
    return AH


for o in objs: source_uv(o)


def smart_uv(obs):  # 원경: 면마다 따로 펴면 판자 한 장이 2px 가 되어 지글거림 → 붙은 면끼리 섬으로 묶어 펴고 한 장에 채움
    for o in obs:
        o.data.uv_layers.new(name='BakedUV'); o.data.uv_layers.active = o.data.uv_layers['BakedUV']
    bpy.ops.object.select_all(action='DESELECT')
    for o in obs: o.select_set(True)
    bpy.context.view_layer.objects.active = obs[0]; bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=.003, area_weight=0, correct_aspect=True, scale_to_bounds=False)
    bpy.ops.uv.pack_islands(margin=.004, rotate=True); bpy.ops.object.mode_set(mode='OBJECT')
    return 1024


HOUSES = [o for o in objs if 'terrace' not in o.name]; TERS = [o for o in objs if 'terrace' in o.name]
AH_FAR = smart_uv(HOUSES); smart_uv(TERS)  # 단 받침은 따로(같이 펴면 넓은 옹벽이 아틀라스를 다 먹어 집 면이 1~2px 가 됨)

# 마당 판: SourceUV = yard_ground.png 한 장, BakedUV = 같은 배치(32텍셀/m 640×432 → 1024×512 아틀라스 왼쪽 아래)
YX0, YX1, YY0, YY1 = -10, 10, -9, 4.5
bpy.ops.mesh.primitive_plane_add(size=1); yard = bpy.context.object; yard.name = 'yard_ground'
yard.scale = (YX1 - YX0, YY1 - YY0, 1); yard.location = ((YX0 + YX1) / 2, (YY0 + YY1) / 2, 0); bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
yi = bpy.data.images.load(J('art', 'source', 'scene-a2-yard', 'yard_ground.png')); YW, YH = yi.size
ym = bpy.data.materials.new('yard_src'); ym.use_nodes = True; n = ym.node_tree.nodes; n.clear(); L = ym.node_tree.links
o_ = n.new('ShaderNodeOutputMaterial'); bs = n.new('ShaderNodeBsdfDiffuse'); t = n.new('ShaderNodeTexImage'); t.image = yi; t.interpolation = 'Closest'
uvn = n.new('ShaderNodeUVMap'); uvn.uv_map = 'SourceUV'; L.new(uvn.outputs[0], t.inputs[0]); L.new(t.outputs[0], bs.inputs[0]); L.new(bs.outputs[0], o_.inputs[0])
yard.data.materials.append(ym); yard.data.uv_layers[0].name = 'SourceUV'; yb = yard.data.uv_layers.new(name='BakedUV')
YAW, YAH = 1024, 512
for li, l in enumerate(yard.data.loops):
    su = yard.data.uv_layers['SourceUV'].data[li].uv; yb.data[li].uv = (su[0] * YW / YAW, su[1] * YH / YAH)
yard.data.uv_layers.active = yb

# ---------- 5) 빛 — V5 와 같은 낮은 가을 해·청록 하늘, Cycles 128
bpy.ops.object.light_add(type='SUN'); sun = bpy.context.object; sun.data.energy = 2.4; sun.data.color = (1, .83, .60); sun.data.angle = .10
sun.rotation_euler = Vector((.7, .65, -.45)).to_track_quat('-Z', 'Y').to_euler()
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
bk = sc.render.bake; bk.margin = 2; bk.use_clear = True; bk.use_pass_direct = bk.use_pass_indirect = bk.use_pass_color = True
pal = np.array([[int(v) for v in l.split()[:3]] for l in open(PAL).read().splitlines() if l[:1].isdigit()], dtype=np.float32) / 255
assert len(pal) == 32


def bake_set(obs, mats, tag, w, h):
    img = bpy.data.images.new(tag + '_bake', w, h, alpha=False); img.generated_color = (.72, .66, .55, 1)  # 덜 채운 틈이 검게 안 읽히게 벽 색 근처
    for m in mats:
        t = m.node_tree.nodes.new('ShaderNodeTexImage'); t.image = img; m.node_tree.nodes.active = t
    bpy.ops.object.select_all(action='DESELECT')
    for o in obs: o.select_set(True)
    bpy.context.view_layer.objects.active = obs[0]
    res = {}
    for kind in ('DIFFUSE', 'AO'):
        bpy.ops.object.bake(type=kind); p = os.path.join(WORK, f'{tag}_{kind.lower()}.png'); img.filepath_raw = p; img.file_format = 'PNG'; img.save()
        res[kind] = np.array(bpy.data.images.load(p).pixels[:], np.float32).reshape(h, w, 4)[:, :, :3]
    for m in mats:
        nn = m.node_tree.nodes; nn.remove(nn.active)
    # 아트1 V5 후처리: AO(.70+.30·AO)·청록 그늘·노출 +1스톱 → 공용 32색(가까운 색, 디더 없음)
    arr = res['DIFFUSE'] * 255; ao = res['AO'][:, :, 0]
    arr *= (.70 + .30 * ao)[:, :, None]; shade = (1 - ao) * .16; arr[:, :, 1] += shade * 24; arr[:, :, 2] += shade * 28
    s = np.clip(arr / 255, 0, 1); lin = np.where(s <= .04045, s / 12.92, ((s + .055) / 1.055) ** 2.4)
    lin = lin * 2.0 + ((1 - s.max(2)) ** 2)[:, :, None] * np.array([.009, .017, .019])
    enc = np.clip(np.where(lin <= .0031308, lin * 12.92, 1.055 * np.maximum(lin, 0) ** (1 / 2.4) - .055), 0, 1)
    flat = enc.reshape(-1, 3); idx = np.empty(len(flat), dtype=np.int32)
    for i in range(0, len(flat), 65536): idx[i:i + 65536] = ((flat[i:i + 65536, None, :] - pal[None]) ** 2).sum(2).argmin(1)
    q = pal[idx].reshape(h, w, 3)
    o = bpy.data.images.new(tag + '_baked32', w, h, alpha=False)
    o.pixels.foreach_set(np.concatenate([q, np.ones((h, w, 1), np.float32)], 2).ravel())
    p32 = os.path.join(OUT, f'{tag}_baked32.png'); o.filepath_raw = p32; o.file_format = 'PNG'; o.save()
    return bpy.data.images.load(p32), sorted(set(int(v) for v in np.unique(idx)))


far_img, far_used = bake_set(HOUSES, [MAT[c] for c in CLASS if not c.startswith('terrace')], 'far_town', AW, AH_FAR)
ter_img, ter_used = bake_set(TERS, [MAT['terrace_wall'], MAT['terrace_top']], 'far_terrace', 256, 256)
yard.hide_render = False
for o in objs: o.hide_render = True  # 마당 굽기 땐 원경을 빼서(먼 곳이라 그늘도 안 닿음) 판 하나만
yard_img, yard_used = bake_set([yard], [ym], 'yard_ground', YAW, YAH)
for o in objs: o.hide_render = False


# ---------- 6) unlit(Emission) + FBX
def unlit(obs, img, name):
    um = bpy.data.materials.new(name); um.use_nodes = True; n = um.node_tree.nodes; n.clear(); L = um.node_tree.links
    tx = n.new('ShaderNodeTexImage'); tx.image = img; tx.interpolation = 'Closest'; em = n.new('ShaderNodeEmission'); mo = n.new('ShaderNodeOutputMaterial')
    L.new(tx.outputs[0], em.inputs[0]); L.new(em.outputs[0], mo.inputs[0])
    for o in obs:
        o.data.materials.clear(); o.data.materials.append(um)
        for f in o.data.polygons: f.material_index = 0
        o.data.uv_layers.remove(o.data.uv_layers['SourceUV'])
        if 'win' in o.data.attributes: o.data.attributes.remove(o.data.attributes['win'])
    return um


def export(obs, path):
    bpy.ops.object.select_all(action='DESELECT')
    for o in obs: o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'MESH'}, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
                             path_mode='COPY', embed_textures=False, mesh_smooth_type='FACE', use_mesh_modifiers=True)


unlit(HOUSES, far_img, 'far_town_unlit'); unlit(TERS, ter_img, 'far_terrace_unlit'); unlit([yard], yard_img, 'yard_ground_unlit')
export(objs, os.path.join(OUT, 'far_town.fbx')); export([yard], os.path.join(OUT, 'yard_ground.fbx'))
for o in sc.objects:
    if o.type == 'LIGHT': o.hide_render = True
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(WORK, 'far_town_unlit.blend'))

wa = sum(p.area for o in HOUSES for p in o.data.polygons)
ua = 0
for o in HOUSES:
    uv = o.data.uv_layers['BakedUV'].data if 'BakedUV' in o.data.uv_layers else o.data.uv_layers[0].data
    for p in o.data.polygons:
        q = [uv[li].uv for li in p.loop_indices]; ua += abs(sum(q[i][0] * q[i - 1][1] - q[i - 1][0] * q[i][1] for i in range(len(q)))) / 2
TPM = round(math.sqrt(ua * AW * AH_FAR / wa), 1)
sha = lambda p: hashlib.sha256(open(p, 'rb').read()).hexdigest()
tris = sum(len(p.vertices) - 2 for o in objs for p in o.data.polygons)
kit = dict(
    task='20261027-0525', note='도현 승인 전. F1/Assets 에 넣지 않음. Unity 임포트 미확인.',
    axes='Blender m, z-up, 한 FBX 안에 물체별 원점(바닥 가운데). FBX 는 Y-up·forward −Z. Unity 로 (x,y,z) → (−x, z, −y).',
    placement=dict(far_town_origin_blender=[16, 78, -8.6], note='SCENE-A2 덩어리 장면 기준: 길 오른쪽 아래 마을 자리(y 62~128) 앞 단 바닥. 뒤 단일수록 3.2m 씩 높다.',
                   yard_ground_origin_blender=[0, 0, .02], yard_note='정비소 마당 x −10~10, y −9~4.5 를 덮는 판. 카트 원점 기준.'),
    files=dict(far_town=dict(fbx='far_town.fbx', png='far_town_baked32.png', size=[AW, AH_FAR], texels_per_m=TPM, tris=tris, objects=len(objs),
                             palette_used=len(far_used), sha256=sha(os.path.join(OUT, 'far_town_baked32.png')),
                             terrace_png='far_terrace_baked32.png', terrace_size=[256, 256], terrace_note='단 받침 4개(far_terrace_*)는 이 PNG 를 쓴다'),
               yard_ground=dict(fbx='yard_ground.fbx', png='yard_ground_baked32.png', size=[YAW, YAH], used_px=[YW, YH], texels_per_m=YARD_DENS, tris=2,
                                palette_used=len(yard_used), sha256=sha(os.path.join(OUT, 'yard_ground_baked32.png')))),
    import_settings='텍스처 sRGB / Point / 비압축 / 밉맵 끔, Unlit BaseMap, 틴트 흰색. 안개는 Unity 장면 안개(굽지 않음).',
    method='V5: 사진 밝기 × 목표색 바탕 → Cycles 128 해(2.4, (1,.83,.60))+하늘(.30,.52,.65 ×.65) DIFFUSE+AO → AO(.70+.30)·청록 그늘·노출+1 → scene-a-v5-combo/palette32.gpl 가까운 색, 디더 없음 → Emission.',
    classes={k: dict(photo=os.path.relpath(PHOTO[v[0]], R).replace('\\', '/'), rgb=v[1], detail=v[2]) for k, v in CLASS.items()},
    bases={k: dict(source='art/thirdparty/' + v['src'], size_m=v['size'], tris=v['tris']) for k, v in BASE.items()},
    items=kit_items)
json.dump(kit, open(os.path.join(OUT, 'kit.json'), 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
print('DONE', json.dumps(dict(tpm=TPM, tris=tris, far=[AW, AH_FAR], far_pal=len(far_used), yard_pal=len(yard_used), bases={k: v['tris'] for k, v in BASE.items()})), flush=True)
