"""항구 심장 블록 손 조립 v0 (D-162) — 키트(harbor_kit.py)를 한 채씩 정한 표(SPEC)대로 놓는다.

자동으로 정하는 것 없음: 층수·벽색·지붕 꼴·지붕색·1층 패턴·위층 창 패턴·덧문색·굴뚝·홈통은 SPEC 에 채마다 손으로 적었다.
도움 함수가 하는 일은 계산뿐 — 벽 길이를 칸(3~4 m)으로 나누기, 붙은 벽(이웃과 맞닿음) 건너뛰기, 길·물가 쪽 면 찾기.

    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/harbor_block.py -- [--kit] [--views a,b1,b2]
"""
import bpy, colorsys, json, math, os, random, sys
from mathutils import Vector, Matrix
sys.path.insert(0, os.path.dirname(__file__))
import harbor_kit as K

ROOT = K.ROOT
OUT = os.path.join(ROOT, 'art', '_review', '2026-10-27-harbor-kit-v0')
MAP = json.load(open(os.path.join(ROOT, 'docs', 'map', 'town-v6osm.json'), encoding='utf8'))
BLOCK = json.load(open(os.path.join(OUT, 'block_assembly.json'), encoding='utf8'))
BX, BZ = BLOCK['block']['x'], BLOCK['block']['z']

# ---------------------------------------------------------------- 땅·바다 (물가선은 손으로)
_g = {(round(x), round(z)): y for x, z, y in MAP['terrain']['controlPoints']}
_st = MAP['terrain']['step']


def th(x, z):
    x0, z0 = math.floor(x / _st) * _st, math.floor(z / _st) * _st
    fx, fz = (x - x0) / _st, (z - z0) / _st
    v = [_g.get((x0 + i * _st, z0 + j * _st), 0.0) for j in (0, 1) for i in (0, 1)]
    return (v[0] * (1 - fx) + v[1] * fx) * (1 - fz) + (v[2] * (1 - fx) + v[3] * fx) * fz


PIERS = [(137.0, 163.5, -296.0, -263.0), (131.0, 154.0, -323.5, -303.5)]   # 창고 부두 · 남쪽 잔교집


def is_sea(x, z):
    if 60 <= x <= 260 and -420 <= z <= -200:       # 블록 둘레: 손으로 그은 물가선 x 137.5 / z -244
        if x >= 137.5 and z <= -244:
            return not any(a <= x <= b and c <= z <= d for a, b, c, d in PIERS)
        return False
    return th(x, z) <= 0


def ground(x, z):
    return max(th(x, z), 0.9)


# ---------------------------------------------------------------- 팔레트 (Look 6 규칙 2)
WALL = dict(cream='#e0cfb0', ochre='#d2a576', orange='#c98a5e', lime='#c8bea6', blue='#a9b7b4', pink='#d4a69a')
ROOF = dict(terra='#b46a52', red='#a85c5e', copper='#4f6a6a')
SHUT = ['#6f8a78', '#7f97a0', '#7a5e48', '#8c9a7a']
DOOR = ['#6b5a48', '#4f6a6a', '#7a4a3e', '#5d6b52']


def jit(hexc, seed, dh=8, dv=0.10):
    """구역 팔레트 흔들림: 색상 ±dh°, 명도 ±dv (MPB _Tint 와 같은 일). 선형 RGB 로 돌려줌."""
    r = random.Random(seed)
    c = [int(hexc[i:i + 2], 16) / 255 for i in (1, 3, 5)]
    h, s, v = colorsys.rgb_to_hsv(*c)
    h = (h + r.uniform(-dh, dh) / 360) % 1; v = min(1, v * (1 + r.uniform(-dv, dv)))
    c = colorsys.hsv_to_rgb(h, s, v)
    return tuple(x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c)


