"""Q4 우체국 — 공용 재질 키트 v2(지붕·데칼은 v1 바이트)만 조립. 새 텍스처 0 · 빛/AO 굽기 0. (과제 20261028-0534)
Blender X 오른쪽, -Y 정면(Unity +Z), Z 위. 피벗 = 바닥 앞면 가운데. 본체 9×8m, 2층(1층 3.4 + 2층 3.0), 용마루 7.6m.
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/q4_postoffice.py
출력: art/export/q4-postoffice/{q4_postoffice.fbx, 키트 png 5 + postbox_G1.png, validation.json}, art/_review/2026-10-28-q4-postoffice/{15m,5m,34}.png
"""
import bpy, math, json, shutil, hashlib, random
from pathlib import Path
from mathutils import Vector, Matrix
R = Path('D:/Github/F1Game'); K = R / 'art/export/kit-shared-v2'; E = R / 'art/export/q4-postoffice'; V = R / 'art/_review/2026-10-28-q4-postoffice'
E.mkdir(parents=True, exist_ok=True); V.mkdir(parents=True, exist_ok=True)
for f in ['wall.png', 'trim.png', 'floor.png', 'roof.png', 'decals.png', 'palette.gpl', 'layout.json']: shutil.copy2(K / f, E / f)
shutil.copy2(R / 'art/export/scene-albedo-kit/postbox_G1.png', E / 'postbox_G1.png')
for o in list(bpy.data.objects): bpy.data.objects.remove(o)
L = json.loads((K / 'layout.json').read_text()); mats = {}; cells = {}
PX = 32  # px/m. 작은 물건은 64(교훈 5)
ICONS = {'parcel': (0, 0, 128, 128), 'clock': (128, 0, 64, 64), 'fragile': (192, 0, 64, 32), 'slip': (192, 32, 32, 48), 'calendar': (128, 64, 48, 64), 'ticket': (176, 64, 48, 24)}  # tools/asset-gen/icons_shared.py
for s in ['wall', 'trim', 'floor', 'roof', 'decals', 'icons']:
    m = bpy.data.materials.new('Shared_' + s); m.use_nodes = True; nt = m.node_tree; b = nt.nodes['Principled BSDF']; b.inputs['Roughness'].default_value = .9
    t = nt.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(str(E / (s + '.png'))); t.interpolation = 'Closest'; nt.links.new(t.outputs['Color'], b.inputs['Base Color'])
    if s in ('decals', 'icons'): nt.links.new(t.outputs['Alpha'], b.inputs['Alpha']); m.surface_render_method = 'DITHERED'
    mats[s] = m
    for c in (L[s] if s != 'icons' else [dict(name=k, rect_px=v) for k, v in ICONS.items()]): cells[c['name']] = (s, c['rect_px'])
GLASS = bpy.data.materials.new('Glass')  # 2230: 창 유리 = 따로 오브젝트·재질(Unity 에서 M_postoffice_glass 반투명). 1846 엔 export 에 불리언으로 덧칠했었다
MI = {s: i for i, s in enumerate(mats)}; MI['glass_mat'] = len(MI); ALLM = list(mats.values()) + [GLASS]
GROUPS = {}; RNG = random.Random(4)
def grp(name):  # 2230: 만질 수 있는 물건은 낱개 오브젝트(교훈 7) — 이름별로 면을 따로 모은다. 몸통 = Q4_PostOffice_body
    global G, PX; G = GROUPS.setdefault(name, dict(v=[], f=[], uv=[], m=[])); PX = 32
def px_for(size):  # 2230: 소품 섬 ≤1.5m = 64px/m, 나머지(몸통·큰 가구) = 32 — check_rules 텍셀 기대값과 같은 기준
    global PX; PX = 64 if G is not GROUPS['Q4_PostOffice_body'] and size <= 1.5 else 32
grp('Q4_PostOffice_body')
def glassq(name, pts, want):  # 두께 없는 유리판(양면은 Unity 재질에서)
    global G; keep = G; grp(name)
    if normal(pts).dot(Vector(want)) < 0: pts = pts[::-1]
    add(pts, [(0, 0), (1, 0), (1, 1), (0, 1)], MI['glass_mat']); G = keep

# ---------- 면 · UV (32px/m, 지정 칸 안에서만) ----------
def add(pts, uv, mi):
    i0 = len(G['v']); G['v'] += [tuple(p) for p in pts]; G['f'].append(tuple(range(i0, i0 + len(pts)))); G['uv'].append(uv); G['m'].append(mi)
def normal(pts):
    P = [Vector(p) for p in pts]; n = Vector()
    for i in range(len(P)): a, b = P[i], P[(i + 1) % len(P)]; n += Vector(((a.y - b.y) * (a.z + b.z), (a.z - b.z) * (a.x + b.x), (a.x - b.x) * (a.y + b.y)))
    return n.normalized()
def uvmap(pts, cell, anchor=None):  # anchor: 벽은 세계 좌표로 이어 붙임(조각마다 같은 무늬로 시작 X), 8m 마다 같은 칸으로 돌아옴
    s, (x, y, w, h) = cells[cell]; P = [Vector(p) for p in pts]; n = normal(pts); up = Vector((0, 0, 1)) if abs(n.z) < .9 else Vector((0, 1, 0))
    e1 = up.cross(n).normalized()
    if anchor is None:  # 비스듬한 막대: 가장 긴 변을 U 축으로
        ed = max(((P[(i + 1) % len(P)] - P[i]) for i in range(len(P))), key=lambda v: v.length); e1 = (ed - n * ed.dot(n)).normalized()
    e2 = n.cross(e1); U = [p.dot(e1) for p in P]; W = [p.dot(e2) for p in P]; Wm, Hm = w / PX, h / PX
    if anchor is not None:
        U = [u + anchor for u in U]; k = math.floor(min(U) / Wm + 1e-6); U = [u - k * Wm for u in U]
    else:
        U = [u - min(U) for u in U]; W = [v - min(W) for v in W]; eu, ev = max(U), max(W)
        if ev > Hm + 1e-6 and eu <= Hm + 1e-6: U, W = W, U; eu, ev = ev, eu  # 긴 축을 U 로(트림 띠)
        assert eu <= Wm + 1e-6 and ev <= Hm + 1e-6, (cell, eu, ev)
        ou, ov = RNG.uniform(0, Wm - eu), RNG.uniform(0, Hm - ev); U = [u + ou for u in U]; W = [v + ov for v in W]
    return list(zip(U, W))
