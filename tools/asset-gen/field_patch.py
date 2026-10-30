"""집에 딸린 텃밭 field_patch — 과제 20261030-2003 (D-208 정정·D-210) 1단계 형태, 20261030-2056 2단계(PD 관찰 형태 손보기 + UV 아틀라스).
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/field_patch.py      (보통은 python -X utf8 tools/art/build_asset.py field_patch)
크기: 6.0 × 5.5m(바깥 울 기준). 이랑 4줄(길이 = Blender Y), 고랑, 뒤·옆 = 낮은 판자 울(2056: 높이 .62~.74 세로 판자 + 틈 — 1단계 .8m 각재+가로대는 「가축 우리」), 앞 = 돌 한 줄 + 입구 틈.
작물: 이랑 1·2 = 배추(속 + 감싼 잎 2 + 바깥잎 6, 공 금지), 이랑 3 = 대파(포기당 2대), 이랑 4 = 고추 지주대 + 늦가을 고추 포기. 소품: 물뿌리개 CAN_01 · 바구니 BASKET_01(+ 뽑은 배추) — 2056: 밭 안 앞쪽 통로(스튜디오 길 띠에 안 걸림).
2056 삼각형 3.9만 → 2만 이하: 잎 = 위·아래 두 장(1cm 옆면 없음), 배추 감싼 잎 4~5 → 2, 대파 3대 → 2·잎 3~4 → 2~3, 고추 잎 10 → 7.
축: 앞(입구) = Blender -Y → FBX/Unity +Z. 피벗 = 밭 바닥(땅 z 0) 가운데. 낱개 = 이름_번호 · 피벗 바닥 가운데(D-195).
겹친 면 0: 이랑·돌·기둥은 흙판(z ≥ .015)을 뚫고 내려가 교차(같은 평면 없음), 가로대는 기둥에, 판자는 가로대에 박힘. 원통(줄기·통·바구니) 16면.
UV(2056, house_home 과 같은 선반 쌓기): 면마다 실제 길이 × 밀도 — 작은 물건(작물·지주대·끈·통·바구니·돌) 64px/m, 나머지(흙·이랑·울) 32px/m. 칸 표 layout.json 을 build_asset.py field_patch 의 grain 이 사진으로 칠한다.
출력: art/export/field-patch/{field_patch.fbx, layout.json, measure.json}
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

def leaf(obj, base, yaw, spine, widths, thick, pt, roll=0.0, sides=False):  # 잎 = 얇은 판(위·아래, 2056: 1cm 옆면은 720 에서 안 보여 뺌). spine = [(앞으로 r, 높이 z)], widths = 마디 폭
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
        if sides:
            for j in (0, 1): quad_out(obj, [top[i][j], top[i + 1][j], bot[i + 1][j], bot[i][j]], pt, ctr)        # 옆
    if sides: quad_out(obj, [top[-1][0], top[-1][1], bot[-1][1], bot[-1][0]], pt, ctr)

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
RL0, RL1 = IY0 + .6, IY1 - .25                   # 이랑 시작·끝(앞 = 입구 쪽 통로 .6 — 2056: 바구니·물뿌리개를 여기 둠)
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
    rr = random.Random(seed); na = 4   # 2056: 6 → 4 마디
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
    """배추 = 속(좁은 통) + 감싼 잎 2장(층마다 .9cm 바깥, 위 끝이 벌어져 들쭉) + 바깥잎 6장(아래 3장은 땅으로 처지고 위 3장은 속을 반쯤 감쌈). 공 금지.
    2056: 감싼 잎 4~5 → 2, 바깥잎 8~9 → 6, 잎 마디 7 → 5(삼각형 배추 10k → 약 4k)."""
    rr = random.Random(seed); H = .42 * s
    core = [(.05 * s, z0 - .02), (.09 * s, z0 + .12 * s), (.085 * s, z0 + .33 * s), (.05 * s, z0 + .4 * s)]
    lathe(nm, cx, cy, core, 'cabbage_head', jit=.08, seed=seed, top=True)
    for L in range(2):  # 감싼 잎
        dr = .009 * (L + 1) * s; hh = H * rr.uniform(.88, 1.05) - L * .015 * s
        prof = [(.06 * s + dr, z0), (.104 * s + dr, z0 + hh * .45), (.098 * s + dr, z0 + hh * .8), (.085 * s + dr, z0 + hh)]
        wrap_leaf(nm, cx, cy, z0, rr.uniform(0, 6.28), rr.uniform(2.6, 3.4), prof, rr.uniform(.02, .05) * s, .008, 'cabbage_head', seed * 10 + L)
    nl = 6
    for i in range(nl):  # 바깥잎: 짝수 = 땅으로 처짐(넓고 길게), 홀수 = 속을 반쯤 감싸고 선 잎. 가장자리 물결 = 마디 폭 번갈아
        yaw = 2 * math.pi * i / nl + rr.uniform(-.25, .25); k = s * rr.uniform(.85, 1.15)
        if i % 2 == 0:
            spine = [(.06, .02), (.16, .13), (.27, .17), (.35, .1), (.39, .03)]; wd = [.07, .19, .2, .17, .05]
        else:
            spine = [(.07, .03), (.115, .17), (.16, .28), (.21, .32), (.24, .3)]; wd = [.07, .16, .15, .17, .08]
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
        for st in range(2):  # 2056: 포기당 3대 → 2대, 줄기 마디 2 → 1, 잎 3~4 → 2~3
            sa = yb + 1.57 + st * 3.14; bx, by = cx + .03 * math.cos(sa) + rr.uniform(-.01, .01), y + .03 * math.sin(sa) + rr.uniform(-.01, .01);   # 두 대는 6cm 떨어져(옛 ±3.5cm 무작위는 겹쳐 치지직)
            hgt = rr.uniform(.18, .25); tl = (rr.uniform(-.02, .02), rr.uniform(-.02, .02))
            tube(nm, [(bx, by, z - .04), (bx + tl[0], by + tl[1], z + hgt)], [.02, .016], 'onion_stem', cap0=False)
            for lf in range(rr.choice((2, 3))):
                yaw = yb + rr.choice((0, 3.14)) + rr.uniform(-.45, .45); L = rr.uniform(.36, .52); droop = rr.uniform(.0, .08) if lf else rr.uniform(.08, .16)
                spine = [(0, 0), (.012, L * .4), (.035 + droop * .3, L * .75), (.06 + droop, L * .95 - droop * .6)]
                leaf(nm, (bx + tl[0] + .008 * lf * math.cos(yaw), by + tl[1] + .008 * lf * math.sin(yaw), z + hgt - .02 - .015 * lf), yaw + .25 * lf, spine, [.022, .021, .017, .007], .01, 'onion_leaf', roll=.25)   # 잎마다 밑동 8mm·1.5cm 비켜(같은 자리 겹침 = 치지직)

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
    for lf in range(7):  # 잎 7장(2056: 10 → 7): 높이 .15~.5 에서 사방으로, 늦가을이라 아래로 처짐
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

# ---------- 울(2056): 뒤·옆 = 낮은 판자 울. 기둥 .06 각재(높이 .72, 간격 ~1.4m) + 가로대 2단(.05 × .018) + 세로 판자(폭 .085 · 두께 .016 · 높이 .62~.74, 틈 .045~.07) ----------
# 1단계(.8m 각재 + 굵은 가로대 2단)는 PD 「가축 우리 같음」. 판자는 바깥 쪽, 가로대에 3mm 박힘 · 가로대는 기둥 바깥 면에 5mm 박힘(같은 평면 없음).
posts, fences = [], []; rails = planks = 0; SEGN = [0]
def fence_line(nm, pts):  # pts = 꺾이는 점들(땅 위 xy). 한 줄 = 낱개 하나(FENCE_nn)
    global rails, planks
    for (ax, ay), (bx, by) in zip(pts, pts[1:]):
        a, b = Vector((ax, ay, 0)), Vector((bx, by, 0)); L = (b - a).length; d = (b - a) / L
        out = d.cross(Vector((0, 0, 1))).normalized()
        if out.dot((a + b) / 2) < 0: out = -out                      # 밭 가운데(0,0)에서 먼 쪽
        np_ = max(1, round(L / 1.4)); tt = [k / np_ for k in range(np_ + 1)]
        for t in tt:                                                  # 기둥(꼭짓점 기둥은 두 줄이 같은 자리에 두 번 서지 않게 첫 줄만)
            p = a + (b - a) * t
            if any((p.xy - q).length < .05 for q in posts): continue
            h = .72 + J(.03); lean = Vector((J(.02), J(.02), 0))
            beam(nm, p + Vector((0, 0, -.15)), p + Vector((0, 0, h)) + lean, .06, .06, 'post'); posts.append(p.xy.copy())
        zk = (SEGN[0] % 2) * .014; SEGN[0] += 1       # 꺾이는 모서리에서 이웃 줄 가로대가 같은 높이(같은 평면)로 겹치지 않게 한 칸 걸러 1.4cm 위
        for zf in (.18 + zk, .5 + zk):                                # 가로대 2단(단면: 세로 .05 · 바깥으로 .018)
            off = out * (.03 + .009 - .005); rails += 1
            beam(nm, a + off + Vector((0, 0, zf)), b + off + Vector((0, 0, zf + J(.01))), .05, .018, 'rail', up=tuple(out))   # 끝 = 기둥 가운데(옛 3cm 내밂은 모서리에서 옆 줄 판자·가로대와 같은 평면)
        x = .05 + rng.uniform(0, .04)
        while x < L - .06:                                            # 세로 판자(높이 들쭉 ±6cm, 살짝 기움 — 손질된 낡음)
            w = .085 + J(.01); c = a + d * (x + w / 2) + out * (.03 + .018 - .005 + .008 - .003)
            h = rng.uniform(.62, .74); tl = d * J(.015)
            beam(nm, c + Vector((0, 0, -.04)), c + tl + Vector((0, 0, h)), w, .016, 'plank', caps=(False, True), up=tuple(out)); planks += 1   # 폭 = 줄 방향, 두께 = 바깥 방향
            x += w + rng.uniform(.045, .07)
    fences.append(nm)
fence_line('FENCE_01', [(X0 + .5, Y0 + .02), (X0 + .04, Y0 + .02), (X0 + .04, Y1 - .04)])   # 서쪽(앞 꺾임 포함)
fence_line('FENCE_02', [(X0 + .04, Y1 - .04), (X1 - .04, Y1 - .04)])                        # 뒤
fence_line('FENCE_03', [(X1 - .04, Y1 - .04), (X1 - .04, Y0 + .02), (X1 - .5, Y0 + .02)])   # 동쪽

# ---------- 앞: 돌 한 줄(입구 틈 1.0m, 가운데에서 왼쪽으로 치우침) ----------
stones = []; x = X0 + .55; gate = (-.9, .1)
while x < X1 - .5:
    sx = rng.uniform(.26, .4)
    if gate[0] < x + sx / 2 and x - sx / 2 < gate[1]: x = gate[1] + .2; continue
    nm = 'STONE_%02d' % (len(stones) + 1); rock(nm, x, Y0 + .16 + J(.04), sx, rng.uniform(.2, .3), rng.uniform(.13, .22), 'stone', 700 + len(stones)); stones.append(nm)
    x += sx + rng.uniform(.0, .06)
gposts = []
for gx in (gate[0] - .12, gate[1] + .12):  # 입구 양옆 기둥(울 끝) — 2056: 낮은 울에 맞춰 .95 → .8, 굵기 .09 → .07
    nm = 'GATEPOST_%02d' % (len(gposts) + 1); beam(nm, (gx, Y0 + .16, -.15), (gx + J(.02), Y0 + .16 + J(.02), .8), .07, .07, 'post'); gposts.append(nm)

# ---------- 소품(SHAPE: 작은 것 1.5배): 물뿌리개 · 바구니(+ 뽑은 배추) — 2056: 밭 안 앞 통로(돌 줄 ~ 이랑 시작 y Y0+.30~.90) 흙 위 ----------
# 1단계는 입구 밖 잔디(Y0 −.45~−.5)라 스튜디오 길 띠(Unity z 1.5~)에 걸렸다. 바닥 = 흙판(z .006~.028) 사이 .014 — 반은 흙에 묻혀 뜨지 않음.
ZP = .014
cx, cy = gate[1] + .9, Y0 + .55
lathe('CAN_01', cx, cy, [(r, z + ZP) for r, z in [(.0, .0), (.13, .0), (.135, .02), (.135, .26), (.12, .3), (.06, .32)]], 'can', top=True)
tube('CAN_01', [(cx + .11, cy, .05 + ZP), (cx + .25, cy, .2 + ZP), (cx + .38, cy, .36 + ZP)], [.024, .019, .015], 'can', cap0=False, cap1=False)
tube('CAN_01', [(cx + .38, cy, .36 + ZP), (cx + .41, cy, .39 + ZP), (cx + .43, cy, .41 + ZP)], [.015, .04, .045], 'can', cap0=False)
for a, b in (((cx - .13, cy, .2), (cx - .19, cy, .27)), ((cx - .19, cy, .27), (cx - .14, cy, .4)), ((cx - .14, cy, .4), (cx + .02, cy, .42)), ((cx + .02, cy, .42), (cx + .05, cy, .31))):
    k = (a[0] + a[2]) * 100 % 2 < 1   # 손잡이 마디 이음에서 옆면(±y)이 같은 평면으로 겹치지 않게 마디마다 두께 2.5/2.8cm 번갈아
    beam('CAN_01', (a[0], a[1], a[2] + ZP), (b[0], b[1], b[2] + ZP), .025 if k else .029, .025 if k else .029, 'can')
bx, by = gate[0] + .5, Y0 + .6
lathe('BASKET_01', bx, by, [(r, z + ZP) for r, z in [(.0, .0), (.2, .0), (.23, .05), (.27, .24), (.29, .26)]], 'basket', top=False)
lathe('BASKET_01', bx, by, [(r, z + ZP) for r, z in [(.27, .26), (.255, .24), (.215, .06), (.0, .05)][::-1]], 'basket', top=False)   # 안쪽 벽·바닥(안을 보게: 단면 거꾸로)
for side in (-1, 1): beam('BASKET_01', (bx + side * .25, by, .24 + ZP), (bx + side * .25, by, .26 + ZP), .03, .05, 'basket')
cabbage('CABBAGE_19', bx, by, .05 + ZP, .8, 999)   # 뽑아 둔 배추(옛 CABBAGE_H01 — 이름_번호 규칙)

# ---------- UV 칸 배치(house_home 과 같은 선반 쌓기) → 메시. 2056: 1단계 회색 → 칸 표 layout.json(build_asset.py field_patch grain 이 사진으로 칠함) ----------
SMALL = ('CABBAGE', 'ONION', 'PEPPER', 'STAKE', 'STRING', 'CAN', 'BASKET', 'STONE')   # 작은 물건 = 64px/m (ART-RULES·교훈 4), 나머지(흙·이랑·울) 32
def dens_for(obj): return 64 if obj.split('_')[0] in SMALL else 32
def local2d(pts):
    nrm = Vector()
    for i in range(len(pts)): a, b = pts[i], pts[(i + 1) % len(pts)]; nrm += Vector(((a.y - b.y) * (a.z + b.z), (a.z - b.z) * (a.x + b.x), (a.x - b.x) * (a.y + b.y)))
    nrm.normalize(); u = (pts[1] - pts[0]).normalized(); v = nrm.cross(u)
    return [((p - pts[0]).dot(u), (p - pts[0]).dot(v)) for p in pts]
AW = 2048; items = []; OZ = {}
for obj, pts, pt in FACES:
    for q in pts: a, b = OZ.get(obj, (q.z, q.z)); OZ[obj] = (min(a, q.z), max(b, q.z))
for i, (obj, pts, pt) in enumerate(FACES):
    if len(pts) < 3 or (pts[1] - pts[0]).length < 1e-6: continue
    d = dens_for(obj); l2 = local2d(pts); u0, v0 = min(a for a, _ in l2), min(b for _, b in l2)
    um, vm = max(a for a, _ in l2) - u0, max(b for _, b in l2) - v0
    hi, lo = max(range(len(pts)), key=lambda k: pts[k].z), min(range(len(pts)), key=lambda k: pts[k].z)   # vz = 칸의 v(행)가 위로 갈수록 커지면 +1 — 대파 흰 줄기→초록, 배추 속 위 끝 색
    vz = 0 if pts[hi].z - pts[lo].z < .02 else (1 if l2[hi][1] > l2[lo][1] else -1)
    za, zb = OZ[obj]; zs = max(zb - za, 1e-6); z01 = [round((pts[lo].z - za) / zs, 3), round((pts[hi].z - za) / zs, 3)]   # 칸의 높이 = 물건 안 0(밑)~1(꼭대기)
    items.append(dict(i=i, obj=obj, pt=pt, dens=d, vz=vz, z01=z01, l2=[(a - u0, b - v0) for a, b in l2], m=(um, vm), w=max(2, math.ceil(um * d)), h=max(2, math.ceil(vm * d))))
# 칠 종류(pt)마다 새 줄부터 — 0.25m 검사 칸에 다른 칠 색이 섞이지 않게(house_home check 2·3차). 줄 시작은 16px 격자
order = sorted(items, key=lambda t: (t['pt'], -t['h'], -t['w'])); x = y = 4; rowh = 0; grp = None
for t in order:
    if x + t['w'] + 4 > AW or (grp is not None and t['pt'] != grp):
        x, y, rowh = 4, y + rowh + 8, 0; y = -(-y // 16) * 16
    grp = t['pt']; t['x'], t['y'] = x, y; x += t['w'] + 6; rowh = max(rowh, t['h'])
AH = math.ceil((y + rowh + 4) / 128) * 128
mat = bpy.data.materials.new('FIELD_alb'); objs = {}
for t in items: objs.setdefault(t['obj'], []).append((FACES[t['i']][1], [((t['x'] + .5 + a * t['dens']) / AW, (t['y'] + .5 + b * t['dens']) / AH) for a, b in t['l2']]))
obs = []; tris = 0; ptc = {}
for t in items: ptc[t['pt']] = ptc.get(t['pt'], 0) + 1
for name, fl in objs.items():
    me = bpy.data.meshes.new(name); bm = bmesh.new(); uvl = bm.loops.layers.uv.new('UVMap'); allp = [p for f, _ in fl for p in f]
    pv = Vector() if name == 'SOIL_base' else Vector(((min(p.x for p in allp) + max(p.x for p in allp)) / 2, (min(p.y for p in allp) + max(p.y for p in allp)) / 2, max(0, min(p.z for p in allp))))
    for pts, uvs in fl:
        try: f = bm.faces.new([bm.verts.new(p - pv) for p in pts])
        except ValueError: continue
        for lp, uv in zip(f.loops, uvs): lp[uvl].uv = uv
    bm.to_mesh(me); bm.free(); ob = bpy.data.objects.new(name, me); ob.location = pv; sc.collection.objects.link(ob); me.materials.append(mat); obs.append(ob)
    tris += sum(len(p.vertices) - 2 for p in me.polygons)
bpy.ops.object.select_all(action='DESELECT')
for ob in obs: ob.select_set(True)
bpy.context.view_layer.objects.active = obs[0]
bpy.ops.export_scene.fbx(filepath=OUT + 'field_patch.fbx', use_selection=True, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
                         path_mode='STRIP', embed_textures=False, mesh_smooth_type='FACE', object_types={'MESH'})
meas = dict(size_m=dict(w=W, d=D), ridges=dict(n=4, base_w=RW, furrow=RF, h=RH, len=round(RL1 - RL0, 2)), cabbage=len(cabbages), onion=len(onions), pepper=len(peppers),
            stakes=len(stakes), posts=len(posts) + len(gposts), rails=rails, planks=planks, fence=dict(h='.62~.74 판자 · 기둥 .72', objs=fences), stones=len(stones), tris=tris, objects=len(obs), faces_by_pt=ptc,
            atlas=[AW, AH], props=dict(CAN_01=[round(cx, 2), round(cy, 2)], BASKET_01=[round(bx, 2), round(by, 2)]),
            pivot='밭 바닥(땅 z 0) 가운데', forward='입구(돌 줄) = Blender -Y → FBX/Unity +Z')
lay = dict(atlas=[AW, AH], cells=[dict(obj=t['obj'], pt=t['pt'], dens=t['dens'], rect=[t['x'], t['y'], t['w'], t['h']], m=[round(t['m'][0], 3), round(t['m'][1], 3)], vz=t['vz'], z01=t['z01'], seed=t['i']) for t in items])
json.dump(lay, open(OUT + 'layout.json', 'w', encoding='utf-8'), indent=0)
json.dump(meas, open(OUT + 'measure.json', 'w', encoding='utf-8'), indent=1, ensure_ascii=False); print('MEAS', meas, flush=True)
sys.stdout.flush(); os._exit(0)
