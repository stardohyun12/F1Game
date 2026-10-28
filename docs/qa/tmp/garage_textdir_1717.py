# artlive 1717: 글씨 면 방향 검사 — 앞(법선 쪽)에서 볼 때 U 가 오른쪽(up×n)으로, V 가 위(Z)로 커지면 OK. 아니면 좌우/상하 반전.
import bpy, os, sys
from mathutils import Vector
bpy.ops.wm.open_mainfile(filepath='D:/Github/F1Game/art/source/q2-garage/garage.blend')
for o in bpy.context.scene.objects:
    if o.type != 'MESH' or not any(k in o.name for k in ('poster', 'record', 'slots', 'festival', 'sign', 'race_photo', 'board', 'photo')): continue
    me = o.data; uv = me.uv_layers.active.data
    for p in me.polygons[:8]:
        n = (o.matrix_world.to_3x3() @ p.normal).normalized()
        if abs(n.z) > 0.9: continue
        rt = Vector((0, 0, 1)).cross(n).normalized(); L = list(p.loop_indices)
        P = [o.matrix_world @ me.vertices[me.loops[i].vertex_index].co for i in L]; T = [uv[i].uv for i in L]
        # 면 안에서 오른쪽·위 방향으로의 UV 변화(최소제곱 대신 꼭짓점 쌍 중 가장 긴 것)
        du_r = sum((T[k].x - T[0].x) * (P[k] - P[0]).dot(rt) for k in range(len(L))); dv_z = sum((T[k].y - T[0].y) * (P[k] - P[0]).z for k in range(len(L)))
        du_z = sum((T[k].x - T[0].x) * (P[k] - P[0]).z for k in range(len(L)))
        ok = 'OK' if du_r > 0 and dv_z > 0 else ('ROT90' if abs(du_z) > abs(du_r) else 'FLIP')
        print('TXT', o.name, 'face', p.index, 'n', [round(c, 2) for c in n], ok, round(du_r, 4), round(dv_z, 4))
sys.stdout.flush(); os._exit(0)
