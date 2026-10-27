"""해안 마을 집 6채 — 모듈 조립 + 사진 질감 × 늦가을 빛 해석식 칠 → 32색 unlit (아트2, 과제 20261027-1232).
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_houses.py
이번 방법 —
  형태: elegantcrow Retro Modular House(CC0) 의 벽(1·2m × 3m)·창(홑 2m·겹 3m)·문·덧창·발코니 난간·계단 조각을 1m 격자로 조립.
        직접 만든 덩어리는 최소만 — 낮은 경사(18°) 기와 지붕판·박공 삼각형·모서리 기둥·발코니 바닥판·차양·세로 간판·실외기·축대·평지붕 난간벽.
        소품: valsekamerplant 화분(CC0), Poly Pizza 자판기(dook, CC-BY 3.0 — 크레딧 필요).
  표면: 면마다 아틀라스 칸(32px/m). 칸의 각 픽셀 = 집 좌표 위치 → Poly Haven(CC0) 사진(회반죽·기와·칠 벗겨진 나무·돌·린넨)을
        32px/m 로 줄인 명암만 약하게 빌려 바탕색에 곱함 + 큰 얼룩(3m)·빗물 자국(처마 아래)·아래 때(0.8m).
  빛: 해 = 정면 왼쪽 위. 밝은 면 100% / 옆 85%(청록 6%) / 그늘 74%(청록 12%). 처마 밑 띠·발코니·차양 밑 그늘(80%, 청록).
  색: 아트1 키트 palette32 + 건물 밝히기 palette32 를 합친 64색 중 이 집들이 많이 쓰는 32색만 → 디더 없음 → 외톨이 픽셀 제거.
출력: art/export/scene-a4-houses/<이름>.fbx·.png, kit.json, palette32.gpl, measure.json · 면 분류 지도 art/models/opus_houses/<이름>_cls.npy
"""
import bpy, bmesh, glob, json, math, os
import numpy as np
from mathutils import Matrix, Vector

R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')); J = lambda *p: os.path.join(R, *p)
OUT = J('art', 'export', 'scene-a4-houses'); WORK = J('art', 'models', 'opus_houses')
os.makedirs(OUT, exist_ok=True); os.makedirs(WORK, exist_ok=True)
TP = J('art', 'thirdparty'); MOD = os.path.join(TP, 'elegantcrow-modular-house-pack', 'retro_modular_house', 'retro_modular_house', 'models')
PH = J('art', 'source', 'hillhouse-surfaces', 'photos')
DENS = 32; AW = 1024; GAP = 2
SUN = np.array((-.45, -.75, .55)); SUN /= np.linalg.norm(SUN)
TEAL = np.array((150, 190, 192)) / 255
ROLES = ['hidden', 'wall', 'roof', 'glass', 'frame', 'door', 'shutter', 'rail', 'slab', 'awning', 'sign', 'stone', 'ac', 'prop', 'flat', 'soffit']
RID = {r: i for i, r in enumerate(ROLES)}
srgb = lambda c: np.array(c, np.float64) / 255

TOK = {  # 토큰 → (모듈, 종류, 덧창)
    'w1': ('walls/wall_3x1.fbx', 'wall', None), 'w2': ('walls/wall_3x2.fbx', 'wall', None),
    'h1': ('walls/wall_1x1.fbx', 'wall', None), 'h2': ('walls/wall_1x2.fbx', 'wall', None),
    's': ('windows/single_windows/window_single_01.fbx', 'window', 'misc/window_shutters/shutters_single_open_01.fbx'),
    'S': ('windows/single_windows/window_single_02.fbx', 'window', None),
    'c': ('windows/single_windows/window_single_03.fbx', 'window', 'misc/window_shutters/shutters_single_closed_01.fbx'),
    'd': ('windows/double_windows/window_double_01.fbx', 'window', 'misc/window_shutters/shutters_double_open_01.fbx'),
    'dn': ('windows/double_windows/window_double_02.fbx', 'window', None),
    'D': ('doors/single_doors/door_single_01.fbx', 'door', None), 'b': ('doors/single_doors/door_single_02.fbx', 'door', None),
    'DD': ('doors/double_doors/door_double_01.fbx', 'door', None), 'GG': ('doors/double_doors/door_double_03.fbx', 'door', None),
}
RAIL = 'railings/railing_balcony_01.fbx'; STAIR = 'misc/stairs/stair_big_01.fbx'