# ---------------------------------------------------------------- 채마다 손으로 정한 표
# id: (층, 벽색, 지붕, 지붕색, 1층 패턴, 위층 창 패턴, 덧문색 번호, 덧붙임)
#  1층: A 가게+차양 / S 가게 / D 문 / W 창 / G 창고문 / - 민벽     위층: O 열린 창 / C 닫힌 창 / P 화분 창 / B 발코니 / - 민벽
#  덧붙임: ch 굴뚝(여러 번 쓰면 여러 개) · dp 홈통 · pier 부두 위
SPEC = {
    1: (3, 'cream', 'hip', 'terra', 'ASAD', 'OPCO', 0, 'ch dp'),      # 북서 큰 ㄱ자 — 길 모서리 가게
    2: (3, 'orange', 'gable', 'red', 'DW', 'COOC', 1, 'ch'),
    3: (2, 'lime', 'gable', 'terra', 'AA', 'BO', 0, 'dp'),           # 물가 북쪽 끝 — 바다 보는 가게
    4: (2, 'ochre', 'gable', 'terra', 'D', 'OC', 2, ''),
    5: (3, 'cream', 'gable', 'red', 'W', 'PO', 1, 'ch'),             # 4 보다 한 층 높게
    6: (3, 'orange', 'hip', 'terra', 'SAS', 'OCOP', 3, 'ch'),
    7: (1, 'lime', 'flat', 'terra', '-', '', 0, ''),                 # 2 m² 헛간
    8: (2, 'cream', 'gable', 'copper', 'DW', 'OPC', 1, 'ch'),        # 10채 중 1채 — 동판 청록 지붕
    9: (2, 'ochre', 'gable', 'terra', 'D', 'CO', 0, ''),
    10: (3, 'cream', 'hip', 'red', 'WDW', 'OOC', 2, 'ch'),
    11: (2, 'lime', 'gable', 'terra', 'DW', 'PO', 3, ''),
    12: (3, 'ochre', 'hip', 'terra', 'AAAA', 'OBOP', 0, 'ch dp'),    # 물가 모퉁이 — 차양 줄
    13: (2, 'orange', 'gable', 'terra', 'SDS', 'COP', 1, ''),
    14: (3, 'cream', 'hip', 'red', 'AASA', 'BOPO', 0, 'ch ch dp'),   # 물가 큰 집(여관 같은)
    15: (1, 'cream', 'flat', 'terra', '-', '', 0, ''),
    16: (1, 'lime', 'flat', 'terra', '-', '', 0, ''),
    17: (2, 'ochre', 'hip', 'red', 'WDWW', 'OCOC', 2, 'ch'),         # 안쪽 큰 덩어리 — 낮게 두 층
    18: (1, 'orange', 'flat', 'terra', '-', '', 0, ''),
    19: (3, 'lime', 'gable', 'terra', 'D', 'OP', 1, ''),
    20: (2, 'orange', 'gable', 'red', 'SD', 'CO', 0, ''),
    21: (1, 'cream', 'gable', 'terra', 'D', '', 0, ''),
    22: (2, 'cream', 'hip', 'terra', 'AAAA', 'OBPO', 1, 'ch dp'),    # 부두 앞 — 찻집 거리 같은 1층
    23: (1, 'lime', 'flat', 'terra', '-', '', 0, 'pier'),
    24: (1, 'blue', 'gable', 'terra', 'GG', '', 0, 'pier'),          # 부두 창고 — 다른 계열 벽(1/10)
    25: (3, 'orange', 'gable', 'terra', 'DWS', 'OOPC', 3, 'ch'),
    26: (2, 'ochre', 'hip', 'red', 'SA', 'PO', 0, ''),
    27: (1, 'lime', 'gable', 'terra', 'D', '', 0, ''),
    28: (2, 'lime', 'gable', 'red', 'GDGG', 'C-OC', 1, 'ch pier'),   # 부두 큰 창고
    29: (2, 'cream', 'gable', 'terra', 'DW', 'OC', 2, ''),
    30: (2, 'cream', 'hip', 'terra', 'AAS', 'PBP', 0, 'ch'),         # 항구 찻집 자리 — art(Codex) 손 모델로 바뀔 자리
    31: (3, 'ochre', 'gable', 'red', 'D', 'OC', 1, ''),
    32: (1, 'cream', 'flat', 'terra', '-', '', 0, ''),
    33: (2, 'orange', 'gable', 'terra', 'DW', 'PO', 3, ''),
    34: (1, 'pink', 'gable', 'terra', 'S', '', 0, ''),               # 키오스크 — 분홍(1/10)
    35: (2, 'orange', 'hip', 'terra', 'ADA', 'BOOP', 0, 'ch dp pier'),  # 남쪽 잔교 집 — 바다 쪽 발코니
    36: (1, 'lime', 'flat', 'terra', '-', '', 0, ''),
}
TREES = [(119, -301, 1.0), (126, -293, 0.8), (112, -317, 1.1), (99, -307, 0.9), (131, -224, 0.8), (96, -313, 0.7), (122, -312, 1.2)]
BOATS = [(171, -256, 12), (167, -301, -25), (146, -299, 84), (183, -282, 40)]

