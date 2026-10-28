# -*- coding: utf-8 -*-
"""build_asset.py garage 의 uv 단계 (artlive 2117).
  전등 발광 원판(재질 Garage_Lamp_Emit)은 UV 가 props 아틀라스의 한 점에 몰려 있었다(텍셀 0·UV 붕괴 100%).
  → 위에서 내려다본 평면 투영, 세상 32px/m, 전용 16×16 garage_lamp.png 가운데에. (그 텍스처는 build_asset 이 만든다)
D:/Tools/Blender/blender.exe -b --factory-startup -P tools/art/build_garage_bpy.py -- <fbx> <out.json>
"""
import bpy, bmesh, sys, json
FBX, OUT = sys.argv[sys.argv.index('--') + 1:][:2]
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=FBX)
LAMP_PX = 32; PPM = 64; log = {}  # 작은 물건 64px/m(교훈 4) — 원판 지름 0.44m = 28px
for o in [o for o in bpy.data.objects if o.type == 'MESH']:
    slots = [i for i, s in enumerate(o.material_slots) if s.material and s.material.name.startswith('Garage_Lamp_Emit')]
    if not slots: continue
    bm = bmesh.new(); bm.from_mesh(o.data); uv = bm.loops.layers.uv.active; M = o.matrix_world
    fs = [f for f in bm.faces if f.material_index in slots]
    ws = [(M @ l.vert.co) for f in fs for l in f.loops]
    cx = (min(w.x for w in ws) + max(w.x for w in ws)) / 2; cy = (min(w.y for w in ws) + max(w.y for w in ws)) / 2
    for f in fs:
        for l in f.loops:
            w = M @ l.vert.co; l[uv].uv = (0.5 + (w.x - cx) * PPM / LAMP_PX, 0.5 + (w.y - cy) * PPM / LAMP_PX)
    bm.to_mesh(o.data); bm.free()
    log[o.name] = dict(faces=len(fs), span_m=round(max(max(w.x for w in ws) - min(w.x for w in ws), max(w.y for w in ws) - min(w.y for w in ws)), 3))
for o in bpy.data.objects: o.select_set(True)
bpy.ops.export_scene.fbx(filepath=FBX, use_selection=True, object_types={'MESH', 'EMPTY'}, path_mode='STRIP', mesh_smooth_type='FACE')
json.dump(log, open(OUT, 'w', encoding='utf-8'), indent=1); print('GARAGE UV', log)