def poly(pts, cell, uvm=None, anchor=None, want=None):
    if want is not None and normal(pts).dot(Vector(want)) < 0: pts = pts[::-1]; uvm = uvm[::-1] if uvm else None
    s, (x, y, w, h) = cells[cell]
    if s in ('decals', 'icons'): A = 512 if s == 'decals' else 256; uvm = uvm or [(0, 0), (1, 0), (1, 1), (0, 1)]; uv = [((x + .5 + u * (w - 1)) / A, 1 - (y + h - .5 - v * (h - 1)) / A) for u, v in uvm]
    else:
        uvm = uvm or uvmap(pts, cell, anchor)
        assert all(-1e-3 <= u * PX <= w + 1e-2 and -1e-3 <= v * PX <= h + 1e-2 for u, v in uvm), (cell, uvm)
        uv = [((x + .01 + min(u * PX, w - .02)) / 512, 1 - (y + h - .01 - min(v * PX, h - .02)) / 512) for u, v in uvm]
    add(pts, uv, MI[s])
def solid(pts, cell):  # 글자 같은 작은 메시: 칸 가운데 한 점
    s, (x, y, w, h) = cells[cell]; add(pts, [((x + w / 2) / 512, 1 - (y + h / 2) / 512)] * len(pts), MI[s])
def box(c, size, cell, skip=()):
    px_for(max(size)); x, y, z = c; a, b, d = [v / 2 for v in size]; lo = (x - a, y - b, z - d); hi = (x + a, y + b, z + d)
    F = {'front': [(lo[0], lo[1], lo[2]), (hi[0], lo[1], lo[2]), (hi[0], lo[1], hi[2]), (lo[0], lo[1], hi[2])], 'back': [(hi[0], hi[1], lo[2]), (lo[0], hi[1], lo[2]), (lo[0], hi[1], hi[2]), (hi[0], hi[1], hi[2])],
         'right': [(hi[0], lo[1], lo[2]), (hi[0], hi[1], lo[2]), (hi[0], hi[1], hi[2]), (hi[0], lo[1], hi[2])], 'left': [(lo[0], hi[1], lo[2]), (lo[0], lo[1], lo[2]), (lo[0], lo[1], hi[2]), (lo[0], hi[1], hi[2])],
         'top': [(lo[0], lo[1], hi[2]), (hi[0], lo[1], hi[2]), (hi[0], hi[1], hi[2]), (lo[0], hi[1], hi[2])], 'bottom': [(lo[0], hi[1], lo[2]), (hi[0], hi[1], lo[2]), (hi[0], lo[1], lo[2]), (lo[0], lo[1], lo[2])]}
    for k, p in F.items():
        if k not in skip: poly(p, cell)
def bar(p0, p1, t, cell):  # 임의 방향 막대(자전거 틀·줄)
    p0, p1 = Vector(p0), Vector(p1); px_for((p1 - p0).length); d = (p1 - p0).normalized(); a = d.cross(Vector((0, 0, 1)) if abs(d.z) < .95 else Vector((1, 0, 0))).normalized(); b = d.cross(a)
    o = [(a * math.cos(math.pi / 4 + k * math.pi / 2) + b * math.sin(math.pi / 4 + k * math.pi / 2)) * t / 2 * math.sqrt(2) for k in range(4)]
    for k in range(4): poly([p0 + o[k], p0 + o[(k + 1) % 4], p1 + o[(k + 1) % 4], p1 + o[k]], cell, want=o[k] + o[(k + 1) % 4])
    poly([p0 + v for v in o], cell, want=-d); poly([p1 + v for v in o], cell, want=d)
def cyl(c, r0, r1, hgt, n, side, cap, axis='z', top=True):  # 원기둥/원뿔대(화분·바퀴·시계)
    px_for(max(2 * r0, 2 * r1, hgt)); M = {'z': Matrix.Identity(3), 'x': Matrix.Rotation(math.pi / 2, 3, 'Y'), '-y': Matrix.Rotation(math.pi / 2, 3, 'X')}[axis]; c = Vector(c)
    ring = lambda r, z: [Vector((r * math.cos(2 * math.pi * k / n), r * math.sin(2 * math.pi * k / n), z)) for k in range(n)]
    A, B = ring(r0, 0), ring(r1, hgt)
    for k in range(n):
        q = [A[k], A[(k + 1) % n], B[(k + 1) % n], B[k]]; mid = (A[k] + A[(k + 1) % n]) / 2; mid.z = 0
        poly([c + M @ p for p in q], side, want=M @ mid)
    if top: poly([c + M @ p for p in B], cap, want=M @ Vector((0, 0, 1)))
    poly([c + M @ p for p in A], cap, want=M @ Vector((0, 0, -1)))
def leaf(base, ang, elev, ln, wd, cell):  # 잎 한 장 = 마름모 양면(초록 네모 X)
    base = Vector(base); d = Vector((math.cos(ang) * math.cos(elev), math.sin(ang) * math.cos(elev), math.sin(elev))); s = Vector((-math.sin(ang), math.cos(ang), 0)) * wd / 2
    px_for(ln); q = [base, base + d * ln * .45 + s, base + d * ln, base + d * ln * .45 - s]; poly(q, cell); poly(q[::-1], cell)
