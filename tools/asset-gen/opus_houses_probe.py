"""해안 집 조립 전 모듈 치수 조사 (아트2, 과제 1232). 각 파일을 빈 장면에 불러 bbox·tris·재질을 찍는다.
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_houses_probe.py
"""
import bpy, os, glob
from mathutils import Vector

TP = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', 'art', 'thirdparty'))
M = os.path.join(TP, 'elegantcrow-modular-house-pack', 'retro_modular_house', 'retro_modular_house', 'models')
FILES = [os.path.join(M, p) for p in (
    'walls/wall_1x1.fbx', 'walls/wall_2x1.fbx', 'walls/wall_3x2.fbx', 'windows/single_windows/window_single_01.fbx',
    'windows/double_windows/window_double_01.fbx', 'doors/single_doors/door_single_frame.fbx',
    'misc/window_shutters/shutters_single_open_01.fbx', 'misc/window_shutters/shutters_double_open_01.fbx',
    'railings/railing_balcony_01.fbx', 'roofs/roof_03/roof_03_middle.fbx', 'roofs/roof_03/roof_03_L.fbx', 'roofs/roof_03/roof_03_top.fbx')]
FILES += glob.glob(os.path.join(M, 'stairs', '*.fbx'))[:3] + glob.glob(os.path.join(M, 'doors', 'single_doors', '*.fbx'))[:3]
FILES += glob.glob(os.path.join(TP, 'polypizza-misc', 'Vending*.glb')) + glob.glob(os.path.join(TP, 'valsekamerplant-plants-pots', '**', '*.glb'), recursive=True)[:4]
FILES += glob.glob(os.path.join(TP, 'valsekamerplant-urban-stacked', '**', '*[Aa][Cc]*.glb'), recursive=True)[:6]
FILES += glob.glob(os.path.join(TP, 'valsekamerplant-urban-stacked', '**', '*vent*.glb'), recursive=True)[:4]
FILES += glob.glob(os.path.join(TP, 'chillydurango-shacks-sheds', '*.blend'))
for f in FILES:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    try:
        if f.endswith('.fbx'): bpy.ops.import_scene.fbx(filepath=f)
        elif f.endswith('.glb'): bpy.ops.import_scene.gltf(filepath=f)
        else:
            with bpy.data.libraries.load(f) as (src, dst): dst.objects = src.objects
            for o in dst.objects:
                if o: bpy.context.scene.collection.objects.link(o)
    except Exception as e:
        print('ERR', f, e); continue
    obs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    if not obs: print('NOMESH', f); continue
    pts = [o.matrix_world @ Vector(c) for o in obs for c in o.bound_box]
    lo = Vector([min(p[i] for p in pts) for i in range(3)]); hi = Vector([max(p[i] for p in pts) for i in range(3)])
    tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in obs)
    mats = sorted({s.material.name for o in obs for s in o.material_slots if s.material})
    imgs = sorted({n.image.name for m in bpy.data.materials if m.use_nodes for n in m.node_tree.nodes if n.type == 'TEX_IMAGE' and n.image})
    print('P', os.path.relpath(f, TP).replace('\\', '/'), '| n', len(obs), [o.name for o in obs][:5], '| lo', [round(v, 2) for v in lo], 'hi', [round(v, 2) for v in hi],
          '| tris', tris, '| mats', mats[:4], imgs[:3], flush=True)
