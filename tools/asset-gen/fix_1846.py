# artlive 1846: 도현 뷰어 코멘트 1/2 — 겹친 면(z-fighting) 자동 검사·띄우기, 데칼 붙이기, 우체국 창 구멍+유리.
# blender -b --factory-startup -P tools/asset-gen/fix_1846.py -- <room|depot|postoffice> <check|fix>
# 검사: 같은 평면(법선 같은 방향, 거리 4mm 안)에 있는 서로 다른 덩어리(위치 1mm 로 이어진 면 묶음)의 삼각형이 1cm² 넘게 겹치면 1건.
# 고침: 둘 중 넓이가 작은 덩어리를 법선 쪽으로 1cm 띄운다. 같은 덩어리 안에서 겹친 삼각형은 뒤의 것을 지운다. 0 건이 될 때까지 최대 5회.
# ⚠ fix 는 export fbx 를 덮어쓴다 — 다시 돌릴 땐 먼저 git checkout -- <fbx> (아니면 두 번 띄워짐)
import bpy, bmesh, sys, os, json, math
from mathutils import Vector
from mathutils.bvhtree import BVHTree
B, MODE = sys.argv[sys.argv.index('--') + 1:][:2]
# 2230: 세 번째 인자 both = 마주 보는 면(법선 반대)도 센다 — 안쪽 카메라에서 보면 벽 뒷면·받침 뒷면이 앞면이 된다(양면 재질·실내 캠). 같은 덩어리 안 양면(지붕·잎)은 뺌
BOTH = 'both' in sys.argv[sys.argv.index('--') + 1:]
REPO = 'D:/Github/F1Game'
ENT = {e['name']: e for e in json.load(open(REPO + '/tools/viewer/buildings.json', encoding='utf-8'))}[B]
FBX = REPO + '/' + ENT['fbx']
LOG = {'building': B, 'passes': []}

def log(*a):
    print('[1846]', *a); sys.stdout.flush()

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX)
OBJS = [o for o in bpy.context.scene.objects if o.type == 'MESH']
log('objs', [(o.name, tuple(round(v, 3) for v in o.matrix_world.to_euler()), tuple(round(v, 3) for v in o.matrix_world.to_scale())) for o in OBJS])

def islands(me):
    par = list(range(len(me.vertices)))
    def f(i):
        while par[i] != i:
            par[i] = par[par[i]]; i = par[i]
        return i
    key = {}
    for v in me.vertices:
        k = tuple(round(c * 1000) for c in v.co)
        if k in key: par[f(v.index)] = f(key[k])
        else: key[k] = v.index
    for p in me.polygons:
        r = f(p.vertices[0])
        for vi in p.vertices[1:]: par[f(vi)] = r
    return [f(i) for i in range(len(me.vertices))]

def tris_world():
    out = []  # (obj, poly index, island id, [3 world pts], normal, area)
    isl_area = {}
    for o in OBJS:
        me = o.data; me.calc_loop_triangles(); root = islands(me); mw = o.matrix_world; nm = mw.to_3x3().inverted().transposed()
        o['_root'] = root
        for t in me.loop_triangles:
            P = [mw @ me.vertices[i].co for i in t.vertices]
            n = (P[1] - P[0]).cross(P[2] - P[0]); a = n.length / 2
            if a < 1e-6: continue
            n.normalize(); iid = (o.name, root[t.vertices[0]])
            isl_area[iid] = isl_area.get(iid, 0) + a
            out.append((o, t.polygon_index, iid, P, n, a))
    return out, isl_area

def clip(poly, a, b, n):
    res = []
    for i in range(len(poly)):
        p, q = poly[i], poly[(i + 1) % len(poly)]
        sp = (b - a).cross(p - a).dot(n); sq = (b - a).cross(q - a).dot(n)
        if sp >= 0: res.append(p)
        if (sp >= 0) != (sq >= 0): res.append(p + (q - p) * (sp / (sp - sq)))
    return res

