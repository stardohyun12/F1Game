# artlive 1653 0단계: 원통 옆면 검정·벽 아래 띠 톱니 원인 — 법선 바깥 비율, UV 가 가리키는 아틀라스 색, 재질 뒷면 컬링(glTF doubleSided)
import bpy, bmesh
from mathutils import Vector
bpy.ops.wm.open_mainfile(filepath='D:/Github/F1Game/art/source/q2-garage/garage.blend')
img = bpy.data.images.load('D:/Github/F1Game/art/export/q2-garage/garage_baked32.png'); W, H = img.size; px = list(img.pixels)
def col(u, v):
    x = min(W - 1, max(0, int(u % 1 * W))); y = min(H - 1, max(0, int(v % 1 * H))); i = (y * W + x) * 4; return px[i:i + 3]
for m in bpy.data.materials: print('MAT', m.name, 'backface_culling', m.use_backface_culling)
for nm in ['tA0', 'tA0_cap', 'drum0', 'drum0_cap', 'can0', 'tLean', 'wall_-1_planks_dark', 'wall_1_planks_dark', 'pegboard']:
    o = bpy.data.objects.get(nm)
    if not o: print('NONE', nm); continue
    me = o.data; c = sum((o.matrix_world @ v.co for v in me.vertices), Vector()) / len(me.vertices)
    uv = me.uv_layers.active; outn = 0; lum = []; uvs = []
    for p in me.polygons:
        pc = o.matrix_world @ p.center; nw = (o.matrix_world.to_3x3() @ p.normal).normalized()
        d = pc - c; d.z = 0 if nm.startswith(('t', 'drum', 'can')) else d.z
        if d.length > 1e-4 and nw.dot(d.normalized()) > 0: outn += 1
        for li in p.loop_indices:
            u = uv.data[li].uv; uvs.append((round(u.x, 3), round(u.y, 3))); r, g, b = col(u.x, u.y); lum.append(0.3 * r + 0.59 * g + 0.11 * b)
    us = sorted(set(uvs))
    print('OBJ', nm, 'polys', len(me.polygons), 'normals_out', outn, 'uv_lum min/mean', round(min(lum), 3), round(sum(lum) / len(lum), 3), 'uv_span', us[0], us[-1], 'n_uv', len(us))
import os, sys; sys.stdout.flush(); os._exit(0)
