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
BD = (-.45, .45, H2, H2 + 2.05)                        # q1_room.py 창 벽 문 구멍(방 x ±.45, z 0..2.05)
W2L, W2R = (-3.4, -2.5, 4.0, 5.2), (2.5, 3.4, 4.0, 5.2)
HOLES = [(DX0, DX1, 0, DZ), WL, WK, BD, W2L, W2R]
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
for w_ in (WL, WK, W2L, W2R): inset(*w_)
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
for w_ in (WL, WK, W2L, W2R): surround(*w_)
# ---------- 기단(비탈, 돌) = 앞 테라스 1.2m(의자 자리) · 현관 계단 한 단 ----------
TY = FY - 1.2
box(SHELL, -WX - .08, WX + .08, TY, BY + .08, -.9, 0, 'stone', ('bottom', 'top'))
# 0351 윗면을 나눔: 바깥 테두리 = 돌 · 집 안 1층 바닥 = 테라코타 타일, 현관 앞 1.1m = 돌(신발 벗는 자리)
for a, b, c, d in ((-WX - .08, WX + .08, TY, FY), (-WX - .08, WX + .08, BY, BY + .08), (-WX - .08, -WX, FY, BY), (WX, WX + .08, FY, BY)):
    face(SHELL, [(a, c, 0), (b, c, 0), (b, d, 0), (a, d, 0)], 'stone')
EX = .9; EYB = FY + 1.1
for a, b, c, d, p in ((-WX, -EX, FY, BY, 'pavers'), (EX, WX, FY, BY, 'pavers'), (-EX, EX, EYB, BY, 'pavers'), (-EX, EX, FY, EYB, 'stone')):
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
BX = 1.85; BYF = FY - 1.8                # 폭 = 3.7(2층 창 덧창 x 1.91 과 6cm 떨어짐)
box(SHELL, -BX, BX, BYF, FY, H2 - .15, H2, {'*': 'trim', 'top': 'pavers'}, ('back',))
for cx in (-1.55, 1.55): box(SHELL, cx - .08, cx + .08, FY - 1.2, FY, H2 - .55, H2 - .15, 'trim', ('back', 'top'))
# ---------- 낱개 물건 ----------
# 현관문: 문짝(문틀 안 1cm 틈) + 손잡이. 바닥 5mm 띄움(기단 윗면과 같은 면 0)
box('DOOR_01', DX0 + .01, DX1 - .01, DY - .04, DY, .005, DZ - .01, 'door', ('bottom', 'back'))
box('DOOR_01', DX1 - .16, DX1 - .11, DY - .08, DY - .04, 1.0, 1.06, 'brass', ('back',))
# 발코니 문(D-200): 방 안쪽으로 80° 열림. 경첩 = 문 구멍 오른쪽(x .43), 방 안 문틀(방 y -1.47) 바로 안. 피벗 = 경첩 아래
HX, HY, OA, LW = .43, FY + .19, math.radians(80), .86
dd = Vector((-math.cos(OA), math.sin(OA), 0)); nn = Vector((dd.y, -dd.x, 0))          # 닫힌 방향 -X 를 방 안(+Y)으로 80°, 두께는 경첩 바깥쪽
box_o = Vector((HX, HY, 0)) - nn * .04
obox('DOOR_BALCONY', box_o, dd, LW, nn, .04, H2 + .005, H2 + 2.03, 'door')
kb = box_o + dd * (LW - .1) - nn * .04                                                 # 손잡이: 문 안쪽 면(n0)에 붙음 → 붙는 면(n1) skip
obox('DOOR_BALCONY', kb, dd, .05, nn, .04, H2 + 1.0, H2 + 1.05, 'brass', ('n1',))
PIVOT['DOOR_BALCONY'] = Vector((HX, HY, H2 + .005))
# 덧창(C2 초록): 거실 창·2층 창 2 = 활짝 열려 벽에 붙음(4cm 띄움). 부엌 창은 덧창 대신 커튼
k = 0
for x0, x1, z0, z1 in (WL, W2L, W2R):
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
# 난간(쇠, 높이 1m): 기둥 → 위·아래 가로대(기둥 사이 토막) → 살 2cm (11.5cm 간격). 맞닿는 면은 skip
RL, z0, zt = 'RAIL_01', H2, H2 + .95
FY0, FY1 = BYF + .02, BYF + .08                 # 앞 가로대 y
SX = [(-BX, -BX + .06), (BX - .06, BX)]          # 옆 가로대 x
posts = [(-BX, -BX + .06, FY0, FY1), (-.03, .03, FY0, FY1), (BX - .06, BX, FY0, FY1), (-BX, -BX + .06, FY - .08, FY - .02), (BX - .06, BX, FY - .08, FY - .02)]
for x0, x1, y0, y1 in posts: box(RL, x0, x1, y0, y1, z0, zt, 'iron', ('bottom', 'top'))
box(RL, -BX, BX, FY0, FY1, zt, zt + .05, 'iron')                                  # 앞 손잡이
for x0, x1 in SX: box(RL, x0, x1, FY1, FY - .02, zt, zt + .05, 'iron', ('front',))  # 옆 손잡이(앞 끝 = 앞 손잡이 뒷면 → skip)
for xa, xb in ((-BX + .06, -.03), (.03, BX - .06)): box(RL, xa, xb, FY0 + .01, FY1 - .01, z0 + .08, z0 + .12, 'iron', ('left', 'right'))
for x0, x1 in SX: box(RL, x0 + .01, x1 - .01, FY1, FY - .08, z0 + .08, z0 + .12, 'iron', ('front', 'back'))
n = 0
for i in range(-16, 17):
    x = i * .115
    if abs(x) < .09 or abs(x) > BX - .12: continue
    box(RL, x - .01, x + .01, FY0 + .02, FY1 - .02, z0 + .12, zt, 'iron', ('top', 'bottom')); n += 1
