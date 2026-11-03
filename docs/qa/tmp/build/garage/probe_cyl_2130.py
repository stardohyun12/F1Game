import bpy
bpy.ops.wm.open_mainfile(filepath='D:/Github/F1Game/art/source/q2-garage/garage.blend')
for o in bpy.data.objects:
    if o.type != 'MESH' or not (o.name.startswith(('owner_chair', 'chair_', 'engine'))): continue
    me = o.data; uv = me.uv_layers.active.data if me.uv_layers.active else None
    us = [uv[i].uv for p in me.polygons for i in p.loop_indices] if uv else []
    ur = [round(min(u.x for u in us), 3), round(min(u.y for u in us), 3), round(max(u.x for u in us), 3), round(max(u.y for u in us), 3)] if us else None
    print('OBJ', o.name, 'v', len(me.vertices), 'f', len(me.polygons), 'dim', [round(d, 3) for d in o.dimensions], 'rot', [round(r, 2) for r in o.rotation_euler],
          'scale', [round(s, 2) for s in o.scale], 'mats', [m.name for m in me.materials], 'uvrect', ur, 'mods', [m.type for m in o.modifiers], 'parent', o.parent.name if o.parent else None)
