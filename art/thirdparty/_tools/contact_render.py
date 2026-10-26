# Blender -b -P contact_render.py -- <out_dir>
# 팩별 한 줄씩, 같은 축척(PPM px/m, 정사영)으로 렌더 + 1.7m 사람 막대. 모델은 원래 단위 그대로(축척 보정 안 함).
import bpy, sys, os, glob, json, math, re
from mathutils import Vector, Matrix
from bpy_extras.object_utils import world_to_camera_view

ROOT = 'D:/Github/F1Game/art/thirdparty'
OUT = sys.argv[sys.argv.index('--') + 1] if '--' in sys.argv else ROOT + '/_tools/rows'
PPM = 32          # px per meter (모든 줄 동일)
YAW = math.radians(-30)
PITCH = math.radians(18)
GAP = 0.6
# Poly Pizza(구글 Poly) 는 단위가 제각각 → 실물 높이로 맞춰서 보여 줌(라벨에 * 표시)
TARGET = {'Bicycle_': 1.0, 'OldBike_': 1.0, 'TelephonePole_': 8.0, 'VendingMachine_': 1.8, 'Tire_': 0.65}
os.makedirs(OUT, exist_ok=True)

def G(pack, *pats, n=99):
    out = []
    for p in pats:
        out += sorted(glob.glob(f'{ROOT}/{pack}/{p}', recursive=True))
    seen = []
    for f in out:
        if f not in seen: seen.append(f)
    return seen[:n]

ROWS = [
 ('kenney-retro-urban-kit', G('kenney-retro-urban-kit', *[f'Models/GLB format/{n}.glb' for n in
   ['wall-a-garage','wall-a-flat-garage','wall-b-door','wall-a-roof','detail-awning-wide','truck-green','truck-grey-cargo',
    'detail-light-single','detail-light-double','detail-bench','detail-dumpster-closed','pallet','detail-cables-type-a','tree-large','tree-small','tree-shrub']])),
 ('chillydurango-shacks-sheds', G('chillydurango-shacks-sheds', 'Large Shed A.blend','Large Shed B.blend','Leanto A.blend','Shack A.blend','Barrel.blend','Pallet.blend','Concrete Barricade.blend','Chainlink Fence.blend')),
 ('miziziziz-ps1-industrial-buildings', G('miziziziz-ps1-industrial-buildings','*Building.glb','Road.glb','SideWalk.glb')),
 ('chillydurango-plumbing-wiring', G('chillydurango-plumbing-wiring', 'Generator.blend','Transformer.blend','Circuit Breaker.blend','Pump.blend','Switch.blend')),
 ('valsekamerplant-urban-stacked', G('valsekamerplant-urban-stacked', 'Buildings Prebuild/Hangar1.glb','Buildings Prebuild/flat_square_brick.glb','Bus stops/busstop_clean_bench.glb',
   'Litter/Dumpsters/dumpster_diagonal_closed_blue.glb', 'Litter/Metal trashcan/metaltrashcan_closed.glb','Litter/TrashCan/trash_can_full.glb','Traffic lights/*.glb','Bus stops/busstop_bench.glb', n=12)),
 ('valsekamerplant-fences/barriers/plants', G('valsekamerplant-fences', 'Fences/metal_fence/*.glb','Fences/white_picket_fence/*.glb','Fences/low_wooden_fence/*.glb','Fences/drystone_wall/*.glb', n=6)
   + G('valsekamerplant-barriers','**/Construction_cone.glb','**/Jersey_barrier_dirty.glb','**/retro_bollard.glb')
   + G('valsekamerplant-plants-pots','**/pot.glb','**/pot3.glb','**/Bird_of_paradise_cluster_1.glb','**/sanseveria.glb')),
 ('elegantcrow-retro-house-pack', G('elegantcrow-retro-house-pack', 'models/fbx/*.fbx')),
 ('elegantcrow-modular-house-pack', G('elegantcrow-modular-house-pack', '**/wall_*.fbx', n=4) + G('elegantcrow-modular-house-pack','**/roof*.fbx', n=3)
   + G('elegantcrow-modular-house-pack','**/door_single_01.fbx','**/stair_medium_01.fbx','**/foundation.fbx')),
 ('elegantcrow-retro-nature-pack', G('elegantcrow-retro-nature-pack', 'models/FBX/**/tree0[1-8].fbx','models/FBX/**/bush0[1-4].fbx','models/FBX/**/grass0[1-3].fbx','models/FBX/**/grass_patch.fbx')),
 ('elegantcrow-tree-pack', G('elegantcrow-tree-pack', '**/models/*tree*.fbx', n=8) + G('elegantcrow-tree-pack','**/models/bush0[1-3].fbx')),
 ('ggbot-psx-style-cars', G('ggbot-psx-style-cars', 'Car 0[1-8]/*.obj','Wheel/*.obj')),
 ('ward727 / xiiixvv / polypizza', G('ward727-train-pack','fbx/electic_line.fbx','fbx/red_container.fbx','fbx/liquid_tank.fbx')
   + G('xiiixvv-ps1-barrels','*.glb') + G('polypizza-misc','*.glb')),
]

