# artlive 1709: 정비소 마무리 — 현재 garage.blend(1653 반영본)를 열어
#  ① 바닥: floor(콘크리트 사진 칸)·apron 이 기울어 절반이 땅 밑(z −0.055~0.075)이라 스튜디오 땅이 비쳤다 → 평평하게 z 0.03 / 0.02
#     앞마당 흙: dirt_v5_combo.fbx 의 흙 쿼드(16x40m)를 앞 12m 로 잘라(UV 도 같은 비율) z 0.01 에 붙임, 재질 Garage_Dirt
#  ③ 포스터 A/B/C·기록판·슬롯판: 옛 아틀라스 칸(21~32px/m) → garage_posters.png(64px/m, 픽셀 글씨) 칸으로 UV 옮김
# 그 뒤 garage.blend 저장 → 합쳐서 garage_v5_combo.fbx (LAMP 빈 객체 포함). 1653 스크립트는 다시 돌리지 않는다(이미 반영됨).
# D:/Tools/Blender/blender.exe -b --factory-startup -P art/source/q2-garage/fix_1709.py
import bpy, os, sys
from mathutils import Matrix, Vector
R = 'D:/Github/F1Game'; E = R + '/art/export/q2-garage'
bpy.ops.wm.open_mainfile(filepath=R + '/art/source/q2-garage/garage.blend')
def mat(name, img):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name); m.use_backface_culling = False
    m.use_nodes = True; nt = m.node_tree; bs = next(n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED')
    t = nt.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(img, check_existing=True); t.interpolation = 'Closest'
    nt.links.new(t.outputs[0], bs.inputs['Base Color']); return m
def flat(o, z):
    mi = o.matrix_world.inverted()
    for v in o.data.vertices:
        w = o.matrix_world @ v.co; w.z = z; v.co = mi @ w
    print('FLAT', o.name, z)
flat(bpy.data.objects['floor'], 0.03); flat(bpy.data.objects['apron'], 0.02)

# 앞마당 흙
if 'yard_dirt' not in bpy.data.objects:
    before = set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=E + '/dirt_v5_combo.fbx')
    new = [o for o in bpy.data.objects if o not in before]; d = next(o for o in new if o.type == 'MESH')
    for o in new:
        if o is not d: bpy.data.objects.remove(o)
    d.parent = None; d.data = d.data.copy(); d.data.transform(d.matrix_world); d.matrix_world = Matrix.Identity(4); d.name = 'yard_dirt'
    me = d.data; uv = me.uv_layers.active.data; Y0, Y1 = 0.0, -12.0
    ys = [v.co.y for v in me.vertices]; ymin = min(ys)
    # uv = 위치(x,y)의 1차식으로 맞춘 뒤(평면 투영 UV), 잘린 꼭짓점의 uv 를 그 식으로 다시 계산 → 텍셀 밀도 그대로
    import numpy as np
    L = [(me.vertices[me.loops[li].vertex_index].co, uv[li].uv) for p in me.polygons for li in p.loop_indices]
    A = np.array([[c.x, c.y, 1.0] for c, _ in L]); B = np.array([[t.x, t.y] for _, t in L]); K = np.linalg.lstsq(A, B, rcond=None)[0]
    print('DIRT fit err', float(np.abs(A @ K - B).max()), 'loops', len(L))
    for p in me.polygons:
        for li in p.loop_indices:
            c = me.vertices[me.loops[li].vertex_index].co
            if c.y < Y1: uv[li].uv = tuple(np.array([c.x, Y1, 1.0]) @ K)
    for v in me.vertices:
        if v.co.y < Y1: v.co.y = Y1
        v.co.z = 0.01
    me.materials.clear(); me.materials.append(mat('Garage_Dirt', E + '/dirt_baked32.png')); d['q2_added'] = 1
    print('DIRT', d.name, 'ymin was', round(ymin, 2), [tuple(round(c, 2) for c in v.co) for v in me.vertices])

