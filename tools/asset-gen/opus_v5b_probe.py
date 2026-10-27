"""V5 정비소 실내 다시 굽기 전 조사 — 주요 물체 bbox·면 법선 (아트2, 과제 1248). 읽기만.
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_v5b_probe.py
"""
import bpy, json, os
from mathutils import Vector
R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
O = os.path.join(R, 'art', 'models', 'garage_scene_a_v5_combo')
bpy.ops.wm.open_mainfile(filepath=os.path.join(O, 'baked-lit.blend'))
man = json.load(open(os.path.join(O, 'manifest.json')))
g = [m for m in man if m['group'] == 'garage'][0]
print('ALL', g['objects'], flush=True)
for n in g['objects']:
    o = bpy.data.objects.get(n)
    if not o: print('MISSING', n); continue
    pts = [o.matrix_world @ Vector(c) for c in o.bound_box]
    lo = [round(min(p[i] for p in pts), 2) for i in range(3)]; hi = [round(max(p[i] for p in pts), 2) for i in range(3)]
    mats = [s.material.name for s in o.material_slots if s.material]
    print('B', n, lo, hi, len(o.data.polygons), mats[:2], flush=True)
for o in bpy.data.objects:
    if o.type == 'LIGHT': print('LIGHT', o.name, o.data.type, o.data.energy, tuple(o.location))
print('IMG', [(i.name, i.size[:], i.filepath) for i in bpy.data.images][:20])
