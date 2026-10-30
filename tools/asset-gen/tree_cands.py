"""나무 후보 4종 — 과제 artlive 20261031-0142 (D-220 「우리 게임 규칙에 맞는 나무」 1단계: 원화 승인용 비교판).
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/tree_cands.py
종(해안 비탈 마을, D-205 남해안 + 이탈리아 해안):
  pine     곰솔(해송)   — 바닷바람에 기운 S 줄기 + 바람 아래쪽으로 몰린 납작한 솔잎 판(층). 남해안 해안 마을의 얼굴.
  celtis   팽나무       — 굵고 짧은 줄기가 2.2m 에서 셋으로 갈라져 넓게 퍼진 지붕 꼴 수관. 마을 어귀 당산나무.
  camellia 동백 관목    — 1.4m 둥근 상록 덤불, 줄기 2~3 가지가 밑에서 보임. 남해안 담장·마당 가.
  cypress  사이프러스   — 8m 좁은 불꽃 기둥. 이탈리아 해안 언덕의 실루엣(곰솔·팽나무의 넓은 꼴과 대비).
형태 규칙: 나무 ≤1,500 · 덤불 ≤500 삼각형. 잎 = 각진 큰 덩어리 몇 개(면마다 평평, 둥근 구름 X) + 알파 카드 소수(가장자리 깨기).
줄기·가지 = 16각 원통(교훈 5). 피벗 = 밑동 땅(z 0) 가운데. 낱개 = TRUNK_01 · BRANCH_nn · CANOPY_nn · CARD_nn.
UV: field_patch 와 같은 선반 쌓기, 면마다 실제 길이 × 32px/m(나무는 세상 물건). 칸 표 layout_<종>.json → tree_tex.py 가 사진으로 칠함.
출력: art/export/tree-cands/{<종>.fbx, layout_<종>.json, measure.json}
"""
import bpy, bmesh, os, sys, json, math, random
from mathutils import Vector, Matrix
R = 'D:/Github/F1Game/'; OUT = R + 'art/export/tree-cands/'; os.makedirs(OUT, exist_ok=True)

FACES = []
def face(obj, pts, pt): FACES.append((obj, [Vector(p) for p in pts], pt))
def quad_out(obj, pts, pt, ctr):
    P = [Vector(p) for p in pts]; n = (P[1] - P[0]).cross(P[2] - P[0]); c = sum(P, Vector()) / len(P)
    face(obj, pts if n.dot(c - Vector(ctr)) >= 0 else pts[::-1], pt)

def tube(obj, path, radii, pt, n=16, cap0=False, cap1=True, seed=0, wob=0.0):  # 16각 원통(줄기·가지), wob = 단면 흔들림(껍질 울퉁불퉁)
    rr = random.Random(seed); P = [Vector(p) for p in path]; rings = []
    for i, p in enumerate(P):
        a = (P[min(i + 1, len(P) - 1)] - P[max(i - 1, 0)]).normalized()
        u = Vector((1, 0, 0)) if abs(a.z) > .9 else Vector((0, 0, 1)); s = a.cross(u).normalized(); t = a.cross(s).normalized()
        ph = rr.uniform(0, 6.28)
        rings.append([p + (s * math.cos(2 * math.pi * k / n) + t * math.sin(2 * math.pi * k / n)) * radii[i] * (1 + wob * math.sin(3 * 2 * math.pi * k / n + ph)) for k in range(n)])
    for i in range(len(P) - 1):
        ctr = (P[i] + P[i + 1]) / 2
        for k in range(n): quad_out(obj, [rings[i][k], rings[i][(k + 1) % n], rings[i + 1][(k + 1) % n], rings[i + 1][k]], pt, ctr)
    if cap0: quad_out(obj, rings[0], pt, P[1])
    if cap1: quad_out(obj, rings[-1], pt, P[-2])

