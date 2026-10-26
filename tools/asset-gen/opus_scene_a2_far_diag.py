"""원경 바탕 건물이 굽기에서 검게 나오는 이유 확인 — 면 방향(안/밖)·겹친 면·물체 이름 (아트2, 과제 0525 진단용)."""
import bpy, os
from mathutils import Vector
R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')); TP = os.path.join(R, 'art', 'thirdparty')
for rel in ('valsekamerplant-urban-stacked/Buildings Prebuild/flat_square_concrete.glb', 'elegantcrow-retro-house-pack/models/fbx/house2.fbx'):
    bpy.ops.wm.read_factory_settings(use_empty=True); p = os.path.join(TP, rel)
    (bpy.ops.import_scene.fbx if p.endswith('.fbx') else bpy.ops.import_scene.gltf)(filepath=p); bpy.context.view_layer.update()
    for o in [o for o in bpy.context.scene.objects if o.type == 'MESH']:
        M = o.matrix_world; vs = [M @ v.co for v in o.data.vertices]; c = sum(vs, Vector()) / len(vs)
        inward = sum(1 for f in o.data.polygons if ((M @ f.center) - c).dot(M.to_3x3() @ f.normal) < 0)
        print('DIAG', rel.split('/')[-1], o.name, 'faces', len(o.data.polygons), 'inward', inward, 'scale', tuple(round(s, 3) for s in o.scale),
              'neg_det', M.determinant() < 0, flush=True)
