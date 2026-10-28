"""주인공 2층집 house_home — 과제 20261029-0031 (D-198·D-199). Blender bpy 절차 생성(형태·UV 칸만, 텍스처는 build_asset.py house_home grain):
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/house_home.py
형태: 바깥 4.4×3.3m, 1층 3.2m + 2층 = 주인공 방(art/export/q1-room, 프리팹 자식으로 Unity 에서 넣음, 바닥 z 3.2) + 박공 지붕(용마루 X축, 24°).
  비탈 집 기단(돌, 0.9m). 1층: 현관문 DOOR_01(파인 문틀) + 창 1(닫힌 덧창 SHUTTER_03·04). 2층: 방 창(구멍 x -1..1, z 4.0..5.2)에 흰 창틀 + 열린 덧창 SHUTTER_01·02,
  창 앞 발코니(돌판·까치발) + 난간 RAIL_01 + 화분 PLANTER_01·02. 발코니 출입 = 방의 반쯤 열린 미닫이 창(방은 손대지 않음).
축: 정면(발코니·현관) = Blender -Y → FBX/Unity +Z. 피벗 = 집 바닥(기단 윗면 z 0) 가운데 = 방 바닥 가운데의 수직 아래.
텍셀: 껍데기 32px/m, 낱개 물건은 면의 긴 변 ≤1.5m 면 64(check_rules 와 같은 규칙). 겹친 면 0: 붙는 면은 skip, 덧댄 판은 2~4cm 띄움.
출력: art/export/house-home/{house_home.fbx, layout.json(칸 표), measure.json}
"""
import bpy, bmesh, os, sys, json, math
from mathutils import Vector
R = 'D:/Github/F1Game/'; OUT = R + 'art/export/house-home/'; os.makedirs(OUT, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True); sc = bpy.context.scene

WX, FY, BY = 2.2, -1.65, 1.65          # 바깥벽 반폭 · 앞면 · 뒷면 (방 창 벽 바깥면 = FY, 방 안쪽은 x ±1.75, y ±1.5)
H2, WT = 3.2, 5.85                     # 2층 바닥(방 바닥) · 벽 윗선
PIT = math.tan(math.radians(24)); OVF, OVS, RT = .35, .25, .09   # 지붕 기울기 · 앞뒤 처마 · 옆 처마 · 지붕 두께
SHELL = 'HOUSE_shell'
FACES = []                              # (obj, [Vector], pt, dens)

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
def wall(side, a0, a1, z0, z1, pt='wall', obj=SHELL):  # 바깥에서 본 반시계(왼아래부터) → 법선 바깥, 텍스처 v = 위
    if side == 'F': face(obj, [(a0, FY, z0), (a1, FY, z0), (a1, FY, z1), (a0, FY, z1)], pt)
    if side == 'B': face(obj, [(a1, BY, z0), (a0, BY, z0), (a0, BY, z1), (a1, BY, z1)], pt)
    if side == 'L': face(obj, [(-WX, a1, z0), (-WX, a0, z0), (-WX, a0, z1), (-WX, a1, z1)], pt)
    if side == 'R': face(obj, [(WX, a0, z0), (WX, a1, z0), (WX, a1, z1), (WX, a0, z1)], pt)

# ---------- 벽 (앞: 현관 구멍 x -1.4..-0.5 z 0..2.15 · 방 창 구멍 x -1..1 z 4.0..5.2) ----------
DX0, DX1, DZ = -1.4, -.5, 2.15
wall('F', -WX, DX0, 0, DZ); wall('F', DX1, WX, 0, DZ); wall('F', -WX, WX, DZ, 4.0)
wall('F', -WX, -1., 4.0, 5.2); wall('F', 1., WX, 4.0, 5.2); wall('F', -WX, WX, 5.2, WT)
wall('B', -WX, WX, 0, WT); wall('L', FY, BY, 0, WT); wall('R', FY, BY, 0, WT)
zr = WT + PIT * BY                      # 박공 꼭짓점(지붕 밑면)
face(SHELL, [(-WX, BY, WT), (-WX, FY, WT), (-WX, 0, zr)], 'wall'); face(SHELL, [(WX, FY, WT), (WX, BY, WT), (WX, 0, zr)], 'wall')
# 현관 문틀 안쪽 면(벽 두께 15cm 중 문짝 앞까지)
face(SHELL, [(DX0, FY, 0), (DX0, -1.52, 0), (DX0, -1.52, DZ), (DX0, FY, DZ)], 'trim')
face(SHELL, [(DX1, -1.52, 0), (DX1, FY, 0), (DX1, FY, DZ), (DX1, -1.52, DZ)], 'trim')
face(SHELL, [(DX0, -1.52, DZ), (DX1, -1.52, DZ), (DX1, FY, DZ), (DX0, FY, DZ)], 'trim')
# 흰 틀(2cm 돋음): 옆은 위·아래·뒤 skip(위 띠·창턱과 같은 면 0)
def surround(x0, x1, z0, z1, sill=True, head=True):
    y0 = FY - .02
    box(SHELL, x0 - .12, x0, y0, FY, z0, z1, 'trim', ('back', 'top', 'bottom')); box(SHELL, x1, x1 + .12, y0, FY, z0, z1, 'trim', ('back', 'top', 'bottom'))
    if head: box(SHELL, x0 - .12, x1 + .12, y0, FY, z1, z1 + .12, 'trim', ('back',))
    if sill: box(SHELL, x0 - .18, x1 + .18, FY - .06, FY, z0 - .08, z0, 'trim', ('back',))
