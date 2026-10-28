# artlive 1857: .blend 오브젝트 목록(이름·종류·재질·크기·면 수·덩어리 수) — 낱개 분리 계획용
# blender -b --factory-startup <file.blend> -P docs/qa/tmp/list_blend_1857.py
import bpy, sys, os
for o in sorted(bpy.data.objects, key=lambda o: o.name):
    if o.type != 'MESH':
        print('OBJ', o.name, o.type); continue
    me = o.data; ws = [o.matrix_world @ v.co for v in me.vertices]
    lo = [round(min(w[i] for w in ws), 2) for i in range(3)] if ws else 0; hi = [round(max(w[i] for w in ws), 2) for i in range(3)] if ws else 0
    print('OBJ', o.name, len(me.polygons), [m.name if m else None for m in me.materials][:4], lo, hi, 'hide' if o.hide_render or o.hide_get() else '')
sys.stdout.flush(); os._exit(0)
