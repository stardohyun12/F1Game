"""정비소·고카트 v5 (아트2, 2026-10-27) — 판정 단위 = 장면 "매일 아침 출발하는 정비소 앞".
v4 스크립트(opus_garage_kart_v4.py)를 읽어 SHAPE(D-166/D-167) 값을 끼우고, 카트는 새로(아이용 카트 형태), 정비소 앞 소품 10여 개를 더한다.
렌더 = 게임 카메라(카트 뒤 높이 2.8m·거리 5.6m·세로 FOV 64) + 소개 컷, 1280×720. 그 뒤 FBX(Y-up, 1unit=1m) 내보내기.

    python -X utf8 tools/asset-gen/opus_kit_tex.py
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_garage_kart_v5.py
    python -X utf8 tools/asset-gen/opus_garage_sheet_v5.py
"""
import bpy, math, os, json
from mathutils import Vector, Matrix
from bpy_extras.object_utils import world_to_camera_view

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, 'opus_garage_kart_v4.py')
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
OUT = os.path.join(ROOT, 'art', '_review', '2026-10-27-garage-kart-opus-v5')
EXP = os.path.join(ROOT, 'art', 'export', 'garage-kart-v5')

code = open(SRC, encoding='utf-8').read()
code = code[:code.rindex('\nmain()')]
PATCH = [
    ("'opus-garage-kart', 'tex_v4')", "'opus-scene-a-kit', 'tex')"),  # v4 텍스처 + 키트 텍스처가 함께 든 폴더
    # SHAPE: 처마 = 벽(3.2)의 0.28 → 0.9m(입구 1.2m). 지붕 두께는 main 에서 Solidify 0.6m(0.19)
    ("a0, a1 = arch(y=-.6, w=W2 + .35, lift=-.05), arch(y=D + .4, w=W2 + .35, lift=-.05)",
     "a0, a1 = arch(y=-1.2, w=W2 + .9, lift=-.2), arch(y=D + .9, w=W2 + .9, lift=-.2)"),
    ("ra = arch(y=-.62, w=W2 + .38, lift=-.02)", "ra = arch(y=-1.22, w=W2 + .93, lift=-.17)"),
    ("rows[1] = [(x, 4.0, z) for", "rows[1] = [(x, 4.0, z + .1) for"),
    # 기둥 굵게 + 두 개가 서로 다르게 기움
    ("lean = (0, .035, 0) if (sx, y) == (-1, 0) else None", "lean = {(-1, 0): (0, .06, 0), (1, 0): (-.04, -.035, 0)}.get((sx, y))"),
    ("(.18, .18, HW), 'wood', skip='bt', tilt=lean)", "(.3, .3, HW), 'wood', skip='bt', tilt=lean)"),
    # 셔터 열림: 미닫이문을 뺀다(셔터 말림통은 extras)
    ("for nm, cx in (('doorL', -4.3), ('doorR', 4.45)):", "for nm, cx in ():"),
    # 소품 1.3배
    ("cyl(f'drum{i}', (x, y, .45), .3, .9, 8", "cyl(f'drum{i}', (x, y, .58), .39, 1.17, 8"),
    ("cyl('drum_lying', (6.3, -2.4, .3), .3, .9, 8", "cyl('drum_lying', (6.6, -2.8, .39), .39, 1.17, 8"),
    ("(x + (.03 if k % 2 else 0), y, .11 + .22 * k), .33, .22, 8", "(x + (.05 if k % 2 else 0), y, .14 + .28 * k), .43, .28, 8"),
    ("box('crate1', (4.3, -1.2, .3), (.6, .6, .6)", "box('crate1', (6.6, -1.4, .39), (.78, .78, .78)"),
    ("box('crate2', (4.35, -1.15, .8), (.45, .45, .4)", "box('crate2', (6.65, -1.35, 1.04), (.58, .58, .52)"),
    # v4 나무는 빼고 extras 에서 벽의 3배·건물에서 띄워 다시 심는다
    ("for i, (x, y, s) in enumerate(((-9, 5, 1.0),", "for i, (x, y, s) in enumerate(() and ((-9, 5, 1.0),"),
]
for a, b in PATCH:
    assert a in code, a
    code = code.replace(a, b)
