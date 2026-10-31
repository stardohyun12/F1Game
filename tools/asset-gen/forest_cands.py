"""빽빽한 짙은 숲 나무 후보 2종 — 과제 artlive 20261031-1926 (D-222 「빽빽한 검은 숲 느낌을 낼 나무」). 산비탈·마을 뒤를 덮는 무리용.
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/forest_cands.py
종:
  sugi    짙은 상록 침엽(삼나무·편백류) — 11m 원뿔. 들쭉한 치마(아래로 처진 끝) 8단이 겹쳐 쌓인 꼴, 밑동 .5m 만 줄기. sugi_b = 같은 꼴 변주(F2). *_lod = LOD1(4단·≤150).
  laurel  짙은 상록 활엽(후박·구실잣밤) — 9.5m 둥근 덩어리(콜리플라워). 가운데 돔 + 둘레 어깨 6 + 꼭대기 2, 밑 2.2m 까지 잎.
형태 규칙: 1그루 ≤600 삼각형(멀리 수백 그루 깔림). 줄기 16각(교훈 5). 피벗 = 밑동 땅 가운데. 낱개 = TRUNK_01 · CANOPY_nn · CARD_nn.
UV·덩어리·카드 = tree_cands.py 와 같은 방식(32px/m 선반 쌓기). 칸 표 layout_<종>.json → forest_tex.py 가 사진으로 칠함.
출력: art/export/forest-cands/{<종>.fbx, layout_<종>.json, measure.json}
"""
import bpy, bmesh, os, sys, json, math, random
from mathutils import Vector
R = 'D:/Github/F1Game/'; OUT = R + 'art/export/forest-cands/'; os.makedirs(OUT, exist_ok=True)

FACES = []
def face(obj, pts, pt): FACES.append((obj, [Vector(p) for p in pts], pt))
def quad_out(obj, pts, pt, ctr):
    P = [Vector(p) for p in pts]; n = (P[1] - P[0]).cross(P[2] - P[0]); c = sum(P, Vector()) / len(P)
    face(obj, pts if n.dot(c - Vector(ctr)) >= 0 else pts[::-1], pt)

def tube(obj, path, radii, pt, n=16, cap1=True, seed=0, wob=0.0):  # 16각 원통 (tree_cands.py 와 같음)
    rr = random.Random(seed); P = [Vector(p) for p in path]; rings = []
    for i, p in enumerate(P):
        a = (P[min(i + 1, len(P) - 1)] - P[max(i - 1, 0)]).normalized()
        u = Vector((1, 0, 0)) if abs(a.z) > .9 else Vector((0, 0, 1)); s = a.cross(u).normalized(); t = a.cross(s).normalized()
        ph = rr.uniform(0, 6.28)
        rings.append([p + (s * math.cos(2 * math.pi * k / n) + t * math.sin(2 * math.pi * k / n)) * radii[i] * (1 + wob * math.sin(3 * 2 * math.pi * k / n + ph)) for k in range(n)])
    for i in range(len(P) - 1):
        ctr = (P[i] + P[i + 1]) / 2
        for k in range(n): quad_out(obj, [rings[i][k], rings[i][(k + 1) % n], rings[i + 1][(k + 1) % n], rings[i + 1][k]], pt, ctr)
    if cap1: quad_out(obj, rings[-1], pt, P[-2])

def lump(obj, c, rx, ry, h, pt, seed, n=8, yaw=0.0, flat=0.35, top_off=(0, 0), jag=.18):  # 각진 잎 덩어리 6n 삼각형 (tree_cands.py 와 같음)
    rr = random.Random(seed); c = Vector(c); cy_, sy_ = math.cos(yaw), math.sin(yaw)
    def P(x, y, z): return c + Vector((x * cy_ - y * sy_, x * sy_ + y * cy_, z))
    R0 = [1 + rr.uniform(-jag, jag) - (.14 if k % 2 else 0) for k in range(n)]
    def ring(sc, z, rot, zj=0.0):
        return [P(rx * sc * R0[k] * math.cos(2 * math.pi * (k + rot) / n), ry * sc * R0[k] * math.sin(2 * math.pi * (k + rot) / n), z + rr.uniform(-zj, zj)) for k in range(n)]
    bot = ring(.45 + .45 * flat, -h * (.32 - .2 * flat), .5, h * .04); mid = ring(1.0, 0, 0, h * .1); up = ring(.62, h * .36, .5, h * .06)
    tp = P(rx * top_off[0], ry * top_off[1], h * (.58 + rr.uniform(-.05, .05))); bp = P(rx * .05, ry * .05, -h * (.42 - .3 * flat)); ctr = c + Vector((0, 0, h * .05))
    for k in range(n):
        k1 = (k + 1) % n
        quad_out(obj, [bot[k], bot[k1], bp], pt, ctr)
        quad_out(obj, [bot[k], bot[k1], mid[k1]], pt, ctr); quad_out(obj, [bot[k], mid[k1], mid[k]], pt, ctr)
        quad_out(obj, [mid[k], mid[k1], up[k]], pt, ctr); quad_out(obj, [mid[k1], up[k1], up[k]], pt, ctr)
        quad_out(obj, [up[k], up[k1], tp], pt, ctr)