# 집 6채. front/side/back = 층마다 토큰(왼→오, 바깥에서 볼 때). 색은 합친 64색 안의 값으로 잡았다.
HOUSES = [
    dict(name='house_narrow_a', note='좁은 3층 · 황토 회반죽 · 초록 덧창 · 2층 발코니 · 낮은 박공 기와',
         front=[['D', 'w1', 's'], ['s', 'w1', 'b'], ['s', 'w1', 's']], side=[['w2', 'w2', 'w2'], ['w1', 'S', 'w1', 'w2'], ['w1', 'S', 'w1', 'w2']],
         back=None, roof='gable', wall=(238, 210, 162), roof_c=(204, 136, 100), shutter=(112, 135, 110), door=(99, 122, 113),
         balcony=True, ac='right', pots=2, place='길가 줄집 — 길 양옆에 반복(좌우 반전·색 바꿔 쓰기 권장 안 함: 형태가 달라야 함, 다른 집과 섞어서)'),
    dict(name='house_narrow_b', note='좁은 3층 · 크림 회반죽 · 청록 덧창 · 평지붕 난간벽(포지타노)',
         front=[['D', 's'], ['s', 's'], ['S', 'w2']], side=[['w1', 'S', 'w2'], ['w1', 'c', 'w2'], ['w1', 'S', 'w2']],
         back=None, roof='flat', wall=(231, 220, 195), roof_c=(199, 179, 141), shutter=(95, 128, 126), door=(161, 109, 66),
         ac='left', pots=3, place='길가 줄집 — 경사 지붕 집 사이에 끼워 지붕선을 끊는 용'),
    dict(name='house_shop', note='가게 딸린 2층 · 연황토 · 1층 가게(겹문+겹창) · 줄무늬 차양 · 세로 간판 · 자판기',
         front=[['DD', 'dn'], ['s', 'w2', 's']], side=[['w2', 'w2', 'w2'], ['w1', 'S', 'w1', 'w2']],
         back=None, roof='gable', wall=(219, 193, 149), roof_c=(166, 109, 93), shutter=(112, 135, 110), door=(99, 122, 113),
         awning=((95, 128, 126), (231, 220, 195)), sign=((204, 136, 100), (231, 220, 195)), vending=True, ac='right', pots=2,
         place='교차로·정류장 앞 — 길 쪽에 차양·간판·자판기(눈높이 생활)'),
    dict(name='house_slope', note='비탈 집 · 연청록 회반죽 · 1.6m 돌 축대 + 앞마당 + 계단 · 갈색 덧창',
         front=[['w1', 'D', 'w1', 's'], ['s', 'w2', 's']], side=[['w1', 'S', 'w2'], ['w1', 'S', 'w2']],
         back=None, roof='gable', wall=(194, 209, 182), roof_c=(204, 136, 100), shutter=(161, 109, 66), door=(128, 80, 50),
         plinth=1.6, pots=2, place='언덕 쪽 비탈길 — 축대 바닥(z=0)을 낮은 쪽 길에 맞춤, 계단이 길 쪽'),
    dict(name='house_warehouse', note='창고형 · 흰 회반죽 · 큰 겹문 2 · 높은 띠벽 · 낮은 박공',
         front=[['w1', 'GG', 'w1', 'GG'], ['h1'] * 8], side=[['w2', 'S', 'w1', 'w2'], ['h1'] * 7],
         back=None, roof='gable', wall=(214, 210, 191), roof_c=(166, 109, 93), shutter=(112, 135, 110), door=(161, 109, 66),
         pots=0, place='항구 부두 쪽 — 문이 물 쪽 또는 길 쪽'),
    dict(name='house_cafe', note='작은 카페 · 크림 · 큰 겹창 + 문 · 테라코타 줄무늬 차양 · 화분',
         front=[['dn', 'D', 'w1']], side=[['w1', 'S', 'w1']],
         back=None, roof='gable', wall=(231, 220, 195), roof_c=(204, 136, 100), shutter=(95, 128, 126), door=(95, 128, 126),
         awning=((204, 136, 100), (231, 220, 195)), ac='left', pots=3, place='바다 보이는 길 모퉁이 — 차양이 길 쪽'),
]


# ---------- 조각 불러오기(한 번만, 로컬 미터로 굳힘) ----------
LIB = {}


def set_attrs(me, roles, cols=None):
    a = me.attributes.get('role') or me.attributes.new('role', 'INT', 'FACE'); a.data.foreach_set('value', np.array(roles, np.int32))
    c = me.attributes.get('pcol') or me.attributes.new('pcol', 'FLOAT_COLOR', 'FACE')
    cc = np.zeros((len(roles), 4), np.float32); cc[:, 3] = 1
    if cols is not None: cc[:, :3] = cols
    c.data.foreach_set('color', cc.ravel())


def mat_color(m):
    if not m or not m.use_nodes: return (.6, .6, .6)
    for n in m.node_tree.nodes:
        if n.type == 'TEX_IMAGE' and n.image and n.image.size[0]:
            px = np.array(n.image.pixels[:]).reshape(-1, 4)[:, :3]; return tuple(px.mean(0))
    for n in m.node_tree.nodes:
        if n.type == 'BSDF_PRINCIPLED': return tuple(np.clip(n.inputs['Base Color'].default_value[:3], 0, 1) ** (1 / 2.2))  # 선형 → sRGB
    return (.6, .6, .6)


