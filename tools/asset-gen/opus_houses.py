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
import sys
VARIANT = (sys.argv[sys.argv.index('--') + 1:] or [None])[0] if '--' in sys.argv else None
VARIANT = VARIANT or os.environ.get('HOUSES_VARIANT', 'b')  # 실행: ... -P opus_houses.py -- c  # 'a' = 과제 1232 원본, 'b' = 과제 1248(따뜻한 벽·밝은 화분·유리)
SUF = {'a': '', 'b': '-b', 'c': '-c', 'd': '-d'}[VARIANT]  # 'c' = 과제 1302(D-173): 깔끔한 칠 벽 + 9텍셀/m 손도트, 형태는 b 와 같음
OUT = J('art', 'export', 'scene-a4-houses' + SUF); WORK = J('art', 'models', 'opus_houses' + SUF.replace('-', '_'))
os.makedirs(OUT, exist_ok=True); os.makedirs(WORK, exist_ok=True)
TP = J('art', 'thirdparty'); MOD = os.path.join(TP, 'elegantcrow-modular-house-pack', 'retro_modular_house', 'retro_modular_house', 'models')
PH = J('art', 'source', 'hillhouse-surfaces', 'photos')
DENS, AW, GAP = (9, 256, 2) if VARIANT == 'c' else (10, 256, 2) if VARIANT == 'd' else (32, 1024, 2)
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

if VARIANT in ('b', 'c', 'd'):  # 1248: ① A Short Hike 처럼 살구·크림 쪽으로 (회녹 벽 없앰)
    WARM = dict(house_narrow_a=(242, 204, 158), house_narrow_b=(244, 226, 198), house_shop=(238, 196, 150),
                house_slope=(242, 196, 166), house_warehouse=(238, 222, 194), house_cafe=(246, 214, 184))
    if VARIANT in ('c', 'd'):  # 1302: 집마다 깨끗한 칠 한 색 — 연황토·크림·살구·연청록·흰색·살구크림
        WARM = dict(house_narrow_a=(238, 204, 146), house_narrow_b=(246, 232, 204), house_shop=(242, 192, 152),
                    house_slope=(172, 210, 200), house_warehouse=(242, 240, 230), house_cafe=(248, 216, 180))
    for h_ in HOUSES: h_['wall'] = WARM[h_['name']]


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
            px = np.array(n.image.pixels[:]).reshape(-1, 4)
            if VARIANT in ('b', 'c'):  # 1248: 투명 칸(RGB 0)까지 평균 내서 화분이 L 48 로 검게 나왔다 → 불투명 픽셀만, 최소 밝기 .45
                px = px[px[:, 3] > .5] if (px[:, 3] > .5).sum() > 16 else px; c = px[:, :3].mean(0); l = c @ (.299, .587, .114)
                return tuple(np.clip(c * max(1, .45 / max(l, 1e-3)), 0, 1))
            return tuple(px[:, :3].mean(0))
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
            k_ = (1 + det + blot - streak - grime)[..., None]; col = base * k_
            if VARIANT == 'b': col = col * (1 - np.clip(1 - k_, 0, 1) * np.array((0, .25, .7)))  # 1248: 어두운 얼룩은 회녹 대신 회갈·황토 쪽
        elif role == 'roof':
            col = srgb(h['roof_c']) * (1 + sample('roof', u, v) * .09 + (vnoise(u / 2.5, v / 2.5, seed + 7) - .5) * .08)[..., None]
        elif role == 'flat': col = srgb(h['roof_c']) * (1 + sample('wall', u, v) * .04)[..., None]
        elif role == 'soffit': col = srgb(h['wall']) * .78 * one[..., None]
        elif role == 'glass':
            zz = (v - r['lo'][1]) / max(r['hi'][1] - r['lo'][1], 1e-6)
            g1, g2 = ((92, 132, 134), (70, 104, 108)) if VARIANT == 'b' else ((74, 114, 118), (54, 84, 90))
            col = np.where((zz < .3)[..., None], srgb(g1), srgb(g2)) * one[..., None]
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


