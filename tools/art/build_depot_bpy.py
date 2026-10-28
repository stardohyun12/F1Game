# build_asset.py 의 Blender 단계(보관소). blender -b --factory-startup -P tools/art/build_depot_bpy.py -- <fbx> <names|uv> <out.json>
#  names: 벽에 붙은 얇은 PARCEL(두께 <12cm, x>4.2m) = 옷걸이 작업복 → 벌마다 합쳐 JACKET_nn
#  uv:    ① 원통 면 수 <16 인 섬을 16면으로 다시 만든다(교훈 12)
#         ② 면마다 텍셀 밀도를 기대값(소품 섬 ≤1.5m = 64, 그 외 32)에 맞춘다 — 점으로 뭉친 UV(<4px/m)는 면 평면 투영으로 펴고,
#            ±25% 밖이면 UV 가운데 기준으로 늘리고/줄인다. 결과는 칸(layout.json) 안으로 옮김(넘치면 줄임).
#         ③ 택배 테이프(PARCEL 의 wall_cream 칸 면)를 칸 위쪽 띠(행 8~)로 옮긴다 → 텍스처 단계가 아래쪽(건물 벽)만 회청으로 칠하고 위 띠는 크림으로 둔다(D-196).
import bpy, bmesh, sys, os, json, math, re
from mathutils import Vector, Matrix
FBX, MODE, OUT = sys.argv[sys.argv.index('--') + 1:][:3]
R = 'D:/Github/F1Game'
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=FBX)
MESH = [o for o in bpy.data.objects if o.type == 'MESH']
LOG = {'mode': MODE}
def bbox(o):
    vs = [o.matrix_world @ v.co for v in o.data.vertices]
    return Vector([min(v[i] for v in vs) for i in range(3)]), Vector([max(v[i] for v in vs) for i in range(3)])

if MODE == 'names':
    wall = []
    for o in MESH:
        if not o.name.startswith('PARCEL_'): continue
        lo, hi = bbox(o)
        if hi.x - lo.x < 0.12 and (lo.x + hi.x) / 2 > 4.2: wall.append((o, (hi - lo)))
    # 원본 확인(assemble.py:114-116): 이건 게시물·우편함이 아니라 옷걸이에 걸린 작업복 2벌(몸통 + 소매 2) → 벌마다 하나로 합쳐 JACKET_nn
    # 두 벌이 겹쳐 걸려 있다(몸통 y 1.52·1.98, 폭 0.44 / 안쪽 소매 1.77·1.73 이 엇갈림) → y 가까운 것끼리 묶으면 2/3/1 로 틀림(2109).
    # 몸통(y 폭 >0.3) 을 벌의 기준으로 두고, 소매는 「몸통 가운데 ±0.25」(assemble.py:116 dy) 에 가장 맞는 몸통에 붙인다.
    cyf = lambda o: (bbox(o)[0].y + bbox(o)[1].y) / 2
    bodies = sorted([o for o, d in wall if d.y > 0.3], key=cyf); sets, ren = [[cyf(b), [b]] for b in bodies], []
    for o, d in wall:
        if d.y > 0.3 or not sets: continue
        min(sets, key=lambda s: abs(abs(cyf(o) - s[0]) - 0.25))[1].append(o)
    for i, (cy, objs) in enumerate(sets):
        olds = [o.name for o in objs]; bpy.ops.object.select_all(action='DESELECT')
        for o in objs: o.select_set(True)
        bpy.context.view_layer.objects.active = objs[0]
        if len(objs) > 1: bpy.ops.object.join()
        j = bpy.context.view_layer.objects.active; j.name = j.data.name = 'JACKET_%02d' % (i + 1); ren.append([olds, j.name])
    LOG['renamed'] = ren
    # 간판: 나무 틀(trim 면)을 Q3_sign_frame 으로 떼어 낸다 — 글씨 검사는 글씨판(icons 면)만 봐야 한다(틀의 나뭇결을 글씨 AA 로 셈, 2058)
    sg = bpy.data.objects.get('Q3_sign')
    if sg:
        fr = sg.copy(); fr.data = sg.data.copy(); fr.name = fr.data.name = 'Q3_sign_frame'
        for col in sg.users_collection: col.objects.link(fr)
        for ob, keep_trim in ((sg, False), (fr, True)):
            bm = bmesh.new(); bm.from_mesh(ob.data)
            dead = [f for f in bm.faces if ('trim' in (ob.data.materials[f.material_index].name.lower() if ob.data.materials[f.material_index] else '')) != keep_trim]
            bmesh.ops.delete(bm, geom=dead, context='FACES'); bm.to_mesh(ob.data); bm.free()
        LOG['split_sign'] = [len(sg.data.polygons), len(fr.data.polygons)]

