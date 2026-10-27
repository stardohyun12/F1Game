"""에셋 루프 #1 MART — 항구 앞 동네 마트 (과제 20261028-0236). Blender bpy 절차 생성:
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/q1_mart.py
형태: 9×7m 회벽 2층(평지붕+난간, 지붕 위 계단실), 1층 큰 진열창+유리문, 줄무늬 차양, 간판 "MART"(글자 0.875m·획 4텍셀),
      앞 진열대(과일·계란 상자), 오른쪽 자판기, 옆벽 B1 사진 셔터. 앞면 = Blender -Y (집 j판과 같은 축 → FBX 앞 +Z), 피벗 = 바닥 앞면 가운데.
텍스처: albedo(빛 없음, D-185) 32텍셀/m 아틀라스 → palette50(=palette46 + 마트 칠 4칸) 양자화(디더 없음) → 외톨이 텍셀 이웃 최빈값.
출력: art/export/q1-mart/{mart.fbx, mart.png, palette50.gpl, measure.json} · 게이트 렌더 art/_review/2026-10-28-q1-mart/{front,q34,game_raw}.png
"""
import bpy, bmesh, os, json, math
import numpy as np
from mathutils import Vector
R = 'D:/Github/F1Game/'; OUT = R + 'art/export/q1-mart/'; RV = R + 'art/_review/2026-10-28-q1-mart/'
for d in (OUT, RV): os.makedirs(d, exist_ok=True)
DENS = 32
bpy.ops.wm.read_factory_settings(use_empty=True); sc = bpy.context.scene

# ---------- 팔레트 ----------
PAL = []
for ln in open(R + 'art/export/scene-a4-houses-alb/palette46.gpl', encoding='utf-8'):
    p = ln.split()
    if len(p) >= 3 and all(t.isdigit() for t in p[:3]): PAL.append((tuple(int(t) for t in p[:3]), ' '.join(p[3:])))
NEW = [((186, 78, 60), 'mart 차양 빨강'), ((146, 50, 42), 'mart 간판 글자'), ((214, 118, 52), 'mart 자판기 주황'), ((226, 192, 78), 'mart 과일 노랑')]
PAL += NEW
with open(OUT + 'palette50.gpl', 'w', encoding='utf-8') as f:
    f.write('GIMP Palette\nName: q1-mart 50 (houses palette46 + mart 4)\nColumns: 8\n#\n')
    for c, n in PAL: f.write('%3d %3d %3d\t%s\n' % (*c, n))
P = np.array([c for c, _ in PAL], float)
WALL = (244, 235, 200); WALL2 = (231, 185, 140); STONE = (154, 164, 89)
C = dict(wall=WALL, patch=(236, 202, 140), stone=(138, 142, 86), joint=(112, 135, 110), glass=(52, 70, 68), glass2=(62, 148, 171),
         frame=(207, 209, 182), shelf=(161, 109, 66), red=NEW[0][0], ink=NEW[1][0], orange=NEW[2][0], yellow=NEW[3][0], cream=(244, 235, 200),
         green=(100, 140, 98), green2=(80, 118, 80), wood=(177, 148, 106), wood2=(158, 127, 78), dark=(50, 49, 43), roof=(169, 158, 108),
         soffit=(131, 134, 110), metal=(146, 148, 91), blue=(112, 120, 166), egg=(236, 202, 140), tray=(174, 166, 106), leaf=(150, 158, 87))
GREY_M = (138, 142, 86)

# ---------- 칠하기 (A[v,u], v=0 아래) ----------
def T(m): return int(round(m * DENS))
def rect(A, x0, x1, z0, z1, col):
    h, w = A.shape[:2]; A[max(T(z0), 0):min(T(z1), h), max(T(x0), 0):min(T(x1), w)] = col
