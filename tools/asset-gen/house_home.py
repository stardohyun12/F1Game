"""주인공 2층집 house_home — 과제 20261029-0031 (D-198·D-199). Blender bpy 절차 생성(형태·UV 칸만, 텍스처는 build_asset.py house_home grain):
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/house_home.py
형태(0052, D-200 넓직·엄마와 둘): 바깥 8.6×6.0m, 1층 3.2m + 2층 = 주인공 방(art/export/q1-room, 프리팹 자식, 바닥 z 3.2, 방 가운데 y = FY+1.65) + 박공(용마루 X축, 24°).
  비탈 기단(돌 0.9m) = 앞 테라스 1.2m + 현관 계단. 1층: 현관 DOOR_01 · 거실 창(열린 덧창) · 부엌 창(커튼 CURTAIN_01·02 + 창턱 화분 PLANTER_03/PLANT_03) · 엄마 의자 CHAIR_01.
  2층: 방 발코니 문(구멍 x ±.45, 방 q1_room.py 창 벽을 문으로) + 문짝 DOOR_BALCONY(1608: 벽 안 닫힌 유리문, 피벗 경첩) + 엄마 방 창 2(열린 덧창).
  발코니 = 방 폭 3.5m(돌판·까치발, 현관 차양 겸) + 난간 RAIL_01 + 화분 PLANTER_01·02.
축: 정면(발코니·현관) = Blender -Y → FBX/Unity +Z. 피벗 = 집 바닥(기단 윗면 z 0) 가운데 = 방 바닥 가운데의 수직 아래.
텍셀: 껍데기 32px/m, 낱개 물건은 면의 긴 변 ≤1.5m 면 64(check_rules 와 같은 규칙). 겹친 면 0: 붙는 면은 skip, 덧댄 판은 2~4cm 띄움.
출력: art/export/house-home/{house_home.fbx, layout.json(칸 표), measure.json}
"""
import bpy, bmesh, os, sys, json, math
from mathutils import Vector
R = 'D:/Github/F1Game/'; OUT = R + 'art/export/house-home/'; os.makedirs(OUT, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True); sc = bpy.context.scene

WX, FY, BY = 4.3, -3.0, 3.0           # 0052(D-200 넓직): 바깥벽 반폭 8.6m · 앞면 · 뒷면 (깊이 6m) — 폭 8.6 = 처마 높이 5.85 × 1.47
H2, WT = 3.2, 5.85                     # 2층 바닥(방 바닥) · 벽 윗선(방 천장 5.6 위)
RY = FY + 1.65                         # 방 가운데 y(방 창→문 벽 바깥면 = FY). Unity 에선 ROOM_2F = (0, H2, -RY)
PIT = math.tan(math.radians(24)); OVF, OVS, RT = .35, .25, .09   # 지붕 기울기 · 앞뒤 처마 · 옆 처마 · 지붕 두께
SHELL = 'HOUSE_shell'
FACES = []                              # (obj, [Vector], pt, dens)
PIVOT = {}                              # 낱개 피벗 예외(문 = 경첩 아래)

def dens_for(obj, pts):
    if obj == SHELL: return 32
    L = max((pts[i] - pts[i - 1]).length for i in range(len(pts)))
    return 64 if L <= 1.5 else 32
def face(obj, pts, pt):
    pts = [Vector(p) for p in pts]; FACES.append((obj, pts, pt, dens_for(obj, pts)))
def box(obj, x0, x1, y0, y1, z0, z1, pt, skip=()):
    P = pt if isinstance(pt, dict) else {'*': pt}; g = lambda k: P.get(k, P['*'])
    S = {'front': [(x0, y0, z0), (x1, y0, z0), (x1, y0, z1), (x0, y0, z1)], 'back': [(x1, y1, z0), (x0, y1, z0), (x0, y1, z1), (x1, y1, z1)],
         'left': [(x0, y1, z0), (x0, y0, z0), (x0, y0, z1), (x0, y1, z1)], 'right': [(x1, y0, z0), (x1, y1, z0), (x1, y1, z1), (x1, y0, z1)],
         'top': [(x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)], 'bottom': [(x0, y1, z0), (x1, y1, z0), (x1, y0, z0), (x0, y0, z0)]}
    for k, p in S.items():
        if k not in skip: face(obj, p, g(k))
def obox(obj, o, d, L, n, T, z0, z1, pt, skip=()):  # 돌린 상자(바닥 평행): o = 한 모서리, d·n = 수평 단위(길이 L·두께 T). 면 키 a0(d 시작)·a1·n0(o 쪽)·n1·top·bottom
    o, d, n = Vector(o), Vector(d), Vector(n); c = [o, o + d * L, o + d * L + n * T, o + n * T]; ctr = sum(c, Vector()) / 4
    q = {'n0': (c[0], c[1]), 'a1': (c[1], c[2]), 'n1': (c[2], c[3]), 'a0': (c[3], c[0])}
    for k, (p, r) in q.items():
        if k in skip: continue
        f = [(p.x, p.y, z0), (r.x, r.y, z0), (r.x, r.y, z1), (p.x, p.y, z1)]
        if ((r - p).cross(Vector((0, 0, 1)))).dot((p + r) / 2 - ctr) < 0: f = [f[1], f[0], f[3], f[2]]
        face(obj, f, pt)
    up = [(v.x, v.y, z1) for v in c]; dn = [(v.x, v.y, z0) for v in c]
    if ((c[1] - c[0]).cross(c[3] - c[0])).z < 0: up = up[::-1]
    else: dn = dn[::-1]
    if 'top' not in skip: face(obj, up, pt)
    if 'bottom' not in skip: face(obj, dn, pt)
def wall(side, a0, a1, z0, z1, pt='wall', obj=SHELL):  # 바깥에서 본 반시계(왼아래부터) → 법선 바깥, 텍스처 v = 위
    if side == 'F': face(obj, [(a0, FY, z0), (a1, FY, z0), (a1, FY, z1), (a0, FY, z1)], pt)
    if side == 'B': face(obj, [(a1, BY, z0), (a0, BY, z0), (a0, BY, z1), (a1, BY, z1)], pt)
    if side == 'L': face(obj, [(-WX, a1, z0), (-WX, a0, z0), (-WX, a0, z1), (-WX, a1, z1)], pt)
    if side == 'R': face(obj, [(WX, a0, z0), (WX, a1, z0), (WX, a1, z1), (WX, a0, z1)], pt)

# ---------- 구멍(x0, x1, z0, z1) — 1층: 현관 · 거실 창(열린 덧창) · 부엌 창(커튼·화분) / 2층: 방 발코니 문(방 벽 구멍과 같은 자리) · 엄마 방 창 2 ----------
DX0, DX1, DZ = -.45, .45, 2.15
WL, WK = (-3.3, -2.1, .95, 2.15), (1.9, 3.1, .95, 2.15)
DOX, DIVX = 2.4, .55                                   # 0133 도현 "방 2개": 주인공 방(q1_room)을 집 오른쪽 끝으로(방 가운데 x = 2.4, 동쪽 벽 = 집 안쪽 벽 4.15) · 부모님 방 = 그 왼쪽 전부
PW = DIVX + .1                                         # 주인공 방 서쪽 벽면(방 로컬 x -1.75 = 0.65)
BD = (DOX - .45, DOX + .45, H2, H2 + 2.05)             # q1_room.py 창 벽 문 구멍(방 로컬 x ±.45, z 0..2.05) → 방과 함께 +2.4
W2L = (-3.4, -2.5, 4.0, 5.2)                            # 부모님 방 앞 창(0133 의 W2M 은 발코니 문 BD2 로 바뀜)
DOX2 = -.55                                            # 1421 도현 "부모님 방에서도 발코니 문 1개": 부모님 방 발코니 문 가운데 x
BD2 = (DOX2 - .45, DOX2 + .45, H2, H2 + 2.05)
SWY = (-2.4, -1.5, 4.0, 5.2)                           # 1421 18e9 "이쯤에 창문" Unity (-4.3, 4.68, 1.948) = Blender (+4.3, y -1.948, z 4.68) (FBX 가져오기에서 x 뒤집힘): 동쪽 벽(주인공 방) 창(y0, y1, z0, z1)
HOLES = [(DX0, DX1, 0, DZ), WL, WK, BD, BD2, W2L]
zs = sorted({0, WT} | {h[2] for h in HOLES} | {h[3] for h in HOLES})
for za, zb in zip(zs, zs[1:]):  # 앞벽 = 높이 띠마다 구멍 사이 토막
    xs = [-WX]
    for x0, x1, z0, z1 in sorted(HOLES):
        if z0 <= za and zb <= z1: wall('F', xs[-1], x0, za, zb) if x0 > xs[-1] else None; xs.append(x1)
    wall('F', xs[-1], WX, za, zb)
wall('B', -WX, WX, 0, WT); wall('L', FY, BY, 0, WT)
for za, zb, ya, yb in ((0, SWY[2], FY, BY), (SWY[2], SWY[3], FY, SWY[0]), (SWY[2], SWY[3], SWY[1], BY), (SWY[3], WT, FY, BY)): wall('R', ya, yb, za, zb)   # 동쪽 벽 = 창 구멍 둘레 토막
zr = WT + PIT * BY                      # 박공 꼭짓점(지붕 밑면)
face(SHELL, [(-WX, BY, WT), (-WX, FY, WT), (-WX, 0, zr)], 'wall'); face(SHELL, [(WX, FY, WT), (WX, BY, WT), (WX, 0, zr)], 'wall')
# 현관 문틀 안쪽 면(벽 두께 15cm 전부 — 0351: 안에서 보면 틈이 없게)
DY = FY + .13; YI = FY + .15
face(SHELL, [(DX0, FY, 0), (DX0, YI, 0), (DX0, YI, DZ), (DX0, FY, DZ)], 'trim')
face(SHELL, [(DX1, YI, 0), (DX1, FY, 0), (DX1, FY, DZ), (DX1, YI, DZ)], 'trim')
face(SHELL, [(DX0, YI, DZ), (DX1, YI, DZ), (DX1, FY, DZ), (DX0, FY, DZ)], 'trim')
GLASS = []                              # 0351: 유리 = 낱개 GLASS_NN, 재질 HOUSE_glass(Unity M_house_home_glass 반투명)
def inset(x0, x1, z0, z1):  # 0351 창(도현 "실제 창같이"): 벽 두께 15cm 흰 면 4 + 창틀 5cm(바깥면에서 6~10cm 안) + 창살 2cm 십자 + 반투명 유리 4장
    face(SHELL, [(x0, FY, z0), (x1, FY, z0), (x1, YI, z0), (x0, YI, z0)], 'trim'); face(SHELL, [(x0, YI, z1), (x1, YI, z1), (x1, FY, z1), (x0, FY, z1)], 'trim')
    face(SHELL, [(x0, FY, z0), (x0, YI, z0), (x0, YI, z1), (x0, FY, z1)], 'trim'); face(SHELL, [(x1, YI, z0), (x1, FY, z0), (x1, FY, z1), (x1, YI, z1)], 'trim')
    fw, ya, yb = .05, FY + .06, FY + .10                # 틀: 흰 면 4 에 붙는 면은 skip
    box(SHELL, x0, x0 + fw, ya, yb, z0, z1, 'trim', ('left', 'top', 'bottom')); box(SHELL, x1 - fw, x1, ya, yb, z0, z1, 'trim', ('right', 'top', 'bottom'))
    box(SHELL, x0 + fw, x1 - fw, ya, yb, z0, z0 + fw, 'trim', ('left', 'right', 'bottom')); box(SHELL, x0 + fw, x1 - fw, ya, yb, z1 - fw, z1, 'trim', ('left', 'right', 'top'))
    cx, cz = (x0 + x1) / 2, (z0 + z1) / 2 + .1           # 창살(틀보다 얕게 1cm씩 = 틀 앞뒤 면과 같은 면 0)
    box(SHELL, cx - .01, cx + .01, ya + .01, yb - .01, z0 + fw, z1 - fw, 'trim', ('top', 'bottom'))
    for a, b in ((x0 + fw, cx - .01), (cx + .01, x1 - fw)): box(SHELL, a, b, ya + .01, yb - .01, cz - .01, cz + .01, 'trim', ('left', 'right'))
    yg = FY + .08
    for a, b in ((x0 + fw, cx - .01), (cx + .01, x1 - fw)):
        for c, d in ((z0 + fw, cz - .01), (cz + .01, z1 - fw)):
            nmg = 'GLASS_%02d' % (len(GLASS) + 1); GLASS.append(nmg); face(nmg, [(a, yg, c), (b, yg, c), (b, yg, d), (a, yg, d)], 'glass')