def plant(x, y, z, n, ln, seed, flower=False):
    rs = random.Random(seed)
    for k in range(n):
        a = 2 * math.pi * k / n + rs.uniform(-.3, .3); el = rs.uniform(.55, 1.2); l = ln * rs.uniform(.7, 1.05)
        leaf((x, y, z), a, el, l, l * .42, 'door_frame' if k % 2 else 'window_frame')
        if flower and k % 2 == 0:
            tip = Vector((x, y, z)) + Vector((math.cos(a) * math.cos(el), math.sin(a) * math.cos(el), math.sin(el))) * l
            box(tuple(tip + Vector((0, 0, .03))), (.07, .07, .06), 'roof_F2')  # 제라늄 빨강(키트 F2 함석 칸)
NPOT = [0]
def pot(x, y, r, h, seed, n=5, flower=False):  # 2230: 화분 하나 = POT_NN(피벗 바닥 가운데), 8면 → 16면(교훈 5)
    global G; keep = G; NPOT[0] += 1; grp('POT_%02d' % NPOT[0])
    cyl((x, y, 0), r * .78, r, h, 16, 'roof_E2', 'floor_asphalt'); plant(x, y, h - .02, n, max(.28, h * 1.1), seed, flower); G = keep; px_for(99)
def text(body, cx, y, zb, hgt, cell):  # 간판 글자 = 실제 메시(새 텍스처 X), 앞면이 -Y
    cu = bpy.data.curves.new('t', 'FONT'); cu.body = body; cu.size = 1; cu.extrude = .012; cu.resolution_u = 2
    o = bpy.data.objects.new('t', cu); bpy.context.collection.objects.link(o); dg = bpy.context.evaluated_depsgraph_get(); oe = o.evaluated_get(dg); me = oe.to_mesh()
    vs = [v.co.copy() for v in me.vertices]; x0, x1 = min(v.x for v in vs), max(v.x for v in vs); y0, y1 = min(v.y for v in vs), max(v.y for v in vs); s = hgt / (y1 - y0)
    for p in me.polygons: solid([(cx + (vs[i].x - (x0 + x1) / 2) * s, y - vs[i].z - .012, zb + (vs[i].y - y0) * s) for i in p.vertices], cell)
    oe.to_mesh_clear(); bpy.data.objects.remove(o)

# ---------- 벽(구멍 뚫린 판을 사각형으로 나눔) ----------
WX, WY, H1, H2 = 4.5, 8.0, 3.4, 6.4
WALL = {'front': (lambda s, z: Vector((s, 0, z)), Vector((0, -1, 0)), 4.5, (-WX, WX)), 'right': (lambda s, z: Vector((WX, s, z)), Vector((1, 0, 0)), 0, (0, WY)),
        'back': (lambda s, z: Vector((-s, WY, z)), Vector((0, 1, 0)), 4.5, (-WX, WX)), 'left': (lambda s, z: Vector((-WX, -s, z)), Vector((-1, 0, 0)), 8, (-WY, 0))}
LO, HI = {'front': 'wall_apricot', 'right': 'wall_apricot', 'left': 'wall_apricot', 'back': 'wall_apricot_B'}, 'wall_cream'  # 1층 살구(우체국) · 2층 크림(사는 집)
HOLES = {k: [] for k in WALL}
def wall_build(wn):
    P, n, A, (s0, s1) = WALL[wn]; xs = {s0, s1, *[v for hl in HOLES[wn] for v in hl[:2]]}
    for k in range(-2, 4):
        if s0 < 8 * k - A < s1: xs.add(8 * k - A)
    xs = sorted(xs)
    for a, b in zip(xs, xs[1:]):
        hs = [hl for hl in HOLES[wn] if hl[0] <= a + 1e-6 and hl[1] >= b - 1e-6]; zs = sorted({0, H1, H2, *[v for hl in hs for v in hl[2:4]]})
        for z0, z1 in zip(zs, zs[1:]):
            if any(hl[2] <= z0 + 1e-6 and hl[3] >= z1 - 1e-6 for hl in hs): continue
            poly([P(a, z0), P(b, z0), P(b, z1), P(a, z1)], LO[wn] if z1 <= H1 + 1e-6 else HI, anchor=A, want=n)
    for g in (1, -1):  # 박공 세모
        if (wn == 'right' and g == 1) or (wn == 'left' and g == -1): poly([P(s0, H2), P(s1, H2), P((s0 + s1) / 2, 7.6)], HI, anchor=A, want=n)
def wbox(wn, s0, s1, z0, z1, o0, o1, cell):
    P, n, _, _ = WALL[wn]; a, b = P(s0, z0) + n * o0, P(s1, z1) + n * o1; lo = [min(a[i], b[i]) for i in range(3)]; hi = [max(a[i], b[i]) for i in range(3)]
    box([(lo[i] + hi[i]) / 2 for i in range(3)], [hi[i] - lo[i] for i in range(3)], cell)
def wquad(wn, s0, s1, z0, z1, out, cell, uvm=None):
    P, n, _, _ = WALL[wn]; poly([P(s0, z0) + n * out, P(s1, z0) + n * out, P(s1, z1) + n * out, P(s0, z1) + n * out], cell, uvm=uvm, want=n)
def reveal(wn, s0, s1, z0, z1, d=.2):
    P, n, _, _ = WALL[wn]; i = -n * d; c = LO[wn] if z1 <= H1 else HI; e1 = (P(1, 0) - P(0, 0))
    if z0 > 0: poly([P(s0, z0), P(s1, z0), P(s1, z0) + i, P(s0, z0) + i], c, want=(0, 0, 1))
    poly([P(s0, z1), P(s1, z1), P(s1, z1) + i, P(s0, z1) + i], c, want=(0, 0, -1))
    poly([P(s0, z0), P(s0, z1), P(s0, z1) + i, P(s0, z0) + i], c, want=e1); poly([P(s1, z0), P(s1, z1), P(s1, z1) + i, P(s1, z0) + i], c, want=-e1)
