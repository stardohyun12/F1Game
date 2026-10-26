"""정비소·고카트 v1 (아트 2 비교 제작, D-163 도트 기반 로우폴리). 면 적은 각진 형태 + 16px/m 도트 텍스처(Closest).
빛 = Look 6: 해 방향 내적 두 단(밝은 0.88~1.0 / 그늘 0.6 청록), 그림자맵 없음, 청록 공기원근. 안쪽 면(뒷면)·실내 물건은 그늘 쪽.

    python -X utf8 tools/asset-gen/opus_pixel_tex.py
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_garage_kart.py
"""
import bpy, bmesh, json, math, os
from mathutils import Vector, Matrix

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
TEX = os.path.join(ROOT, 'art', 'source', 'opus-garage-kart', 'tex')
OUT = os.path.join(ROOT, 'art', '_review', '2026-10-27-garage-kart-opus')
PPM = 16
SUN = Vector((0.5, -0.6, 0.62)).normalized()


def lin(h):
    h = h.lstrip('#'); c = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    return tuple(v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4 for v in c)


MATS, IMGS = {}, {}


def mat(name, glow=False):
    if name in MATS: return MATS[name]
    img = bpy.data.images.load(os.path.join(TEX, name + '.png')); IMGS[name] = img
    m = bpy.data.materials.new(name); m.use_nodes = True; nt = m.node_tree; nt.nodes.clear()
    N, L = nt.nodes.new, nt.links.new
    t = N('ShaderNodeTexImage'); t.image = img; t.interpolation = 'Closest'
    em = N('ShaderNodeEmission'); out = N('ShaderNodeOutputMaterial'); L(em.outputs[0], out.inputs[0])
    if glow:
        L(t.outputs['Color'], em.inputs['Color']); em.inputs['Strength'].default_value = 1.5
        MATS[name] = m; return m
    geo = N('ShaderNodeNewGeometry')

    def vm(op, a, b=None, sc=None):
        n = N('ShaderNodeVectorMath'); n.operation = op
        for i, s in enumerate((a, b)):
            if s is None: continue
            if isinstance(s, tuple): n.inputs[i].default_value = s
            else: L(s, n.inputs[i])
        if sc is not None: L(sc, n.inputs[3])
        return n.outputs['Value' if op == 'DOT_PRODUCT' else 'Vector']

    def fm(op, a, b, c=None, clamp=False):
        n = N('ShaderNodeMath'); n.operation = op; n.use_clamp = clamp
        for i, s in enumerate((a, b, c)):
            if s is None: continue
            if isinstance(s, (int, float)): n.inputs[i].default_value = s
            else: L(s, n.inputs[i])
        return n.outputs[0]

    def mr(v, a, b, c, d):
        n = N('ShaderNodeMapRange'); n.clamp = True; L(v, n.inputs['Value'])
        for k, x in zip(('From Min', 'From Max', 'To Min', 'To Max'), (a, b, c, d)): n.inputs[k].default_value = x
        return n.outputs['Result']

    ndl = vm('DOT_PRODUCT', geo.outputs['Normal'], tuple(SUN))
    sunp = N('ShaderNodeAttribute'); sunp.attribute_type = 'OBJECT'; sunp.attribute_name = 'sun'
    f = fm('MULTIPLY', mr(ndl, 0.0, 0.08, 0.0, 1.0), sunp.outputs['Fac'])
    f = fm('MULTIPLY', f, fm('SUBTRACT', 1.0, geo.outputs['Backfacing']))
    litv = fm('MULTIPLY_ADD', ndl, 0.12, 0.88, clamp=True)
    cx = N('ShaderNodeCombineXYZ')
    for i in range(3): L(litv, cx.inputs[i])
    tl = lin('#4d6b78'); shade = tuple(0.6 * (0.62 + 0.38 * c / max(tl)) for c in tl)
    light = vm('ADD', vm('SCALE', vm('SUBTRACT', cx.outputs[0], shade), None, f), shade)
    col = vm('MULTIPLY', t.outputs['Color'], light)
    cam = N('ShaderNodeCameraData')
    k = mr(cam.outputs['View Distance'], 40.0, 300.0, 0.0, 0.55)
    col = vm('ADD', col, vm('SCALE', vm('SUBTRACT', lin('#a7cdd6'), col), None, k))
    L(col, em.inputs['Color'])
    MATS[name] = m; return m


