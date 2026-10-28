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
# 현관 문틀 안쪽 면(벽 두께 15cm 중 문짝 앞까지)
DY = FY + .13
face(SHELL, [(DX0, FY, 0), (DX0, DY, 0), (DX0, DY, DZ), (DX0, FY, DZ)], 'trim')
face(SHELL, [(DX1, DY, 0), (DX1, FY, 0), (DX1, FY, DZ), (DX1, DY, DZ)], 'trim')
face(SHELL, [(DX0, DY, DZ), (DX1, DY, DZ), (DX1, FY, DZ), (DX0, FY, DZ)], 'trim')
def inset(x0, x1, z0, z1):  # 창 = 15cm 파인 흰 면 4 + 안쪽 유리(어두운 방 + 창살 십자)
    yb = FY + .15
    face(SHELL, [(x0, FY, z0), (x1, FY, z0), (x1, yb, z0), (x0, yb, z0)], 'trim'); face(SHELL, [(x0, yb, z1), (x1, yb, z1), (x1, FY, z1), (x0, FY, z1)], 'trim')
    face(SHELL, [(x0, FY, z0), (x0, yb, z0), (x0, yb, z1), (x0, FY, z1)], 'trim'); face(SHELL, [(x1, yb, z0), (x1, FY, z0), (x1, FY, z1), (x1, yb, z1)], 'trim')
    face(SHELL, [(x0, yb, z0), (x1, yb, z0), (x1, yb, z1), (x0, yb, z1)], 'pane')
for w_ in (WL, WK, W2L, W2R): inset(*w_)
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
box(SHELL, -WX - .08, WX + .08, TY, BY + .08, -.9, 0, 'stone', ('bottom',))
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
# ---------- 발코니(방 폭 3.5m = 방 문 앞, 돌판 15cm · 까치발 2) — 1층 현관 차양을 겸함 ----------
BX = 1.75; BYF = FY - .9
box(SHELL, -BX, BX, BYF, FY, H2 - .15, H2, {'*': 'trim', 'top': 'pavers'}, ('back',))
for cx in (-1.45, 1.45): box(SHELL, cx - .08, cx + .08, BYF + .35, FY, H2 - .45, H2 - .15, 'trim', ('back', 'top'))
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
x0, x1, z0, z1 = WK; yb = FY + .15
box('CURTAIN_01', x0 + .02, x0 + .40, yb - .04, yb - .02, z0 + .02, z1, 'curtain', ('top', 'back', 'left', 'right', 'bottom'))  # 앞면 한 장(2cm 옆면 2px 칸이 검사 칸에서 빈자리와 섞였다)
box('CURTAIN_02', x1 - .40, x1 - .02, yb - .04, yb - .02, z0 + .02, z1, 'curtain', ('top', 'back', 'left', 'right', 'bottom'))  # 앞면 한 장(2cm 옆면 2px 칸이 검사 칸에서 빈자리와 섞였다)
cx = (x0 + x1) / 2
box('PLANTER_03', cx - .08, cx + .08, FY + .02, FY + .10, z0, z0 + .12, {'*': 'terracotta', 'top': 'soil'}, ('bottom',))
for dx, h, w in ((-.04, .14, .07), (.04, .18, .08)): box('PLANT_03', cx + dx - w / 2, cx + dx + w / 2, FY + .04, FY + .08, z0 + .12, z0 + .12 + h, 'leaf', ('bottom',))  # 잎 둘 5mm 떨어짐
box('PLANT_03', cx - .015, cx + .015, FY + .02, FY + .04, z0 + .22, z0 + .26, 'flower', ('back',))
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
    for kk in range(5):
        y = BYF + .2 + kk * .12
        box(RL, x0 + .02, x1 - .02, y - .01, y + .01, z0 + .12, zt, 'iron', ('top', 'bottom')); n += 1
# 화분 2(테라코타 + 흙 + 잎·꽃) — 문 양옆(문 앞 길 비움). 잎 바닥 = 흙 윗면 → skip
for nm_, pl_, cx in (('PLANTER_01', 'PLANT_01', -1.25), ('PLANTER_02', 'PLANT_02', 1.25)):  # 화분 / 식물(잎·꽃) 낱개 따로
    box(nm_, cx - .25, cx + .25, BYF + .13, BYF + .35, H2, H2 + .24, {'*': 'terracotta', 'top': 'soil'}, ('bottom',))
    for dx, dy, h, w in ((-.15, -.05, .22, .14), (.0, .03, .30, .16), (.14, -.04, .20, .13)):
        lf = BYF + .24 + dy - .05  # 잎 앞면 → 꽃은 그 앞 2cm(뒷면 = 잎 앞면 → skip)
        box(pl_, cx + dx - w / 2, cx + dx + w / 2, lf, lf + .10, H2 + .24, H2 + .24 + h, 'leaf', ('bottom',))
        box(pl_, cx + dx - .03, cx + dx + .03, lf - .02, lf, H2 + .24 + h - .10, H2 + .24 + h - .04, 'flower', ('back',))

