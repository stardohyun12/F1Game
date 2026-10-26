"""장면 A2 구성안 — 덩어리(blockout) 배치 + 게임 컷 렌더 + 화면 띠 측정 (아트2, 2026-10-27 과제 0511).
좌표(Blender, m): 카트 = 원점, 카트 앞(마을 쪽) = +y, 정비소는 카메라 뒤(y −8 ~ −16)에 있어 화면에 안 들어온다.
배치 목록은 ITEMS 하나이고 art/_review/2026-10-27-scene-a2/layout.json 으로도 남긴다(평면 스케치·Unity 배치가 같은 목록을 읽게).
렌더 두 벌 × 카메라 두 개(A = 지시 5.6/2.8/64, D = 카트를 ⑥ 트럭 폭만큼 보이게 3.9/2.0/64):
  - look_*.png: 덩어리 단색 + 거리 안개
  - id_*.png: 층별 색(앞 물건·앞 땅·중 물건·중 땅·원경·하늘·카트)
  - 층 면적·행별 띠 높이·아래 1/3 의 맨땅 비율을 measure.json 에 적는다.
    D:/Tools/Blender/blender.exe -b --factory-startup --python tools/asset-gen/opus_scene_a2_blockout.py
"""
import bpy, json, math, os, random
import numpy as np
from mathutils import Vector, Matrix
from bpy_extras.object_utils import world_to_camera_view

R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
REV = os.path.join(R, 'art', '_review', '2026-10-27-scene-a2'); os.makedirs(REV, exist_ok=True)
KART = os.path.join(R, 'art', 'export', 'scene-a2-kart', 'kart_a2.fbx')
CAMS = {'A': (5.6, 2.8, 64, 6, 1.0), 'D': (3.9, 2.0, 64, 5, .3)}
FOG, FOG0, FOG1, FOGMAX = (.86, .85, .80), 12, 140, .65
TP = 'art/thirdparty/'


def h(x, y):
    """땅 높이: 마당(y<4)은 평평, 길 따라 내려가고(60m 에서 −7.8m), 오른쪽(마을·바다)은 더 낮고 왼쪽은 언덕, 130m 너머는 바다로 떨어짐."""
    yy = max(0, y - 4)
    z = -min(yy, 60) * .13 - max(0, yy - 60) * .03
    z -= .05 * max(0, x - 8) * min(1, yy / 10)
    z += .22 * max(0, -x - 9) * min(1, (y + 15) / 20)
    z -= max(0, y - 130) * .08
    return z


