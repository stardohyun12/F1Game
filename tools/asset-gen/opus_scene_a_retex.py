"""장면 A 텍스처 시도 V1/V2 (아트2, 2026-10-27 과제 0412) — 형태는 v5 그대로, 텍스처·UV 만 바꾼다.
    D:/Tools/Blender/blender.exe -b art/models/opus_garage_kart_v5.blend --python tools/asset-gen/opus_scene_a_retex.py -- v1
    (v2 도 같음)
V1 (EDC 포토배싱): 월드 텍스처 재질(tin_wall·tin_roof·planks·planks_dark·wood·floor·dirt·ground·metal)을
    art/source/scene-a-v1/tex/ 의 새 PNG 로 바꾸고 UV 를 월드 박스 투영으로 다시 편다
    (면 법선의 큰 축: 위/아래 → (x, y), ±x → (y, z), ±y → (x, z); ÷ tile_m.json — 1m = 32 텍셀, v = 월드 z 라 벽 아래 줄 = 땅).
    crate·drum·tire 는 원래 둘레/맞춤 UV 그대로 이미지만 바꿈. 글자 데칼·사람·잎 등은 v5 그대로.
V2 (A Short Hike 팔레트): art/palette/palette.json 의 칸으로 UV 를 모은다.
    위를 보는 면 → 밝은 칸, 해 쪽(SUN) 옆면 → 중간 칸, 나머지 → 어두운 칸(계단 명암). 4m² 넘는 면 → 그라데이션 칸
    (옆면은 월드 z, 바닥 면은 월드 y 로 위치를 정함). 글자 데칼(palette.json keep)은 자기 텍스처.
둘 다: 그림자 없음(발광 재질 = unlit), 거리 안개(카메라 거리 12~70m → 안개색으로 최대 55%), 하늘 = 안개색 한 색.
미리보기 x1_game 1장(참고용 — 판정은 Unity) + FBX(garage·props·kart·env, 원점은 v5 와 같음).
"""
import bpy, json, math, os, sys
from mathutils import Matrix, Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
VAR = sys.argv[sys.argv.index('--') + 1] if '--' in sys.argv else 'v1'
B = VAR.endswith('b')  # v1b·v2b = 과제 0430(직전 판 약점 고침) — 폴더·파일만 다르고 방법은 같음
REV = os.path.join(ROOT, 'art', '_review', '2026-10-27-scene-a-tex' + ('-b' if B else ''))
EXP = os.path.join(ROOT, 'art', 'export', {'v1': 'scene-a-v1-photobash', 'v2': 'scene-a-v2-palette',
                                           'v1b': 'scene-a-v1b-photobash', 'v2b': 'scene-a-v2b-palette'}[VAR])
V1TEX = os.path.join(ROOT, 'art', 'source', 'scene-a-v1b' if B else 'scene-a-v1', 'tex')
PAL = os.path.join(ROOT, 'art', 'palette')
PAL_JSON = 'palette_b.json' if B else 'palette.json'
PER_OBJ = {'apron': 'path'} if B else {}  # 정비소 앞 마당 길(콘크리트 띠) → 흙·자갈·바퀴 자국. apron 재질은 사람 옷과 같이 써서 물체 단위로 새 재질
FOG = (0.80, 0.84, 0.82)  # 늦가을 옅은 회청 — v5 시트 하늘 지평선색(208,218,208) 근처
SUN = Vector((.5, -.6, .62)).normalized()
KART_AT, KART_ROT, D = Vector((.7, -2.3, 0)), math.radians(-10), 12.0
WORLD_UV = ('tin_wall', 'tin_roof', 'planks', 'planks_dark', 'wood', 'floor', 'dirt', 'ground', 'metal', 'path')
SWAP_ONLY = ('crate', 'drum_red', 'drum_blue', 'drum_top', 'tire')


def texname(m):
    if m is None or not m.use_nodes: return None
    img = next((n.image for n in m.node_tree.nodes if n.type == 'TEX_IMAGE' and n.image), None)
    return os.path.splitext(os.path.basename(img.filepath))[0] if img else None


def img_node(m):
    return next(n for n in m.node_tree.nodes if n.type == 'TEX_IMAGE' and n.image)