# ---------------------------------------------------------------- 기하 도움
def signed_area(p):
    return sum(p[i - 1][0] * p[i][1] - p[i][0] * p[i - 1][1] for i in range(len(p))) / 2


def inside(pt, poly):
    x, y = pt; c = False
    for i in range(len(poly)):
        (x1, y1), (x2, y2) = poly[i - 1], poly[i]
        if (y1 > y) != (y2 > y) and x < x1 + (y - y1) * (x2 - x1) / (y2 - y1): c = not c
    return c


def seg_dist(p, a, b):
    ax, ay = a; bx, by = b; px, py = p
    dx, dy = bx - ax, by - ay; L = dx * dx + dy * dy
    t = 0 if L == 0 else max(0, min(1, ((px - ax) * dx + (py - ay) * dy) / L))
    return math.hypot(px - ax - t * dx, py - ay - t * dy)


ROAD_SEGS = [((r['knots'][i][0], r['knots'][i][1]), (r['knots'][i + 1][0], r['knots'][i + 1][1]), r['width'])
             for r in MAP['roads'] for i in range(len(r['knots']) - 1)
             if 0 <= r['knots'][i][0] <= 260 and -400 <= r['knots'][i][1] <= -160]


def faces_open(pt):
    """이 점이 길(폭/2+4 m 안) 또는 물가(바다·부두 가장자리 6 m 안) 쪽인가."""
    if any(seg_dist(pt, a, b) < w / 2 + 4 for a, b, w in ROAD_SEGS): return True
    return any(is_sea(pt[0] + dx, pt[1] + dz) for dx, dz in ((6, 0), (-6, 0), (0, 6), (0, -6)))


def obb(poly):
    best = None
    n = len(poly)
    for i in range(n):
        (x1, y1), (x2, y2) = poly[i - 1], poly[i]
        L = math.hypot(x2 - x1, y2 - y1)
        if L < 1e-6: continue
        u = ((x2 - x1) / L, (y2 - y1) / L); v = (-u[1], u[0])
        us = [x * u[0] + y * u[1] for x, y in poly]; vs = [x * v[0] + y * v[1] for x, y in poly]
        A = (max(us) - min(us)) * (max(vs) - min(vs))
        if best is None or A < best[0]:
            best = (A, u, v, (max(us) + min(us)) / 2, (max(vs) + min(vs)) / 2, (max(us) - min(us)) / 2, (max(vs) - min(vs)) / 2)
    A, u, v, cu, cv, a, b = best
    if b > a: u, v, a, b, cu, cv = v, (-v[0], -v[1]), b, a, cv, -cu
    c = (cu * u[0] + cv * (-u[1]) if False else 0, 0)
    cx = cu * u[0] + cv * v[0]; cy = cu * u[1] + cv * v[1]
    return Vector((cx, cy)), Vector(u), Vector(v), a, b


# ---------------------------------------------------------------- 놓기
COLL = None
KIT = {}


def put(name, M, props):
    ob = KIT[name].copy()
    ob.matrix_world = M
    for k, v in props.items(): ob[k] = v
    COLL.objects.link(ob)
    return ob


def edge_frame(p, q, z):
    e = Vector((q[0] - p[0], q[1] - p[1])); L = e.length; e.normalize()
    return Matrix.Translation((p[0], p[1], z)) @ Matrix.Rotation(math.atan2(e.y, e.x), 4, 'Z') @ Matrix.Translation((0, -0.03, 0)), L


def S(sx=1, sy=1, sz=1):
    m = Matrix.Identity(4); m[0][0], m[1][1], m[2][2] = sx, sy, sz; return m


