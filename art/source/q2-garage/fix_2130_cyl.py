# artlive 2130: 원통 16면(교훈 5). 의자 다리·등받이 기둥·보강대(8각)와 엔진 플라이휠(6각)을 같은 크기 16각 원통으로 바꿔 garage.blend 에 저장.
#  - 축 = 메시 로컬 bbox 에서 나머지 둘과 크기가 다른 축, 반지름 = 나머지 두 축 반폭(타원 그대로), 중심 = bbox 가운데.
#  - UV = 원래 UV 사각형 안에 그대로(옆면: 둘레 → u, 높이 → v / 뚜껑: 원판을 사각형 안에) → 같은 텍스처 자리를 같은 밀도로 씀.
#  - 한 번만(표시 q2_cyl16_2130b — 첫 판 q2_cyl16_2130 은 옆면 UV 를 둘레째 욱여넣어 텍셀 F 라 다시). build_asset.py garage 의 overlap 단계가 부른다.
# D:/Tools/Blender/blender.exe -b --factory-startup -P art/source/q2-garage/fix_2130_cyl.py
import bpy, bmesh, math, os, re
R = 'D:/Github/F1Game'; N = 16
bpy.ops.wm.open_mainfile(filepath=R + '/art/source/q2-garage/garage.blend')
done = []
for o in bpy.data.objects:
    if o.type != 'MESH' or o.get('q2_cyl16_2130b') or not re.match(r'^(owner_chair_leg|chair_backpost|chair_repair|engine_fly)', o.name): continue
    me = o.data; uvl = me.uv_layers.active.data
    us = [uvl[i].uv for p in me.polygons for i in p.loop_indices]
    u0, v0, u1, v1 = min(u.x for u in us), min(u.y for u in us), max(u.x for u in us), max(u.y for u in us)
    vs = [v.co for v in me.vertices]; lo = [min(v[i] for v in vs) for i in range(3)]; hi = [max(v[i] for v in vs) for i in range(3)]
    ext = [hi[i] - lo[i] for i in range(3)]; c = [(hi[i] + lo[i]) / 2 for i in range(3)]
    ax = max(range(3), key=lambda i: min(abs(ext[i] - ext[j]) for j in range(3) if j != i))  # 다른 둘과 가장 다른 축
    a1, a2 = [i for i in range(3) if i != ax]; r1, r2 = ext[a1] / 2, ext[a2] / 2
    def P(t, h):
        p = [0.0, 0.0, 0.0]; p[ax] = c[ax] + h * ext[ax] / 2; p[a1] = c[a1] + r1 * math.cos(t); p[a2] = c[a2] + r2 * math.sin(t); return p
    bm = bmesh.new(); uv = bm.loops.layers.uv.new('UVMap')
    ring = [[bm.verts.new(P(2 * math.pi * k / N, h)) for k in range(N)] for h in (-1, 1)]
    img = next((n.image for m in me.materials if m and m.use_nodes for n in m.node_tree.nodes if n.type == 'TEX_IMAGE' and n.image), None)
    asp = img.size[1] / img.size[0] if img and img.size[0] else 1.0  # H/W
    fw = 2 * math.pi * (r1 + r2) / 2 / N * (v1 - v0) / ext[ax] * asp  # 옆면 한 칸 너비(UV) = 세로와 같은 px/m
    fw = min(fw, u1 - u0)
    for k in range(N):  # 옆면: 칸마다 사각형 안에 제 너비로(둘레를 사각형 너비에 욱여넣으면 밀도가 1/5 로 떨어짐)
        k2 = (k + 1) % N; f = bm.faces.new((ring[0][k], ring[0][k2], ring[1][k2], ring[1][k]))
        us0 = u0 + (k * fw) % max(u1 - u0 - fw, 1e-9)
        for l, (uu, vv) in zip(f.loops, ((0, 0), (1, 0), (1, 1), (0, 1))): l[uv].uv = (us0 + uu * fw, v0 + vv * (v1 - v0))
    for h, rr in ((0, ring[0][::-1]), (1, ring[1])):  # 뚜껑
        f = bm.faces.new(rr)
        for l in f.loops:
            q = l.vert.co; l[uv].uv = (u0 + (0.5 + 0.5 * (q[a1] - c[a1]) / r1) * (u1 - u0), v0 + (0.5 + 0.5 * (q[a2] - c[a2]) / r2) * (v1 - v0))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mats = list(me.materials); old = me.uv_layers.active.name
    me.clear_geometry(); bm.to_mesh(me); bm.free()
    for i in range(len(me.uv_layers) - 1, -1, -1):
        if me.uv_layers[i].name != 'UVMap': me.uv_layers.remove(me.uv_layers[i])
    me.uv_layers[0].name = old
    if not me.materials: [me.materials.append(m) for m in mats]
    for p in me.polygons: p.use_smooth = False; p.material_index = 0
    o['q2_cyl16_2130b'] = 1; done.append((o.name, 'axis' + 'XYZ'[ax], round(r1, 3), round(r2, 3)))
print('CYL16', done)
bpy.ops.wm.save_as_mainfile(filepath=R + '/art/source/q2-garage/garage.blend'); os._exit(0)