def lump(obj, c, rx, ry, h, pt, seed, n=8, yaw=0.0, flat=0.35, top_off=(0, 0), jag=.18, tilt=(0, 0)):
    """잎 덩어리 = 각진 다면체(면 6n개). 둘레 = 들쭉한 n각(번갈아 바깥·안 → 잎 뭉치 끝), 밑 = flat(1 = 판판한 바닥, 곰솔 판), 위 = 한쪽으로 쏠린 봉우리.
    둥근 구름(공) 대신: 둘레 반지름 ±jag + 링마다 반 칸 돌림 + 봉우리 치우침."""
    rr = random.Random(seed); c = Vector(c); cy_, sy_ = math.cos(yaw), math.sin(yaw)
    def P(x, y, z):
        z2 = z + tilt[0] * x + tilt[1] * y
        return c + Vector((x * cy_ - y * sy_, x * sy_ + y * cy_, z2))
    R0 = [1 + rr.uniform(-jag, jag) - (.14 if k % 2 else 0) for k in range(n)]
    def ring(sc, z, rot, zj=0.0):
        return [P(rx * sc * R0[k] * math.cos(2 * math.pi * (k + rot) / n), ry * sc * R0[k] * math.sin(2 * math.pi * (k + rot) / n), z + rr.uniform(-zj, zj)) for k in range(n)]
    bot = ring(.45 + .45 * flat, -h * (.32 - .2 * flat), .5, h * .04)
    mid = ring(1.0, 0, 0, h * .1)
    up = ring(.62, h * .36, .5, h * .06)
    tp = P(rx * top_off[0], ry * top_off[1], h * (.58 + rr.uniform(-.05, .05)))
    bp = P(rx * .05, ry * .05, -h * (.42 - .3 * flat))
    ctr = c + Vector((0, 0, h * .05))
    for k in range(n):
        k1 = (k + 1) % n
        quad_out(obj, [bot[k], bot[k1], bp], pt, ctr)
        quad_out(obj, [bot[k], bot[k1], mid[k1]], pt, ctr); quad_out(obj, [bot[k], mid[k1], mid[k]], pt, ctr)
        quad_out(obj, [mid[k], mid[k1], up[k]], pt, ctr); quad_out(obj, [mid[k1], up[k1], up[k]], pt, ctr)
        quad_out(obj, [up[k], up[k1], tp], pt, ctr)

def card(obj, c, w, h, yaw, pitch=0.0, pt='card'):  # 알파 카드 = 사각 한 장(Cull Off 셰이더라 양면). c = 아래 가운데
    c = Vector(c); s = Vector((math.cos(yaw), math.sin(yaw), 0)); up = Vector((0, 0, 1)) * math.cos(pitch) + Vector((-math.sin(yaw), math.cos(yaw), 0)) * math.sin(pitch)
    face(obj, [c - s * w / 2, c + s * w / 2, c + s * w / 2 + up * h, c - s * w / 2 + up * h], pt)

def bez(p0, p1, p2, k):  # 2차 곡선 점 k+1 개(줄기 휨)
    p0, p1, p2 = Vector(p0), Vector(p1), Vector(p2)
    return [(1 - t) ** 2 * p0 + 2 * (1 - t) * t * p1 + t * t * p2 for t in [i / k for i in range(k + 1)]]