ROAD = [(0, -12), (0, 0), (0, 8), (1.5, 16), (5, 24), (10, 32), (16, 40), (22, 52), (26, 70), (27, 95), (24, 125)]
# 물건: (이름, 층, 출처, (x, y), 회전°, (폭, 깊이, 높이), 색, 모양[box|cyl|pole|car|house|tree], 기울기°)
ITEMS = [
    ('tire_stack_L1', 'near', 'garage-kart-v5/garage_props_v5.fbx 타이어 (V1b tire)', (-3.2, .6), 10, (.9, .9, .95), (.16, .16, .17), 'cyl', 0),
    ('tire_stack_L2', 'near', 'garage-kart-v5/garage_props_v5.fbx 타이어', (-4.1, 1.7), 0, (.9, .9, .6), (.18, .18, .19), 'cyl', 6),
    ('drum_red', 'near', 'garage-kart-v5/garage_props_v5.fbx 드럼 (V1b drum_red)', (3.3, .9), 0, (.8, .8, 1.15), (.70, .26, .20), 'cyl', 0),
    ('drum_teal', 'near', 'garage-kart-v5/garage_props_v5.fbx 드럼 (V1b drum_blue)', (3.95, 1.9), 0, (.8, .8, 1.15), (.28, .55, .52), 'cyl', 4),
    ('pot_R1', 'near', TP + 'valsekamerplant-plants-pots/pots & plants (화분+잎)', (2.5, -.3), 0, (.55, .55, .8), (.72, .44, .30), 'cyl', 0),
    ('pot_R2', 'near', TP + 'valsekamerplant-plants-pots/pots & plants', (2.9, 3.3), 0, (.5, .5, .7), (.45, .55, .32), 'cyl', 0),
    ('pot_L', 'near', TP + 'valsekamerplant-plants-pots/pots & plants', (-2.6, 3.6), 0, (.6, .6, .9), (.62, .40, .28), 'cyl', 0),
    ('pallet_crates', 'near', TP + 'chillydurango-shacks-sheds/Pallet.blend + garage_props_v5 상자', (-4.6, 3.1), 18, (1.3, 1.1, .9), (.66, .52, .34), 'box', 0),
    ('fence_L', 'near', TP + 'valsekamerplant-fences/Fences/low_wooden_fence (기둥 기울기 ±5~10°)', (-4.6, 4.6), 3, (4.2, .12, .95), (.58, .46, .33), 'box', 6),
    ('fence_R', 'near', TP + 'valsekamerplant-fences/Fences/low_wooden_fence', (4.8, 4.5), -4, (4.4, .12, .9), (.58, .46, .33), 'box', -5),
    ('sign_garage', 'near', 'garage-kart-v5/garage_props_v5.fbx 표지판(다리 2개)', (3.1, 5.4), -20, (1.6, .15, 2.0), (.30, .45, .62), 'box', 4),
    ('oil_stain', 'near', '새로 — 아트2 데칼(기름 얼룩·맨홀, 0.1~0.3m 무늬로 아래 빈 칸 줄임)', (-.9, 1.6), 0, (1.4, .9, .02), (.30, .27, .22), 'cyl', 0),
    ('manhole', 'near', '새로 — 아트2 데칼', (1.3, 3.4), 0, (.8, .8, .03), (.36, .34, .32), 'cyl', 0),
    ('yard_curb', 'near', '새로 — 아트2 (마당 끝 콘크리트 턱, 길 양옆)', (0, 4.2), 0, (3.0, .3, .18), (.68, .64, .56), 'box', 0),
    ('pole_1', 'mid', TP + 'ward727-train-pack/fbx/electic_line.fbx (CC0) — 전선은 아트2 처짐 곡선', (-3.2, 10), 0, (.28, .28, 7.5), (.40, .32, .25), 'pole', 8),
    ('pole_2', 'mid', TP + 'ward727-train-pack/fbx/electic_line.fbx', (-2.4, 22), 0, (.28, .28, 7.5), (.40, .32, .25), 'pole', -4),
    ('pole_3', 'mid', TP + 'ward727-train-pack/fbx/electic_line.fbx', (1.8, 33), 0, (.28, .28, 7.5), (.40, .32, .25), 'pole', 11),
    ('pole_4', 'mid', TP + 'ward727-train-pack/fbx/electic_line.fbx', (8.5, 44), 0, (.28, .28, 7.5), (.40, .32, .25), 'pole', 3),
    ('street_lamp', 'mid', TP + 'valsekamerplant-urban-stacked/Traffic lights/street_light.glb', (4.6, 14), 0, (.25, .25, 5.0), (.30, .34, .32), 'pole', 2),
    ('bicycle', 'mid', TP + 'polypizza-misc/OldBike_AeronQuick_bgT_0ItQRxd.glb (CC-BY 3.0 — 크레딧)', (6.2, 18.5), 70, (1.7, .5, 1.0), (.25, .52, .55), 'box', 12),
    ('old_car', 'mid', TP + 'ggbot-psx-style-cars/Car 0N (낡은 왜건 1대 — 아트1 이 고름)', (7.4, 26.5), 25, (1.8, 4.2, 1.5), (.62, .50, .30), 'car', 0),
    ('house_L1', 'mid', TP + 'elegantcrow-retro-house-pack/models/fbx/house2.fbx (재칠: 크림 벽·주황 지붕)', (-9, 16), 8, (7, 6, 3.6), (.86, .80, .66), 'house', 0),
    ('house_R1', 'mid', TP + 'elegantcrow-retro-house-pack/models/fbx/house4.fbx (재칠: 황토 벽·청록 문)', (12.5, 22), -15, (8, 7, 3.8), (.80, .66, .46), 'house', 0),
    ('shed_L2', 'mid', TP + 'chillydurango-shacks-sheds/Shack A.blend (골함석 창고)', (-5.5, 35), 20, (6, 5, 3.2), (.52, .60, .56), 'house', 0),
    ('tree_L', 'mid', TP + 'elegantcrow-retro-nature-pack/models/FBX/trees (가을 텍스처, 키 = 벽의 3배)', (-12.5, 9), 0, (5, 5, 10), (.78, .44, .18), 'tree', 0),
    ('tree_R', 'mid', TP + 'elegantcrow-retro-nature-pack/models/FBX/trees (가을)', (14.5, 11), 0, (5, 5, 9), (.84, .64, .22), 'tree', 0),
    ('tree_L2', 'mid', TP + 'elegantcrow-retro-nature-pack/models/FBX/trees (가을)', (-11, 28), 0, (4.5, 4.5, 9), (.70, .36, .16), 'tree', 0),
]
random.seed(11)
for i in range(16):  # 원경 마을 지붕 무리 — 아트2 절차 생성(harbor-kit-v0 덩어리를 원경용 저폴리로)
    x, y = random.uniform(-14, 46), random.uniform(62, 128)
    if abs(x - 26) < 4 and y < 100: x += 9
    ITEMS.append((f'town_{i:02d}', 'far', '새로 — 아트2 절차 생성(harbor-kit-v0 → 원경 저폴리 집, 안개 속)', (x, y), random.uniform(-30, 30),
                  (random.uniform(5, 8), random.uniform(5, 7), random.uniform(3.5, 6)),
                  random.choice([(.86, .80, .66), (.80, .68, .48), (.84, .76, .62)]), 'house', 0))
