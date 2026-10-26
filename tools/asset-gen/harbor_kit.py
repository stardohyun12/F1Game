"""항구 모듈 키트 v0 (D-162) — Blender bpy. 조각마다 손으로 정한 치수·세월(정점색)을 함수 안에 박는다.

좌표(조각 로컬): x = 벽면을 따라 0..w, 앞(밖) = -y, z = 그 층 바닥에서 위. 1 단위 = 1 m.
칠: 재질 = 구역 팔레트 색(객체 속성 wall_col/roof_col/shut_col/door_col = Unity MPB `_Tint`) × 손칠 텍스처(harbor_kit_paint.py)
     × 정점색(세월: 벽 밑 때·빗물 자국·바램 — 조각마다 위치·세기 다르게).
빛(Look 6): 해 방향 내적 2단(양지 1.0 / 그늘 0.6 + 청록 틴트), 그림자맵 없음, 청록 대기 원근(40→300 m, 최대 0.55). 윤곽선 없음.

    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/harbor_kit.py -- --sheet <out.png> [--only wall_res_3,window_open]
"""
import bpy, bmesh, math, os, sys, random
from mathutils import Vector, Matrix

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
TEX = os.path.join(ROOT, 'art', 'source', 'harbor-kit-v0', 'tex')
SUN = Vector((-0.55, -0.62, 0.56)).normalized()   # 늦가을 오후, 남서에서
FOG = '#a7cdd6'


def lin(h):
    h = h.lstrip('#'); c = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    return tuple(x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c)


# ---------------------------------------------------------------- 재질(Look 6 면 음영)
class G:
    def __init__(s, m):
        s.nt = m.node_tree; s.N = s.nt.nodes; s.L = s.nt.links; s.N.clear()

    def n(s, t, **kw):
        x = s.N.new(t)
        for k, v in kw.items(): setattr(x, k, v)
        return x

    def link(s, a, b): s.L.new(a, b)

    def vop(s, op, a, b=None, sc=None):
        x = s.n('ShaderNodeVectorMath', operation=op)
        for i, v in ((0, a), (1, b)):
            if v is None: continue
            if isinstance(v, tuple): x.inputs[i].default_value = v
            else: s.link(v, x.inputs[i])
        if sc is not None:
            if isinstance(sc, float): x.inputs['Scale'].default_value = sc
            else: s.link(sc, x.inputs['Scale'])
        return x.outputs[1] if op in ('DOT_PRODUCT', 'LENGTH', 'DISTANCE') else x.outputs[0]

    def fop(s, op, a, b=None, clamp=False):
        x = s.n('ShaderNodeMath', operation=op, use_clamp=clamp)
        for i, v in ((0, a), (1, b)):
            if v is None: continue
            if isinstance(v, (int, float)): x.inputs[i].default_value = v
            else: s.link(v, x.inputs[i])
        return x.outputs[0]

    def mrange(s, v, a, b, c, d):
        x = s.n('ShaderNodeMapRange', clamp=True)
        s.link(v, x.inputs['Value'])
        for k, val in zip(('From Min', 'From Max', 'To Min', 'To Max'), (a, b, c, d)): x.inputs[k].default_value = val
        return x.outputs['Result']

    def mix(s, a, b, f):   # a + (b-a)*f
        return s.vop('ADD', a, s.vop('SCALE', s.vop('SUBTRACT', b, a), sc=f))

    def const(s, c):
        x = s.n('ShaderNodeCombineXYZ')
        for i in range(3): x.inputs[i].default_value = c[i]
        return x.outputs[0]


_IMG = {}


def image(name):
    if name not in _IMG:
        _IMG[name] = bpy.data.images.load(os.path.join(TEX, name + '.png'), check_existing=True)
    return _IMG[name]


MATS = {}


def mat(name, base=None, attr=None, tex=None, mult=1.0, vcol=True, fog=0.55, sea=False):
    """base = '#hex' 고정색, attr = 객체 속성 이름(건물마다 색). tex = 손칠 텍스처(UV, m/4)."""
    if name in MATS: return MATS[name]
    m = bpy.data.materials.new(name); m.use_nodes = True
    g = G(m)
    out = g.n('ShaderNodeOutputMaterial'); em = g.n('ShaderNodeEmission')
    if attr:
        a = g.n('ShaderNodeAttribute', attribute_type='OBJECT', attribute_name=attr); col = a.outputs['Vector']
    else:
        col = g.const(lin(base))
    if mult != 1.0: col = g.vop('SCALE', col, sc=float(mult))
    if tex:
        uv = g.n('ShaderNodeUVMap').outputs[0]
        oi = g.n('ShaderNodeObjectInfo')
        uv = g.vop('ADD', uv, g.vop('SCALE', g.const((1.0, 0.37, 0.0)), sc=g.fop('MULTIPLY', oi.outputs['Random'], 3.0)))
        t = g.n('ShaderNodeTexImage', image=image(tex), interpolation='Linear')
        g.link(uv, t.inputs['Vector'])
        col = g.vop('MULTIPLY', col, t.outputs['Color'])
    if vcol:
        vc = g.n('ShaderNodeVertexColor', layer_name='Age')
        col = g.vop('MULTIPLY', col, vc.outputs['Color'])
    if sea:   # 바다: 근 #1f5f7a → 원 #3f8fa6, 결 따라 부드러운 띠
        cam = g.n('ShaderNodeCameraData')
        f = g.mrange(cam.outputs['View Distance'], 15, 220, 0, 1)
        col = g.mix(g.const(lin('#1f5f7a')), g.const(lin('#3f8fa6')), f)
        tc = g.n('ShaderNodeTexCoord'); mp = g.n('ShaderNodeMapping'); mp.inputs['Scale'].default_value = (0.06, 0.35, 1)
        g.link(tc.outputs['Object'], mp.inputs['Vector'])
        nz = g.n('ShaderNodeTexNoise'); nz.inputs['Scale'].default_value = 1.2; nz.inputs['Detail'].default_value = 1.0
        g.link(mp.outputs[0], nz.inputs['Vector'])
        col = g.vop('SCALE', col, sc=g.mrange(nz.outputs['Fac'], 0.35, 0.65, 0.88, 1.12))
    # 면 음영 2단: 양지 0.9~1.0, 그늘 0.6 × 청록 틴트
    geo = g.n('ShaderNodeNewGeometry')
    ndl = g.vop('DOT_PRODUCT', geo.outputs['Normal'], tuple(SUN))
    lit = g.mrange(ndl, 0.0, 1.0, 0.88, 1.0)
    tl = lin('#4d6b78')
    shade = g.const(tuple(0.6 * (0.62 + 0.38 * c / max(tl)) for c in tl))
    k = g.mrange(ndl, -0.04, 0.04, 0.0, 1.0)
    light = g.mix(shade, g.vop('SCALE', g.const((1, 1, 1)), sc=lit), k)
    if sea: light = g.const((1, 1, 1))
    c = g.vop('MULTIPLY', col, light)
    cam = g.n('ShaderNodeCameraData')
    fk = g.mrange(cam.outputs['View Distance'], 40, 300, 0, fog)
    c = g.mix(c, g.const(lin(FOG)), fk)
    g.link(c, em.inputs['Color']); g.link(em.outputs[0], out.inputs['Surface'])
    MATS[name] = m
    return m


