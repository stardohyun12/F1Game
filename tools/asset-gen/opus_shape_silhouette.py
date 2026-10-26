"""형태 언어(D-166 B) — 정비소 v4 흑실루엣 전/후. v4 스크립트를 그대로 읽고, after 는 docs/design/SHAPE.md 규칙으로 고친 값을 끼운다.

    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_shape_silhouette.py -- before
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_shape_silhouette.py -- after
"""
import bpy, math, os, sys, json
from mathutils import Vector, Matrix

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, 'opus_garage_kart_v4.py')
MODE = sys.argv[sys.argv.index('--') + 1] if '--' in sys.argv else 'before'
OUT = os.path.join(HERE, '..', '..', 'art', '_review', '2026-10-27-shape-language', '_raw')

code = open(SRC, encoding='utf-8').read()
code = code[:code.rindex('\nmain()')] if '\nmain()' in code else code

# SHAPE.md 규칙을 v4 값에 끼운다 (after 만). (찾을 문자열, 바꿀 문자열, 규칙)
PATCH = [
    # 지붕: 처마 돌출 0.35m → 1.1m(벽 높이의 1/3), 앞 1.6m. 지붕 두께는 아래 Solidify 0.55m(벽의 1/6)
    ("a0, a1 = arch(y=-.6, w=W2 + .35, lift=-.05), arch(y=D + .4, w=W2 + .35, lift=-.05)",
     "a0, a1 = arch(y=-1.6, w=W2 + 1.1, lift=-.3), arch(y=D + 1.1, w=W2 + 1.1, lift=-.3)"),
    ("ra = arch(y=-.62, w=W2 + .38, lift=-.02)", "ra = arch(y=-1.62, w=W2 + 1.13, lift=-.27)"),
    ("rows[1] = [(x, 4.0, z) for", "rows[1] = [(x, 4.0, z + .12) for"),  # 지붕 줄마다 높이를 어긋나게(비뚤어짐)
    # 기둥 0.18 → 0.34 (뭉툭)
    ("(.18, .18, HW), 'wood', skip='bt', tilt=lean)", "(.34, .34, HW), 'wood', skip='bt', tilt=lean)"),
    # 드럼·타이어·상자 1.35배 (소품 ÷ 사람 = 실제의 1.3~1.5배)
    ("cyl(f'drum{i}', (x, y, .45), .3, .9, 8", "cyl(f'drum{i}', (x, y, .6), .4, 1.2, 8"),
    ("cyl('drum_lying', (6.3, -2.4, .3), .3, .9, 8", "cyl('drum_lying', (6.3, -2.6, .4), .4, 1.2, 8"),
    ("(x + (.03 if k % 2 else 0), y, .11 + .22 * k), .33, .22, 8", "(x + (.06 if k % 2 else 0), y, .14 + .28 * k), .44, .28, 8"),
    ("box('crate1', (4.3, -1.2, .3), (.6, .6, .6)", "box('crate1', (4.3, -1.3, .42), (.85, .85, .85)"),
    ("box('crate2', (4.35, -1.15, .8), (.45, .45, .4)", "box('crate2', (4.4, -1.2, 1.12), (.62, .62, .55)"),
    # 카트: 범퍼 0.06 → 0.15, 바퀴 1.3배, 옆 통 굵게
    ("box('k_bumper', (0, .86, .14), (.82, .06, .06)", "box('k_bumper', (0, .88, .16), (.96, .16, .14)"),
    ("box('k_rbumper', (0, -.74, .16), (.86, .06, .06)", "box('k_rbumper', (0, -.78, .18), (1.0, .14, .14)"),
    ("(.14, .72, .14), 'kart_body'", "(.22, .8, .22), 'kart_body'"),
    ("((.42, .55, .13, .12), (-.42, .55, .13, .12), (.44, -.5, .15, .16), (-.44, -.5, .15, .16))",
     "((.46, .55, .17, .16), (-.46, .55, .17, .16), (.5, -.5, .21, .22), (-.5, -.5, .21, .22))"),
    # 나무: 집(5.6m)의 1.6~2.1배, 줄기 굵게
    ("(-8.5, 14, .9), (9.5, 11, 1.2), (12, 4, .9), (-13, -4, .8))):",
     "(-8.5, 14, .9), (9.5, 11, 1.2), (12, 4, .9), (-13, -4, .8))):\n        s *= 2.0"),
    ("(.3 * s, .3 * s, 2.4 * s)", "(.5 * s, .5 * s, 2.4 * s)"),
]
if MODE == 'after':
    for a, b in PATCH:
        assert a in code, a
        code = code.replace(a, b)

ns = {'__file__': SRC, '__name__': 'shape'}
exec(compile(code, SRC, 'exec'), ns)


