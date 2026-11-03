import bpy, sys, os, math
from mathutils import Vector
sys.path.insert(0,'D:/Github/F1Game/tools/art')
from mesh_parts import islands
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath='D:/Github/F1Game/docs/qa/tmp/build/depot/03_snap/q3_depot.fbx')
for n in ('PARCEL_01','PARCEL_08','PARCEL_16','PARCEL_37'):
 o=bpy.data.objects[n]
 for g in islands(o):
  vs={i for f in g['faces'] for i in o.data.polygons[f].vertices}
  if len(vs)>8: print(n,len(vs),[[round(c,4) for c in o.data.vertices[i].co] for i in sorted(vs)])
sys.stdout.flush();os._exit(0)