for x0, x1 in SX:
    for kk in range(13):  # 0351: 깊이 1.8m → 12cm 간격 13개
        y = BYF + .2 + kk * .12
        box(RL, x0 + .02, x1 - .02, y - .01, y + .01, z0 + .12, zt, 'iron', ('top', 'bottom')); n += 1
# 화분 2(테라코타 + 흙 + 잎·꽃) — 문 양옆(문 앞 길 비움). 잎 바닥 = 흙 윗면 → skip
for nm_, pl_, cx in (('PLANTER_01', 'PLANT_01', -1.25), ('PLANTER_02', 'PLANT_02', 1.25)):  # 화분 / 식물(잎·꽃) 낱개 따로
    box(nm_, cx - .25, cx + .25, BYF + .13, BYF + .35, H2, H2 + .24, {'*': 'terracotta', 'top': 'soil'}, ('bottom',))
    for dx, dy, h, w in ((-.15, -.05, .22, .14), (.0, .03, .30, .16), (.14, -.04, .20, .13)):
        lf = BYF + .24 + dy - .05  # 잎 앞면 → 꽃은 그 앞 2cm(뒷면 = 잎 앞면 → skip)
        box(pl_, cx + dx - w / 2, cx + dx + w / 2, lf, lf + .10, H2 + .24, H2 + .24 + h, 'leaf', ('bottom',))
        box(pl_, cx + dx - .03, cx + dx + .03, lf - .02, lf, H2 + .24 + h - .10, H2 + .24 + h - .04, 'flower', ('back',))

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
def roomwall(c, z, xr=True): return ('wainlow' if z < RW else 'rwall') if (abs(c) < RX if xr else True) else 'plaster'
# --- 바깥벽 안쪽 면(1층 0~2.8 · 2층 3.2~5.85), 앞벽은 구멍 그대로 ---
hwall(SHELL, (XI, YI), (-XI, YI), 0, C1, [(DX0, DX1, 0, DZ), WL, WK])
hwall(SHELL, (-XI, YB), (XI, YB), 0, C1); hwall(SHELL, (-XI, YI), (-XI, YB), 0, C1); hwall(SHELL, (XI, YB), (XI, YI), 0, C1)
hwall(SHELL, (XI, YI), (-XI, YI), H2, H2C, [BD, W2L, W2R], roomwall, (-RX - .1, -RX, RX, RX + .1), (RW,))
hwall(SHELL, (-XI, YB), (XI, YB), H2, H2C); hwall(SHELL, (-XI, YI), (-XI, YB), H2, H2C); hwall(SHELL, (XI, YB), (XI, YI), H2, H2C)
# --- 슬래브(1층 천장 2.8 / 2층 바닥 3.2): 계단 구멍(x SX0..SX1, y SY0..YB)만 비움 ---
for x0, x1, y0, y1 in ((-XI, XI, YI, SY0), (-XI, SX0, SY0, YB), (SX1, XI, SY0, YB)):
    hface(SHELL, x0, x1, y0, y1, H2, 'boards'); hface(SHELL, x0, x1, y0, y1, C1, 'ceil', up=False)
