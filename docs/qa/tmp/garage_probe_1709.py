# artlive 1709: dirt_v5_combo.fbx(바닥) 내용 + garage.blend 의 간판·포스터 크기와 텍셀 밀도(px/m)
import bpy, os, sys
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath='D:/Github/F1Game/art/export/q2-garage/dirt_v5_combo.fbx')
for o in bpy.context.scene.objects:
    if o.type != 'MESH': print('DIRT obj', o.type, o.name); continue
    P = [o.matrix_world @ v.co for v in o.data.vertices]
    cnt = {}
    for p in o.data.polygons: cnt[p.material_index] = cnt.get(p.material_index, 0) + 1
    print('DIRT', o.name, len(o.data.polygons), [round(min(p[i] for p in P), 2) for i in range(3)], [round(max(p[i] for p in P), 2) for i in range(3)], [(o.material_slots[i].material.name, n) for i, n in cnt.items()], [l.name for l in o.data.uv_layers])
for m in bpy.data.materials:
    imgs = [n.image.filepath for n in (m.node_tree.nodes if m.node_tree else []) if n.type == 'TEX_IMAGE' and n.image]
    print('DIRTMAT', m.name, imgs)
bpy.ops.wm.open_mainfile(filepath='D:/Github/F1Game/art/source/q2-garage/garage.blend')
for o in bpy.context.scene.objects:
    if o.type != 'MESH' or not any(k in o.name for k in ('sign', 'poster', 'record', 'slots', 'board', 'calendar', 'notice')): continue
    me = o.data; P = [o.matrix_world @ v.co for v in me.vertices]; d = [max(p[i] for p in P) - min(p[i] for p in P) for i in range(3)]
    uv = me.uv_layers.active; us = [l.uv for l in uv.data]
    du = max(u.x for u in us) - min(u.x for u in us); dv = max(u.y for u in us) - min(u.y for u in us)
    img = None
    for s in o.material_slots:
        if s.material and s.material.node_tree:
            for n in s.material.node_tree.nodes:
                if n.type == 'TEX_IMAGE' and n.image: img = n.image
    W, H = img.size if img else (0, 0); w = sorted(d)[-1]; h = sorted(d)[-2]
    print('SIGN', o.name, 'polys', len(me.polygons), 'size_m', round(w, 2), round(h, 2), 'img', img.name if img else None, (W, H), 'uv', round(du, 3), round(dv, 3), 'px/m', round(du * W / w, 1) if w else 0, round(dv * H / h, 1) if h else 0, 'hidden', o.hide_render)
sys.stdout.flush(); os._exit(0)