def skirt(obj, c, r, h, pt, seed, n=9, rot=0.0):
    """침엽 한 단 = 아래로 처진 들쭉한 치마(4n 삼각형). 둘레 점이 번갈아 바깥·아래(가지 끝 처짐)/안·위 → 톱니 실루엣.
    밑면은 가운데로 오목하게 올라가 카트 높이에서 올려봐도 속이 막혀 보임."""
    rr = random.Random(seed); c = Vector(c)
    bot = []
    for k in range(n * 2):
        a = 2 * math.pi * (k + rot) / (n * 2); tip = k % 2 == 0
        rk = r * (1 + rr.uniform(-.1, .1)) * (1 if tip else .72); zk = (-h * .17 + rr.uniform(-.05, .05) * h) if tip else h * .02
        bot.append(c + Vector((rk * math.cos(a), rk * math.sin(a), zk)))
    mid = [c + Vector((r * .46 * math.cos(2 * math.pi * (k + rot + .5) / n), r * .46 * math.sin(2 * math.pi * (k + rot + .5) / n), h * .5 + rr.uniform(-.05, .05) * h)) for k in range(n)]
    tp = c + Vector((rr.uniform(-.06, .06) * r, rr.uniform(-.06, .06) * r, h)); bp = c + Vector((0, 0, h * .12)); ctr = c + Vector((0, 0, h * .3))
    for k in range(n):
        b0, b1, b2 = bot[2 * k], bot[2 * k + 1], bot[(2 * k + 2) % (2 * n)]; m0, m1 = mid[(k - 1) % n], mid[k]
        quad_out(obj, [b0, b2, bp], pt, ctr)                     # 밑면(오목) n
        quad_out(obj, [b0, b1, m1], pt, ctr); quad_out(obj, [b1, b2, m1], pt, ctr)   # 치마 2n
        quad_out(obj, [m0, m1, tp], pt, ctr)                     # 윗면 n
        quad_out(obj, [b0, m1, m0], pt, ctr)                     # 톱니 사이 메움 (b0 는 m0·m1 사이)

def card(obj, c, w, h, yaw, pitch=0.0, pt='card'):
    c = Vector(c); s = Vector((math.cos(yaw), math.sin(yaw), 0)); up = Vector((0, 0, 1)) * math.cos(pitch) + Vector((-math.sin(yaw), math.cos(yaw), 0)) * math.sin(pitch)
    face(obj, [c - s * w / 2, c + s * w / 2, c + s * w / 2 + up * h, c - s * w / 2 + up * h], pt)