ns = {'__file__': SRC, '__name__': 'v5'}
exec(compile(code, SRC, 'exec'), ns)
box, cyl, mk, quad, CUR, COLL = ns['box'], ns['cyl'], ns['mk'], ns['quad'], ns['CUR'], ns['COLL']
W2, D, HW, HA, arch = ns['W2'], ns['D'], ns['HW'], ns['HA'], ns['arch']


# ---------------------------------------------------------------- 카트 v5: 아이용 카트(낮은 튜브 틀·노즈 판·큰 뒷바퀴·등받이·옆 엔진)
def kart_v5(loc, rot):
    CUR[0] = 'kart'; obs0 = set(bpy.data.objects)
    M = 'metal'
    for sx in (-1, 1):
        box(f'k_rail{sx}', (sx * .26, 0, .1), (.07, 1.3, .07), M)  # 튜브 틀(굵게 — SHAPE 1.5배)
        box(f'k_nerf{sx}', (sx * .53, .03, .15), (.08, .58, .08), M)  # 옆 보호대
        for y in (-.18, .24): box(f'k_nerfc{sx}{y}', (sx * .4, y, .13), (.26, .06, .06), M, skip='lr')
        box(f'k_bstrut{sx}', (sx * .2, .76, .13), (.06, .24, .06), M, skip='fk')
        box(f'k_rup{sx}', (sx * .3, -.8, .18), (.06, .06, .16), M, skip='bt')
    box('k_pan', (0, .05, .075), (.48, 1.0, .03), M, skip='b')
    box('k_bumper', (0, .9, .15), (.74, .1, .1), M)  # 앞 범퍼 두껍게(v4 .06 → .10)
    box('k_rbumper', (0, -.88, .25), (.96, .09, .09), M)
    # 앞 노즈 판: 앞이 낮고 뒤가 솟은 쐐기, 칠 벗겨진 몸 텍스처
    V = [(-.3, .48, .1), (.3, .48, .1), (.26, .86, .1), (-.26, .86, .1), (-.3, .48, .34), (.3, .48, .34), (.22, .86, .2), (-.22, .86, .2)]
    mk('k_nose', V, [(0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7), (4, 5, 6, 7)], 'kart_body', uv='fit')  # 판정 1차: 무지 면 → 칠 벗겨짐·녹 전체가 한 면에
    quad('k_numF', [(-.11, .867, .11), (.11, .867, .11), (.11, .867, .19), (-.11, .867, .19)], 'sticker', uv='fit')
    for sx in (-1, 1):  # 노즈 옆 번호
        p = [(sx * .29, .56, .14), (sx * .29, .74, .14), (sx * .29, .74, .26), (sx * .29, .56, .26)]
        quad(f'k_numS{sx}', p if sx > 0 else p[::-1], 'sticker', uv='fit')
    # 좌석: 바닥 + 높은 등받이(뒤로 젖힘) + 옆 날개
    box('k_seat', (0, -.24, .19), (.4, .36, .08), 'seat', uv='fit', skip='b')
    box('k_back', (0, -.45, .45), (.42, .08, .5), 'seat', uv='fit', tilt=(-.32, 0, 0))
    for sx in (-1, 1): box(f'k_wing{sx}', (sx * .23, -.3, .3), (.05, .34, .2), 'seat', uv='fit', tilt=(-.15, 0, 0))
    p0, p1 = Vector((0, .44, .2)), Vector((0, .18, .5)); dv = p1 - p0; th = math.atan2(-dv.y, dv.z)
    box('k_col', tuple((p0 + p1) / 2), (.045, .045, dv.length), M, skip='bt', tilt=(th, 0, 0))
    cyl('k_wheel', tuple(p1), .15, .04, 8, 'seat', cap=M, tilt=(th, 0, 0))
    # 바퀴: 앞 작게, 뒤 크게·넓게
    for (x, y, r, w) in ((.44, .58, .14, .12), (-.44, .58, .14, .12), (.53, -.52, .21, .22), (-.53, -.52, .21, .22)):
        cyl(f'k_tire{x}{y}', (x, y, r), r, w, 8, 'tire', axis='x', cap='tire')
        cyl(f'k_hub{x}{y}', (x + (w / 2 + .01) * (1 if x > 0 else -1), y, r), r * .5, .03, 6, M, axis='x', cap=M, rot=0 if x > 0 else math.pi)
        if x < 0: bpy.data.objects[f'k_tire{x}{y}_cap'].location.x -= w
    box('k_axle', (0, -.52, .21), (1.0, .06, .06), M)
    # 엔진: 왼쪽 뒤에 드러남 — 블록 + 냉각핀 3장 + 빨강 시동 손잡이 덮개 + 오른쪽으로 빠지는 배기관
    box('k_eng', (-.3, -.64, .33), (.24, .26, .26), M, skip='b')
    for k in range(3): box(f'k_fin{k}', (-.3, -.64, .49 + k * .045), (.28 - k * .03, .3 - k * .03, .025), M)
    box('k_ftank', (-.3, -.66, .64), (.24, .22, .13), 'drum_red', uv='fit', rect=(0, .2, .5, .8))  # 엔진 위 빨강 연료통 = 실루엣 표지
    cyl('k_starter', (-.44, -.62, .36), .1, .06, 8, 'drum_red', axis='x', cap='drum_top', rot=math.pi)
    box('k_carb', (-.17, -.56, .34), (.06, .08, .08), M)
    box('k_chain', (-.3, -.5, .2), (.04, .3, .12), 'kart_body', skip='b')
    cyl('k_exh', (.02, -.82, .32), .045, .5, 6, M, axis='x')
    cyl('k_muf', (.3, -.8, .32), .07, .18, 6, M, axis='x', cap='drum_top')
    # 짐: 오른쪽 뒤 작은 받침 + 작은 상자 + 끈
    box('k_rack', (.22, -.72, .36), (.28, .24, .03), M, skip='b')
    box('k_crate', (.22, -.72, .46), (.24, .2, .16), 'crate', uv='fit', skip='b', rot=.08)
    box('k_strap', (.22, -.72, .46), (.04, .21, .17), 'strap', uv='fit', skip='bfk', rot=.08)
    quad('k_plate', [(.1, -.93, .2), (-.1, -.93, .2), (-.1, -.93, .3), (.1, -.93, .3)], 'sticker', uv='fit')
    rootM = Matrix.Translation(loc) @ Matrix.Rotation(rot, 4, 'Z')
    bpy.context.view_layer.update()
    for ob in set(bpy.data.objects) - obs0: ob.matrix_world = rootM @ ob.matrix_world
    return rootM


