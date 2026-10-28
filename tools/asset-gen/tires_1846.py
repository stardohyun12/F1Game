# artlive 1846: 정비소 코멘트 183800-oih2 에서 도현이 옮긴 타이어 2개(p094·p095)를 export fbx 에 그대로 적용.
# 부품 = garage_v5_combo 안에서 위치(1mm)로 이어진 면 묶음 중 상자 가운데가 pivot_blender 에 가장 가까운 것. 이동 = transform_delta.pos_blender.
# ⚠ 원본 art/source/q2-garage/garage.blend 는 안 고쳤다 — 원본에서 다시 내보내면 이 이동은 사라진다.
# blender -b --factory-startup -P tools/asset-gen/tires_1846.py
import bpy, sys, os, json
from mathutils import Vector
REPO = 'D:/Github/F1Game'; FBX = REPO + '/art/export/q2-garage/garage_v5_combo.fbx'
d = [json.loads(l) for l in open(REPO + '/tools/viewer/feedback/garage.jsonl', encoding='utf-8') if l.strip()]
mv = [x for x in d if x['id'] == 'garage-20261028-183800-oih2'][0]['moved_nodes']
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=FBX)
o = bpy.data.objects['garage_v5_combo']; me = o.data; mw = o.matrix_world
par = list(range(len(me.vertices)))
def f(i):
    while par[i] != i: par[i] = par[par[i]]; i = par[i]
    return i
key = {}
for v in me.vertices:
    k = tuple(round(c * 1000) for c in (mw @ v.co))
    if k in key: par[f(v.index)] = f(key[k])
    else: key[k] = v.index
for p in me.polygons:
    for vi in p.vertices[1:]: par[f(vi)] = f(p.vertices[0])
groups = {}
for v in me.vertices: groups.setdefault(f(v.index), []).append(v.index)
cen = {r: (Vector([min((mw @ me.vertices[i].co)[c] for i in L) for c in range(3)]) + Vector([max((mw @ me.vertices[i].co)[c] for i in L) for c in range(3)])) / 2 for r, L in groups.items()}
out = []
for name, t in sorted(mv.items()):
    pv = Vector(t['pivot_blender']); r = min(cen, key=lambda r: (cen[r] - pv).length)
    dl = mw.to_3x3().inverted() @ Vector(t['pos_blender'])
    for i in groups[r]: me.vertices[i].co += dl
    out.append([name, round((cen[r] - pv).length, 3), len(groups[r]), t['pos_blender']])
me.update(); print('[tires]', out)
bpy.ops.object.select_all(action='SELECT')
for ob in bpy.context.scene.objects:
    if ob.type != 'MESH': ob.select_set(False)
bpy.ops.export_scene.fbx(filepath=FBX, use_selection=True, object_types={'MESH'}, path_mode='STRIP', mesh_smooth_type='FACE')
print('[tires] exported'); sys.stdout.flush(); os._exit(0)
