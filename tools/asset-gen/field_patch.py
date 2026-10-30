"""집에 딸린 텃밭 field_patch — 과제 20261030-2003 (D-208 정정·D-210). 1단계 = 회색 형태만(재질 칸 pt 는 2단계 텍스처용 표식).
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/field_patch.py
크기: 6.0 × 5.5m(바깥 울 기준). 이랑 4줄(길이 = Blender Y), 고랑, 뒤·옆 = 낮은 나무 울(기둥 0.8m ±기울기·간격 ±20%, SHAPE §2), 앞 = 돌 한 줄 + 입구 틈.
작물: 이랑 1·2 = 배추(속 + 바깥잎 겹침, 공 금지), 이랑 3 = 대파(북주기한 높은 이랑), 이랑 4 = 고추 지주대 + 늦가을 고추 포기. 소품: 물뿌리개 CAN_01 · 바구니 BASKET_01(+ 뽑은 배추).
축: 앞(입구) = Blender -Y → FBX/Unity +Z. 피벗 = 밭 바닥(땅 z 0) 가운데. 낱개 = 이름_번호 · 피벗 바닥 가운데(D-195).
겹친 면 0: 이랑·돌·기둥은 흙판(z ≥ .015)을 뚫고 내려가 교차(같은 평면 없음), 가로대는 기둥에 1cm 박힘. 원통(줄기·통·바구니) 16면.
출력: art/export/field-patch/{field_patch.fbx, measure.json}
"""
import bpy, bmesh, os, sys, json, math, random
from mathutils import Vector, Matrix
R = 'D:/Github/F1Game/'; OUT = R + 'art/export/field-patch/'; os.makedirs(OUT, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True); sc = bpy.context.scene
rng = random.Random(2003)
J = lambda a: rng.uniform(-a, a)

W, D = 6.0, 5.5                     # 바깥 폭(X) · 깊이(Y)
X0, X1, Y0, Y1 = -W / 2, W / 2, -D / 2, D / 2
FACES = []                          # (obj, [Vector], pt)
def face(obj, pts, pt): FACES.append((obj, [Vector(p) for p in pts], pt))
def quad_out(obj, pts, pt, ctr):    # 법선이 ctr 바깥을 보게
    P = [Vector(p) for p in pts]; n = (P[1] - P[0]).cross(P[2] - P[0]); c = sum(P, Vector()) / len(P)
    face(obj, pts if n.dot(c - Vector(ctr)) >= 0 else pts[::-1], pt)

