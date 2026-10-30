"""배경 집 유형 후보 H1~H4 — artlive 20261031-0205 (D-217·D-220, 원화 승인용). house_home 의 친척: 같은 면·칸·텍셀 규칙, 같은 사진 칠(house_types_tex.py).
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/house_types.py
형태 4(형태 하나에 A·B 텍스처 둘):
  H1 단독 2층 박공(박공 끝이 길 쪽, 굴뚝, 작은 발코니 1) · H2 붙은 연립 3칸(칸마다 바닥·처마 높이·벽색 다름, 비탈 따라 계단)
  H3 좁은 3층(폭 4.2m, 박공 끝이 길 쪽, 2층 발코니·빨래) · H4 가게주택(1층 가게 = 반쯤 내린 셔터·줄무늬 차양·빈 간판, 2층 집 = 발코니·빨래)
반려 이력(D-211 아파트·1판 시트)을 피함: 4층 상자 X · 격자 창 X(집마다 창 2~4개, 크기·높이 불규칙) · 평지붕 X(전부 기와 박공) · 흰 벽 X(살구·황토·크림 회벽 + 돌 굽).
축: 정면 = Blender -Y → Unity +Z. 피벗 = 본체 바닥 앞면 가운데(docs/map/building-types.md 계약). 창 = 15cm 들어간 어두운 판(실내 없음).
텍셀: 껍데기 32px/m, 낱개(PROP 이름) 긴 변 ≤1.5m 면 64. 겹친 면 0: 붙는 면 skip, 덧댄 판 2~4cm 띄움.
출력: art/export/house-types/{H1..H4}.fbx + layout_H*.json + site.fbx(비탈 단 3개, 돌 옹벽) + measure.json
"""
import bpy, bmesh, os, json, math, sys
from mathutils import Vector
R = 'D:/Github/F1Game/'; OUT = R + 'art/export/house-types/'; os.makedirs(OUT, exist_ok=True)
PROP = ('CRATE_', 'SHUTTER_', 'PLANTER_', 'PLANT_', 'RAIL_', 'DOOR_', 'LAUNDRY_')

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    global FACES, SHELL; FACES = []
def dens_for(obj, pts):
    if not obj.startswith(PROP): return 32
    L = max((pts[i] - pts[i - 1]).length for i in range(len(pts)))
    return 64 if L <= 1.5 else 32
def face(obj, pts, pt):
    pts = [Vector(p) for p in pts]; FACES.append((obj, pts, pt, dens_for(obj, pts)))
def qf(obj, pts, pt, hint):
    P = [Vector(p) for p in pts]; n = (P[1] - P[0]).cross(P[2] - P[0])
    face(obj, pts if n.dot(Vector(hint)) > 0 else pts[::-1], pt)
def box(obj, x0, x1, y0, y1, z0, z1, pt, skip=()):
    P = pt if isinstance(pt, dict) else {'*': pt}; g = lambda k: P.get(k, P['*'])
    S = {'front': [(x0, y0, z0), (x1, y0, z0), (x1, y0, z1), (x0, y0, z1)], 'back': [(x1, y1, z0), (x0, y1, z0), (x0, y1, z1), (x1, y1, z1)],
         'left': [(x0, y1, z0), (x0, y0, z0), (x0, y0, z1), (x0, y1, z1)], 'right': [(x1, y0, z0), (x1, y1, z0), (x1, y1, z1), (x1, y0, z1)],
         'top': [(x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)], 'bottom': [(x0, y1, z0), (x1, y1, z0), (x1, y0, z0), (x0, y0, z0)]}
    for k, p in S.items():
        if k not in skip: face(obj, p, g(k))

# ---------- 벽 한 면의 좌표틀: 바깥에서 본 왼→오른 = u, 바깥 = n, 위 = z. P(s, z, t) = o + u·s + n·t ----------
class Side:
    def __init__(s, o, u, n): s.o, s.u, s.n = Vector(o), Vector(u), Vector(n)
    def P(s, a, z, t=0.): v = s.o + s.u * a + s.n * t; return (v.x, v.y, z)
    def quad(s, obj, a0, a1, z0, z1, t, pt): face(obj, [s.P(a0, z0, t), s.P(a1, z0, t), s.P(a1, z1, t), s.P(a0, z1, t)], pt)   # n 쪽을 봄
    def lbox(s, obj, a0, a1, z0, z1, t0, t1, pt, skip=()):  # 면 키 out(t1)·in(t0)·l(a0)·r(a1)·top·bot
        P = pt if isinstance(pt, dict) else {'*': pt}; g = lambda k: P.get(k, P['*'])
        if 'out' not in skip: s.quad(obj, a0, a1, z0, z1, t1, g('out'))
        if 'in' not in skip: face(obj, [s.P(a1, z0, t0), s.P(a0, z0, t0), s.P(a0, z1, t0), s.P(a1, z1, t0)], g('in'))
        if 'l' not in skip: qf(obj, [s.P(a0, z0, t0), s.P(a0, z0, t1), s.P(a0, z1, t1), s.P(a0, z1, t0)], g('l'), -s.u)
        if 'r' not in skip: qf(obj, [s.P(a1, z0, t0), s.P(a1, z0, t1), s.P(a1, z1, t1), s.P(a1, z1, t0)], g('r'), s.u)
        if 'top' not in skip: qf(obj, [s.P(a0, z1, t0), s.P(a1, z1, t0), s.P(a1, z1, t1), s.P(a0, z1, t1)], g('top'), (0, 0, 1))
        if 'bot' not in skip: qf(obj, [s.P(a0, z0, t0), s.P(a1, z0, t0), s.P(a1, z0, t1), s.P(a0, z0, t1)], g('bot'), (0, 0, -1))