def mass(name, poly, z0, z1, props, cap=False):
    p = K.Part(name)
    for i in range(len(poly)):
        (x1, y1), (x2, y2) = poly[i - 1], poly[i]
        p.quad([(x1, y1, z0), (x2, y2, z0), (x2, y2, z1), (x1, y1, z1)], 'plaster')
    if cap: p.quad([(x, y, z1) for x, y in poly], 'pave')
    ob = p.build(None, COLL)
    for k, v in props.items(): ob[k] = v
    return ob


def roof(name, poly, H, kind, props, pitch=24, pid=0):
    """지붕 면은 바닥 사각(OBB) 치수로 짓고, 용마루·박공 끝·처마는 키트 조각을 늘려 놓는다."""
    c, u, v, a, b = obb(poly)
    t = math.tan(math.radians(pitch)); o = 0.45; og = 0.3
    W = lambda U, V, Z: (c.x + u.x * U + v.x * V, c.y + u.y * U + v.y * V, Z)
    p = K.Part(name)
    ez, rz = H - o * t, H + b * t
    caps = []
    if kind == 'gable':
        for s in (1, -1):
            q = [W(-(a + og), s * (b + o), ez), W(a + og, s * (b + o), ez), W(a + og, 0, rz), W(-(a + og), 0, rz)]
            sl = (b + o) / math.cos(math.radians(pitch))
            uv = [(0, 0), ((2 * a + 2 * og) / 4, 0), ((2 * a + 2 * og) / 4, sl / 4), (0, sl / 4)]
            if s < 0: q = q[::-1]; uv = uv[::-1]
            p.quad(q, 'roof', uv)
        for s in (1, -1):   # 박공 삼각 벽
            tri = [W(s * a, -b, H), W(s * a, b, H), W(s * a, 0, rz - 0.05)]
            if s < 0: tri = tri[::-1]
            p.quad(tri, 'plaster')
        caps.append((W(-(a + og), 0, rz), W(a + og, 0, rz), 'ridge'))
        for s in (1, -1):
            for sv in (1, -1):
                caps.append((W(s * (a + og), sv * (b + o), ez), W(s * (a + og), 0, rz), 'verge'))
        roof_z = lambda U, V: H + (b - abs(V)) * t
    elif kind == 'hip':
        r = max(0.0, a - b)
        E = [W(-(a + o), -(b + o), ez), W(a + o, -(b + o), ez), W(a + o, b + o, ez), W(-(a + o), b + o, ez)]
        R0, R1 = W(-r, 0, rz), W(r, 0, rz)
        sl = (b + o) / math.cos(math.radians(pitch))
        p.quad([E[0], E[1], R1, R0], 'roof', [(0, 0), ((2 * a + 2 * o) / 4, 0), ((a + o + r) / 4, sl / 4), ((a + o - r) / 4, sl / 4)])
        p.quad([E[2], E[3], R0, R1], 'roof', [(0, 0), ((2 * a + 2 * o) / 4, 0), ((a + o + r) / 4, sl / 4), ((a + o - r) / 4, sl / 4)])
        p.quad([E[1], E[2], R1], 'roof', [(0, 0), ((2 * b + 2 * o) / 4, 0), ((b + o) / 4, sl / 4)])
        p.quad([E[3], E[0], R0], 'roof', [(0, 0), ((2 * b + 2 * o) / 4, 0), ((b + o) / 4, sl / 4)])
        if r > 0.05: caps.append((R0, R1, 'ridge'))
        for e_, rr in ((E[0], R0), (E[3], R0), (E[1], R1), (E[2], R1)): caps.append((e_, rr, 'ridge'))
        roof_z = lambda U, V: H + min(b - abs(V), a - abs(U)) * t
    else:
        p.quad([(x, y, H) for x, y in poly], 'pave')
        roof_z = lambda U, V: H
    # 처마 밑면(짙은 그늘): 지붕 면을 0.16 내려 뒤집은 것
    nface = len(p.f)
    for fi in range(nface):
        if p.mats[p.fm[fi]] != 'roof': continue
        pts = [tuple(p.v[k] - Vector((0, 0, 0.16))) for k in p.f[fi]][::-1]
        p.quad(pts, 'eave', [(0, 0)] * len(pts))
    ob = p.build(None, COLL)
    for k, vv in props.items(): ob[k] = vv
    for A_, B_, kn in caps:   # 늘린 키트 조각: x 축 = A→B
        A_, B_ = Vector(A_), Vector(B_); d = B_ - A_; L = d.length
        x = d.normalized(); y = Vector((0, 0, 1)).cross(x)
        y = y.normalized() if y.length > 1e-6 else Vector((0, 1, 0)); z = x.cross(y)
        R = Matrix((x, y, z)).transposed().to_4x4()
        put(kn, Matrix.Translation(A_ + Vector((0, 0, 0.04))) @ R @ S(L, 1, 1), props)
    return (c, u, v, a, b, roof_z)