def materials():
    mat('plaster', attr='wall_col', tex='plaster')
    mat('plaster_trim', attr='wall_col', tex='plaster_b', mult=1.10)
    mat('stone', base='#d8cfb8', tex='stone')
    mat('stone_dark', base='#a89c86', tex='stone')
    mat('glass', base='#ffffff', tex='glass', vcol=False)
    mat('shutter', attr='shut_col', tex='shutter')
    mat('wood', attr='door_col', tex='door')
    mat('awning', base='#ffffff', tex='awning')
    mat('iron', base='#3a4746', vcol=False)
    mat('roof', attr='roof_col', tex='roof')
    mat('ridge', attr='roof_col', tex='roof', mult=0.86)
    mat('eave', base='#ffffff', tex='eave', vcol=False)
    mat('pot', base='#b06a4c', vcol=False)
    mat('leaf', base='#5d7a52', vcol=False)
    mat('flower', base='#b8483e', vcol=False)
    mat('cloth_w', base='#eae6dc', tex='cloth', vcol=False)
    mat('cloth_b', base='#9fb6c2', tex='cloth', vcol=False)
    mat('cloth_p', base='#d9a69c', tex='cloth', vcol=False)
    mat('pave', base='#ffffff', tex='pave')
    mat('sea', base='#1f5f7a', vcol=False, fog=0.3, sea=True)
    mat('tree_lit', base='#8f9a5e', vcol=False)
    mat('tree_mid', base='#5d7a52', vcol=False)
    mat('hull', base='#e2d8c4', tex='plaster_b', vcol=False)
    mat('hull_b', base='#4f6a6a', vcol=False)


