"""원경 지붕 무리 후보 건물 재기 — 크기·tris·물체 수 (아트2, 과제 0525).
    D:/Tools/Blender/blender.exe -b --factory-startup --python tools/asset-gen/opus_scene_a2_far_probe.py
"""
import bpy, json, os
from mathutils import Vector

R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')); TP = os.path.join(R, 'art', 'thirdparty')
SRC = ['elegantcrow-retro-house-pack/models/fbx/house%d.fbx' % i for i in range(1, 6)] + \
      ['chillydurango-shacks-sheds/%s.blend' % n for n in ('Shack A', 'Large Shed A', 'Large Shed B', 'Leanto A')] + \
      ['valsekamerplant-urban-stacked/Buildings Prebuild/%s.glb' % n for n in ('flat_square_concrete', 'flat_square_brick', 'flat_Lshape', 'Hangar1')]


def load(rel):
    bpy.ops.wm.read_factory_settings(use_empty=True); p = os.path.join(TP, rel)
    if p.endswith('.fbx'): bpy.ops.import_scene.fbx(filepath=p)
    elif p.endswith('.glb'): bpy.ops.import_scene.gltf(filepath=p)
    else:
        with bpy.data.libraries.load(p) as (a, b): b.objects = a.objects
        for o in b.objects:
            if o: bpy.context.scene.collection.objects.link(o)
    bpy.context.view_layer.update()
    return [o for o in bpy.context.scene.objects if o.type == 'MESH']


out = {}
for rel in SRC:
    obs = load(rel); P = [o.matrix_world @ v.co for o in obs for v in o.data.vertices]
    lo = [min(p[k] for p in P) for k in range(3)]; hi = [max(p[k] for p in P) for k in range(3)]
    tris = sum(len(p.vertices) - 2 for o in obs for p in o.data.polygons)
    out[rel] = dict(objs=len(obs), names=[o.name for o in obs][:12], tris=tris, size=[round(hi[k] - lo[k], 2) for k in range(3)], lo=[round(v, 2) for v in lo],
                    mats=sorted({s.material.name for o in obs for s in o.material_slots if s.material})[:10])
print('PROBE', json.dumps(out, ensure_ascii=False, indent=0), flush=True)