def load(path, kind):
    if path in LIB: return LIB[path]
    before = set(bpy.data.objects)
    if path.endswith('.glb'): bpy.ops.import_scene.gltf(filepath=path)
    else: bpy.ops.import_scene.fbx(filepath=path)
    bpy.context.view_layer.update()
    new = [o for o in bpy.data.objects if o not in before]; out = []
    for o in [o for o in new if o.type == 'MESH']:
        me = o.data.copy(); me.transform(o.matrix_world)
        roles = []; cols = []
        for p in me.polygons:
            n = p.normal; inner = n.y > .9  # 모듈 바깥 = -y
            if kind == 'wall': r = 'hidden' if inner else 'wall'
            elif kind in ('window', 'door'):
                panel = o.name.startswith(('window_single', 'window_double', 'door_single', 'door_double'))
                if panel: r = 'hidden' if inner else 'wall'
                elif inner: r = 'hidden'
                elif abs(n.y) > .9 and p.area > .12: r = 'glass' if kind == 'window' else 'door'
                else: r = 'frame'
            elif kind == 'shutter': r = 'shutter'
            elif kind == 'rail': r = 'rail'
            elif kind == 'stair': r = 'stone'
            else: r = 'prop'
            roles.append(RID[r]); mi = p.material_index
            cols.append(mat_color(o.material_slots[mi].material if mi < len(o.material_slots) else None))
        set_attrs(me, roles, np.array(cols))
        out.append(me)
    for o in new: bpy.data.objects.remove(o, do_unlink=True)
    allv = np.concatenate([np.array([v.co for v in me.vertices]) for me in out])
    LIB[path] = (out, allv.min(0), allv.max(0)); return LIB[path]


PARTS = []  # 지금 집에 쌓는 물체들


def place(path, kind, T):
    mes, lo, hi = load(path, kind)
    for me in mes:
        m2 = me.copy(); m2.transform(T); o = bpy.data.objects.new(os.path.basename(path), m2); bpy.context.scene.collection.objects.link(o); PARTS.append(o)
    return lo, hi


def mk(verts, faces, roles):
    me = bpy.data.meshes.new('own'); me.from_pydata([tuple(v) for v in verts], [], faces); me.update()
    set_attrs(me, roles if isinstance(roles, list) else [RID[roles]] * len(faces))
    o = bpy.data.objects.new('own', me); bpy.context.scene.collection.objects.link(o); PARTS.append(o); return o


def box(x0, x1, y0, y1, z0, z1, side='wall', top=None, bottom='hidden'):
    v = [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0), (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)]
    f = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    return mk(v, f, [RID[bottom], RID[top or side]] + [RID[side]] * 4)


def Tm(x, y, z, rot=0, s=1):
    return Matrix.Translation((x, y, z)) @ Matrix.Rotation(math.radians(rot), 4, 'Z') @ Matrix.Scale(s, 4)


FACES = dict(front=((-1, -1), (1, 0), 0), right=((1, -1), (0, 1), 90), back=((1, 1), (-1, 0), 180), left=((-1, 1), (0, -1), -90))


def width(tok): lo, hi = load(os.path.join(MOD, TOK[tok][0]), TOK[tok][1])[1:]; return round(hi[0] - lo[0], 2)


def height(tok): lo, hi = load(os.path.join(MOD, TOK[tok][0]), TOK[tok][1])[1:]; return round(hi[2] - lo[2], 2)