def add_fog(m):
    """발광색 = 텍스처 → 카메라 거리로 안개색과 섞음. 그림자·조명 없음."""
    nt = m.node_tree; t = img_node(m); t.interpolation = 'Closest'
    em = next((n for n in nt.nodes if n.type == 'EMISSION'), None)
    if em is None: return
    cam = nt.nodes.new('ShaderNodeCameraData')
    mr = nt.nodes.new('ShaderNodeMapRange'); mr.inputs['From Min'].default_value = 12; mr.inputs['From Max'].default_value = 70
    mr.inputs['To Max'].default_value = .55; mr.clamp = True
    mix = nt.nodes.new('ShaderNodeMixRGB'); mix.inputs['Color2'].default_value = (*FOG, 1)
    nt.links.new(cam.outputs['View Distance'], mr.inputs['Value']); nt.links.new(mr.outputs['Result'], mix.inputs['Fac'])
    nt.links.new(t.outputs['Color'], mix.inputs['Color1']); nt.links.new(mix.outputs['Color'], em.inputs['Color'])


def meshes():
    return [o for o in bpy.data.objects if o.type == 'MESH' and o.users_collection and o.users_collection[0].name in ('garage', 'kart', 'props', 'env')]


def v1():
    tiles = json.load(open(os.path.join(V1TEX, 'tile_m.json')))
    for m in bpy.data.materials:
        tn = texname(m)
        if tn in WORLD_UV or tn in SWAP_ONLY:
            n = img_node(m); n.image = bpy.data.images.load(os.path.join(V1TEX, tn + '.png'), check_existing=True)
            m['v1'] = tn
    for on, tn in PER_OBJ.items():
        o = bpy.data.objects[on]; m = o.material_slots[0].material.copy(); m.name = tn
        img_node(m).image = bpy.data.images.load(os.path.join(V1TEX, tn + '.png'), check_existing=True); m['v1'] = tn
        for s in o.material_slots: s.material = m
    for o in meshes():
        me = o.data
        if not me.uv_layers: me.uv_layers.new()
        uv = me.uv_layers.active.data; mw = o.matrix_world; n3 = mw.to_3x3()
        cx = sum((mw @ v.co).x for v in me.vertices) / max(len(me.vertices), 1)  # 길은 가운데 = 텍스처 가운데(바퀴 자국 정렬)
        for p in me.polygons:
            m = o.material_slots[p.material_index].material if o.material_slots else None
            tn = m.get('v1') if m else None
            if tn not in WORLD_UV: continue
            tu, tv = tiles[tn]; nw = (n3 @ p.normal).normalized(); ax = max(range(3), key=lambda i: abs(nw[i]))
            for li in p.loop_indices:
                c = mw @ me.vertices[me.loops[li].vertex_index].co
                u, v = ((c.x, c.y), (c.y, c.z), (c.x, c.z))[(2, 0, 1).index(ax)]
                if ax == 0 and nw.x < 0: u = -u
                if ax == 1 and nw.y > 0: u = -u
                if tn == 'path' and ax == 2: uv[li].uv = ((c.x - cx) / tu + .5, v / tv); continue
                uv[li].uv = (u / tu, v / tv)


def v2():
    P = json.load(open(os.path.join(PAL, PAL_JSON))); W, H = P['size']
    img = bpy.data.images.load(os.path.join(PAL, P['image']), check_existing=True)
    pm = bpy.data.materials.new('autumn_palette'); pm.use_nodes = True; nt = pm.node_tree; nt.nodes.clear()
    t = nt.nodes.new('ShaderNodeTexImage'); t.image = img; t.interpolation = 'Closest'
    em = nt.nodes.new('ShaderNodeEmission'); o_ = nt.nodes.new('ShaderNodeOutputMaterial')
    nt.links.new(t.outputs['Color'], em.inputs['Color']); nt.links.new(em.outputs[0], o_.inputs[0])
    centre = lambda r: ((r[0] + r[2] / 2) / W, 1 - (r[1] + r[3] / 2) / H)
    count = dict(light=0, mid=0, dark=0, grad=0, keep=0)
    for o in meshes():
        me = o.data
        if not me.uv_layers: me.uv_layers.new()
        uv = me.uv_layers.active.data; mw = o.matrix_world; n3 = mw.to_3x3()
        ws = [mw @ v.co for v in me.vertices]
        zs, ys = [c.z for c in ws] or [0], [c.y for c in ws] or [0]
        z0, z1, y0, y1 = min(zs), max(zs), min(ys), max(ys)
        slot_of = {}
        for p in me.polygons:
            m = o.material_slots[p.material_index].material if o.material_slots else None
            e = P['mats'].get(texname(m))
            if e is None: count['keep'] += 1; continue
            nw = (n3 @ p.normal).normalized()
            area = p.area * abs(mw.determinant()) ** (2 / 3)
            if area > 4.0:
                gx, gy, gw, gh = e['grad_px']; count['grad'] += 1
                for li in p.loop_indices:
                    c = ws[me.loops[li].vertex_index]
                    k = 1 - (c.z - z0) / max(z1 - z0, 1e-3) if abs(nw.z) < .7 else (c.y - y0) / max(y1 - y0, 1e-3)
                    uv[li].uv = ((gx + gw / 2) / W, 1 - (gy + .5 + k * (gh - 1)) / H)  # 위 = 밝은 계단
            else:
                key = 'light' if nw.z > .7 else ('mid' if nw.dot(SUN) > .15 else 'dark'); count[key] += 1
                cu = centre(e[key + '_px'])
                for li in p.loop_indices: uv[li].uv = cu
            slot_of[p.material_index] = True
        for i in slot_of: o.material_slots[i].material = pm  # 팔레트 칸을 쓰는 슬롯은 모두 같은 재질 하나
    print('V2 faces', count)
    return count