face(SHELL, [(SX1, SY0, C1), (SX0, SY0, C1), (SX0, SY0, H2), (SX1, SY0, H2)], 'ceil')   # 구멍 가장자리 3면
face(SHELL, [(SX0, SY0, C1), (SX0, YB, C1), (SX0, YB, H2), (SX0, SY0, H2)], 'ceil')
face(SHELL, [(SX1, YB, C1), (SX1, SY0, C1), (SX1, SY0, H2), (SX1, YB, H2)], 'ceil')
hface(SHELL, -XI, XI, YI, YB, H2C, 'ceil', up=False)                                   # 2층 천장
# --- 2층 칸막이 10cm: 방 왼·오른 벽(x ±1.75..1.85) + 복도 벽(y .15..0.25, 문 3: 엄마 방 · 주인공 방 · 욕실) ---
MD, RD, BTD = (-3.3, -2.5, H2, H2 + 2.0), (.6, 1.4, H2, H2 + 2.0), (2.6, 3.4, H2, H2 + 2.0)   # 방 문 = q1_room 문틀 x .6..1.4, 높이 2.0
hwall(SHELL, (-RX, YI), (-RX, PB0), H2, H2C, (), lambda c, z: roomwall(0, z), (), (RW,)); hwall(SHELL, (-RX - .1, PB0), (-RX - .1, YI), H2, H2C)
hwall(SHELL, (RX, PB0), (RX, YI), H2, H2C, (), lambda c, z: roomwall(0, z), (), (RW,)); hwall(SHELL, (RX + .1, YI), (RX + .1, PB0), H2, H2C)
hwall(SHELL, (-XI, PB0), (XI, PB0), H2, H2C, [MD, RD, BTD], roomwall, (-RX - .1, -RX, RX, RX + .1), (RW,))
hwall(SHELL, (XI, PB1), (-XI, PB1), H2, H2C, [MD, RD, BTD])
for x0, x1, z0, z1 in (MD, RD, BTD):  # 문 구멍 안쪽 면 + 문틀(복도 쪽 · 방 쪽은 주인공 방만) + 닫힌 문짝 + 손잡이 양쪽
    face(SHELL, [(x0, PB0, z0), (x0, PB1, z0), (x0, PB1, z1), (x0, PB0, z1)], 'trim'); face(SHELL, [(x1, PB1, z0), (x1, PB0, z0), (x1, PB0, z1), (x1, PB1, z1)], 'trim')
    hface(SHELL, x0, x1, PB0, PB1, z1, 'trim', up=False)
    for ya, yb_, sk in ((PB1, PB1 + .03, 'front'), (PB0 - .03, PB0, 'back')) if x0 == RD[0] else ((PB1, PB1 + .03, 'front'),):
        box(SHELL, x0 - .06, x0, ya, yb_, z0, z1, 'chair', (sk, 'top', 'bottom')); box(SHELL, x1, x1 + .06, ya, yb_, z0, z1, 'chair', (sk, 'top', 'bottom'))
        box(SHELL, x0 - .06, x1 + .06, ya, yb_, z1, z1 + .06, 'chair', (sk,))
    dn = {MD[0]: 'DOOR_MOM', RD[0]: 'DOOR_ROOM', BTD[0]: 'DOOR_BATH'}[x0]
    ly0 = PB0 if x0 == RD[0] else PB0 + .03            # 방 문짝 = 방 쪽 벽면과 같은 줄(q1_room 손잡이 KNOB_01 이 y .15 에 붙음)
    box(dn, x0 + .005, x1 - .005, ly0, ly0 + .04, z0 + .005, z1 - .005, 'door')
    box(dn, x0 + .06, x0 + .1, ly0 + .04, ly0 + .08, z0 + .95, z0 + .99, 'brass', ('front',))
    if x0 != RD[0]: box(dn, x0 + .06, x0 + .1, ly0 - .04, ly0, z0 + .95, z0 + .99, 'brass', ('back',))
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
    box(nm_, x0, x1, y0, y1, .42, .46, 'chair'); box(nm_, x0, x1, yb0 + .01, yb0 + .03, .68, .84, 'chair')