# ---------------------------------------------------------------- 메시 짓기
class Part:
    """면 목록을 모아 한 객체로. 정점색 'Age' = age(x,y,z) 함수(조각 로컬 좌표)."""

    def __init__(s, name):
        s.name = name; s.v = []; s.f = []; s.fm = []; s.uv = []; s.mats = []

    def mi(s, m):
        if m not in s.mats: s.mats.append(m)
        return s.mats.index(m)

    def quad(s, pts, m, uvs=None):
        """pts 반시계(앞에서 보아). uvs 없으면 면 평면에 m/4 로 투영."""
        i = len(s.v); s.v += [Vector(p) for p in pts]
        s.f.append(tuple(range(i, i + len(pts)))); s.fm.append(s.mi(m))
        if uvs is None:
            p0 = Vector(pts[0]); n = (Vector(pts[1]) - p0).cross(Vector(pts[-1]) - p0)
            if n.length < 1e-9: n = Vector((0, 0, 1))
            n.normalize()
            up = Vector((0, 0, 1)) if abs(n.z) < 0.9 else Vector((0, 1, 0))
            u = up.cross(n).normalized() * -1 if abs(n.z) < 0.9 else Vector((1, 0, 0))
            vv = n.cross(u).normalized() * -1 if abs(n.z) < 0.9 else Vector((0, 1, 0))
            uvs = [((Vector(p) - Vector((0, 0, 0))).dot(u) / 4, (Vector(p)).dot(vv) / 4) for p in pts]
            if abs(n.z) < 0.9: uvs = [(a, Vector(p).z / 4) for (a, _), p in zip(uvs, pts)]
        s.uv.append(uvs)

    def grid(s, x0, x1, z0, z1, y, m, nx, nz, front=True):
        """y 평면의 앞면(−y 향함) 격자 — 정점색 세월이 실릴 만큼 촘촘하게."""
        for i in range(nx):
            for j in range(nz):
                a, b = x0 + (x1 - x0) * i / nx, x0 + (x1 - x0) * (i + 1) / nx
                c, d = z0 + (z1 - z0) * j / nz, z0 + (z1 - z0) * (j + 1) / nz
                if front: s.quad([(a, y, c), (b, y, c), (b, y, d), (a, y, d)], m)
                else: s.quad([(b, y, c), (a, y, c), (a, y, d), (b, y, d)], m)

    def box(s, x0, x1, y0, y1, z0, z1, m, skip=()):
        P = lambda x, y, z: (x, y, z)
        F = {
            '-y': [P(x0, y0, z0), P(x1, y0, z0), P(x1, y0, z1), P(x0, y0, z1)],
            '+y': [P(x1, y1, z0), P(x0, y1, z0), P(x0, y1, z1), P(x1, y1, z1)],
            '-x': [P(x0, y1, z0), P(x0, y0, z0), P(x0, y0, z1), P(x0, y1, z1)],
            '+x': [P(x1, y0, z0), P(x1, y1, z0), P(x1, y1, z1), P(x1, y0, z1)],
            '+z': [P(x0, y0, z1), P(x1, y0, z1), P(x1, y1, z1), P(x0, y1, z1)],
            '-z': [P(x0, y1, z0), P(x1, y1, z0), P(x1, y0, z0), P(x0, y0, z0)],
        }
        for k, pts in F.items():
            if k not in skip: s.quad(pts, m)

    def cyl(s, p0, p1, r, m, seg=8, half=False, caps=True):
        p0, p1 = Vector(p0), Vector(p1); ax = (p1 - p0).normalized()
        t = ax.orthogonal().normalized(); b = ax.cross(t)
        if half:  # 반원통: 위쪽(+z 쪽) 반
            t = Vector((0, 0, 1)).cross(ax).normalized() if abs(ax.z) < 0.9 else t
            b = ax.cross(t)
            if b.z < 0: b = -b
        rng = [math.pi * i / seg for i in range(seg + 1)] if half else [2 * math.pi * i / seg for i in range(seg + 1)]
        ring = [t * math.cos(a) * r + b * math.sin(a) * r for a in rng]
        L = (p1 - p0).length
        for i in range(len(ring) - 1):
            a0, a1 = ring[i], ring[i + 1]
            s.quad([p0 + a0, p0 + a1, p1 + a1, p1 + a0], m,
                   uvs=[(i / seg, 0), ((i + 1) / seg, 0), ((i + 1) / seg, L / 4), (i / seg, L / 4)])
        if caps and not half:
            s.quad([p1 + a for a in ring[:-1]], m); s.quad([p0 + a for a in reversed(ring[:-1])], m)

    def build(s, age=None, coll=None):
        me = bpy.data.meshes.new(s.name)
        me.from_pydata([tuple(v) for v in s.v], [], s.f)
        for m in s.mats: me.materials.append(MATS[m])
        for p, k in zip(me.polygons, s.fm): p.material_index = k
        uvl = me.uv_layers.new(name='UV')
        li = 0
        for p, uvs in zip(me.polygons, s.uv):
            for j in range(p.loop_total): uvl.data[p.loop_start + j].uv = uvs[j]
        ca = me.color_attributes.new('Age', 'FLOAT_COLOR', 'POINT')
        for i, v in enumerate(me.vertices):
            a = age(v.co) if age else 1.0
            ca.data[i].color = (a, a * (0.99 if a < 1 else 1.0), a * (0.97 if a < 1 else 1.0), 1)
        me.validate(); me.update()
        ob = bpy.data.objects.new(s.name, me)
        (coll or bpy.context.scene.collection).objects.link(ob)
        return ob


def smooth(t): t = max(0.0, min(1.0, t)); return t * t * (3 - 2 * t)


def age_fn(grime=0.10, grime_h=0.7, streaks=(), fade=(), seed=0):
    """세월: 벽 밑 때(grime, 높이 grime_h) · 빗물 자국 streaks=[(x, 위 z, 길이, 폭, 세기)] · 바랜 곳 fade=[(x,z,반경,밝힘)]."""
    rnd = random.Random(seed)

    def f(co):
        x, z = co.x, co.z
        a = 1.0 - grime * smooth(1 - z / grime_h)
        for sx, top, ln, w, k in streaks:
            if z < top:
                d = abs(x - sx) / w
                a -= k * smooth(1 - d) * smooth(1 - (top - z) / ln)
        for fx, fz, r, k in fade:
            d = math.hypot(x - fx, z - fz) / r
            a += k * smooth(1 - d)
        return a
    return f


# ---------------------------------------------------------------- 키트 조각
FH = 3.0       # 위층 층고
FH0 = 3.4      # 1층(가게) 층고
T = 0.14       # 벽판 두께


def wall_res(w, name, age):
    """2층 주거 벽판: 회반죽 면 + 바닥 쪽 띠돌림(층 띠) + 양끝 살짝 두꺼운 갓(벽판 이음 가림)."""
    p = Part(name)
    nx, nz = max(2, int(w / 0.25)), int(FH / 0.25)
    p.grid(0, w, 0.16, FH, 0, 'plaster', nx, nz - 1)
    p.box(0, w, -0.06, T, 0, 0.16, 'plaster_trim', skip=('+y',))        # 층 띠
    p.quad([(0, T, 0.16), (0, 0, 0.16), (0, 0, FH), (0, T, FH)], 'plaster')
    p.quad([(w, 0, 0.16), (w, T, 0.16), (w, T, FH), (w, 0, FH)], 'plaster')
    return p.build(age)