# ---------- UV 칸 배치(선반 쌓기) → 메시 ----------
def local2d(pts):
    nrm = Vector()
    for i in range(len(pts)): a, b = pts[i], pts[(i + 1) % len(pts)]; nrm += Vector(((a.y - b.y) * (a.z + b.z), (a.z - b.z) * (a.x + b.x), (a.x - b.x) * (a.y + b.y)))
    nrm.normalize(); u = (pts[1] - pts[0]).normalized(); v = nrm.cross(u)
    return [((p - pts[0]).dot(u), (p - pts[0]).dot(v)) for p in pts]
AW = 1024; items = []
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
mat = bpy.data.materials.new('HOUSE_alb')
obs = []; tris = 0
for name, fl in objs.items():
    me = bpy.data.meshes.new(name); bm = bmesh.new(); uvl = bm.loops.layers.uv.new('UVMap')
    allp = [p for pts, _ in fl for p in pts]
    pv = Vector() if name == SHELL else PIVOT[name] if name in PIVOT else Vector(((min(p.x for p in allp) + max(p.x for p in allp)) / 2, (min(p.y for p in allp) + max(p.y for p in allp)) / 2, min(p.z for p in allp)))
    for pts, uvs in fl:
        f = bm.faces.new([bm.verts.new(p - pv) for p in pts])
        for lp, uv in zip(f.loops, uvs): lp[uvl].uv = uv
    bm.to_mesh(me); bm.free(); ob = bpy.data.objects.new(name, me); ob.location = pv; sc.collection.objects.link(ob); me.materials.append(mat); obs.append(ob)
    tris += sum(len(p.vertices) - 2 for p in me.polygons)
# 캠: 발코니 문(방 안 문턱 뒤 눈높이 → 열린 문·발코니 너머 바깥) · 1층 정면 근접(테라스 앞 → 현관·부엌 창·의자)
for en, loc in (('STUDIO_CamBalc', (-.15, FY + .75, H2 + 1.5)), ('STUDIO_CamBalcLook', (.25, FY - 8.0, H2 - .7)),
                ('STUDIO_CamDoor1', (.9, FY - 4.6, 1.55)), ('STUDIO_CamDoor1Look', (.9, FY, 1.15))):
    e = bpy.data.objects.new(en, None); e.location = loc; sc.collection.objects.link(e); obs.append(e)
bpy.ops.object.select_all(action='DESELECT')
for ob in obs: ob.select_set(True)
bpy.context.view_layer.objects.active = obs[0]
bpy.ops.export_scene.fbx(filepath=OUT + 'house_home.fbx', use_selection=True, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
                         path_mode='STRIP', embed_textures=False, mesh_smooth_type='FACE', object_types={'MESH', 'EMPTY'})
lay = dict(atlas=[AW, AH], cells=[dict(obj=t['obj'], pt=t['pt'], dens=t['dens'], rect=[t['x'], t['y'], t['w'], t['h']], m=[round(t['m'][0], 3), round(t['m'][1], 3)], seed=t['i'])
                                  for t in items])
json.dump(lay, open(OUT + 'layout.json', 'w', encoding='utf-8'), indent=0)
meas = dict(tris=tris, objects=[o.name for o in obs], atlas=[AW, AH], faces=len(FACES), balusters=n, size_m=dict(w=2 * WX, d=BY - FY, wall_top=WT, ridge=round(zu(0) + RT + .07, 2)),
            room_offset_unity=[0, H2, round(-RY, 3)], pivot='기단 윗면(z 0) 가운데(방 가운데는 앞으로 1.35m)', forward='발코니·현관 = Blender -Y → FBX/Unity +Z')
json.dump(meas, open(OUT + 'measure.json', 'w', encoding='utf-8'), indent=1, ensure_ascii=False); print('MEAS', meas, flush=True)
sys.stdout.flush(); os._exit(0)