for w_ in (WL, WK, W2L): inset(*w_)
# 발코니 문 구멍: 벽 두께 흰 면 3 + 문턱(방 q1_room 의 껍데기를 버려서 집이 직접 — 0351)
bx0, bx1, bz0, bz1 = BD
face(SHELL, [(bx0, FY, bz0), (bx0, YI, bz0), (bx0, YI, bz1), (bx0, FY, bz1)], 'trim'); face(SHELL, [(bx1, YI, bz0), (bx1, FY, bz0), (bx1, FY, bz1), (bx1, YI, bz1)], 'trim')
face(SHELL, [(bx0, YI, bz1), (bx1, YI, bz1), (bx1, FY, bz1), (bx0, FY, bz1)], 'trim'); face(SHELL, [(bx0, FY, bz0), (bx1, FY, bz0), (bx1, YI, bz0), (bx0, YI, bz0)], 'trim')
bx0, bx1, bz0, bz1 = BD2
face(SHELL, [(bx0, FY, bz0), (bx0, YI, bz0), (bx0, YI, bz1), (bx0, FY, bz1)], 'trim'); face(SHELL, [(bx1, YI, bz0), (bx1, FY, bz0), (bx1, FY, bz1), (bx1, YI, bz1)], 'trim')
face(SHELL, [(bx0, YI, bz1), (bx1, YI, bz1), (bx1, FY, bz1), (bx0, FY, bz1)], 'trim'); face(SHELL, [(bx0, FY, bz0), (bx1, FY, bz0), (bx1, YI, bz0), (bx0, YI, bz0)], 'trim')
# 흰 틀(2cm 돋음): 옆은 위·아래·뒤 skip(위 띠·창턱·발코니 바닥과 같은 면 0)
def surround(x0, x1, z0, z1, sill=True, head=True):
    y0 = FY - .02
    box(SHELL, x0 - .12, x0, y0, FY, z0, z1, 'trim', ('back', 'top', 'bottom')); box(SHELL, x1, x1 + .12, y0, FY, z0, z1, 'trim', ('back', 'top', 'bottom'))
    if head: box(SHELL, x0 - .12, x1 + .12, y0, FY, z1, z1 + .12, 'trim', ('back',))
    if sill: box(SHELL, x0 - .18, x1 + .18, FY - .06, FY, z0 - .08, z0, 'trim', ('back',))
surround(DX0, DX1, 0, DZ, sill=False); surround(*BD, sill=False); surround(*BD2, sill=False)
for w_ in (WL, WK, W2L): surround(*w_)
# 1421 18e9 동쪽 벽 창(주인공 방): 앞 창과 같은 규칙 — 벽 두께 흰 면 4 + 틀 5cm + 십자 살 + 반투명 유리 4 + 바깥 돋운 흰 틀 + 활짝 열린 덧창 둘
def qf(obj, pts, pt, hint):  # 법선이 hint 쪽을 보게 자동으로 감는 사각 면
    P = [Vector(p) for p in pts]; n = (P[1] - P[0]).cross(P[2] - P[0])
    face(obj, pts if n.dot(Vector(hint)) > 0 else pts[::-1], pt)
def inset_side(y0, y1, z0, z1):
    xo, xi = WX, XI0
    qf(SHELL, [(xo, y0, z0), (xi, y0, z0), (xi, y1, z0), (xo, y1, z0)], 'trim', (0, 0, 1)); qf(SHELL, [(xo, y0, z1), (xi, y0, z1), (xi, y1, z1), (xo, y1, z1)], 'trim', (0, 0, -1))
    qf(SHELL, [(xo, y0, z0), (xi, y0, z0), (xi, y0, z1), (xo, y0, z1)], 'trim', (0, 1, 0)); qf(SHELL, [(xo, y1, z0), (xi, y1, z0), (xi, y1, z1), (xo, y1, z1)], 'trim', (0, -1, 0))
    fw, xa, xb = .05, WX - .10, WX - .06
    box(SHELL, xa, xb, y0, y0 + fw, z0, z1, 'trim', ('front', 'top', 'bottom')); box(SHELL, xa, xb, y1 - fw, y1, z0, z1, 'trim', ('back', 'top', 'bottom'))
    box(SHELL, xa, xb, y0 + fw, y1 - fw, z0, z0 + fw, 'trim', ('front', 'back', 'bottom')); box(SHELL, xa, xb, y0 + fw, y1 - fw, z1 - fw, z1, 'trim', ('front', 'back', 'top'))
    cy, cz = (y0 + y1) / 2, (z0 + z1) / 2 + .1
    box(SHELL, xa + .01, xb - .01, cy - .01, cy + .01, z0 + fw, z1 - fw, 'trim', ('top', 'bottom'))
    for a, b in ((y0 + fw, cy - .01), (cy + .01, y1 - fw)): box(SHELL, xa + .01, xb - .01, a, b, cz - .01, cz + .01, 'trim', ('front', 'back'))
    xg = WX - .08
    for a, b in ((y0 + fw, cy - .01), (cy + .01, y1 - fw)):
        for c, d in ((z0 + fw, cz - .01), (cz + .01, z1 - fw)):
            nmg = 'GLASS_%02d' % (len(GLASS) + 1); GLASS.append(nmg); qf(nmg, [(xg, a, c), (xg, b, c), (xg, b, d), (xg, a, d)], 'glass', (1, 0, 0))
def surround_side(y0, y1, z0, z1):
    xo = WX + .02
    box(SHELL, WX, xo, y0 - .12, y0, z0, z1, 'trim', ('left', 'top', 'bottom')); box(SHELL, WX, xo, y1, y1 + .12, z0, z1, 'trim', ('left', 'top', 'bottom'))
    box(SHELL, WX, xo, y0 - .12, y1 + .12, z1, z1 + .12, 'trim', ('left',)); box(SHELL, WX, WX + .06, y0 - .18, y1 + .18, z0 - .08, z0, 'trim', ('left',))
XI0 = WX - .15
inset_side(*SWY[:2], *SWY[2:]); surround_side(*SWY)
hw_ = (SWY[1] - SWY[0]) / 2
for kk, (a, b) in enumerate(((SWY[0] - .14 - hw_, SWY[0] - .14), (SWY[1] + .14, SWY[1] + .14 + hw_))):
    box('SHUTTER_%02d' % (5 + kk), WX + .04, WX + .07, a, b, SWY[2], SWY[3], 'shutter', ('bottom',))
# ---------- 기단(비탈, 돌) = 앞 테라스 1.2m(의자 자리) · 현관 계단 한 단 ----------
TY = FY - 1.2
SITE = json.load(open(R + 'tools/art/house_home_site.json', encoding='utf-8'))['foundation']
assert (SITE['x_min'], SITE['x_max'], SITE['blender_y_min'], SITE['blender_y_max']) == (-WX - .08, WX + .08, TY, BY + .08)
box(SHELL, SITE['x_min'], SITE['x_max'], SITE['blender_y_min'], SITE['blender_y_max'], SITE['bottom'], SITE['top'], 'stone', ('bottom', 'top'))
# 0351 윗면을 나눔: 바깥 테두리 = 돌 · 집 안 1층 바닥 = 테라코타 타일, 현관 앞 1.1m = 돌(신발 벗는 자리)
for a, b, c, d in ((-WX - .08, WX + .08, TY, FY), (-WX - .08, WX + .08, BY, BY + .08), (-WX - .08, -WX, FY, BY), (WX, WX + .08, FY, BY)):
    face(SHELL, [(a, c, 0), (b, c, 0), (b, d, 0), (a, d, 0)], 'stone')
EX = .9; EYB = FY + 1.1
for a, b, c, d, p in ((-WX, -EX, FY, BY, 'pavers'), (EX, WX, FY, BY, 'pavers'), (-EX, EX, EYB, BY, 'pavers'), (-EX, EX, FY, EYB, 'entry_stone')):
    face(SHELL, [(a, c, 0), (b, c, 0), (b, d, 0), (a, d, 0)], p)
box(SHELL, DX0 - .25, DX1 + .25, TY - .32, TY, -.9, -.22, 'stone', ('back', 'bottom'))
# ---------- 지붕(박공, 기와 E2) ----------
def zu(y): return WT + PIT * (BY - abs(y))
YE, XE = BY + OVF, WX + OVS
for s in (-1, 1):  # s=-1 앞
    ye = s * YE; a, b = (-XE, XE) if s < 0 else (XE, -XE)
    face(SHELL, [(a, ye, zu(ye) + RT), (b, ye, zu(ye) + RT), (b, 0, zu(0) + RT), (a, 0, zu(0) + RT)], 'roof')        # 윗면
    face(SHELL, [(b, ye, zu(ye)), (a, ye, zu(ye)), (a, 0, zu(0)), (b, 0, zu(0))], 'soffit')                          # 밑면(처마 밑)
    face(SHELL, [(a, ye, zu(ye)), (b, ye, zu(ye)), (b, ye, zu(ye) + RT), (a, ye, zu(ye) + RT)], 'fascia')             # 처마 끝
for x in (-XE, XE):  # 옆 끝(박공 처마)
    for s in (-1, 1):
        ye = s * YE; q = [(x, ye, zu(ye)), (x, 0, zu(0)), (x, 0, zu(0) + RT), (x, ye, zu(ye) + RT)]
        if (x < 0) == (s < 0): q = [q[1], q[0], q[3], q[2]]
        face(SHELL, q, 'fascia')
box(SHELL, -XE - .03, XE + .03, -.11, .11, zu(0) + RT - .06, zu(0) + RT + .07, 'ridge', ('bottom',))
# ---------- 발코니(0351 도현 "조금 더 넓게": 폭 3.7m · 깊이 1.8m, 돌판 15cm · 까치발 2) — 1층 현관 차양을 겸함 ----------
BX = 3.9; BYF = FY - 1.8                 # 0133 도현 "양옆으로 길게": 폭 7.8m(집 폭 8.6 - 벽 양쪽 0.4), 깊이 1.8m
box(SHELL, -BX, BX, BYF, FY, H2 - .15, H2, {'*': 'trim', 'top': 'pavers'}, ('back',))
for cx in (-3.4, -1.15, 1.15, 3.4): box(SHELL, cx - .08, cx + .08, FY - 1.2, FY, H2 - .55, H2 - .15, 'trim', ('back', 'top'))
# ---------- 낱개 물건 ----------
# 현관문: 문짝(문틀 안 1cm 틈) + 손잡이. 바닥 5mm 띄움(기단 윗면과 같은 면 0)
box('DOOR_01', DX0 + .01, DX1 - .01, DY - .04, DY, .005, DZ - .01, 'door', ('bottom', 'back'))
box('DOOR_01', DX1 - .16, DX1 - .11, DY - .08, DY - .04, 1.0, 1.06, 'brass', ('back',))
# 발코니 문(D-200): 1421: 부모님 방에도 하나(DOOR_PARENTS). 피벗 = 경첩(문 구멍 동쪽 끝) 아래.
# 1608 fa9f [다시] 판정 "가운데 열린 문짝 = 방마다 문": 80° 열어 방 안에 세워 둔 문짝이 방 문으로 읽힘 → 벽 두께 안에 닫힌 유리문(아래 판 + 문틀 + 유리 1장). 발코니는 유리로 보인다
def balcony_door(nm, dox):
    x0, x1, ya, yb, z0, z1 = dox - .44, dox + .44, FY + .09, FY + .13, H2 + .005, H2 + 2.04   # 구멍(±.45 · 2.05) 안 1cm 틈 · 방 쪽 벽면(YI)에서 2cm 들어감
    zp, zt, sw = H2 + .3, H2 + 1.93, .1                                                          # 아래 굽 윗선(1608b: 통유리 — 방문으로 읽히지 않게 바깥이 보임) · 위 가로대 아랫선 · 세로 틀 폭
    box(nm, x0, x1, ya, yb, z0, zp, 'door', ('bottom',))                                          # 아래 굽(걷어차기 판)
    for a, b in ((x0, x0 + sw), (x1 - sw, x1)): box(nm, a, b, ya, yb, zp, zt, 'door', ('bottom', 'top'))   # 세로 틀 2(위·아래 = 판·가로대에 묻힘)
    box(nm, x0, x1, ya, yb, zt, z1, 'door')                                                       # 위 가로대
    nmg = 'GLASS_%02d' % (len(GLASS) + 1); GLASS.append(nmg); yg = (ya + yb) / 2
    face(nmg, [(x0 + sw, yg, zp), (x1 - sw, yg, zp), (x1 - sw, yg, zt), (x0 + sw, yg, zt)], 'glass')
    box(nm, x0 + .06, x0 + .11, yb, yb + .04, H2 + 1.0, H2 + 1.05, 'brass', ('front',))          # 손잡이(방 쪽, 경첩 반대편 = 서쪽)
    PIVOT[nm] = Vector((x1, (ya + yb) / 2, z0))
balcony_door('DOOR_BALCONY', DOX); balcony_door('DOOR_PARENTS', DOX2)
# 덧창(C2 초록): 거실 창·2층 창 2 = 활짝 열려 벽에 붙음(4cm 띄움). 부엌 창은 덧창 대신 커튼
k = 0
for x0, x1, z0, z1 in (WL, W2L):
    hw = (x1 - x0) / 2
    for a, b in ((x0 - .14 - hw, x0 - .14), (x1 + .14, x1 + .14 + hw)):
        k += 1; box('SHUTTER_%02d' % k, a, b, FY - .07, FY - .04, z0, z1, 'shutter', ('bottom',))
