# artlive 1709: 바닥처럼 납작하고 넓은 객체 + 포스터 UV 칸 잘라 보기(아틀라스 확대 → docs/qa/tmp/posters_1709_before.png)
import bpy, os, sys
bpy.ops.wm.open_mainfile(filepath='D:/Github/F1Game/art/source/q2-garage/garage.blend')
for o in bpy.context.scene.objects:
    if o.type != 'MESH': continue
    P = [o.matrix_world @ v.co for v in o.data.vertices]; lo = [min(p[i] for p in P) for i in range(3)]; hi = [max(p[i] for p in P) for i in range(3)]
    if hi[2] - lo[2] < 0.06 and lo[2] < 0.1 and (hi[0] - lo[0]) * (hi[1] - lo[1]) > 2:
        print('FLOOR', o.name, len(o.data.polygons), [round(v, 2) for v in lo], [round(v, 2) for v in hi], [s.material.name for s in o.material_slots], 'hide', o.hide_render, dict(o.items()) if o.keys() else '')
for nm in ['posterA', 'posterB', 'posterC', 'record', 'slots']:
    o = bpy.data.objects[nm]; us = [l.uv for l in o.data.uv_layers.active.data]
    P = [o.matrix_world @ v.co for v in o.data.vertices]
    print('PUV', nm, round(min(u.x for u in us), 4), round(min(u.y for u in us), 4), round(max(u.x for u in us), 4), round(max(u.y for u in us), 4), 'pos', [round(sum(p[i] for p in P) / len(P), 2) for i in range(3)], 'n', [round(c, 2) for c in (o.matrix_world.to_3x3() @ o.data.polygons[0].normal)])
sys.stdout.flush(); os._exit(0)