def building(bid, poly, spec):
    f, wall, rk, rc, gpat, upat, sh, flags = spec
    if signed_area(poly) < 0: poly = poly[::-1]
    if poly[0] == poly[-1]: poly = poly[:-1]
    cx = sum(x for x, _ in poly) / len(poly); cz = sum(z for _, z in poly) / len(poly)
    g = 0.9 if 'pier' in flags else ground(cx, cz)
    H = g + K.FH0 + (f - 1) * K.FH if f > 1 else g + (3.4 if gpat != '-' else 2.6)
    props = dict(wall_col=jit(WALL[wall], bid), roof_col=jit(ROOF[rc], bid * 7, dh=6, dv=0.12),
                 shut_col=jit(SHUT[sh], bid * 3, dh=5, dv=0.08), door_col=jit(DOOR[bid % 4], bid * 5, dh=5, dv=0.1))
    mass(f'B{bid}_mass', poly, g - 2.0, H, props, cap=(rk == 'flat'))
    others = [b['poly'] for b in MAP['buildings'] if b['poly'] is not None]
    n = len(poly)
    for i in range(n):
        p0, p1 = poly[i], poly[(i + 1) % n]
        M, L = edge_frame(p0, p1, g)
        if L < 0.8: continue
        e = ((p1[0] - p0[0]) / L, (p1[1] - p0[1]) / L); out = (e[1], -e[0])
        mid = ((p0[0] + p1[0]) / 2, (p0[1] + p1[1]) / 2)
        probe = (mid[0] + out[0] * 0.6, mid[1] + out[1] * 0.6)
        if any(inside(probe, o) for o in others if o is not poly and len(o) > 2 and not (abs(o[0][0] - poly[0][0]) < 1e-6 and abs(o[0][1] - poly[0][1]) < 1e-6)):
            continue   # 이웃과 붙은 벽 — 덩어리 벽만
        front = faces_open((mid[0] + out[0] * 3, mid[1] + out[1] * 3))
        nb = max(1, round(L / 3.4)); bw = L / nb
        gp = gpat if front else ('-W' if f > 1 else '-')
        up = upat if front else 'C-'
        small = L < 2.2
        # 1층
        z0h = (H - g) if f == 1 else K.FH0
        for k in range(nb):
            ch = '-' if small else gp[(k + i) % len(gp)]
            x0 = k * bw
            if ch in 'ASG':
                base = 'wall_shop_3' if bw < 3.5 else 'wall_shop_4'
                put(base, M @ Matrix.Translation((x0, 0, 0)) @ S(bw / (3 if bw < 3.5 else 4), 1, z0h / K.FH0), props)
                if ch == 'A':
                    put('awning', M @ Matrix.Translation((x0 + bw / 2, 0, 2.62)) @ S((bw - 0.6) / 2.6, 1, 1), props)
            else:
                base = ('wall_res_3' if (k + bid) % 2 else 'wall_res_3b') if bw < 3.5 else 'wall_res_4'
                put(base, M @ Matrix.Translation((x0, 0, 0)) @ S(bw / (3 if bw < 3.5 else 4), 1, z0h / K.FH), props)
                if ch == 'D': put('door', M @ Matrix.Translation((x0 + bw / 2, 0, 0)), props)
                if ch == 'W': put('window_closed', M @ Matrix.Translation((x0 + bw / 2, 0, 0.1)), props)
        # 위층
        for fl in range(1, f):
            z = K.FH0 + (fl - 1) * K.FH
            for k in range(nb):
                ch = '-' if small else up[(k + fl + i) % len(up)]
                x0 = k * bw
                base = ('wall_res_3b' if (k + fl + bid) % 2 else 'wall_res_3') if bw < 3.5 else 'wall_res_4'
                put(base, M @ Matrix.Translation((x0, 0, z)) @ S(bw / (3 if bw < 3.5 else 4), 1, 1), props)
                kn = dict(O='window_open', C='window_closed', P='window_pot', B='balcony').get(ch)
                if kn and bw >= 1.9: put(kn, M @ Matrix.Translation((x0 + bw / 2, 0, z + (0.0 if ch == 'B' else 0))), props)
        put('eave_band', M @ Matrix.Translation((0, 0, H - g)) @ S(L, 1, 1), props)
        put('corner', M @ S(1, 1, (H - g) / K.FH), props)
        if 'dp' in flags and front and L > 4 and not getattr(building, '_dp', {}).get(bid):
            put('downpipe', M @ Matrix.Translation((L - 0.35, 0, 0)) @ S(1, 1, (H - g) / 6.0), props)
            building.__dict__.setdefault('_dp', {})[bid] = True
    c, u, v, a, b, rz = roof(f'B{bid}_roof', poly, H, rk, props)
    for j in range(flags.split().count('ch')):   # 굴뚝: 지붕 위 손으로 고른 자리(긴 축 0.35a, 짧은 축 0.4b, 두 번째는 반대편)
        U, V = (0.35 * a, 0.4 * b) if j == 0 else (-0.5 * a, -0.3 * b)
        z = rz(U, V) - 0.35
        put('chimney', Matrix.Translation((c.x + u.x * U + v.x * V, c.y + u.y * U + v.y * V, z)) @ Matrix.Rotation(math.atan2(u.y, u.x), 4, 'Z'), props)
    return H