def sides(x0, x1, y0, y1):
    return {'F': (Side((x0, y0, 0), (1, 0, 0), (0, -1, 0)), x1 - x0), 'B': (Side((x1, y1, 0), (-1, 0, 0), (0, 1, 0)), x1 - x0),
            'L': (Side((x0, y1, 0), (0, -1, 0), (-1, 0, 0)), y1 - y0), 'R': (Side((x1, y0, 0), (0, 1, 0), (1, 0, 0)), y1 - y0)}

def wall(S, L, z0, z1, holes, pt, plinth=None, sh=None):
    """구멍 뚫린 벽 한 장: 높이 띠마다 구멍 사이 토막. holes = [(a0, a1, z0, z1)]. plinth = 돌 굽 윗선(그 아래 'stone')."""
    obj = sh or SHELL
    zs = sorted({z0, z1} | {h[2] for h in holes if z0 < h[2] < z1} | {h[3] for h in holes if z0 < h[3] < z1} | ({plinth} if plinth and z0 < plinth < z1 else set()))
    for za, zb in zip(zs, zs[1:]):
        p = 'stone' if plinth and zb <= plinth + 1e-6 else pt
        xs = [0.]
        for a0, a1, h0, h1 in sorted(holes):
            if h0 <= za + 1e-6 and zb <= h1 + 1e-6: (S.quad(obj, xs[-1], a0, za, zb, 0, p) if a0 > xs[-1] + 1e-6 else None); xs.append(a1)
        if L > xs[-1] + 1e-6: S.quad(obj, xs[-1], L, za, zb, 0, p)

K = {}
def nm(base): K[base] = K.get(base, 0) + 1; return '%s_%02d' % (base, K[base])
def reveal(S, a0, a1, z0, z1, d, skip=()):  # 벽 구멍 안쪽 면(벽 두께) — 법선 = 구멍 가운데 쪽
    Q = {'l': ([(a0, z0, 0), (a0, z0, -d), (a0, z1, -d), (a0, z1, 0)], S.u), 'r': ([(a1, z0, 0), (a1, z0, -d), (a1, z1, -d), (a1, z1, 0)], -S.u),
         'top': ([(a0, z1, 0), (a1, z1, 0), (a1, z1, -d), (a0, z1, -d)], Vector((0, 0, -1))), 'bot': ([(a0, z0, 0), (a1, z0, 0), (a1, z0, -d), (a0, z0, -d)], Vector((0, 0, 1)))}
    for k, (q, h) in Q.items():
        if k not in skip: qf(SHELL, [S.P(*p) for p in q], 'trim', h)
def window(S, a0, a1, z0, z1, shut=None, box_=False, sill=True, deep=.15):
    """창: 벽 두께 흰 면 4 + 어두운 판(pane, 십자 살은 칠) + 돋운 흰 틀(머리·창턱·옆) [+ 활짝 연 덧창 2 / 창턱 꽃 상자]."""
    reveal(S, a0, a1, z0, z1, deep)
    S.quad(SHELL, a0, a1, z0, z1, -deep, 'pane')
    S.lbox(SHELL, a0 - .1, a0, z0, z1, 0, .02, 'trim', ('in', 'top', 'bot')); S.lbox(SHELL, a1, a1 + .1, z0, z1, 0, .02, 'trim', ('in', 'top', 'bot'))
    S.lbox(SHELL, a0 - .1, a1 + .1, z1, z1 + .1, 0, .02, 'trim', ('in',))
    if sill: S.lbox(SHELL, a0 - .14, a1 + .14, z0 - .07, z0, 0, .06, 'trim', ('in',))
    if shut:
        hw = (a1 - a0) / 2
        for b0, b1 in ((a0 - .12 - hw, a0 - .12), (a1 + .12, a1 + .12 + hw)): S.lbox(nm('SHUTTER'), b0, b1, z0, z1, .04, .07, 'shutter', ('in',))
    if box_:  # 창 아래 꽃 상자: 벽에 붙음(붙는 면 skip), 윗면 = 창턱 밑 2cm
        zt = z0 - .09; c = nm('PLANTER'); S.lbox(c, a0 - .02, a1 + .02, zt - .18, zt, 0, .2, {'*': 'terracotta', 'top': 'soil'}, ('in',))
        p = nm('PLANT'); w = (a1 - a0) / 4
        for i, h in enumerate((.18, .26, .2)):
            m = a0 + w * (i + .6); S.lbox(p, m - .1, m + .1, zt, zt + h, .05, .15, 'leaf', ('bot',))
            S.lbox(p, m - .03, m + .03, zt + h - .09, zt + h - .03, .15, .17, 'flower', ('in',))