for nm, x, y, r, hh, col in (('hill_L', -95, 230, 75, 38, (.48, .50, .32)), ('hill_R', 120, 260, 85, 45, (.56, .44, .28)), ('hill_C', 20, 330, 60, 22, (.52, .52, .40))):
    ITEMS.append((nm, 'far', '새로 — 아트2 지형(언덕, V5 ground 텍스처)', (x, y), 0, (r * 2, r * 1.4, hh), col, 'hill', 0))
ITEMS.append(('sea', 'far', 'art/export/scene-a-v5-combo/sea_v5_combo.fbx (바다 한 조각)', (0, 500), 0, (900, 720, 0), (.36, .55, .62), 'sea', 0))
ROOF = [(.74, .42, .26), (.66, .34, .26), (.50, .54, .50), (.78, .52, .30)]

bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene; MATS = {}
camD = Vector((0, -CAMS['D'][0], CAMS['D'][1]))


def fogged(col, p):
    k = min(1, max(0, ((p - camD).length - FOG0) / (FOG1 - FOG0))) * FOGMAX
    return tuple(c * (1 - k) + f * k for c, f in zip(col, FOG))


def mat(col, tag):
    key = (tuple(round(c, 3) for c in col), tag)
    if key not in MATS:
        m = bpy.data.materials.new(f'{tag}_{len(MATS)}'); m.use_nodes = True; n = m.node_tree.nodes; n.clear()
        e = n.new('ShaderNodeEmission'); e.inputs[0].default_value = (*key[0], 1); o = n.new('ShaderNodeOutputMaterial')
        m.node_tree.links.new(e.outputs[0], o.inputs[0]); m['look'] = key[0]; m['layer'] = tag; MATS[key] = m
    return MATS[key]


def add(ob, col, layer, p):
    ob.data.materials.clear(); ob.data.materials.append(mat(fogged(col, p), layer)); ob['layer'] = layer; return ob