def context_building(i, poly):
    """블록 밖(배경 덩어리): 키트 없이 벽+모임지붕. 블록과 비교용 거리층(원경)."""
    if signed_area(poly) < 0: poly = poly[::-1]
    if poly[0] == poly[-1]: poly = poly[:-1]
    if len(poly) < 3: return
    cx = sum(x for x, _ in poly) / len(poly); cz = sum(z for _, z in poly) / len(poly)
    r = random.Random(i)
    g = ground(cx, cz); f = r.choice((2, 2, 3))
    H = g + 3.2 * f
    fam = r.choice(('cream', 'ochre', 'orange', 'lime', 'cream'))
    props = dict(wall_col=jit(WALL[fam], i + 999), roof_col=jit(ROOF[r.choice(('terra', 'terra', 'red'))], i + 77, dh=6, dv=0.12),
                 shut_col=jit(SHUT[0], 1), door_col=jit(DOOR[0], 1))
    mass(f'C{i}_mass', poly, g - 2, H, props)
    roof(f'C{i}_roof', poly, H, 'hip', props)


# ---------------------------------------------------------------- 땅·바다·길·나무·배
def terrain():
    p = K.Part('ground_town'); q = K.Part('ground_green')
    cents = [(sum(x for x, _ in b['poly']) / len(b['poly']), sum(z for _, z in b['poly']) / len(b['poly'])) for b in MAP['buildings']]
    st = 3.0
    X0, X1, Z0, Z1 = -150, 330, -480, -60
    nx, nz = int((X1 - X0) / st), int((Z1 - Z0) / st)
    hz = {}
    for i in range(nx + 1):
        for j in range(nz + 1):
            x, z = X0 + i * st, Z0 + j * st; hz[i, j] = ground(x, z)
    sea = {(i, j): is_sea(X0 + (i + .5) * st, Z0 + (j + .5) * st) for i in range(nx) for j in range(nz)}
    for i in range(nx):
        for j in range(nz):
            if sea[i, j]: continue
            x, z = X0 + (i + .5) * st, Z0 + (j + .5) * st
            town = any(abs(cx - x) < 16 and abs(cz - z) < 16 for cx, cz in cents)
            tgt = p if town else q
            pts = [(X0 + a * st, Z0 + b * st, hz[a, b]) for a, b in ((i, j), (i + 1, j), (i + 1, j + 1), (i, j + 1))]
            tgt.quad(pts, 'pave' if town else 'tree_lit')
            for (di, dj), (a, b) in (((1, 0), ((i + 1, j), (i + 1, j + 1))), ((-1, 0), ((i, j + 1), (i, j))),
                                     ((0, 1), ((i + 1, j + 1), (i, j + 1))), ((0, -1), ((i, j), (i + 1, j)))):
                if sea.get((i + di, j + dj)):   # 부두 벽(돌, 수면 아래 1.5 m 까지)
                    A_, B_ = (X0 + a[0] * st, Z0 + a[1] * st), (X0 + b[0] * st, Z0 + b[1] * st)
                    tgt.quad([(A_[0], A_[1], -1.5), (B_[0], B_[1], -1.5), (B_[0], B_[1], hz[b]), (A_[0], A_[1], hz[a])][::-1], 'stone')
    p.build(None, COLL); q.build(None, COLL)
    s = K.Part('sea'); s.quad([(-1200, -1500, 0), (1600, -1500, 0), (1600, 800, 0), (-1200, 800, 0)], 'sea'); s.build(None, COLL)
    r = K.Part('roads')
    for a, b, w in ROAD_SEGS:
        d = Vector((b[0] - a[0], b[1] - a[1]))
        if d.length < 1e-3: continue
        n_ = Vector((-d.y, d.x)).normalized() * (w / 2)
        za, zb = ground(*a) + 0.06, ground(*b) + 0.06
        r.quad([(a[0] - n_.x, a[1] - n_.y, za), (b[0] - n_.x, b[1] - n_.y, zb), (b[0] + n_.x, b[1] + n_.y, zb), (a[0] + n_.x, a[1] + n_.y, za)],
               'road', [(0, 0), (d.length / 4, 0), (d.length / 4, w / 4), (0, w / 4)])
    r.build(None, COLL)


