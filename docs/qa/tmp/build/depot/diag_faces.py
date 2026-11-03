# 2058 진단: 남은 위반 물체의 면별 재질·월드 면적·UV 밀도·UV 박스(px)
import bpy, bmesh, sys, math
from mathutils import Vector
FBX = sys.argv[sys.argv.index('--') + 1]
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=FBX)
SZ = {'Shared_trim': 512, 'Shared_wall': 512, 'Shared_icons': 80, 'Clock_dial': 32}
for o in bpy.data.objects:
    if o.type != 'MESH' or not any(o.name.startswith(p) for p in ('CART_01', 'CLOCK', 'Q3_sign', 'COUNTER_01')): continue
    me = o.data; bm = bmesh.new(); bm.from_mesh(me); uvl = bm.loops.layers.uv.active; M = o.matrix_world
    print('##', o.name, [m.name for m in me.materials])
    for f in bm.faces:
        mn = me.materials[f.material_index].name
        S = next((v for k, v in SZ.items() if mn.lower().startswith(k.lower())), 0)
        if not S or not ('trim' in mn or 'icons' in mn or 'Clock' in mn): continue
        W = [M @ v.co for v in f.verts]; uv = [l[uvl].uv for l in f.loops]
        wa = sum(((W[0] - W[k]).cross(W[0] - W[k + 1])).length / 2 for k in range(1, len(W) - 1))
        sa = abs(sum(uv[k].x * uv[(k + 1) % len(uv)].y - uv[(k + 1) % len(uv)].x * uv[k].y for k in range(len(uv)))) / 2
        e = [(W[(k + 1) % len(W)] - W[k]).length for k in range(len(W))]
        px = [(round(q.x * S, 1), round((1 - q.y) * S, 1)) for q in uv]
        print(f"  {mn[:12]:12s} wa {wa:.4f} dd {math.sqrt(sa * S * S / max(wa, 1e-9)):6.1f} edges {max(e):.3f}x{min(e):.3f} uvx {min(p[0] for p in px)}-{max(p[0] for p in px)} uvy {min(p[1] for p in px)}-{max(p[1] for p in px)}")
    bm.free()
import json, os
for c in json.load(open('D:/Github/F1Game/art/export/q3-depot/layout.json', encoding='utf-8'))['trim']: print('  TRIMCELL', c['name'], c['rect_px'])
sys.stdout.flush(); os._exit(0)