# 거실(왼쪽): 소파(왼벽 5cm 앞) · 낮은 탁자 · 엄마 바느질 바구니
box('SOFA', -4.1, -3.3, -1.6, -1.45, 0, .62, 'sofa', ('bottom',)); box('SOFA', -4.1, -3.3, .25, .4, 0, .62, 'sofa', ('bottom',))          # 팔걸이 2
box('SOFA', -4.1, -3.3, -1.45, .25, 0, .42, 'sofa', ('bottom', 'front', 'back'))                                                       # 바닥 몸
box('SOFA', -4.1, -3.85, -1.45, .25, .42, .85, 'sofa', ('bottom', 'front', 'back'))                                                   # 등받이
box('SOFA', -3.84, -3.32, -1.44, .24, .42, .52, 'cushion', ('bottom',))                                                               # 앉는 방석
box('TABLE_LIVING', -2.9, -2.3, -1.0, -.2, .36, .4, 'chair')
for lx, ly in ((-2.88, -.98), (-2.36, -.98), (-2.88, -.26), (-2.36, -.26)): box('TABLE_LIVING', lx, lx + .04, ly, ly + .04, 0, .36, 'chair', ('bottom', 'top'))
box('BASKET_01', -3.95, -3.6, .55, .85, 0, .22, 'basket', ('bottom',)); box('BASKET_01', -3.82, -3.74, .66, .74, .22, .29, 'flower', ('bottom',))  # 엄마 바느질 바구니 + 털실
# 현관: 신발장 · 발매트(1cm 위 한 장)
box('SHOERACK', -1.55, -.8, YI + .03, YI + .33, 0, .45, 'chair', ('bottom', 'front'))
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
    items.append(dict(i=i, obj=obj, pt=pt, dens=d, l2=[(a - u0, b - v0) for a, b in l2], m=(um, vm), w=max(2, round(um * d)), h=max(2, round(vm * d))))
# 껍데기는 키 순, 낱개는 물건·칠 종류끼리 모아 새 줄부터(작은 면이 남의 칸 사이에 흩어지면 0.25m 검사 칸에 색 경계가 섞인다, check 2차)
order = sorted(items, key=lambda t: (t['obj'] != SHELL, t['obj'] if t['obj'] != SHELL else '', t['pt'] if t['obj'] != SHELL else '', -t['h'], -t['w'])); x = y = rowh = 0; grp = None
for t in order:
    g = (t['obj'], t['pt']) if t['obj'] != SHELL else None
    if x + t['w'] > AW or (g != grp and g is not None and x > 0 and t['obj'] != (grp or ('',))[0]):
        x, y, rowh = 0, y + rowh + 1, 0
        if g is not None: y = -(-y // 16) * 16  # 낱개 줄 시작 = 16px(0.25m@64) 검사 격자에 맞춤(어긋나면 검사 칸이 이웃 줄을 먹는다, check 3차)
    grp = g
    t['x'], t['y'] = x, y; x += t['w'] + 1; rowh = max(rowh, t['h'])
AH = 1 << math.ceil(math.log2(y + rowh + 1))
objs = {}
for t in items:
    obj, pts = FACES[t['i']][0], FACES[t['i']][1]
    objs.setdefault(obj, []).append((pts, [((t['x'] + a * t['w'] / max(t['m'][0], 1e-6)) / AW, (t['y'] + b * t['h'] / max(t['m'][1], 1e-6)) / AH) for a, b in t['l2']]))
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
                ('STUDIO_CamDoor1', (.9, FY - 4.6, 1.55)), ('STUDIO_CamDoor1Look', (.9, FY, 1.15)),
                ('STUDIO_Cam_living', (3.3, YI + .35, 1.6)), ('STUDIO_Cam_livingLook', (-3.2, 1.2, .7)),          # 0351: 부엌 앞 구석 → 거실·식탁·계단
                ('STUDIO_Cam_stairs', (-2.6, .2, 1.55)), ('STUDIO_Cam_stairsLook', (2.2, 2.35, 2.3)),             # 거실 → 계단 올라감
                ('STUDIO_Cam_hall2f', (3.85, 1.6, H2 + 1.55)), ('STUDIO_Cam_hall2fLook', (-3.6, .6, H2 + 1.0))):  # 2층 도착 → 복도·방 문·엄마 방 문
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