def build(h):
    PARTS.clear()
    W = sum(width(t) for t in h['front'][0]); D = sum(width(t) for t in h['side'][0]); zb = h.get('plinth', 0)
    for spec in (h['front'], h['side']):
        for st in spec: assert abs(sum(width(t) for t in st) - (W if spec is h['front'] else D)) < .01, (h['name'], st)
    sh = [max(height(t) for t in st) for st in h['front']]; Hw = zb + sum(sh)
    back = h['back'] or [['h1'] * int(W) if s_ < 2 else ['w2'] * int(W // 2) + ['w1'] * int(W % 2) for s_ in sh]
    overhangs = []; bal_x = None
    for face, ((sx, sy), (dx, dy), rot) in FACES.items():
        spec = h['front'] if face == 'front' else back if face == 'back' else h['side']
        L = W if face in ('front', 'back') else D; z = zb
        for si, st in enumerate(spec):
            acc = 0
            for t in st:
                w = width(t); path, kind, shut = TOK[t]
                cx = sx * W / 2 + dx * (acc + w / 2); cy = sy * D / 2 + dy * (acc + w / 2)
                T = Tm(cx, cy, z, rot); place(os.path.join(MOD, path), kind, T)
                if shut: place(os.path.join(MOD, shut), 'shutter', T)
                if t == 'b' and face == 'front': bal_x = (cx, z)
                acc += w
            z += sh[si]
    for x in (-W / 2, W / 2):  # 모서리 기둥(벽판 두께 이음 메움)
        for y in (-D / 2, D / 2): box(x - .1, x + .1, y - .1, y + .1, zb, Hw)
    if h['roof'] == 'gable':
        p = math.tan(math.radians(18)); ov, og, th = .45, .3, .12
        x0, x1 = -W / 2 - og, W / 2 + og; ye = D / 2 + ov
        zt = lambda y: Hw + .1 + (D / 2 - abs(y)) * p
        for sgn in (-1, 1):  # 앞·뒤 경사판(윗면 roof, 아랫면 soffit, 옆 soffit)
            v = [(x0, sgn * ye, zt(ye)), (x1, sgn * ye, zt(ye)), (x1, 0, zt(0)), (x0, 0, zt(0))]
            v += [(a, b, c - th) for a, b, c in v]
            f = [(0, 1, 2, 3), (4, 7, 6, 5), (0, 4, 5, 1), (1, 5, 6, 2), (3, 7, 4, 0), (2, 6, 7, 3)]
            if sgn > 0: f = [tuple(reversed(q)) for q in f]
            mk(v, f, [RID['roof'], RID['soffit'], RID['soffit'], RID['soffit'], RID['soffit'], RID['hidden']])
        for sx in (-1, 1):  # 박공 삼각형
            x = sx * (W / 2 + .1); v = [(x, -D / 2 - .1, Hw), (x, D / 2 + .1, Hw), (x, 0, zt(0) - th)]
            mk(v, [(2, 1, 0) if sx < 0 else (0, 1, 2)], 'wall')
        overhangs.append(('eave', Hw))
    else:  # 평지붕: 판 + 난간벽
        box(-W / 2 - .15, W / 2 + .15, -D / 2 - .15, D / 2 + .15, Hw, Hw + .2, side='wall', top='flat')
        for (a, b, c, d) in ((-W / 2 - .15, W / 2 + .15, -D / 2 - .15, -D / 2 + .05), (-W / 2 - .15, W / 2 + .15, D / 2 - .05, D / 2 + .15),
                             (-W / 2 - .15, -W / 2 + .05, -D / 2 - .15, D / 2 + .15), (W / 2 - .05, W / 2 + .15, -D / 2 - .15, D / 2 + .15)):
            box(a, b, c, d, Hw + .2, Hw + .75, side='wall', top='slab')
        overhangs.append(('eave', Hw))
    if h.get('balcony') and bal_x:  # 발코니: 바닥판 + 난간 2 + 옆 난간
        cx, z = bal_x; y0 = -D / 2 - .1
        box(cx - 1.1, cx + 1.1, y0 - .9, y0, z - .15, z, side='slab', top='slab', bottom='soffit')
        for i in (-1, 1): place(os.path.join(MOD, RAIL), 'rail', Tm(cx + i * .53, y0 - .85, z))
        for i in (-1, 1): place(os.path.join(MOD, RAIL), 'rail', Tm(cx + i * 1.05, y0 - .45, z, 90, .8))
        overhangs.append(('box', cx - 1.1, cx + 1.1, y0 - .9, y0 + .2, z - .15))
    if h.get('awning'):  # 차양: 벽 z 2.75 → 1.3m 밖 z 2.3, 폭 = 정면 전체 - 0.4
        a0, a1 = -W / 2 + .2, W / 2 - .2; y0 = -D / 2 - .1; out = 1.3
        v = [(a0, y0, 2.75), (a1, y0, 2.75), (a1, y0 - out, 2.3), (a0, y0 - out, 2.3)]
        v += [(a, b, c - .05) for a, b, c in v] + [(a0, y0 - out, 2.05), (a1, y0 - out, 2.05)]
        mk(v, [(0, 3, 2, 1), (4, 5, 6, 7), (8, 9, 2, 3)], [RID['awning'], RID['soffit'], RID['awning']])  # 윗면·아랫면·앞 늘어진 천
        overhangs.append(('box', a0, a1, y0 - out, y0 + .2, 2.75))
    if h.get('sign'):  # 세로 간판(벽에서 직각으로 튀어나옴, 일본식)
        x = W / 2 - .04; y0 = -D / 2 - .1  # 모서리 쪽(덧창과 안 겹치게)
        box(x - .06, x + .06, y0 - .75, y0 - .1, 3.25, 4.75, side='sign', top='frame', bottom='frame')
        box(x - .03, x + .03, y0 - .12, y0, 4.3, 4.4, side='rail', top='rail')
    if h.get('ac'):  # 실외기
        sx = 1 if h['ac'] == 'right' else -1; x = sx * (W / 2 + .1); y = D / 2 - 1.0
        box(min(x, x + sx * .32), max(x, x + sx * .32), y - .4, y + .4, zb + .05, zb + .65, side='ac', top='ac')
    if h.get('plinth'):  # 축대 + 앞마당 + 계단(모듈) + 마당 난간
        P = h['plinth']; ty = -D / 2 - 1.8
        box(-W / 2 - .1, W / 2 + .1, ty, D / 2 + .1, 0, P, side='stone', top='slab')
        mes, lo, hi = load(os.path.join(MOD, STAIR), 'stair')
        sz = hi - lo; allv = np.concatenate([np.array([v.co for v in me.vertices]) for me in mes])
        # 오르는 쪽 찾기: 네 끝 조각 중 평균 높이가 가장 높은 쪽
        ends = {}
        for ax in (0, 1):
            t = (allv[:, ax] - lo[ax]) / max(sz[ax], 1e-6)
            ends[(ax, -1)] = allv[t < .2, 2].mean(); ends[(ax, 1)] = allv[t > .8, 2].mean()
        ax, sgn = max(ends, key=ends.get); up_dir = {(0, 1): 0, (1, 1): 90, (0, -1): 180, (1, -1): -90}[(ax, sgn)]
        s = P / sz[2]; rot = 90 - up_dir  # 오르는 방향을 +y(집 쪽)로
        run = (sz[ax]) * s; sx_ = W / 2 - 1.2
        c = (lo + hi) / 2
        T = Tm(sx_, ty - run / 2, 0, rot, s) @ Matrix.Translation((-c[0], -c[1], -lo[2]))
        place(os.path.join(MOD, STAIR), 'stair', T)
        span = sz[1 - ax] * s
        for xi in np.arange(-W / 2 + .55, sx_ - span / 2 - .4, 1.06): place(os.path.join(MOD, RAIL), 'rail', Tm(xi, ty + .1, P, 0, 1))
    if h.get('vending'):  # 자판기(Poly Pizza, CC-BY) — 1.8m, 정면 왼쪽 앞
        vp = glob.glob(os.path.join(TP, 'polypizza-misc', 'Vending*.glb'))[0]; mes, lo, hi = load(vp, 'prop'); s = 1.8 / (hi[2] - lo[2]); c = (lo + hi) / 2
        place(vp, 'prop', Tm(-W / 2 - .6, -D / 2 - .45, 0, 0, s) @ Matrix.Translation((-c[0], -c[1], -lo[2])))
    pots = sorted(glob.glob(os.path.join(TP, 'valsekamerplant-plants-pots', '**', 'Plants', '*.glb'), recursive=True))
    for i in range(h.get('pots', 0)):
        pp = pots[i % len(pots)]; mes, lo, hi = load(pp, 'prop'); c = (lo + hi) / 2
        x = (-W / 2 + .35, W / 2 - .35, W / 2 - 1.1)[i % 3]
        place(pp, 'prop', Tm(x, -D / 2 - .45, zb, 30 * i) @ Matrix.Translation((-c[0], -c[1], -lo[2])))
    # 하나로 합치기
    bpy.ops.object.select_all(action='DESELECT')
    for o in PARTS: o.select_set(True)
    bpy.context.view_layer.objects.active = PARTS[0]; bpy.ops.object.join(); o = bpy.context.view_layer.objects.active; o.name = h['name']; o.data.name = h['name']
    for uv in list(o.data.uv_layers): o.data.uv_layers.remove(uv)
    o.data.uv_layers.new(name='BakedUV')
    return o, dict(W=W, D=D, Hw=Hw, zb=zb, overhangs=overhangs)


# ---------- 사진 질감 ----------
def photo(name, cover):
    im = bpy.data.images.load(os.path.join(PH, name)); w, hh = im.size
    a = np.array(im.pixels[:]).reshape(hh, w, 4)[:, :, :3]; n = max(8, int(cover * DENS)); f = w // n
    a = a[:f * n, :f * n].reshape(n, f, n, f, 3).mean((1, 3)); lum = a @ (.299, .587, .114)
    d = (lum - lum.mean()) / (lum.std() + 1e-6); return np.clip(d, -2, 2), cover


PHOTOS = dict(wall=photo('worn_plaster_wall_diff_1k.jpg', 2.5), roof=photo('clay_roof_tiles_diff_1k.jpg', 2.0),
              wood=photo('wood_peeling_paint_weathered_diff_1k.jpg', 1.5), stone=photo('stone_block_wall_diff_1k.jpg', 3.0),
              cloth=photo('rough_linen_diff_1k.jpg', 1.0))


def sample(key, u, v):
    d, cov = PHOTOS[key]; n = d.shape[0]
    return d[((v / cov) * n).astype(int) % n, ((u / cov) * n).astype(int) % n]


def vnoise(x, y, seed):
    xi, yi = np.floor(x), np.floor(y); fx, fy = x - xi, y - yi
    hsh = lambda a, b: (np.sin(a * 127.1 + b * 311.7 + seed * 74.7) * 43758.5453) % 1
    sm = lambda t: t * t * (3 - 2 * t); fx, fy = sm(fx), sm(fy)
    return (hsh(xi, yi) * (1 - fx) + hsh(xi + 1, yi) * fx) * (1 - fy) + (hsh(xi, yi + 1) * (1 - fx) + hsh(xi + 1, yi + 1) * fx) * fy


# ---------- 칸 나누기·칠하기 ----------
def pack_paint(o, h, g, seed):
    me = o.data; V = np.array([v.co for v in me.vertices]); roles = np.zeros(len(me.polygons), np.int32); me.attributes['role'].data.foreach_get('value', roles)
    pc = np.zeros(len(me.polygons) * 4, np.float32); me.attributes['pcol'].data.foreach_get('color', pc); pc = pc.reshape(-1, 4)[:, :3]
    rects = []
    for f in me.polygons:
        ps = V[list(f.vertices)]; n = np.array(f.normal); role = ROLES[roles[f.index]]
        if abs(n[2]) < .999: U = np.cross((0, 0, 1.), n); U /= np.linalg.norm(U)
        else: U = np.array((1., 0, 0))
        Vv = np.cross(n, U); Vv /= max(np.linalg.norm(Vv), 1e-9)
        q = np.stack([ps @ U, ps @ Vv], 1); lo_, hi_ = q.min(0), q.max(0)
        dn = 1 if role == 'hidden' else DENS
        w, hgt = (max(2, math.ceil((hi_[k] - lo_[k]) * dn)) for k in (0, 1))
        rects.append(dict(i=f.index, n=n, U=U, V=Vv, q=q, lo=lo_, hi=hi_, w=w, h=hgt, role=role, pc=pc[f.index], c=ps.mean(0)))
    x = y = GAP; rh = 0
    for r in sorted(rects, key=lambda r: (r['h'], r['w']), reverse=True):
        if x + r['w'] + GAP > AW: x = GAP; y += rh + GAP; rh = 0
        r['x'], r['y'] = x, y; x += r['w'] + GAP; rh = max(rh, r['h'])
    AH = 2 ** math.ceil(math.log2(y + rh + GAP))
    img = np.zeros((AH, AW, 3)); img[:] = srgb(h['wall']) * .85; cls = np.zeros((AH, AW), np.uint8)
    uvl = me.uv_layers['BakedUV']
    for r in rects:
        w, hh = r['w'], r['h']; ii = np.arange(-1, w + 1); jj = np.arange(-1, hh + 1)
        q0 = r['lo'][0] + np.clip(ii, 0, w - 1) / max(w - 1, 1) * (r['hi'][0] - r['lo'][0])
        q1 = r['lo'][1] + np.clip(jj, 0, hh - 1) / max(hh - 1, 1) * (r['hi'][1] - r['lo'][1])
        u = np.broadcast_to(q0[None, :], (hh + 2, w + 2)); v = np.broadcast_to(q1[:, None], (hh + 2, w + 2))
        n = r['n']; P = u[..., None] * r['U'] + v[..., None] * r['V']
        P = P + n * float(r['c'] @ n)  # 평면 위 실제 위치
        z = P[..., 2]; role = r['role']; one = np.ones(u.shape)
        if role in ('wall', 'frame'):
            base = srgb(h['wall']) * 1.07 if role == 'wall' else srgb((238, 234, 222))  # 1차: 가게·비탈·창고 벽 L 158~165 → 7% 올림
            det = sample('wall', u, v) * .035
            blot = (vnoise(P[..., 0] / 3 + P[..., 1] / 3, z / 3, seed) - .5) * .08
            streak = (vnoise(u * 2.2, 0 * u, seed + 3) > .62) * np.clip(1 - (g['Hw'] - z) / 2.8, 0, 1) * .05
            grime = np.where(z - g['zb'] < .8, .10 * (1 - np.clip((z - g['zb']) / .8, 0, 1)), 0)
            col = base * (1 + det + blot - streak - grime)[..., None]
        elif role == 'roof':
            col = srgb(h['roof_c']) * (1 + sample('roof', u, v) * .09 + (vnoise(u / 2.5, v / 2.5, seed + 7) - .5) * .08)[..., None]
        elif role == 'flat': col = srgb(h['roof_c']) * (1 + sample('wall', u, v) * .04)[..., None]
        elif role == 'soffit': col = srgb(h['wall']) * .78 * one[..., None]
        elif role == 'glass':
            zz = (v - r['lo'][1]) / max(r['hi'][1] - r['lo'][1], 1e-6)
            col = np.where((zz < .3)[..., None], srgb((74, 114, 118)), srgb((54, 84, 90))) * one[..., None]
            diag = ((u - r['lo'][0]) - (v - r['lo'][1]) * .6) % 1.4 < .18; col[diag] = srgb((95, 128, 126))
        elif role == 'door': col = srgb(h['door']) * (1 + sample('wood', u, v) * .07)[..., None]
        elif role == 'shutter': col = srgb(h['shutter']) * (1 + sample('wood', u, v) * .07)[..., None]
        elif role == 'rail': col = srgb((81, 98, 88)) * one[..., None]
        elif role == 'slab': col = srgb((207, 204, 186)) * (1 + sample('stone', u, v) * .03)[..., None]
        elif role == 'stone': col = srgb((182, 178, 157)) * (1 + sample('stone', u, v) * .10)[..., None]
        elif role == 'awning':
            a, b = h['awning']; st = (np.floor((P[..., 0] + 10) / .4) % 2)[..., None]
            col = (srgb(a) * st + srgb(b) * (1 - st)) * (1 + sample('cloth', u, v) * .03)[..., None]
        elif role == 'sign':
            a, b = h['sign']; zz = (v - r['lo'][1]) / max(r['hi'][1] - r['lo'][1], 1e-6); uu = (u - r['lo'][0]) / max(r['hi'][0] - r['lo'][0], 1e-6)
            inner = (zz > .08) & (zz < .92) & (uu > .15) & (uu < .85)
            col = np.where(inner[..., None], srgb(b), srgb(a)) * one[..., None]
            for k in range(3):  # 글자 자리 세 덩이(읽히는 글자는 넣지 않음)
                m = inner & (zz > .2 + k * .25) & (zz < .36 + k * .25) & (uu > .33) & (uu < .67); col[m] = srgb((128, 80, 50))
        elif role == 'ac':
            col = srgb((215, 208, 183)) * one[..., None]
            if abs(n[2]) < .5:
                uu = (u - r['lo'][0]) - (r['hi'][0] - r['lo'][0]) * .4; vv = (v - r['lo'][1]) - (r['hi'][1] - r['lo'][1]) * .5
                if (r['hi'][0] - r['lo'][0]) > .5: col[uu ** 2 + vv ** 2 < .2 ** 2] = srgb((81, 98, 88))
        elif role == 'prop': col = np.clip(r['pc'], 0, 1) * one[..., None]
        else: col = srgb(h['wall']) * .7 * one[..., None]
        if role in ('wall', 'frame', 'glass', 'door', 'shutter'):  # 처마·발코니·차양 밑 그늘
            sh = np.zeros(u.shape, bool)
            for oh in g['overhangs']:
                if oh[0] == 'eave': sh |= (z > oh[1] - .35) & (z < oh[1] + .2)
                else:
                    _, x0, x1, y0, y1, zb_ = oh; sh |= (P[..., 0] >= x0) & (P[..., 0] <= x1) & (P[..., 1] >= y0 - .05) & (P[..., 1] <= y1) & (z < zb_) & (z > zb_ - .9)
            col[sh] = col[sh] * .82 * (1 - .10) + TEAL * .82 * .10
        d = float(n @ SUN)
        f, tint = (1.0, 0) if d > .45 else (.85, .06) if d > .10 else (.74, .12)
        col = col * f * (1 - tint) + TEAL * f * tint
        y0, x0 = r['y'] - 1, r['x'] - 1
        img[y0:y0 + hh + 2, x0:x0 + w + 2] = np.clip(col, 0, 1); cls[y0:y0 + hh + 2, x0:x0 + w + 2] = RID[role]
        f_ = me.polygons[r['i']]
        for li, qq in zip(f_.loop_indices, r['q']):
            uvl.data[li].uv = ((r['x'] + .5 + (qq[0] - r['lo'][0]) / max(r['hi'][0] - r['lo'][0], 1e-8) * (w - 1)) / AW,
                               (r['y'] + .5 + (qq[1] - r['lo'][1]) / max(r['hi'][1] - r['lo'][1], 1e-8) * (hh - 1)) / AH)
    return img, cls, len(rects)


# ---------- 팔레트 ----------
def read_gpl(p):
    return [tuple(int(t) for t in l.split()[:3]) for l in open(p) if l[:1].isdigit() or (l.strip() and l.split()[0].isdigit())]


def nearest(img, pal):
    flat = (img.reshape(-1, 3) * 255); P = np.array(pal, np.float64); idx = np.empty(len(flat), np.int32)
    for s in range(0, len(flat), 1 << 18): idx[s:s + (1 << 18)] = ((flat[s:s + (1 << 18), None] - P[None]) ** 2).sum(2).argmin(1)
    return idx.reshape(img.shape[:2])


def despeckle(idx):
    for _ in range(3):
        up = np.roll(idx, 1, 0); dn = np.roll(idx, -1, 0); lf = np.roll(idx, 1, 1); rt = np.roll(idx, -1, 1)
        iso = (idx != up) & (idx != dn) & (idx != lf) & (idx != rt)
        if not iso.any(): break
        idx = np.where(iso, np.where(up == dn, up, np.where(lf == rt, lf, up)), idx)
    return idx


def speckle(idx):
    return float(((idx != np.roll(idx, 1, 0)) & (idx != np.roll(idx, -1, 0)) & (idx != np.roll(idx, 1, 1)) & (idx != np.roll(idx, -1, 1))).mean() * 100)


def save_png(arr, path):
    hh, w = arr.shape[:2]; im = bpy.data.images.new(os.path.basename(path), w, hh, alpha=False)
    im.pixels.foreach_set(np.concatenate([arr, np.ones((hh, w, 1))], 2).astype(np.float32).ravel())
    im.filepath_raw = path; im.file_format = 'PNG'; im.save(); return im


def export(o, im, path):
    m = bpy.data.materials.new('unlit_' + o.name); m.use_nodes = True; n = m.node_tree.nodes; n.clear(); L = m.node_tree.links
    tx = n.new('ShaderNodeTexImage'); tx.image = im; tx.interpolation = 'Closest'; em = n.new('ShaderNodeEmission'); mo = n.new('ShaderNodeOutputMaterial')
    L.new(tx.outputs[0], em.inputs[0]); L.new(em.outputs[0], mo.inputs[0])
    o.data.materials.clear(); o.data.materials.append(m)
    for f in o.data.polygons: f.material_index = 0
    for a in ('role', 'pcol'): o.data.attributes.remove(o.data.attributes[a])
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'MESH'}, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
                             path_mode='STRIP', embed_textures=False, mesh_smooth_type='FACE')