# 부엌 창: 커튼 2(창 머리에 매달린 천 = 앞면 한 장, 유리 2cm 앞) + 창턱 안 작은 화분(바닥 = 창 아래 면 → skip)
x0, x1, z0, z1 = WK  # 0351: 창틀이 벽 안으로 들어가서 커튼·화분은 집 안(안쪽 창턱 13cm 위)으로
face('CURTAIN_01', [(x0 - .22, YI + .03, z0 + .02), (x0 + .2, YI + .03, z0 + .02), (x0 + .2, YI + .03, z1 + .1), (x0 - .22, YI + .03, z1 + .1)][::-1], 'curtain')  # 방 쪽(+Y)을 보는 한 장
face('CURTAIN_02', [(x1 - .15, YI + .03, z0 + .02), (x1 + .12, YI + .03, z0 + .02), (x1 + .12, YI + .03, z1 + .1), (x1 - .15, YI + .03, z1 + .1)][::-1], 'curtain')
box('CURTAIN_01', x0 - .26, x1 + .16, YI + .01, YI + .03, z1 + .12, z1 + .14, 'brass', ('front',))  # 커튼 봉(벽에서 1cm)
box(SHELL, x0 - .05, x1 + .05, YI, YI + .13, z0 - .04, z0, 'trim', ('front',))  # 안쪽 창턱(윗면 = 흰 면 아래와 같은 높이, 맞닿기만)
cx = (x0 + x1) / 2; py = YI + .02
box('PLANTER_03', cx - .08, cx + .08, py, py + .08, z0, z0 + .12, {'*': 'terracotta', 'top': 'soil'}, ('bottom',))
for dx, h, w in ((-.04, .14, .07), (.04, .18, .08)): box('PLANT_03', cx + dx - w / 2, cx + dx + w / 2, py + .02, py + .06, z0 + .12, z0 + .12 + h, 'leaf', ('bottom',))  # 잎 둘 5mm 떨어짐
box('PLANT_03', cx - .015, cx + .015, py + .06, py + .08, z0 + .22, z0 + .26, 'flower', ('front',))
# 의자(엄마 의자, 현관 오른쪽 테라스): 다리 4(뒷다리 = 등받이 기둥) + 앉판 + 등받이 살. 다리 윗면은 앉판 밑면과 맞닿음 → skip
CX0, CX1, CY0, CY1 = 1.0, 1.44, FY - .58, FY - .16
for lx0, ly0, lh in ((CX0 + .01, CY0 + .01, .42), (CX1 - .05, CY0 + .01, .42), (CX0 + .01, CY1 - .05, .9), (CX1 - .05, CY1 - .05, .9)):
    box('CHAIR_01', lx0, lx0 + .04, ly0, ly0 + .04, 0, lh, 'chair', ('bottom', 'top') if lh < .5 else ('bottom',))
box('CHAIR_01', CX0, CX1, CY0, CY1, .42, .46, 'chair')
box('CHAIR_01', CX0, CX1, CY1 - .04, CY1 - .02, .68, .84, 'chair')
# 난간(쇠, 높이 1m): 기둥 → 위·아래 가로대(기둥 사이 토막) → 살 2cm (11.5cm 간격). 맞닿는 면은 skip. 0133: 폭 7.8m → 기둥 5(양끝·1/4·가운데)
RL, z0, zt = 'RAIL_01', H2, H2 + .95
FY0, FY1 = BYF + .02, BYF + .08                 # 앞 가로대 y
SX = [(-BX, -BX + .06), (BX - .06, BX)]          # 옆 가로대 x
PXC = [-BX + .03, -BX / 2, 0, BX / 2, BX - .03]  # 앞 기둥 가운데 x(폭 6cm)
posts = [(c - .03, c + .03, FY0, FY1) for c in PXC] + [(-BX, -BX + .06, FY - .08, FY - .02), (BX - .06, BX, FY - .08, FY - .02)]
for x0, x1, y0, y1 in posts: box(RL, x0, x1, y0, y1, z0, zt, 'iron', ('bottom', 'top'))
box(RL, -BX, BX, FY0, FY1, zt, zt + .05, 'iron')                                  # 앞 손잡이
for x0, x1 in SX: box(RL, x0, x1, FY1, FY - .02, zt, zt + .05, 'iron', ('front',))  # 옆 손잡이(앞 끝 = 앞 손잡이 뒷면 → skip)
for c0, c1 in zip(PXC, PXC[1:]): box(RL, c0 + .03, c1 - .03, FY0 + .01, FY1 - .01, z0 + .08, z0 + .12, 'iron', ('left', 'right'))
for x0, x1 in SX: box(RL, x0 + .01, x1 - .01, FY1, FY - .08, z0 + .08, z0 + .12, 'iron', ('front', 'back'))
n = 0
for i in range(-34, 35):
    x = i * .115
    if abs(x) > BX - .12 or any(abs(x - c) < .09 for c in PXC): continue
    box(RL, x - .01, x + .01, FY0 + .02, FY1 - .02, z0 + .12, zt, 'iron', ('top', 'bottom')); n += 1
for x0, x1 in SX:
    for kk in range(13):  # 0351: 깊이 1.8m → 12cm 간격 13개
        y = BYF + .2 + kk * .12
        box(RL, x0 + .02, x1 - .02, y - .01, y + .01, z0 + .12, zt, 'iron', ('top', 'bottom')); n += 1
def cyl(obj, cx, cy, r, z0, z1, pt, top=None, bottom=True, sides=16, btm=None):
    """세운 원통(16면, check cyl): 옆면 사각 16장 + 윗면 16각. top = 윗면 칠(없으면 pt), bottom=False 면 바닥 면 skip(받침 위)."""
    ang = [2 * math.pi * k / sides for k in range(sides)]; P = [(cx + r * math.cos(a), cy + r * math.sin(a)) for a in ang]
    for k in range(sides):
        (xa, ya), (xb, yb) = P[k], P[(k + 1) % sides]; face(obj, [(xa, ya, z0), (xb, yb, z0), (xb, yb, z1), (xa, ya, z1)], pt)
    face(obj, [(x, y, z1) for x, y in P], top or pt)
    if bottom: face(obj, [(x, y, z0) for x, y in P[::-1]], btm or pt)
def table_legs(obj, x0, x1, y0, y1, h, w, pt, skip=('top', 'bottom'), z0=0):  # 다리 4(w 굵기). 윗면은 위 판 밑면과 맞닿음 → skip
    for lx, ly in ((x0 + .01, y0 + .01), (x1 - w - .01, y0 + .01), (x0 + .01, y1 - w - .01), (x1 - w - .01, y1 - w - .01)): box(obj, lx, lx + w, ly, ly + w, z0, z0 + h, pt, skip)
# 화분 2(테라코타 + 흙 + 잎·꽃) — 0133: 발코니 양 끝 안쪽(문 앞 길·의자 자리 비움). 잎 바닥 = 흙 윗면 → skip
for nm_, pl_, cx in (('PLANTER_01', 'PLANT_01', -3.3), ('PLANTER_02', 'PLANT_02', .5)):  # 화분 / 식물(잎·꽃) 낱개 따로
    box(nm_, cx - .25, cx + .25, BYF + .13, BYF + .35, H2, H2 + .24, {'*': 'terracotta', 'top': 'soil'}, ('bottom',))
    for dx, dy, h, w in ((-.15, -.05, .22, .14), (.0, .03, .30, .16), (.14, -.04, .20, .13)):
        lf = BYF + .24 + dy - .05  # 잎 앞면 → 꽃은 그 앞 2cm(뒷면 = 잎 앞면 → skip)
        box(pl_, cx + dx - w / 2, cx + dx + w / 2, lf, lf + .10, H2 + .24, H2 + .24 + h, 'leaf', ('bottom',))
        box(pl_, cx + dx - .03, cx + dx + .03, lf - .02, lf, H2 + .24 + h - .10, H2 + .24 + h - .04, 'flower', ('back',))
# 0133 발코니 쉼터: 철제 탁자 + 플라스틱 의자 2 + 탁자 위 커피잔·작은 화분 (문(x 1.95..2.85) 오른쪽 난간 안쪽, 난간 살 y 안쪽 ≥ 25cm 비움)
TBX, TBY = -1.9, BYF + .95                       # 탁자 가운데(부모님 방 창 둘 사이 앞 — 주인공 방 문 앞길은 비움)
ZB = H2                                          # 발코니 바닥 높이
box('TABLE_BALC', TBX - .32, TBX + .32, TBY - .32, TBY + .32, ZB + .70, ZB + .73, 'iron')                 # 철 판(윗면 73cm)
table_legs('TABLE_BALC', TBX - .32, TBX + .32, TBY - .32, TBY + .32, .70, .035, 'iron', z0=ZB)
def pchair(nm_, cx, cy, bx):  # 플라스틱 의자(앉판 + 다리 4 + 등받이 판 3). bx = 등받이 쪽 x 방향(+1/-1)
    x0, x1, y0, y1 = cx - .21, cx + .21, cy - .21, cy + .21
    box(nm_, x0, x1, y0, y1, ZB + .44, ZB + .47, 'plastic')
    for lx, ly in ((x0 + .015, y0 + .015), (x1 - .045, y0 + .015), (x0 + .015, y1 - .045), (x1 - .045, y1 - .045)): box(nm_, lx, lx + .03, ly, ly + .03, ZB, ZB + .44, 'plastic', ('top', 'bottom'))
    bx0, bx1 = (x1 - .04, x1) if bx > 0 else (x0, x0 + .04)
    for a, b in ((y0 + .01, y0 + .15), (y0 + .17, y0 + .25), (y0 + .27, y1 - .01)): box(nm_, bx0, bx1, a, b, ZB + .47, ZB + .86, 'plastic', ('bottom',))   # 판 3(틈 2cm)
pchair('CHAIR_04', TBX - .72, TBY, -1); pchair('CHAIR_05', TBX + .72, TBY + .05, +1)
# 커피(흰 잔 16면 + 커피 윗면 + 손잡이) · 작은 화분(16면 테라코타 + 흙 + 잎) — 탁자 윗면 73cm 위
cyl('CUP_01', TBX - .12, TBY - .05, .04, ZB + .73, ZB + .81, 'ceramic', 'coffee', bottom=False)
box('CUP_01', TBX - .08, TBX - .06, TBY - .06, TBY - .04, ZB + .75, ZB + .79, 'ceramic', ('left',))               # 손잡이: 잔 옆면과 2~4cm 걸침이 없게(잔 r 4cm → 옆 x -.04 ~ -.08 사이에 닿음)
cyl('PLANTER_04', TBX + .14, TBY + .06, .06, ZB + .73, ZB + .80, 'terracotta', 'soil', bottom=False)
for dx, dy, h, w in ((-.02, -.01, .09, .05), (.03, .02, .12, .05), (.0, .03, .07, .04)):
    box('PLANT_04', TBX + .14 + dx - w / 2, TBX + .14 + dx + w / 2, TBY + .06 + dy - w / 2, TBY + .06 + dy + w / 2, ZB + .80, ZB + .80 + h, 'leaf', ('bottom',))

# ========== 0351 집 안(도현 "실제 집처럼"): 1층 현관·거실 겸 부엌·계단 / 2층 바닥 슬래브·주인공 방(벽 = 집 칸막이)·복도·엄마 방·욕실 ==========
XI, YB = WX - .15, BY - .15              # 바깥벽 안쪽 면(두께 15cm)
C1, H2C = 2.8, WT                        # 1층 천장 2.8 · 2층 천장 = 벽 윗선 5.85(층고 2.65)
RX, RW = 1.75, 4.1                       # 방 벽 x ±1.75(q1_room 3.5m) · 방 판벽 윗선 z(방 바닥 + 0.9)
PB0, PB1 = .15, .25                      # 2층 복도 벽 y(방 뒷벽 = q1_room y +1.5)
SX0, ST, SR, SN_, SY0 = -.2, .25, .2, 15, 1.85   # 계단: 첫 단 x · 디딤 25cm · 단 20cm · 15단(+ 슬래브 1단 = 3.2m) · 계단 앞선 y(뒤 = 뒷벽)
SX1 = SX0 + SN_ * ST                     # 3.55 = 계단 끝 = 2층 도착
def hwall(obj, p0, p1, z0, z1, holes=(), pf=lambda c, z: 'plaster', ss=(), zz=()):
    """수직 벽 한 장(p0→p1 이 바깥에서 본 왼→오른, 법선 = (dy, -dx)). holes·ss = 벽 방향 세계 좌표(x 또는 y). 띠마다 토막, pf(좌표, z) = 칠."""
    ax = 0 if abs(p1[0] - p0[0]) > 1e-6 else 1; c0, c1 = p0[ax], p1[ax]
    P = lambda c: (c, p0[1]) if ax == 0 else (p0[0], c)
    zs = sorted({z0, z1} | {z for h in holes for z in h[2:] if z0 < z < z1} | {z for z in zz if z0 < z < z1})
    for za, zb in zip(zs, zs[1:]):
        cs = sorted({c0, c1} | {c for c in ss if min(c0, c1) < c < max(c0, c1)} | {c for h in holes if h[2] <= za and zb <= h[3] for c in h[:2]}, reverse=c1 < c0)
        for ca, cb in zip(cs, cs[1:]):
            m = (ca + cb) / 2
            if any(min(h[:2]) < m < max(h[:2]) and h[2] <= za and zb <= h[3] for h in holes): continue
            a, b = P(ca), P(cb); face(obj, [(a[0], a[1], za), (b[0], b[1], za), (b[0], b[1], zb), (a[0], a[1], zb)], pf(m, (za + zb) / 2))
def hface(obj, x0, x1, y0, y1, z, pt, up=True):
    q = [(x0, y0, z), (x1, y0, z), (x1, y1, z), (x0, y1, z)]; face(obj, q if up else q[::-1], pt)
def hexa(obj, c, pt, skip=()):  # 여섯 면 육면체(기운 손잡이용): c = 아래 4(x0y0,x1y0,x1y1,x0y1) + 위 4. 면 키 front(y0)·right·back·left·bottom·top, 법선은 가운데에서 바깥
    c = [Vector(p) for p in c]; ctr = sum(c, Vector()) / 8
    for k, ix in (('front', (0, 1, 5, 4)), ('right', (1, 2, 6, 5)), ('back', (2, 3, 7, 6)), ('left', (3, 0, 4, 7)), ('bottom', (3, 2, 1, 0)), ('top', (4, 5, 6, 7))):
        if k in skip: continue
        f = [c[i] for i in ix]; n = (f[1] - f[0]).cross(f[2] - f[0])
        if n.dot(sum(f, Vector()) / 4 - ctr) < 0: f = f[::-1]
        face(obj, f, pt)