def sp_sugi(vseed=0, lod=False):  # 삼나무·편백: 높이 ~11m, 폭 ~5.2m. 치마 8단(아래 넓고 위로 좁게), 단마다 중심 조금씩 흔들림 → 손으로 깎은 원뿔 아닌 들쭉한 탑
    # vseed: 0 = A(F1) · 1 = B(F2) 형태 변주(단 흔들림·돌림). lod: LOD1 = 4단 6각 치마(≤150 삼각형), 줄기·카드 없음
    rng = random.Random(61 + 100 * vseed)
    if lod:   # 멀리(수백 그루)용 단순판. 아래 단이 땅 가까이 내려와 줄기 없이도 빈틈 없음
        for i, (z, r, h) in enumerate([(.3, 2.3, 3.5), (2.7, 1.85, 3.3), (5.0, 1.3, 3.2), (7.1, .75, 3.9)]):
            skirt('CANOPY_%02d' % (i + 1), (rng.uniform(-.1, .1), rng.uniform(-.1, .1), z), r, h, 'leaf', 600 + i + 10 * vseed, n=6, rot=.5 * (i % 2) + rng.uniform(0, .3))
        return
    tube('TRUNK_01', [(0, 0, -.15), (0, 0, 1.0), (0, 0, 2.2)], [.3, .25, .2], 'bark', seed=6 + vseed, wob=.06, cap1=False)
    # artlive 2009: 무리로 심으면 아래 가지 사이로 비탈이 비쳤다 → 맨 아래 단을 .6m 내리고 아래 4단 반지름 +.3~.4m(폭 4.3 → 5.2m), 단 하나 더(7 → 8단)
    tiers = [(.9, 2.35, 2.6), (1.95, 2.2, 2.55), (3.0, 2.0, 2.5), (4.05, 1.75, 2.4), (5.1, 1.45, 2.35), (6.15, 1.1, 2.3), (7.2, .78, 2.3), (8.25, .5, 2.6)]   # (밑 z, 반지름, 높이)
    for i, (z, r, h) in enumerate(tiers):
        skirt('CANOPY_%02d' % (i + 1), (rng.uniform(-.12, .12), rng.uniform(-.12, .12), z), r, h, 'leaf', 600 + i + 10 * vseed, n=9 if i < 5 else 7, rot=.5 * (i % 2) + rng.uniform(0, .3))
    k = 0
    for i, (z, r, h) in enumerate(tiers[:6]):   # 잎 카드: 치마 끝에서 아래로 처진 가지 끝(밑선·옆선 깨기)
        a = rng.uniform(0, 6.28) + i * 2.1; k += 1
        p = Vector((math.cos(a) * r * .88, math.sin(a) * r * .88, z - h * .3))
        card('CARD_%02d' % k, p, .85, .75, a + 1.57 + rng.uniform(-.3, .3), pitch=rng.uniform(-.2, .2))

def sp_laurel():  # 후박·구실잣밤: 높이 ~9.5m, 폭 ~7.6m. 콜리플라워 덩어리 — 돔 + 어깨 6 + 꼭대기 2, 밑 2.2m 부터 잎 → 숲에서 덩어리끼리 맞물림
    rng = random.Random(72)
    # s02: s01 은 어깨 밑이 ~3.4m 라 카트 높이에서 줄기 숲(공원) 꼴 → 전부 .9m 낮춰 어깨 밑 ~2.4m, 덩어리끼리 맞물려 벽처럼
    tube('TRUNK_01', [(0, 0, -.15), (.05, .03, 1.2), (.1, 0, 2.4)], [.42, .34, .26], 'bark', seed=7, wob=.08, cap1=False)
    blobs = [((0, 0, 4.5), 2.9, 2.75, 3.6, .3, 10, .45)]
    for i in range(6):
        a = i * 1.047 + .35; rr_ = 2.45 + .25 * (i % 2)
        blobs.append(((math.cos(a) * rr_, math.sin(a) * rr_ * .92, 3.2 + .45 * (i % 3) - .2), 1.65 + rng.uniform(-.1, .15), 1.45, 2.2, a, 8, .4))
    blobs += [((.7, .4, 6.7), 1.5, 1.35, 1.9, 1.2, 7, .3), ((-.8, -.5, 6.35), 1.35, 1.25, 1.7, 2.9, 7, .3)]
    for i, (pc, rx, ry, h, yaw, nn, fl) in enumerate(blobs):
        lump('CANOPY_%02d' % (i + 1), pc, rx, ry, h, 'leaf', 700 + i, n=nn, yaw=yaw, flat=fl, top_off=(rng.uniform(-.15, .15), rng.uniform(-.15, .15)), jag=.2)
    k = 0
    for i, (pc, rx, ry, h, yaw, nn, fl) in enumerate(blobs[1:7]):   # 어깨 밑선에 늘어진 잎 카드
        k += 1; a = yaw + rng.uniform(-.4, .4); p = Vector(pc) + Vector((math.cos(a) * rx * .8, math.sin(a) * ry * .8, -h * .45))
        card('CARD_%02d' % k, p, 1.1, .9, a + 1.57 + rng.uniform(-.3, .3), pitch=rng.uniform(-.2, .2))

SPECIES = {'sugi': sp_sugi, 'sugi_b': lambda: sp_sugi(1), 'sugi_lod': lambda: sp_sugi(0, True), 'sugi_b_lod': lambda: sp_sugi(1, True), 'laurel': sp_laurel}
DENS = 32
def local2d(pts):
    nrm = Vector()
    for i in range(len(pts)): a, b = pts[i], pts[(i + 1) % len(pts)]; nrm += Vector(((a.y - b.y) * (a.z + b.z), (a.z - b.z) * (a.x + b.x), (a.x - b.x) * (a.y + b.y)))
    nrm.normalize(); u = (pts[1] - pts[0]).normalized(); v = nrm.cross(u)
    return [((p - pts[0]).dot(u), (p - pts[0]).dot(v)) for p in pts]

