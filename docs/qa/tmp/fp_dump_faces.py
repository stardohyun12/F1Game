# fix_1846 check 의 (obj, 덩어리 root 꼭짓점) → 그 덩어리 bbox 를 찍는다. artlive 2128
import bpy, sys, os
sys.path.insert(0, 'D:/Github/F1Game/tools/asset-gen')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath='D:/Github/F1Game/art/export/field-patch/field_patch.fbx')
Q = dict(ONION_10=(84, 96), FENCE_01=(156, 480), CAN_01=(592, 616))
def islands(me):
    par = list(range(len(me.vertices)))
    def f(i):
        while par[i] != i: par[i] = par[par[i]]; i = par[i]
        return i
    key = {}
    for v in me.vertices:
        k = tuple(round(c * 1000) for c in v.co)
        if k in key: par[f(v.index)] = f(key[k])
        else: key[k] = v.index
    for p in me.polygons:
        r = f(p.vertices[0])
        for vi in p.vertices[1:]: par[f(vi)] = r
    return [f(i) for i in range(len(me.vertices))]
for o in bpy.context.scene.objects:
    if o.name in Q:
        mw = o.matrix_world; me = o.data; root = islands(me)
        for r in Q[o.name]:
            P = [mw @ v.co for v in me.vertices if root[v.index] == r]
            print('ISL', o.name, r, len(P), [round(min(p[c] for p in P), 3) for c in range(3)], [round(max(p[c] for p in P), 3) for c in range(3)])
sys.stdout.flush(); os._exit(0)