COLL = {}


def coll(name):
    if name not in COLL:
        c = bpy.data.collections.new(name); bpy.context.scene.collection.children.link(c); COLL[name] = c
    return COLL[name]


CUR = ['garage']
TEXSIZE = {}


def texsize(name):
    if name not in TEXSIZE:
        mat(name); TEXSIZE[name] = IMGS[name].size[:]
    return TEXSIZE[name]


def mk(name, verts, faces, m, uv='world', rect=(0, 0, 1, 1), loc=(0, 0, 0), rot=0.0, sun=1.0, tilt=None, glow=False):
    """verts 로컬 좌표, faces 반시계=바깥. uv: 'world' = 면 평면에 미터 투영(16px/m), 'fit' = 면 바운드를 rect 에 맞춤."""
    me = bpy.data.meshes.new(name); me.from_pydata([Vector(v) for v in verts], [], faces); me.update()
    bm = bmesh.new(); bm.from_mesh(me); lay = bm.loops.layers.uv.new('UVMap')
    tw, th = texsize(m)
    for fc in bm.faces:
        n = fc.normal
        u = Vector((0, 0, 1)).cross(n) if abs(n.z) < 0.95 else Vector((1, 0, 0))
        u.normalize(); v = n.cross(u)
        if abs(n.z) >= 0.95 and n.z < 0: v = -v
        pts = [(l.vert.co.dot(u), l.vert.co.dot(v)) for l in fc.loops]
        if uv == 'world':
            for l, (a, b) in zip(fc.loops, pts): l[lay].uv = (a * PPM / tw, b * PPM / th)
        else:
            a0, a1 = min(p[0] for p in pts), max(p[0] for p in pts); b0, b1 = min(p[1] for p in pts), max(p[1] for p in pts)
            for l, (a, b) in zip(fc.loops, pts):
                l[lay].uv = (rect[0] + (rect[2] - rect[0]) * (a - a0) / max(a1 - a0, 1e-6),
                             rect[1] + (rect[3] - rect[1]) * (b - b0) / max(b1 - b0, 1e-6))
    bm.to_mesh(me); bm.free()
    me.materials.append(mat(m, glow))
    ob = bpy.data.objects.new(name, me); ob['sun'] = sun
    ob.location = loc; ob.rotation_euler = (tilt or (0, 0, 0))[:2] + (rot,) if tilt else (0, 0, rot)
    coll(CUR[0]).objects.link(ob); return ob


def box(name, c, s, m, skip='', **kw):
    """c 중심, s 크기. skip 에 'b'(아래) 't' 'f'(-y) 'k'(+y) 'l'(-x) 'r'(+x) 넣으면 그 면 생략."""
    x, y, z = (v / 2 for v in s)
    V = [(-x, -y, -z), (x, -y, -z), (x, y, -z), (-x, y, -z), (-x, -y, z), (x, -y, z), (x, y, z), (-x, y, z)]
    F = {'b': (0, 3, 2, 1), 't': (4, 5, 6, 7), 'f': (0, 1, 5, 4), 'k': (2, 3, 7, 6), 'l': (3, 0, 4, 7), 'r': (1, 2, 6, 5)}
    kw.setdefault('loc', c)
    return mk(name, V, [F[k] for k in 'btfklr' if k not in skip], m, **kw)


def quad(name, p, m, **kw):
    return mk(name, p, [(0, 1, 2, 3)], m, **kw)


