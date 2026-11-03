import bpy, sys, os
from mathutils import Vector
from mathutils.bvhtree import BVHTree
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath='D:/Github/F1Game/art/export/q3-depot/q3_depot.fbx')
def bvh(o):
    mw=o.matrix_world; return BVHTree.FromPolygons([mw@v.co for v in o.data.vertices],[p.vertices[:] for p in o.data.polygons],epsilon=0.0)
M=[o for o in bpy.data.objects if o.type=='MESH' and 'decal' not in o.name.lower() and 'glass' not in o.name.lower() and not o.name.startswith('JACKET') and not o.name.startswith('CLOCK') and not o.name.startswith('Q3_sign')]
T={o.name:bvh(o) for o in M}
names=sorted(T)
for i,a in enumerate(names):
    if not a.startswith('PARCEL_'): continue
    for b in names:
        if b==a or (b.startswith('PARCEL_') and b<a): continue
        n=len(T[a].overlap(T[b]))
        if n: print('##',a,b,n)
sys.stdout.flush(); os._exit(0)