bpy.ops.wm.read_factory_settings(use_empty=True)
PH_KEEP = PHOTOS  # 사진은 위에서 이미 읽음(빈 장면으로 바꾸기 전) — read_factory_settings 가 이미지를 지우므로 numpy 로만 들고 있음
built = []
for k, h in enumerate(HOUSES):
    o, g = build(h)
    img, cls, nf = pack_paint(o, h, g, k * 13 + 5)
    tris = sum(len(p.vertices) - 2 for p in o.data.polygons)
    pts = np.array([v.co for v in o.data.vertices]); lo, hi = pts.min(0), pts.max(0)
    built.append((h, o, img, cls, g, tris, nf, lo, hi)); print('BUILT', h['name'], 'W', g['W'], 'D', g['D'], 'Hw', round(g['Hw'], 2), 'tris', tris, 'tex', img.shape[1], img.shape[0], flush=True)
# 합친 64색 → 이 집들이 많이 쓰는 32색
union = list(dict.fromkeys(read_gpl(J('art', 'export', 'scene-a2-kit', 'palette32.gpl')) + read_gpl(J('art', 'export', 'scene-a2-bldg-light', 'palette32.gpl'))))
cnt = np.zeros(len(union))
for b in built:
    idx = nearest(b[2], union); vis = b[3] != RID['hidden']; cnt += np.bincount(idx[vis], minlength=len(union))