def tree(x, z, s, seed):
    """나무 = 덩어리 4~5개(Look 6 규칙 10). 덩어리째 음영 — 양지 올리브-황토 / 중간 / 속은 그늘 틴트가 맡음."""
    p = K.Part(f'tree_{seed}'); r = random.Random(seed); g = ground(x, z)
    p.cyl((0, 0, 0), (0, 0, 2.2 * s), 0.18 * s, 'wood', seg=5)
    for k in range(5):
        c = (r.uniform(-1.2, 1.2) * s, r.uniform(-1.2, 1.2) * s, (2.6 + r.uniform(0, 1.6)) * s)
        K.blob(p, c, r.uniform(1.2, 1.7) * s, 'tree_mid' if k % 2 else 'tree_lit', seed=seed * 10 + k)
    ob = p.build(None, COLL); ob.location = (x, z, g); ob['door_col'] = jit('#6b5a48', 1)


def scene_block():
    global COLL
    COLL = bpy.data.collections.new('BLOCK'); bpy.context.scene.collection.children.link(COLL)
    hs = {}
    for b in BLOCK['buildings']:
        hs[b['id']] = building(b['id'], [tuple(q) for q in b['poly']], SPEC[b['id']])
    ids = {(round(b['poly'][0][0], 2), round(b['poly'][0][1], 2)) for b in BLOCK['buildings']}
    for i, b in enumerate(MAP['buildings']):
        cx = sum(x for x, _ in b['poly']) / len(b['poly']); cz = sum(z for _, z in b['poly']) / len(b['poly'])
        if (round(b['poly'][0][0], 2), round(b['poly'][0][1], 2)) in ids: continue
        if -60 <= cx <= 300 and -460 <= cz <= -100: context_building(i, [tuple(q) for q in b['poly']])
    terrain()
    for k, (x, z, s) in enumerate(TREES): tree(x, z, s, k + 1)
    for x, z, rot in BOATS:
        put('boat', Matrix.Translation((x, z, 0)) @ Matrix.Rotation(math.radians(rot), 4, 'Z'), {})
    for z in range(-292, -262, 6): put('bollard', Matrix.Translation((137.6, z, 0.9)), {})
    for x in range(140, 162, 6): put('bollard', Matrix.Translation((x, -295.3, 0.9)), {})
    return hs