# 포스터 → garage_posters.png 칸
PW = 256; cells = {'posterA': (0, 0, 64, 96), 'posterB': (64, 0, 128, 96), 'posterC': (128, 0, 173, 67), 'record': (0, 96, 128, 160), 'slots': (128, 96, 256, 160)}
POST = mat('Garage_Posters_Photo', E + '/garage_posters.png')
for nm, (x0, y0, x1, y1) in cells.items():
    o = bpy.data.objects[nm]; o.data = o.data.copy(); me = o.data; uv = me.uv_layers.active.data
    # 옛 UV 는 칸 안에서 180° 돌아 있던 것이 있어(대칭 블록이라 안 보였다) 기하로 다시 깐다: v = 월드 위(Z), u = 앞에서 본 오른쪽(up × n)
    n = (o.matrix_world.to_3x3() @ me.polygons[0].normal).normalized(); rt = Vector((0, 0, 1)).cross(n).normalized()
    P = {li: o.matrix_world @ me.vertices[me.loops[li].vertex_index].co for li in range(len(me.loops))}
    a = [P[i].dot(rt) for i in P]; z = [P[i].z for i in P]; a0, a1, z0, z1 = min(a), max(a), min(z), max(z)
    for li, l in enumerate(uv):
        l.uv = (x0 / PW + (P[li].dot(rt) - a0) / (a1 - a0) * (x1 - x0) / PW, 1 - y1 / PW + (P[li].z - z0) / (z1 - z0) * (y1 - y0) / PW)
    me.materials.clear(); me.materials.append(POST); print('POSTER', nm, len(me.polygons))

# 1717: 레이스 사진 3장·간판 뒷면이 좌우 반전(앞에서 볼 때 U 가 왼쪽으로 커짐) → 그 면만 U 를 면 가운데 기준으로 뒤집음(다시 돌려도 안전)
for o in [bpy.data.objects[n] for n in ('race_photo_0', 'race_photo_1', 'race_photo_2', 'sign_main')]:
    me = o.data; uv = me.uv_layers.active.data
    for p in me.polygons:
        n = (o.matrix_world.to_3x3() @ p.normal).normalized()
        if abs(n.z) > 0.9: continue
        rt = Vector((0, 0, 1)).cross(n).normalized(); L = list(p.loop_indices)
        P = [o.matrix_world @ me.vertices[me.loops[i].vertex_index].co for i in L]
        if sum((uv[L[k]].uv.x - uv[L[0]].uv.x) * (P[k] - P[0]).dot(rt) for k in range(len(L))) < -1e-6:
            us = [uv[i].uv.x for i in L]; c = min(us) + max(us)
            for i in L: uv[i].uv.x = c - uv[i].uv.x
            print('UNFLIP', o.name, p.index)

bpy.ops.wm.save_as_mainfile(filepath=R + '/art/source/q2-garage/garage.blend')
obs = [o for o in bpy.context.scene.objects if o.type == 'MESH' and not o.get('q2_before_only') and not o.hide_render]
print('AFTER export-set polys', sum(len(o.data.polygons) for o in obs), 'objs', len(obs))
bpy.ops.object.select_all(action='DESELECT')
for o in obs: o.select_set(True)
bpy.context.view_layer.objects.active = obs[0]; bpy.ops.object.duplicate(); bpy.ops.object.join(); j = bpy.context.object; j.name = 'garage_v5_combo'
bpy.context.scene.cursor.location = (0, 0, 0); bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
cnt = {}
for p in j.data.polygons: cnt[j.data.materials[p.material_index].name] = cnt.get(j.data.materials[p.material_index].name, 0) + 1
print('JOINED', len(j.data.polygons), cnt)
for e in [bpy.data.objects['LAMP_0'], bpy.data.objects['LAMP_1']]: e.select_set(True)
bpy.ops.export_scene.fbx(filepath=E + '/garage_v5_combo.fbx', use_selection=True, object_types={'MESH', 'EMPTY'}, axis_forward='-Z', axis_up='Y', path_mode='COPY', bake_anim=False)
print('EXPORT ok'); sys.stdout.flush(); os._exit(0)
