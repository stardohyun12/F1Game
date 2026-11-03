# 2230: 남은 겹친 쌍의 덩어리 위치(blender 좌표 bbox) 출력
import bpy, json, sys, os
R = 'D:/Github/F1Game'
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=R + '/art/export/q4-postoffice/q4_postoffice.fbx')
d = json.load(open(R + '/docs/qa/tmp/fix_1846_postoffice_fix.json', encoding='utf-8'))['after']['list']
src = open(R + '/tools/asset-gen/fix_1846.py', encoding='utf-8').read(); i0 = src.index('def islands'); i1 = src.index('def tris_world'); exec(src[i0:i1])
for a, ia, b, ib, ov, cnt, n in d:
    for on, rid in ((a, ia), (b, ib)):
        o = bpy.data.objects[on]; root = islands(o.data); P = [o.matrix_world @ v.co for v in o.data.vertices if root[v.index] == rid]
        print('ISL', on, rid, [round(min(p[k] for p in P), 3) for k in range(3)], [round(max(p[k] for p in P), 3) for k in range(3)], 'ov', ov, 'n', n)
sys.stdout.flush(); os._exit(0)
