# artlive 1857: export fbx 의 오브젝트별 덩어리(위치 1mm 연결) 목록 — 낱개 분리 계획용
# blender -b --factory-startup -P docs/qa/tmp/islands_1857.py -- <fbx> <obj1,obj2,...>
import bpy, sys, os
from collections import Counter
fbx, names = sys.argv[sys.argv.index('--') + 1:][:2]
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=fbx)
for nm in names.split(','):
    o = bpy.data.objects[nm]; me = o.data; mw = o.matrix_world
    par = list(range(len(me.vertices)))
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
    isl = {}
    for p in me.polygons: isl.setdefault(f(p.vertices[0]), []).append(p)
    print('OBJ', nm, 'islands', len(isl))
    for r, ps in sorted(isl.items(), key=lambda kv: min((mw @ me.vertices[i].co).x for p in kv[1] for i in p.vertices)):
        ws = [mw @ me.vertices[i].co for p in ps for i in p.vertices]
        lo = [round(min(w[i] for w in ws), 2) for i in range(3)]; hi = [round(max(w[i] for w in ws), 2) for i in range(3)]
        mats = Counter(me.materials[p.material_index].name for p in ps)
        print(' I', r, len(ps), dict(mats), lo, hi)
sys.stdout.flush(); os._exit(0)