meas = {}
for sp, fn in SPECIES.items():
    bpy.ops.wm.read_factory_settings(use_empty=True); sc = bpy.context.scene
    FACES.clear(); fn()
    items = []
    for i, (obj, pts, pt) in enumerate(FACES):
        if len(pts) < 3 or (pts[1] - pts[0]).length < 1e-6: continue
        l2 = local2d(pts)
        if pt == 'card': l2 = [(0, 0), ((pts[1] - pts[0]).length, 0), ((pts[1] - pts[0]).length, (pts[3] - pts[0]).length), (0, (pts[3] - pts[0]).length)]
        u0, v0 = min(a for a, _ in l2), min(b for _, b in l2); um, vm = max(a for a, _ in l2) - u0, max(b for _, b in l2) - v0
        items.append(dict(i=i, obj=obj, pt=pt, l2=[(a - u0, b - v0) for a, b in l2], m=(um, vm), w=max(2, math.ceil(um * DENS)), h=max(2, math.ceil(vm * DENS))))
    AW = 1024; order = sorted(items, key=lambda t: (t['pt'], -t['h'], -t['w'])); x = y = 4; rowh = 0; grp = None
    for t in order:
        if x + t['w'] + 4 > AW or (grp is not None and t['pt'] != grp):
            x, y, rowh = 4, y + rowh + 8, 0; y = -(-y // 16) * 16
        grp = t['pt']; t['x'], t['y'] = x, y; x += t['w'] + 6; rowh = max(rowh, t['h'])
    AH = math.ceil((y + rowh + 4) / 64) * 64
    mat = bpy.data.materials.new('FOREST_alb'); objs = {}
    for t in items: objs.setdefault(t['obj'], []).append((FACES[t['i']][1], [((t['x'] + a * DENS) / AW, (t['y'] + b * DENS) / AH) for a, b in t['l2']]))
    obs = []; tris = 0; per = {}
    for name, fl in objs.items():
        me = bpy.data.meshes.new(name); bm = bmesh.new(); uvl = bm.loops.layers.uv.new('UVMap'); allp = [p for f, _ in fl for p in f]
        pv = Vector(((min(p.x for p in allp) + max(p.x for p in allp)) / 2, (min(p.y for p in allp) + max(p.y for p in allp)) / 2, max(0, min(p.z for p in allp))))
        if name == 'TRUNK_01': pv = Vector((0, 0, 0))
        for pts, uvs in fl:
            try: f = bm.faces.new([bm.verts.new(p - pv) for p in pts])
            except ValueError: continue
            for lp, uv in zip(f.loops, uvs): lp[uvl].uv = uv
        bm.to_mesh(me); bm.free(); ob = bpy.data.objects.new(name, me); ob.location = pv; sc.collection.objects.link(ob); me.materials.append(mat); obs.append(ob)
        nt = sum(len(p.vertices) - 2 for p in me.polygons); tris += nt; k_ = name.split('_')[0]; per[k_] = per.get(k_, 0) + nt
    bpy.ops.object.select_all(action='DESELECT')
    for ob in obs: ob.select_set(True)
    bpy.context.view_layer.objects.active = obs[0]
    bpy.ops.export_scene.fbx(filepath=OUT + sp + '.fbx', use_selection=True, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
                             path_mode='STRIP', embed_textures=False, mesh_smooth_type='FACE', object_types={'MESH'})
    allv = [p for _, pts, _ in FACES for p in pts]
    meas[sp] = dict(tris=tris, by_part=per, objects=len(obs), atlas=[AW, AH], height=round(max(p.z for p in allv), 2),
                    width=round(max(max(p.x for p in allv) - min(p.x for p in allv), max(p.y for p in allv) - min(p.y for p in allv)), 2))
    lay = dict(atlas=[AW, AH], cells=[dict(obj=t['obj'], pt=t['pt'], rect=[t['x'], t['y'], t['w'], t['h']], m=[round(t['m'][0], 3), round(t['m'][1], 3)], seed=t['i']) for t in items])
    json.dump(lay, open(OUT + 'layout_' + sp + '.json', 'w', encoding='utf-8'), indent=0)
json.dump(meas, open(OUT + 'measure.json', 'w', encoding='utf-8'), indent=1, ensure_ascii=False); print('MEAS', json.dumps(meas), flush=True)
sys.stdout.flush(); os._exit(0)