# ---------------------------------------------------------------- 정비소 덧붙임 + 정비소 앞 소품
def roof_z(x):  # 지붕 아랫면 높이(옆벽 위 빈틈 메우기용)
    a = arch(w=W2 + .9, lift=-.2)
    for (x0, _, z0), (x1, _, z1) in zip(a, a[1:]):
        if x1 <= abs(x) <= x0: return z0 + (z1 - z0) * (x0 - abs(x)) / (x0 - x1)
    return HW


def garage_extras():
    CUR[0] = 'garage'
    zr = roof_z(W2)
    for sx in (-1, 1):  # 옆벽 위 ~ 지붕 사이 함석 띠(처마를 키우면 생기는 틈)
        p = [(sx * W2, -.0, HW), (sx * W2, D, HW), (sx * W2, D, zr), (sx * W2, 0, zr)]
        quad(f'wall_top{sx}', p if sx > 0 else p[::-1], 'tin_wall')
    # 셔터: 말림통 + 아래 끝 쇠막대 + 양옆 레일 (열림)
    box('sh_box', (0, -.3, HW + .05), (7.5, .5, .42), 'tin_wall', tilt=(0, .012, 0))
    cyl('sh_roll', (0, -.3, HW - .22), .2, 7.2, 8, 'tin_roof', axis='x')
    box('sh_lip', (0, -.32, HW - .45), (7.1, .1, .1), 'metal')
    for sx in (-1, 1): box(f'sh_guide{sx}', (sx * 3.62, -.16, HW / 2), (.12, .14, HW), 'metal', skip='bt')
    # 간판: 입구 위 박공 앞, 살짝 기움
    box('sign_main', (0, -.42, 4.15), (3.3, .14, 1.05), 'sign_shop', uv='fit', tilt=(0, .025, 0))
    for sx in (-1, 1): box(f'sign_br{sx}', (sx * 1.3, -.3, 4.15), (.08, .3, .08), 'metal')
    # 사람 문(오른쪽 판자벽): 2.0m — 사람 1.6 ÷ 문 = 0.8 (D-167)
    box('pdoor', (4.3, -.05, 1.0), (.9, .08, 2.0), 'planks_dark', skip='b')
    box('pdoor_fr', (4.3, -.04, 2.06), (1.06, .1, .12), 'wood', skip='b')
    box('pdoor_knob', (3.98, -.12, 1.0), (.06, .06, .06), 'metal')