def wall_shop(w, name, age):
    """1층 가게 벽판: 돌 굽(0.45) + 회반죽 + 큰 가게 개구(나무 가게틀·간판 판·짙은 속)."""
    p = Part(name)
    ox0, ox1, oz0, oz1 = 0.35, w - 0.35, 0.12, 2.55
    nz = int(FH0 / 0.25)
    for (a, b, c, d) in ((0, ox0, 0.45, FH0), (ox1, w, 0.45, FH0), (ox0, ox1, oz1 + 0.5, FH0)):
        p.grid(a, b, c, d, 0, 'plaster', max(1, int((b - a) / 0.25)), max(1, int((d - c) / 0.25)))
    p.box(0, ox0, -0.05, T, 0, 0.45, 'stone_dark', skip=('+y',))
    p.box(ox1, w, -0.05, T, 0, 0.45, 'stone_dark', skip=('+y',))
    # 가게틀: 기둥 2 + 간판 판(0.5) + 문턱
    p.box(ox0, ox0 + 0.14, -0.10, 0.05, 0, oz1, 'wood')
    p.box(ox1 - 0.14, ox1, -0.10, 0.05, 0, oz1, 'wood')
    p.box(ox0 - 0.05, ox1 + 0.05, -0.16, 0.05, oz1, oz1 + 0.5, 'wood')       # 간판 판
    p.box(ox0 - 0.08, ox1 + 0.08, -0.20, 0.05, oz1 + 0.5, oz1 + 0.56, 'wood')  # 간판 갓
    p.box(ox0, ox1, -0.14, 0.05, 0, oz0, 'stone')
    # 짙은 속(진열창): 0.25 들어간 유리 + 가운데 문틀
    p.quad([(ox0 + .14, 0.25, oz0), (ox1 - .14, 0.25, oz0), (ox1 - .14, 0.25, oz1), (ox0 + .14, 0.25, oz1)], 'glass',
           uvs=[(0, 0), (1, 0), (1, 1), (0, 1)])
    for xx, side in ((ox0 + .14, 1), (ox1 - .14, -1)):   # 안쪽 볼(측면)
        p.quad([(xx, 0.05, oz0), (xx, 0.25, oz0), (xx, 0.25, oz1), (xx, 0.05, oz1)][::side], 'wood')
    p.quad([(ox0 + .14, 0.05, oz1), (ox1 - .14, 0.05, oz1), (ox1 - .14, 0.25, oz1), (ox0 + .14, 0.05, oz1)][:3] +
           [(ox0 + .14, 0.25, oz1)], 'eave')
    mx = (ox0 + ox1) / 2
    p.box(mx - 0.05, mx + 0.05, 0.12, 0.24, oz0, oz1, 'wood')
    p.box(ox0 + .14, ox1 - .14, 0.12, 0.24, 1.0, 1.06, 'wood')                 # 진열창 가로살
    p.quad([(0, T, 0), (0, 0, 0), (0, 0, FH0), (0, T, FH0)], 'plaster')
    p.quad([(w, 0, 0), (w, T, 0), (w, T, FH0), (w, 0, FH0)], 'plaster')
    return p.build(age)


DEEP = 0.16   # 창 테두리가 벽면에서 튀어나온 깊이 — 벽에 구멍을 뚫지 않고 테두리 안쪽 볼로 깊이를 낸다


def surround(p, w, h, deep=DEEP):
    """창 돌 테두리 + 창턱 + 인방 갓. 원점 = 창 개구 왼쪽 아래, 벽면 y=0, 전부 벽 앞(y<0)."""
    b = 0.13
    p.box(-b, 0, -deep, 0.0, -0.05, h + b, 'stone', skip=('+y',))
    p.box(w, w + b, -deep, 0.0, -0.05, h + b, 'stone', skip=('+y',))
    p.box(0, w, -deep, 0.0, h, h + b, 'stone', skip=('+y',))
    p.box(-b - 0.06, w + b + 0.06, -deep - 0.12, 0.0, -0.12, 0.0, 'stone', skip=('+y',))      # 창턱(튀어나옴)
    p.box(-b - 0.04, w + b + 0.04, -deep - 0.05, 0.0, h + b, h + b + 0.07, 'stone', skip=('+y',))  # 인방 갓


def glazing(p, w, h, open_=True):
    """유리(짙은 속) + 창살. open_ 이면 한 짝이 안으로 열려 비스듬히 — 속이 더 깊어 보인다."""
    y = -0.01
    p.quad([(0, y, 0), (w, y, 0), (w, y, h), (0, y, h)], 'glass', uvs=[(0, 0), (1, 0), (1, 1), (0, 1)])
    m = w / 2
    p.box(m - 0.03, m + 0.03, y - 0.04, y, 0, h, 'stone')
    p.box(0, w, y - 0.04, y, h * 0.62, h * 0.62 + 0.05, 'stone')
    # 테두리 안쪽 볼(위·옆) — 짙은 그늘색(처마 띠 텍스처), 위가 제일 짙다
    yb, yf = y, -DEEP
    p.quad([(0, yf, h), (w, yf, h), (w, yb, h), (0, yb, h)][::-1], 'eave')
    p.quad([(0, yb, 0), (0, yf, 0), (0, yf, h), (0, yb, h)][::-1], 'eave')
    p.quad([(w, yf, 0), (w, yb, 0), (w, yb, h), (w, yf, h)][::-1], 'eave')
    if open_:   # 오른짝이 안쪽으로 35° 열림: 밝은 창틀 모서리만 보임
        a = math.radians(35); L = m * 0.9
        x1, y1 = w - L * math.cos(a), y - L * math.sin(a) * 0.15
        p.quad([(x1, y1 - 0.02, 0.04), (w - 0.01, y - 0.02, 0.04), (w - 0.01, y - 0.02, h - 0.04), (x1, y1 - 0.02, h - 0.04)],
               'stone')