def beam(obj, p0, p1, w, h, pt, caps=(True, True), up=(0, 0, 1)):  # p0→p1 각재(단면 w×h), 8점
    p0, p1 = Vector(p0), Vector(p1); a = (p1 - p0).normalized(); u = Vector(up)
    if abs(a.dot(u)) > .95: u = Vector((0, 1, 0))
    s = a.cross(u).normalized(); t = s.cross(a).normalized()
    ring = lambda p: [p + s * sx * w / 2 + t * ty * h / 2 for sx, ty in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
    A, B = ring(p0), ring(p1); ctr = (p0 + p1) / 2
    for i in range(4): quad_out(obj, [A[i], A[(i + 1) % 4], B[(i + 1) % 4], B[i]], pt, ctr)
    if caps[0]: quad_out(obj, A, pt, ctr)
    if caps[1]: quad_out(obj, B, pt, ctr)

def tube(obj, path, radii, pt, n=16, cap0=True, cap1=True, squash=1.0):  # 길(점 목록)을 따라 16각 고리 — 원통 규칙(≥16면)
    P = [Vector(p) for p in path]; rings = []
    for i, p in enumerate(P):
        a = (P[min(i + 1, len(P) - 1)] - P[max(i - 1, 0)]).normalized()
        u = Vector((1, 0, 0)) if abs(a.z) > .9 else Vector((0, 0, 1)); s = a.cross(u).normalized(); t = a.cross(s).normalized()
        rings.append([p + (s * math.cos(2 * math.pi * k / n) + t * math.sin(2 * math.pi * k / n) * squash) * radii[i] for k in range(n)])
    for i in range(len(P) - 1):
        ctr = (P[i] + P[i + 1]) / 2
        for k in range(n): quad_out(obj, [rings[i][k], rings[i][(k + 1) % n], rings[i + 1][(k + 1) % n], rings[i + 1][k]], pt, ctr)
    if cap0 and radii[0] > 1e-4: quad_out(obj, rings[0], pt, P[1])
    if cap1 and radii[-1] > 1e-4: quad_out(obj, rings[-1], pt, P[-2])

def lathe(obj, cx, cy, prof, pt, n=16, jit=0.0, seed=0, lean=(0, 0), top=True):  # (r, z) 단면을 돌린 몸통(배추 속·통). jit = 반지름 흔들림
    rr = random.Random(seed); ph = [rr.uniform(0, 6.28) for _ in range(3)]
    rings = []
    for r, z in prof:
        ring = []
        for k in range(n):
            a = 2 * math.pi * k / n; f = 1 + jit * (math.sin(3 * a + ph[0]) * .6 + math.sin(5 * a + ph[1] + z * 9) * .4)
            ring.append(Vector((cx + r * f * math.cos(a) + lean[0] * z, cy + r * f * math.sin(a) + lean[1] * z, z)))
        rings.append(ring)
    ctr = Vector((cx, cy, sum(z for _, z in prof) / len(prof)))
    for i in range(len(rings) - 1):
        for k in range(n): quad_out(obj, [rings[i][k], rings[i][(k + 1) % n], rings[i + 1][(k + 1) % n], rings[i + 1][k]], pt, Vector((cx + lean[0] * prof[i][1], cy + lean[1] * prof[i][1], (prof[i][1] + prof[i + 1][1]) / 2)))
    if top and prof[-1][0] > 1e-4: quad_out(obj, rings[-1], pt, Vector((cx, cy, prof[-1][1] - 1)))
    return rings

def leaf(obj, base, yaw, spine, widths, thick, pt, roll=0.0):  # 잎 = 얇은 두께 있는 판(위·아래·옆). spine = [(앞으로 r, 높이 z)], widths = 마디 폭
    c, s = math.cos(yaw), math.sin(yaw); fwd = Vector((c, s, 0)); side = Vector((-s, c, 0))
    top, bot = [], []
    for (r, z), w in zip(spine, widths):
        p = Vector(base) + fwd * r + Vector((0, 0, z)); sw = side * (w / 2)
        cup = Vector((0, 0, roll * w))     # 가장자리를 들어 올려 잎이 오목하게
        top.append((p - sw + cup, p + sw + cup)); bot.append((p - sw + cup - Vector((0, 0, thick)), p + sw + cup - Vector((0, 0, thick))))
    ctr = Vector(base) + fwd * spine[len(spine) // 2][0] + Vector((0, 0, spine[len(spine) // 2][1] - thick / 2))
    for i in range(len(top) - 1):
        quad_out(obj, [top[i][0], top[i][1], top[i + 1][1], top[i + 1][0]], pt, ctr - Vector((0, 0, 1)))   # 윗면(위로)
        quad_out(obj, [bot[i][0], bot[i][1], bot[i + 1][1], bot[i + 1][0]], pt, ctr + Vector((0, 0, 1)))   # 아랫면
        for j in (0, 1): quad_out(obj, [top[i][j], top[i + 1][j], bot[i + 1][j], bot[i][j]], pt, ctr)        # 옆
    quad_out(obj, [top[-1][0], top[-1][1], bot[-1][1], bot[-1][0]], pt, ctr)

def rock(obj, cx, cy, sx, sy, h, pt, seed):  # 낮은 돌: 8점 상자를 흔든 덩어리(윗면 둥근 모서리 4점 추가), 땅에 4cm 박힘
    rr = random.Random(seed); yaw = rr.uniform(-.4, .4); c, s = math.cos(yaw), math.sin(yaw)
    P = lambda x, y, z: Vector((cx + x * c - y * s, cy + x * s + y * c, z))
    b = [P(-sx / 2 + rr.uniform(-.03, .03), -sy / 2 + rr.uniform(-.03, .03), -.04), P(sx / 2 + rr.uniform(-.03, .03), -sy / 2, -.04), P(sx / 2, sy / 2 + rr.uniform(-.03, .03), -.04), P(-sx / 2, sy / 2, -.04)]
    m = [P(-sx * .5, -sy * .48, h * .55), P(sx * .52, -sy * .5, h * .5), P(sx * .5, sy * .5, h * .6), P(-sx * .48, sy * .52, h * .52)]
    t = [P(-sx * .3 + rr.uniform(-.03, .03), -sy * .28, h * rr.uniform(.9, 1.0)), P(sx * .32, -sy * .3, h * rr.uniform(.85, 1.0)), P(sx * .28, sy * .3, h), P(-sx * .3, sy * .32, h * rr.uniform(.88, 1.0))]
    ctr = P(0, 0, h * .4)
    for L0, L1 in ((b, m), (m, t)):
        for i in range(4): quad_out(obj, [L0[i], L0[(i + 1) % 4], L1[(i + 1) % 4], L1[i]], pt, ctr)
    quad_out(obj, t, pt, ctr)

# ---------- 흙판(고랑·울 안 바닥): 격자 + 높이 결 ±1cm(≥1.5cm), 가장자리 불규칙 ----------
IX0, IX1, IY0, IY1 = X0 + .12, X1 - .12, Y0 + .30, Y1 - .12     # 울 안쪽(앞은 돌 줄 안쪽)
NX, NY = 16, 15; G = {}
for i in range(NX + 1):
    for j in range(NY + 1):
        x = IX0 + (IX1 - IX0) * i / NX; y = IY0 + (IY1 - IY0) * j / NY
        edge = i in (0, NX) or j in (0, NY)
        if edge: x += J(.05) if i in (0, NX) else 0; y += J(.05) if j in (0, NY) else 0
        G[i, j] = Vector((x, y, .006 if edge else .02 + J(.008)))
for i in range(NX):
    for j in range(NY): face('SOIL_base', [G[i, j], G[i + 1, j], G[i + 1, j + 1], G[i, j + 1]], 'soil')

# ---------- 이랑 4줄(길이 = Y). 단면 7점(밑 넓고 위 둥근 사다리꼴), 마디마다 폭·높이 흔들림, 끝은 낮아지며 둥글게 ----------
RW, RF = .92, .36                                # 이랑 밑 폭 · 고랑 폭
RL0, RL1 = IY0 + .35, IY1 - .25                  # 이랑 시작·끝(앞 = 입구 쪽 발 디딜 자리 .35)
RX = [(-1.5 * (RW + RF)) + k * (RW + RF) for k in range(4)]     # 이랑 가운데 x
RH = [.22, .22, .30, .2]                         # 대파 이랑(3)은 북주기로 높게
def ridge(obj, cx, h, seed):
    rr = random.Random(seed); n = 18; prof = []
    for k in range(n + 1):
        t = k / n; y = RL0 + (RL1 - RL0) * t
        e = min(t, 1 - t) * (RL1 - RL0); f = min(1, e / .35); f = .35 + .65 * (f * f * (3 - 2 * f))   # 끝 .35m 에서 낮아짐
        bw = (RW + rr.uniform(-.05, .05)) * (.8 + .2 * f); tw = bw * .62; hh = (h + rr.uniform(-.025, .025)) * f
        sh = rr.uniform(-.03, .03)
        prof.append([Vector((cx + sh - bw / 2, y, -.01)), Vector((cx + sh - tw / 2 - .06, y, hh * .72 + rr.uniform(-.01, .01))), Vector((cx + sh - tw / 4, y, hh + rr.uniform(-.015, .01))),
                     Vector((cx + sh, y, hh + rr.uniform(-.01, .015))), Vector((cx + sh + tw / 4, y, hh + rr.uniform(-.015, .01))),
                     Vector((cx + sh + tw / 2 + .06, y, hh * .72 + rr.uniform(-.01, .01))), Vector((cx + sh + bw / 2, y, -.01))])
    for k in range(n):
        A, B = prof[k], prof[k + 1]
        for i in range(6): quad_out(obj, [A[i], A[i + 1], B[i + 1], B[i]], 'soil_ridge', Vector((A[3].x, A[3].y, -.3)))
    for P_, sgn in ((prof[0], -1), (prof[-1], 1)):   # 끝 막음(단면 다각형 → 부채)
        c = sum(P_, Vector()) / len(P_)
        for i in range(6): quad_out(obj, [P_[i], P_[i + 1], c], 'soil_ridge', c - Vector((0, sgn * .5, 0)))
    return prof
RIDGE = [ridge('RIDGE_%02d' % (k + 1), RX[k], RH[k], 40 + k) for k in range(4)]
def ridge_top(k, y):  # 이랑 k 윗면 높이(y 에서 가장 가까운 마디)
    P_ = RIDGE[k]; t = (y - RL0) / (RL1 - RL0); i = max(0, min(len(P_) - 1, round(t * (len(P_) - 1)))); return P_[i][3].z, P_[i][3].x

# ---------- 배추(이랑 1·2): 속 = 위로 긴 통(위 끝 들쭉) + 바깥잎 6~8장 겹침·벌어짐. 실물 높이 .35 × 1.2 ----------
cabbages = []
def wrap_leaf(nm, cx, cy, z0, a0, arc, prof, flare, th, pt, seed):  # 속을 감싼 잎 = 부분 원통 껍질(바깥·안·옆·위 끝). prof = [(r, z)] 아래→위, 위 끝은 바깥으로 벌어짐
    rr = random.Random(seed); na = 6
    def P(i, j, dr):
        r, z = prof[j]; a = a0 + arc * i / na
        if j == len(prof) - 1: r += flare * (.6 + .4 * math.sin(math.pi * i / na)) ; z += rr.uniform(-.015, .01) if 0 < i < na else -.02
        return Vector((cx + (r + dr) * math.cos(a), cy + (r + dr) * math.sin(a), z))
    O = [[P(i, j, 0) for j in range(len(prof))] for i in range(na + 1)]; I = [[P(i, j, -th) for j in range(len(prof))] for i in range(na + 1)]
    ctr = Vector((cx, cy, prof[len(prof) // 2][1]))
    for i in range(na):
        for j in range(len(prof) - 1):
            quad_out(nm, [O[i][j], O[i + 1][j], O[i + 1][j + 1], O[i][j + 1]], pt, ctr)          # 바깥(속 가운데에서 먼 쪽)
            q = [I[i][j], I[i + 1][j], I[i + 1][j + 1], I[i][j + 1]]; n_ = (q[1] - q[0]).cross(q[2] - q[0]); c_ = sum(q, Vector()) / 4
            face(nm, q if n_.dot(ctr - c_) > 0 else q[::-1], pt)                                    # 안(가운데 쪽)
        quad_out(nm, [O[i][-1], O[i + 1][-1], I[i + 1][-1], I[i][-1]], pt, ctr - Vector((0, 0, 1)))   # 위 끝
    for i in (0, na):
        for j in range(len(prof) - 1): quad_out(nm, [O[i][j], O[i][j + 1], I[i][j + 1], I[i][j]], pt, (O[na // 2][j] + I[na // 2][j]) / 2)   # 옆 = 잎 가운데에서 먼 쪽
def cabbage(nm, cx, cy, z0, s, seed):
    """배추 = 속(좁은 통) + 감싼 잎 4~5장(층마다 .9cm 바깥, 위 끝이 벌어져 들쭉) + 바깥잎 8~9장(아래 4~5장은 땅으로 처지고 위 4장은 속을 반쯤 감쌈). 공 금지."""
    rr = random.Random(seed); H = .42 * s
    core = [(.05 * s, z0 - .02), (.085 * s, z0 + .1 * s), (.095 * s, z0 + .25 * s), (.08 * s, z0 + .36 * s), (.05 * s, z0 + .4 * s)]
    lathe(nm, cx, cy, core, 'cabbage_head', jit=.08, seed=seed, top=True)
    for L in range(rr.choice((4, 5))):  # 감싼 잎
        dr = .009 * (L + 1) * s; hh = H * rr.uniform(.88, 1.05) - L * .015 * s
        prof = [(.06 * s + dr, z0), (.098 * s + dr, z0 + hh * .25), (.108 * s + dr, z0 + hh * .6), (.098 * s + dr, z0 + hh * .85), (.085 * s + dr, z0 + hh)]
        wrap_leaf(nm, cx, cy, z0, rr.uniform(0, 6.28), rr.uniform(2.4, 3.2), prof, rr.uniform(.02, .05) * s, .008, 'cabbage_head', seed * 10 + L)
    nl = rr.choice((8, 9))
    for i in range(nl):  # 바깥잎: 짝수 = 땅으로 처짐(넓고 길게), 홀수 = 속을 반쯤 감싸고 선 잎. 가장자리 물결 = 마디 폭 번갈아
        yaw = 2 * math.pi * i / nl + rr.uniform(-.2, .2); k = s * rr.uniform(.85, 1.15)
        if i % 2 == 0:
            spine = [(.06, .02), (.13, .1), (.2, .16), (.27, .17), (.33, .13), (.37, .07), (.39, .03)]; wd = [.06, .15, .2, .17, .21, .14, .05]
        else:
            spine = [(.07, .03), (.1, .12), (.13, .21), (.16, .28), (.2, .32), (.24, .3)]; wd = [.06, .13, .17, .15, .17, .08]
        leaf(nm, (cx, cy, z0), yaw, [(r * k, z * k) for r, z in spine], [w * k for w in wd], .01, 'cabbage_leaf', roll=.3)
    cabbages.append(nm)
n = 0
for k in (0, 1):
    for i in range(9):  # 한 줄 지그재그, 간격 .46 ± .04
        y = RL0 + .3 + i * (RL1 - RL0 - .6) / 8 + J(.04); z, xc = ridge_top(k, y)
        n += 1; cabbage('CABBAGE_%02d' % n, xc + (.12 if i % 2 else -.12) + J(.03), y, z - .015, rng.uniform(1.05, 1.3), 100 + n)

# ---------- 대파(이랑 3): 포기(3대)마다 흰 줄기(16각, 흙에 묻힘) + 파란 잎 3~4장(위로 선 V 부채, 한 장만 끝이 꺾임 = 얇은 판). 실물 .65 × 1.15 ----------
onions = []; n = 0
for row in (-.14, .14):
    for i in range(11):
        y = RL0 + .2 + i * (RL1 - RL0 - .4) / 10 + (.1 if row > 0 else 0) + J(.03)
        if y > RL1 - .15: continue
        z, xc = ridge_top(2, y); n += 1; nm = 'ONION_%02d' % n; onions.append(nm)
        rr = random.Random(300 + n); cx = xc + row + J(.03)
        yb = rr.uniform(0, 6.28)   # 포기마다 잎 부채 방향 하나 — 대파 잎은 한 평면에 V 로 선다(사방으로 처지면 「작은 나무」로 읽힘, s03)
        for st in range(3):
            bx, by = cx + rr.uniform(-.035, .035), y + rr.uniform(-.035, .035); hgt = rr.uniform(.18, .25); tl = (rr.uniform(-.02, .02), rr.uniform(-.02, .02))
            tube(nm, [(bx, by, z - .04), (bx + tl[0] * .5, by + tl[1] * .5, z + hgt * .5), (bx + tl[0], by + tl[1], z + hgt)], [.019, .017, .015], 'onion_stem', cap0=False)
            for lf in range(rr.choice((3, 4))):
                yaw = yb + rr.choice((0, 3.14)) + rr.uniform(-.45, .45); L = rr.uniform(.36, .52); droop = rr.uniform(.0, .08) if lf else rr.uniform(.08, .16)
                spine = [(0, 0), (.012, L * .4), (.035 + droop * .3, L * .75), (.06 + droop, L * .95 - droop * .6)]
                leaf(nm, (bx + tl[0], by + tl[1], z + hgt - .02), yaw, spine, [.022, .021, .017, .007], .01, 'onion_leaf', roll=.25)

# ---------- 고추(이랑 4): 지주대(각재 3cm, 1.1m, ±4° 기울기) 두 줄 + 끈 2단 + 늦가을 포기(잎 처짐·빨간 고추 몇 개) ----------
stakes = []; n = 0; peppers = []; sl = []
for i in range(7):
    y = RL0 + .25 + i * (RL1 - RL0 - .5) / 6 + J(.04); z, xc = ridge_top(3, y)
    n += 1; nm = 'STAKE_%02d' % n; tl = Vector((J(.07), J(.07), 0))
    b = Vector((xc + J(.02), y, z - .12)); t = b + Vector((0, 0, 1.2)) + tl
    beam(nm, b, t, .035, .035, 'stake'); stakes.append(nm); sl.append((b, t))
    rr = random.Random(500 + i); pnm = 'PEPPER_%02d' % (i + 1); peppers.append(pnm)
    px, py = xc + .1 * (1 if i % 2 else -1), y + .12
    beam(pnm, (px, py, z - .03), (px + J(.03), py + J(.03), z + .48), .016, .016, 'pepper_stem')
    for lf in range(10):  # 잎 10장: 높이 .15~.5 에서 사방으로, 늦가을이라 아래로 처짐
        zz = z + rr.uniform(.15, .48); yaw = rr.uniform(0, 6.28); L = rr.uniform(.09, .14)
        leaf(pnm, (px, py, zz), yaw, [(0, 0), (L * .5, -.01), (L, -.05 - rr.uniform(0, .04))], [.02, .05, .01], .008, 'pepper_leaf', roll=.15)
    for pp in range(rr.choice((3, 4, 5))):  # 매달린 고추: 꼭지에서 아래로 길쭉, 끝으로 가늘어짐(16각)
        a = rr.uniform(0, 6.28); r0 = rr.uniform(.05, .1); hx, hy, hz = px + r0 * math.cos(a), py + r0 * math.sin(a), z + rr.uniform(.22, .42)
        tube(pnm, [(hx, hy, hz), (hx + .01, hy, hz - .06), (hx + .02, hy + .01, hz - .13)], [.016, .014, .002], 'pepper_fruit')
for row in (.45, .82):  # 끈: 지주대 앞면에 감김 — 지주대 사이 각 칸이 낱개 끈(끝은 지주대 안으로 1cm)
    for i in range(len(sl) - 1):
        (b0, t0), (b1, t1) = sl[i], sl[i + 1]
        z0_, z1_ = b0 + (t0 - b0) * ((row + .12) / 1.2), b1 + (t1 - b1) * ((row + .12) / 1.2)
        beam('STRING_%02d' % (len(stakes) + i + (0 if row < .5 else 10)), z0_ + Vector((.012, 0, 0)), z1_ + Vector((.012, 0, 0)), .006, .006, 'string')

# ---------- 울: 뒤·옆 = 나무 기둥(.08 각재, 높이 .8 ±.08, 기울기 ±4~8°, 간격 1.0 ±20%) + 가로대 2단(.09 × .025 판) ----------
posts = []
def fence_line(pts):  # pts = 꺾이는 점들(땅 위 xy)
    seg = []
    for (ax, ay), (bx, by) in zip(pts, pts[1:]):
        L = math.hypot(bx - ax, by - ay); nseg = max(1, round(L / 1.0)); tt = [0.0]
        for k in range(1, nseg): tt.append(k / nseg + rng.uniform(-.2, .2) / nseg)
        for t in tt: seg.append((ax + (bx - ax) * t, ay + (by - ay) * t))
    seg.append(pts[-1])
    P = []
    for x, y in seg:
        nm = 'POST_%02d' % (len(posts) + 1); h = .8 + J(.08); tilt = math.radians(rng.uniform(4, 8)) * rng.choice((-1, 1)); ta = rng.uniform(0, 6.28)
        b = Vector((x, y, -.15)); t = Vector((x + math.sin(tilt) * h * math.cos(ta), y + math.sin(tilt) * h * math.sin(ta), h))
        beam(nm, b, t, .08, .08, 'post'); posts.append(nm); P.append((b, t))
        # 기둥 머리 깎기(뭉툭한 끝 = 각재 끝 .04 비스듬) 는 2단계에서
    return P
FP = fence_line([(X0 + .5, Y0 + .02), (X0 + .04, Y0 + .02), (X0 + .04, Y1 - .04), (X1 - .04, Y1 - .04), (X1 - .04, Y0 + .02), (X1 - .5, Y0 + .02)])
rails = 0
for i in range(len(FP) - 1):
    (b0, t0), (b1, t1) = FP[i], FP[i + 1]
    for zf, ww in ((.32, .09), (.64, .09)):
        if zf > .5 and rng.random() < .12: continue      # 위 가로대 한두 칸 빠짐(손질된 낡음)
        p0 = b0 + (t0 - b0) * ((zf + .15) / (t0.z + .15)); p1 = b1 + (t1 - b1) * ((zf + .15) / (t1.z + .15))
        d = (p1 - p0).normalized(); out = d.cross(Vector((0, 0, 1))).normalized()   # 바깥 쪽(울 진행 방향 기준 오른쪽) — 기둥 앞면에 1cm 박힘
        ctr = Vector((0, 0, 0)); out = out if out.dot(((p0 + p1) / 2) - ctr) > 0 else -out
        off = out * (.04 + .0125 - .01)
        rails += 1; beam('RAIL_%02d' % rails, p0 - d * .06 + off, p1 + d * .06 + off, .025, ww, 'rail', up=tuple(out))

# ---------- 앞: 돌 한 줄(입구 틈 1.0m, 가운데에서 왼쪽으로 치우침) ----------
stones = []; x = X0 + .55; gate = (-.9, .1)
while x < X1 - .5:
    sx = rng.uniform(.26, .4)
    if gate[0] < x + sx / 2 and x - sx / 2 < gate[1]: x = gate[1] + .2; continue
    nm = 'STONE_%02d' % (len(stones) + 1); rock(nm, x, Y0 + .16 + J(.04), sx, rng.uniform(.2, .3), rng.uniform(.13, .22), 'stone', 700 + len(stones)); stones.append(nm)
    x += sx + rng.uniform(.0, .06)
for gx in (gate[0] - .12, gate[1] + .12):  # 입구 양옆 기둥(울 끝)
    nm = 'POST_%02d' % (len(posts) + 1); beam(nm, (gx, Y0 + .16, -.15), (gx + J(.02), Y0 + .16 + J(.02), .95), .09, .09, 'post'); posts.append(nm)

# ---------- 소품(SHAPE: 작은 것 1.5배): 물뿌리개 · 바구니(+ 뽑은 배추) — 입구 밖 풀 위(z 0) ----------
cx, cy = gate[1] + .55, Y0 - .45
lathe('CAN_01', cx, cy, [(.0, .0), (.13, .0), (.135, .02), (.135, .26), (.12, .3), (.06, .32)], 'can', top=True)
tube('CAN_01', [(cx + .11, cy, .05), (cx + .25, cy, .2), (cx + .38, cy, .36)], [.024, .019, .015], 'can', cap0=False, cap1=False)
tube('CAN_01', [(cx + .38, cy, .36), (cx + .41, cy, .39), (cx + .43, cy, .41)], [.015, .04, .045], 'can', cap0=False)
for a, b in (((cx - .13, cy, .2), (cx - .19, cy, .27)), ((cx - .19, cy, .27), (cx - .14, cy, .4)), ((cx - .14, cy, .4), (cx + .02, cy, .42)), ((cx + .02, cy, .42), (cx + .05, cy, .31))):
    beam('CAN_01', a, b, .025, .025, 'can')
bx, by = gate[0] - .65, Y0 - .5
lathe('BASKET_01', bx, by, [(.0, .0), (.2, .0), (.23, .05), (.27, .24), (.29, .26)], 'basket', top=False)
lathe('BASKET_01', bx, by, [(.27, .26), (.255, .24), (.215, .06), (.0, .05)][::-1], 'basket', top=False)   # 안쪽 벽·바닥(안을 보게: 단면 거꾸로)
for side in (-1, 1): beam('BASKET_01', (bx + side * .25, by, .24), (bx + side * .25, by, .26), .03, .05, 'basket')
cabbage('CABBAGE_H01', bx, by, .05, .8, 999)

# ---------- 메시(1단계: 재질 하나 = 회색, pt 는 measure.json 에 칸별 면 수로) ----------
mat = bpy.data.materials.new('FIELD_grey'); objs = {}
for obj, pts, pt in FACES: objs.setdefault(obj, []).append(pts)
obs = []; tris = 0; ptc = {}
for _, _, pt in FACES: ptc[pt] = ptc.get(pt, 0) + 1
for name, fl in objs.items():
    me = bpy.data.meshes.new(name); bm = bmesh.new(); allp = [p for f in fl for p in f]
    pv = Vector() if name == 'SOIL_base' else Vector(((min(p.x for p in allp) + max(p.x for p in allp)) / 2, (min(p.y for p in allp) + max(p.y for p in allp)) / 2, max(0, min(p.z for p in allp))))
    vmap = {}
    def V(p):
        k = (round(p.x, 5), round(p.y, 5), round(p.z, 5))
        if k not in vmap: vmap[k] = bm.verts.new(p - pv)
        return vmap[k]
    for f in fl:
        try: bm.faces.new([V(p) for p in f])
        except ValueError: pass
    bm.to_mesh(me); bm.free(); ob = bpy.data.objects.new(name, me); ob.location = pv; sc.collection.objects.link(ob); me.materials.append(mat); obs.append(ob)
    tris += sum(len(p.vertices) - 2 for p in me.polygons)
bpy.ops.object.select_all(action='DESELECT')
for ob in obs: ob.select_set(True)
bpy.context.view_layer.objects.active = obs[0]
bpy.ops.export_scene.fbx(filepath=OUT + 'field_patch.fbx', use_selection=True, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
                         path_mode='STRIP', embed_textures=False, mesh_smooth_type='FACE', object_types={'MESH'})
meas = dict(size_m=dict(w=W, d=D), ridges=dict(n=4, base_w=RW, furrow=RF, h=RH, len=round(RL1 - RL0, 2)), cabbage=len(cabbages), onion=len(onions), pepper=len(peppers),
            stakes=len(stakes), posts=len(posts), rails=rails, stones=len(stones), tris=tris, objects=len(obs), faces_by_pt=ptc,
            pivot='밭 바닥(땅 z 0) 가운데', forward='입구(돌 줄) = Blender -Y → FBX/Unity +Z')
json.dump(meas, open(OUT + 'measure.json', 'w', encoding='utf-8'), indent=1, ensure_ascii=False); print('MEAS', meas, flush=True)
sys.stdout.flush(); os._exit(0)
