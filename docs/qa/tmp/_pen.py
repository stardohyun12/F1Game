import bpy, sys, os
from mathutils import Vector
from mathutils.bvhtree import BVHTree
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath='D:/Github/F1Game/art/export/q3-depot/q3_depot.fbx')
def bb(o):
    vs=[o.matrix_world@v.co for v in o.data.vertices]
    return Vector([min(v[i] for v in vs) for i in range(3)]),Vector([max(v[i] for v in vs) for i in range(3)])
P=sorted([o for o in bpy.data.objects if o.type=='MESH' and o.name.startswith('PARCEL_')],key=lambda o:o.name)
B={o.name:bb(o) for o in P}
for n in ('PARCEL_30','PARCEL_08','PARCEL_37'): print('##',n,[round(x,3) for x in B[n][0]],[round(x,3) for x in B[n][1]],len(bpy.data.objects[n].data.polygons))
print('## pairs')
for i,a in enumerate(P):
    for b in P[i+1:]:
        la,ha=B[a.name]; lb,hb=B[b.name]
        d=[min(ha[k],hb[k])-max(la[k],lb[k]) for k in range(3)]
        if all(x>0.005 for x in d): print('##',a.name,b.name,[round(x*100,1) for x in d])
other=[o for o in bpy.data.objects if o.type=='MESH' and not o.name.startswith('PARCEL_') and not o.name.startswith('JACKET') and 'decal' not in o.name.lower() and 'glass' not in o.name.lower()]
print('## vs other (bbox overlap >1cm each axis, names)')
for p in P:
    lp,hp=B[p.name]
    for o in other:
        lo,ho=bb(o); d=[min(hp[k],ho[k])-max(lp[k],lo[k]) for k in range(3)]
        if all(x>0.01 for x in d): print('##',p.name,o.name,[round(x*100,1) for x in d])
sys.stdout.flush(); os._exit(0)