# --- 바깥벽 안쪽 면(1층 0~2.8 · 2층 3.2~5.85), 앞벽은 구멍 그대로. 2층: 주인공 방 쪽(x ≥ PW) = 방 칠(판벽·크림), 부모님 쪽 = 회벽 ---
hwall(SHELL, (XI, YI), (-XI, YI), 0, C1, [(DX0, DX1, 0, DZ), WL, WK])
hwall(SHELL, (-XI, YB), (XI, YB), 0, C1); hwall(SHELL, (-XI, YI), (-XI, YB), 0, C1); hwall(SHELL, (XI, YB), (XI, YI), 0, C1)
rw = lambda c, z: ('wainlow' if z < RW else 'rwall')
hwall(SHELL, (XI, YI), (-XI, YI), H2, H2C, [BD, BD2, W2L], lambda c, z: rw(c, z) if c > PW else 'plaster', (PW, DIVX), (RW,))
hwall(SHELL, (-XI, YB), (XI, YB), H2, H2C, (), lambda c, z: rw(c, z) if c > PW else 'plaster', (PW, DIVX), (RW,))
hwall(SHELL, (-XI, YI), (-XI, YB), H2, H2C)                                                          # 서쪽 벽(부모님 방)
hwall(SHELL, (XI, YB), (XI, YI), H2, H2C, [SWY], lambda c, z: rw(c, z), (), (RW,))                   # 동쪽 벽: 주인공 방(계단 도착 구석까지) = 판벽 · 창 구멍 SWY
# --- 슬래브(1층 천장 2.8 / 2층 바닥 3.2): 계단 구멍(x SX0..SX1, y SY0..YB)만 비움 ---
for x0, x1, y0, y1 in ((-XI, XI, YI, SY0), (-XI, SX0, SY0, YB), (SX1, XI, SY0, YB)):
    hface(SHELL, x0, x1, y0, y1, H2, 'boards'); hface(SHELL, x0, x1, y0, y1, C1, 'ceil', up=False)
face(SHELL, [(SX1, SY0, C1), (SX0, SY0, C1), (SX0, SY0, H2), (SX1, SY0, H2)], 'ceil')   # 구멍 가장자리 3면
face(SHELL, [(SX0, SY0, C1), (SX0, YB, C1), (SX0, YB, H2), (SX0, SY0, H2)], 'ceil')
face(SHELL, [(SX1, YB, C1), (SX1, SY0, C1), (SX1, SY0, H2), (SX1, YB, H2)], 'ceil')
hface(SHELL, -XI, XI, YI, YB, H2C, 'ceil', up=False)                                   # 2층 천장
# --- 2층 칸막이 10cm(1421 도현 "2층 전체를 방으로 · 부모님 방도 방 하나 · 발코니 문 방마다 1개"): 복도 없음. 2층 = 방 2개가 바닥을 꽉 채움.
#   주인공 방 = q1_room 3.5×3.0 앞쪽 + 계단 도착 구석(y .15..1.85, 난간까지) — 계단은 이 방으로 올라온다(계단참 = 방 안 도착 구석).
#   부모님 방 = 칸막이(x .55..0.65) 서쪽 전부. 둘 사이 출입구 1(칸막이, 계단참 쪽 y DY0..DY1, 문짝 없음). 칸막이 북쪽 끝 = 계단 구멍 난간 앞 1cm.
SYE = SY0 - .06                                                                                    # 칸막이 북쪽 끝 y(난간 가로대 y SY0-.05 앞)
#   1608 fa9f [다시] "방 2개 — 하나는 올라오자마자 바로, 하나는 옆에": 출입구를 문틀 없는 넓은 트임(1.2m, 높이 2.15)으로. 옆·머리 면 = 벽과 같은 회벽(흰 문틀 칠 없음)
DY0, DY1, DZ2 = .35, 1.55, H2 + 2.15
hwall(SHELL, (PW, YI), (PW, SYE), H2, H2C, [(DY0, DY1, H2, DZ2)], lambda c, z: rw(0, z), (), (RW,))   # 주인공 방 쪽 면(+x 를 봄)
hwall(SHELL, (DIVX, SYE), (DIVX, YI), H2, H2C, [(DY0, DY1, H2, DZ2)])                              # 부모님 방 쪽 면(−x 를 봄)
hwall(SHELL, (PW, SYE), (DIVX, SYE), H2, H2C)                                                      # 북쪽 끝면(+y 를 봄)
face(SHELL, [(DIVX, DY0, H2), (PW, DY0, H2), (PW, DY0, DZ2), (DIVX, DY0, DZ2)][::-1], 'plaster'); face(SHELL, [(DIVX, DY1, H2), (PW, DY1, H2), (PW, DY1, DZ2), (DIVX, DY1, DZ2)], 'plaster')  # 트임 옆 면 2
hface(SHELL, DIVX, PW, DY0, DY1, DZ2, 'plaster', up=False)                                          # 트임 머리 밑면
# --- 계단(STAIRS, 1층 → 2층, 38.7°): 디딤·챌판·옆판(단마다 한 장 = 톱니 다각형 없음) ---
for i in range(SN_):
    xa, xb, zt = SX0 + i * ST, SX0 + (i + 1) * ST, (i + 1) * SR
    face('STAIRS', [(xa, YB, i * SR), (xa, SY0, i * SR), (xa, SY0, zt), (xa, YB, zt)], 'boards')               # 챌판(-X 를 봄 = 오르는 사람 쪽)
    hface('STAIRS', xa, xb, SY0, YB, zt, 'boards')                                                          # 디딤
    face('STAIRS', [(xa, SY0, 0), (xb, SY0, 0), (xb, SY0, zt), (xa, SY0, zt)], 'plaster')                    # 옆판(계단 밑 = 막힘)