def door(S, a0, a1, z1, step=True, zf=0.):
    reveal(S, a0, a1, zf, z1, .15, ('bot',))
    S.lbox(nm('DOOR'), a0 + .01, a1 - .01, zf + .005, z1 - .01, -.13, -.09, 'door', ('in', 'bot'))
    d = 'DOOR_%02d' % K['DOOR']; S.lbox(d, a1 - .16, a1 - .11, zf + 1.0, zf + 1.06, -.09, -.05, 'brass', ('in',))
    S.lbox(SHELL, a0 - .1, a0, zf, z1, 0, .02, 'trim', ('in', 'top', 'bot')); S.lbox(SHELL, a1, a1 + .1, zf, z1, 0, .02, 'trim', ('in', 'top', 'bot'))
    S.lbox(SHELL, a0 - .1, a1 + .1, z1, z1 + .1, 0, .02, 'trim', ('in',))
    if step: S.lbox(SHELL, a0 - .25, a1 + .25, zf, zf + .12, 0, .45, 'stone', ('in', 'bot'))
def balcony(S, a0, a1, z, dep=.7, rail=True):
    """작은 발코니: 돌판 12cm + 까치발 2 + 쇠 난간(기둥·손잡이·살 12cm)."""
    S.lbox(SHELL, a0, a1, z - .12, z, 0, dep, {'*': 'trim', 'top': 'pavers'}, ('in',))
    for c in (a0 + .2, a1 - .2): S.lbox(SHELL, c - .06, c + .06, z - .45, z - .12, 0, dep * .7, 'trim', ('in', 'top'))
    if not rail: return
    r = nm('RAIL'); zt = z + .95; t0, t1 = dep - .06, dep - .02
    for c in (a0 + .03, a1 - .03): S.lbox(r, c - .025, c + .025, z, zt, t0, t1, 'iron', ('bot', 'top'))
    for c in (a0 + .03, a1 - .03): S.lbox(r, c - .025, c + .025, z, zt, .02, .06, 'iron', ('bot', 'top'))
    S.lbox(r, a0, a1, zt, zt + .04, t0, t1, 'iron')
    for c in (a0 + .005, a1 - .055): S.lbox(r, c, c + .05, zt, zt + .04, .02, t0, 'iron', ('out',))
    k = a0 + .14
    while k < a1 - .1: S.lbox(r, k - .01, k + .01, z, zt, t0 + .01, t1 - .01, 'iron', ('bot', 'top')); k += .12
def laundry(S, a0, a1, z, t, poles=None):
    """빨래: 줄(쇠 1.5cm) + 옷 3~4(천 2cm, 줄 밑에 매달림 — 윗면 = 줄 밑면 skip). poles = 받침 높이(발코니 바닥)면 양끝 쇠 기둥."""
    l = nm('LAUNDRY'); S.lbox(l, a0, a1, z, z + .015, t - .008, t + .008, 'iron', ('l', 'r') if poles is not None else ())
    if poles is not None:
        for c in (a0 - .02, a1 + .02): S.lbox(l, c - .02, c + .02, poles, z + .1, t - .02, t + .02, 'iron', ('bot',))
    x = a0 + .15
    for i, (w, h) in enumerate(((.42, .55), (.3, .38), (.5, .7), (.28, .32))):
        if x + w > a1 - .1: break
        S.lbox(l, x, x + w, z - h, z, t - .01, t + .01, 'cloth' + 'ABCD'[i], ('top',)); x += w + .12
def acunit(S, a, z, t0=0.):
    S.lbox(SHELL, a - .4, a + .4, z, z + .55, t0 + .04, t0 + .32, {'*': 'acbody', 'out': 'acface'}, ('in',))
    for c in (a - .3, a + .3): S.lbox(SHELL, c - .02, c + .02, z - .06, z, t0, t0 + .34, 'iron', ('in',))
