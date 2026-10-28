# artlive 1653: 정비소 fbx 안의 객체 목록(이름·면 수·재질·경계·UV 범위)
import bpy
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath='D:/Github/F1Game/art/export/q2-garage/garage_v5_combo.fbx')
for o in sorted(bpy.context.scene.objects, key=lambda o: o.name):
    if o.type != 'MESH': print('OBJ', o.type, o.name); continue
    me = o.data; P = [o.matrix_world @ v.co for v in me.vertices]
    lo = [round(min(p[i] for p in P), 2) for i in range(3)]; hi = [round(max(p[i] for p in P), 2) for i in range(3)]
    mats = {}
    for p in me.polygons: mats[p.material_index] = mats.get(p.material_index, 0) + 1
    ml = [(o.material_slots[i].material.name if i < len(o.material_slots) and o.material_slots[i].material else '?', n) for i, n in mats.items()]
    uv = me.uv_layers.active
    us = [d.uv for d in uv.data] if uv else []
    ur = (round(min(u.x for u in us), 3), round(max(u.x for u in us), 3), round(min(u.y for u in us), 3), round(max(u.y for u in us), 3)) if us else None
    print('MESH', o.name, 'polys', len(me.polygons), 'lo', lo, 'hi', hi, ml, 'uvs', [l.name for l in me.uv_layers], ur, 'par', o.parent.name if o.parent else None)