NGL = [0]
def window(wn, s0, s1, z0, z1, shutters=False, cross=True):
    # 2230: 1층 창(안이 보이는 창) = 구멍 + 1cm 안쪽 진짜 유리판 Q4_window_glass_N(1846 export 불리언을 생성기로). 2층 = 어두운 유리 칸 그대로(안이 비어 있음)
    # 창틀·창턱은 벽면에서 1cm 띄움(옛 −2cm 는 벽 구멍 안쪽 면과 같은 평면 = 치지직), 창살은 유리 앞 1.5cm
    HOLES[wn].append((s0, s1, z0, z1)); reveal(wn, s0, s1, z0, z1)
    if z1 <= H1: P, n, _, _ = WALL[wn]; NGL[0] += 1; glassq('Q4_window_glass_%d' % NGL[0], [P(s0, z0) - n * .18, P(s1, z0) - n * .18, P(s1, z1) - n * .18, P(s0, z1) - n * .18], n)
    else: wquad(wn, s0, s1, z0, z1, -.17, 'glass')
    for a, b in ((s0 - .09, s0), (s1, s1 + .09)): wbox(wn, a, b, z0 - .05, z1 + .05, .01, .06, 'window_frame')
    wbox(wn, s0 - .12, s1 + .12, z1, z1 + .1, .01, .07, 'window_frame'); wbox(wn, s0 - .14, s1 + .14, z0 - .08, z0, .01, .14, 'window_frame')
    if cross: m = (s0 + s1) / 2; wbox(wn, m - .03, m + .03, z0 - .01, z1 + .01, -.155, -.11, 'window_frame'); wbox(wn, s0 - .01, s1 + .01, z0 + (z1 - z0) * .62 - .03, z0 + (z1 - z0) * .62 + .03, -.155, -.11, 'window_frame')  # 끝은 구멍 옆면에 1cm 박음(같은 평면 X)
    if shutters:  # 이탈리아식 덧창(트림 셔터 띠 = 옅은 민트 살), 열어 둠
        w = (s1 - s0) / 2
        for a, b in ((s0 - .1 - w, s0 - .1), (s1 + .1, s1 + .1 + w)): wbox(wn, a, b, z0 - .02, z1 + .02, .02, .06, 'shutter')

# 정면 1층: 창구 창 2 + 유리문(가운데 살짝 왼쪽). 2층: 덧창 창 2 + 작은 창
window('front', -3.7, -2.0, .95, 2.35); window('front', 1.45, 3.15, .95, 2.35)
window('front', -3.45, -2.25, 4.15, 5.55, shutters=True); window('front', 1.75, 2.95, 4.15, 5.55, shutters=True); window('front', -.75, -.15, 4.75, 5.35, cross=False)
D0, D1, DH = -.85, .75, 2.45; HOLES['front'].append((D0, D1, 0, DH)); reveal('front', D0, D1, 0, DH)
for a, b in ((D0 - .1, D0), (D1, D1 + .1)): wbox('front', a, b, 0, DH + .1, .01, .07, 'door_frame')  # 2230: 문틀도 벽에서 1cm
wbox('front', D0 - .1, D1 + .1, DH, DH + .1, .01, .07, 'door_frame'); M = (D0 + D1) / 2
# 1623: 왼짝은 닫힌 채, 오른짝(M..D1)은 경첩 x D1 에서 안쪽으로 80° 열림. 2230: 두 짝 유리 = 진짜 유리판(문짝 틀 가운데 −0.16)
_Pf, _nf = WALL['front'][0], WALL['front'][1]
glassq('Q4_door_glass_1', [_Pf(s, z) - _nf * .16 for s, z in ((D0, .28), (M, .28), (M, DH - .08), (D0, DH - .08))], _nf)
for a, b in ((D0 + .005, D0 + .07), (M - .035, M)): wbox('front', a, b, 0, DH - .005, -.19, -.13, 'door_frame')  # 2230: 구멍 옆·윗면과 5mm(같은 평면 X)
wbox('front', D0 + .005, M, DH - .085, DH - .005, -.19, -.13, 'door_frame'); wbox('front', D0, M, 0, .28, -.19, -.13, 'door_frame')
wbox('front', M - .15, M - .12, .85, 1.35, -.13, -.07, 'gutter')  # 문 손잡이 막대
i0 = len(G['v'])
glassq('Q4_door_glass_2', [_Pf(s, z) - _nf * .16 for s, z in ((M, .28), (D1, .28), (D1, DH - .08), (M, DH - .08))], _nf)
for a, b in ((M, M + .035), (D1 - .07, D1)): wbox('front', a, b, 0, DH, -.19, -.13, 'door_frame')
wbox('front', M, D1, DH - .085, DH - .005, -.19, -.13, 'door_frame'); wbox('front', M, D1, 0, .28, -.19, -.13, 'door_frame')
for o0, o1 in ((-.13, -.07), (-.25, -.19)): wbox('front', M + .12, M + .15, .85, 1.35, o0, o1, 'gutter')
_hg = Vector((D1, .16, 0)); _RZ = Matrix.Rotation(math.radians(-80), 4, 'Z')
G['v'][i0:] = [tuple(_hg + _RZ @ (Vector(v) - _hg)) for v in G['v'][i0:]]
_g2 = GROUPS['Q4_door_glass_2']; _g2['v'] = [tuple(_hg + _RZ @ (Vector(v) - _hg)) for v in _g2['v']]
box((-.05, -.35, .06), (2.3, .7, .12), 'floor_concrete', skip=('bottom',))  # 계단 한 단
# 옆(오른쪽): 2층 덧창 · 1층 뒷방 창 / 왼쪽·뒤: 창
window('right', 3.2, 4.3, 4.2, 5.5, shutters=True); window('right', 5.6, 6.6, 1.1, 2.2)
window('left', -4.6, -3.5, 4.2, 5.5, shutters=True); window('left', -6.4, -5.4, 1.1, 2.2)
window('back', -3.0, -1.9, 4.2, 5.5); window('back', 1.9, 3.0, 4.2, 5.5)
HOLES['back'].append((-.5, .4, 0, 2.1)); reveal('back', -.5, .4, 0, 2.1); wquad('back', -.5, .4, 0, 2.1, -.18, 'wood_door')
for wn in WALL: wall_build(wn)
# 걸레받이 · 층 띠
# 2230: 벽에 붙은 띠는 벽면에서 1cm 띄움(같은 평면 = 안에서 본 뒷면 치지직), 문틀과 5mm 떨어뜨림
for a, b in ((-WX, D0 - .105), (D1 + .105, WX)): wbox('front', a, b, 0, .32, .01, .04, 'skirting')
for wn in ('right', 'left', 'back'): s0, s1 = WALL[wn][3]; wbox(wn, s0, s1, 0, .32, .01, .04, 'skirting')
for wn in WALL: s0, s1 = WALL[wn][3]; wbox(wn, s0, s1, H1 - .04, H1 + .06, .01, .07, 'window_frame')

