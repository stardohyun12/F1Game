"""주인공 2층집 house_home — 과제 20261029-0031 (D-198·D-199). Blender bpy 절차 생성(형태·UV 칸만, 텍스처는 build_asset.py house_home grain):
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/house_home.py
형태(0052, D-200 넓직·엄마와 둘): 바깥 8.6×6.0m, 1층 3.2m + 2층 = 주인공 방(art/export/q1-room, 프리팹 자식, 바닥 z 3.2, 방 가운데 y = FY+1.65) + 박공(용마루 X축, 24°).
  비탈 기단(돌 0.9m) = 앞 테라스 1.2m + 현관 계단. 1층: 현관 DOOR_01 · 거실 창(열린 덧창) · 부엌 창(커튼 CURTAIN_01·02 + 창턱 화분 PLANTER_03/PLANT_03) · 엄마 의자 CHAIR_01.
  2층: 방 발코니 문(구멍 x ±.45, 방 q1_room.py 창 벽을 문으로) + 문짝 DOOR_BALCONY(방 안으로 80° 열림, 피벗 경첩) + 엄마 방 창 2(열린 덧창).
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
W2L, W2M = (-3.4, -2.5, 4.0, 5.2), (-1.0, -.1, 4.0, 5.2)   # 부모님 방 창 2(옛 W2R = 주인공 방 발코니 문 자리라 없앰)
HOLES = [(DX0, DX1, 0, DZ), WL, WK, BD, W2L, W2M]
zs = sorted({0, WT} | {h[2] for h in HOLES} | {h[3] for h in HOLES})
for za, zb in zip(zs, zs[1:]):  # 앞벽 = 높이 띠마다 구멍 사이 토막
    xs = [-WX]
    for x0, x1, z0, z1 in sorted(HOLES):
        if z0 <= za and zb <= z1: wall('F', xs[-1], x0, za, zb) if x0 > xs[-1] else None; xs.append(x1)
    wall('F', xs[-1], WX, za, zb)
wall('B', -WX, WX, 0, WT); wall('L', FY, BY, 0, WT); wall('R', FY, BY, 0, WT)
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
for w_ in (WL, WK, W2L, W2M): inset(*w_)
# 발코니 문 구멍: 벽 두께 흰 면 3 + 문턱(방 q1_room 의 껍데기를 버려서 집이 직접 — 0351)
bx0, bx1, bz0, bz1 = BD
face(SHELL, [(bx0, FY, bz0), (bx0, YI, bz0), (bx0, YI, bz1), (bx0, FY, bz1)], 'trim'); face(SHELL, [(bx1, YI, bz0), (bx1, FY, bz0), (bx1, FY, bz1), (bx1, YI, bz1)], 'trim')
face(SHELL, [(bx0, YI, bz1), (bx1, YI, bz1), (bx1, FY, bz1), (bx0, FY, bz1)], 'trim'); face(SHELL, [(bx0, FY, bz0), (bx1, FY, bz0), (bx1, YI, bz0), (bx0, YI, bz0)], 'trim')
# 흰 틀(2cm 돋음): 옆은 위·아래·뒤 skip(위 띠·창턱·발코니 바닥과 같은 면 0)
def surround(x0, x1, z0, z1, sill=True, head=True):
    y0 = FY - .02
    box(SHELL, x0 - .12, x0, y0, FY, z0, z1, 'trim', ('back', 'top', 'bottom')); box(SHELL, x1, x1 + .12, y0, FY, z0, z1, 'trim', ('back', 'top', 'bottom'))
    if head: box(SHELL, x0 - .12, x1 + .12, y0, FY, z1, z1 + .12, 'trim', ('back',))
    if sill: box(SHELL, x0 - .18, x1 + .18, FY - .06, FY, z0 - .08, z0, 'trim', ('back',))
surround(DX0, DX1, 0, DZ, sill=False); surround(*BD, sill=False)
for w_ in (WL, WK, W2L, W2M): surround(*w_)
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
# 발코니 문(D-200): 방 안쪽으로 80° 열림. 경첩 = 문 구멍 오른쪽(x .43), 방 안 문틀(방 y -1.47) 바로 안. 피벗 = 경첩 아래
HX, HY, OA, LW = DOX + .43, FY + .19, math.radians(80), .86
dd = Vector((-math.cos(OA), math.sin(OA), 0)); nn = Vector((dd.y, -dd.x, 0))          # 닫힌 방향 -X 를 방 안(+Y)으로 80°, 두께는 경첩 바깥쪽
box_o = Vector((HX, HY, 0)) - nn * .04
obox('DOOR_BALCONY', box_o, dd, LW, nn, .04, H2 + .005, H2 + 2.03, 'door')
kb = box_o + dd * (LW - .1) - nn * .04                                                 # 손잡이: 문 안쪽 면(n0)에 붙음 → 붙는 면(n1) skip
obox('DOOR_BALCONY', kb, dd, .05, nn, .04, H2 + 1.0, H2 + 1.05, 'brass', ('n1',))
PIVOT['DOOR_BALCONY'] = Vector((HX, HY, H2 + .005))
# 덧창(C2 초록): 거실 창·2층 창 2 = 활짝 열려 벽에 붙음(4cm 띄움). 부엌 창은 덧창 대신 커튼
k = 0
for x0, x1, z0, z1 in (WL, W2L, W2M):
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
hwall(SHELL, (XI, YI), (-XI, YI), H2, H2C, [BD, W2L, W2M], lambda c, z: rw(c, z) if c > PW else 'plaster', (PW, DIVX), (RW,))
hwall(SHELL, (-XI, YB), (XI, YB), H2, H2C); hwall(SHELL, (-XI, YI), (-XI, YB), H2, H2C)
hwall(SHELL, (XI, YB), (XI, YI), H2, H2C, (), lambda c, z: rw(c, z) if c < PB0 else 'plaster', (PB0,), (RW,))   # 동쪽 벽: 방 쪽(y < PB0)은 포스터 벽
# --- 슬래브(1층 천장 2.8 / 2층 바닥 3.2): 계단 구멍(x SX0..SX1, y SY0..YB)만 비움 ---
for x0, x1, y0, y1 in ((-XI, XI, YI, SY0), (-XI, SX0, SY0, YB), (SX1, XI, SY0, YB)):
    hface(SHELL, x0, x1, y0, y1, H2, 'boards'); hface(SHELL, x0, x1, y0, y1, C1, 'ceil', up=False)
face(SHELL, [(SX1, SY0, C1), (SX0, SY0, C1), (SX0, SY0, H2), (SX1, SY0, H2)], 'ceil')   # 구멍 가장자리 3면
face(SHELL, [(SX0, SY0, C1), (SX0, YB, C1), (SX0, YB, H2), (SX0, SY0, H2)], 'ceil')
face(SHELL, [(SX1, YB, C1), (SX1, SY0, C1), (SX1, SY0, H2), (SX1, YB, H2)], 'ceil')
hface(SHELL, -XI, XI, YI, YB, H2C, 'ceil', up=False)                                   # 2층 천장
# --- 2층 칸막이 10cm(0133 도현 "방 마다 문 만들어두지 말고 2층 전체를 방으로 / 부모님 방 · 주인공 방 2개"): 문짝·복도 없이 방 2개.
#   주인공 방 = q1_room 3.5×3.0 상자(서쪽 벽 x .55..0.65 · 뒷벽 y .15..0.25 · 동쪽 = 집 벽) · 뒷벽에 문 없는 출입구 구멍 1(방 로컬 x .6..1.4 = 옛 방 문 자리)
#   부모님 방 = 그 서쪽 전부(앞 y -2.85..0.15 은 칸막이 안, 그 뒤는 계단 쪽으로 트임 — 계단 홀 겸 침실 안쪽)
RDN = (DOX + .6, DOX + 1.4, H2, H2 + 2.0)
hwall(SHELL, (PW, YI), (PW, PB0), H2, H2C, (), lambda c, z: rw(0, z), (), (RW,))                  # 서쪽 벽(방 쪽, +x 를 봄)
hwall(SHELL, (DIVX, PB1), (DIVX, YI), H2, H2C)                                                  # 같은 칸막이 부모님 쪽(−x 를 봄) + 뒷벽 끝면
hwall(SHELL, (PW, PB0), (XI, PB0), H2, H2C, [RDN], lambda c, z: rw(0, z), (), (RW,))              # 뒷벽 방 쪽(−y 를 봄)
hwall(SHELL, (XI, PB1), (DIVX, PB1), H2, H2C, [RDN])                                            # 뒷벽 계단 홀 쪽(+y 를 봄)
x0, x1, z0, z1 = RDN                                                                            # 출입구 안쪽 면(문틀 없이 흰 면 3)
face(SHELL, [(x0, PB0, z0), (x0, PB1, z0), (x0, PB1, z1), (x0, PB0, z1)], 'trim'); face(SHELL, [(x1, PB1, z0), (x1, PB0, z0), (x1, PB0, z1), (x1, PB1, z1)], 'trim')
hface(SHELL, x0, x1, PB0, PB1, z1, 'trim', up=False)
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
pendant('PENDANT_03', -1.9, -.6, H2C, .55)
# 2층 방(ROOM_2F)의 전등은 Unity 에서 0.25m 올림(천장 2.4 → 2.65, library_import.cs)

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
                ('STUDIO_Cam_hall2f', (3.85, 1.6, H2 + 1.55)), ('STUDIO_Cam_hall2fLook', (-3.6, -.2, H2 + .8)),   # 2층 도착 → 부모님 방(침대·옷장) 트임
                ('STUDIO_Cam_room2f', (3.4, .5, H2 + 1.5)), ('STUDIO_Cam_room2fLook', (2.2, -1.5, H2 + .5))):  # 문 없는 출입구 → 주인공 방
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