surround(DX0, DX1, 0, DZ, sill=False)
surround(-1., 1., 4.0, 5.2)
W1X0, W1X1, W1Z0, W1Z1 = .5, 1.6, .95, 2.15          # 1층 창(닫힌 덧창 — 벽은 통짜, 덧창이 2~5cm 앞)
surround(W1X0, W1X1, W1Z0, W1Z1)
# ---------- 기단(비탈, 돌) · 현관 디딤돌 ----------
box(SHELL, -WX - .08, WX + .08, FY - .08, BY + .08, -.9, 0, 'stone', ('bottom',))
box(SHELL, DX0 - .2, DX1 + .2, FY - .4, FY - .08, -.18, 0, 'stone', ('back', 'bottom'))
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
# ---------- 발코니(방 창 앞, 돌판 15cm · 까치발 2) ----------
BYF = -2.55; BX = 1.6
box(SHELL, -BX, BX, BYF, FY, H2 - .15, H2, {'*': 'trim', 'top': 'pavers'}, ('back',))
for cx in (-1.25, 1.25): box(SHELL, cx - .08, cx + .08, -2.2, FY, H2 - .45, H2 - .15, 'trim', ('back', 'top'))
# ---------- 낱개 물건 ----------
# 현관문: 문짝(문틀 안 1cm 틈) + 손잡이. 바닥 5mm 띄움(기단 윗면과 같은 면 0)
box('DOOR_01', DX0 + .01, DX1 - .01, -1.56, -1.52, .005, DZ - .01, 'door', ('bottom', 'back'))
box('DOOR_01', DX1 - .16, DX1 - .11, -1.60, -1.56, 1.0, 1.06, 'brass', ('back',))
# 덧창(C2 초록): 2층 = 활짝 열려 벽에 붙음(4cm 띄움) · 1층 = 닫힘(창턱 위에 섬)
box('SHUTTER_01', -2.10, -1.14, FY - .07, FY - .04, 4.0, 5.2, 'shutter', ('bottom',))
box('SHUTTER_02', 1.14, 2.10, FY - .07, FY - .04, 4.0, 5.2, 'shutter', ('bottom',))
box('SHUTTER_03', W1X0, 1.045, FY - .05, FY - .02, W1Z0, W1Z1, 'shutter', ('bottom',))
box('SHUTTER_04', 1.055, W1X1, FY - .05, FY - .02, W1Z0, W1Z1, 'shutter', ('bottom',))
# 난간(쇠, 높이 1m): 기둥 → 위·아래 가로대(기둥 사이 토막) → 살 2cm (12cm 간격). 맞닿는 면은 skip
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
for i in range(-13, 14):
    x = i * .115
    if abs(x) < .09 or abs(x) > BX - .12: continue
    box(RL, x - .01, x + .01, FY0 + .02, FY1 - .02, z0 + .12, zt, 'iron', ('top', 'bottom')); n += 1
for x0, x1 in SX:
    for y in (-2.35, -2.23, -2.11, -1.99, -1.87):
        box(RL, x0 + .02, x1 - .02, y - .01, y + .01, z0 + .12, zt, 'iron', ('top', 'bottom')); n += 1
# 화분 2(테라코타 + 흙 + 잎·꽃). 잎 바닥 = 흙 윗면 → skip
for nm_, pl_, cx in (('PLANTER_01', 'PLANT_01', -1.15), ('PLANTER_02', 'PLANT_02', 1.15)):  # 화분 / 식물(잎·꽃) 낱개 따로
    box(nm_, cx - .25, cx + .25, -2.42, -2.20, H2, H2 + .24, {'*': 'terracotta', 'top': 'soil'}, ('bottom',))
    for dx, dy, h, w in ((-.15, -.05, .22, .14), (.0, .03, .30, .16), (.14, -.04, .20, .13)):
        lf = -2.31 + dy - .05  # 잎 앞면 → 꽃은 그 앞 2cm(뒷면 = 잎 앞면 → skip)
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
    pv = Vector() if name == SHELL else Vector(((min(p.x for p in allp) + max(p.x for p in allp)) / 2, (min(p.y for p in allp) + max(p.y for p in allp)) / 2, min(p.z for p in allp)))
    for pts, uvs in fl:
        f = bm.faces.new([bm.verts.new(p - pv) for p in pts])
        for lp, uv in zip(f.loops, uvs): lp[uvl].uv = uv
    bm.to_mesh(me); bm.free(); ob = bpy.data.objects.new(name, me); ob.location = pv; sc.collection.objects.link(ob); me.materials.append(mat); obs.append(ob)
    tris += sum(len(p.vertices) - 2 for p in me.polygons)
# 발코니 캠(바깥 보기): 방 창 앞 발코니 위 눈높이 → 정면 아래 바다 쪽
for en, loc in (('STUDIO_CamBalc', (-.35, -1.95, H2 + 1.55)), ('STUDIO_CamBalcLook', (.8, -9.0, H2 - .6))):
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
            room_offset=[0, 0, H2], pivot='기단 윗면(z 0) 가운데 = 방 바닥 가운데 수직 아래', forward='발코니·현관 = Blender -Y → FBX/Unity +Z')
json.dump(meas, open(OUT + 'measure.json', 'w', encoding='utf-8'), indent=1, ensure_ascii=False); print('MEAS', meas, flush=True)
sys.stdout.flush(); os._exit(0)
