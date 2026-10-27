import bpy, bmesh, sys, os, numpy as np
src = open('D:/Github/F1Game/tools/asset-gen/opus_houses.py', encoding='utf-8').read()
pre = src[:src.index('def load(')]
__file__ = 'D:/Github/F1Game/tools/asset-gen/opus_houses.py'; exec(compile(pre, __file__, 'exec'))
for tok, (p, kind, sh) in TOK.items():
    for path, k in ((p, kind), (sh, 'shutter')):
        if not path: continue
        bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=os.path.join(MOD, path))
        for o in [o for o in bpy.data.objects if o.type == 'MESH']:
            bm = bmesh.new(); bm.from_mesh(o.data); bm.transform(o.matrix_world); bm.verts.ensure_lookup_table()
            seen = set(); thin = []; n = 0
            for v in bm.verts:
                if v.index in seen: continue
                st = [v]; isl = []; seen.add(v.index); n += 1
                while st:
                    a = st.pop(); isl.append(a.co.copy())
                    for e in a.link_edges:
                        b = e.other_vert(a)
                        if b.index not in seen: seen.add(b.index); st.append(b)
                A = np.array(isl); d = np.sort(A.max(0) - A.min(0))
                if d[1] < .09: thin.append(tuple(np.round(d, 3)))
            print('ISL', tok, k, o.name, 'islands', n, 'thin', len(thin), thin[:4])