def pots(S, a, n=2, t=.3):
    for i in range(n):
        c = a + i * .38; h = .28 - i * .06; p = nm('PLANTER'); S.lbox(p, c - .14, c + .14, 0, h, t, t + .26, {'*': 'terracotta', 'top': 'soil'}, ('bot', 'in'))
        q = nm('PLANT'); S.lbox(q, c - .12, c + .12, h, h + .34 - i * .08, t + .05, t + .21, 'leaf', ('bot',))
def pipe(S, a, z1):
    S.lbox(SHELL, a - .045, a + .045, .05, z1, .03, .12, 'fascia', ('in',))

def gable_roof(x0, x1, y0, y1, ze, pitch, axis, ov=.35, ovs=.25, rt=.09, pt='roof', tri=(True, True), ovr=None, wp='wall'):
    """박공 지붕(기와) + 박공 삼각 벽. axis = 용마루 방향('x' = 처마가 앞뒤, 'y' = 박공 끝이 앞). tri = 삼각 벽 (왼·오른) 그림 여부, ovr = 오른 옆 처마(연립 칸 사이 0)."""
    P = math.tan(math.radians(pitch))
    if axis == 'x':
        cy, hy = (y0 + y1) / 2, (y1 - y0) / 2; cx = (x0 + x1) / 2
        zu = lambda y: ze + P * (hy - abs(y - cy)); YE = hy + ov; XL, XR = x0 - ovs, x1 + (ovs if ovr is None else ovr)
        for s in (-1, 1):
            ye = cy + s * YE; a, b = (XL, XR) if s < 0 else (XR, XL)
            face(SHELL, [(a, ye, zu(ye) + rt), (b, ye, zu(ye) + rt), (b, cy, zu(cy) + rt), (a, cy, zu(cy) + rt)], pt)
            face(SHELL, [(b, ye, zu(ye)), (a, ye, zu(ye)), (a, cy, zu(cy)), (b, cy, zu(cy))], 'soffit')
            face(SHELL, [(a, ye, zu(ye)), (b, ye, zu(ye)), (b, ye, zu(ye) + rt), (a, ye, zu(ye) + rt)], 'fascia')
            for x in ((XL, XR) if ovr != 0 else (XL,)): qf(SHELL, [(x, ye, zu(ye)), (x, cy, zu(cy)), (x, cy, zu(cy) + rt), (x, ye, zu(ye) + rt)], 'fascia', (x - cx, 0, 0))
        if tri[0]: face(SHELL, [(x0, y1, ze), (x0, y0, ze), (x0, cy, zu(cy))], wp)
        if tri[1]: face(SHELL, [(x1, y0, ze), (x1, y1, ze), (x1, cy, zu(cy))], wp)
        box(SHELL, XL - .03, XR + .03, cy - .11, cy + .11, zu(cy) + rt - .06, zu(cy) + rt + .07, 'ridge', ('bottom',))
        return zu(cy) + rt + .07
    cx, hx = (x0 + x1) / 2, (x1 - x0) / 2
    zu = lambda x: ze + P * (hx - abs(x - cx)); XE, YE = hx + ov, (y1 - y0) / 2 + ovs; cy = (y0 + y1) / 2
    for s in (-1, 1):
        xe = cx + s * XE; a, b = (cy + YE, cy - YE) if s < 0 else (cy - YE, cy + YE)
        face(SHELL, [(xe, a, zu(xe) + rt), (xe, b, zu(xe) + rt), (cx, b, zu(cx) + rt), (cx, a, zu(cx) + rt)], pt)
        face(SHELL, [(xe, b, zu(xe)), (xe, a, zu(xe)), (cx, a, zu(cx)), (cx, b, zu(cx))], 'soffit')
        qf(SHELL, [(xe, a, zu(xe)), (xe, b, zu(xe)), (xe, b, zu(xe) + rt), (xe, a, zu(xe) + rt)], 'fascia', (s, 0, 0))
        for y in (cy - YE, cy + YE): qf(SHELL, [(xe, y, zu(xe)), (cx, y, zu(cx)), (cx, y, zu(cx) + rt), (xe, y, zu(xe) + rt)], 'fascia', (0, y - cy, 0))
    face(SHELL, [(x0, y0, ze), (x1, y0, ze), (cx, y0, zu(cx))], 'wall'); face(SHELL, [(x1, y1, ze), (x0, y1, ze), (cx, y1, zu(cx))], 'wall')
    box(SHELL, cx - .11, cx + .11, cy - YE - .03, cy + YE + .03, zu(cx) + rt - .06, zu(cx) + rt + .07, 'ridge', ('bottom',))
    return zu(cx) + rt + .07