VIEWS = {   # 이름: (카메라 위치, 바라보는 점, 렌즈) — 위치·높이는 손으로 고름
    'a_aerial': ((205.0, -340.0, 92.0), (120.0, -268.0, 4.0), 32),
    'b1_quay': ((134.6, -301.0, 2.5), (126.0, -238.0, 6.5), 26),
    'b2_street': None,   # 아래에서 길 위 점으로
}


def street_view():
    """마을 안 차길: 블록 안 길 점 가운데 (100,-268) 에 가장 가까운 점에서 길 방향으로."""
    best = min(((seg_dist((100, -268), a, b), a, b) for a, b, w in ROAD_SEGS if w >= 3), key=lambda t: t[0])
    _, a, b = best
    d = Vector((b[0] - a[0], b[1] - a[1])).normalized()
    pos = Vector((a[0], a[1])) - d * 2
    tgt = pos + d * 30
    return ((pos.x, pos.y, ground(pos.x, pos.y) + 1.7), (tgt.x, tgt.y, ground(tgt.x, tgt.y) + 3.5), 26)


def main():
    argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    for o in list(bpy.data.objects): bpy.data.objects.remove(o)
    sc = K.scene_setup((960, 540)); K.materials()
    K.mat('road', base='#b9ac93', tex='pave')
    kitc = bpy.data.collections.new('KIT'); sc.collection.children.link(kitc)
    KIT.update(K.build_kit(kitc))
    for ob in KIT.values():
        ob['wall_col'] = jit(WALL['ochre'], 3); ob['roof_col'] = jit(ROOF['terra'], 3)
        ob['shut_col'] = jit(SHUT[0], 3); ob['door_col'] = jit(DOOR[0], 3)
    os.makedirs(OUT, exist_ok=True)
    meta = {}
    if '--kit' in argv:
        kit_sheet(sc, kitc, meta)
    else:
        kitc.hide_render = True
        scene_block()
        VIEWS['b2_street'] = street_view()
        for name, (loc, tgt, lens) in VIEWS.items():
            cam = K.camera(loc, tgt, lens, name)
            K.render(os.path.join(OUT, f'{name}_raw.png'))
            meta[name] = dict(loc=loc, tgt=tgt, lens=lens, pitch=math.degrees(math.asin((Vector(tgt) - Vector(loc)).normalized().z)))
        tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in COLL.objects if o.type == 'MESH' and (o.name.startswith('B') or o.name.split('.')[0] in KIT))
        meta['block_tris'] = tris
        bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT, 'art', 'source', 'harbor-kit-v0', 'harbor_block_v0.blend'))
    json.dump(meta, open(os.path.join(OUT, 'render_meta.json' if '--kit' not in argv else 'kit_meta.json'), 'w'), indent=1)


def kit_sheet(sc, kitc, meta):
    """조각마다 따로 렌더(앞-왼쪽 위 25°), 칸 240×240."""
    sc.render.resolution_x = sc.render.resolution_y = 320
    d = os.path.join(OUT, '_kit'); os.makedirs(d, exist_ok=True)
    for ob in kitc.objects: ob.hide_render = True
    for name, ob in KIT.items():
        ob.hide_render = False
        bb = [ob.matrix_world @ Vector(c) for c in ob.bound_box]
        lo = Vector((min(v.x for v in bb), min(v.y for v in bb), min(v.z for v in bb)))
        hi = Vector((max(v.x for v in bb), max(v.y for v in bb), max(v.z for v in bb)))
        c = (lo + hi) / 2; r = max((hi - lo).length / 2, 0.5)
        dirv = Vector((-0.45, -1.0, 0.42)).normalized()
        K.camera(tuple(c + dirv * r * 3.1), tuple(c), 40, 'kc_' + name)
        K.render(os.path.join(d, name + '.png'))
        tris = sum(len(p.vertices) - 2 for p in ob.data.polygons)
        meta[name] = dict(tris=tris, size=[round(x, 2) for x in (hi - lo)])
        ob.hide_render = True


if __name__ == '__main__':
    main()