# ---------- 지붕(E3 기와 — 택배 보관소 E2 와 다르게) ----------
OV = .8; k = (7.6 - H2) / 4; ez = H2 - OV * k  # 깊은 처마(게이트 1: 지붕이 안 보여 실루엣 단조)
for x0, x1 in ((-5.05, 0), (0, 5.05)):
    f = [(x0, -OV, ez), (x1, -OV, ez), (x1, 4, 7.6), (x0, 4, 7.6)]; poly(f, 'roof_E3'); poly(f[::-1], 'roof_E3')
    b = [(x1, 8 + OV, ez), (x0, 8 + OV, ez), (x0, 4, 7.6), (x1, 4, 7.6)]; poly(b, 'roof_E3'); poly(b[::-1], 'roof_E3')
ln = math.hypot(4 + OV, 7.6 - ez)
for x in (-5.05, 5.05):
    for y0, y1 in ((-OV, 4), (8 + OV, 4)):
        q = [(x, y0, ez - .16), (x, y1, 7.44), (x, y1, 7.6), (x, y0, ez)]; u0 = RNG.uniform(0, 10); uvm = [(u0, .1), (u0 + ln, .1), (u0 + ln, .26), (u0, .26)]
        poly(q, 'eaves', uvm=uvm); poly(q[::-1], 'eaves', uvm=uvm[::-1])
box((0, 4, 7.63), (10.2, .24, .1), 'eaves'); box((0, -OV - .07, ez - .04), (10.1, .14, .12), 'gutter'); box((0, 8 + OV + .03, ez - .06), (10.1, .06, .16), 'eaves')
for x in (-4.2, -1.4, 1.4, 4.2): bar((x, .01, ez - .5), (x, -OV + .05, ez - .1), .1, 'eaves')  # 서까래 까치발
box((4.38, -.08, (ez - .1) / 2), (.1, .1, ez - .1), 'gutter')  # 홈통
box((3.0, 5.6, 7.45), (.5, .5, 1.3), 'wall_cream', skip=('bottom', 'top')); box((3.0, 5.6, 8.13), (.62, .62, .06), 'floor_concrete')  # 굴뚝

# ---------- 간판 · 차양 · 시계 ----------
# 2230: 간판 판 1cm · 크림 판 판 앞 1.5cm · 기호·글자 크림 앞 1cm(옛 6mm·0mm = 치지직)
wbox('front', -1.35, 1.25, 2.66, 3.3, .01, .13, 'wood_door'); wquad('front', -1.27, 1.17, 2.71, 3.25, .145, 'sign_cream')
for s0, s1, z0, z1 in ((-1.03, -.67, 3.1, 3.15), (-1.03, -.67, 3.0, 3.05), (-.88, -.82, 2.8, 3.0)): wbox('front', s0, s1, z0, z1, .155, .18, 'roof_F2')  # 〒 류 기호(빨강)
text('POST', .27, -.155, 2.9, .28, 'gutter'); text('CAPO MARE', .27, -.155, 2.76, .08, 'gutter')  # 가상 이름
aw = [(-4.0, -1.05, 2.28), (3.6, -1.05, 2.28), (3.6, -.01, 2.6), (-4.0, -.01, 2.6)]; poly(aw, 'roof_F3'); poly(aw[::-1], 'roof_F3')  # 차양(F3 파란 함석)
box((-.2, -1.06, 2.23), (7.62, .04, .12), 'eaves')
for x in (-3.7, 3.3): bar((x, .01, 3.3), (x, -1.0, 2.31), .025, 'gutter')  # 2230: 벽에 1cm 박음(끝면이 벽과 같은 평면 X)
cyl((2.6, -.01, 3.0), .27, .27, .08, 16, 'eaves', 'wall_cream', axis='-y')  # 2230: 벽에서 1cm  # 벽 시계(테 = 처마 갈색, 판 = 크림 회벽)
cz = Vector((2.6, -.115, 3.0))  # 2230: 바늘·눈금 = 판(−0.09) 앞 1~2.5cm; bar(cz, cz + Vector((-.1, 0, .09)), .025, 'gutter'); bar(cz, cz + Vector((.05, 0, .19)), .018, 'gutter')
for a in range(4): t = cz + Vector((math.cos(a * math.pi / 2) * .21, 0, math.sin(a * math.pi / 2) * .21)); box(tuple(t + Vector((0, .01, 0))), (.035, .01, .035), 'gutter')