def grain(A, amt, seed, cell=6):
    rs = np.random.default_rng(seed); h, w = A.shape[:2]
    g = rs.uniform(-amt, amt, (h // cell + 2, w // cell + 2)); g = np.kron(g, np.ones((cell, cell)))[:h, :w]
    A *= (1 + g)[..., None]
def wallbase(A, seed, plinth=True):
    A[:] = WALL; grain(A, .03, seed)
    if plinth:
        rect(A, 0, 99, 0, .45, C['stone']); h, w = A.shape[:2]
        for u in range(0, w, 13): A[:T(.45), u] = C['joint']
        A[T(.22), :] = C['joint']; A[T(.45):T(.45) + 1, :] = C['soffit']
    A[T(.45) + 1:T(.45) + 6] *= .97  # 튄 빗물 자리(아래만, 약하게)

def window(A, x0, x1, z0, z1, shut=None, bars=1):
    rect(A, x0 - .07, x1 + .07, z0 - .1, z1 + .07, C['frame']); rect(A, x0, x1, z0, z1, C['glass'])
    rect(A, x0, x1, z0 + (z1 - z0) * .55, z0 + (z1 - z0) * .55 + .05, C['frame'])
    for i in range(1, bars + 1): xm = x0 + (x1 - x0) * i / (bars + 1); rect(A, xm - .03, xm + .03, z0, z1, C['frame'])
    k = int(x0 * 7 + z0 * 3) % 3; rect(A, x0 + .06 + .1 * k, x0 + .22 + .12 * k, z1 - .3 - .08 * k, z1 - .08, C['glass2'])  # 하늘 비침(창마다 다르게)
    if shut:
        sw = (x1 - x0) / 2 + .02
        for a, b in ((x0 - .07 - sw, x0 - .07), (x1 + .07, x1 + .07 + sw)):
            rect(A, a, b, z0 - .05, z1 + .05, C[shut])
            for z in np.arange(z0 + .02, z1, .1): rect(A, a + .04, b - .04, z, z + .035, C[shut + '2'] if shut + '2' in C else C['dark'])

def p_front(A):
    wallbase(A, 1)
    # 1층 진열창 (x -4.1..1.4, 벽 u = x + 4.5)
    o = 4.5; rect(A, -4.25 + o, 1.55 + o, .45, 2.72, C['frame']); rect(A, -4.1 + o, 1.4 + o, .55, 2.6, C['glass'])
    for zs in (.95, 1.45, 1.95):  # 안쪽 선반 + 물건
        rect(A, -4.1 + o, 1.4 + o, zs - .06, zs, C['shelf']); rs = np.random.default_rng(int(zs * 100)); x = -4.0
        while x < 1.25:
            ww = rs.choice([.1, .13, .16]); hh = rs.choice([.13, .19, .25]); col = [C['red'], C['yellow'], C['orange'], C['cream'], C['green'], C['blue'], C['cream']][rs.integers(7)]
            rect(A, x + o, x + ww + o, zs, zs + hh, col); x += ww + .03
    for xm in (-2.25, -.4): rect(A, xm - .05 + o, xm + .05 + o, .55, 2.6, C['frame'])
    rect(A, -4.1 + o, 1.4 + o, 2.18, 2.24, C['frame'])
    rect(A, -3.9 + o, -3.35 + o, 1.5, 2.1, C['cream']); rect(A, -3.85 + o, -3.4 + o, 1.9, 2.0, C['red']); rect(A, -3.85 + o, -3.4 + o, 1.6, 1.66, C['dark'])  # 붙인 전단
    rect(A, .55 + o, 1.25 + o, 1.55, 2.05, C['cream']); rect(A, .6 + o, 1.2 + o, 1.85, 1.98, C['ink'])
    # 유리문 x 1.85..3.55
    rect(A, 1.75 + o, 3.65 + o, 0, 2.62, C['frame']); rect(A, 1.88 + o, 2.66 + o, .08, 2.5, C['glass']); rect(A, 2.74 + o, 3.52 + o, .08, 2.5, C['glass'])
    rect(A, 1.95 + o, 3.45 + o, 1.05, 1.1, C['metal']); rect(A, 2.0 + o, 2.4 + o, 2.1, 2.4, C['glass2'])
    rect(A, 3.8 + o, 4.35 + o, 1.2, 1.9, C['wood2']); rect(A, 3.85 + o, 4.3 + o, 1.25, 1.85, C['cream'])  # 게시판
    # 층 사이 줄눈 + 2층
    rect(A, 0, 99, 4.3, 4.34, C['patch'])
    window(A, -3.2 + o, -2.1 + o, 4.62, 5.8, 'green'); window(A, .9 + o, 2.0 + o, 4.62, 5.8, 'green')
    window(A, 3.45 + o, 4.0 + o, 4.95, 5.6, None, 0)  # 작은 창(화장실)
    rect(A, -1.4 + o, -.4 + o, 4.45, 5.2, (236, 226, 192))  # 보수 칠 자국
    rect(A, 0, 99, 6.0, 9, C['wall'])
def p_side(A, right):
    wallbase(A, 3 if right else 4)
    if right:
        window(A, 1.4, 2.4, 4.5, 5.75, 'green'); window(A, 4.6, 5.3, 4.9, 5.6, None, 0)
        window(A, 5.6, 6.4, 1.2, 2.2, None, 1)
        A[T(.45):T(2.75), T(2.3):T(4.9)] = C['frame']  # 셔터 틀(셔터는 사진 부품으로 덮음)
    else:
        window(A, 3.0, 4.0, 4.5, 5.75, 'green')
        rect(A, .9, 4.2, 1.6, 2.6, C['patch']); rect(A, 1.0, 4.1, 1.7, 2.5, (236, 226, 192))  # 바랜 옛 광고 칸
        rect(A, 1.3, 2.2, 1.9, 2.3, C['red']); rect(A, 2.4, 3.8, 1.95, 2.25, C['ink'])
def p_back(A):
    wallbase(A, 5); rect(A, 6.2, 7.3, 0, 2.2, C['frame']); rect(A, 6.3, 7.2, .05, 2.1, C['wood2'])
    window(A, 2.0, 3.0, 4.5, 5.6, None, 1); window(A, 6.0, 7.0, 4.5, 5.6, None, 1); window(A, 2.2, 3.0, 1.4, 2.1, None, 0)
def p_flat(col, amt=.03, seed=7, lines=None):
    def f(A):
        A[:] = col; grain(A, amt, seed)
        if lines:
            for v in range(0, A.shape[0], lines[0]): A[v] = lines[1]
    return f
def p_awning(A):
    h, w = A.shape[:2]; sw = T(.36)
    for u in range(w): A[:, u] = C['red'] if (u // sw) % 2 == 0 else C['cream']
    A[:2] = C['ink']
def p_valance(A):
    p_awning(A); h, w = A.shape[:2]; A[:2] = C['ink']
    for u in range(w): A[:max(1, 3 - abs((u % T(.36)) - T(.18)) // 3)] = C['ink']  # 아래 물결 끝
GLY = {'M': ['10001', '11011', '11111', '10101', '10001', '10001', '10001'], 'A': ['01110', '10001', '10001', '11111', '10001', '10001', '10001'],
       'R': ['11110', '10001', '10001', '11110', '10100', '10010', '10001'], 'T': ['11111'] + ['00100'] * 6}
def p_sign(A):
    h, w = A.shape[:2]; A[:] = C['cream']; grain(A, .02, 9)
    A[:3] = C['ink']; A[-3:] = C['ink']; A[:, :3] = C['ink']; A[:, -3:] = C['ink']
    A[3:5, 3:-3] = C['red']; A[-5:-3, 3:-3] = C['red']
    cell = 5; gw = 5 * cell; gap = 11; tw = 4 * gw + 3 * gap; u0 = (w - tw) // 2; v0 = (h - 7 * cell) // 2
    for i, ch in enumerate('MART'):
        for r, row in enumerate(GLY[ch]):
            for c, b in enumerate(row):
                if b == '1': A[v0 + (6 - r) * cell:v0 + (7 - r) * cell, u0 + i * (gw + gap) + c * cell:u0 + i * (gw + gap) + (c + 1) * cell] = C['ink']
    yy, xx = np.mgrid[0:h, 0:w]
    for cx, col in ((u0 - 48, 'egg'), (u0 + tw + 48, 'red')):  # 왼: 계란, 오: 사과
        m = ((xx - cx) / 12.0) ** 2 + ((yy - h / 2) / (16.0 if col == 'egg' else 13.5)) ** 2 <= 1; A[m] = C[col]
        if col == 'red': A[int(h / 2) + 12:int(h / 2) + 18, cx:cx + 8] = C['green']
        else: A[m & (((xx - cx) / 12.0) ** 2 + ((yy - h / 2) / 16.0) ** 2 > .7)] = C['wood']
def p_rail(A): A[:] = C['frame']; A[:2] = C['soffit']
def p_crate_side(A):
    A[:] = C['wood']; h, w = A.shape[:2]
    for v in range(0, h, 3): A[v] = C['wood2']
    A[:, :2] = C['wood2']; A[:, -2:] = C['wood2']
def p_fruit(cols):
    def f(A):
        h, w = A.shape[:2]; A[:] = C['wood2']; rs = np.random.default_rng(len(cols) * 7 + w); A[1:-1, 1:-1] = cols[0]
        for v in range(1, h - 3, 4):
            for u in range((v // 4) % 2 * 2 + 1, w - 3, 4):
                if rs.random() < .3: A[v:v + 3, u:u + 3] = cols[rs.integers(1, len(cols))]
    return f
def p_egg(A):
    h, w = A.shape[:2]; A[:] = C['egg']; A[:2] = C['wood2']; A[-2:] = C['wood2']; A[:, :2] = C['wood2']; A[:, -2:] = C['wood2']
    for v in range(3, h - 4, 5):
        for u in range(3, w - 4, 5): A[v:v + 4, u:u + 4] = C['cream']
def p_vend_front(A):
    h, w = A.shape[:2]; A[:] = C['orange']; grain(A, .02, 11)
    rect(A, .08, .92, .8, 1.68, C['cream']); rect(A, .12, .88, .84, 1.64, C['glass'])
    for z in (.88, 1.15, 1.42):
        rect(A, .12, .88, z - .02, z, C['frame'])
        for i, x in enumerate(np.arange(.15, .84, .12)): rect(A, x, x + .07, z, z + .17, [C['red'], C['blue'], C['yellow'], C['green'], C['cream'], C['orange']][i % 6])
    for x in np.arange(.15, .84, .12): rect(A, x + .01, x + .06, .74, .78, C['ink'])
    rect(A, .68, .9, .4, .7, C['frame']); rect(A, .73, .85, .55, .62, C['dark'])
    rect(A, .1, .62, .12, .34, C['dark']); rect(A, 0, 1, 1.72, 1.85, C['cream'])
def p_vend_side(A): A[:] = C['orange']; rect(A, 0, 9, 1.72, 1.85, C['cream']); rect(A, 0, 9, .0, .08, C['dark'])
def p_ac(A):
    A[:] = C['frame']; h, w = A.shape[:2]; cx, cy = w * .6, h * .5; yy, xx = np.mgrid[0:h, 0:w]
    m = (xx - cx) ** 2 + (yy - cy) ** 2 <= (h * .38) ** 2; A[m] = C['soffit']
    for r in range(2, int(h * .38), 3): A[m & (np.abs(np.hypot(xx - cx, yy - cy) - r) < .6)] = C['dark']
def p_pipe(A): A[:] = C['soffit']; A[:, :1] = C['joint']

# ---------- 기하 ----------
quads = {}  # mesh 이름 -> [(o, U, V, painter)]
def Q(mesh, o, U, V, pt): quads.setdefault(mesh, []).append((Vector(o), Vector(U), Vector(V), pt))
def box(mesh, x0, x1, y0, y1, z0, z1, pt, skip=()):
    """pt: 문자열 하나(전 면) 또는 dict(front/back/left/right/top/bottom). 앞 = -Y."""
    g = (lambda k: pt.get(k, pt.get('*'))) if isinstance(pt, dict) else (lambda k: pt)
    dx, dy, dz = x1 - x0, y1 - y0, z1 - z0
    F = dict(front=((x0, y0, z0), (dx, 0, 0), (0, 0, dz)), back=((x1, y1, z0), (-dx, 0, 0), (0, 0, dz)),
             right=((x1, y0, z0), (0, dy, 0), (0, 0, dz)), left=((x0, y1, z0), (0, -dy, 0), (0, 0, dz)),
             top=((x0, y0, z1), (dx, 0, 0), (0, dy, 0)), bottom=((x0, y1, z0), (dx, 0, 0), (0, -dy, 0)))
    for k, (o, U, V) in F.items():
        if k not in skip and g(k): Q(mesh, o, U, V, g(k))

PT = dict(front=p_front, sideR=lambda A: p_side(A, True), sideL=lambda A: p_side(A, False), back=p_back,
          roof=p_flat(C['roof'], .04, 3, (16, C['soffit'])), cornice=p_flat(C['frame'], .02, 4), soffit=p_flat(C['soffit'], .02, 5),
          awning=p_awning, valance=p_valance, sign=p_sign, signedge=p_flat(C['ink'], 0, 1), rail=p_rail,
          crate=p_crate_side, fruitA=p_fruit([C['orange'], C['yellow']]), fruitB=p_fruit([C['red'], C['green']]),
          fruitC=p_fruit([C['yellow'], C['leaf']]), egg=p_egg, table=p_flat(C['green2'], .02, 6, (5, C['green'])),
          vf=p_vend_front, vs=p_vend_side, vt=p_flat(C['soffit'], 0, 1), ac=p_ac, acs=p_flat(C['frame'], 0, 1), pipe=p_pipe,
          hut=p_flat(WALL, .03, 8), hutroof=p_flat(C['roof'], .03, 9, (10, C['wood2'])), bluebox=p_flat(C['blue'], .02, 10, (6, (92, 100, 142))),
          pot=p_flat(C['wood2'], .02, 12), plant=p_fruit([C['green'], C['green2'], C['leaf']]), card=p_flat(C['cream'], 0, 1), stick=p_flat(C['dark'], 0, 1))
PT['shutter'] = 'PHOTO_B1'
W0, W1, D, H = -4.5, 4.5, 7.0, 6.0
box('MART_body', W0, W1, 0, D, 0, H, dict(front='front', back='back', right='sideR', left='sideL', top='roof'), skip=('bottom',))
# 처마 띠(코니스) + 난간
box('MART_body', W0 - .15, W1 + .15, -.15, D + .15, H, H + .22, dict({'*': 'cornice'}, bottom='soffit', top=None))
for x0, x1, y0, y1 in ((W0 - .15, W1 + .15, -.15, .05), (W0 - .15, W1 + .15, D - .05, D + .15), (W0 - .15, W0 + .05, .05, D - .05), (W1 - .05, W1 + .15, .05, D - .05)):
    box('MART_body', x0, x1, y0, y1, H + .22, H + .62, dict({'*': 'cornice'}, top='rail', bottom=None))
box('MART_body', W0 + .6, W0 + 3.0, 3.8, D - .4, H, H + 2.2, dict({'*': 'hut'}, top=None, bottom=None))  # 지붕 위 계단실
Q('MART_body', (W0 + .45, 3.6, H + 2.45), (2.7, 0, 0), (0, 3.25, -.3), 'hutroof')
Q('MART_body', (W0 + .45, D - .25, H + 2.11), (2.7, 0, 0), (0, -3.25, .3), 'soffit')
# 간판 (앞으로 .15)
box('MART_body', -4.3, 4.3, -.15, 0, 2.85, 4.2, dict({'*': 'signedge'}, front='sign', back=None))
# 차양 (벽 2.78 → 앞 1.6m 에서 2.38) + 앞 드림
Q('MART_body', (-4.6, -1.6, 2.38), (9.2, 0, 0), (0, 1.6, .4), 'awning')
Q('MART_body', (-4.6, 0, 2.76), (9.2, 0, 0), (0, -1.6, -.4), 'awning')  # 아랫면(2cm 아래, 겹침 방지)
Q('MART_body', (-4.6, -1.6, 2.08), (9.2, 0, 0), (0, 0, .3), 'valance'); Q('MART_body', (4.6, -1.58, 2.08), (-9.2, 0, 0), (0, 0, .3), 'valance')
for x in (-4.6, 4.6):  # 옆 삼각 대신 얇은 판(쿼드)
    s = 1 if x > 0 else -1; Q('MART_body', (x, 0 if s > 0 else -1.6, 2.38), (0, -1.6 * s, 0), (0, 0, .4), 'awning')
# 홈통·실외기·셔터(옆벽 사진 부품, 벽에서 2cm)
box('MART_body', W1 - .02, W1 + .1, -.1, .02, 0, H, 'pipe')
box('MART_body', W1, W1 + .3, 1.2, 2.05, 3.2, 3.8, dict({'*': 'acs'}, right='ac'))
Q('MART_body', (W1 + .02, 2.35, .45), (0, 2.5, 0), (0, 0, 2.25), 'shutter')
# 진열대: 2단 탁자 + 상자
box('MART_display', -3.95, -.35, -1.25, -.2, 0, .55, 'table')  # 진열대: 앞으로 35° 기운 2줄 받침(낮은 카메라에서 물건 면이 보이게)
box('MART_display', -3.95, -.35, -.25, -.2, .55, 1.3, 'table')
for x in (-3.98, -.38): box('MART_display', x, x + .03, -1.25, -.2, .55, 1.3, 'table')
tops = [['egg', 'fruitA', 'egg', 'fruitB'], ['fruitC', 'egg', 'fruitB', 'fruitA']]
for r, (y0, z0) in enumerate(((-1.25, .6), (-.75, .95))):
    for i, x in enumerate(np.arange(-3.9, -.4, .88)):
        Q('MART_display', (x, y0, z0 - .16), (.82, 0, 0), (0, 0, .16), 'crate')
        Q('MART_display', (x, y0, z0), (.82, 0, 0), (0, .5, .33), tops[r][i % 4])
box('MART_display', -2.3, -2.27, -1.24, -1.21, .73, 1.25, 'stick'); Q('MART_display', (-2.5, -1.25, 1.1), (.42, 0, 0), (0, 0, .28), 'card')
box('MART_display', 3.75, 4.3, -.75, -.25, 0, .3, 'bluebox'); box('MART_display', 3.78, 4.27, -.72, -.28, .3, .6, 'bluebox')  # 빈 상자 더미
box('MART_display', -4.45, -4.05, -.55, -.15, 0, .45, dict({'*': 'pot'}, top='plant')); box('MART_display', -4.4, -4.1, -.5, -.2, .45, .95, 'plant')
# 자판기 (오른쪽 옆, 건물에서 .25 띄움)
box('MART_vending', W1 + .25, W1 + 1.25, -.85, -.1, 0, 1.85, dict({'*': 'vs'}, front='vf', top='vt', bottom=None))

# ---------- 아틀라스 배치(선반 쌓기) ----------
items = []
for mesh, qs in quads.items():
    for qi, (o, U, V, pt) in enumerate(qs): items.append([mesh, qi, max(2, T(U.length)), max(2, T(V.length)), pt])
AW = 1024; order = sorted(range(len(items)), key=lambda k: -items[k][3]); x = y = rowh = 0; PAD = 2
for k in order:
    w, h = items[k][2], items[k][3]
    if x + w + PAD > AW: x = 0; y += rowh + PAD; rowh = 0
    items[k] += [x, y]; x += w + PAD; rowh = max(rowh, h)
AH = 1 << max(9, int(math.ceil(math.log2(y + rowh + 1))))
print('ATLAS', AW, AH, len(items), flush=True)
A = np.zeros((AH, AW, 3)); A[:] = (23, 37, 40); USED = np.zeros((AH, AW), bool); PHOTO = np.zeros((AH, AW), bool)
B1 = R + 'art/source/photo-picks/B/B1_shutters_greengrocer.jpg'; B1Q = (1050, 430, 1588, 962)
def photo_b1(h, w):
    im = bpy.data.images.load(B1); iw, ih = im.size; a = np.empty(iw * ih * 4, np.float32); im.pixels.foreach_get(a); a = a.reshape(ih, iw, 4)[::-1, :, :3]
    x0, y0, x1, y1 = B1Q; c = a[y0:y1, x0:x1] @ np.array((.299, .587, .114))
    ys = (np.arange(h)[::-1] + .5) / h * (y1 - y0); xs = (np.arange(w) + .5) / w * (x1 - x0)  # v=0 아래 = 사진 아래
    s = np.array([[c[max(0, int(yy) - 2):int(yy) + 3, max(0, int(xx) - 2):int(xx) + 3].mean() for xx in xs] for yy in ys])
    r = np.clip((s / s.mean() - 1) * .6, -.09, .09)  # 사진 명도 결만 약하게
    return np.array((200, 202, 190))[None, None, :] * (1 + r)[..., None]
for mesh, qi, w, h, pt, ux, uy in items:
    if pt == 'shutter': blk = photo_b1(h, w); PHOTO[uy:uy + h, ux:ux + w] = True
    else: blk = np.zeros((h, w, 3)); blk[:] = WALL; PT[pt](blk)
    A[uy:uy + h, ux:ux + w] = blk; USED[uy:uy + h, ux:ux + w] = True

# ---------- 양자화 + 외톨이 정리 ----------
flat = A.reshape(-1, 3); idx = np.concatenate([np.argmin(((flat[i:i + 65536, None, :] - P[None]) ** 2 * (.35, .45, .2)).sum(-1), 1) for i in range(0, len(flat), 65536)]).reshape(AH, AW)
def iso_mask(I):
    p = np.pad(I, 1, mode='edge'); nb = [p[:-2, 1:-1], p[2:, 1:-1], p[1:-1, :-2], p[1:-1, 2:]]
    return np.all([n != I for n in nb], 0) & USED, nb
for _ in range(2):
    m, nb = iso_mask(idx); st = np.stack(nb, -1)
    for yy, xx in zip(*np.nonzero(m)):
        v, cnt = np.unique(st[yy, xx], return_counts=True); idx[yy, xx] = v[np.argmax(cnt)]
Qd = P[idx]; Qd[~USED] = (23, 37, 40)
m, _ = iso_mask(idx); Lq = (Qd @ np.array((.299, .587, .114)))[USED]
img = bpy.data.images.new('mart', AW, AH, alpha=False); px = np.ones((AH, AW, 4), np.float32); px[..., :3] = Qd / 255.
img.pixels.foreach_set(px.ravel()); img.filepath_raw = OUT + 'mart.png'; img.file_format = 'PNG'; img.save()
# 다시 읽어 렌더에 사용(색 공간 sRGB)
bpy.data.images.remove(img); TEX = bpy.data.images.load(OUT + 'mart.png')

# ---------- 메시 ----------
mat = bpy.data.materials.new('MART_alb'); mat.use_nodes = True; nt = mat.node_tree; nt.nodes.clear()
tx = nt.nodes.new('ShaderNodeTexImage'); tx.image = TEX; tx.interpolation = 'Closest'; bs = nt.nodes.new('ShaderNodeBsdfDiffuse'); mo = nt.nodes.new('ShaderNodeOutputMaterial')
nt.links.new(tx.outputs[0], bs.inputs[0]); nt.links.new(bs.outputs[0], mo.inputs[0])
obs = []; tris = 0
for mesh in quads:
    me = bpy.data.meshes.new(mesh); bm = bmesh.new(); uvl = bm.loops.layers.uv.new('UVMap')
    for mname, qi, w, h, pt, ux, uy in items:
        if mname != mesh: continue
        o, U, V, _ = quads[mesh][qi]
        vs = [bm.verts.new(p) for p in (o, o + U, o + U + V, o + V)]; f = bm.faces.new(vs)
        uvs = ((ux + .02, uy + .02), (ux + w - .02, uy + .02), (ux + w - .02, uy + h - .02), (ux + .02, uy + h - .02))
        for lp, (a, b) in zip(f.loops, uvs): lp[uvl].uv = (a / AW, b / AH)
    bm.to_mesh(me); bm.free(); ob = bpy.data.objects.new(mesh, me); sc.collection.objects.link(ob); me.materials.append(mat); obs.append(ob)
    tris += sum(len(p.vertices) - 2 for p in me.polygons)
bpy.ops.object.select_all(action='DESELECT')
for ob in obs: ob.select_set(True)
bpy.context.view_layer.objects.active = obs[0]
bpy.ops.export_scene.fbx(filepath=OUT + 'mart.fbx', use_selection=True, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
                         path_mode='STRIP', embed_textures=False, mesh_smooth_type='FACE', object_types={'MESH'})
uvA = sum(i[2] * i[3] for i in items); wA = sum(q[1].length * q[2].length for qs in quads.values() for q in qs)
meas = dict(tris=tris, objects=[o.name for o in obs], atlas=[AW, AH], texels_per_m=round(math.sqrt(uvA / wA), 2), colors=int(len(np.unique(idx[USED]))),
            palette='palette50.gpl (palette46 + mart 4)', in_palette=True, isolated_pct=round(100 * m.sum() / USED.sum(), 3),
            p5=round(float(np.percentile(Lq, 5)), 1), p50=round(float(np.percentile(Lq, 50)), 1), p95=round(float(np.percentile(Lq, 95)), 1),
            photo_texels=int(PHOTO.sum()), size_m=dict(w=round(W1 - W0, 2), d=D, h_body=H, h_max=round(H + 2.45, 2), front_protrude=1.6 + .25),
            pivot='바닥 앞면 가운데 (앞벽 면 = 0)', forward='Blender -Y → FBX/Unity +Z (집 j판과 같은 설정)')
json.dump(meas, open(OUT + 'measure.json', 'w', encoding='utf-8'), indent=1, ensure_ascii=False); print('MEAS', meas, flush=True)

# ---------- 게이트 렌더: 해 1 + 하늘빛 ----------
gm = bpy.data.materials.new('ground'); gm.use_nodes = True; gm.node_tree.nodes['Principled BSDF'].inputs[0].default_value = (.32, .31, .27, 1)
bpy.ops.mesh.primitive_plane_add(size=80, location=(0, 10, 0)); bpy.context.object.data.materials.append(gm)
wd = bpy.data.worlds.new('w'); sc.world = wd; wd.use_nodes = True; bgn = wd.node_tree.nodes['Background']; bgn.inputs[0].default_value = (.62, .72, .78, 1); bgn.inputs[1].default_value = .9
sd = bpy.data.lights.new('sun', 'SUN'); sd.energy = 3.2; sd.angle = math.radians(2); so = bpy.data.objects.new('sun', sd); sc.collection.objects.link(so)
so.rotation_euler = (math.radians(40), 0, math.radians(-35))
sc.render.engine = 'CYCLES'; sc.cycles.samples = 96; sc.cycles.use_denoising = False; sc.cycles.filter_width = .5; sc.view_settings.view_transform = 'Standard'
cd = bpy.data.cameras.new('cam'); cd.sensor_fit = 'VERTICAL'; cam = bpy.data.objects.new('cam', cd); sc.collection.objects.link(cam); sc.camera = cam
def shot(name, pos, tgt, fov, rx, ry):
    cam.location = Vector(pos); cam.rotation_euler = (Vector(tgt) - cam.location).to_track_quat('-Z', 'Y').to_euler(); cd.angle = math.radians(fov)
    sc.render.resolution_x, sc.render.resolution_y = rx, ry; sc.render.filepath = RV + name + '.png'; bpy.ops.render.render(write_still=True); print('RENDER', name, flush=True)
shot('front', (0, -15, 2.2), (0, 0, 3.0), 30, 1280, 720)
shot('q34', (10.5, -11.5, 3.0), (.6, 1.0, 2.9), 36, 1280, 720)
shot('game_raw', (-2.5, -20, 2.8), (0, 0, 2.6), 64, 2560, 1440)  # 카메라 B 높이 2.8 · FOV 64 · 20m → 시트에서 1280×720 Nearest 축소