# ---------------- 종별 형태 ----------------
def sp_pine():  # 곰솔: 높이 ~6.6m. 바람 = -x 쪽(바다)에서 → 줄기·판이 +x 로 기움
    rng = random.Random(11)
    path = bez((0, 0, -.15), (.15, 0, 3.0), (1.5, .2, 5.0), 5)
    path[2] += Vector((-.25, 0, 0))   # S 굽음
    tube('TRUNK_01', path, [.26, .22, .19, .16, .13, .1], 'bark', seed=1, wob=.06)
    pads = [((2.05, .3, 5.15), 1.7, 1.25, .62, 0.3), ((.55, -.6, 4.25), 1.5, 1.0, .5, -.5), ((2.4, -.5, 3.85), 1.35, .9, .48, .4),
            ((1.15, .95, 3.35), 1.25, .85, .45, 1.2), ((-.35, .45, 3.05), .95, .7, .4, 2.6), ((3.0, .55, 4.65), 1.0, .75, .4, .1)]
    for i, (pc, rx, ry, h, yaw) in enumerate(pads):
        lump('CANOPY_%02d' % (i + 1), pc, rx, ry, h, 'leaf', 100 + i, n=10 if rx > 1.3 else 8, yaw=yaw, flat=.9, top_off=(.25, 0), jag=.22, tilt=(.04, 0))
    # 가지: 줄기 → 판 밑(판 바닥 위 .1 까지 박힘)
    starts = [path[4], path[3], path[3], path[2], path[2], path[4]]
    for i, ((pc, rx, ry, h, yaw), s) in enumerate(zip(pads, starts)):
        e = Vector(pc) + Vector((0, 0, -.05)); m = (Vector(s) + e) / 2 + Vector((0, 0, -.15))
        if (e - Vector(s)).length < .35: continue
        tube('BRANCH_%02d' % (i + 1), [s, m, e], [.075, .055, .04], 'bark', n=16, cap1=False, seed=20 + i)
    # 솔잎 카드: 판 가장자리(아래·바깥)로 삐친 솔잎 뭉치
    k = 0
    for i, (pc, rx, ry, h, yaw) in enumerate(pads[:5]):
        for a in (yaw + .4, yaw + 3.3):
            k += 1; p = Vector(pc) + Vector((math.cos(a) * rx * .78, math.sin(a) * ry * .78, -h * .35))
            card('CARD_%02d' % k, p, .9, .7, a + 1.57 + rng.uniform(-.3, .3), pitch=rng.uniform(-.25, .25))

def sp_celtis():  # 팽나무: 높이 ~7.4m, 수관 폭 ~8m. 굵은 줄기 + 2.2m 에서 셋 갈림 + 넓고 낮은 지붕 수관(밑이 판판)
    rng = random.Random(22)
    tube('TRUNK_01', [(0, 0, -.15), (.05, .02, .9), (.1, -.03, 2.2)], [.48, .36, .33], 'bark', seed=2, wob=.1)
    limbs = [((.1, -.03, 2.1), (1.3, .3, 3.6), (2.6, .5, 4.6)), ((.1, -.03, 2.1), (-1.0, .9, 3.7), (-2.3, 1.4, 4.7)), ((.1, -.03, 2.1), (.1, -1.2, 3.9), (-.2, -2.3, 5.0))]
    for i, (a, b, c) in enumerate(limbs):
        tube('BRANCH_%02d' % (i + 1), bez(a, b, c, 3), [.27, .2, .15, .1], 'bark', seed=30 + i, wob=.05, cap1=False)
    subs = [((1.3, .3, 3.6), (1.5, -1.3, 4.9)), ((-1.0, .9, 3.7), (-.6, 2.4, 5.0)), ((.1, -1.2, 3.9), (1.6, -2.2, 4.8)), ((-1.0, .9, 3.7), (-2.6, -.6, 4.9))]
    for i, (a, b) in enumerate(subs):
        m = (Vector(a) + Vector(b)) / 2 + Vector((0, 0, .25))
        tube('BRANCH_%02d' % (i + 4), [a, m, b], [.12, .09, .06], 'bark', seed=40 + i, cap1=False)
    blobs = [((2.6, .5, 5.0), 2.0, 1.6, 1.5, .3), ((-2.3, 1.4, 5.1), 1.9, 1.6, 1.4, 2.0), ((-.2, -2.3, 5.3), 1.8, 1.5, 1.5, -1.3), ((1.5, -1.4, 5.3), 1.5, 1.3, 1.3, -.6),
             ((-.6, 2.4, 5.4), 1.6, 1.3, 1.3, 1.4), ((-2.6, -.7, 5.2), 1.5, 1.3, 1.2, 3.0), ((.2, .2, 6.2), 2.2, 1.9, 1.5, .7)]
    for i, (pc, rx, ry, h, yaw) in enumerate(blobs):
        lump('CANOPY_%02d' % (i + 1), pc, rx, ry, h, 'leaf', 200 + i, n=10, yaw=yaw, flat=.7, top_off=(rng.uniform(-.2, .2), rng.uniform(-.2, .2)), jag=.2)
    k = 0
    for i, (pc, rx, ry, h, yaw) in enumerate(blobs[:6]):   # 수관 아래 가장자리에 늘어진 잎 카드(밑선을 깨기)
        for a in (yaw + .9, yaw - .9):
            k += 1; p = Vector(pc) + Vector((math.cos(a) * rx * .82, math.sin(a) * ry * .82, -h * .5))
            card('CARD_%02d' % k, p, 1.2, .95, a + 1.57 + rng.uniform(-.3, .3), pitch=rng.uniform(-.2, .2))