objs = []
for nm, layer, src, (x, y), rot, (w, d, hh), col, shape, tilt in ITEMS:
    z = h(x, y); p = Vector((x, y, z))
    if shape == 'sea':
        bpy.ops.mesh.primitive_plane_add(size=1, location=(x, y, -13)); ob = bpy.context.object; ob.scale = (w, d, 1)
        objs.append(add(ob, col, layer, Vector((x, 160, -13)))); continue
    if shape == 'hill':
        bpy.ops.mesh.primitive_uv_sphere_add(segments=10, ring_count=6, location=(x, y, -12)); ob = bpy.context.object; ob.scale = (w / 2, d / 2, hh + 12)
        objs.append(add(ob, col, layer, p)); continue
    if shape in ('cyl', 'pole'):
        bpy.ops.mesh.primitive_cylinder_add(vertices=8 if shape == 'cyl' else 6, radius=w / 2, depth=hh, location=(x, y, z + hh / 2))
    else:
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x, y, z + hh / 2)); bpy.context.object.scale = (w, d, hh)
    ob = bpy.context.object; ob.rotation_euler = (math.radians(tilt), 0, math.radians(rot)); ob.name = nm; objs.append(add(ob, col, layer, p))
    if shape == 'tree':  # 줄기 + 큰 덩이 3개
        ob.scale = (.35, .35, .5); ob.location.z = z + hh * .25
        for j, (dx, dy, dz, s) in enumerate(((0, 0, .62, .42), (-.25, .1, .48, .3), (.28, -.1, .5, .32))):
            bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=hh * s, location=(x + dx * w, y + dy * w, z + hh * dz))
            objs.append(add(bpy.context.object, col, layer, p))
        add(ob, (.36, .26, .18), layer, p)
    if shape == 'house':  # 지붕 판(두께 = 벽의 0.17, 처마 0.3)
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x, y, z + hh + hh * .17 / 2)); rf = bpy.context.object
        rf.scale = (w + hh * .6, d + hh * .6, hh * .17); rf.rotation_euler = (0, 0, math.radians(rot)); objs.append(add(rf, random.choice(ROOF), layer, p))
    if shape == 'car':
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x, y, z + hh + .3)); cb = bpy.context.object; cb.scale = (w * .9, d * .5, .6)
        cb.rotation_euler = (0, 0, math.radians(rot)); objs.append(add(cb, col, layer, p))
    if shape == 'pole':
        if nm.startswith('pole'):
            bpy.ops.mesh.primitive_cube_add(size=1, location=(x, y, z + hh - .5)); ca = bpy.context.object; ca.scale = (1.6, .12, .12)
            ca.rotation_euler = (math.radians(tilt), 0, 0); objs.append(add(ca, col, layer, p))
        else:  # 가로등 팔
            bpy.ops.mesh.primitive_cube_add(size=1, location=(x - .6, y, z + hh)); objs.append(add(bpy.context.object, (.95, .85, .55), layer, p)); bpy.context.object.scale = (1.3, .2, .15)


def top(nm):
    ob = bpy.data.objects[nm]; return ob.matrix_world @ Vector((0, 0, .5 * (1 if ob.type else 1)))


# 전선 3가닥: 정비소 지붕(카메라 뒤) → 전신주 1 → 2 → 3 → 4. 처짐 0.5~0.9m
POLES = [(-6, -14, 5.8)] + [(x, y, h(x, y) + 7.5 - .5) for nm, _, _, (x, y), *_ in ITEMS if nm.startswith('pole_')]
for k, off in enumerate((-.7, 0, .7)):
    for (ax, ay, az), (bx, by, bz) in zip(POLES, POLES[1:]):
        cu = bpy.data.curves.new(f'wire{k}', 'CURVE'); cu.dimensions = '3D'; cu.bevel_depth = .035; sp = cu.splines.new('POLY'); sp.points.add(11)
        for i in range(12):
            t = i / 11; sag = (.5 + .2 * k) * 4 * t * (1 - t)
            sp.points[i].co = (ax + (bx - ax) * t + off, ay + (by - ay) * t, az + (bz - az) * t - sag - .1 * k, 1)
        ob = bpy.data.objects.new(f'wire{k}', cu); sc.collection.objects.link(ob); ob.data.materials.append(mat((.12, .12, .13), 'mid')); ob['layer'] = 'mid'

# 땅: 격자 + 층(카메라 D 에서 0~8 / 8~40 / 40m+) + 길 리본
import bmesh
bm = bmesh.new(); S = 3; xs = range(-150, 151, S); ys = range(-18, 420, S); V = {}
for i in xs:
    for j in ys: V[i, j] = bm.verts.new((i, j, h(i, j)))
