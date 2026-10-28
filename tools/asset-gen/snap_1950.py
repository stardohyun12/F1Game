# artlive 1950: 보관소 택배가 떠 있음(도현 뷰어 코멘트 depot-…-mshy) → 모든 PARCEL 바닥을 가장 가까운 받침 면에 붙인다.
# blender -b --factory-startup -P tools/asset-gen/snap_1950.py
#  - 낮은 상자부터 차례로(쌓인 상자는 아래 상자를 먼저 내린 뒤 그 윗면에).
#  - 상자 바닥 5점(가운데 + 안쪽 25% 모서리 4)에서 아래로 레이: 시작 = 바닥 + min(높이/2, 15cm) — 파묻힌 상자도 받침 윗면을 찾는다.
#  - 받침 = 5점이 맞은 면 중 가장 높은 것(선반 판·카운터·손수레·바닥·아래 상자). 아무것도 안 맞으면 땅 z=0.
#  - 이동 = 받침 − 바닥 (틈 0, 파묻힘 0). 0.5mm 미만은 그대로. 결과 표 docs/qa/tmp/snap_1950.json.
#  입력·출력 = export fbx(덮어씀). 되돌리기 = docs/qa/tmp/q3_depot_pre1950.fbx.
import bpy, sys, os, json
from mathutils import Vector
from mathutils.bvhtree import BVHTree
R = 'D:/Github/F1Game'; FBX = R + '/art/export/q3-depot/q3_depot.fbx'
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=FBX)
dg = bpy.context.evaluated_depsgraph_get()
def wverts(o): return [o.matrix_world @ v.co for v in o.data.vertices]
def bvh(o):
    mw = o.matrix_world; return BVHTree.FromPolygons([mw @ v.co for v in o.data.vertices], [p.vertices[:] for p in o.data.polygons])
meshes = [o for o in bpy.data.objects if o.type == 'MESH' and 'decal' not in o.name.lower() and 'glass' not in o.name.lower()]
parcels = sorted([o for o in meshes if o.name.startswith('PARCEL_')], key=lambda o: min(v.z for v in wverts(o)))
rows = []
for p in parcels:
    vs = wverts(p); lo = Vector([min(v[i] for v in vs) for i in range(3)]); hi = Vector([max(v[i] for v in vs) for i in range(3)])
    z0 = min(0.5 * (hi.z - lo.z), 0.15)
    # 1950b: 3x3 점(5~95%) — 받침 판이 좁아도(접수창 턱 등) 맞게. 위를 보는 면(법선 z>0.5)만 받침 — 서로 파고든 상자의 바닥면을 받침으로 잡지 않게
    pts = [Vector((lo.x + (hi.x - lo.x) * fx, lo.y + (hi.y - lo.y) * fy)) for fx in (0.05, 0.5, 0.95) for fy in (0.05, 0.5, 0.95)]
    best, who = None, 'GROUND'
    for o in meshes:
        if o is p: continue
        ovs = wverts(o)
        if min(v.z for v in ovs) > lo.z + z0 or max(v.x for v in ovs) < lo.x or min(v.x for v in ovs) > hi.x or max(v.y for v in ovs) < lo.y or min(v.y for v in ovs) > hi.y: continue
        t = bvh(o)
        for q in pts:
            org = Vector((q.x, q.y, lo.z + z0))
            for _ in range(4):  # 아래를 보는 면에 맞으면 그 아래에서 다시 쏜다
                hit = t.ray_cast(org, Vector((0, 0, -1)))
                if hit[0] is None: break
                if hit[1].z > 0.5:
                    if best is None or hit[0].z > best: best, who = hit[0].z, o.name
                    break
                org = hit[0] - Vector((0, 0, 0.001))
    sup = best if best is not None and best > -0.05 else 0.0
    if best is None or best <= -0.05: who = 'GROUND'
    d = sup - lo.z
    if d < -0.40:  # 1950b: 40cm 넘게 떨어지면 놓인 상자가 아니라 벽에 걸린 것(x 두께 8~10cm, 벽 x≈4.4m — PARCEL_21·22) → 그대로
        who = 'skip_wall(' + who + ')'; d = 0.0
    if abs(d) >= 0.0005:
        m = p.matrix_world.copy(); m.translation.z += d; p.matrix_world = m; bpy.context.view_layer.update()
    rows.append({'parcel': p.name, 'support': who, 'bottom_before_cm': round(lo.z * 100, 1), 'move_cm': round(d * 100, 1),
                 'xy_cm': [round(lo.x * 100), round(hi.x * 100), round(lo.y * 100), round(hi.y * 100)]})
    print('[snap]', p.name, who, round(lo.z * 100, 1), '->', round(d * 100, 1)); sys.stdout.flush()
json.dump(rows, open(R + '/docs/qa/tmp/snap_1950.json', 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
bpy.ops.object.select_all(action='SELECT')
for o in bpy.context.scene.objects:
    if o.type not in ('MESH', 'EMPTY'): o.select_set(False)
bpy.ops.export_scene.fbx(filepath=FBX, use_selection=True, object_types={'MESH', 'EMPTY'}, path_mode='STRIP', mesh_smooth_type='FACE')
print('[snap] exported'); sys.stdout.flush(); os._exit(0)