def clear():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def imp(f):
    before = set(bpy.data.objects)
    ext = f.lower().rsplit('.', 1)[1]
    try:
        if ext in ('glb', 'gltf'): bpy.ops.import_scene.gltf(filepath=f)
        elif ext == 'fbx': bpy.ops.import_scene.fbx(filepath=f)
        elif ext == 'obj': bpy.ops.wm.obj_import(filepath=f)
        elif ext == 'blend':
            with bpy.data.libraries.load(f, link=False) as (src, dst):
                dst.objects = [n for n in src.objects]
            for o in dst.objects:
                if o is not None and o.type in ('MESH', 'EMPTY'):
                    bpy.context.scene.collection.objects.link(o)
    except Exception as e:
        print('IMPORT FAIL', f, e)
    new = [o for o in bpy.data.objects if o not in before]
    for o in new:
        if o.type in ('CAMERA', 'LIGHT'): bpy.data.objects.remove(o)
    return [o for o in bpy.data.objects if o not in before]

def pngs_for(f):
    pack = f.replace('\\', '/').split('/thirdparty/')[1].split('/')[0]
    return glob.glob(f'{ROOT}/{pack}/**/*.png', recursive=True) + glob.glob(f'{ROOT}/{pack}/**/*.PNG', recursive=True)

def fix_textures(objs, f):
    stem = os.path.splitext(os.path.basename(f))[0].lower()
    cands = None
    missing = 0
    for o in objs:
        if o.type != 'MESH': continue
        for slot in o.material_slots:
            m = slot.material
            if not m: continue
            m.use_nodes = True
            nt = m.node_tree
            tex = [n for n in nt.nodes if n.type == 'TEX_IMAGE' and n.image and (n.image.has_data or os.path.exists(bpy.path.abspath(n.image.filepath)))]
            if tex:
                nt.nodes.active = tex[0]; continue
            if cands is None: cands = pngs_for(f)
            key = (m.name.lower().split('.')[0], stem, o.name.lower().split('.')[0])
            best, bs = None, 0
            for p in cands:
                b = os.path.basename(p).lower()
                s = 0
                for k in key:
                    if k and k in b: s += 3
                    s += sum(1 for t in re.split(r'[_\- ]', k) if len(t) > 2 and t in b) * 0.5
                if 'fall' in b: s += 1.5
                if 'tex1' in b: s += 1
                if 'snow' in b or 'winter' in b: s -= 3
                if s > bs: best, bs = p, s
            if best:
                img = bpy.data.images.load(best, check_existing=True)
                n = nt.nodes.new('ShaderNodeTexImage'); n.image = img; n.interpolation = 'Closest'
                bsdf = next((x for x in nt.nodes if x.type == 'BSDF_PRINCIPLED'), None)
                if bsdf: nt.links.new(n.outputs['Color'], bsdf.inputs['Base Color'])
                nt.nodes.active = n
            else:
                missing += 1
    return missing

def world_bbox(objs):
    pts = []
    for o in objs:
        if o.type != 'MESH': continue
        pts += [o.matrix_world @ Vector(c) for c in o.bound_box]
    if not pts: return None
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return mn, mx