# ---------- 2층 사는 집: 창 화분 상자 · 빨래 ----------
grp('PLANTER_01'); box((-2.85, -.18, 4.0), (1.3, .26, .2), 'roof_E2')  # 2230: 창 화분 상자 = 낱개
for i, x in enumerate((-3.3, -2.85, -2.4)): plant(x, -.18, 4.1, 5, .24, 10 + i, flower=True)
grp('Q4_PostOffice_body')
box((2.35, -.46, 4.0), (2.5, .9, .14), 'floor_concrete')  # 2층 작은 발코니(대칭 깨기 · 사는 집)
for x0, x1 in ((1.1, 3.6),): bar((x0, -.88, 4.95), (x1, -.88, 4.95), .05, 'gutter'); bar((x0, -.88, 4.12), (x1, -.88, 4.12), .03, 'gutter')
for s in (1.1, 3.6): bar((s, -.88, 4.07), (s, -.88, 4.95), .04, 'gutter')  # 2230: 기둥 0.04 ≠ 손잡이 0.05(같은 두께면 앞뒤 면이 같은 평면 = 낀 판); bar((s, .01, 4.95), (s, -.88, 4.95), .04, 'gutter')
for i in range(1, 12): x = 1.1 + 2.5 * i / 12; bar((x, -.88, 4.12), (x, -.88, 4.95), .02, 'gutter')
bar((1.45, -.55, 5.68), (3.25, -.55, 5.68), .03, 'gutter')
for x in (1.5, 3.2): bar((x, .01, 5.68), (x, -.55, 5.68), .03, 'gutter')
box((1.9, -.55, 5.33), (.5, .02, .62), 'wall_cream'); box((2.47, -.55, 5.4), (.38, .02, .5), 'wall_mint'); box((2.95, -.55, 5.47), (.3, .02, .36), 'door_frame')  # 셔츠·수건·작은 천

# ---------- 옆: 공지판 · 자전거 · 화분 무리 / 앞 왼쪽 화분 둘 ----------
wbox('right', .6, 1.95, 1.0, 1.95, .01, .06, 'wood_door'); wbox('right', .55, 2.0, 1.95, 2.02, .01, .13, 'eaves')  # 2230: 벽 1cm · 종이 판 앞 1.5cm
wquad('right', .72, 1.12, 1.2, 1.8, .075, 'flyer'); wquad('right', 1.3, 1.72, 1.24, 1.8, .075, 'poster_festival'); wquad('right', 1.5, 1.82, 1.03, 1.2, .085, 'number_07')
grp('BIKE_01')  # 2230: 우편 자전거 = 낱개, 바퀴·허브 16면
X = 4.88; RW, FW = Vector((X, 3.7, .33)), Vector((X, 2.62, .33))
for c in (RW, FW): cyl(tuple(c), .33, .33, .045, 16, 'gutter', 'floor_asphalt', axis='x'); cyl(tuple(c + Vector((-.005, 0, 0))), .07, .07, .055, 16, 'skirting', 'skirting', axis='x')
C, S, Hd = Vector((X, 3.22, .36)), Vector((X, 3.38, .95)), Vector((X, 2.76, .98))
for a, b, t in ((RW, C, .04), (RW, S, .05), (C, S, .03), (C, Hd, .06), (S, Hd, .04), (Hd, FW, .05)): bar(a, b, t, 'roof_F2')  # 우편 자전거 빨간 틀. 2230: 만나는 관끼리 두께 1cm 차(같은 X 평면 = 낀 판)
bar(Hd + Vector((-.25, 0, .06)), Hd + Vector((.25, 0, .06)), .03, 'gutter'); box(tuple(S + Vector((0, .03, .04))), (.12, .26, .05), 'gutter')
box((X, 2.5, .9), (.36, .3, .24), 'wood_door'); box((X, 3.8, .7), (.34, .4, .04), 'roof_F2')  # 앞 바구니 · 뒤 짐받이(2230: 64px/m 에선 gutter 띠 16px 에 안 들어감 → 빨간 틀 칸)
grp('MAIL_01'); box((X, 3.82, .855), (.3, .34, .28), 'wall_apricot_B'); grp('Q4_PostOffice_body')  # 짐받이 소포 = 우편물 낱개(짐받이에 5mm 박음)
for i, (x, y, r, h, f) in enumerate(((4.8, 4.5, .2, .36, False), (4.76, 4.98, .15, .28, True), (4.84, 5.45, .24, .46, False), (5.18, 4.8, .12, .2, False))): pot(x, y, r, h, 30 + i, 6 if r > .18 else 5, f)
pot(-4.1, -.38, .2, .36, 40, 6); pot(-3.72, -.3, .14, .24, 41, 5, True)

# ---------- 1623 내부: 바닥·천장·실내등·창구 2(유리 칸막이·번호표)·사서함 벽·우편 분류 선반·대기 의자 2·포스터·달력·화분 ----------
for x0, x1 in ((-4.45, 0), (0, 4.45)):
    poly([(x0, .2, .01), (x1, .2, .01), (x1, 7.8, .01), (x0, 7.8, .01)], 'floor_wood', want=(0, 0, 1))
    poly([(x0, .2, H1 - .06), (x1, .2, H1 - .06), (x1, 7.8, H1 - .06), (x0, 7.8, H1 - .06)], 'wall_cream', want=(0, 0, -1))
for y in (2.2, 6.0): box((0, y, H1 - .12), (1.1, .22, .07), 'glass')  # 실내등(형광등 판)
# 2230: 소품마다 낱개 오브젝트(COUNTER_·POBOX_·POBOX_DOOR_·SHELF_·MAIL_·CHAIR_), 텍셀은 px_for 가 섬 크기로(≤1.5m = 64)
grp('COUNTER_01')
box((0, 4.6, .5), (5.0, .6, 1.0), 'wood_door', skip=('top',)); box((0, 4.55, 1.03), (5.2, .74, .06), 'wood_door')  # 창구 카운터(몸통 윗면 = 상판 밑 = 같은 평면 → 뺌)
for i, (x0, x1) in enumerate(((-2.3, -.5), (.5, 2.3))): glassq('Q4_counter_glass_%d' % (i + 1), [(x0, 4.45, 1.06), (x1, 4.45, 1.06), (x1, 4.45, 1.95), (x0, 4.45, 1.95)], (0, -1, 0))  # 1623 검은 판 → 진짜 유리
for x in (-2.35, -.45, .45, 2.35): box((x, 4.45, 1.5), (.06, .06, .9), 'gutter')
box((0, 4.45, 1.98), (4.8, .08, .06), 'gutter')
for x in (-1.4, 1.4):  # 번호표 표시기(창구마다)
    box((x, 4.5, 2.25), (.56, .06, .3), 'shutter'); box((x, 4.5, 2.84), (.02, .02, .9), 'gutter')
    poly([(x - .26, 4.455, 2.13), (x + .26, 4.455, 2.13), (x + .26, 4.455, 2.37), (x - .26, 4.455, 2.37)], 'ticket', want=(0, -1, 0))
