# artlive 1653: garage.blend 객체 목록(이름·면·재질·경계·꼭짓점 수·평균 법선 방향 검사)
import bpy
bpy.ops.wm.open_mainfile(filepath='D:/Github/F1Game/art/source/q2-garage/garage.blend')
for o in sorted(bpy.context.scene.objects, key=lambda o: o.name):
    if o.type != 'MESH': print('OBJ', o.type, o.name); continue
    me = o.data; P = [o.matrix_world @ v.co for v in me.vertices]
    lo = [round(min(p[i] for p in P), 2) for i in range(3)]; hi = [round(max(p[i] for p in P), 2) for i in range(3)]
    m = [s.material.name if s.material else '?' for s in o.material_slots]
    print('MESH', o.name, len(me.polygons), lo, hi, m, 'flags', o.get('q2_added'), o.get('q2_before_only'), o.hide_render)