def extras():
    """after 에만: 기운 전신주+전선, 굵은 다리 두 개 표지판, 기운 울타리, 사람(머리 1/4)."""
    box, cyl, CUR = ns['box'], ns['cyl'], ns['CUR']
    CUR[0] = 'env'
    box('pole', (-3.6, -7.5, 3.8), (.3, .3, 7.6), 'wood', skip='b', tilt=(.04, -.09, 0))  # 5° 기움
    box('pole_arm', (-3.25, -7.2, 7.1), (1.8, .16, .16), 'wood', tilt=(.04, -.09, 0))
    for k, dz in enumerate((0, -.35)):  # 전선: 전신주 → 정비소 앞 처짐
        p0, p1 = Vector((-2.6 - k * .5, -7.25, 7.15 + dz)), Vector((-1.5 - k * .5, -1.0, 4.9 + dz))
        for j in range(4):
            a, b = p0.lerp(p1, j / 4), p0.lerp(p1, (j + 1) / 4)
            a.z -= .5 * math.sin(math.pi * j / 4); b.z -= .5 * math.sin(math.pi * (j + 1) / 4)
            d = b - a
            box(f'wire{k}{j}', tuple((a + b) / 2), (.05, d.length, .05), 'metal',
                tilt=(math.atan2(d.z, math.hypot(d.x, d.y)), 0, 0), rot=math.atan2(-d.x, d.y))
    for dx in (-.55, .55):
        box(f'sign_post{dx}', (6.4 + dx, -4.6, 1.0), (.16, .16, 2.0), 'wood', skip='b', tilt=(0, .04 * (1 if dx > 0 else -1), 0))
    box('sign_board', (6.4, -4.62, 2.05), (1.7, .12, .95), 'crate', tilt=(0, .05, 0))
    for i, x in enumerate((-9.5, -8.4, -7.2, -6.1, -5.0)):  # 울타리 기둥 기울기 ±4~9°
        box(f'fence{i}', (x, -2.2, .6), (.16, .16, 1.2), 'wood', skip='b', tilt=((.07, -.12, .1, -.05, .14)[i], (.09, 0, -.08, .05, 0)[i], 0))
    for z in (.45, .9):
        box(f'rail{z}', (-7.25, -2.25, z + (.06 if z > .5 else 0)), (4.9, .08, .1), 'wood', tilt=(0, .03 if z > .5 else -.02, 0))
    # 사람 1.6m, 머리 0.4m(키의 1/4) — ①⑫ 비율
    box('p_legs', (-1.2, -4.6, .35), (.34, .22, .7), 'wood', skip='b')
    box('p_body', (-1.2, -4.6, .93), (.48, .3, .46), 'wood')
    box('p_head', (-1.2, -4.6, 1.38), (.44, .4, .44), 'wood')


def main():
    for ob in list(bpy.data.objects): bpy.data.objects.remove(ob)
    ns['scene_setup'](); ns['garage'](); ns['kart'](ns['KART_AT'], ns['KART_ROT']); ns['environment']()
    if MODE == 'after':
        roof = bpy.data.objects['roof']
        sm = roof.modifiers.new('thick', 'SOLIDIFY'); sm.thickness = .55; sm.offset = -1.0  # 지붕 두께 0.55m
        # 정비소 전체를 2° 비튼다(완벽한 직각 금지): x += 0.035·z
        sh = Matrix.Identity(4); sh[0][2] = .035
        bpy.context.view_layer.update()
        for ob in ns['COLL']['garage'].objects: ob.matrix_world = sh @ ob.matrix_world
        extras()
    for nm in ('ground', 'dirt', 'sea', 'quay', 'floor', 'apron'):
        if nm in bpy.data.objects: bpy.data.objects.remove(bpy.data.objects[nm])
    blk = bpy.data.materials.new('BLACK'); blk.use_nodes = True; nt = blk.node_tree; nt.nodes.clear()
    em = nt.nodes.new('ShaderNodeEmission'); em.inputs['Color'].default_value = (0, 0, 0, 1)
    nt.links.new(em.outputs[0], nt.nodes.new('ShaderNodeOutputMaterial').inputs[0])
    for ob in bpy.data.objects:
        if ob.type == 'MESH': ob.data.materials.clear(); ob.data.materials.append(blk)
    bpy.context.view_layer.update()
    s = bpy.context.scene; os.makedirs(OUT, exist_ok=True)
    tris = {}
    dg = bpy.context.evaluated_depsgraph_get()
    for c in ('garage', 'kart', 'env'):
        t = 0
        for ob in ns['COLL'][c].objects:
            me = ob.evaluated_get(dg).to_mesh(); me.calc_loop_triangles(); t += len(me.loop_triangles)
        tris[c] = t
    for k in ('g1_front', 'g2_34', 'x1_game'):
        loc, tgt, lens = ns['VIEWS'][k]
        cam = ns['camera'](k, loc, tgt, lens); s.camera = cam
        s.render.filepath = os.path.join(OUT, f'{MODE}_{k}.png'); bpy.ops.render.render(write_still=True)
    json.dump(tris, open(os.path.join(OUT, f'{MODE}_tris.json'), 'w'))
    print('TRIS', MODE, tris)


main()
