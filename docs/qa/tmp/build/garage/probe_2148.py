import bpy, math
bpy.ops.wm.open_mainfile(filepath='D:/Github/F1Game/art/source/q2-garage/garage.blend')
def img_of(m):
    return next((n.image for n in m.node_tree.nodes if n.type == 'TEX_IMAGE' and n.image), None) if m and m.use_nodes else None
for o in bpy.data.objects:
    if o.type != 'MESH' or o.get('q2_before_only') or o.hide_render: continue
    me = o.data; uvl = me.uv_layers.active.data; M = o.matrix_world
    want = o.name.startswith(('drum', 'table', 'trest', 'sign_', 'sh_', 'pdoor', 'posterC')) or o.name == 'floor'
    rows = {}
    for p in me.polygons:
        m = me.materials[p.material_index] if me.materials else None; im = img_of(m); nm = im.name if im else '-'
        if not want and 'props' not in nm: continue
        wa = sum(((M @ me.vertices[p.vertices[0]].co - M @ me.vertices[p.vertices[i]].co).cross(M @ me.vertices[p.vertices[0]].co - M @ me.vertices[p.vertices[i + 1]].co)).length / 2 for i in range(1, len(p.vertices) - 1))
        us = [uvl[i].uv for i in p.loop_indices]
        sa = abs(sum(us[i].x * us[(i + 1) % len(us)].y - us[(i + 1) % len(us)].x * us[i].y for i in range(len(us))) / 2)
        W, H = im.size if im else (1, 1)
        r = rows.setdefault(nm, [0, 0, 0, [9, 9, -9, -9]]); r[0] += wa; r[1] += sa * W * H; r[2] += 1
        b = r[3]; b[0] = min(b[0], *[u.x for u in us]); b[1] = min(b[1], *[u.y for u in us]); b[2] = max(b[2], *[u.x for u in us]); b[3] = max(b[3], *[u.y for u in us])
    for nm, (wa, px, n, b) in rows.items():
        print('P', o.name, nm, 'faces', n, 'm2', round(wa, 3), 'ppm', round(math.sqrt(px / wa), 1) if wa else 0, 'uv', [round(x, 3) for x in b], 'loc', [round(x, 2) for x in o.location], 'dim', [round(x, 2) for x in o.dimensions])