# ---------- c판(1302): 깔끔한 칠 + 손도트 ----------
def pack_paint_c(o, h, g, seed):
    """면마다 9텍셀/m 칸. 사진·얼룩·AO 없음 — 역할별 단색 + 면 단위 빛 3단(해/옆/그늘) + 처마 밑·바닥 0.6m 한 단 어둡게.
    디테일은 텍셀 단위로 그림: 창·문 1텍셀 짙은 테두리, 유리 반사 2텍셀, 덧창 살, 기와 2텍셀 줄, 처마 끝 밝은 선, 건물 모서리·바닥 짙은 선, 간판 도트 기호, 돌 줄눈.
    한 변이 3텍셀 미만인 작은 면(창틀 두께·화분·난간 등)은 같은 색끼리 1텍셀 칸 하나를 같이 쓴다."""
    me = o.data; V = np.array([v.co for v in me.vertices]); roles = np.zeros(len(me.polygons), np.int32); me.attributes['role'].data.foreach_get('value', roles)
    pc = np.zeros(len(me.polygons) * 4, np.float32); me.attributes['pcol'].data.foreach_get('color', pc); pc = pc.reshape(-1, 4)[:, :3]
    wv = np.array([V[list(f.vertices)].mean(0) for f in me.polygons if roles[f.index] == RID['wall']]); wall_v = np.concatenate([V[list(f.vertices)] for f in me.polygons if roles[f.index] == RID['wall']])
    bx0, by0 = wall_v[:, :2].min(0); bx1, by1 = wall_v[:, :2].max(0); tx = .5 / DENS + 1e-4
    W_ = srgb(h['wall']); DARK = lambda c: c * .52 + srgb((70, 52, 44)) * .18  # 짙은 선: 같은 색을 따뜻한 갈색 쪽으로 어둡게
    BASE = dict(wall=W_, frame=srgb((244, 240, 230)), roof=srgb(h['roof_c']), flat=srgb(h['roof_c']), soffit=W_ * .80, glass=srgb((58, 98, 104)),
                door=srgb(h['door']), shutter=srgb(h['shutter']), rail=srgb((81, 98, 88)), slab=srgb((214, 210, 192)), stone=srgb((190, 184, 160)),
                ac=srgb((222, 216, 196)), hidden=W_ * .7)

    def light(n, col):
        d = float(n @ SUN); f, tint = (1.0, 0) if d > .45 else (.86, .06) if d > .10 else (.74, .12)
        return col * f * (1 - tint) + TEAL * f * tint
    rects = []; swatch = {}
    for f in me.polygons:
        ps = V[list(f.vertices)]; n = np.array(f.normal); role = ROLES[roles[f.index]]
        if abs(n[2]) < .999: U = np.cross((0, 0, 1.), n); U /= np.linalg.norm(U)
        else: U = np.array((1., 0, 0))
        Vv = np.cross(n, U); Vv /= max(np.linalg.norm(Vv), 1e-9)
        q = np.stack([ps @ U, ps @ Vv], 1); lo_, hi_ = q.min(0), q.max(0)
        w, hgt = (max(1, math.ceil((hi_[k] - lo_[k]) * DENS)) for k in (0, 1))
        r = dict(i=f.index, n=n, U=U, V=Vv, q=q, lo=lo_, hi=hi_, w=w, h=hgt, role=role, pc=pc[f.index], c=ps.mean(0))
        if role == 'hidden' or w < 3 or hgt < 3 or role in ('prop', 'rail'):  # 작은 면 → 같은 색 칸 공유
            base = np.clip(r['pc'], 0, 1) if role == 'prop' else BASE.get(role, W_)
            if role == 'awning': base = srgb(h['awning'][0])
            if role == 'sign': base = srgb(h['sign'][0])
            if role == 'wall': base = DARK(W_)  # 모서리 기둥(0.2m)·창 안쪽 턱 = 짙은 테두리 몫
            col = np.round(np.clip(light(n, base), 0, 1) * 255 / 6) * 6
            key = tuple(col.astype(int)); r['swatch'] = key; swatch.setdefault(key, dict(w=1, h=1, col=col / 255, role=role, faces=[]))['faces'].append(r)
        else: rects.append(r)
    cells = rects + list(swatch.values())
    x = y = GAP; rh = 0
    for r in sorted(cells, key=lambda r: (r['h'], r['w']), reverse=True):
        if x + r['w'] + GAP > AW: x = GAP; y += rh + GAP; rh = 0
        r['x'], r['y'] = x, y; x += r['w'] + GAP; rh = max(rh, r['h'])
    AH = 2 ** math.ceil(math.log2(y + rh + GAP))
    img = np.zeros((AH, AW, 3)); img[:] = W_ * .8; cls = np.zeros((AH, AW), np.uint8); uvl = me.uv_layers['BakedUV']
    for s_ in swatch.values():
        img[s_['y'] - 1:s_['y'] + 2, s_['x'] - 1:s_['x'] + 2] = s_['col']; cls[s_['y'] - 1:s_['y'] + 2, s_['x'] - 1:s_['x'] + 2] = RID[s_['role']]
        for r in s_['faces']:
            for li in me.polygons[r['i']].loop_indices: uvl.data[li].uv = ((s_['x'] + .5) / AW, (s_['y'] + .5) / AH)
    for r in rects:
        w, hh = r['w'], r['h']; I = np.clip(np.arange(-1, w + 1), 0, w - 1)[None, :]; Jj = np.clip(np.arange(-1, hh + 1), 0, hh - 1)[:, None]
        u = r['lo'][0] + I / max(w - 1, 1) * (r['hi'][0] - r['lo'][0]); v = r['lo'][1] + Jj / max(hh - 1, 1) * (r['hi'][1] - r['lo'][1])
        u, v = np.broadcast_arrays(u, v); I, Jj = np.broadcast_arrays(I, Jj)
        n = r['n']; P = u[..., None] * r['U'] + v[..., None] * r['V'] + n * float(r['c'] @ n); z = P[..., 2]; role = r['role']
        edge = (I == 0) | (I == w - 1) | (Jj == 0) | (Jj == hh - 1)
        col = np.broadcast_to(BASE.get(role, W_), u.shape + (3,)).copy(); dark = np.zeros(u.shape, bool); bright = np.zeros(u.shape, bool)
        if role == 'wall':
            if abs(n[1]) > .7: dark |= (np.abs(P[..., 0] - bx0) < tx) | (np.abs(P[..., 0] - bx1) < tx)  # 건물 모서리
            if abs(n[0]) > .7: dark |= (np.abs(P[..., 1] - by0) < tx) | (np.abs(P[..., 1] - by1) < tx)
            dark |= np.abs(z - g['zb']) < tx  # 바닥 닿는 줄
        elif role == 'roof':
            col[(I // 1) % 2 == 1] *= .86  # 기와 줄: 2텍셀 간격 명암
            bright |= Jj == 0  # 처마 끝 밝은 선
        elif role == 'flat': bright |= edge
        elif role == 'glass':
            dark |= edge
            if w >= 4 and hh >= 5: col[(I == 1) & ((Jj == hh - 2) | (Jj == hh - 3))] = srgb((176, 214, 210))  # 반사 2텍셀
            if w >= 9: dark |= I == w // 2  # 겹창 가운데 살
        elif role == 'door':
            dark |= edge
            if w >= 7: dark |= I == w // 2
            if hh >= 8: col[(I == (w - 2 if w < 7 else w // 2 - 1)) & ((Jj == hh // 2) | (Jj == hh // 2 + 1))] = srgb((226, 196, 120))  # 손잡이 2텍셀
        elif role == 'shutter':
            dark |= edge | ((Jj % 3 == 1) & (I > 0) & (I < w - 1))  # 살
        elif role == 'awning':
            a, b = h['awning']; st = (np.floor((P[..., 0] + 10) / .44) % 2)[..., None]; col = srgb(a) * st + srgb(b) * (1 - st); dark |= Jj == 0
        elif role == 'sign':
            a, b = h['sign']; inner = (I > 0) & (I < w - 1) & (Jj > 0) & (Jj < hh - 1); col[:] = srgb(a); col[inner] = srgb(b)
            ci = w // 2
            for k, cj in enumerate(range(hh - 4, 2, -5)):  # 도트 기호: 네모 고리 · 십자 번갈아
                ring = (np.maximum(np.abs(I - ci), np.abs(Jj - cj)) == 1) if k % 2 == 0 else (((I == ci) & (np.abs(Jj - cj) <= 1)) | ((Jj == cj) & (np.abs(I - ci) <= 1)))
                col[ring & inner] = srgb((128, 80, 50))
        elif role == 'stone':
            dark |= (Jj % 3 == 0) | (((I + 3 * ((Jj // 3) % 2)) % 5 == 0))  # 줄눈
        elif role in ('slab', 'ac'): dark |= edge if role == 'ac' else Jj == 0
        col[dark] = DARK(col[dark]); col[bright] = np.clip(col[bright] * 1.18 + .04, 0, 1)
        if role in ('wall', 'frame', 'glass', 'door', 'shutter', 'stone'):  # 처마·발코니·차양 밑, 바닥 0.6m → 한 단 어둡게(흐림 없음)
            sh = (z - g['zb'] < .6) & (role == 'wall')
            for oh in g['overhangs']:
                if oh[0] == 'eave': sh |= (z > oh[1] - .45) & (z < oh[1] + .2)
                else:
                    _, x0, x1, y0, y1, zb_ = oh; sh |= (P[..., 0] >= x0) & (P[..., 0] <= x1) & (P[..., 1] >= y0 - .05) & (P[..., 1] <= y1) & (z < zb_) & (z > zb_ - .9)
            col[sh] = col[sh] * .84 * .9 + TEAL * .84 * .1
        col = light(n, col)
        y0, x0 = r['y'] - 1, r['x'] - 1
        img[y0:y0 + hh + 2, x0:x0 + w + 2] = np.clip(col, 0, 1); cls[y0:y0 + hh + 2, x0:x0 + w + 2] = RID[role]
        for li, qq in zip(me.polygons[r['i']].loop_indices, r['q']):
            uvl.data[li].uv = ((r['x'] + .5 + (qq[0] - r['lo'][0]) / max(r['hi'][0] - r['lo'][0], 1e-8) * (w - 1)) / AW,
                               (r['y'] + .5 + (qq[1] - r['lo'][1]) / max(r['hi'][1] - r['lo'][1], 1e-8) * (hh - 1)) / AH)
    return img, cls, len(rects) + sum(len(s_['faces']) for s_ in swatch.values())


# ---------- d판(과제 1331): 실제 건물 정면 사진 포토배싱 ----------
FAC = J('art', 'source', 'facades-cc0')
SP, MZ = 'commons_Colonial_facade_with_graffiti_in_central_Sao_Paulo.jpg', 'commons_Casa_en_Mazatlán__'
TUS = 'commons_Romantic_Facade_In_Tuscany_Broken_Symmetry__135007059_.jpg'
# 집마다 (정면 사진, 크롭) · (옆·뒤 사진, 크롭). 크롭 = 사진을 400×246 상자에 맞춘 축소판 좌표(x0, y0, x1, y1) — 원본 배율은 불러올 때 계산
PHOTO_D = dict(house_narrow_a=((SP, (100, 0, 225, 228)), (SP, (262, 0, 400, 228))),
               house_narrow_b=(('commons_Barcelona_003.jpg', (30, 10, 160, 236)), (MZ + '9_de_diciembre_de_2022_07.jpg', (60, 0, 180, 215))),
               house_shop=(('commons_Finalborgo__Italy__52404658808_.jpg', (15, 0, 300, 246)), (MZ + '30_de_noviembre_de_2022.jpg', (0, 0, 140, 140))),
               house_slope=(('commons_Venice__Unsplash_.jpg', (30, 0, 300, 246)), ('commons_Casas_en_Mazatlán__30_de_noviembre_de_2022.jpg', (240, 70, 340, 190))),
               house_warehouse=(('commons_Small_pink_colonial_house_with_clay_tile_roof_in_Laguna_de_R.jpg', (65, 106, 265, 196)), (MZ + '9_de_diciembre_de_2022.jpg', (0, 70, 230, 195))),
               house_cafe=((TUS, (0, 60, 368, 246)), (TUS, (230, 60, 368, 166))))
ROOF_D = ['RoofingTiles013A_Color.jpg', 'RoofingTiles011A_Color.jpg']; ROOF_M = 1.5  # 기와 사진 한 장 = 1.5m
LUMA = np.array((.299, .587, .114)); _PH = {}


def ph_load(fn):
    if fn not in _PH:
        im = bpy.data.images.load(os.path.join(FAC, fn)); w, hh = im.size; a = np.empty(w * hh * 4, np.float32); im.pixels.foreach_get(a)
        _PH[fn] = a.reshape(hh, w, 4)[::-1, :, :3].astype(np.float64); bpy.data.images.remove(im)
    return _PH[fn]


def area_resize(c, tw, th):  # 넓이 평균으로 부드럽게 줄이기
    S = np.zeros((c.shape[0] + 1, c.shape[1] + 1, 3)); S[1:, 1:] = c.cumsum(0).cumsum(1)
    ys = np.linspace(0, c.shape[0], th + 1).round().astype(int); xs = np.linspace(0, c.shape[1], tw + 1).round().astype(int)
    y0, y1, x0, x1 = ys[:-1, None], ys[1:, None], xs[None, :-1], xs[None, 1:]
    return (S[y1, x1] - S[y0, x1] - S[y1, x0] + S[y0, x0]) / np.maximum((y1 - y0) * (x1 - x0), 1)[..., None]


def box_blur(L, r):
    for ax in (0, 1):
        P = np.concatenate([np.repeat(L.take([0], ax), r + 1, ax), L, np.repeat(L.take([-1], ax), r, ax)], ax); C = P.cumsum(ax)
        L = (C.take(range(2 * r + 1, C.shape[ax]), ax) - C.take(range(0, C.shape[ax] - 2 * r - 1), ax)) / (2 * r + 1)
    return L


def ph_tex(fn, box, wm, hm, target=.75):
    """사진 크롭 → (hm·DENS, wm·DENS) 텍셀. 넓이 평균 축소 → 큰 그늘·밝은 쪽 눌러 평평한 빛 → 대비 0.85 → 채도 0.8 → 벽 평균 밝기 맞춤 → 따뜻하게."""
    a = ph_load(fn); H, W = a.shape[:2]; s = max(W / 400, H / 246); x0, y0, x1, y1 = [int(round(v * s)) for v in box]
    tw, th = max(1, math.ceil(wm * DENS)), max(1, math.ceil(hm * DENS)); t = area_resize(a[y0:y1, x0:x1], tw, th)
    L = t @ LUMA; Lb = box_blur(L, max(2, min(tw, th) // 3)); t = t * ((L.mean() / np.maximum(Lb, .02)) ** .6)[..., None]
    m = t.mean((0, 1)); t = m + (t - m) * .85; Lc = (t @ LUMA)[..., None]; t = Lc + (t - Lc) * .80;  # 채도 0.8(밝힌 풀이 형광 연두로 뜨지 않게)
    t = t * (target / max(float((t @ LUMA).mean()), .05)); t = t * (1.04, 1.0, .92)
    t = np.where(t > .92, .92 + (t - .92) * .3, t); return np.clip(t, 0, 1)


def simplify_d(o, g):
    """c판 메시에서 벽·창·문·덧창·난간·차양·간판·실외기·높은 발코니판을 빼고 벽 상자 4면으로 바꿈(창·문은 사진 속 것). 지붕·박공·축대·땅 소품은 그대로."""
    me = o.data; roles = np.zeros(len(me.polygons), np.int32); me.attributes['role'].data.foreach_get('value', roles); V = np.array([v.co for v in me.vertices])
    wv = np.concatenate([V[list(f.vertices)] for f in me.polygons if roles[f.index] == RID['wall']]); (bx0, by0), (bx1, by1) = wv[:, :2].min(0), wv[:, :2].max(0)
    zb, Hw = float(g['zb']), float(g['Hw'])
    bm = bmesh.new(); bm.from_mesh(me); rl = bm.faces.layers.int['role']; pl = bm.faces.layers.float_color['pcol']; dl = []
    for f in bm.faces:
        r = ROLES[f[rl]]; zs = [v.co.z for v in f.verts]
        if r in ('glass', 'frame', 'door', 'shutter', 'rail', 'awning', 'sign', 'ac', 'hidden') or (r == 'wall' and min(zs) < Hw - .01) \
                or (r == 'slab' and min(zs) > zb + .3) or (r == 'prop' and min(zs) > zb + .4): dl.append(f)
    bmesh.ops.delete(bm, geom=dl, context='FACES')
    c = [(bx0, by0), (bx1, by0), (bx1, by1), (bx0, by1)]
    for k in range(4):
        (xa, ya), (xb, yb) = c[k], c[(k + 1) % 4]
        f = bm.faces.new([bm.verts.new((xa, ya, zb)), bm.verts.new((xb, yb, zb)), bm.verts.new((xb, yb, Hw)), bm.verts.new((xa, ya, Hw))]); f[rl] = RID['wall']; f[pl] = (1, 1, 1, 1)
    bm.to_mesh(me); bm.free(); me.update(); g['box'] = (float(bx0), float(by0), float(bx1), float(by1))


def pack_paint_d(o, h, g, seed):
    """면마다 10텍셀/m 칸. 벽 = 실제 건물 사진(정면·옆) 투영, 지붕 = 기와 사진, 나머지 = 단색. 빛은 그늘 면만 한 단(80%, 청록 12%)."""
    me = o.data; V = np.array([v.co for v in me.vertices]); roles = np.zeros(len(me.polygons), np.int32); me.attributes['role'].data.foreach_get('value', roles)
    pc = np.zeros(len(me.polygons) * 4, np.float32); me.attributes['pcol'].data.foreach_get('color', pc); pc = pc.reshape(-1, 4)[:, :3]
    bx0, by0, bx1, by1 = g['box']; zb, Hw = float(g['zb']), float(g['Hw']); Wd, Dd, Hh = bx1 - bx0, by1 - by0, Hw - zb
    (ff, fb), (sf, sb) = PHOTO_D[h['name']]
    tg = .86 if h['name'] == 'house_cafe' else .75  # 카페 사진은 문·돌이 짙어 한 단 더 밝힘
    TX = dict(front=ph_tex(ff, fb, Wd, Hh, tg), side=ph_tex(sf, sb, Dd, Hh, tg), back=ph_tex(sf, sb, Wd, Hh, tg))
    top = {k: np.median(t[:max(1, t.shape[0] // 10)].reshape(-1, 3), 0) for k, t in TX.items()}
    T = round(ROOF_M * DENS); roof = area_resize(ph_load(ROOF_D[seed % 2]), T, T)
    roof = roof * (.50 / max(float((roof @ LUMA).mean()), .05)); rm = roof.mean((0, 1)); roof = np.clip(rm + (roof - rm) * .8, 0, 1)
    wall_avg = TX['front'].reshape(-1, 3).mean(0)
    BASE = dict(roof=roof.reshape(-1, 3).mean(0), flat=srgb((196, 184, 160)), soffit=wall_avg * .8, slab=srgb((206, 200, 184)), stone=srgb((184, 176, 152)), hidden=wall_avg * .7)

    def light(n, col):
        return col if float(n @ SUN) > .10 else col * .80 * .88 + TEAL * .80 * .12
    rects = []; swatch = {}
    for f in me.polygons:
        ps = V[list(f.vertices)]; n = np.array(f.normal); role = ROLES[roles[f.index]]
        if abs(n[2]) < .999: U = np.cross((0, 0, 1.), n); U /= np.linalg.norm(U)
        else: U = np.array((1., 0, 0))
        Vv = np.cross(n, U); Vv /= max(np.linalg.norm(Vv), 1e-9)
        q = np.stack([ps @ U, ps @ Vv], 1); lo_, hi_ = q.min(0), q.max(0)
        w, hgt = (max(1, math.ceil((hi_[k] - lo_[k]) * DENS - 1e-6)) for k in (0, 1))
        r = dict(i=f.index, n=n, U=U, V=Vv, q=q, lo=lo_, hi=hi_, w=w, h=hgt, role=role, pc=pc[f.index], c=ps.mean(0))
        if role in ('hidden', 'prop', 'rail') or ((w < 3 or hgt < 3) and role != 'wall'):  # 작은 면 → 같은 색 칸 공유
            base = np.clip(r['pc'], 0, 1) if role == 'prop' else BASE.get(role, wall_avg)
            col = np.round(np.clip(light(n, base), 0, 1) * 255 / 6) * 6
            key = tuple(col.astype(int)); swatch.setdefault(key, dict(w=1, h=1, col=col / 255, role=role, faces=[]))['faces'].append(r)
        else: rects.append(r)
    cells = rects + list(swatch.values()); x = y = GAP; rh = 0
    for r in sorted(cells, key=lambda r: (r['h'], r['w']), reverse=True):
        if x + r['w'] + GAP > AW: x = GAP; y += rh + GAP; rh = 0
        r['x'], r['y'] = x, y; x += r['w'] + GAP; rh = max(rh, r['h'])
    AH = 2 ** math.ceil(math.log2(y + rh + GAP))
    img = np.zeros((AH, AW, 3)); img[:] = wall_avg * .8; cls = np.zeros((AH, AW), np.uint8); uvl = me.uv_layers['BakedUV']
    for s_ in swatch.values():
        img[s_['y'] - 1:s_['y'] + 2, s_['x'] - 1:s_['x'] + 2] = s_['col']; cls[s_['y'] - 1:s_['y'] + 2, s_['x'] - 1:s_['x'] + 2] = RID[s_['role']]
        for r in s_['faces']:
            for li in me.polygons[r['i']].loop_indices: uvl.data[li].uv = ((s_['x'] + .5) / AW, (s_['y'] + .5) / AH)
    for r in rects:  # 텍셀 칸 = 면을 w×hh 로 나눈 칸의 가운데(가장자리 1칸은 번짐 막이로 같은 값)
        w, hh = r['w'], r['h']; I = np.clip(np.arange(-1, w + 1), 0, w - 1)[None, :]; Jj = np.clip(np.arange(-1, hh + 1), 0, hh - 1)[:, None]
        u = r['lo'][0] + (I + .5) / w * (r['hi'][0] - r['lo'][0]); v = r['lo'][1] + (Jj + .5) / hh * (r['hi'][1] - r['lo'][1])
        u, v = np.broadcast_arrays(u, v); I, Jj = np.broadcast_arrays(I, Jj)
        n = r['n']; P = u[..., None] * r['U'] + v[..., None] * r['V'] + n * float(r['c'] @ n); z = P[..., 2]; role = r['role']
        col = np.broadcast_to(BASE.get(role, wall_avg), u.shape + (3,)).copy()
        if role == 'wall':
            if n[1] < -.7: k, uu = 'front', (P[..., 0] - bx0) / Wd
            elif n[1] > .7: k, uu = 'back', (bx1 - P[..., 0]) / Wd
            elif n[0] > .7: k, uu = 'side', (P[..., 1] - by0) / Dd
            else: k, uu = 'side', (by1 - P[..., 1]) / Dd
            t = TX[k]; vv = (z - zb) / Hh
            ci = np.clip((uu * t.shape[1]).astype(int), 0, t.shape[1] - 1); ri = np.clip(((1 - vv) * t.shape[0]).astype(int), 0, t.shape[0] - 1)
            col = t[ri, ci]; col[vv > 1.001] = top[k]  # 박공·난간벽 = 사진 맨 위 띠의 중앙값
        elif role == 'roof':
            col = roof[T - 1 - np.floor((v - r['lo'][1]) * DENS).astype(int) % T, np.floor((u - r['lo'][0]) * DENS).astype(int) % T]
        elif role == 'stone':
            col[(Jj % 4 == 0) | ((I + 4 * ((Jj // 4) % 2)) % 7 == 0)] *= .72
        col = light(n, col)
        y0, x0 = r['y'] - 1, r['x'] - 1
        img[y0:y0 + hh + 2, x0:x0 + w + 2] = np.clip(col, 0, 1); cls[y0:y0 + hh + 2, x0:x0 + w + 2] = RID[role]
        for li, qq in zip(me.polygons[r['i']].loop_indices, r['q']):
            uvl.data[li].uv = ((r['x'] + (qq[0] - r['lo'][0]) / max(r['hi'][0] - r['lo'][0], 1e-8) * w) / AW,
                               (r['y'] + (qq[1] - r['lo'][1]) / max(r['hi'][1] - r['lo'][1], 1e-8) * hh) / AH)
    return img, cls, len(rects) + sum(len(s_['faces']) for s_ in swatch.values())


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
    if VARIANT == 'd': simplify_d(o, g)
    img, cls, nf = (pack_paint_d if VARIANT == 'd' else pack_paint_c if VARIANT == 'c' else pack_paint)(o, h, g, k * 13 + 5)
    tris = sum(len(p.vertices) - 2 for p in o.data.polygons)
    pts = np.array([v.co for v in o.data.vertices]); lo, hi = pts.min(0), pts.max(0)
    built.append((h, o, img, cls, g, tris, nf, lo, hi)); print('BUILT', h['name'], 'W', g['W'], 'D', g['D'], 'Hw', round(g['Hw'], 2), 'tris', tris, 'tex', img.shape[1], img.shape[0], flush=True)
# 합친 64색 → 이 집들이 많이 쓰는 32색
union = list(dict.fromkeys(read_gpl(J('art', 'export', 'scene-a2-kit', 'palette32.gpl')) + read_gpl(J('art', 'export', 'scene-a2-bldg-light', 'palette32.gpl'))))
cnt = np.zeros(len(union))
for b in built:
    idx = nearest(b[2], union); vis = b[3] != RID['hidden']; cnt += np.bincount(idx[vis], minlength=len(union))
pal = [union[i] for i in np.argsort(-cnt)[:32]]
if VARIANT == 'b':  # 1248: 공용 64색엔 살구·황토 벽 단계가 없다 → 공용 많이 쓴 20색 + 벽·처마밑 픽셀 k-means 12색
    cool = lambda c: c[1] >= c[0] and (c[0] * .299 + c[1] * .587 + c[2] * .114) > 140  # 밝은 회녹 = 벽 얼룩이 붙던 색
    pal = [union[i] for i in np.argsort(-cnt) if not cool(union[i])][:20]
    wp = np.concatenate([b[2][np.isin(b[3], [RID['wall'], RID['soffit'], RID['frame']])] for b in built]) * 255
    rng = np.random.default_rng(5); wp = wp[rng.choice(len(wp), min(60000, len(wp)), replace=False)]
    cen = wp[rng.choice(len(wp), 12, replace=False)]
    for _ in range(25):
        lab = ((wp[:, None] - cen[None]) ** 2).sum(2).argmin(1); cen = np.array([wp[lab == i].mean(0) if (lab == i).any() else cen[i] for i in range(12)])
    pal += [tuple(int(round(v)) for v in c) for c in cen]
if VARIANT == 'c':  # 1302: b판 공용 32색 바탕 — 덜 쓰이는 10색만 c판 칠에서 오차 큰 픽셀의 k-means 10색으로 바꿈(같은 색이 계단을 잃지 않게)
    pal = read_gpl(J('art', 'export', 'scene-a4-houses-b', 'palette32.gpl')); P0 = np.array(pal, np.float64)
    vp = np.concatenate([b[2][b[3] != RID['hidden']] for b in built]) * 255
    d2 = ((vp[:, None] - P0[None]) ** 2).sum(2); use = np.bincount(d2.argmin(1), minlength=32); err = np.sqrt(d2.min(1))
    wp = vp[err > 6]; rng = np.random.default_rng(5); wp = wp[rng.choice(len(wp), min(60000, len(wp)), replace=False)]
    cen = wp[rng.choice(len(wp), 10, replace=False)]
    for _ in range(25):
        lab = ((wp[:, None] - cen[None]) ** 2).sum(2).argmin(1); cen = np.array([wp[lab == i].mean(0) if (lab == i).any() else cen[i] for i in range(10)])
    keep = sorted(np.argsort(-use)[:22]); pal = [pal[i] for i in keep] + [tuple(int(round(v)) for v in c) for c in cen]
    print('PAL c: keep 22, new 10, err>6 px %', round(float((err > 6).mean() * 100), 1), flush=True)
if VARIANT == 'd':  # 1331: 사진 6채 텍셀 전체에서 k-means 32색 = 이 6채 공용 팔레트(디더 없음)
    vp = np.concatenate([b[2][b[3] != RID['hidden']] for b in built]) * 255; rng = np.random.default_rng(7); vp = vp[rng.choice(len(vp), min(80000, len(vp)), replace=False)]
    cen = vp[rng.choice(len(vp), 32, replace=False)]
    for _ in range(30):
        lab = ((vp[:, None] - cen[None]) ** 2).sum(2).argmin(1); cen = np.array([vp[lab == i].mean(0) if (lab == i).any() else cen[i] for i in range(32)])
    pal = [tuple(int(round(v)) for v in c) for c in cen[np.argsort(cen @ np.array((.299, .587, .114)))]]
open(os.path.join(OUT, 'palette32.gpl'), 'w').write('GIMP Palette\nName: scene-a4-houses' + ('-b (공용 20 + 따뜻한 벽 12)' if VARIANT == 'b' else '-c (b판 32 중 22 + c판 칠 10)' if VARIANT == 'c' else '-d (사진 6채 k-means 32)' if VARIANT == 'd' else ' (아트1 키트 + 건물 밝히기 합집합 중 32)') + '\nColumns: 8\n#\n' +
                                                    ''.join(f'{r} {g_} {b_} c{i}\n' for i, (r, g_, b_) in enumerate(pal)))
kit = dict(task={'a': '20261027-1232', 'b': '20261027-1248', 'c': '20261027-1302', 'd': '20261027-1331'}[VARIANT], note='도현 승인 전. F1/Assets 에 넣지 않음. Unity 임포트 미확인. 원점 = 집 바닥(축대 집은 축대 바닥) 가운데.',
           axis='Blender: 정면 -Y, 위 +Z → FBX axis_forward -Z, axis_up Y, 1u = 1m', method=__doc__.split('이번 방법 —')[1].split('출력:')[0].strip(),
           sun_dir=SUN.round(3).tolist(), palette='palette32.gpl', texels_per_m=DENS,
           license='CC0 — elegantcrow Retro Modular House(모듈) · valsekamerplant PSX house plants(화분) · Poly Haven 사진(worn_plaster_wall, clay_roof_tiles, wood_peeling_paint_weathered, stone_block_wall, rough_linen). '
                   'house_shop 만 + CC-BY 3.0 "Vending Machine" by dook via Poly Pizza (게임 크레딧 필요).',
           files=[])
meas = {}
for h, o, img, cls, g, tris, nf, lo, hi in built:
    idx = nearest(img, pal) if VARIANT == 'c' else despeckle(nearest(img, pal)); q = np.array(pal, np.float64)[idx] / 255  # c: 1텍셀 선이 지워지지 않게 반점 지우기 안 함(측정만)
    png = os.path.join(OUT, h['name'] + '.png'); im = save_png(q, png); np.save(os.path.join(WORK, h['name'] + '_cls.npy'), cls[::-1])
    Lm = q @ np.array((.299, .587, .114)) * 255; vis = cls != RID['hidden']
    by = {r: round(float(Lm[cls == RID[r]].mean()), 1) for r in ROLES if (cls == RID[r]).any()}
    meas[h['name']] = dict(L_all=round(float(Lm.mean()), 1), L_visible=round(float(Lm[vis].mean()), 1), L_wall=by.get('wall'), by_role=by,
                           p5_visible=round(float(np.percentile(Lm[vis], 5)), 1), under40_visible_pct=round(float((Lm[vis] < 40).mean() * 100), 3),
                           prop_p5=round(float(np.percentile(Lm[cls == RID['prop']], 5)), 1) if (cls == RID['prop']).any() else None,
                           speckle_1px_pct=round(speckle(idx), 3), colors=int(len(np.unique(idx))), size=[img.shape[1], img.shape[0]],
                           wall_colors=int(len(np.unique(idx[cls == RID['wall']]))), texels_per_m=DENS)
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
