import bpy, sys, os, json
from mathutils import Vector
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath='D:/Github/F1Game/art/export/q3-depot/q3_depot.fbx')
def bb(o):
    vs=[o.matrix_world@v.co for v in o.data.vertices]
    return [[round(fn(v[k] for v in vs),4) for k in range(3)] for fn in (min,max)]
ps=sorted([o for o in bpy.data.objects if o.type=='MESH' and o.name.startswith('PARCEL_')],key=lambda o:o.name)
for o in ps: print(o.name,bb(o))
for name in ('SHELF_01','SHELF_02'):
    o=bpy.data.objects[name]
    print(name,bb(o))
    for p in o.data.polygons:
        if (o.matrix_world.to_3x3()@p.normal).z>.9:
            vs=[o.matrix_world@o.data.vertices[i].co for i in p.vertices]
            print('TOP',[[round(fn(v[k] for v in vs),4) for k in range(3)] for fn in (min,max)])
sys.stdout.flush();os._exit(0)