for i in xs[:-1]:
    for j in ys[:-1]: bm.faces.new((V[i, j], V[i + S, j], V[i + S, j + S], V[i, j + S]))
me = bpy.data.meshes.new('terrain'); bm.to_mesh(me); bm.free(); ter = bpy.data.objects.new('terrain', me); sc.collection.objects.link(ter)
GCOL = dict(near=(.64, .56, .42), mid=(.56, .54, .34), far=(.50, .50, .34))
for f in me.polygons:
    c = Vector(f.center); d = (c - camD).length; lay = 'near' if d < 8 else 'mid' if d < 40 else 'far'
    col = (.62, .58, .50) if (abs(c.x) < 8 and c.y < 4.5) else GCOL[lay]
    m = mat(fogged(col, c), lay + '_ground')
    if m.name not in me.materials: me.materials.append(m)
    f.material_index = list(me.materials).index(m)
pts = []
for (ax, ay), (bx, by) in zip(ROAD, ROAD[1:]):
    n = max(1, int(math.hypot(bx - ax, by - ay)))
    pts += [(ax + (bx - ax) * t / n, ay + (by - ay) * t / n) for t in range(n)]
bm = bmesh.new(); prev = None
for i, (x, y) in enumerate(pts[:-1]):
    nx, ny = pts[i + 1][0] - x, pts[i + 1][1] - y; L = math.hypot(nx, ny); sx, sy = ny / L * 2, -nx / L * 2
    a = bm.verts.new((x - sx, y - sy, h(x - sx, y - sy) + .04)); b = bm.verts.new((x + sx, y + sy, h(x + sx, y + sy) + .04))
    if prev: bm.faces.new((prev[0], prev[1], b, a))
    prev = (a, b)
me = bpy.data.meshes.new('road'); bm.to_mesh(me); bm.free(); road = bpy.data.objects.new('road', me); sc.collection.objects.link(road)
for f in me.polygons:
    c = Vector(f.center); d = (c - camD).length; lay = 'near' if d < 8 else 'mid' if d < 40 else 'far'
    m = mat(fogged((.58, .48, .36), c), lay + '_ground')
    if m.name not in me.materials: me.materials.append(m)
    f.material_index = list(me.materials).index(m)

# 카트(구운 32색 unlit)
before = set(sc.objects); bpy.ops.import_scene.fbx(filepath=KART); kart = [o for o in sc.objects if o not in before and o.type == 'MESH']
for o in kart:
    o['layer'] = 'kart'
    for s in o.material_slots:
        m = s.material; n = m.node_tree.nodes; img = next(x.image for x in n if x.type == 'TEX_IMAGE'); n.clear()
        t = n.new('ShaderNodeTexImage'); t.image = img; t.interpolation = 'Closest'; e = n.new('ShaderNodeEmission'); oo = n.new('ShaderNodeOutputMaterial')
        m.node_tree.links.new(t.outputs[0], e.inputs[0]); m.node_tree.links.new(e.outputs[0], oo.inputs[0]); m['layer'] = 'kart'; m['tex'] = 1

# 렌더 설정
sc.world = bpy.data.worlds.new('w'); sc.world.use_nodes = True; bgn = sc.world.node_tree.nodes['Background']
sc.render.engine = 'CYCLES'; sc.cycles.samples = 1; sc.cycles.use_denoising = False; sc.cycles.pixel_filter_type = 'BOX'; sc.cycles.filter_width = .01
sc.render.dither_intensity = 0; sc.view_settings.view_transform = 'Standard'; sc.render.resolution_x, sc.render.resolution_y = 1280, 720
sc.render.resolution_percentage = 100; sc.render.film_transparent = False
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam; cam.data.sensor_fit = 'VERTICAL'
cam.data.clip_end = 2000
IDC = dict(near=(1, 0, 0), near_ground=(1, 1, 0), mid=(0, 1, 0), mid_ground=(0, 1, 1), far=(1, 0, 1), far_ground=(1, 0, 1), sky=(0, 0, 1), kart=(1, 1, 1))
NAMES = ['near', 'near_ground', 'mid', 'mid_ground', 'far', 'sky', 'kart']
PAL = np.array([IDC[k] for k in NAMES], np.float32)