def shutter_leaf(p, hx, d, wl, h, y, ang=0.0):
    """덧문 한 짝. 경첩 x=hx 에서 d(−1 왼쪽/+1 오른쪽)로 폭 wl. ang = 벽에서 들뜬 각(짝마다 손으로 다르게).
    닫힌 짝은 ang=None — 개구 안쪽으로 접혀 창을 덮는다."""
    th = 0.035
    a = math.radians(ang)
    ex, ey = hx + d * wl * math.cos(a), y - wl * math.sin(a)
    lo, hi = ((ex, ey), (hx, y)) if d < 0 else ((hx, y), (ex, ey))
    q = [(lo[0], lo[1], 0), (hi[0], hi[1], 0), (hi[0], hi[1], h), (lo[0], lo[1], h)]
    p.quad(q, 'shutter', uvs=[(0, 0), (1, 0), (1, 1), (0, 1)])
    qb = [(x, yy + th, z) for x, yy, z in q][::-1]
    p.quad(qb, 'shutter', uvs=[(0, 0), (1, 0), (1, 1), (0, 1)])
    p.quad([(lo[0], lo[1] + th, 0), (lo[0], lo[1], 0), (lo[0], lo[1], h), (lo[0], lo[1] + th, h)], 'shutter')
    p.quad([(hi[0], hi[1], 0), (hi[0], hi[1] + th, 0), (hi[0], hi[1] + th, h), (hi[0], hi[1], h)], 'shutter')


WIN_W, WIN_H, WIN_Z = 1.0, 1.55, 0.85   # 위층 창: 개구 폭·높이, 바닥에서 창턱 높이


def window(kind, name, age):
    """kind = 'closed' | 'open' | 'pot'. 원점 = 창 중심 아래 바닥(x=0 이 창 가운데)."""
    p = Part(name)
    w, h = WIN_W, WIN_H
    sub = Part('tmp')
    surround(p, w, h)
    if kind == 'closed':   # 닫힌 짝: 개구 안, 가운데 한 짝이 덜 닫혀 틈(손 디테일)
        glazing(p, w, h, open_=False)
        shutter_leaf(p, 0.0, 1, w / 2 - 0.01, h - 0.02, -0.05, ang=0)
        shutter_leaf(p, w, -1, w / 2 - 0.01, h - 0.02, -0.05, ang=7)
    else:                  # 열린 짝: 테두리 바깥 벽 위로 펼침, 두 짝 들뜬 각이 다르다
        glazing(p, w, h, open_=True)
        shutter_leaf(p, -0.13, -1, w / 2, h, -0.04, ang=4 if kind == 'open' else 10)
        shutter_leaf(p, w + 0.13, 1, w / 2, h, -0.04, ang=15 if kind == 'open' else 5)
    if kind == 'pot':   # 화분대: 쇠 받침 + 테라코타 3개 + 붉은 꽃·잎 덩어리
        p.box(-0.1, w + 0.1, -0.42, -0.2, -0.16, -0.12, 'iron')
        for i, (px, r, fl) in enumerate(((0.12, 0.13, 'flower'), (0.5, 0.15, 'leaf'), (0.86, 0.12, 'flower'))):
            p.box(px - r * .8, px + r * .8, -0.38 - r * .6, -0.38 + r * .6, -0.12, 0.10, 'pot')
            blob(p, (px, -0.38, 0.22), r * 1.5, fl, seed=i)
            blob(p, (px + 0.07, -0.33, 0.16), r * 1.1, 'leaf', seed=i + 7)
    ob = p.build(age)
    for v in ob.data.vertices: v.co.x -= w / 2; v.co.z += WIN_Z
    return ob


def blob(p, c, r, m, seed=0):
    """잎·꽃 덩어리: 찌그러진 팔면체 2단(낮은 면수, 덩어리째 음영)."""
    rnd = random.Random(seed); c = Vector(c)
    pts = [Vector((math.cos(a) * r * rnd.uniform(.8, 1.15), math.sin(a) * r * rnd.uniform(.8, 1.15), 0))
           for a in [i * math.pi / 3 for i in range(6)]]
    top, bot = c + Vector((0, 0, r * 0.9)), c + Vector((0, 0, -r * 0.6))
    for i in range(6):
        a, b = c + pts[i], c + pts[(i + 1) % 6]
        p.quad([tuple(a), tuple(b), tuple(top)], m, uvs=[(0, 0), (1, 0), (0, 1)])
        p.quad([tuple(b), tuple(a), tuple(bot)], m, uvs=[(0, 0), (1, 0), (0, 1)])


def kit_wall_window(coll):
    """1단계 확인용: 벽판 3 m + 열린 창."""
    w = wall_res(3.0, 'wall_res_3', age_fn(0.12, 0.8, streaks=[(1.0, 0.75, 1.1, 0.22, 0.12), (2.35, 2.9, 1.6, 0.16, 0.08)],
                                            fade=[(2.4, 2.4, 0.9, 0.05)]))
    win = window('open', 'window_open', age_fn(0.0, 0.1, streaks=[(-0.3, 0.75, 0.6, 0.12, 0.10)]))
    win.location = (1.5, 0, 0)
    for o in (w, win):
        o['wall_col'] = lin('#d2a576'); o['shut_col'] = lin('#6f8a78'); o['door_col'] = lin('#6b5a48')
        if o.users_collection: o.users_collection[0].objects.unlink(o)
        coll.objects.link(o)
    return [w, win]