def tree(nm, x, y, H, leafm, lean=(0, 0)):
    box(f'{nm}_trunk', (x, y, .25 * H), (.12 * H / 2.4, .12 * H / 2.4, .5 * H), 'wood', skip='b', tilt=(lean[0], lean[1], 0))
    for j, (dx, dy, z, r) in enumerate(((0, 0, .66, .26), (.2, .12, .55, .2), (-.18, -.12, .78, .19), (.06, .16, .86, .14))):
        R = r * H  # 판정 1차: 마름모 판 → 8각 두 고리 덩이(둥근 왕관)
        ring = lambda rr, z, ph: [(rr * math.cos(k * math.pi / 4 + ph), rr * math.sin(k * math.pi / 4 + ph), z + .08 * R * (k % 2)) for k in range(8)]
        V = [(0, 0, -.6 * R)] + ring(.75 * R, -.3 * R, j) + ring(R, .15 * R, j + .4) + ring(.6 * R, .55 * R, j) + [(.1 * R, 0, .8 * R)]
        F = [(0, (k + 1) % 8 + 1, k + 1) for k in range(8)] + [(25, 17 + k, 17 + (k + 1) % 8) for k in range(8)]
        for a in (1, 9):
            F += [(a + k, a + (k + 1) % 8, a + 8 + (k + 1) % 8, a + 8 + k) for k in range(8)]
        mk(f'{nm}_crown{j}', V, F, leafm, loc=(x + dx * H + lean[1] * H * .6, y + dy * H - lean[0] * H * .6, z * H))


def wire(nm, p0, p1, sag, n=6):
    for j in range(n):
        a, b = p0.lerp(p1, j / n), p0.lerp(p1, (j + 1) / n)
        a.z -= sag * math.sin(math.pi * j / n); b.z -= sag * math.sin(math.pi * (j + 1) / n)
        d = b - a
        box(f'{nm}{j}', tuple((a + b) / 2), (.04, d.length, .04), 'metal', skip='fk',
            tilt=(math.atan2(d.z, math.hypot(d.x, d.y)), 0, 0), rot=math.atan2(-d.x, d.y))