def overlap(P, Q, n):
    poly = [p - n * n.dot(p - Q[0]) for p in P]
    for i in range(3):
        if len(poly) < 3: return 0
        poly = clip(poly, Q[i], Q[(i + 1) % 3], n)
    if len(poly) < 3: return 0
    s = Vector((0, 0, 0))
    for i in range(1, len(poly) - 1): s += (poly[i] - poly[0]).cross(poly[i + 1] - poly[0])
    return s.length / 2

def find():
    T, area = tris_world(); bk = {}
    for k, t in enumerate(T):
        n = t[4]
        if BOTH and (n.x, n.y, n.z) < (0, 0, 0): n = -n  # 반대 법선도 같은 통에
        d = n.dot(t[3][0])
        bk.setdefault((round(n.x * 40), round(n.y * 40), round(n.z * 40), round(d / 0.004)), []).append(k)
    pairs = {}; dup = []
    for key, L in bk.items():
        M = list(L)
        for dd in (-1, 1): M += bk.get(key[:3] + (key[3] + dd,), [])
        for i in L:
            ti = T[i]
            for j in M:
                if j <= i and j in L: continue
                tj = T[j]
                dt = ti[4].dot(tj[4])
                if (abs(dt) if BOTH else dt) < 0.999 or abs(ti[4].dot(tj[3][0] - ti[3][0])) > 0.004: continue
                if dt < 0 and ti[2] == tj[2]: continue  # 같은 덩어리의 일부러 만든 양면
                lo = [min(p[c] for p in ti[3]) for c in range(3)]; hi = [max(p[c] for p in ti[3]) for c in range(3)]
                if any(max(p[c] for p in tj[3]) < lo[c] - 1e-4 or min(p[c] for p in tj[3]) > hi[c] + 1e-4 for c in range(3)): continue
                ov = overlap(ti[3], tj[3], ti[4])
                if ov < 1e-4: continue
                if ti[2] == tj[2]:
                    if ti[1] != tj[1]: dup.append((ti[0].name, max(ti[1], tj[1])))
                    continue
                k2 = tuple(sorted([ti[2], tj[2]]))
                e = pairs.setdefault(k2, [0, ti[4], 0]); e[0] += ov; e[2] += 1
    return pairs, area, dup

MOVED = {}  # 덩어리별 띄운 횟수 — 늘 작은 쪽(데칼·띠)이 위로 오게, 한 덩어리는 최대 3cm 까지만

def lift(pairs, area, dup):
    moved = []
    for (ia, ib), (ov, n, cnt) in sorted(pairs.items(), key=lambda kv: -kv[1][0]):
        small = ia if area[ia] <= area[ib] else ib
        if MOVED.get(small, 0) >= 3 or small in [tuple(m[:2]) for m in moved]: continue  # 낀 판은 남은 목록으로 · 한 회에 한 번만
        MOVED[small] = MOVED.get(small, 0) + 1
        o = bpy.data.objects[small[0]]; root = o['_root']
        dl = o.matrix_world.to_3x3().inverted() @ (n * 0.01)
        for v in o.data.vertices:
            if root[v.index] == small[1]: v.co += dl
        moved.append([small[0], small[1], round(ov * 1e4, 1), cnt])
    by = {}
    for on, pi in dup: by.setdefault(on, set()).add(pi)
    for on, S in by.items():
        bm = bmesh.new(); bm.from_mesh(bpy.data.objects[on].data); bm.faces.ensure_lookup_table()
        bmesh.ops.delete(bm, geom=[bm.faces[i] for i in S], context='FACES'); bm.to_mesh(bpy.data.objects[on].data); bm.free()
    for o in OBJS: o.data.update()
    return moved

