# check_rules.py 의 Blender 쪽 (읽기 전용): 메시 노드 x 재질 칸별 면(월드 면적·UV 삼각형·UV 부호 면적·섬 id), 섬 크기, 원통 면 수.
# 실행: blender -b --python tools/art/_rules_bpy.py -- out.json a.fbx b.glb ...
import bpy, bmesh, sys, json, math
from mathutils import Vector
argv = sys.argv[sys.argv.index('--') + 1:]
outp = argv[0]; files = argv[1:]
res = {}
def ring_sides(pts):
    best = None
    for ax in (Vector((1,0,0)), Vector((0,1,0)), Vector((0,0,1))):
        # plane basis
        u = ax.orthogonal().normalized(); w = ax.cross(u).normalized()
        P = {(round(p.dot(u), 3), round(p.dot(w), 3)) for p in pts}
        if len(P) < 5: continue
        cx = sum(a for a, b in P) / len(P); cy = sum(b for a, b in P) / len(P)
        R = [(math.hypot(a - cx, b - cy), math.atan2(b - cy, a - cx)) for a, b in P]
        mx = max(r for r, t in R)
        if mx < 0.02: continue
        outer = [(r, t) for r, t in R if r > 0.85 * mx]
        if len(outer) < 5: continue
        mr = sum(r for r, t in outer) / len(outer)
        sd = (sum((r - mr) ** 2 for r, t in outer) / len(outer)) ** .5
        if sd / mr > 0.03: continue
        ang = sorted({round(math.degrees(t)) % 360 for r, t in outer})
        # check angular regularity: max gap < 1.6 * 360/n
        n = len(ang); gaps = [(ang[(i + 1) % n] - ang[i]) % 360 for i in range(n)]
        if max(gaps) > 1.6 * 360 / n: continue
        # ring must cover most of the projected silhouette (exclude boxes with many verts)
        if len(outer) < 0.3 * len(R) and n < 8: continue
        cand = (n, round(mr, 3))
        if best is None or cand[0] > best[0]: best = cand
    return best
for f in files:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if f.endswith('.glb'): bpy.ops.import_scene.gltf(filepath=f)
    else: bpy.ops.import_scene.fbx(filepath=f)
    objs = {}
    for o in bpy.data.objects:
        if o.type != 'MESH': continue
        me = o.data; M = o.matrix_world
        bm = bmesh.new(); bm.from_mesh(me); bm.transform(M); bm.faces.ensure_lookup_table(); bm.verts.ensure_lookup_table()
        uvl = bm.loops.layers.uv.active
        # islands
        isl = {}; nid = 0
        for fc in bm.faces:
            if fc.index in isl: continue
            st = [fc]; isl[fc.index] = nid
            while st:
                c = st.pop()
                for e in c.edges:
                    for g in e.link_faces:
                        if g.index not in isl: isl[g.index] = nid; st.append(g)
            nid += 1
        ipts = {}
        for fc in bm.faces:
            ipts.setdefault(isl[fc.index], set()).update(v.index for v in fc.verts)
        idim = {}
        for i, vs in ipts.items():
            cs = [bm.verts[j].co for j in vs]
            idim[str(i)] = round(max(max(c[k] for c in cs) - min(c[k] for c in cs) for k in range(3)), 3)
        cyl = {}
        for i, vs in ipts.items():
            if len(vs) < 10: continue
            r = ring_sides([bm.verts[j].co.copy() for j in vs]) if hasattr(bm.verts, '__getitem__') else None
            if r: cyl[i] = r
        slots = {}
        for fc in bm.faces:
            si = fc.material_index
            m = me.materials[si] if si < len(me.materials) else None
            img = None
            if m and m.use_nodes:
                for n in m.node_tree.nodes:
                    if n.type == 'TEX_IMAGE' and n.image: img = n.image.name; break
            key = str(si)
            S = slots.setdefault(key, {'mat': m.name if m else None, 'img': img, 'faces': []})
            area = fc.calc_area()
            if uvl is None: continue
            uvs = [tuple(l[uvl].uv) for l in fc.loops]
            # fan triangulate
            for k in range(1, len(uvs) - 1):
                a, b, c = uvs[0], uvs[k], uvs[k + 1]
                sa = ((b[0] - a[0]) * (c[1] - a[1]) - (c[0] - a[0]) * (b[1] - a[1])) / 2
                va, vb, vc = fc.loops[0].vert.co, fc.loops[k].vert.co, fc.loops[k + 1].vert.co
                wa = ((vb - va).cross(vc - va)).length / 2
                S['faces'].append([round(wa, 6), round(sa, 9), [round(x, 6) for x in (*a, *b, *c)], isl[fc.index]])
        bb = [M @ Vector(c) for c in o.bound_box]
        dims = [round(max(v[i] for v in bb) - min(v[i] for v in bb), 3) for i in range(3)]
        objs[o.name] = {'dims': dims, 'tris': sum(len(p.vertices) - 2 for p in me.polygons), 'islands': nid, 'idim': idim,
                        'cyl': {str(k): v for k, v in cyl.items()}, 'slots': slots}
        bm.free()
    res[f] = objs
json.dump(res, open(outp, 'w'))
print('DONE', {k: len(v) for k, v in res.items()})