def props():
    CUR[0] = 'props'
    # 기운 나무 전신주(7°) + 가로대 + 애자 + 전선(정비소 모서리로 1가닥, 길 따라 2가닥)
    lean = (.05, -.11)
    P = Vector((-6.1, .4, 0))  # 게임 컷 왼쪽 끝 안(수평 반각 48°, 카트 -10° 회전)
    box('pole', tuple(P + Vector((3.8 * math.sin(lean[1]), -3.8 * math.sin(lean[0]), 3.8))), (.3, .3, 7.6), 'wood', skip='b', tilt=(lean[0], lean[1], 0))
    tip = Vector((P.x + 7.2 * math.sin(lean[1]), P.y - 7.2 * math.sin(lean[0]), 7.2))  # 기둥 꼭대기 근처(기운 쪽으로)
    box('pole_arm', tuple(tip), (2.0, .16, .16), 'wood', tilt=(lean[0], .04, 0))
    for k, dx in enumerate((-.8, 0, .8)):
        box(f'insul{k}', tuple(tip + Vector((dx, 0, .16))), (.1, .1, .16), 'porcelain', uv='fit')
    wire('wA', tip + Vector((.8, 0, .2)), Vector((-W2 - .3, -.4, 3.5)), .45)
    wire('wB', tip + Vector((-.8, 0, .2)), Vector((-6.5, -45, 7.0)), 1.6, 8)
    wire('wC', tip + Vector((0, 0, .2)), Vector((-5.7, -45, 7.0)), 1.9, 8)
    wire('wD', tip + Vector((0, 0, .2)), Vector((-8.5, 30, 7.0)), 1.4, 8)
    # 나무 2그루: 벽(3.2)의 3배 ≈ 9.6m, 건물에서 띄움 + 덤불
    tree('treeL', -11.5, 3.5, 9.6, 'leaf_o', lean=(0, .04))
    tree('treeR', 11.0, 9.5, 9.0, 'leaf_y', lean=(.03, -.03))
    tree('bush', -8.2, -3.0, 1.6, 'leaf')
    # 밖 작업대(왼쪽 벽 밖): 상판·다리·바이스·올려둔 타이어·공구통
    box('ob_top', (-5.75, 2.6, .95), (.8, 1.9, .1), 'wood', rot=.03)
    for dx in (-.3, .3):
        for dy in (-.8, .8): box(f'ob_leg{dx}{dy}', (-5.75 + dx, 2.6 + dy, .45), (.1, .1, .9), 'wood', skip='bt')
    box('ob_vise', (-5.7, 1.9, 1.1), (.24, .32, .2), 'metal', skip='b')
    cyl('ob_tire', (-5.75, 3.05, 1.12), .4, .26, 8, 'tire', cap='drum_top', tilt=(.12, 0, 0))
    box('ob_tool', (-5.8, 2.3, 1.1), (.3, .45, .22), 'drum_red', uv='fit', rect=(0, .25, .5, .7), skip='b')
    # 눕힌 드럼 반대쪽 뚜껑(v4 는 한쪽만 막혀 고리처럼 보였다)
    dv = Vector((math.cos(.5), math.sin(.5), 0)) * .585
    cyl('drum_lid', tuple(Vector((6.6, -2.8, .39)) - dv), .39, .01, 8, 'drum_red', axis='x', cap='drum_top', rot=.5 + math.pi)
    # 길 표지판: 다리 2개, 판 폭 1.8m(사람 1.1배), 게임 컷 오른쪽
    # 판정 1차 "소품이 진열처럼 일렬" → 표지판을 길 쪽 앞으로 빼고 더 돌림, 상자는 엇갈려 쌓고 하나는 땅에
    for sx in (-1, 1): box(f'rs_leg{sx}', (4.4 + sx * .7, -4.9 + sx * .32, 1.1), (.16, .16, 2.2), 'wood', skip='b', rot=.42, tilt=(0, sx * .03, 0))
    box('rs_board', (4.4, -4.95, 1.75), (1.8, .1, .9), 'sign_road', uv='fit', rot=.42, tilt=(0, -.06, 0))
    box('crate3', (5.6, -2.2, .3), (.62, .62, .6), 'crate', uv='fit', skip='b', rot=.6)
    cyl('tire_loose', (2.9, -3.4, .14), .43, .28, 8, 'tire', cap='tire', rot=.3)  # 길가에 굴러나온 타이어 하나
    # 자전거: 왼쪽 앞 판자벽에 기댐(벽과 나란히) — 로컬은 +y 길이·+x 쪽 벽, 끝에 90° 돌림
    obs0 = set(bpy.data.objects)
    bx, by = 0, 0
    tl = (.0, .16, 0)
    for k, dy in enumerate((-.55, .55)):
        cyl(f'bk_wheel{k}', (bx + .02, by + dy, .44), .44, .05, 8, 'tire', axis='x', tilt=tl)
    for nm, c, s, t in (('bk_top', (bx + .08, by, .86), (.05, 1.0, .05), (.16, 0, 0)), ('bk_down', (bx + .06, by + .1, .62), (.05, .9, .05), (-.75, 0, 0)),
                        ('bk_seatp', (bx + .1, by - .3, .82), (.05, .05, .6), (-.3, 0, 0)), ('bk_fork', (bx + .07, by + .5, .7), (.05, .05, .6), (.25, 0, 0))):
        box(nm, c, s, 'paint_teal', uv='fit', tilt=(t[0], .16, 0))
    box('bk_seat', (bx + .16, by - .38, 1.12), (.14, .26, .07), 'seat', uv='fit', tilt=(0, .16, 0))
    box('bk_bar', (bx + .18, by + .45, 1.08), (.62, .05, .05), 'metal', tilt=(0, .16, 0))
    box('bk_basket', (bx + .2, by + .62, .98), (.36, .28, .22), 'crate', uv='fit', skip='t', tilt=(0, .16, 0))
    bpy.context.view_layer.update()
    BM = Matrix.Translation((-4.35, -.5, 0)) @ Matrix.Rotation(math.pi / 2, 4, 'Z') @ Matrix.Translation((-.3, 0, 0))
    for ob in set(bpy.data.objects) - obs0: ob.matrix_world = BM @ ob.matrix_world
    # 화분 줄(오른쪽 모서리 밖) — 크기·회전 제각각
    for k, (x, s, m) in enumerate(((5.45, 1.0, 'leaf'), (5.85, .85, 'leaf_y'), (5.5, 1.1, 'leaf'))):
        y0 = -.45 if k < 2 else .1
        box(f'pot{k}', (x, y0, .2 * s), (.36 * s, .36 * s, .4 * s), 'planter', uv='fit', skip='b', rot=k * .2)
        R = .26 * s
        V = [(0, 0, -.3 * R)] + [(R * math.cos(i * math.pi / 3 + k), R * math.sin(i * math.pi / 3 + k), .1 * R * (i % 2)) for i in range(6)] + [(0, 0, .9 * R)]
        F = [(0, (i + 1) % 6 + 1, i + 1) for i in range(6)] + [(7, i + 1, (i + 1) % 6 + 1) for i in range(6)]
        mk(f'pot{k}_leaf', V, F, m, loc=(x, y0, .4 * s + .12))
    # 사장(사람 자리표시): 1.6m, 머리 0.4(1/4), 사람 문 옆
    px, py, pr = 3.55, -1.05, -.5
    for nm, c, s in (('p_legL', (-.09, 0, .36), (.14, .16, .72)), ('p_legR', (.09, 0, .36), (.14, .16, .72)), ('p_body', (0, 0, .97), (.46, .28, .5)),
                     ('p_armL', (-.29, 0, .9), (.11, .14, .48)), ('p_armR', (.29, 0, .9), (.11, .14, .48)), ('p_head', (0, 0, 1.4), (.4, .38, .4))):
        w = Matrix.Translation((px, py, 0)) @ Matrix.Rotation(pr, 4, 'Z') @ Vector(c)
        box(nm, tuple(w), s, {'p_body': 'apron', 'p_head': 'paint_cream'}.get(nm, 'person'), uv='fit', rot=pr)
    box('p_cap', (px, py, 1.63), (.44, .42, .07), 'paint_red', uv='fit', rot=pr)