def corner(name, age, h=FH):
    """모서리돌(퀸): 긴 돌·짧은 돌 번갈아(0.5/0.3), 0.04 튀어나옴. 원점 = 모서리, 두 벽 x+·y+ 방향(로컬)."""
    p = Part(name)
    z, i = 0.0, 0
    while z < h - 0.05:
        hh = min(0.32, h - z); L = 0.5 if i % 2 == 0 else 0.3
        p.box(-0.05, L, -0.05, 0.02, z + 0.02, z + hh - 0.02, 'stone')           # 앞면 쪽
        p.box(-0.05, 0.02, -0.05, L - 0.05 if i % 2 else 0.25, z + 0.02, z + hh - 0.02, 'stone')
        z += hh; i += 1
    return p.build(age)


def door(name, age):
    """주거 문: 돌 테두리 + 위 채광창(짙은) + 나무 두 짝(색은 door_col) + 디딤돌. 원점 = 문 가운데 바닥."""
    p = Part(name)
    w, h = 1.1, 2.3
    surround(p, w, h, deep=0.2)
    p.quad([(0, -0.02, h - 0.45), (w, -0.02, h - 0.45), (w, -0.02, h), (0, -0.02, h)], 'glass', uvs=[(0, 0), (1, 0), (1, 1), (0, 1)])
    p.box(0, w, -0.05, -0.02, h - 0.5, h - 0.45, 'wood')
    p.box(0.02, w / 2 - 0.01, -0.06, -0.02, 0.0, h - 0.5, 'wood')
    p.box(w / 2 + 0.01, w - 0.02, -0.08, -0.02, 0.0, h - 0.5, 'wood')        # 한 짝이 살짝 덜 닫힘
    p.quad([(0, -0.2, h), (w, -0.2, h), (w, -0.02, h), (0, -0.02, h)][::-1], 'eave')
    p.box(-0.25, w + 0.25, -0.55, 0.0, -0.18, 0.0, 'stone_dark')                 # 디딤돌
    ob = p.build(age)
    for v in ob.data.vertices: v.co.x -= w / 2
    return ob


def awning(name, w=2.6):
    """가게 차양: 쇠팔 2 + 비스듬한 천(0.9 m 나옴) + 물결 늘어짐(가리비 끝). 원점 = 가운데, 높이 0 = 차양 윗선."""
    p = Part(name)
    d, drop = 0.95, 0.42
    x0, x1 = -w / 2, w / 2
    p.quad([(x0, -d, -drop), (x1, -d, -drop), (x1, 0, 0), (x0, 0, 0)], 'awning', uvs=[(0, 0), (w / 2.6, 0), (w / 2.6, 1), (0, 1)])
    p.quad([(x0, 0, 0), (x1, 0, 0), (x1, -d, -drop), (x0, -d, -drop)], 'eave', uvs=[(0, 0), (1, 0), (1, 1), (0, 1)])
    n = max(4, int(w / 0.32))
    for i in range(n):     # 가리비 늘어짐: 반원 대신 삼각 두 단
        a, b = x0 + w * i / n, x0 + w * (i + 1) / n
        p.quad([(a, -d, -drop), (b, -d, -drop), (b, -d, -drop - 0.12), ((a + b) / 2, -d, -drop - 0.2), (a, -d, -drop - 0.12)][::-1][::-1],
               'awning', uvs=[(i / n * w / 2.6, 0.9), ((i + 1) / n * w / 2.6, 0.9), ((i + 1) / n * w / 2.6, 0.78), ((i + .5) / n * w / 2.6, 0.72), (i / n * w / 2.6, 0.78)])
        p.quad([(a, -d + 0.005, -drop), (a, -d + 0.005, -drop - 0.12), ((a + b) / 2, -d + 0.005, -drop - 0.2), (b, -d + 0.005, -drop - 0.12), (b, -d + 0.005, -drop)],
               'eave', uvs=[(0, 0)] * 5)
    for x in (x0 + 0.1, x1 - 0.1):
        p.cyl((x, 0, -0.75), (x, -d + 0.05, -drop + 0.02), 0.02, 'iron', seg=5)
    return p.build(None)