def report(pairs, area, dup):
    return {'pairs': len(pairs), 'same_island_dups': len(dup),
            'list': [[a[0], a[1], b[0], b[1], round(v[0] * 1e4, 1), v[2], [round(c, 2) for c in v[1]]] for (a, b), v in sorted(pairs.items(), key=lambda kv: -kv[1][0])][:60]}

pairs, area, dup = find(); r = report(pairs, area, dup); LOG['before'] = r
log('BEFORE pairs', r['pairs'], 'dups', r['same_island_dups']); [log('  ', x) for x in r['list'][:40]]

if MODE == 'fix':
    for it in range(5):
        if not pairs and not dup: break
        mv = lift(pairs, area, dup); LOG['passes'].append(mv)
        pairs, area, dup = find(); log('pass', it, 'moved', len(mv), '-> pairs', len(pairs), 'dups', len(dup))
    LOG['after'] = report(pairs, area, dup)

    def others_bvh(skip):
        V, F = [], []
        for o in OBJS:
            if o.name in skip: continue
            mw = o.matrix_world; b = len(V); V += [mw @ v.co for v in o.data.vertices]; F += [[b + i for i in p.vertices] for p in o.data.polygons]
        return BVHTree.FromPolygons(V, F)

    if B == 'depot' and 'Q3_decals' in bpy.data.objects:  # 공중부양 데칼: 각 데칼 덩어리를 뒤쪽 면까지 1cm 로
        o = bpy.data.objects['Q3_decals']; me = o.data; root = islands(me); mw = o.matrix_world; tree = others_bvh({'Q3_decals'}); snaps = []
        for rid in sorted(set(root[p.vertices[0]] for p in me.polygons)):
            ps = [p for p in me.polygons if root[p.vertices[0]] == rid]
            c = sum((mw @ p.center for p in ps), Vector()) / len(ps)
            n = sum(((mw.to_3x3().inverted().transposed() @ p.normal) * p.area for p in ps), Vector()).normalized()
            hit = tree.ray_cast(c + n * 0.002, -n, 1.0)
            if hit[0] is None: snaps.append([rid, None]); continue
            gap = hit[3] - 0.002; d = gap - 0.01
            if abs(d) < 0.002: snaps.append([rid, round(gap, 3), 'ok']); continue
            dl = mw.to_3x3().inverted() @ (-n * d)
            for v in me.vertices:
                if root[v.index] == rid: v.co += dl
            snaps.append([rid, round(gap, 3), '->0.01'])
        me.update(); LOG['decals'] = snaps; log('decals', snaps)

    if B == 'postoffice' and 'Q4_window_glass_1' not in bpy.data.objects:  # 2230: 생성기가 유리를 만들면(build_asset.py postoffice) 건너뜀. 창: 코멘트 184350-vpwt 의 부품 중심 근처 덩어리에서 어두운 유리 칸 → 벽까지 구멍 + 얇은 유리
        fb = [json.loads(l) for l in open(REPO + '/tools/viewer/feedback/postoffice.jsonl', encoding='utf-8') if l.strip()]
        d = [x for x in fb if x['id'] == 'postoffice-20261028-184350-vpwt'][0]
        cents = [Vector(nd['extras']['center_blender_m']) for nd in d['nodes'] if (nd.get('extras') or {}).get('center_blender_m')]
        img = bpy.data.images.load(REPO + '/' + ENT['tex_dir'] + '/trim.png'); W, H = img.size; px = img.pixels[:]
        body = bpy.data.objects[ENT['split_loose'][0]]; me = body.data; root = islands(me); mw = body.matrix_world
        uvl = me.uv_layers.active.data; glass_mat = bpy.data.materials.new('Glass'); body.data.materials.append(glass_mat)
        trim_i = [i for i, m in enumerate(me.materials) if m and m.name.startswith('Shared_trim')]
        wins = []
        for c in cents:
            best = min(me.polygons, key=lambda p: ((mw @ p.center) - c).length); rid = root[best.vertices[0]]
            dark = []
            for p in me.polygons:
                if root[p.vertices[0]] != rid or p.material_index not in trim_i: continue
                u = sum((uvl[li].uv for li in p.loop_indices), Vector((0, 0))) / p.loop_total
                x, y = int(u.x % 1 * W), int(u.y % 1 * H); k = (y * W + x) * 4
                if 0.2126 * px[k] + 0.7152 * px[k + 1] + 0.0722 * px[k + 2] < 0.33: dark.append(p.index)  # 유리 칸 (58,73,73) ≈ 0.27, 창틀 초록은 더 밝다
            if not dark: wins.append([round(c.x, 2), round(c.y, 2), round(c.z, 2), 'no dark faces']); continue
            P = [mw @ me.vertices[vi].co for i in dark for vi in me.polygons[i].vertices]
            n = (mw.to_3x3().inverted().transposed() @ me.polygons[dark[0]].normal).normalized()
            lo = Vector([min(p[i] for p in P) for i in range(3)]); hi = Vector([max(p[i] for p in P) for i in range(3)])
            wins.append([[round(v, 3) for v in lo], [round(v, 3) for v in hi], [round(v, 2) for v in n], len(dark)])
            # 구멍: 유리 칸 크기의 상자(앞 2cm ~ 뒤 40cm)로 몸통을 뺀다
            ctr = (lo + hi) / 2 - n * 0.19; size = (hi - lo); size += Vector([abs(n[i]) * 0.42 - 0.002 for i in range(3)])
            bpy.ops.mesh.primitive_cube_add(size=1, location=ctr); cut = bpy.context.active_object; cut.scale = size
            bpy.ops.object.transform_apply(scale=True)
            m = body.modifiers.new('cut', 'BOOLEAN'); m.operation = 'DIFFERENCE'; m.object = cut; m.solver = 'EXACT'
            bpy.context.view_layer.objects.active = body; bpy.ops.object.modifier_apply(modifier='cut'); bpy.data.objects.remove(cut)
            me = body.data; root = islands(me); uvl = me.uv_layers.active.data
            # 유리: 원래 유리 칸 면에서 1cm 안쪽, 두께 없는 판(양면은 Unity 재질에서)
            gname = 'Q4_window_glass_%d' % len(wins)
            a = [i for i in range(3) if abs(n[i]) < 0.5]
            vs = []
            for s, t in ((0, 0), (1, 0), (1, 1), (0, 1)):
                v = (lo + hi) / 2 - n * 0.01; v[a[0]] = lo[a[0]] if s == 0 else hi[a[0]]; v[a[1]] = lo[a[1]] if t == 0 else hi[a[1]]; vs.append(v)
            gm = bpy.data.meshes.new(gname); gm.from_pydata(vs, [], [[0, 1, 2, 3]]); gm.update()
            if gm.polygons[0].normal.dot(n) < 0: gm.polygons[0].flip()
            gm.uv_layers.new(); gm.materials.append(glass_mat)
            g = bpy.data.objects.new(gname, gm); bpy.context.scene.collection.objects.link(g); OBJS.append(g)
        LOG['windows'] = wins; log('windows', wins)

    out = FBX
    bpy.ops.object.select_all(action='SELECT')
    for o in bpy.context.scene.objects:
        if o.type not in ('MESH', 'EMPTY'): o.select_set(False)  # 2230: 빈 객체(STUDIO_CamIn 등)도 — 옛 판은 메시만 내보내 실내 캠이 사라졌다
    bpy.ops.export_scene.fbx(filepath=out, use_selection=True, object_types={'MESH', 'EMPTY'}, path_mode='STRIP', mesh_smooth_type='FACE')
    log('exported', out)

json.dump(LOG, open(REPO + '/docs/qa/tmp/fix_1846_%s_%s%s.json' % (B, MODE, '_both' if BOTH else ''), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
sys.stdout.flush(); os._exit(0)