# ---------------------------------------------------------------- 카메라·렌더·내보내기
KART_AT, KART_ROT = Vector((.7, -2.3, 0)), math.radians(-10)
FWD = Vector((-math.sin(KART_ROT), math.cos(KART_ROT), 0))
SIDE = Vector((FWD.y, -FWD.x, 0))
VIEWS = {  # 이름: (위치, 바라볼 곳, 세로 FOV°)
    'x1_game': (KART_AT - FWD * 5.6 + Vector((0, 0, 2.8)), KART_AT + FWD * 4 + Vector((0, 0, .5)), 64),
    'x2_intro': (KART_AT - FWD * 12.5 - SIDE * 4.5 + Vector((0, 0, 4.2)), Vector((0, 2.5, 2.2)), 50),
    'k1_34': (KART_AT + FWD * 1.7 + SIDE * 1.6 + Vector((0, 0, 1.0)), KART_AT + Vector((0, 0, .25)), 40),
    'k2_side': (KART_AT - SIDE * 2.6 - FWD * .3 + Vector((0, 0, .7)), KART_AT + Vector((0, 0, .25)), 40),
}


def camera(name, loc, tgt, fov):
    c = bpy.data.cameras.new(name); c.sensor_fit = 'VERTICAL'; c.angle_y = math.radians(fov); c.clip_end = 1000
    ob = bpy.data.objects.new(name, c); bpy.context.scene.collection.objects.link(ob)
    ob.location = loc; ob.rotation_euler = (Vector(tgt) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
    return ob


def tris(cname):
    dg = bpy.context.evaluated_depsgraph_get(); t = 0
    for ob in COLL[cname].objects:
        me = ob.evaluated_get(dg).to_mesh(); me.calc_loop_triangles(); t += len(me.loop_triangles)
    return t


def export(cname, path, M):
    """M = 원점(바닥 중심)으로 옮기는 행렬. Y-up, 1unit=1m. 재질을 Principled+이미지(Point)로 바꿔 FBX 가 텍스처를 들고 가게."""
    bpy.ops.object.select_all(action='DESELECT')
    for ob in COLL[cname].objects:
        ob.matrix_world = M @ ob.matrix_world; ob.select_set(True)
    bpy.context.view_layer.objects.active = COLL[cname].objects[0]
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
                             apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True, use_mesh_modifiers=True,
                             mesh_smooth_type='FACE', path_mode='COPY', embed_textures=False)
    for ob in COLL[cname].objects: ob.matrix_world = M.inverted() @ ob.matrix_world