def balcony(name, age):
    """발코니: 돌 바닥판 + 까치발 3 + 쇠 난간(살 0.12 간격) + 빨랫줄(천 3장, 색·처짐 다르게) + 뒤 프랑스 창(키 큰 열린 창)."""
    p = Part(name)
    w, d = 2.2, 0.75
    x0, x1 = -w / 2, w / 2
    p.box(x0, x1, -d, 0.0, -0.16, 0.0, 'stone')
    for x in (x0 + 0.2, 0, x1 - 0.2):
        p.box(x - 0.07, x + 0.07, -d + 0.15, 0.0, -0.5, -0.16, 'stone')
    hr = 0.95
    p.box(x0, x1, -d, -d + 0.04, hr, hr + 0.04, 'iron')
    p.box(x0, x0 + 0.04, -d, 0.0, hr, hr + 0.04, 'iron'); p.box(x1 - 0.04, x1, -d, 0.0, hr, hr + 0.04, 'iron')
    x = x0
    while x <= x1 + 1e-6:
        p.box(x - 0.012, x + 0.012, -d + 0.005, -d + 0.03, 0.0, hr, 'iron'); x += 0.12
    for y in (-0.12,):
        for xx in (x0 + 0.02, x1 - 0.04):
            p.box(xx, xx + 0.02, -d, y, 0.0, hr, 'iron')
    # 프랑스 창(벽 앞): 테두리 + 짙은 속 + 열린 덧문
    fw, fh = 1.0, 2.2
    sub = -fw / 2
    q = Part('_tmp')
    surround(q, fw, fh); glazing(q, fw, fh, open_=True)
    shutter_leaf(q, -0.13, -1, fw / 2, fh, -0.04, ang=8)
    shutter_leaf(q, fw + 0.13, 1, fw / 2, fh, -0.04, ang=3)
    base = len(p.v)
    for fi, f in enumerate(q.f):
        pts = [tuple(q.v[k] + Vector((sub, 0, 0))) for k in f]
        p.quad(pts, q.mats[q.fm[fi]], uvs=q.uv[fi])
    # 빨랫줄: 난간 위 1.6 m, 천 3장
    zl = 1.75
    p.cyl((x0 + 0.05, -d + 0.02, zl), (x1 - 0.05, -d + 0.02, zl - 0.04), 0.008, 'iron', seg=4, caps=False)
    for cx, cw, ch, m, sw in ((-0.65, 0.55, 0.7, 'cloth_w', 0.03), (0.0, 0.42, 0.5, 'cloth_b', -0.04), (0.55, 0.5, 0.82, 'cloth_p', 0.02)):
        y = -d + 0.02
        p.quad([(cx - cw / 2 + sw, y - 0.03, zl - ch), (cx + cw / 2 + sw, y - 0.03, zl - ch + 0.03), (cx + cw / 2, y, zl - 0.02), (cx - cw / 2, y, zl)], m,
               uvs=[(0, 0), (1, 0), (1, 1), (0, 1)])
        p.quad([(cx - cw / 2, y + 0.005, zl), (cx + cw / 2, y + 0.005, zl - 0.02), (cx + cw / 2 + sw, y - 0.025, zl - ch + 0.03), (cx - cw / 2 + sw, y - 0.025, zl - ch)], m,
               uvs=[(0, 0), (1, 0), (1, 1), (0, 1)])
    return p.build(age)


def eave_band(name):
    """처마 띠(1 m, x 로 늘려 씀): 3단 몰딩 + 밑면 짙은 띠. 높이 0 = 벽 윗선."""
    p = Part(name)
    for (y0, z0, z1) in ((-0.06, -0.34, -0.22), (-0.14, -0.22, -0.10), (-0.24, -0.10, 0.02)):
        p.box(0, 1, y0, 0.0, z0, z1, 'plaster_trim', skip=('+y', '-x', '+x'))
    p.quad([(0, 0.0, -0.52), (1, 0.0, -0.52), (1, -0.004, -0.34), (0, -0.004, -0.34)], 'eave', uvs=[(0, 1), (1, 1), (1, 0), (0, 0)])
    return p.build(None)


def ridge(name):
    """용마루 기와(1 m, 늘려 씀): 반원통 + 3장마다 살짝 굵은 이음."""
    p = Part(name)
    p.cyl((0, 0, 0), (1, 0, 0), 0.13, 'ridge', seg=6, half=True)
    return p.build(None)


def verge(name):
    """박공 끝 판(1 m, 늘려 씀): 지붕 가장자리 막음 — 밝은 회반죽 띠 + 밑 그늘."""
    p = Part(name)
    p.box(0, 1, -0.06, 0.0, -0.22, 0.06, 'plaster_trim')
    return p.build(None)


def chimney(name, age):
    """굴뚝: 회반죽 몸통 0.7×0.55×1.3 + 돌 갓 + 테라코타 연통 2(높이 다르게) + 위쪽 그을음(정점색)."""
    p = Part(name)
    p.box(-0.35, 0.35, -0.28, 0.28, 0, 1.3, 'plaster')
    p.box(-0.42, 0.42, -0.35, 0.35, 1.3, 1.42, 'stone')
    p.cyl((-0.14, 0, 1.42), (-0.14, 0, 1.78), 0.09, 'pot', seg=7)
    p.cyl((0.16, 0.02, 1.42), (0.16, 0.02, 1.66), 0.08, 'pot', seg=7)
    return p.build(age)


def downpipe(name, h=6.0):
    """홈통: 처마 물받이 머리 + 세로관(벽에서 0.08) + 아래 꺾임. 원점 = 바닥, 높이 h."""
    p = Part(name)
    p.box(-0.12, 0.12, -0.24, -0.02, h - 0.3, h, 'iron')
    p.cyl((0, -0.1, 0.25), (0, -0.1, h - 0.3), 0.045, 'iron', seg=6)
    p.cyl((0, -0.1, 0.25), (0, -0.32, 0.05), 0.045, 'iron', seg=6)
    for z in (1.0, h * 0.5, h - 0.8):
        p.box(-0.06, 0.06, -0.06, 0.0, z, z + 0.05, 'iron')
    return p.build(None)


def bollard(name):
    """계선주: 돌 받침 + 쇠 버섯머리."""
    p = Part(name)
    p.cyl((0, 0, 0), (0, 0, 0.45), 0.16, 'iron', seg=8)
    p.cyl((0, 0, 0.45), (0, 0, 0.55), 0.24, 'iron', seg=8)
    return p.build(None)