def set_mode(mode):
    bgn.inputs[0].default_value = (*(FOG if mode == 'look' else IDC['sky']), 1)
    for m in bpy.data.materials:
        if 'layer' not in m or 'tex' in m and mode == 'look':
            if 'tex' in m: m.node_tree.nodes['Emission'].inputs[1].default_value = 1
            continue
        n = m.node_tree.nodes; e = n['Emission']
        if 'tex' in m:  # 카트: id 에서는 흰색 한 색
            lk = m.node_tree.links
            for l in list(lk):
                if l.to_node == e and l.to_socket == e.inputs[0]: lk.remove(l)
            e.inputs[0].default_value = (1, 1, 1, 1); continue
        e.inputs[0].default_value = (*(m['look'] if mode == 'look' else IDC[m['layer']]), 1)


meas = {}
for mode in ('look', 'id'):
    set_mode(mode)
    for k, (dist, hgt, fov, ahead, th) in CAMS.items():
        cam.location = (0, -dist, hgt); cam.data.angle_y = math.radians(fov)
        cam.rotation_euler = (Vector((0, ahead, th)) - cam.location).to_track_quat('-Z', 'Y').to_euler(); bpy.context.view_layer.update()
        path = os.path.join(REV, f'{mode}_{k}.png'); sc.render.filepath = path; bpy.ops.render.render(write_still=True)
        if mode != 'id': continue
        im = bpy.data.images.load(path); a = np.array(im.pixels[:], np.float32).reshape(720, 1280, 4)[::-1, :, :3]
        cls = ((a[:, :, None, :] - PAL[None, None]) ** 2).sum(3).argmin(2)
        share = {nm: round(float((cls == i).mean()) * 100, 1) for i, nm in enumerate(NAMES)}
        rows = [np.bincount(cls[r], minlength=len(NAMES)).argmax() for r in range(720)]
        band = {}
        for r, c in enumerate(rows):
            nm = NAMES[c].replace('_ground', ''); band.setdefault(nm, [r, r]); band[nm][1] = r
        bot = cls[480:]
        vis = {}
        for o in sc.objects:
            if 'layer' not in o or o.name.startswith(('terrain', 'road', 'wire')) or o['layer'] == 'kart': continue
            v = world_to_camera_view(sc, cam, o.matrix_world.translation)
            if 0 < v.x < 1 and 0 < v.y < 1 and v.z > 0: vis.setdefault(o['layer'], set()).add(o.name.split('.')[0])
        meas[k] = dict(cam=dict(back=dist, height=hgt, vfov=fov, look_ahead=ahead, look_h=th), area_pct=share,
                       objects_pct=round(share['near'] + share['mid'] + share['far'] + share['kart'], 1),
                       row_bands_pct={nm: [round(a0 / 7.2), round((a1 + 1) / 7.2)] for nm, (a0, a1) in band.items()},
                       bottom_third_bare_ground_pct=round(float(((bot == 1) | (bot == 3)).mean()) * 100, 1),
                       visible_items={l: len(s) for l, s in vis.items()})
json.dump(dict(measure=meas, fog=dict(color=FOG, start=FOG0, end=FOG1, max=FOGMAX)), open(os.path.join(REV, 'measure.json'), 'w'), indent=1, ensure_ascii=False)
json.dump(dict(axes='Blender m, z-up. 카트 원점, +y = 마을 쪽. Unity 로는 (x,y,z) → (−x, z, −y)', road_centerline=ROAD, cams=CAMS,
               items=[dict(name=nm, layer=l, source=s, pos=[round(x, 2), round(y, 2), round(h(x, y), 2)], rot_deg=round(r, 1), size_m=list(map(lambda v: round(v, 2), sz)),
                           tilt_deg=t, shape=sh) for nm, l, s, (x, y), r, sz, c, sh, t in ITEMS]),
          open(os.path.join(REV, 'layout.json'), 'w'), indent=1, ensure_ascii=False)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(R, 'art', 'models', 'opus_scene_a2_blockout.blend'))
print('DONE', json.dumps(meas, ensure_ascii=False), flush=True)