def render():
    s = bpy.context.scene
    for m in bpy.data.materials:
        if m.use_nodes and any(n.type == 'TEX_IMAGE' and n.image for n in m.node_tree.nodes): add_fog(m)
    if s.world is None: s.world = bpy.data.worlds.new('w')
    s.world.use_nodes = True; bg = s.world.node_tree.nodes.get('Background')
    if bg: bg.inputs['Color'].default_value = (*FOG, 1); bg.inputs['Strength'].default_value = 1.0
    for ob in bpy.data.objects:
        if ob.type == 'LIGHT': ob.hide_render = True
    s.view_settings.view_transform = 'Standard'; s.render.film_transparent = False  # v5 는 투명 배경(시트에서 하늘 합성)
    s.camera = bpy.data.objects['x1_game']; s.render.resolution_x, s.render.resolution_y = 1280, 720
    s.render.resolution_percentage = 100
    os.makedirs(REV, exist_ok=True); s.render.filepath = os.path.join(REV, f'{VAR}_game.png')
    bpy.ops.render.render(write_still=True)


def to_principled():
    for m in bpy.data.materials:
        if not m.use_nodes: continue
        img = next((n.image for n in m.node_tree.nodes if n.type == 'TEX_IMAGE' and n.image), None)
        if img is None: continue
        nt = m.node_tree; nt.nodes.clear()
        t = nt.nodes.new('ShaderNodeTexImage'); t.image = img; t.interpolation = 'Closest'
        b = nt.nodes.new('ShaderNodeBsdfPrincipled'); b.inputs['Roughness'].default_value = 1.0
        o = nt.nodes.new('ShaderNodeOutputMaterial')
        nt.links.new(t.outputs['Color'], b.inputs['Base Color']); nt.links.new(b.outputs[0], o.inputs[0])


def export(cname, path, M):
    C = bpy.data.collections[cname]
    bpy.ops.object.select_all(action='DESELECT')
    for ob in C.objects: ob.matrix_world = M @ ob.matrix_world; ob.select_set(True)
    bpy.context.view_layer.objects.active = C.objects[0]
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
                             apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True, use_mesh_modifiers=True,
                             mesh_smooth_type='FACE', path_mode='COPY', embed_textures=False)
    for ob in C.objects: ob.matrix_world = M.inverted() @ ob.matrix_world


def main():
    info = v1() if VAR.startswith('v1') else v2()
    render()
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT, 'art', 'models', f'opus_scene_a_{VAR}.blend'))
    os.makedirs(EXP, exist_ok=True); to_principled()
    G = Matrix.Translation((0, -D / 2, 0))
    kartM = (Matrix.Translation(KART_AT) @ Matrix.Rotation(KART_ROT, 4, 'Z')).inverted()
    for c, f, M in (('garage', 'garage', G), ('props', 'garage_props', G), ('env', 'env', G), ('kart', 'kart', kartM)):
        if c in bpy.data.collections and bpy.data.collections[c].objects: export(c, os.path.join(EXP, f'{f}_{VAR}.fbx'), M)
    print('DONE', VAR, info)


main()