def sp_camellia():  # 동백 관목: 높이 ~1.45m, 폭 ~1.6m. 밑에 줄기 3개 보임
    rng = random.Random(33)
    stems = [((0, 0, -.08), (.25, .1, .6)), ((.05, .05, -.08), (-.3, .15, .55)), ((0, -.05, -.08), (0, -.3, .5))]
    for i, (a, b) in enumerate(stems): tube('TRUNK_%02d' % (i + 1), [a, b], [.045, .035], 'bark', seed=50 + i, cap1=False)
    blobs = [((.25, .1, .78), .62, .55, .62, .4), ((-.33, .15, .72), .58, .52, .6, 2.1), ((0, -.33, .66), .55, .5, .55, -1.2), ((0, 0, 1.08), .55, .5, .52, 1.0)]
    for i, (pc, rx, ry, h, yaw) in enumerate(blobs):
        lump('CANOPY_%02d' % (i + 1), pc, rx, ry, h, 'leaf', 300 + i, n=8, yaw=yaw, flat=.45, top_off=(rng.uniform(-.2, .2), 0), jag=.2)
    for k, a in enumerate((.3, 1.9, 3.4, 4.9, 5.8)):
        p = Vector((math.cos(a) * .72, math.sin(a) * .66, .42 + .2 * (k % 2)))
        card('CARD_%02d' % (k + 1), p, .55, .5, a + 1.57 + rng.uniform(-.3, .3), pitch=rng.uniform(-.2, .3))