pal = [union[i] for i in np.argsort(-cnt)[:32]]
open(os.path.join(OUT, 'palette32.gpl'), 'w').write('GIMP Palette\nName: scene-a4-houses (아트1 키트 + 건물 밝히기 합집합 중 32)\nColumns: 8\n#\n' +
                                                    ''.join(f'{r} {g_} {b_} c{i}\n' for i, (r, g_, b_) in enumerate(pal)))
kit = dict(task='20261027-1232', note='도현 승인 전. F1/Assets 에 넣지 않음. Unity 임포트 미확인. 원점 = 집 바닥(축대 집은 축대 바닥) 가운데.',
           axis='Blender: 정면 -Y, 위 +Z → FBX axis_forward -Z, axis_up Y, 1u = 1m', method=__doc__.split('이번 방법 —')[1].split('출력:')[0].strip(),
           sun_dir=SUN.round(3).tolist(), palette='palette32.gpl', texels_per_m=DENS,
           license='CC0 — elegantcrow Retro Modular House(모듈) · valsekamerplant PSX house plants(화분) · Poly Haven 사진(worn_plaster_wall, clay_roof_tiles, wood_peeling_paint_weathered, stone_block_wall, rough_linen). '
                   'house_shop 만 + CC-BY 3.0 "Vending Machine" by dook via Poly Pizza (게임 크레딧 필요).',
           files=[])