PROP = re.compile(r'^(PARCEL|SHELF|COUNTER|CART|TABLE|RACK|PROP|JACKET)_\d+$|^CLOCK')
LAY = json.load(open(R + '/art/export/q3-depot/layout.json', encoding='utf-8'))
def cells(mat):
    for k in ('wall', 'trim', 'floor', 'roof'):
        if mat == 'Shared_' + k: return 512, [c['rect_px'] for c in LAY[k]]
    return None, None  # 시계판(clock_dial)은 원본 UV 그대로. 바늘은 아래에서 trim 으로 옮긴다
import numpy as np
def cell_rgb(k):  # 칸별 평균색(칸을 옮길 때 가장 비슷한 색 칸을 고르려고)
    im = bpy.data.images.load(R + '/art/export/q3-depot/%s.png' % k); w, h = im.size
    a = np.array(im.pixels[:]).reshape(h, w, 4)[::-1, :, :3] * 255  # 아틀라스 = 512, 칸 px 그대로
    return [a[y:y + ch, x:x + cw].reshape(-1, 3).mean(0) for x, y, cw, ch in [c['rect_px'] for c in LAY[k]]]
INK = np.array([58, 46, 40])  # icons_shared DARK = 시계판 바늘 색

def islands(bm):
    isl = {}; n = 0
    for f in bm.faces:
        if f.index in isl: continue
        st = [f]; isl[f.index] = n
        while st:
            c = st.pop()
            for e in c.edges:
                for g in e.link_faces:
                    if g.index not in isl: isl[g.index] = n; st.append(g)
        n += 1
    return isl, n

def prism(vs):  # 축 정렬 n각 기둥이면 (축, 가운데, 반지름, 높이, n)
    for ax in range(3):
        a, b = [k for k in range(3) if k != ax]
        P = {(round(v[a], 3), round(v[b], 3)) for v in vs}; Z = sorted({round(v[ax], 3) for v in vs})
        if not (5 <= len(P) < 16) or len(Z) != 2: continue
        cx = sum(p[0] for p in P) / len(P); cy = sum(p[1] for p in P) / len(P)
        rr = [math.hypot(p[0] - cx, p[1] - cy) for p in P]; r = sum(rr) / len(rr)
        if r < 0.005 or max(abs(x - r) for x in rr) > 0.03 * r: continue
        c = [0, 0, 0]; c[ax] = (Z[0] + Z[1]) / 2; c[a] = cx; c[b] = cy
        return ax, Vector(c), r, Z[1] - Z[0], len(P)
    return None