def sp_cypress():  # 사이프러스: 높이 ~8m, 폭 ~1.5m. 좁은 기둥 = 층마다 반지름·중심이 흔들리는 12각 링(뾰족한 끝)
    rng = random.Random(44)
    tube('TRUNK_01', [(0, 0, -.15), (0, 0, .75)], [.17, .14], 'bark', seed=4, cap1=False)
    lv = [(.55, .45), (.7, 1.2), (.78, 2.2), (.76, 3.3), (.7, 4.4), (.6, 5.4), (.46, 6.3), (.3, 7.1), (.12, 7.7)]
    n = 12; rings = []
    for j, (r, z) in enumerate(lv):
        ox, oy = rng.uniform(-.06, .06), rng.uniform(-.06, .06); rot = .5 * (j % 2)
        rings.append([Vector((ox + r * (1 + rng.uniform(-.12, .12) - (.1 if k % 2 else 0)) * math.cos(2 * math.pi * (k + rot) / n), oy + r * (1 + rng.uniform(-.12, .12)) * math.sin(2 * math.pi * (k + rot) / n) * .92, z + rng.uniform(-.08, .08))) for k in range(n)])
    tip = Vector((rng.uniform(-.05, .05), 0, 8.05)); base = Vector((0, 0, .35))
    for j in range(len(rings) - 1):
        ctr = Vector((0, 0, (lv[j][1] + lv[j + 1][1]) / 2))
        for k in range(n): quad_out('CANOPY_01', [rings[j][k], rings[j][(k + 1) % n], rings[j + 1][(k + 1) % n], rings[j + 1][k]], 'leaf', ctr)
    for k in range(n):
        quad_out('CANOPY_01', [rings[-1][k], rings[-1][(k + 1) % n], tip], 'leaf', Vector((0, 0, 7.5)))
        quad_out('CANOPY_01', [rings[0][k], rings[0][(k + 1) % n], base], 'leaf', Vector((0, 0, 1.0)))
    for i, (z, a) in enumerate(((2.6, .5), (4.2, 2.6), (5.6, 4.6))):   # 옆으로 부푼 뭉치(바람에 흐트러진 가지 끝)
        lump('CANOPY_%02d' % (i + 2), (math.cos(a) * .55, math.sin(a) * .5, z), .42, .34, .9, 'leaf', 400 + i, n=6, yaw=a, flat=.3, jag=.15)
    for k, (z, a) in enumerate(((1.4, .9), (2.9, 3.1), (3.8, 5.0), (4.9, 1.8), (6.0, 3.9), (6.8, .2))):
        rr_ = .75 - .06 * z
        card('CARD_%02d' % (k + 1), (math.cos(a) * rr_, math.sin(a) * rr_, z - .35), .6, 1.0, a + 1.57 + rng.uniform(-.3, .3), pitch=rng.uniform(-.15, .15))

SPECIES = {'pine': sp_pine, 'celtis': sp_celtis, 'camellia': sp_camellia, 'cypress': sp_cypress}
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
        if pt == 'card': l2 = [(0, 0), ((pts[1] - pts[0]).length, 0), ((pts[1] - pts[0]).length, (pts[3] - pts[0]).length), (0, (pts[3] - pts[0]).length)]   # 카드 = 칸 한 장 그대로(아래 = v 0)
        u0, v0 = min(a for a, _ in l2), min(b for _, b in l2); um, vm = max(a for a, _ in l2) - u0, max(b for _, b in l2) - v0
        items.append(dict(i=i, obj=obj, pt=pt, l2=[(a - u0, b - v0) for a, b in l2], m=(um, vm), w=max(2, math.ceil(um * DENS)), h=max(2, math.ceil(vm * DENS))))
    AW = 512; order = sorted(items, key=lambda t: (t['pt'], -t['h'], -t['w'])); x = y = 4; rowh = 0; grp = None
    for t in order:
        if x + t['w'] + 4 > AW or (grp is not None and t['pt'] != grp):
            x, y, rowh = 4, y + rowh + 8, 0; y = -(-y // 16) * 16
        grp = t['pt']; t['x'], t['y'] = x, y; x += t['w'] + 6; rowh = max(rowh, t['h'])
    AH = math.ceil((y + rowh + 4) / 64) * 64
    mat = bpy.data.materials.new('TREE_alb'); objs = {}
    for t in items: objs.setdefault(t['obj'], []).append((FACES[t['i']][1], [((t['x'] + a * DENS) / AW, (t['y'] + b * DENS) / AH) for a, b in t['l2']]))
    obs = []; tris = 0; per = {}
    for name, fl in objs.items():
        me = bpy.data.meshes.new(name); bm = bmesh.new(); uvl = bm.loops.layers.uv.new('UVMap'); allp = [p for f, _ in fl for p in f]
        pv = Vector(((min(p.x for p in allp) + max(p.x for p in allp)) / 2, (min(p.y for p in allp) + max(p.y for p in allp)) / 2, max(0, min(p.z for p in allp))))
        if name.startswith('TRUNK'): pv = Vector((0, 0, 0)) if name == 'TRUNK_01' else pv
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