meas = {}
for h, o, img, cls, g, tris, nf, lo, hi in built:
    idx = despeckle(nearest(img, pal)); q = np.array(pal, np.float64)[idx] / 255
    png = os.path.join(OUT, h['name'] + '.png'); im = save_png(q, png); np.save(os.path.join(WORK, h['name'] + '_cls.npy'), cls[::-1])
    Lm = q @ np.array((.299, .587, .114)) * 255; vis = cls != RID['hidden']
    by = {r: round(float(Lm[cls == RID[r]].mean()), 1) for r in ROLES if (cls == RID[r]).any()}
    meas[h['name']] = dict(L_all=round(float(Lm.mean()), 1), L_visible=round(float(Lm[vis].mean()), 1), L_wall=by.get('wall'), by_role=by,
                           speckle_1px_pct=round(speckle(idx), 3), colors=int(len(np.unique(idx))), size=[img.shape[1], img.shape[0]])
    srcs = sorted({os.path.relpath(p, TP).replace('\\', '/') for p in LIB})
    export(o, im, os.path.join(OUT, h['name'] + '.fbx'))
    kit['files'].append(dict(name=h['name'], fbx=h['name'] + '.fbx', png=h['name'] + '.png', note=h['note'], place=h['place'],
                             size_m=[round(float(v), 2) for v in (hi - lo)], footprint_m=[float(g['W']), float(g['D'])], wall_top_m=round(float(g['Hw']), 2),
                             tris=int(tris), faces_in_atlas=nf, tex=[img.shape[1], img.shape[0]], wall=h['wall'], roof=h['roof_c'],
                             L_wall=meas[h['name']]['L_wall'], speckle_1px_pct=meas[h['name']]['speckle_1px_pct']))
    print('MEAS', h['name'], meas[h['name']], flush=True)
kit['sources_used'] = sorted({os.path.relpath(p, TP).replace('\\', '/') for p in LIB})
json.dump(kit, open(os.path.join(OUT, 'kit.json'), 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
json.dump(dict(luma='0.299R+0.587G+0.114B (32색 뒤)', speckle='네 이웃과 모두 다른 픽셀 %', files=meas), open(os.path.join(OUT, 'measure.json'), 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
print('DONE', flush=True)