def to_principled():
    for m in bpy.data.materials:
        if not m.use_nodes: continue
        img = next((n.image for n in m.node_tree.nodes if n.type == 'TEX_IMAGE'), None)
        if img is None: continue
        nt = m.node_tree; nt.nodes.clear()
        t = nt.nodes.new('ShaderNodeTexImage'); t.image = img; t.interpolation = 'Closest'
        b = nt.nodes.new('ShaderNodeBsdfPrincipled'); b.inputs['Roughness'].default_value = 1.0
        o = nt.nodes.new('ShaderNodeOutputMaterial')
        nt.links.new(t.outputs['Color'], b.inputs['Base Color']); nt.links.new(b.outputs[0], o.inputs[0])


def main():
    for ob in list(bpy.data.objects): bpy.data.objects.remove(ob)
    ns['scene_setup'](); ns['garage'](); garage_extras()
    roof = bpy.data.objects['roof']
    sm = roof.modifiers.new('thick', 'SOLIDIFY'); sm.thickness = .6; sm.offset = 0.0  # 지붕 끝 두께 0.6m = 벽의 0.19
    sh = Matrix.Identity(4); sh[0][2] = .026  # 정비소 1.5° 기울임(완벽한 직각 금지)
    bpy.context.view_layer.update()
    for ob in COLL['garage'].objects: ob.matrix_world = sh @ ob.matrix_world
    rootM = kart_v5(KART_AT, KART_ROT); ns['environment'](); props()
    meta = dict(garage_tris=tris('garage'), kart_tris=tris('kart'), props_tris=tris('props'), env_tris=tris('env'), views={}, kart_px={})
    s = bpy.context.scene; os.makedirs(os.path.join(OUT, '_raw'), exist_ok=True)
    # 판정 1차 "그림자가 없다" → 그림자 패스 2장(낮은 해 그림자 on/off, 흰 확산 재질)을 같이 렌더, 시트에서 나눠 그늘만 청록으로 곱한다
    wm = bpy.data.materials.new('shadow_white'); wm.use_nodes = True
    wm.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (1, 1, 1, 1)
    wm.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = 1.0
    sun_d = bpy.data.lights.new('sun_sh', 'SUN'); sun_d.energy = 3.0; sun_d.angle = 0.0
    sun_o = bpy.data.objects.new('sun_sh', sun_d); s.collection.objects.link(sun_o)
    sun_o.rotation_euler = Vector((.5, -.6, .4)).to_track_quat('Z', 'Y').to_euler()  # 고도 ≈ 27° — 늦가을 오후의 긴 그림자
    if s.world is None: s.world = bpy.data.worlds.new('w')
    s.world.use_nodes = True; bg = s.world.node_tree.nodes.get('Background')
    sun_o.hide_render = True
    for k, (loc, tgt, fov) in VIEWS.items():
        cam = camera(k, loc, tgt, fov); s.camera = cam; bpy.context.view_layer.update()
        s.render.filepath = os.path.join(OUT, '_raw', k + '.png'); bpy.ops.render.render(write_still=True)
        bgv = tuple(bg.inputs['Strength'].default_value for _ in (0,)) if bg else None
        if bg: bg.inputs['Strength'].default_value = 0.0
        bpy.context.view_layer.material_override = wm; sun_o.hide_render = False
        for tag, shd in (('sh', True), ('ns', False)):
            sun_d.use_shadow = shd
            s.render.filepath = os.path.join(OUT, '_raw', f'{k}_{tag}.png'); bpy.ops.render.render(write_still=True)
        bpy.context.view_layer.material_override = None; sun_o.hide_render = True
        if bg: bg.inputs['Strength'].default_value = bgv[0]
        p = world_to_camera_view(s, cam, KART_AT + Vector((0, 0, .3)))
        meta['kart_px'][k] = [round(p.x * 1920), round((1 - p.y) * 1080)]
        meta['views'][k] = dict(loc=list(loc), tgt=list(tgt), fov_v=fov, dist_to_kart=round((Vector(loc) - KART_AT).length, 2))
    json.dump(meta, open(os.path.join(OUT, 'render_meta.json'), 'w'), indent=1)
    os.makedirs(os.path.join(ROOT, 'art', 'models'), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT, 'art', 'models', 'opus_garage_kart_v5.blend'))
    os.makedirs(EXP, exist_ok=True); to_principled()
    G = Matrix.Translation((0, -D / 2, 0))  # 정비소 바닥 중심 = 원점
    export('garage', os.path.join(EXP, 'garage_v5.fbx'), G)
    export('props', os.path.join(EXP, 'garage_props_v5.fbx'), G)  # 정비소 원점 기준 배치 그대로
    export('kart', os.path.join(EXP, 'kart_v5.fbx'), rootM.inverted())
    print('META', json.dumps({k: v for k, v in meta.items() if k not in ('views',)}))


main()