if MODE == 'uv':
    cyl_log, fix, moved = [], {'collapsed': 0, 'rescaled': 0, 'clamped': 0, 'tape': 0, 'rotated': 0, 'moved': 0, 'hand_trim': 0}, {}
    RGB = {'Shared_' + k: cell_rgb(k) for k in ('wall', 'trim', 'floor', 'roof')}
    CN = {'Shared_' + k: [c['name'] for c in LAY[k]] for k in ('wall', 'trim', 'floor', 'roof')}
    tr_rows = []  # 건물이 wall_cream 칸에서 쓰는 행(위에서) — 텍스처 단계 경계
    for o in MESH:
        me = o.data; M = o.matrix_world
        if not me.uv_layers: continue
        prop = bool(PROP.search(o.name))
        # ① 원통
        if prop:
            bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table(); uvl = bm.loops.layers.uv.active
            isl, n = islands(bm); groups = {}
            for f in bm.faces: groups.setdefault(isl[f.index], []).append(f)
            changed = False
            for i, fs in groups.items():
                vs = list({v for f in fs for v in f.verts}); pr = prism([v.co for v in vs])
                if not pr: continue
                ax, c, r, h, nside = pr; mi = fs[0].material_index
                uv0 = sum((l[uvl].uv for f in fs for l in f.loops), Vector((0, 0))) / sum(len(f.loops) for f in fs)
                bmesh.ops.delete(bm, geom=vs, context='VERTS')
                rot = {0: Matrix.Rotation(math.pi / 2, 4, 'Y'), 1: Matrix.Rotation(-math.pi / 2, 4, 'X'), 2: Matrix()}[ax]
                res = bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=16, radius1=r, radius2=r, depth=h, matrix=Matrix.Translation(c) @ rot)
                for f in {f for v in res['verts'] for f in v.link_faces}:
                    f.material_index = mi
                    for l in f.loops: l[uvl].uv = uv0  # 아래 ②가 평면 투영으로 편다
                cyl_log.append([o.name, nside, 16, round(r, 3), round(h, 3)]); changed = True
            if changed: bm.normal_update(); bm.to_mesh(me)
            bm.free()
        # 시계 바늘: 8x3 잉크 칸은 바늘(0.2m×64=13px)이 안 들어가 늘 줄어들었다 → trim 의 잉크에 가장 가까운 칸으로(2058)
        hand = o.name.startswith('CLOCK_hand')  # door_handle 은 아님
        if hand:
            for i, m in enumerate(me.materials):
                if m and m.name.lower().startswith('clock_dial'): me.materials[i] = bpy.data.materials['Shared_trim']; fix['hand_trim'] += 1
        # ②③ 텍셀
        bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table(); uvl = bm.loops.layers.uv.active
        isl, n = islands(bm); idim = {}
        for f in bm.faces:
            lo, hi = idim.get(isl[f.index], (Vector((9e9,) * 3), Vector((-9e9,) * 3)))
            for v in f.verts:
                w = M @ v.co; lo = Vector([min(lo[k], w[k]) for k in range(3)]); hi = Vector([max(hi[k], w[k]) for k in range(3)])
            idim[isl[f.index]] = (lo, hi)
        for f in bm.faces:
            mat = me.materials[f.material_index].name if f.material_index < len(me.materials) and me.materials[f.material_index] else ''
            S, CL = cells(mat)
            if S is None: continue
            lo, hi = idim[isl[f.index]]
            tgt = 64 if prop and max(hi - lo) <= 1.5 else 32
            W = [M @ v.co for v in f.verts]; uv = [Vector(l[uvl].uv) for l in f.loops]
            wa = sum(((W[0] - W[k]).cross(W[0] - W[k + 1])).length / 2 for k in range(1, len(W) - 1))
            if wa < 1e-6: continue
            sa = sum(uv[k].x * uv[(k + 1) % len(uv)].y - uv[(k + 1) % len(uv)].x * uv[k].y for k in range(len(uv))) / 2
            dd = math.sqrt(abs(sa) * S * S / wa); c0 = sum(uv, Vector((0, 0))) / len(uv)
            if dd < 4:  # 점 UV → 면 평면 투영(긴 변 = U)
                nrm = (W[1] - W[0]).cross(W[2] - W[0]).normalized()
                e = max(((W[(k + 1) % len(W)] - W[k]) for k in range(len(W))), key=lambda x: x.length).normalized(); v2 = nrm.cross(e)
                p = [Vector(((w - W[0]).dot(e), (w - W[0]).dot(v2))) * tgt / S for w in W]
                pc = sum(p, Vector((0, 0))) / len(p); uv = [c0 + q - pc for q in p]; fix['collapsed'] += 1
            elif abs(dd / tgt - 1) > 0.25:
                s = tgt / dd; uv = [c0 + (q - c0) * s for q in uv]; fix['rescaled'] += 1
            # 칸 안으로(px, 위에서 행)
            px = [(q.x * S, (1 - q.y) * S) for q in uv]; cx = sum(a for a, b in px) / len(px); cy = sum(b for a, b in px) / len(px)
            cell = min(CL, key=lambda r: 0 if (r[0] <= cx < r[0] + r[2] and r[1] <= cy < r[1] + r[3]) else 1 + min(abs(cx - (r[0] + r[2] / 2)), abs(cy - (r[1] + r[3] / 2))))
            g = 0.5; bw = max(a for a, b in px) - min(a for a, b in px); bh = max(b for a, b in px) - min(b for a, b in px)
            tape = o.name.startswith('PARCEL_') and mat == 'Shared_wall' and cell == [256, 0, 256, 256]
            fits = lambda r, w_, h_: w_ <= r[2] - 2 * g and h_ <= r[3] - 2 * g
            if not tape and (hand or not fits(cell, bw, bh)):
                # 칸에 안 들어가면 줄이지 않는다(줄이면 텍셀이 깨짐): ㉮ 같은 칸에서 90° 돌려 보고 ㉯ 들어가는 칸 중 평균색이 가장 가까운 칸으로
                ref = INK if hand else RGB[mat][CL.index(cell)]
                cand = [(r, rot) for r in CL for rot in (False, True) if fits(r, *((bh, bw) if rot else (bw, bh)))]
                if cand:
                    r, rot = min(cand, key=lambda t: (0 if (t[0] == cell and not hand) else 1, float(np.abs(RGB[mat][CL.index(t[0])] - ref).sum()), t[1]))
                    if rot: px = [(cx - (b - cy), cy + (a - cx)) for a, b in px]; bw, bh = bh, bw; fix['rotated'] += 1
                    if r != cell:
                        fix['moved'] += 1; key = f"{o.name}:{CN[mat][CL.index(cell)] if not hand else 'clock_dial'}→{CN[mat][CL.index(r)]}"; moved[key] = moved.get(key, 0) + 1
                    cell = r
            x0, y0, cw, ch = cell
            if tape: y0, ch = 4, 52; fix['tape'] += 1  # ③ 테이프 = 위 크림 띠(건물 천장이 행 60~ 를 씀)
            k = min(1.0, (cw - 2 * g) / max(bw, 1e-6), (ch - 2 * g) / max(bh, 1e-6))
            if k < 1: px = [(cx + (a - cx) * k, cy + (b - cy) * k) for a, b in px]; fix['clamped'] += 1
            mx0 = min(a for a, b in px); mx1 = max(a for a, b in px); my0 = min(b for a, b in px); my1 = max(b for a, b in px)
            dx = max(x0 + g - mx0, 0) - max(mx1 - (x0 + cw - g), 0); dy = max(y0 + g - my0, 0) - max(my1 - (y0 + ch - g), 0)
            px = [(a + dx, b + dy) for a, b in px]
            for l, (a, b) in zip(f.loops, px): l[uvl].uv = (a / S, 1 - b / S)
            if mat == 'Shared_wall' and cell == [256, 0, 256, 256] and o.name.startswith('Q3_'): tr_rows.append(min(b for a, b in px))
        bm.to_mesh(me); bm.free()
    LOG.update(cyl=cyl_log, fix=fix, moved=moved,cream_building_top_row=min(tr_rows) if tr_rows else None)

json.dump(LOG, open(OUT, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
bpy.ops.object.select_all(action='SELECT')
for o in bpy.context.scene.objects:
    if o.type not in ('MESH', 'EMPTY'): o.select_set(False)
bpy.ops.export_scene.fbx(filepath=FBX, use_selection=True, object_types={'MESH', 'EMPTY'}, path_mode='STRIP', mesh_smooth_type='FACE')
print('[build_depot_bpy]', MODE, json.dumps({k: v for k, v in LOG.items() if k != 'cyl'}, ensure_ascii=False)[:400]); sys.stdout.flush(); os._exit(0)