def boat(name, L=6.0):
    """작은 어선: 바랜 크림 선체 + 청록 띠 + 작은 조타실. 원점 = 수면 가운데."""
    p = Part(name)
    w, hh = 1.9, 0.9
    ring = [(-L / 2, 0), (-L / 2 + 0.6, -w / 2), (L / 2 - 1.4, -w / 2), (L / 2, 0), (L / 2 - 1.4, w / 2), (-L / 2 + 0.6, w / 2)]
    for i in range(len(ring)):
        (ax, ay), (bx, by) = ring[i], ring[(i + 1) % len(ring)]
        p.quad([(ax * .8, ay * .6, -0.35), (bx * .8, by * .6, -0.35), (bx, by, hh), (ax, ay, hh)][::-1], 'hull',
               uvs=[(0, 0), (1, 0), (1, 1), (0, 1)])
        p.quad([(ax * 1.01, ay * 1.01, hh - 0.25), (bx * 1.01, by * 1.01, hh - 0.25), (bx * 1.01, by * 1.01, hh - 0.08), (ax * 1.01, ay * 1.01, hh - 0.08)][::-1], 'hull_b')
    p.quad([(x, y, hh - 0.15) for x, y in ring], 'stone_dark')
    p.box(-1.6, -0.3, -0.55, 0.55, hh - 0.15, hh + 1.0, 'hull')
    p.box(-1.7, -0.2, -0.65, 0.65, hh + 1.0, hh + 1.08, 'hull_b')
    p.cyl((0.6, 0, hh), (0.6, 0, hh + 3.2), 0.05, 'iron', seg=5)
    return p.build(None)


# 키트 일람 — (이름, 만드는 함수, 손으로 정한 세월)
def kit_pieces():
    A = age_fn
    return [
        ('wall_res_3', lambda n: wall_res(3.0, n, A(0.12, 0.8, streaks=[(1.0, 0.75, 1.1, 0.22, 0.12), (2.35, 2.9, 1.6, 0.16, 0.08)], fade=[(2.4, 2.4, 0.9, 0.05)]))),
        ('wall_res_3b', lambda n: wall_res(3.0, n, A(0.08, 0.6, streaks=[(0.4, 2.95, 2.2, 0.3, 0.07)], fade=[(1.2, 1.4, 1.1, 0.06)]))),
        ('wall_res_4', lambda n: wall_res(4.0, n, A(0.10, 0.7, streaks=[(1.2, 0.75, 1.3, 0.25, 0.1), (3.1, 0.75, 0.8, 0.18, 0.09)], fade=[(3.3, 2.6, 1.0, 0.05)]))),
        ('wall_shop_3', lambda n: wall_shop(3.0, n, A(0.16, 1.0, streaks=[(0.2, 3.3, 1.4, 0.15, 0.08)]))),
        ('wall_shop_4', lambda n: wall_shop(4.0, n, A(0.14, 0.9, streaks=[(3.8, 3.3, 1.8, 0.18, 0.1)], fade=[(0.3, 2.9, 0.6, 0.05)]))),
        ('corner', lambda n: corner(n, A(0.1, 0.6))),
        ('window_closed', lambda n: window('closed', n, A(0.0, 0.1, streaks=[(0.3, 0.75, 0.5, 0.1, 0.08)]))),
        ('window_open', lambda n: window('open', n, A(0.0, 0.1, streaks=[(-0.3, 0.75, 0.6, 0.12, 0.10)]))),
        ('window_pot', lambda n: window('pot', n, A(0.0, 0.1))),
        ('door', lambda n: door(n, A(0.15, 0.5))),
        ('awning', lambda n: awning(n)),
        ('balcony', lambda n: balcony(n, A(0.0, 0.1, streaks=[(-0.6, -0.5, 1.0, 0.15, 0.1), (0.6, -0.5, 0.7, 0.12, 0.08)]))),
        ('eave_band', eave_band), ('ridge', ridge), ('verge', verge),
        ('chimney', lambda n: chimney(n, lambda co: 1.0 - 0.25 * smooth((co.z - 0.8) / 0.5))),
        ('downpipe', downpipe), ('bollard', bollard), ('boat', boat),
    ]


def build_kit(coll):
    out = {}
    for name, fn in kit_pieces():
        ob = fn(name)
        for c in ob.users_collection: c.objects.unlink(ob)
        coll.objects.link(ob); out[name] = ob
    return out


# ---------------------------------------------------------------- 렌더 공통
def scene_setup(res=(960, 540)):
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = res
    sc.render.film_transparent = True
    sc.view_settings.view_transform = 'Standard'
    sc.view_settings.look = 'None'
    try: sc.eevee.taa_render_samples = 32
    except Exception: pass
    w = bpy.data.worlds.new('W'); sc.world = w; w.use_nodes = True
    w.node_tree.nodes['Background'].inputs['Color'].default_value = (*lin(FOG), 1)
    return sc


def camera(loc, target, lens=35, name='Cam'):
    cd = bpy.data.cameras.new(name); cd.lens = lens; cd.clip_end = 2000
    ob = bpy.data.objects.new(name, cd); bpy.context.scene.collection.objects.link(ob)
    ob.location = loc
    d = Vector(target) - Vector(loc)
    ob.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
    bpy.context.scene.camera = ob
    return ob


def render(path):
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


def main():
    argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    out = argv[argv.index('--out') + 1] if '--out' in argv else os.path.join(ROOT, 'art', '_review', '2026-10-27-harbor-kit-v0', 'step1')
    os.makedirs(out, exist_ok=True)
    for o in list(bpy.data.objects): bpy.data.objects.remove(o)
    sc = scene_setup((960, 540))
    materials()
    coll = bpy.data.collections.new('KIT'); sc.collection.children.link(coll)
    kit_wall_window(coll)
    camera((-1.6, -6.2, 2.1), (1.5, 0, 1.7), lens=40, name='C1')
    render(os.path.join(out, 'wall_window_1x.png'))
    camera((0.2, -2.6, 1.9), (1.5, 0, 1.6), lens=40, name='C2')
    render(os.path.join(out, 'wall_window_close.png'))
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out, 'step1.blend'))


if __name__ == '__main__':
    main()