grp('POBOX_01'); box((-4.42, 2.32, 1.55), (.12, 2.3, 1.7), 'wood_door')  # 사서함 벽(왼쪽 안벽) 바탕
for r in range(5):
    for c in range(7):  # 사서함 문 35 = 낱개(문 + 손잡이), 바탕 앞 5mm·손잡이 문 앞 1cm
        y = 1.37 + c * .3; z = .85 + r * .3; grp('POBOX_DOOR_%02d' % (r * 7 + c + 1))
        box((-4.3375, y, z), (.035, .27, .27), 'wall_mint'); box((-4.3, y + .08, z), (.02, .03, .03), 'gutter')
grp('SHELF_01'); box((2.5, 7.75, 1.5), (3.6, .1, 2.0), 'wood_door')  # 우편 분류 선반(뒷벽, 뒷문 피해 오른쪽)
for k in range(9): box((.7 + k * .45, 7.55, 1.5), (.03, .4, 1.98), 'wood_door')  # 칸막이 윗면 = 뒤판 윗면 같은 평면 → 2cm 낮춤
for k in range(7): box((2.5, 7.55, .5 + k * .33), (3.58, .4, .03), 'wood_door')
NM = 0
for k in range(8):
    for r in range(6):
        if RNG.random() < .45:  # 우편물 = 낱개, 선반 윗면에 바로 섬
            x = .7 + k * .45 + .225; z = .515 + r * .33; NM += 1; grp('MAIL_%02d' % (NM + 1))
            poly([(x - .1, 7.45, z), (x + .1, 7.45, z), (x + .1, 7.45, z + .2), (x - .1, 7.45, z + .2)], 'slip', want=(0, -1, 0))
for i, y in enumerate((1.4, 2.2)):  # 대기 의자 2 — 다리는 좌판에 1.5cm 박고, 등받이는 좌판을 뚫고 내려감(같은 평면 X)
    grp('CHAIR_%02d' % (i + 1))
    box((3.4, y, .45), (.46, .46, .05), 'wood_door'); box((3.61, y, .77), (.04, .46, .62), 'wood_door')
    for dx in (-.2, .2):
        for dy in (-.2, .2): box((3.4 + dx, y + dy, .22), (.04, .04, .44), 'gutter')
grp('Q4_PostOffice_body')
poly([(4.47, 3.6, 1.3), (4.47, 3.0, 1.3), (4.47, 3.0, 2.1), (4.47, 3.6, 2.1)], 'poster_harbor', want=(-1, 0, 0))  # 오른쪽 안벽 포스터
poly([(4.47, 4.45, 1.4), (4.47, 4.0, 1.4), (4.47, 4.0, 2.0), (4.47, 4.45, 2.0)], 'calendar', want=(-1, 0, 0))
pot(-1.6, .7, .2, .36, 52, 6)  # 화분(문 옆 안쪽)

# ---------- 메시 만들기 ----------
def build(name, g):  # 2230: 낱개는 피벗 바닥 가운데(교훈 7), 몸통·유리는 루트 원점. 재질 슬롯은 그 물건이 쓰는 것만
    piv = Vector()
    if not (name == 'Q4_PostOffice_body' or 'glass' in name):
        V = [Vector(v) for v in g['v']]; piv = Vector(((min(v.x for v in V) + max(v.x for v in V)) / 2, (min(v.y for v in V) + max(v.y for v in V)) / 2, min(v.z for v in V)))
    me = bpy.data.meshes.new(name); me.from_pydata([tuple(Vector(v) - piv) for v in g['v']], [], g['f']); me.update()
    used = sorted(set(g['m'])); [me.materials.append(ALLM[i]) for i in used]
    ul = me.uv_layers.new(name='UVMap')
    for p, uv, mi in zip(me.polygons, g['uv'], g['m']):
        p.material_index = used.index(mi)
        for li, u in zip(p.loop_indices, uv): ul.data[li].uv = u
    o = bpy.data.objects.new(name, me); o.location = piv; bpy.context.collection.objects.link(o); return o
OBJS = [build(n, g) for n, g in GROUPS.items() if g['f']]; body = OBJS[0]
print('OBJS', len(OBJS), [o.name for o in OBJS])
# 우체통 = scene-albedo-kit/postbox_G1 재사용(형상·PNG 그대로)
before = set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=str(R / 'art/export/scene-albedo-kit/postbox_G1.fbx')); pb = [o for o in bpy.data.objects if o not in before and o.type == 'MESH']
dg = bpy.context.evaluated_depsgraph_get(); pts = [o.matrix_world @ v.co for o in pb for v in o.data.vertices]; lo = Vector([min(p[i] for p in pts) for i in range(3)]); hi = Vector([max(p[i] for p in pts) for i in range(3)])
print('POSTBOX dims', tuple(round(v, 3) for v in hi - lo), [o.name for o in pb])
sc = 1.3 / (hi.z - lo.z) if not 1.0 < hi.z - lo.z < 1.6 else 1.0; base = Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z))
for o in pb:
    o.matrix_world = Matrix.Translation(Vector((-1.42, -.45, 0))) @ Matrix.Scale(sc, 4) @ Matrix.Translation(-base) @ o.matrix_world
    for ms in o.material_slots:
        for nd in ms.material.node_tree.nodes:
            if nd.type == 'TEX_IMAGE' and nd.image: nd.image.filepath = str(E / 'postbox_G1.png'); nd.interpolation = 'Closest'