def export(name, extra=None):
    AW = 1024; items = []
    def local2d(pts):
        nrm = Vector()
        for i in range(len(pts)): a, b = pts[i], pts[(i + 1) % len(pts)]; nrm += Vector(((a.y - b.y) * (a.z + b.z), (a.z - b.z) * (a.x + b.x), (a.x - b.x) * (a.y + b.y)))
        nrm.normalize(); u = (pts[1] - pts[0]).normalized(); v = nrm.cross(u)
        return [((p - pts[0]).dot(u), (p - pts[0]).dot(v)) for p in pts]
    for i, (obj, pts, pt, d) in enumerate(FACES):
        l2 = local2d(pts); u0, v0 = min(a for a, _ in l2), min(b for _, b in l2); um, vm = max(a for a, _ in l2) - u0, max(b for _, b in l2) - v0
        items.append(dict(i=i, obj=obj, pt=pt, dens=d, l2=[(a - u0, b - v0) for a, b in l2], m=(um, vm), w=max(2, math.ceil(um * d)), h=max(2, math.ceil(vm * d))))
    order = sorted(items, key=lambda t: (t['obj'] != SHELL, t['obj'] if t['obj'] != SHELL else '', t['pt'] if t['obj'] != SHELL else '', -t['h'], -t['w'])); x = y = 4; rowh = 0; grp = None
    for t in order:
        g = (t['obj'], t['pt']) if t['obj'] != SHELL else None
        if x + t['w'] + 4 > AW or (g != grp and g is not None and x > 4 and t['obj'] != (grp or ('',))[0]):
            x, y, rowh = 4, y + rowh + 8, 0
            if g is not None: y = -(-y // 16) * 16
        grp = g; t['x'], t['y'] = x, y; x += t['w'] + 8; rowh = max(rowh, t['h'])
    AH = math.ceil((y + rowh + 4) / 128) * 128
    objs = {}
    for t in items:
        obj, pts = FACES[t['i']][0], FACES[t['i']][1]
        objs.setdefault(obj, []).append((pts, [((t['x'] + .5 + a * t['dens']) / AW, (t['y'] + .5 + b * t['dens']) / AH) for a, b in t['l2']]))
    mat = bpy.data.materials.new(name + '_alb'); sc = bpy.context.scene; obs = []; tris = 0
    for on, fl in objs.items():
        me = bpy.data.meshes.new(on); bm = bmesh.new(); uvl = bm.loops.layers.uv.new('UVMap')
        allp = [p for pts, _ in fl for p in pts]
        pv = Vector() if on == SHELL else Vector(((min(p.x for p in allp) + max(p.x for p in allp)) / 2, (min(p.y for p in allp) + max(p.y for p in allp)) / 2, min(p.z for p in allp)))
        for pts, uvs in fl:
            f = bm.faces.new([bm.verts.new(p - pv) for p in pts])
            for lp, uv in zip(f.loops, uvs): lp[uvl].uv = uv
        bm.to_mesh(me); bm.free(); ob = bpy.data.objects.new(on, me); ob.location = pv; sc.collection.objects.link(ob); me.materials.append(mat); obs.append(ob)
        tris += sum(len(p.vertices) - 2 for p in me.polygons)
    bpy.ops.object.select_all(action='DESELECT')
    for ob in obs: ob.select_set(True)
    bpy.context.view_layer.objects.active = obs[0]
    bpy.ops.export_scene.fbx(filepath=OUT + name + '.fbx', use_selection=True, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
                             path_mode='STRIP', embed_textures=False, mesh_smooth_type='FACE', object_types={'MESH'})
    json.dump(dict(atlas=[AW, AH], cells=[dict(obj=t['obj'], pt=t['pt'], dens=t['dens'], rect=[t['x'], t['y'], t['w'], t['h']], m=[round(t['m'][0], 3), round(t['m'][1], 3)], seed=t['i']) for t in items]),
              open(OUT + 'layout_' + name + '.json', 'w', encoding='utf-8'), indent=0)
    allv = [p for _, pts, _, _ in FACES for p in pts]
    return dict(tris=tris, objects=sorted(o.name for o in obs), atlas=[AW, AH], size=[round(max(p.x for p in allv) - min(p.x for p in allv), 2), round(max(p.y for p in allv) - min(p.y for p in allv), 2), round(max(p.z for p in allv), 2)], **(extra or {}))

MEAS = {}
# ===== H1 단독 2층 박공: 6.8×6.4, 처마 5.5, 박공 끝(삼각)이 길 쪽 · 굴뚝 · 2층 작은 발코니 1 · 옆 실외기 · 문 옆 화분 =====
reset(); K.clear(); SHELL = 'H1_shell'
W, D, E = 6.8, 6.4, 5.5; x0, x1 = -W / 2, W / 2; SD = sides(x0, x1, 0, D); PL = .45
S, L = SD['F']
fh = [(1.0, 1.95, 0, 2.15), (3.0, 4.2, .95, 2.05), (1.4, 2.3, 3.2, 5.0), (4.3, 5.0, 3.7, 4.7)]   # 문 · 거실 창(넓적) · 2층 발코니 문 · 2층 작은 창(높이 다름)
wall(S, L, 0, E, fh, 'wall', PL); door(S, *fh[0][:2], 2.15); window(S, *fh[1], shut='g', box_=True); window(S, *fh[2], sill=False); window(S, *fh[3], shut='g')
balcony(S, .95, 2.75, 3.2, .75)
S, L = SD['R']; rh = [(1.2, 2.0, 1.0, 2.0), (3.6, 4.2, 3.7, 4.5)]
wall(S, L, 0, E, rh, 'wall', PL); window(S, *rh[0], shut='g'); window(S, *rh[1]); acunit(S, 3.0, 1.0); pipe(S, .25, E)
S, L = SD['L']; lh = [(2.6, 3.3, 3.6, 4.6)]
wall(S, L, 0, E, lh, 'wall', PL); window(S, *lh[0], shut='g'); laundry(S, .6, 4.4, 2.6, .6)
for c in (1.0, 4.4): SD['L'][0].lbox(SHELL, c - .03, c + .03, 2.45, 2.75, 0, .6, 'iron', ('in',))
S, L = SD['B']; bh = [(2.0, 3.0, 1.0, 2.0)]
wall(S, L, 0, E, bh, 'wall', PL); window(S, *bh[0])
top = gable_roof(x0, x1, 0, D, E, 30, 'y')
box(SHELL, x1 - 1.6, x1 - .9, 3.6, 4.2, E, top + .9, 'wall', ('bottom',)); box(SHELL, x1 - 1.66, x1 - .84, 3.54, 4.26, top + .9, top + 1.0, 'trim')   # 굴뚝(지붕에 묻힘) + 머리
pots(SD['F'][0], 2.35, 2, .02)
MEAS['H1'] = export('H1')

# ===== H2 붙은 연립 3칸: 칸 폭 4.0/4.4/3.8, 깊이 7, 칸마다 바닥 +0.55m 씩(비탈) · 처마 높이 · 벽색 다름. 처마가 길 쪽(용마루 X) =====
reset(); K.clear(); SHELL = 'H2_shell'
UW, UZ, UE, UP = [4.0, 4.4, 3.8], [0., .55, 1.1], [5.2, 5.7, 5.3], ['wall', 'wall2', 'wall3']
D = 7.0; tot = sum(UW); xa = -tot / 2; edges = []
for i in range(3):
    x0, x1 = xa, xa + UW[i]; xa = x1; zb, e, wp = UZ[i], UZ[i] + UE[i], UP[i]; SD = sides(x0, x1, 0, D); edges.append((x0, x1, zb, e))
    S, L = SD['F']
    if i == 0:
        fh = [(.5, 1.4, zb, zb + 2.1), (2.3, 3.1, zb + 1.0, zb + 2.0), (.7, 1.6, zb + 3.1, zb + 4.4), (2.6, 3.3, zb + 3.4, zb + 4.4)]
        wall(S, L, 0, e, fh, wp, zb + .35); door(S, .5, 1.4, zb + 2.1, False, zb); window(S, *fh[1], shut='g', box_=True); window(S, *fh[2], shut='g'); window(S, *fh[3])
    elif i == 1:
        fh = [(2.9, 3.8, zb, zb + 2.1), (.6, 2.1, zb + .9, zb + 2.1), (.9, 1.8, zb + 3.0, zb + 5.1)]
        wall(S, L, 0, e, fh, wp, zb + .35); door(S, 2.9, 3.8, zb + 2.1, False, zb); window(S, *fh[1]); window(S, *fh[2], sill=False)
        balcony(S, .6, 2.2, zb + 3.0, .65); S.lbox(SHELL, 2.75, 3.95, zb + 2.3, zb + 2.36, 0, .55, {'*': 'trim', 'top': 'awning'}, ('in',))   # 문 위 작은 차양판
    else:
        fh = [(.4, 1.3, zb, zb + 2.1), (2.1, 3.0, zb + 1.1, zb + 2.0), (1.5, 2.3, zb + 3.2, zb + 4.3)]
        wall(S, L, 0, e, fh, wp, zb + .35); door(S, .4, 1.3, zb + 2.1, False, zb); window(S, *fh[1], shut='g'); window(S, *fh[2], shut='g', box_=True)
        acunit(S, 3.25, zb + 2.6)
    # 계단 앞(칸 바닥까지 돌계단 = 앞 1.2m 돌 단, 윗면 = 칸 바닥)
    if zb > 0: box(SHELL, x0 + .1, x1 - .1, -1.2, 0, 0, zb, {'*': 'stone', 'top': 'pavers'}, ('back', 'bottom'))
    S, L = SD['B']; wall(S, L, 0, e, [(1.2, 2.0, zb + 3.2, zb + 4.1)], wp, zb + .35); window(S, 1.2, 2.0, zb + 3.2, zb + 4.1)
    top = gable_roof(x0, x1, 0, D, e, 26, 'x', tri=(True, i == 2), ovr=None if i == 2 else 0, wp=wp)
for i, (x0, x1, zb, e) in enumerate(edges):  # 옆벽: 맨 끝 = 전부, 칸 사이 = 높은 칸의 벽만 낮은 칸 지붕 위로 보이는 부분(낮은 칸 벽은 skip)
    if i == 0:
        S, L = sides(x0, x1, 0, D)['L']; wall(S, L, 0, e, [(4.6, 5.4, zb + 3.3, zb + 4.2)], UP[i], zb + .35); window(S, 4.6, 5.4, zb + 3.3, zb + 4.2, shut='g')
        pipe(S, .2, e)
    if i == 2:
        S, L = sides(x0, x1, 0, D)['R']; wall(S, L, 0, e, [], UP[i], zb + .35)
        laundry(S, 1.0, 5.6, zb + 2.5, .55)
        for c in (1.0, 5.6): S.lbox(SHELL, c - .03, c + .03, zb + 2.35, zb + 2.65, 0, .55, 'iron', ('in',))
    if i < 2:  # 오른쪽 이웃(더 높다)의 왼벽: 이 칸 처마 위(박공 삼각 위로)부터
        nx0, nx1, nzb, ne = edges[i + 1]
        S, L = sides(nx0, nx1, 0, D)['L']; P = math.tan(math.radians(26)); zr = e + P * D / 2 + .2
        wall(S, L, e - .05, ne, [], UP[i + 1])
for i, (x0, x1, zb, e) in enumerate(edges):  # 낮은 칸 박공 삼각이 이웃 벽에 묻히는 쪽은 그대로(이웃 벽 뒤 = 안 보임)
    pass
pots(sides(edges[0][0], edges[0][1], 0, D)['F'][0], 1.65, 2, .02)
MEAS['H2'] = export('H2', dict(units=dict(w=UW, floor=UZ, eave=UE)))

# ===== H3 좁은 3층: 4.2×7.0, 층 2.8 → 처마 8.3, 박공 끝이 길 쪽(가파른 34°) · 2층 발코니(빨래) · 3층 작은 창 하나 · 1층 문+작은 창 =====
reset(); K.clear(); SHELL = 'H3_shell'
W, D, E = 4.2, 7.0, 8.3; x0, x1 = -W / 2, W / 2; SD = sides(x0, x1, 0, D)
S, L = SD['F']
fh = [(.45, 1.35, 0, 2.15), (2.4, 3.3, 1.1, 2.0), (1.0, 3.2, 2.9, 5.0), (1.5, 2.3, 6.1, 7.4)]   # 문 · 작은 창 · 2층 발코니 문(넓게 두 짝) · 3층 창
wall(S, L, 0, E, fh, 'wall', .5); door(S, *fh[0][:2], 2.15); window(S, *fh[1], shut='g'); window(S, *fh[2], sill=False); window(S, *fh[3], shut='g', box_=True)
balcony(S, .8, 3.4, 2.9, .8); laundry(S, 1.0, 3.2, 4.6, .45, poles=2.9)
S, L = SD['R']; rh = [(2.0, 2.8, 3.3, 4.3), (4.8, 5.4, 6.3, 7.1)]
wall(S, L, 0, E, rh, 'wall', .5); window(S, *rh[0], shut='g'); window(S, *rh[1]); acunit(S, 5.6, 3.4); pipe(S, .2, E)
S, L = SD['L']; lh = [(3.4, 4.2, 1.0, 1.9)]
wall(S, L, 0, E, lh, 'wall', .5); window(S, *lh[0])
S, L = SD['B']; wall(S, L, 0, E, [(1.6, 2.6, 6.2, 7.2)], 'wall', .5); window(S, 1.6, 2.6, 6.2, 7.2)
gable_roof(x0, x1, 0, D, E, 34, 'y', ov=.3, ovs=.22)
pots(SD['F'][0], 3.25, 2, .02)
MEAS['H3'] = export('H3')

# ===== H4 가게주택: 7.4×7.0, 1층 가게 3.6m(층고 3.4) + 2층 집(처마 6.3), 처마가 길 쪽 · 줄무늬 차양 · 빈 간판판 · 반쯤 내린 셔터 · 2층 발코니·빨래 =====
reset(); K.clear(); SHELL = 'H4_shell'
W, D, E, G = 7.4, 7.0, 6.3, 3.4; x0, x1 = -W / 2, W / 2; SD = sides(x0, x1, 0, D)
S, L = SD['F']
fh = [(.4, 4.0, 0, 2.6), (4.9, 5.8, 0, 2.2), (.8, 1.7, 4.0, 5.6), (2.9, 4.3, 4.2, 5.4), (5.9, 6.6, 4.2, 5.2)]   # 가게 입구 · 집 문 · 2층 발코니 문 · 2층 넓은 창 · 2층 작은 창
wall(S, L, 0, E, fh, 'wall', .4)
a0, a1 = fh[0][:2]  # 가게 입구(위 2.6): 안쪽 면 + 진열 어둠 판 + 반쯤 내린 셔터(밑선 1.5)
reveal(S, a0, a1, 0, 2.6, .25, ('bot',)); S.quad(SHELL, a0, a1, 0, 1.5, -.25, 'pane'); S.quad(SHELL, a0, a1, 1.5, 2.6, -.2, 'rollshut')
S.lbox(SHELL, a0 - .12, a1 + .12, 2.6, 2.66, 0, .03, 'trim', ('in',))                                     # 입구 위 띠
for c in (a0 - .12, a1): S.lbox(SHELL, c, c + .12, 0, 2.6, 0, .03, 'trim', ('in', 'top', 'bot'))
P0, P1, ZA = 2.75, 2.3, 1.0   # 차양: 벽 2.75m → 앞 1.0m 에서 2.3m, 두께 2cm(윗면·밑면 따로), 앞 드림 20cm
for dz, pt_, fl in ((0, 'awning', False), (-.02, 'awning', True)):
    q = [S.P(a0 - .1, P1 + dz, ZA), S.P(a1 + .1, P1 + dz, ZA), S.P(a1 + .1, P0 + dz, .0), S.P(a0 - .1, P0 + dz, .0)]
    face(SHELL, q[::-1] if fl else q, pt_)
S.lbox(SHELL, a0 - .1, a1 + .1, P1 - .2, P1, ZA, ZA + .02, 'awning', ('top',))
S.lbox(SHELL, a0, a1, 2.95, 3.4, 0, .05, 'signboard', ('in',))                                          # 빈 간판판(글씨 없음) — 차양 위
S.lbox(SHELL, a0 - .12, a1 + .12, 3.4, 3.46, 0, .03, 'trim', ('in',))
door(S, fh[1][0], fh[1][1], 2.2); window(S, *fh[2], sill=False); window(S, *fh[3], shut='g', box_=True); window(S, *fh[4], shut='g')
balcony(S, .6, 1.9, 4.0, .65); laundry(S, .75, 1.75, 5.3, .45, poles=4.0)
for i in range(3): box('CRATE_%02d' % (i + 1), -3.0 + i * .55, -2.55 + i * .55, -.75, -.35, 0, .32, 'basket', ('bottom',))   # 가게 앞 상자 3(낱개)
S, L = SD['R']; rh = [(2.6, 3.4, 4.2, 5.2)]
wall(S, L, 0, E, rh, 'wall', .4); window(S, *rh[0], shut='g'); acunit(S, 4.6, 4.3); pipe(S, .2, E)
S, L = SD['L']; wall(S, L, 0, E, [(1.5, 2.3, 1.0, 1.9)], 'wall', .4); window(S, 1.5, 2.3, 1.0, 1.9)
S, L = SD['B']; wall(S, L, 0, E, [(3.0, 4.0, 4.3, 5.3)], 'wall', .4); window(S, 3.0, 4.0, 4.3, 5.3)
gable_roof(x0, x1, 0, D, E, 24, 'x')
pots(SD['F'][0], 6.25, 1, .02)
MEAS['H4'] = export('H4')

# ===== 비탈 단(스튜디오 촬영용 받침): 단 3개(0 · 1.4 · 2.8m) 돌 옹벽 + 윗면 돌판. 단마다 폭 30m · 깊이 10m =====
reset(); K.clear(); SHELL = 'SITE_shell'
for i, (y0, y1, z) in enumerate(((-10, 0, 1.4), (-20, -10, 2.8))):
    box(SHELL, -15, 15, -y1, -y0, 0, z, {'*': 'stone', 'top': 'pavers'}, ('bottom', 'back') if i == 0 else ('bottom',))
box(SHELL, -5.4, 5.4, -7.5, 0, 0, .02, {'*': 'stone', 'top': 'pavers'}, ('bottom', 'back'))   # 아래 단 앞 돌판 마당(스튜디오 house_home 자리 땅 위 2cm)
MEAS['site'] = export('site')
json.dump(MEAS, open(OUT + 'measure.json', 'w', encoding='utf-8'), indent=1, ensure_ascii=False); print('MEAS', json.dumps(MEAS), flush=True)
sys.stdout.flush(); os._exit(0)