def cyl(name, c, r, h, n, m, cap=None, axis='z', **kw):
    """n각 기둥. 옆면 uv = 둘레 0..1 × 높이 0..1. cap 재질이 있으면 위(축 +)뚜껑을 따로 붙인다."""
    V, F = [], []
    for i in range(n + 1):
        a = 2 * math.pi * i / n; V += [(r * math.cos(a), r * math.sin(a), -h / 2), (r * math.cos(a), r * math.sin(a), h / 2)]
    for i in range(n): F.append((2 * i, 2 * i + 2, 2 * i + 3, 2 * i + 1))
    R = {'z': Matrix(), 'x': Matrix.Rotation(math.pi / 2, 4, 'Y'), 'y': Matrix.Rotation(-math.pi / 2, 4, 'X')}[axis]
    V2 = [tuple(R @ Vector(v)) for v in V]
    kw.setdefault('loc', c)
    me_ob = mk(name, V2, F, m, uv='world', **kw)
    bm = bmesh.new(); bm.from_mesh(me_ob.data); lay = bm.loops.layers.uv.active
    for fc in bm.faces:
        i = fc.index
        for l in fc.loops:
            j = l.vert.index; l[lay].uv = ((j // 2) / n, (j % 2))
    bm.to_mesh(me_ob.data); bm.free()
    obs = [me_ob]
    for sgn in ((1,) if cap else ()):
        cv = [tuple(R @ Vector((r * math.cos(2 * math.pi * i / n), r * math.sin(2 * math.pi * i / n), sgn * h / 2))) for i in range(n)]
        obs.append(mk(name + '_cap', cv, [tuple(range(n))], cap, uv='fit', **kw))
    return obs


def tri_count(cname):
    t = 0
    for ob in COLL[cname].objects:
        ob.data.calc_loop_triangles(); t += len(ob.data.loop_triangles)
    return t


# ---------------------------------------------------------------- 정비소 (입구 y=0 이 -y 쪽, 깊이 12m)
W2, D, HW, HA = 5.0, 12.0, 3.2, 5.6


def arch(n=6, w=W2, y=0.0, lift=0.0):
    return [(w * math.cos(math.pi * i / n), y, HW + lift + (HA - HW) * math.sin(math.pi * i / n)) for i in range(n + 1)]


def garage():
    CUR[0] = 'garage'
    # 벽: 아래 1.2m 짙은 판자, 위 함석. 바깥 = 반시계.
    for sx in (-1, 1):
        x = sx * W2
        for z0, z1, m in ((0, 1.2, 'planks_dark'), (1.2, HW, 'tin_wall')):
            p = [(x, 0, z0), (x, D, z0), (x, D, z1), (x, 0, z1)]
            quad(f'wall_{sx}_{m}', p if sx > 0 else p[::-1], m)
    quad('win_L', [(-W2 - .02, 6.5, 1.5), (-W2 - .02, 4.5, 1.5), (-W2 - .02, 4.5, 2.5), (-W2 - .02, 6.5, 2.5)], 'window', uv='fit')
    quad('win_R', [(W2 + .02, 7.5, 1.5), (W2 + .02, 9.5, 1.5), (W2 + .02, 9.5, 2.5), (W2 + .02, 7.5, 2.5)], 'window', uv='fit')
    quad('back', [(W2, D, 0), (-W2, D, 0), (-W2, D, HW), (W2, D, HW)], 'planks')
    a = arch(y=D); mk('back_gable', [(0, D, HW)] + a, [(0, i + 2, i + 1) for i in range(6)], 'planks')
    for sx in (-1, 1):  # 입구 양옆 벽
        p = [(sx * 3.6, 0, 0), (sx * W2, 0, 0), (sx * W2, 0, HW), (sx * 3.6, 0, HW)]
        quad(f'front_{sx}', p if sx < 0 else p[::-1], 'planks')
    a = arch(y=0); mk('front_gable', [(0, 0, HW)] + a, [(0, i + 1, i + 2) for i in range(6)], 'tin_wall')
    box('header', (0, -.08, HW - .12), (7.4, .16, .24), 'wood', skip='b')
    # 둥근 함석 지붕(6면) + 앞 테두리 갈빗대
    a0, a1 = arch(y=-.6, w=W2 + .35, lift=-.05), arch(y=D + .4, w=W2 + .35, lift=-.05)
    V = a0 + a1; mk('roof', V, [(i + 1, i, 7 + i, 8 + i) for i in range(6)], 'tin_roof')
    ra = arch(y=-.62, w=W2 + .38, lift=-.02)
    for i in range(6):
        p, q = Vector(ra[i]), Vector(ra[i + 1]); d = q - p
        box(f'rib{i}', (p + q) / 2, (d.length + .05, .14, .14), 'wood', skip='lr', rot=0,
            tilt=(0, -math.atan2(d.z, d.x), 0))
    for sx in (-1, 1):
        for y in (0, D):
            box(f'post_{sx}_{y}', (sx * (W2 + .05), y, HW / 2), (.18, .18, HW), 'wood', skip='bt')
    # 미닫이문: 왼쪽 반쯤 열림, 오른쪽 활짝 — 대각 버팀대
    for nm, cx in (('doorL', -4.3), ('doorR', 4.45)):
        box(nm, (cx, -.2, 1.5), (1.9, .08, 3.0), 'planks_dark', skip='b')
        box(nm + '_brace', (cx, -.26, 1.5), (2.9, .05, .14), 'wood', skip='bklr', tilt=(0, -math.atan2(2.6, 1.6), 0))
    box('rail', (0, -.22, 3.06), (10.6, .06, .08), 'metal', skip='b')
    # 덧댄 판자(바깥 벽에 붙인 새 판자 조각)
    for i, (c, s) in enumerate((((-W2 - .03, 2.0, .7), (.03, .9, .3)), ((-W2 - .03, 9.2, 2.1), (.03, .7, .25)),
                                ((W2 + .03, 3.4, .5), (.03, 1.1, .28)), ((-4.3, -.03, 2.2), (.6, .03, .25)))):
        box(f'patch{i}', c, s, 'crate', skip='b')
    quad('floor', [(-W2, 0, .01), (W2, 0, .01), (W2, D, .01), (-W2, D, .01)], 'floor', sun=.35)
    quad('apron', [(-W2 - 1, -3.5, .01), (W2 + 1, -3.5, .01), (W2 + 1, 0, .01), (-W2 - 1, 0, .01)], 'floor')

    IN = dict(sun=.3)
    # 작업대(오른쪽 벽) + 공구 판 + 클램프 등
    box('bench', (4.35, 4.8, .92), (.8, 3.4, .08), 'wood', **IN)
    for y in (3.2, 6.4):
        for x in (4.05, 4.65): box(f'leg{x}{y}', (x, y, .45), (.08, .08, .9), 'wood', skip='bt', **IN)
    box('bench_shelf', (4.35, 4.8, .25), (.7, 3.2, .05), 'wood', skip='b', **IN)
    quad('pegboard', [(W2 - .04, 6.4, 1.3), (W2 - .04, 3.2, 1.3), (W2 - .04, 3.2, 2.6), (W2 - .04, 6.4, 2.6)], 'pegboard', uv='fit', **IN)
    box('vise', (4.2, 3.6, 1.04), (.18, .26, .16), 'metal', skip='b', **IN)
    box('toolbox', (4.4, 5.7, 1.06), (.35, .5, .2), 'drum_red', uv='fit', rect=(0, .25, .5, .7), skip='b', **IN)
    # 선반 두 개(뒷벽) + 물건
    for sx in (-1, 1):
        cx = sx * 3.2
        for dx in (-1.1, 1.1):
            for dy in (-.25, .25): box(f'shpost{sx}{dx}{dy}', (cx + dx, D - .4 + dy, 1.1), (.06, .06, 2.2), 'wood', skip='bt', **IN)
        for z in (.4, 1.15, 1.9): box(f'shelf{sx}{z}', (cx, D - .4, z), (2.3, .6, .05), 'wood', skip='b', **IN)
        for j, (dx, z, cell, s) in enumerate(((-.7, .43, 0, .3), (-.25, .43, 3, .4), (.5, .43, 1, .45), (-.6, 1.18, 2, .35),
                                               (.1, 1.18, 0, .25), (.7, 1.93, 3, .3), (-.3, 1.93, 1, .3))):
            box(f'part{sx}{j}', (cx + dx * sx, D - .45, z + s / 2), (s * 1.2, .4, s), 'parts', uv='fit',
                rect=(cell / 4, 0, (cell + 1) / 4, 1), skip='b', **IN)
    # 타이어 더미(안 2 + 밖 1), 기댄 타이어
    def tire_stack(nm, x, y, n, sun, rot=0):
        for k in range(n):
            cyl(f'{nm}{k}', (x + (.03 if k % 2 else 0), y, .11 + .22 * k), .33, .22, 8, 'tire',
                cap='drum_top' if k == n - 1 else None, sun=sun, rot=rot + k * .3)
    tire_stack('tA', -4.2, 1.4, 4, .3); tire_stack('tB', -4.25, 2.3, 3, .3); tire_stack('tO', -5.7, -1.2, 3, 1.0)
    cyl('tLean', (-4.75, 3.3, .34), .33, .22, 8, 'tire', axis='x', sun=.3, tilt=(0, .25, 0))
    # 기름 드럼(안 3 + 밖 1)
    for i, (x, y, m, sun) in enumerate(((-4.3, 6.2, 'drum_red', .3), (-4.3, 7.0, 'drum_blue', .3), (-3.6, 6.6, 'drum_red', .3), (5.75, -.9, 'drum_blue', 1.0))):
        cyl(f'drum{i}', (x, y, .45), .3, .9, 8, m, cap='drum_top', sun=sun, rot=i * .7)
    # 작업등(지붕에 매단 갓 + 전구) 2개
    for i, (x, y) in enumerate(((0, 5.0), (2.4, 8.5))):
        box(f'cable{i}', (x, y, 4.6), (.02, .02, 1.6), 'metal', skip='bt', **IN)
        V = [(0, 0, .25)] + [(.32 * math.cos(2 * math.pi * k / 6), .32 * math.sin(2 * math.pi * k / 6), 0) for k in range(6)]
        mk(f'shade{i}', V, [(0, k + 1, (k + 1) % 6 + 1) for k in range(6)], 'lamp', uv='fit', loc=(x, y, 3.55), **IN)
        box(f'bulb{i}', (x, y, 3.52), (.12, .12, .1), 'lamp', uv='fit', rect=(.3, 0, .6, .2), glow=True)
    # 포스터·기록 판·빈 칸 보드
    quad('posterA', [(-W2 + .04, 9.6, 1.4), (-W2 + .04, 8.6, 1.4), (-W2 + .04, 8.6, 2.9), (-W2 + .04, 9.6, 2.9)][::-1], 'poster_a', uv='fit', **IN)
    quad('posterB', [(-4.95, -.03, 1.3), (-3.95, -.03, 1.3), (-3.95, -.03, 2.8), (-4.95, -.03, 2.8)], 'poster_b', uv='fit')
    quad('posterC', [(-W2 + .04, 2.6, 1.6), (-W2 + .04, 1.9, 1.6), (-W2 + .04, 1.9, 2.65), (-W2 + .04, 2.6, 2.65)][::-1], 'poster_a', uv='fit', **IN)
    quad('record', [(-W2 + .04, 7.6, 1.3), (-W2 + .04, 5.6, 1.3), (-W2 + .04, 5.6, 2.3), (-W2 + .04, 7.6, 2.3)][::-1], 'record', uv='fit', **IN)
    quad('slots', [(1.0, D - .05, 2.5), (-1.0, D - .05, 2.5), (-1.0, D - .05, 3.5), (1.0, D - .05, 3.5)], 'slots', uv='fit', **IN)
    box('slots_frame', (0, D - .07, 2.45), (2.2, .1, .08), 'wood', skip='b', **IN)
    # 상자 더미(입구 오른쪽 밖) + 사다리(왼쪽 벽 안)
    box('crate1', (4.3, -1.2, .3), (.6, .6, .6), 'crate', uv='fit', skip='b', rot=.15)
    box('crate2', (4.35, -1.15, .8), (.45, .45, .4), 'crate', uv='fit', skip='b', rot=-.2)
    for x in (-4.85, -4.35):
        box(f'lad{x}', (x, 10.6, 1.4), (.06, .06, 2.9), 'wood', skip='bt', sun=.3, tilt=(-.18, 0, 0))
    for k in range(5):
        box(f'rung{k}', (-4.6, 10.6 - .1 - k * .1, .35 + k * .55), (.5, .04, .04), 'wood', skip='lr', sun=.3)
    # 가운데 작업대 + 엔진 덩어리, 체인 도르래, 뒤쪽 타이어·상자 더미, 기름통
    box('table', (-.6, 7.2, .8), (1.6, .9, .08), 'wood', **IN)
    for dx in (-.7, .7):
        box(f'trest{dx}', (-.6 + dx, 7.2, .4), (.08, .8, .8), 'wood', skip='bt', **IN)
    box('engine', (-.8, 7.2, 1.04), (.6, .45, .4), 'metal', skip='b', **IN)
    cyl('engine_fly', (-.4, 7.2, 1.04), .2, .08, 6, 'drum_top', axis='x', **IN)
    box('part_box', (-.05, 7.0, .95), (.35, .3, .22), 'parts', uv='fit', rect=(.75, 0, 1, 1), skip='b', **IN)
    box('hoist_beam', (-.6, 7.2, 4.2), (3.0, .12, .12), 'wood', **IN)
    box('chain', (-.8, 7.2, 3.0), (.03, .03, 2.3), 'metal', skip='bt', **IN)
    box('hook', (-.8, 7.2, 1.82), (.12, .04, .14), 'metal', **IN)
    tire_stack('tC', 1.6, 11.2, 4, .3); tire_stack('tD', 2.4, 11.4, 2, .3); tire_stack('tE', -1.8, 11.3, 3, .3)
    for i, (x, y, z, s) in enumerate(((-3.9, 9.4, .35, .7), (-3.9, 9.4, .95, .5), (-3.2, 9.6, .3, .6), (3.6, 10.3, .3, .6))):
        box(f'icrate{i}', (x, y, z), (s, s, s), 'crate', uv='fit', skip='b', rot=i * .4, **IN)
    for i, (x, y, m) in enumerate(((3.9, 7.4, 'drum_red'), (4.1, 7.8, 'drum_blue'), (3.7, 8.0, 'drum_red'), (-2.4, 1.0, 'drum_blue'))):
        cyl(f'can{i}', (x, y, .2), .13, .4, 6, m, cap='drum_top', sun=.3, rot=i)
    cyl('tWall', (W2 - .2, 9.0, 2.1), .33, .22, 8, 'tire', axis='x', sun=.3)


# ---------------------------------------------------------------- 고카트 (로컬 +y = 앞)
def kart(loc, rot):
    CUR[0] = 'kart'; obs0 = set(bpy.data.objects)
    box('k_pan', (0, 0, .12), (.62, 1.3, .05), 'metal', skip='b')
    for sx in (-1, 1):
        box(f'k_pod{sx}', (sx * .39, -.05, .17), (.14, .72, .14), 'kart_body', skip='b')
        quad(f'k_num{sx}', [(sx * .465, .15 * -sx, .11), (sx * .465, -.15 * -sx, .11), (sx * .465, -.15 * -sx, .23), (sx * .465, .15 * -sx, .23)][::(1 if sx > 0 else 1)], 'sticker', uv='fit')
    V = [(-.3, .3, .12), (.3, .3, .12), (.25, .78, .1), (-.25, .78, .1), (-.3, .3, .4), (.3, .3, .4), (.22, .78, .22), (-.22, .78, .22)]
    mk('k_nose', V, [(0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7), (4, 5, 6, 7)], 'kart_body')
    quad('k_numF', [(-.1, .787, .12), (.1, .787, .12), (.1, .787, .21), (-.1, .787, .21)], 'sticker', uv='fit')
    box('k_bumper', (0, .86, .14), (.82, .06, .06), 'metal')
    box('k_rbumper', (0, -.74, .16), (.86, .06, .06), 'metal')
    box('k_seat', (0, -.18, .22), (.42, .38, .1), 'seat', uv='fit', skip='b')
    box('k_back', (0, -.38, .42), (.42, .08, .42), 'seat', uv='fit', tilt=(-.25, 0, 0))
    box('k_col', (0, .28, .38), (.04, .04, .4), 'metal', skip='bt', tilt=(.7, 0, 0))
    cyl('k_wheel', (0, .17, .52), .15, .03, 6, 'metal', cap='seat', tilt=(.9, 0, 0))
    for (x, y, r, w) in ((.42, .55, .13, .12), (-.42, .55, .13, .12), (.44, -.5, .15, .16), (-.44, -.5, .15, .16)):
        cyl(f'k_tire{x}{y}', (x, y, r), r, w, 8, 'tire', axis='x', cap='drum_top')
        if x < 0: bpy.data.objects[f'k_tire{x}{y}_cap'].location.x -= w  # 바깥쪽 뚜껑
    box('k_axle', (0, -.5, .15), (.9, .05, .05), 'metal')
    box('k_engine', (.22, -.55, .3), (.2, .22, .2), 'metal', skip='b')
    cyl('k_exh', (.36, -.62, .3), .04, .3, 6, 'metal', axis='y')
    box('k_rack', (0, -.62, .3), (.56, .36, .04), 'metal', skip='b')
    box('k_crate', (0, -.62, .48), (.5, .34, .32), 'crate', uv='fit', skip='b', rot=.04)
    for x in (-.13, .13):
        box(f'k_strap{x}', (x, -.62, .48), (.05, .36, .345), 'strap', uv='fit', skip='bfk')
    quad('k_plate', [(.1, -.792, .45), (-.1, -.792, .45), (-.1, -.792, .57), (.1, -.792, .57)], 'sticker', uv='fit')
    rootM = Matrix.Translation(loc) @ Matrix.Rotation(rot, 4, 'Z')
    bpy.context.view_layer.update()  # matrix_world 를 location/rotation 으로 갱신한 뒤 곱한다
    for ob in set(bpy.data.objects) - obs0:
        ob.matrix_world = rootM @ ob.matrix_world


def environment():
    CUR[0] = 'env'
    quad('ground', [(-40, -40, 0), (40, -40, 0), (40, 22, 0), (-40, 22, 0)], 'ground')
    quad('dirt', [(-8, -7, .005), (8, -7, .005), (8, 0, .005), (-8, 0, .005)], 'dirt')
    quad('path', [(-2.5, -40, .005), (3.5, -40, .005), (3.5, -7, .005), (-2.5, -7, .005)], 'dirt')
    box('quay', (0, 22.4, -.8), (80, .8, 1.6), 'stone', skip='b')
    quad('sea', [(-400, 22, -1.2), (400, 22, -1.2), (400, 600, -1.2), (-400, 600, -1.2)], 'sea')
    for i, (x, y, s) in enumerate(((-9, 5, 1.0), (-11.5, 10, 1.3), (-8.5, 14, .9), (9.5, 11, 1.2), (12, 4, .9), (-13, -4, .8))):
        box(f'trunk{i}', (x, y, 1.2 * s), (.3 * s, .3 * s, 2.4 * s), 'wood', skip='bt')
        for j, (dx, dy, z, r) in enumerate(((0, 0, 3.0, 1.9), (1.1, .5, 2.6, 1.3), (-.8, -.7, 3.5, 1.2))):  # 둥근 덩이 셋(올리브·떡갈 꼴)
            V = [(0, 0, -.55 * r * s)] + [(r * s * math.cos(k * math.pi / 3 + j), r * s * math.sin(k * math.pi / 3 + j), .1 * r * s * (k % 2)) for k in range(6)] + [(.15 * r, 0, .8 * r * s)]
            F = [(0, (k + 1) % 6 + 1, k + 1) for k in range(6)] + [(7, k + 1, (k + 1) % 6 + 1) for k in range(6)]
            mk(f'crown{i}{j}', V, F, 'leaf', loc=(x + dx * s, y + dy * s, z * s))


def scene_setup():
    s = bpy.context.scene; s.render.engine = 'BLENDER_EEVEE'
    s.render.resolution_x, s.render.resolution_y = 960, 540; s.render.film_transparent = True
    s.view_settings.view_transform = 'Standard'; s.render.filter_size = .5
    try: s.eevee.taa_render_samples = 16
    except Exception: pass


def camera(name, loc, tgt, lens):
    c = bpy.data.cameras.new(name); c.lens = lens; c.clip_end = 1000
    ob = bpy.data.objects.new(name, c); bpy.context.scene.collection.objects.link(ob)
    ob.location = loc; ob.rotation_euler = (Vector(tgt) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
    return ob


KART_AT, KART_ROT = Vector((1.6, -3.0, 0)), math.radians(-18)  # 정비소로 들어가는 방향


def kv(dx, dy, dz):  # 카트 기준 상대 위치
    return tuple(KART_AT + Vector((dx, dy, dz)))


VIEWS = {
    'g1_front': ((0, -17, 2.0), (0, 2, 2.6), 26),
    'g2_34': ((-13.5, -13, 5.5), (0, 3, 2.0), 28),
    'g3_inside': ((3.4, .8, 1.6), (-2.5, 9.5, 1.4), 18),
    'k1_34': (kv(1.9, 1.6, 1.1), kv(0, 0, .3), 40),
    'k2_side': (kv(2.7, -.6, .7), kv(0, 0, .3), 40),
    'k3_rear': (kv(-1.5, -2.0, 1.2), kv(0, 0, .3), 40),
    'x1_game': (kv(-1.0, -6.6, 2.5), kv(.8, 6, 1.0), 30),
}


def main():
    for ob in list(bpy.data.objects): bpy.data.objects.remove(ob)
    scene_setup(); garage(); kart(KART_AT, KART_ROT); environment()
    meta = dict(garage_tris=tri_count('garage'), kart_tris=tri_count('kart'), env_tris=tri_count('env'), views={})
    os.makedirs(OUT, exist_ok=True)
    s = bpy.context.scene
    for k, (loc, tgt, lens) in VIEWS.items():
        cam = camera(k, loc, tgt, lens); s.camera = cam
        s.render.filepath = os.path.join(OUT, '_raw', k + '.png'); bpy.ops.render.render(write_still=True)
        meta['views'][k] = dict(loc=list(loc), tgt=list(tgt), lens=lens,
                                dist_to_kart=round((Vector(loc) - KART_AT).length, 2))
    json.dump(meta, open(os.path.join(OUT, 'render_meta.json'), 'w'), indent=1)
    os.makedirs(os.path.join(ROOT, 'art', 'models'), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT, 'art', 'models', 'opus_garage_kart_v1.blend'))
    print('META', json.dumps({k: v for k, v in meta.items() if k != 'views'}))


main()