bpy.context.view_layer.update()
for i, o in enumerate(pb):  # 2230: 우체통 = POSTBOX_01(피벗 바닥 가운데로 메시를 옮김)
    o.name = 'POSTBOX_%02d' % (i + 1) if len(pb) > 1 else 'POSTBOX_01'; mw = o.matrix_world.copy(); o.data.transform(mw); o.matrix_world = Matrix.Identity(4)
    W = [v.co for v in o.data.vertices]; piv = Vector(((min(v.x for v in W) + max(v.x for v in W)) / 2, (min(v.y for v in W) + max(v.y for v in W)) / 2, min(v.z for v in W)))
    o.data.transform(Matrix.Translation(-piv)); o.location = piv
root = bpy.data.objects.new('Q4_PostOffice', None); bpy.context.collection.objects.link(root)
asset = OBJS + pb
for o in asset: mw = o.matrix_world.copy(); o.parent = root; o.matrix_world = mw
EMP = []
for nm, loc in (('STUDIO_CamIn', (-.05, 2.0, 1.5)), ('STUDIO_CamInLook', (-1.0, 6.0, 1.3)), ('STUDIO_Lamp', (0, 4.0, 3.1))):
    e = bpy.data.objects.new(nm, None); bpy.context.collection.objects.link(e); e.location = loc; e.parent = root; EMP.append(e)
bpy.context.view_layer.update()
tris = sum(len(p.vertices) - 2 for o in asset for p in o.data.polygons)
P = [body.matrix_world @ v.co for v in body.data.vertices]; bounds = [[round(min(p[i] for p in P), 3) for i in range(3)], [round(max(p[i] for p in P), 3) for i in range(3)]]
bpy.ops.wm.save_as_mainfile(filepath=str(E / 'q4_postoffice.blend'))
bpy.ops.object.select_all(action='DESELECT')
for o in asset + [root] + EMP: o.select_set(True)
bpy.context.view_layer.objects.active = root
bpy.ops.export_scene.fbx(filepath=str(E / 'q4_postoffice.fbx'), use_selection=True, object_types={'MESH', 'EMPTY'}, axis_forward='-Z', axis_up='Y', path_mode='COPY', bake_anim=False)
val = dict(tris=tris, body_tris=sum(len(p.vertices) - 2 for p in body.data.polygons), bounds_blender=bounds, body_m=[9, 8, 6.4], ridge_m=7.6, front='Unity +Z / Blender -Y', pivot='바닥 앞면 가운데',
           materials=list(mats) + ['postbox_G1(재사용)'], textures={n: hashlib.sha256((E / n).read_bytes()).hexdigest() for n in ['wall.png', 'trim.png', 'floor.png', 'roof.png', 'decals.png', 'icons.png', 'postbox_G1.png']},
           kit_same_bytes={n: (E / n).read_bytes() == (K / n).read_bytes() for n in ['wall.png', 'trim.png', 'floor.png', 'roof.png', 'decals.png']}, light_bake=False, new_textures=0, postbox_scale=round(sc, 3))
(E / 'validation.json').write_text(json.dumps(val, indent=1, ensure_ascii=False), encoding='utf-8'); print('VAL', val['tris'], bounds, flush=True)
import sys, os
if '--no-render' in sys.argv: os._exit(0)

# ---------- 미리보기(내보내지 않음): 같은 키트 바닥 + 해 1 + 하늘빛 (택배 보관소와 같은 조건) ----------
G.update(v=[], f=[], uv=[], m=[])
for x in range(-16, 16, 8):
    for y in range(-16, 16, 8): poly([(x, y, -.02), (x + 8, y, -.02), (x + 8, y + 8, -.02), (x, y + 8, -.02)], 'floor_stone', uvm=[(0, 0), (8, 0), (8, 8), (0, 8)])
gm = bpy.data.meshes.new('ground'); gm.from_pydata(G['v'], [], G['f']); gm.update(); [gm.materials.append(m) for m in mats.values()]; gl = gm.uv_layers.new(name='UVMap')
for p, uv, mi in zip(gm.polygons, G['uv'], G['m']):
    p.material_index = mi
    for li, u in zip(p.loop_indices, uv): gl.data[li].uv = u
bpy.context.collection.objects.link(bpy.data.objects.new('preview_ground', gm))
scn = bpy.context.scene
bpy.ops.object.light_add(type='SUN'); sun = bpy.context.object; sun.data.energy = 1.6; sun.data.color = (1, .94, .85); sun.data.angle = .15; sun.rotation_euler = Vector((.3, .5, -.8)).to_track_quat('-Z', 'Y').to_euler()
scn.world = bpy.data.worlds.new('sky'); scn.world.use_nodes = True; scn.world.node_tree.nodes['Background'].inputs[0].default_value = (.48, .59, .64, 1); scn.world.node_tree.nodes['Background'].inputs[1].default_value = .85
scn.render.engine = 'CYCLES'; scn.cycles.samples = 64; scn.cycles.use_denoising = True; scn.cycles.device = 'GPU'
try:
    pr = bpy.context.preferences.addons['cycles'].preferences; pr.compute_device_type = 'OPTIX'; pr.get_devices(); [setattr(d, 'use', d.type == 'OPTIX') for d in pr.devices]
except Exception as ex: print('GPU', ex)
scn.render.resolution_x = 1600; scn.render.resolution_y = 900; scn.render.resolution_percentage = 100; scn.render.dither_intensity = 0
scn.view_settings.view_transform = 'Standard'; scn.view_settings.look = 'None'; scn.view_settings.exposure = 0
bpy.ops.object.camera_add(); cam = bpy.context.object; scn.camera = cam; cam.data.sensor_fit = 'VERTICAL'; cam.data.sensor_height = 32; cam.data.lens = 32 / (2 * math.tan(math.radians(64) / 2))
VIEWS = {'15m': ((0, -15, 2.8), (0, 2, 3.4)), '5m': ((1.2, -5, 1.7), (-.2, 0, 2.3)), '34': ((11, -9.5, 3.4), (1.6, 2.6, 2.6))}
for nm, (loc, tg) in VIEWS.items():
    cam.location = loc; cam.rotation_euler = (Vector(tg) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler(); scn.render.filepath = str(V / (nm + '.png')); bpy.ops.render.render(write_still=True); print('RENDER', nm, flush=True)