face('STAIRS', [(SX1, SY0, 0), (SX1, YB, 0), (SX1, YB, C1), (SX1, SY0, C1)], 'plaster')                    # 끝(위는 슬래브 가장자리)
PIVOT['STAIRS'] = Vector(((SX0 + SX1) / 2, (SY0 + YB) / 2, 0))
zr = lambda x: SR + SR * (x - SX0) / ST + .9                                                                # 손잡이 = 단 코 + 90cm
ry0, ry1 = SY0 + .01, SY0 + .05
for x in (SX0 + .04, SX1 - .2):                                                                             # 기둥 2(첫 단·끝 단 위)
    i = int((x - SX0) // ST); box('RAIL_STAIRS', x, x + .06, ry0, ry1, (i + 1) * SR, zr(x + .03) + .08, 'iron', ('bottom',))
xa, xb = SX0 + .1, SX1 - .2
hexa('RAIL_STAIRS', [(xa, ry0 + .005, zr(xa)), (xb, ry0 + .005, zr(xb)), (xb, ry1 - .005, zr(xb)), (xa, ry1 - .005, zr(xa)),
                     (xa, ry0 + .005, zr(xa) + .05), (xb, ry0 + .005, zr(xb) + .05), (xb, ry1 - .005, zr(xb) + .05), (xa, ry1 - .005, zr(xa) + .05)], 'iron', ('left', 'right'))
for i in range(1, SN_ - 1):                                                                                 # 살: 단마다 하나(디딤 가운데)
    x = SX0 + (i + .5) * ST; box('RAIL_STAIRS', x - .01, x + .01, ry0 + .01, ry1 - .01, (i + 1) * SR, zr(x) + .02, 'iron', ('bottom', 'top'))  # 위끝 = 손잡이 속 2cm
def rail_run(obj, x0, x1, y0, y1, z0, h=.95, skip_end=()):  # 2층 계단 구멍 난간(가로 = x 또는 y 긴 쪽): 위 손잡이 + 살 12cm
    alongx = (x1 - x0) > (y1 - y0); L = (x1 - x0) if alongx else (y1 - y0)
    box(obj, x0, x1, y0, y1, z0 + h, z0 + h + .05, 'iron', skip_end)
    n = int(L / .12)
    for k in range(n + 1):
        t = min(max(.03, k * .12), L - .03)
        if alongx: box(obj, x0 + t - .01, x0 + t + .01, y0 + .01, y1 - .01, z0, z0 + h, 'iron', ('bottom', 'top'))
        else: box(obj, x0 + .01, x1 - .01, y0 + t - .01, y0 + t + .01, z0, z0 + h, 'iron', ('bottom', 'top'))
rail_run('RAIL_02', SX0 - .05, SX1, SY0 - .05, SY0, H2)                        # 복도 쪽(도착 x SX1..XI 는 비움)
rail_run('RAIL_02', SX0 - .05, SX0, SY0, YB, H2, skip_end=('front', 'back'))   # 끝(앞 = 위 손잡이에 닿음 · 뒤 = 뒷벽)
# --- 1층 가구(낱개, 피벗 바닥 가운데) ---
def chair(nm_, x0, x1, y0, y1, back_low):
    yb0 = y0 if back_low else y1 - .04
    for lx, ly in ((x0 + .01, y0 + .01), (x1 - .05, y0 + .01), (x0 + .01, y1 - .05), (x1 - .05, y1 - .05)):
        tall = (ly < (y0 + y1) / 2) == back_low; box(nm_, lx, lx + .04, ly, ly + .04, 0, .9 if tall else .42, 'chair', ('bottom',) if tall else ('bottom', 'top'))
    box(nm_, x0, x1, y0, y1, .42, .46, 'chair'); box(nm_, x0, x1, yb0 + .015, yb0 + .035, .68, .84, 'chair')
# 거실(왼쪽): 소파(왼벽 5cm 앞) · 낮은 탁자 · 엄마 바느질 바구니
box('SOFA', -4.1, -3.3, -1.6, -1.45, 0, .62, 'sofa', ('bottom',)); box('SOFA', -4.1, -3.3, .25, .4, 0, .62, 'sofa', ('bottom',))          # 팔걸이 2
box('SOFA', -4.1, -3.3, -1.45, .25, 0, .42, 'sofa', ('bottom', 'front', 'back'))                                                       # 바닥 몸
box('SOFA', -4.1, -3.85, -1.45, .25, .42, .85, 'sofa', ('bottom', 'front', 'back'))                                                   # 등받이
box('SOFA', -3.84, -3.32, -1.44, .24, .42, .52, 'cushion', ('bottom',))                                                               # 앉는 방석
box('TABLE_LIVING', -2.9, -2.3, -1.0, -.2, .36, .4, 'chair')
for lx, ly in ((-2.88, -.98), (-2.36, -.98), (-2.88, -.26), (-2.36, -.26)): box('TABLE_LIVING', lx, lx + .04, ly, ly + .04, 0, .36, 'chair', ('bottom', 'top'))
box('BASKET_01', -3.95, -3.6, .55, .85, 0, .22, 'basket', ('bottom',)); box('BASKET_01', -3.82, -3.74, .66, .74, .22, .29, 'flower', ('bottom',))  # 엄마 바느질 바구니 + 털실
# 현관: 신발장 · 발매트(1cm 위 한 장)
hface('MAT_01', -.45, .45, YI + .1, YI + .6, .012, 'mat')
# 부엌(오른쪽 벽): 조리대(하부장 그림) · 싱크 · 가스레인지 · 찬장 · 식탁 2인 · 엄마 앞치마
box('COUNTER', 3.55, XI - .03, YI + .03, 1.2, 0, .86, {'*': 'cabinet', 'top': 'counter'}, ('bottom', 'right', 'front'))
box('SINK', 3.62, 4.07, -.9, -.35, .86, .88, {'*': 'steel', 'top': 'sinktop'}, ('bottom',))
box('SINK', 4.0, 4.04, -.65, -.6, .88, 1.12, 'steel', ('bottom',)); box('SINK', 3.86, 4.0, -.65, -.6, 1.08, 1.12, 'steel', ('right',))
box('STOVE', 3.62, 4.07, -2.2, -1.6, .86, .9, {'*': 'steel', 'top': 'stovetop'}, ('bottom',))
box('CUPBOARD', 3.8, XI - .03, -2.3, .9, 1.45, 2.15, 'cabinet', ('right',))
box('TABLE_KITCHEN', 1.55, 2.45, -.9, -.1, .72, .76, 'chair')
for lx, ly in ((1.57, -.88), (2.39, -.88), (1.57, -.16), (2.39, -.16)): box('TABLE_KITCHEN', lx, lx + .04, ly, ly + .04, 0, .72, 'chair', ('bottom', 'top'))
chair('CHAIR_02', 1.78, 2.22, -1.42, -1.0, True); chair('CHAIR_03', 1.78, 2.22, .02, .44, False)
box('VASE_01', 1.95, 2.05, -.55, -.45, .76, .94, 'terracotta', ('bottom',)); box('VASE_01', 1.96, 2.04, -.54, -.46, .94, 1.02, 'flower', ('bottom',))  # 엄마 꽃병
face('APRON_01', [(3.27, YI + .02, 1.0), (3.52, YI + .02, 1.0), (3.52, YI + .02, 1.6), (3.27, YI + .02, 1.6)][::-1], 'apron')
box('APRON_01', 3.37, 3.41, YI, YI + .03, 1.6, 1.64, 'brass', ('front',))
# ---------- 0133 도현 "실제 사는 집처럼 생활감" — 전등·시계·신발장·후추통·프라이팬·냉장고 + 부모님 방 ----------
def pendant(nm_, cx, cy, ztop, drop):  # 천장 펜던트: 줄(16면) + 갓(16면, 윗면 천) + 아래 전구 면. Unity 에서 PENDANT_NN 마다 따뜻한 점광원 자식을 붙임
    cyl(nm_, cx, cy, .012, ztop - drop, ztop - .003, 'iron', bottom=False)
    cyl(nm_, cx, cy, .17, ztop - drop - .16, ztop - drop, 'shade', 'shade', btm='bulb')
# 현관: 신발장(문 달린 장) + 위에 한 짝 + 바닥 신발 2켤레(벗어 둔 자리)
box('SHOERACK', -1.6, -.85, YI + .02, YI + .34, 0, .95, {'*': 'shoecab', 'top': 'chair'}, ('bottom',))
for nm_, sx, sy in (('SHOE_03', -1.42, YI + .42), ('SHOE_04', -1.08, YI + .5)):
    for dx in (0, .13):   # 한 켤레 = 왼·오른 2짝
        box(nm_, sx + dx, sx + dx + .1, sy, sy + .27, 0, .03, 'sole', ('bottom',)); box(nm_, sx + dx + .01, sx + dx + .09, sy + .07, sy + .26, .03, .1, 'leather', ('bottom',))
box('SHOE_05', -1.5, -1.4, YI + .05, YI + .3, .95, .98, 'sole', ('bottom',)); box('SHOE_05', -1.49, -1.41, YI + .12, YI + .29, .98, 1.04, 'leather', ('bottom',))   # 신발장 위 한 짝
# 전등 2(거실 탁자 위 · 부엌 식탁 위)
pendant('PENDANT_01', -2.6, -.6, C1, .6); pendant('PENDANT_02', 2.0, -.5, C1, .6)
# 거실: 깔개(탁자 밑) · 둥근 벽시계(소파 위, 서쪽 벽) — 틀 16면 + 문자판 + 바늘 2(문자판에서 1cm 띄움)
hface('RUG_02', -3.25, -1.75, -1.4, .2, .011, 'rug')
CKY, CKZ, CKR, CX0, CX1 = -.45, 1.9, .17, -XI, -XI + .04
CP = [(CKY + CKR * math.cos(2 * math.pi * k / 16), CKZ + CKR * math.sin(2 * math.pi * k / 16)) for k in range(16)]
for k in range(16):
    (ya, za), (yb, zb) = CP[k], CP[(k + 1) % 16]; face('WALLCLOCK_02', [(CX0, ya, za), (CX0, yb, zb), (CX1, yb, zb), (CX1, ya, za)], 'chair')
face('WALLCLOCK_02', [(CX1, y, z) for y, z in CP], 'clockface')
box('WALLCLOCK_02', CX1 + .01, CX1 + .02, CKY - .008, CKY + .008, CKZ, CKZ + .13, 'hand'); box('WALLCLOCK_02', CX1 + .025, CX1 + .035, CKY - .08, CKY + .008, CKZ - .008, CKZ + .008, 'hand')
# 부엌: 후추통·소금통·접시 2·머그(식탁 위) · 후라이팬(가스레인지 위, 계란 하나) · 냉장고 · 쓰레기통
cyl('PEPPER_01', 1.78, -.3, .022, .76, .85, 'pepper', 'steel', bottom=False); cyl('SALT_01', 1.89, -.27, .03, .76, .86, 'chair', 'steel', bottom=False)
cyl('PLATE_01', 1.8, -.72, .11, .76, .775, 'ceramic', bottom=False); cyl('PLATE_02', 2.2, -.3, .11, .76, .775, 'ceramic', bottom=False)
cyl('CUP_02', 2.32, -.7, .04, .76, .84, 'ceramic', 'coffee', bottom=False); box('CUP_02', 2.36, 2.38, -.72, -.68, .78, .82, 'ceramic', ('left',))
cyl('PAN_01', 3.85, -1.9, .1, .9, .95, 'pan', 'pantop', bottom=False); box('PAN_01', 3.5, 3.745, -1.915, -1.885, .915, .945, 'pan', ('right',))
box('FRIDGE_01', 3.5, 4.1, 1.25, 1.8, 0, 1.7, {'*': 'fridge', 'left': 'fridgef'}, ('bottom',))
box('FRIDGE_01', 3.46, 3.5, 1.3, 1.33, .8, 1.15, 'steel', ('right',)); box('FRIDGE_01', 3.46, 3.5, 1.3, 1.33, 1.25, 1.6, 'steel', ('right',))
cyl('WASTE_01', 3.3, -2.55, .13, 0, .36, 'bin', bottom=False)
# ---- 2층 부모님 방(칸막이 서쪽): 침대(머리 서쪽 벽) · 협탁 2(스탠드) · 옷장 · 서랍장(칸막이 쪽, 거울) · 깔개 · 천장등. 바닥 = H2 ----
Z = H2
box('BED_02', -4.1, -2.1, -1.0, .4, Z, Z + .3, 'chair', ('bottom',)); box('BED_02', -4.1, -4.04, -1.02, .42, Z + .3, Z + 1.0, 'chair', ('bottom',))   # 틀 · 머리판
box('BED_02', -4.04, -2.12, -.97, .37, Z + .3, Z + .45, 'bedding', ('bottom', 'left')); box('BED_02', -3.35, -2.1, -.99, .39, Z + .45, Z + .53, 'quilt', ('bottom',))   # 요 · 이불
box('PILLOW_02', -4.0, -3.5, -.9, -.5, Z + .45, Z + .55, 'bedding', ('bottom',)); box('PILLOW_03', -4.0, -3.5, -.1, .3, Z + .45, Z + .55, 'bedding', ('bottom',))
for nm_, y0 in (('NIGHTSTAND_01', -1.5), ('NIGHTSTAND_02', .45)):
    box(nm_, -4.1, -3.65, y0, y0 + .45, Z, Z + .5, {'*': 'chair', 'right': 'drawer'}, ('bottom',))
for nm_, ly in (('BEDLIGHT_01', -1.275), ('BEDLIGHT_02', .675)):
    cyl(nm_, -3.88, ly, .035, Z + .5, Z + .62, 'brass', bottom=False); cyl(nm_, -3.88, ly, .1, Z + .62, Z + .8, 'shade', 'shade', bottom=False)
box('WARDROBE_01', -4.1, -3.5, 1.0, 2.4, Z, Z + 2.0, {'*': 'wardrobe', 'right': 'wardrobef'}, ('bottom',))
box('DRESSER_01', .15, .55, -2.1, -.9, Z, Z + .85, {'*': 'chair', 'left': 'dresserf'}, ('bottom', 'right'))
box('MIRROR_01', .52, .55, -1.85, -1.15, Z + 1.05, Z + 1.75, {'*': 'chair', 'left': 'mirror'}, ('right',))   # 벽 거울(벽에 닿는 면 skip)
hface('RUG_03', -2.0, -.5, -1.6, 0, Z + .011, 'rug')
pendant('PENDANT_03', -1.9, -.6, H2C, .12)   # 1608b: 줄 짧게(반매입) — 길면 트임 윗선에 줄이 가려 갓만 떠 보임
# 2층 방(ROOM_2F)의 전등은 Unity 에서 0.25m 올림(천장 2.4 → 2.65, library_import.cs)

# ========== 1421 867e "생활감이 아직 부족함 — 소품 개수·세세한 디테일(손잡이·뚜껑·두께·라벨 자리)" — 엄마와 사는 집 ==========
import random
RNG = random.Random(1421)
CT = .86                                             # 조리대 윗면 z
def kettle(nm, cx, cy):  # 주전자: 몸통 16면 + 뚜껑 턱 + 꼭지 + 주둥이(서쪽) + 손잡이(ㄷ자, 동쪽)
    cyl(nm, cx, cy, .085, CT, CT + .15, 'steel', 'steel', bottom=False); cyl(nm, cx, cy, .06, CT + .15, CT + .17, 'steel', 'steel', bottom=False)
    cyl(nm, cx, cy, .013, CT + .17, CT + .2, 'sole', 'sole', bottom=False)
    box(nm, cx - .14, cx - .07, cy - .015, cy + .015, CT + .09, CT + .125, 'steel', ('right',))
    box(nm, cx + .08, cx + .125, cy - .012, cy + .012, CT + .13, CT + .15, 'sole', ('left',)); box(nm, cx + .105, cx + .125, cy - .012, cy + .012, CT + .04, CT + .13, 'sole', ('top', 'bottom'))
    box(nm, cx + .08, cx + .125, cy - .012, cy + .012, CT + .02, CT + .04, 'sole', ('left',))
def pot(nm, cx, cy, r=.12, h=.13):  # 냄비: 몸통 + 뚜껑(조금 큼) + 꼭지 + 양 귀 손잡이
    cyl(nm, cx, cy, r, CT, CT + h, 'steel', 'steel', bottom=False); cyl(nm, cx, cy, r + .006, CT + h, CT + h + .012, 'steel', 'steel', bottom=False)
    cyl(nm, cx, cy, .013, CT + h + .012, CT + h + .04, 'sole', 'sole', bottom=False)
    box(nm, cx + r - .01, cx + r + .05, cy - .02, cy + .02, CT + h - .05, CT + h - .03, 'sole', ('left',)); box(nm, cx - r - .05, cx - r + .01, cy - .02, cy + .02, CT + h - .05, CT + h - .03, 'sole', ('right',))
def bottle(nm, cx, cy, pt):  # 양념 병: 몸통 + 라벨 띠 + 뚜껑
    cyl(nm, cx, cy, .025, CT, CT + .1, pt, 'ceramic', bottom=False)       # 라벨 띠는 칠(spice 칸 가운데 종이색) — 따로 덧대면 겹침
    cyl(nm, cx, cy, .022, CT + .1, CT + .122, 'ceramic', 'ceramic', bottom=False)
kettle('KETTLE_01', 3.82, -1.3); pot('POT_02', 3.85, -2.5)
for i, pt_ in enumerate(('spice1', 'spice2', 'spice3', 'spice1', 'spice2')): bottle('SPICE_01', 4.05, -1.52 + i * .1, pt_)
box('TOWEL_01', 3.65, 3.85, -.3, -.06, CT, CT + .012, 'apron', ('bottom',)); box('TOWEL_01', 3.67, 3.83, -.27, -.09, CT + .012, CT + .024, 'apron', ('bottom',))   # 행주(접은 두 겹)
cyl('COOKER_01', 3.82, .3, .13, CT, CT + .2, 'ceramic', 'ceramic', bottom=False); cyl('COOKER_01', 3.82, .3, .125, CT + .2, CT + .25, 'steel', 'steel', bottom=False)
box('COOKER_01', 3.77, 3.87, .27, .33, CT + .25, CT + .27, 'sole', ('bottom',)); cyl('COOKER_01', 3.9, .3, .014, CT + .25, CT + .275, 'steel', 'steel', bottom=False)
box('COOKER_01', 3.68, 3.7, .25, .35, CT + .06, CT + .12, 'sole', ('right',)); box('COOKER_01', 3.675, 3.68, .26, .28, CT + .08, CT + .1, 'brass', ('right',)); box('COOKER_01', 3.675, 3.68, .32, .34, CT + .08, CT + .1, 'apron', ('right',))
box('CUTBOARD_01', 3.68, 3.96, .62, 1.0, CT, CT + .02, 'chair', ('bottom',)); box('CUTBOARD_01', 3.78, 3.86, 1.0, 1.07, CT, CT + .02, 'chair', ('bottom', 'front')); box('CUTBOARD_01', 3.81, 3.83, 1.02, 1.05, CT + .02, CT + .021, 'sole', ('bottom',))
for i in range(3): cyl('CUTBOARD_01', 3.77 + i * .03, .8, .02, CT + .02, CT + .03, 'flower', 'flower', bottom=False)   # 썰어 둔 당근 세 점
box('KNIFE_01', 3.99, 4.09, .64, .76, CT, CT + .2, 'chair', ('bottom',))                                        # 칼꽂이
for i, yy in enumerate((.67, .70, .73)): box('KNIFE_01', 4.025, 4.045, yy, yy + .018, CT + .2, CT + .27, 'sole', ('bottom',)); box('KNIFE_01', 4.025, 4.045, yy, yy + .018, CT + .27, CT + .272, 'steel', ('bottom',))
box('RICE_01', 3.7, 4.05, 2.05, 2.4, 0, .5, 'plastic', ('bottom',)); box('RICE_01', 3.68, 4.07, 2.03, 2.42, .5, .54, 'plastic', ('bottom',)); box('RICE_01', 3.69, 3.7, 2.12, 2.33, .18, .38, 'paper', ('right',))  # 쌀통 + 뚜껑 + 라벨 자리
cyl('RICE_01', 3.92, 2.22, .045, .54, .6, 'plastic', 'plastic', bottom=False)                                    # 뚜껑 위 계량컵
box('CEREAL_01', 3.6, 3.72, 1.35, 1.5, 1.7, 1.98, {'*': 'cereal', 'left': 'cerealf'}, ('bottom',)); box('CEREAL_01', 3.61, 3.71, 1.36, 1.49, 1.98, 1.995, 'cereal', ('bottom',))   # 냉장고 위
box('BASKET_02', 3.76, 4.06, 1.42, 1.72, 1.7, 1.82, 'basket', ('bottom',)); box('BASKET_02', 3.75, 4.07, 1.41, 1.73, 1.82, 1.85, 'basket', ('bottom',))
for cx_, cy_ in ((3.84, 1.52), (3.95, 1.62)): cyl('BASKET_02', cx_, cy_, .035, 1.85, 1.91, 'apron', 'apron', bottom=False)
cyl('JAR_01', 3.66, 1.66, .045, 1.7, 1.8, 'jar', 'jar', bottom=False); cyl('JAR_01', 3.66, 1.66, .048, 1.8, 1.83, 'brass', 'brass', bottom=False)
box('MEMO_01', 3.488, 3.5, 1.42, 1.62, 1.35, 1.55, 'paper', ('right',)); box('MEMO_01', 3.486, 3.488, 1.45, 1.59, 1.4, 1.42, 'sole', ('right',))
box('MEMO_01', 3.478, 3.486, 1.45, 1.48, 1.5, 1.53, 'apron', ('right',)); box('MEMO_01', 3.478, 3.486, 1.56, 1.59, 1.38, 1.41, 'sofa', ('right',))   # 자석 둘
# 현관: 열쇠 걸이 · 우산꽂이 · 장바구니 · 전화기
box('KEYRACK_01', -1.3, -.95, YI, YI + .02, 1.32, 1.44, 'chair', ('front',))
for hx in (-1.22, -1.12, -1.02): box('KEYRACK_01', hx - .008, hx + .008, YI + .02, YI + .05, 1.36, 1.38, 'brass', ('front',)); box('KEYRACK_01', hx - .008, hx + .008, YI + .05, YI + .056, 1.2, 1.36, 'steel', ('front',))
cyl('UMBRELLA_01', .72, YI + .16, .09, 0, .45, 'bin', 'bin', bottom=True)
for dx, dy, h_, pt_ in ((-.04, -.02, .42, 'sole'), (.03, .03, .38, 'sofa'), (.0, -.045, .34, 'apron')):
    cyl('UMBRELLA_01', .72 + dx, YI + .16 + dy, .012, .45, .45 + h_, pt_, pt_, bottom=False); box('UMBRELLA_01', .72 + dx - .02, .72 + dx + .02, YI + .16 + dy - .012, YI + .16 + dy + .012, .45 + h_, .45 + h_ + .03, 'sole', ('bottom',))
box('BAG_02', .85, 1.15, YI + .1, YI + .36, 0, .28, 'bagcloth', ('bottom',))
for hx in (.9, 1.04):
    box('BAG_02', hx, hx + .01, YI + .22, YI + .24, .28, .4, 'bagcloth', ('bottom',)); box('BAG_02', hx + .09, hx + .1, YI + .22, YI + .24, .28, .4, 'bagcloth', ('bottom',))
    box('BAG_02', hx + .01, hx + .09, YI + .22, YI + .24, .4, .41, 'bagcloth', ('bottom', 'left', 'right'))
box('BAG_02', .95, .98, YI + .18, YI + .22, .28, .52, 'leaf', ('bottom',)); box('BAG_02', 1.02, 1.1, YI + .12, YI + .2, .28, .34, 'basket', ('bottom',)); box('BAG_02', 1.05, 1.07, YI + .15, YI + .17, .34, .36, 'flower', ('bottom',))   # 대파·빵·당근 머리
box('PHONE_01', -1.26, -1.02, YI + .05, YI + .2, .95, .98, 'plastic', ('bottom',))
for xx in (-1.235, -1.055): box('PHONE_01', xx, xx + .02, YI + .08, YI + .095, .98, 1.0, 'plastic', ('bottom', 'top'))
box('PHONE_01', -1.215, -1.06, YI + .075, YI + .1, 1.0, 1.025, 'plastic'); box('PHONE_01', -1.26, -1.215, YI + .072, YI + .103, 1.0, 1.03, 'plastic', ('right',)); box('PHONE_01', -1.06, -1.02, YI + .072, YI + .103, 1.0, 1.03, 'plastic', ('left',))
for i in range(3):
    for j in range(2): box('PHONE_01', -1.2 + i * .035, -1.2 + i * .035 + .02, YI + .13 + j * .03, YI + .13 + j * .03 + .02, .98, .985, 'sole', ('bottom',))
# 거실: 리모컨 · 잡지 · 쿠션 · 담요 · 큰 화분 · 액자 둘 · 달력
box('REMOTE_01', -2.56, -2.49, -.78, -.58, .4, .42, {'*': 'sole', 'top': 'remote'}, ('bottom',))                  # 리모컨: 버튼은 윗면 칠(1~2px 짜리 면을 따로 만들면 단색 칸)
box('MAGAZINE_01', -2.85, -2.62, -.95, -.67, .4, .41, 'cover', ('bottom',)); box('MAGAZINE_01', -2.83, -2.61, -.93, -.65, .41, .42, 'paper', ('bottom',))
box('CUSHION_01', -3.83, -3.58, -1.35, -1.0, .52, .66, 'cushionB', ('bottom',)); box('CUSHION_01', -3.81, -3.6, -1.33, -1.02, .66, .68, 'cushionB', ('bottom',))
box('CUSHION_02', -3.83, -3.58, -.12, .18, .52, .65, 'cushion', ('bottom',)); box('CUSHION_02', -3.81, -3.6, -.1, .16, .65, .67, 'cushion', ('bottom',))
for i, (a, b, c, d) in enumerate(((-3.78, -3.4, -.62, -.25), (-3.76, -3.42, -.6, -.27), (-3.74, -3.44, -.58, -.29))): box('BLANKET_01', a, b, c, d, .52 + i * .022, .52 + (i + 1) * .022, 'blanket', ('bottom',))
cyl('PLANTER_05', -3.88, -2.55, .15, 0, .28, 'terracotta', 'soil', bottom=True); cyl('PLANTER_05', -3.88, -2.55, .165, .28, .31, 'terracotta', 'terracotta', bottom=False)
for dx, dy, h_, w_ in ((-.05, .0, .5, .07), (.04, .03, .62, .08), (.02, -.05, .42, .07), (-.03, .06, .36, .06)): box('PLANT_05', -3.88 + dx - w_ / 2, -3.88 + dx + w_ / 2, -2.55 + dy - w_ / 2, -2.55 + dy + w_ / 2, .3, .3 + h_, 'leaf', ('bottom',))
def frame(nm, x_wall, y0, y1, z0, z1, pic, dirx=1):  # 벽에 건 액자: 나무 틀 3cm + 안쪽에 들어간 그림. dirx = +1 이면 벽 법선이 +x(서쪽 벽), -1 이면 -x(칸막이 부모님 쪽)
    xa, xb = (x_wall, x_wall + .03) if dirx > 0 else (x_wall - .03, x_wall)
    skip = ('left',) if dirx > 0 else ('right',)
    fw = .03
    box(nm, xa, xb, y0, y1, z0, z0 + fw, 'chair', skip + ('top',)); box(nm, xa, xb, y0, y1, z1 - fw, z1, 'chair', skip + ('bottom',))
    box(nm, xa, xb, y0, y0 + fw, z0 + fw, z1 - fw, 'chair', skip + ('top', 'bottom')); box(nm, xa, xb, y1 - fw, y1, z0 + fw, z1 - fw, 'chair', skip + ('top', 'bottom'))
    xp = (x_wall, x_wall + .012) if dirx > 0 else (x_wall - .012, x_wall)
    box(nm, xp[0], xp[1], y0 + fw, y1 - fw, z0 + fw, z1 - fw, pic, skip + ('top', 'bottom', 'front', 'back'))
frame('FRAME_01', -XI, -1.45, -.95, 1.5, 1.9, 'pic1'); frame('FRAME_02', -XI, .02, .32, 1.4, 1.75, 'pic2')
box('CALENDAR_01', -XI, -XI + .008, -2.72, -2.32, 1.3, 1.72, 'calpage', ('left', 'top')); box('CALENDAR_01', -XI, -XI + .02, -2.72, -2.32, 1.72, 1.8, 'apron', ('left',))
# 책장 + 책 줄(벽에 붙은 책장 = SHELF_nn, 책 = BOOKS_nn). 'N' = 북쪽 벽(YB)에 서서 앞이 −y · 'E' = 동쪽 벽(XI)에 서서 앞이 −x
def bookshelf(sh, bk, wall, u0, w, z0, h, nplank, d=.28):
    def bx(nm, a0, a1, d0, d1, za, zb, pt, skip=()):  # a = 벽 따라 u 좌표, d = 벽에서 앞으로 거리. skip: 벽쪽 'wallf'
        if wall == 'N':
            sk = tuple({'wallf': 'back'}.get(k, k) for k in skip); box(nm, u0 + a0, u0 + a1, YB - d1, YB - d0, za, zb, pt, sk)
        else:
            sk = tuple({'wallf': 'right'}.get(k, k) for k in skip); box(nm, XI - d1, XI - d0, u0 + a0, u0 + a1, za, zb, pt, sk)
    bx(sh, 0, .03, 0, d, z0, z0 + h, 'chair', ('wallf', 'bottom')); bx(sh, w - .03, w, 0, d, z0, z0 + h, 'chair', ('wallf', 'bottom'))
    gaps = [z0 + .04 + i * (h - .08 - .03) / (nplank - 1) for i in range(nplank)]
    for gz in gaps: bx(sh, .03, w - .03, 0, d, gz, gz + .03, 'chair', ('wallf', 'left', 'right') if wall == 'N' else ('wallf', 'front', 'back'))
    for gz in gaps[:-1]:
        x_ = .05
        while x_ < w - .09:
            bw = RNG.choice((.025, .03, .035, .04, .05)); bh = RNG.choice((.16, .19, .22, .25, .27)); ptb = RNG.choice(('bk1', 'bk2', 'bk3', 'bk4', 'bk5', 'bk6'))
            if x_ + bw > w - .22: break
            bx(bk, x_, x_ + bw, .03, d - .04, gz + .03, gz + .03 + bh, ptb, ('bottom',)); x_ += bw + .004
            if RNG.random() < .12: x_ += RNG.choice((.03, .06))     # 가끔 빈틈
        bx(bk, w - .2, w - .06, .03, d - .04, gz + .03, gz + .03 + .025, RNG.choice(('bk3', 'bk5')), ('bottom',))   # 눕혀 둔 책 한 권(오른쪽 끝, 두께 2.5cm)
Z = H2
bookshelf('SHELF_03', 'BOOKS_01', 'N', -3.0, 1.2, Z, 1.8, 4)        # 부모님 방 뒤 벽(북쪽)
bookshelf('SHELF_04', 'BOOKS_02', 'E', .3, .9, Z, 1.5, 4)           # 주인공 방 동쪽 벽(계단 도착 구석 앞)
# 부모님 방: 옷걸이 옷(스탠드 행거) · 핸드백 · 액자
RKX0, RKX1, RKY0, RKY1, RKZ = -1.55, -.55, 2.4, 2.72, Z + 1.6
for px in (RKX0, RKX1 - .03): box('RACK_02', px, px + .03, RKY0 + .14, RKY0 + .17, Z + .03, RKZ, 'iron', ('bottom', 'top'))
for px in (RKX0 - .05, RKX1 - .08): box('RACK_02', px, px + .13, RKY0, RKY1, Z, Z + .03, 'iron', ('bottom',))
box('RACK_02', RKX0, RKX1, RKY0 + .14, RKY0 + .17, RKZ, RKZ + .03, 'iron', ('left', 'right'))
for i, (cw, pt_) in enumerate(((.17, 'clothA'), (.16, 'clothB'), (.15, 'clothC'))):
    gx = RKX0 + .12 + i * .3; gy = RKY0 + .155
    box('HANGER_01', gx + cw * .5 - .006, gx + cw * .5 + .006, gy - .006, gy + .006, RKZ - .04, RKZ, 'steel', ('bottom', 'top'))                     # 걸이 고리(바에서 매달림)
    box('HANGER_01', gx, gx + cw, gy - .006, gy + .006, RKZ - .06, RKZ - .04, 'steel')                                                            # 걸이 어깨
    box('CLOTHES_01', gx, gx + cw, gy - .02, gy + .02, RKZ - .06 - .7, RKZ - .06, pt_, ('top',))                                      # 몸판
    box('CLOTHES_01', gx - .06, gx, gy - .016, gy + .016, RKZ - .06 - .5, RKZ - .06, pt_, ('top', 'right'))                                      # 소매 둘(몸판 옆, 얇게)
    box('CLOTHES_01', gx + cw, gx + cw + .06, gy - .016, gy + .016, RKZ - .06 - .5, RKZ - .06, pt_, ('top', 'left'))
box('BAG_03', .2, .5, -1.7, -1.4, Z + .85, Z + 1.0, 'leather', ('bottom',)); box('BAG_03', .22, .48, -1.71, -1.7, Z + .9, Z + .95, 'brass', ('back',))
for yy in (-1.65, -1.45): box('BAG_03', .26, .27, yy, yy + .03, Z + 1.0, Z + 1.1, 'leather', ('bottom',)); box('BAG_03', .43, .44, yy, yy + .03, Z + 1.0, Z + 1.1, 'leather', ('bottom',))
box('BAG_03', .27, .43, -1.65, -1.62, Z + 1.1, Z + 1.11, 'leather', ('bottom',)); box('BAG_03', .27, .43, -1.45, -1.42, Z + 1.1, Z + 1.11, 'leather', ('bottom',))
frame('FRAME_03', DIVX, -.7, -.2, Z + 1.3, Z + 1.7, 'pic1', dirx=-1)
# 주인공 방: 동쪽 벽 갈고리 판 + 걸린 옷(계단 도착 구석)
box('HANGER_02', XI - .03, XI, 1.3, 1.75, Z + 1.55, Z + 1.61, 'chair', ('right',))
for hy in (1.4, 1.62): box('HANGER_02', XI - .06, XI - .03, hy - .008, hy + .008, Z + 1.56, Z + 1.58, 'brass', ('right',))
for hy, pt_ in ((1.4, 'clothD'), (1.62, 'clothA')):
    box('CLOTHES_02', XI - .09, XI - .06, hy - .012, hy + .012, Z + 1.52, Z + 1.58, 'brass', ('right', 'bottom'))    # 고리
    box('CLOTHES_02', XI - .1, XI - .06, hy - .06, hy + .06, Z + .85, Z + 1.52, pt_, ('top',))                      # 겉옷(몸판)
    box('CLOTHES_02', XI - .1, XI - .06, hy - .09, hy - .06, Z + 1.0, Z + 1.52, pt_, ('top', 'back')); box('CLOTHES_02', XI - .1, XI - .06, hy + .06, hy + .09, Z + 1.0, Z + 1.52, pt_, ('top', 'front'))

# ===== 1421b 867e 재시도 — 판정 "작고 드문드문" : 계단 위 · 바닥 가운데 · 식탁 · 부엌 벽 · 거실 · 계단 밑 =====
# 1608 PD "계단 디딤마다 물건(러너·신발·책·수건·바구니)" → 계단은 비움. 첫 단 옆 바닥에 둘만: 벗어 둔 신발 한 켤레(첫 단 앞 뒷벽 쪽) · 빨래 바구니(계단 옆판 앞, 상자 옆)
sx_ = SX0 - .44
for nm_, dx in (('SHOE_06', .02), ('SHOE_07', .14)):
    box(nm_, sx_ + dx, sx_ + dx + .1, 2.5, 2.77, 0, .03, 'sole', ('bottom',)); box(nm_, sx_ + dx + .01, sx_ + dx + .09, 2.57, 2.76, .03, .1, 'leather', ('bottom',))
cyl('LAUNDRY_01', -.05, 1.68, .1, 0, .19, 'basket', 'sole', bottom=False); cyl('LAUNDRY_01', -.05, 1.68, .085, .19, .25, 'clothB', 'clothB', bottom=False)
# 바닥 가운데: 빨래 건조대(막대 4 + 널린 천 3) · 슬리퍼 · 장난감 레이싱카
DX0_, DX1_, DY0_, DY1_ = .9, 1.55, .7, 1.15
for px in (DX0_, DX1_ - .03):
    for py in (DY0_, DY1_ - .03): box('DRYER_01', px, px + .03, py, py + .03, 0, .96, 'iron', ('top',))
    box('DRYER_01', px, px + .03, DY0_, DY1_, .96, 1.0, 'iron')
for k, ry in enumerate((.78, .88, .98, 1.08)):
    box('DRYER_01', DX0_ + .03, DX1_ - .03, ry, ry + .016, .96, .976, 'iron', ('left', 'right'))
    if k < 3: box('CLOTHES_03', DX0_ + .1 + k * .05, DX1_ - .12 - (2 - k) * .05, ry + .016, ry + .026, .55 + k * .05, .976, ('clothA', 'clothB', 'clothC')[k], ('front', 'top'))
for nm_, sx, sy in (('SLIPPER_01', -.72, 1.2), ('SLIPPER_02', -.57, 1.24)):
    box(nm_, sx, sx + .1, sy, sy + .26, 0, .02, 'sole', ('bottom',)); box(nm_, sx + .01, sx + .09, sy + .12, sy + .25, .02, .055, 'apron', ('bottom',)); box(nm_, sx + .01, sx + .09, sy + .08, sy + .12, .02, .04, 'apron', ('bottom',))
TX0, TY0 = -1.9, -.62
box('TOY_01', TX0, TX0 + .12, TY0, TY0 + .05, .012, .04, 'flower', ('bottom',)); box('TOY_01', TX0 + .05, TX0 + .08, TY0 + .01, TY0 + .04, .04, .06, 'chair', ('bottom',))
box('TOY_01', TX0 - .03, TX0, TY0 + .015, TY0 + .035, .012, .03, 'flower', ('bottom', 'right')); box('TOY_01', TX0 + .125, TX0 + .13, TY0 - .005, TY0 + .055, .04, .055, 'sole', ('bottom', 'left'))
for wx_ in (TX0 + .015, TX0 + .085):
    box('TOY_01', wx_, wx_ + .025, TY0 - .015, TY0, 0, .03, 'sole', ('back',)); box('TOY_01', wx_, wx_ + .025, TY0 + .05, TY0 + .065, 0, .03, 'sole', ('front',))
# 식탁 위: 빵 바구니 · 물병 · 과일 그릇 · 수저 2벌 · 의자 방석 2
cyl('BREAD_01', 2.2, -.55, .06, .76, .8, 'basket', 'basket', bottom=False); box('BREAD_01', 2.155, 2.245, -.585, -.515, .8, .83, 'terracotta', ('bottom',))
cyl('PITCHER_01', 1.68, -.47, .045, .76, .92, 'ceramic', 'coffee', bottom=False); box('PITCHER_01', 1.725, 1.755, -.482, -.458, .8, .9, 'ceramic', ('left',))
cyl('FRUIT_01', 1.66, -.2, .07, .76, .8, 'ceramic', 'ceramic', bottom=False)
for ax, ay, pt_ in ((1.63, -.2, 'flower'), (1.692, -.2, 'leaf'), (1.661, -.146, 'flower')): cyl('FRUIT_01', ax, ay, .03, .8, .86, pt_, pt_, bottom=False)
for cx_, cy_ in ((1.65, -.76), (2.05, -.3)):
    box('CUTLERY_01', cx_ - .01, cx_ + .01, cy_ - .06, cy_ + .06, .76, .766, 'steel', ('bottom',)); box('CUTLERY_01', cx_ - .022, cx_ + .022, cy_ + .06, cy_ + .1, .76, .766, 'steel', ('bottom', 'front'))
    box('CUTLERY_01', cx_ + .27 + (.0 if cy_ < -.5 else .06), cx_ + .29 + (.0 if cy_ < -.5 else .06), cy_ - .06, cy_ + .0, .76, .77, 'sole', ('bottom',)); box('CUTLERY_01', cx_ + .27 + (.0 if cy_ < -.5 else .06), cx_ + .29 + (.0 if cy_ < -.5 else .06), cy_ + .0, cy_ + .1, .76, .766, 'steel', ('bottom', 'front'))
box('CUSHION_03', 1.8, 2.2, -1.4, -1.02, .46, .49, 'cushionB', ('bottom',)); box('CUSHION_04', 1.8, 2.2, .04, .42, .46, .49, 'cushion', ('bottom',))
# 부엌 벽: 걸이 막대 + 걸린 도구 4 · 싱크 위 선반 + 단지 3
box('UTENSIL_01', 4.12, 4.14, -2.05, -2.0, 1.28, 1.34, 'steel', ('right',)); box('UTENSIL_01', 4.12, 4.14, -1.1, -1.05, 1.28, 1.34, 'steel', ('right',)); box('UTENSIL_01', 4.12, 4.14, -2.0, -1.1, 1.3, 1.32, 'steel', ('front', 'back', 'right'))
for uy, kind in ((-1.88, 'spat'), (-1.74, 'ladle'), (-1.6, 'whisk'), (-1.46, 'tong')):
    box('UTENSIL_01', 4.125, 4.135, uy - .004, uy + .004, 1.22, 1.3, 'steel', ('top', 'right'))
    wd = .02 if kind != 'whisk' else .035
    box('UTENSIL_01', 4.115, 4.135, uy - wd / 2, uy + wd / 2, 1.0 + (.03 if kind == 'tong' else 0), 1.22, 'sole', ('top', 'right'))
    if kind == 'spat': box('UTENSIL_01', 4.115, 4.135, uy - .035, uy + .035, .94, 1.0, 'steel', ('top', 'right'))
    if kind == 'ladle': box('UTENSIL_01', 4.115, 4.135, uy - .04, uy + .04, .9, 1.0, 'steel', ('top', 'right'))
    if kind == 'whisk': box('UTENSIL_01', 4.115, 4.135, uy - .035, uy + .035, .9, 1.0, 'steel', ('top', 'right'))
box('SHELF_05', 3.98, 4.15, -.9, -.35, 1.18, 1.2, 'chair', ('right',))
for jy in (-.8, -.65, -.5):
    cyl('JAR_02', 4.06, jy, .04, 1.2, 1.3, 'jar', 'jar', bottom=False); cyl('JAR_02', 4.06, jy, .043, 1.3, 1.32, 'brass', 'brass', bottom=False)
# 거실: 스탠드 조명(소파 옆) · 벽 선반(소파 북쪽 벽, 화분·책·사진)
cyl('LAMP_02', -3.85, -2.0, .1, 0, .03, 'iron', 'iron'); cyl('LAMP_02', -3.85, -2.0, .012, .03, 1.3, 'iron', 'iron', bottom=False)
cyl('LAMP_02', -3.85, -2.0, .16, 1.15, 1.5, 'shade', 'shade', btm='bulb')
box('SHELF_06', -XI, -XI + .22, .65, 1.45, 1.5, 1.53, 'chair', ('left',))
cyl('PLANTER_06', -4.03, .8, .04, 1.53, 1.6, 'terracotta', 'soil', bottom=False); box('PLANT_06', -4.045, -4.015, .785, .815, 1.6, 1.7, 'leaf', ('bottom',))
box('BOOKS_04', -4.1, -3.96, 1.0, 1.04, 1.53, 1.75, 'bk1', ('bottom',)); box('BOOKS_04', -4.1, -3.96, 1.04, 1.075, 1.53, 1.69, 'bk3', ('bottom',)); box('BOOKS_04', -4.1, -3.96, 1.075, 1.12, 1.53, 1.72, 'bk5', ('bottom',))
box('FRAME_04', -4.1, -4.07, 1.2, 1.36, 1.53, 1.66, 'chair', ('bottom', 'right')); box('FRAME_04', -4.07, -4.058, 1.215, 1.345, 1.545, 1.645, 'pic2', ('bottom', 'left', 'top', 'front', 'back'))
# 계단 밑(계단 옆판 = 안쪽 벽 y 1.85): 나무 상자 2단 · 빗자루 · 양동이 · 코트 걸이 + 외투 2
def crate(nm, x0, x1, y0, y1, z0, h):
    box(nm, x0, x1, y0, y1, z0, z0 + .02, 'chair', ('bottom',))
    for px, py in ((x0, y0), (x1 - .03, y0), (x0, y1 - .03), (x1 - .03, y1 - .03)): box(nm, px, px + .03, py, py + .03, z0 + .02, z0 + h, 'chair', ('bottom',))
    for k in range(3):
        zs = z0 + .04 + k * .08
        box(nm, x0 + .03, x1 - .03, y0, y0 + .02, zs, zs + .05, 'chair', ('left', 'right')); box(nm, x0 + .03, x1 - .03, y1 - .02, y1, zs, zs + .05, 'chair', ('left', 'right'))
        box(nm, x0, x0 + .02, y0 + .03, y1 - .03, zs, zs + .05, 'chair', ('front', 'back')); box(nm, x1 - .02, x1, y0 + .03, y1 - .03, zs, zs + .05, 'chair', ('front', 'back'))
crate('CRATE_01', .2, .7, 1.3, 1.82, 0, .3); crate('CRATE_02', .22, .66, 1.34, 1.8, .3, .26)
for cx_, cy_ in ((.35, 1.5), (.52, 1.62)): cyl('CRATE_02', cx_, cy_, .07, .32, .4, 'leaf', 'leaf', bottom=False)
box('BROOM_01', 1.0, 1.1, 1.74, 1.82, 0, .14, 'basket', ('bottom',)); box('BROOM_01', 1.04, 1.06, 1.77, 1.79, .14, 1.4, 'chair', ('bottom',)); box('BROOM_01', 1.03, 1.07, 1.765, 1.795, .14, .18, 'sole', ('bottom',))
cyl('BUCKET_01', 1.5, 1.6, .13, 0, .28, 'bin', 'sole')
for px in (1.365, 1.625): box('BUCKET_01', px, px + .01, 1.595, 1.605, .28, .4, 'steel', ('top', 'bottom'))
box('BUCKET_01', 1.365, 1.635, 1.595, 1.605, .4, .41, 'steel', ('bottom',))
box('HOOK_03', 2.0, 2.8, 1.8, 1.85, 1.5, 1.54, 'chair', ('back',))
for hx, nm_, pt_ in ((2.15, 'COAT_01', 'clothD'), (2.55, 'COAT_02', 'clothB')):
    box('HOOK_03', hx - .01, hx + .01, 1.77, 1.8, 1.5, 1.54, 'brass', ('back', 'bottom'))
    box(nm_, hx - .12, hx + .12, 1.78, 1.82, .8, 1.46, pt_, ('top',)); box(nm_, hx - .17, hx - .12, 1.785, 1.815, .9, 1.46, pt_, ('top', 'right')); box(nm_, hx + .12, hx + .17, 1.785, 1.815, .9, 1.46, pt_, ('top', 'left'))
    box(nm_, hx - .01, hx + .01, 1.79, 1.8, 1.46, 1.5, 'steel', ('bottom', 'top'))

# ===== 1421c 867e 3차 — 판정 "주방·가구 위 소품 종류·수량 · 세부 표현 적음" : 손잡이 · 벽 액자·거울 · 식탁 깔개 · 현관 장화·화분 · 고양이 =====
for k in range(6):                                             # 윗장 문짝 6장: 각 문 오른쪽 가장자리 세로 손잡이(1.8cm 폭, 3cm 튀어나옴)
    hy = -2.3 + 3.2 / 6 * (k + 1) - .05; box('KNOB_03', 3.77, 3.8, hy - .009, hy + .009, 1.5, 1.64, 'brass', ('right',))
for k in range(8):                                             # 하부장 문짝 8장
    hy = YI + .03 + .5 * (k + 1) - .05; box('KNOB_04', 3.52, 3.55, hy - .009, hy + .009, .66, .78, 'steel', ('right',))
def fy(nm, x0, x1, z0, z1, pic):                               # 앞벽 안쪽 면(y = YI, 법선 +y) 액자: 틀 3cm + 안쪽 그림
    fw = .03; sk = ('front',)
    box(nm, x0, x1, YI, YI + .03, z0, z0 + fw, 'chair', sk + ('top',)); box(nm, x0, x1, YI, YI + .03, z1 - fw, z1, 'chair', sk + ('bottom',))
    box(nm, x0, x0 + fw, YI, YI + .03, z0 + fw, z1 - fw, 'chair', sk + ('top', 'bottom', 'right')); box(nm, x1 - fw, x1, YI, YI + .03, z0 + fw, z1 - fw, 'chair', sk + ('top', 'bottom', 'left'))
    box(nm, x0 + fw, x1 - fw, YI, YI + .012, z0 + fw, z1 - fw, pic, sk + ('top', 'bottom', 'left', 'right'))
fy('FRAME_05', 1.3, 1.65, 1.45, 1.85, 'pic2'); fy('FRAME_06', -1.55, -1.1, 1.65, 2.0, 'pic1')
box('MIRROR_02', .8, 1.2, YI, YI + .025, 1.35, 1.85, {'*': 'chair', 'back': 'mirror'}, ('front',))
hface('RUG_04', 1.25, 2.75, -1.5, .5, .011, 'rug')             # 식탁 밑 깔개
cyl('PLANTER_07', 1.6, -2.55, .17, 0, .3, 'terracotta', 'soil', bottom=True); cyl('PLANTER_07', 1.6, -2.55, .185, .3, .33, 'terracotta', 'terracotta', bottom=False)
for dx, dy, h_, w_ in ((-.06, .0, .75, .09), (.05, .04, .95, .1), (.02, -.06, .6, .08), (-.04, .07, .5, .07), (.08, -.02, .4, .07)): box('PLANT_07', 1.6 + dx - w_ / 2, 1.6 + dx + w_ / 2, -2.55 + dy - w_ / 2, -2.55 + dy + w_ / 2, .32, .32 + h_, 'leaf', ('bottom',))
for nm_, bx0 in (('SHOE_08', -.82), ('SHOE_09', -.67)):          # 현관 장화 한 켤레(문 옆 벽)
    box(nm_, bx0, bx0 + .12, YI + .1, YI + .36, 0, .03, 'sole', ('bottom',)); box(nm_, bx0 + .01, bx0 + .11, YI + .12, YI + .35, .03, .1, 'bin', ('bottom',)); box(nm_, bx0 + .01, bx0 + .11, YI + .12, YI + .26, .1, .3, 'bin', ('bottom',))
box('CAT_01', -3.2, -2.95, -.1, .04, .011, .12, 'terracotta', ('bottom',)); box('CAT_01', -2.95, -2.87, -.08, -.0, .02, .13, 'terracotta', ('bottom', 'right'))
for ey in (-.075, -.02): box('CAT_01', -2.93, -2.9, ey, ey + .02, .13, .15, 'terracotta', ('bottom',))
box('CAT_01', -3.28, -3.2, -.03, .01, .011, .045, 'sole', ('bottom', 'right'))

# ---------- UV 칸 배치(선반 쌓기) → 메시 ----------
def local2d(pts):
    nrm = Vector()
    for i in range(len(pts)): a, b = pts[i], pts[(i + 1) % len(pts)]; nrm += Vector(((a.y - b.y) * (a.z + b.z), (a.z - b.z) * (a.x + b.x), (a.x - b.x) * (a.y + b.y)))
    nrm.normalize(); u = (pts[1] - pts[0]).normalized(); v = nrm.cross(u)
    return [((p - pts[0]).dot(u), (p - pts[0]).dot(v)) for p in pts]
AW = 2048; items = []  # 0351: 집 안이 들어와 1024 폭이면 8192 높이
for i, (obj, pts, pt, d) in enumerate(FACES):
    l2 = local2d(pts); u0, v0 = min(a for a, _ in l2), min(b for _, b in l2)
    um, vm = max(a for a, _ in l2) - u0, max(b for _, b in l2) - v0
    items.append(dict(i=i, obj=obj, pt=pt, dens=d, l2=[(a - u0, b - v0) for a, b in l2], m=(um, vm), w=max(2, math.ceil(um * d)), h=max(2, math.ceil(vm * d))))
# 껍데기는 키 순, 낱개는 물건·칠 종류끼리 모아 새 줄부터(작은 면이 남의 칸 사이에 흩어지면 0.25m 검사 칸에 색 경계가 섞인다, check 2차)
order = sorted(items, key=lambda t: (t['obj'] != SHELL, t['obj'] if t['obj'] != SHELL else '', t['pt'] if t['obj'] != SHELL else '', -t['h'], -t['w'])); x = y = 4; rowh = 0; grp = None
for t in order:
    g = (t['obj'], t['pt']) if t['obj'] != SHELL else None
    if x + t['w'] + 4 > AW or (g != grp and g is not None and x > 4 and t['obj'] != (grp or ('',))[0]):
        x, y, rowh = 4, y + rowh + 8, 0
        if g is not None: y = -(-y // 16) * 16  # 낱개 줄 시작 = 16px(0.25m@64) 검사 격자에 맞춤(어긋나면 검사 칸이 이웃 줄을 먹는다, check 3차)
    grp = g
    t['x'], t['y'] = x, y; x += t['w'] + 8; rowh = max(rowh, t['h'])
AH = math.ceil((y + rowh + 4) / 128) * 128  # Point·mip off·NPOT None: 빈 하반부 때문에 8192로 두 배 늘리지 않음.
objs = {}
for t in items:
    obj, pts = FACES[t['i']][0], FACES[t['i']][1]
    # 0419: UV는 실제 길이×밀도. 2cm 난간을 최소 2px로 늘리던 80px/m 오류 제거.
    objs.setdefault(obj, []).append((pts, [((t['x'] + .5 + a * t['dens']) / AW, (t['y'] + .5 + b * t['dens']) / AH) for a, b in t['l2']]))
mat = bpy.data.materials.new('HOUSE_alb'); gmat = bpy.data.materials.new('HOUSE_glass')  # 0351: 유리 = 따로 재질(Unity 에서 반투명)
obs = []; tris = 0
for name, fl in objs.items():
    me = bpy.data.meshes.new(name); bm = bmesh.new(); uvl = bm.loops.layers.uv.new('UVMap')
    allp = [p for pts, _ in fl for p in pts]
    pv = Vector() if name == SHELL else PIVOT[name] if name in PIVOT else Vector(((min(p.x for p in allp) + max(p.x for p in allp)) / 2, (min(p.y for p in allp) + max(p.y for p in allp)) / 2, min(p.z for p in allp)))
    for pts, uvs in fl:
        f = bm.faces.new([bm.verts.new(p - pv) for p in pts])
        for lp, uv in zip(f.loops, uvs): lp[uvl].uv = uv
    bm.to_mesh(me); bm.free(); ob = bpy.data.objects.new(name, me); ob.location = pv; sc.collection.objects.link(ob); me.materials.append(gmat if name.startswith('GLASS_') else mat); obs.append(ob)
    tris += sum(len(p.vertices) - 2 for p in me.polygons)
# 캠: 발코니 문(방 안 문턱 뒤 눈높이 → 열린 문·발코니 너머 바깥) · 1층 정면 근접(테라스 앞 → 현관·부엌 창·의자)
for en, loc in (('STUDIO_CamBalc', (-.15, FY + .75, H2 + 1.5)), ('STUDIO_CamBalcLook', (.25, FY - 8.0, H2 - .7)),
                ('STUDIO_Cam_balc', (.3, FY - 4.0, H2 + 3.1)), ('STUDIO_Cam_balcLook', (-1.9, BYF + .6, H2 + .45)),      # 0133: 마당 쪽 위에서 → 발코니 탁자·의자·커피·화분
                ('STUDIO_Cam_kitchen', (.3, -2.0, 1.6)), ('STUDIO_Cam_kitchenLook', (3.8, .2, .95)),                      # 0133: 부엌 → 식탁·후추통·프라이팬·냉장고
                ('STUDIO_CamDoor1', (.9, FY - 4.6, 1.55)), ('STUDIO_CamDoor1Look', (.9, FY, 1.15)),
                ('STUDIO_Cam_living', (3.3, YI + .35, 1.6)), ('STUDIO_Cam_livingLook', (-3.2, 1.2, .7)),          # 0351: 부엌 앞 구석 → 거실·식탁·계단
                ('STUDIO_Cam_stairs', (-2.6, .2, 1.55)), ('STUDIO_Cam_stairsLook', (2.2, 2.35, 2.3)),             # 거실 → 계단 올라감
                ('STUDIO_Cam_hall2f', (3.2, 1.0, H2 + 1.55)), ('STUDIO_Cam_hall2fLook', (-3.0, -.4, H2 + .9)),    # 1421: 주인공 방 안쪽 → 칸막이 출입구(층계참 쪽) 너머 부모님 방
                ('STUDIO_Cam_room2f', (3.9, -2.4, H2 + 1.6)), ('STUDIO_Cam_room2fLook', (1.2, .6, H2 + .8)),      # 주인공 방 남동 구석 → 방 안(책장·옷 걸이·계단 도착)
                ('STUDIO_Cam_parents', (-.3, 1.5, H2 + 1.55)), ('STUDIO_Cam_parentsLook', (-3.4, -1.6, H2 + .6)),  # 1421: 부모님 방 출입구 안쪽 → 침대·옷장·서랍·핸드백
                ('STUDIO_Cam_parentsB', (-.3, -2.3, H2 + 1.6)), ('STUDIO_Cam_parentsBLook', (-3.2, 2.2, H2 + 1.0)),  # 부모님 방 앞 문 쪽 → 뒤 책장·스탠드 행거·옷
                ('STUDIO_Cam_win', (1.5, .5, H2 + 1.45)), ('STUDIO_Cam_winLook', (4.15, -1.95, H2 + 1.35)),    # 18e9: 안에서 본 동쪽 창
                ('STUDIO_Cam_winout', (8.2, -4.2, 5.2)), ('STUDIO_Cam_winoutLook', (4.3, -1.95, H2 + 1.4)),     # 18e9: 밖에서 본 동쪽 창(틀·유리·창살·덧창)
                ('STUDIO_Cam_fridge', (1.6, -.3, 1.5)), ('STUDIO_Cam_fridgeLook', (3.8, 1.5, 1.4)),                # 867e: 냉장고 위·쌀통·메모
                ('STUDIO_Cam_counter', (2.4, -2.6, 1.55)), ('STUDIO_Cam_counterLook', (3.9, -1.0, .95)),           # 867e: 조리대 위 주전자·냄비·양념·도마·칼꽂이
                ('STUDIO_Cam_entry', (0.0, .5, 1.5)), ('STUDIO_Cam_entryLook', (.0, YI, .9)),                      # 867e: 현관 열쇠 걸이·우산꽂이·장바구니
                ('STUDIO_Cam_sofa', (-1.4, 1.1, 1.4)), ('STUDIO_Cam_sofaLook', (-3.8, -.8, .6))):                 # 867e: 소파 쿠션·담요·액자·달력·화분
    e = bpy.data.objects.new(en, None); e.location = loc; sc.collection.objects.link(e); obs.append(e)
bpy.ops.object.select_all(action='DESELECT')
for ob in obs: ob.select_set(True)
bpy.context.view_layer.objects.active = obs[0]
bpy.ops.export_scene.fbx(filepath=OUT + 'house_home.fbx', use_selection=True, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
                         path_mode='STRIP', embed_textures=False, mesh_smooth_type='FACE', object_types={'MESH', 'EMPTY'})
lay = dict(atlas=[AW, AH], cells=[dict(obj=t['obj'], pt=t['pt'], dens=t['dens'], rect=[t['x'], t['y'], t['w'], t['h']], m=[round(t['m'][0], 3), round(t['m'][1], 3)], seed=t['i'])
                                  for t in items])
json.dump(lay, open(OUT + 'layout.json', 'w', encoding='utf-8'), indent=0)
meas = dict(stairs=dict(rise=SR, tread=ST, steps=SN_ + 1, deg=round(math.degrees(math.atan2(SR, ST)), 1)), ceil=dict(f1=C1, f2=round(H2C - H2, 2)),
            balcony=dict(w=2 * BX, d=round(FY - BYF, 2)), glass=len(GLASS), tris=tris, objects=[o.name for o in obs], atlas=[AW, AH], faces=len(FACES), balusters=n, size_m=dict(w=2 * WX, d=BY - FY, wall_top=WT, ridge=round(zu(0) + RT + .07, 2)),
            room_offset_unity=[0, H2, round(-RY, 3)], pivot='기단 윗면(z 0) 가운데(방 가운데는 앞으로 1.35m)', forward='발코니·현관 = Blender -Y → FBX/Unity +Z')
json.dump(meas, open(OUT + 'measure.json', 'w', encoding='utf-8'), indent=1, ensure_ascii=False); print('MEAS', meas, flush=True)
sys.stdout.flush(); os._exit(0)
