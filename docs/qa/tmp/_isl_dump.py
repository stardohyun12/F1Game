# artlive 0330: 메시 하나 안에 서로 떨어진 조각(1mm 연결)이 2개 이상인 것을 센다 — 도현 "여러 오브젝트 묶지마".
# blender -b --factory-startup -P tools/art/count_bundled.py -- <fbx> <out.json>
# 닿는(2cm) 조각은 한 물건(상자+띠, 시계 몸통)으로 본다 → 닿지 않는 조각이 2개 이상일 때만 "묶음".
import bpy, sys, json, os
from mathutils import Vector
a = sys.argv[sys.argv.index('--') + 1:]
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=a[0])
def islands(o):
    me = o.data; mw = o.matrix_world; par = list(range(len(me.vertices)))
    def f(i):
        while par[i] != i: par[i] = par[par[i]]; i = par[i]
        return i
    key = {}
    for v in me.vertices:
        k = tuple(round(c * 1000) for c in (mw @ v.co))
        if k in key: par[f(v.index)] = f(key[k])
        else: key[k] = v.index
    for p in me.polygons:
        for vi in p.vertices[1:]: par[f(vi)] = f(p.vertices[0])
    g = {}
    for p in me.polygons: g.setdefault(f(p.vertices[0]), []).append(p.index)
    out = []
    for ps in g.values():
        ws = [mw @ me.vertices[i].co for pi in ps for i in me.polygons[pi].vertices]
        out.append((Vector([min(w[i] for w in ws) for i in range(3)]), Vector([max(w[i] for w in ws) for i in range(3)]), ps))
    return out
def touch(x, y, m=0.02): return all(x[0][i] - m <= y[1][i] and y[0][i] - m <= x[1][i] for i in range(3))

for n in ('Q3_decals','Q3_sign','Q3_sign_frame'):
    o=bpy.data.objects[n]; print('##',n,tuple(o.location),[m.name for m in o.data.materials])
    for lo,hi,ps in islands(o):
        mats={o.data.materials[o.data.polygons[p].material_index].name for p in ps}
        print('  ##',[round(x,2) for x in lo],[round(x,2) for x in hi],len(ps),mats)
sys.stdout.flush(); os._exit(0)