def render_row(idx, label, files):
    clear()
    sc = bpy.context.scene
    placed = []  # (name, objs)
    x = 0.0
    # 사람 막대 1.7m
    bpy.ops.mesh.primitive_cube_add(size=1)
    bar = bpy.context.active_object; bar.name = 'HUMAN_1.7m'
    bar.scale = (0.4, 0.25, 1.7); bar.location = (0.2, 0, 0.85)
    mat = bpy.data.materials.new('bar'); mat.diffuse_color = (0.9, 0.1, 0.1, 1); bar.data.materials.append(mat)
    x = 1.2
    report = []
    for f in files:
        objs = imp(f)
        if not objs: report.append((f, 'import fail')); continue
        roots = [o for o in objs if o.parent is None]
        piv = bpy.data.objects.new('piv', None); sc.collection.objects.link(piv)
        for r in roots: r.parent = piv
        bpy.context.view_layer.update()
        bb = world_bbox(objs)
        if bb is None: report.append((f, 'no mesh')); continue
        nat = bb[1] - bb[0]
        k = 1.0
        for pre, th in TARGET.items():
            if os.path.basename(f).startswith(pre): k = th / nat.z
        if k == 1.0 and max(nat) > 60: k = 0.01
        piv.scale = (k, k, k)
        piv.rotation_euler = (0, 0, YAW)
        bpy.context.view_layer.update()
        bb = world_bbox(objs)
        mn, mx = bb
        piv.location = (x - mn.x, -(mn.y + mx.y) / 2, -mn.z)
        bpy.context.view_layer.update()
        mn, mx = world_bbox(objs)
        miss = fix_textures(objs, f)
        size = mx - mn
        placed.append((os.path.splitext(os.path.basename(f))[0] + ('' if k == 1.0 else '*'), objs, (mn, mx)))
        d = sorted([nat.x, nat.y])
        report.append((os.path.relpath(f, ROOT).replace('\\', '/'), f'native L{d[1]:.2f} W{d[0]:.2f} H{nat.z:.2f}', f'scale={k:.3g}', f'notex={miss}'))
        x = mx.x + GAP
    # 카메라
    cam_d = bpy.data.cameras.new('cam'); cam_d.type = 'ORTHO'
    cam = bpy.data.objects.new('cam', cam_d); sc.collection.objects.link(cam); sc.camera = cam
    cam.rotation_euler = (math.radians(90) - PITCH, 0, 0)
    cam.location = (0, -200, 0)
    bpy.context.view_layer.update()
    # 카메라 공간에서 전체 범위
    inv = cam.matrix_world.inverted()
    pts = []
    for o in bpy.data.objects:
        if o.type == 'MESH':
            pts += [inv @ (o.matrix_world @ Vector(c)) for c in o.bound_box]
    minx = min(p.x for p in pts) - 0.5; maxx = max(p.x for p in pts) + 0.5
    miny = min(p.y for p in pts) - 0.5; maxy = max(p.y for p in pts) + 0.5
    w, h = maxx - minx, maxy - miny
    cx, cy = (minx + maxx) / 2, (miny + maxy) / 2
    cam.location = cam.matrix_world @ Vector((cx, cy, 0))
    cam_d.ortho_scale = max(w, h)
    cam_d.clip_end = 1000
    sc.render.resolution_x = min(4200, max(64, int(round(w * PPM))))
    sc.render.resolution_y = min(1600, max(64, int(round(h * PPM))))
    sc.render.resolution_percentage = 100
    sc.render.engine = 'BLENDER_WORKBENCH'
    sh = sc.display.shading
    sh.light = 'STUDIO'; sh.color_type = 'TEXTURE'; sh.show_cavity = False; sh.show_shadows = False
    sc.render.film_transparent = False
    world = bpy.data.worlds.new('w'); sc.world = world; world.color = (0.82, 0.82, 0.80)
    sc.view_settings.view_transform = 'Standard'
    sc.render.filepath = f'{OUT}/row{idx:02d}.png'
    bpy.context.view_layer.update()
    labels = []
    for name, objs, (mn, mx) in placed:
        c = Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, mn.z))
        v = world_to_camera_view(sc, cam, c)
        labels.append((name, int(v.x * sc.render.resolution_x), int((1 - v.y) * sc.render.resolution_y)))
    vb = world_to_camera_view(sc, cam, Vector((0.2, 0, 0)))
    bpy.ops.render.render(write_still=True)
    return dict(label=label, file=sc.render.filepath, w=sc.render.resolution_x, h=sc.render.resolution_y,
                labels=labels, report=report, bar=(int(vb.x * sc.render.resolution_x), int((1 - vb.y) * sc.render.resolution_y)))

meta = []
only = os.environ.get('ROWS')
for i, (label, files) in enumerate(ROWS):
    if only and str(i) not in only.split(','): continue
    print('ROW', i, label, len(files))
    meta.append(render_row(i, label, files))
    json.dump(meta, open(f'{OUT}/meta{"_"+only.replace(",","-") if only else ""}.json', 'w'), indent=1, ensure_ascii=False)
print('DONE')
