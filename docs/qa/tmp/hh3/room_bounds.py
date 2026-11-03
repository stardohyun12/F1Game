import bpy
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath='D:/Github/F1Game/art/export/q1-room/room.fbx')
from mathutils import Vector
for o in bpy.context.scene.objects:
    if o.type!='MESH': print('EMPTY',o.name,tuple(round(v,2) for v in o.matrix_world.translation)); continue
    P=[o.matrix_world@v.co for v in o.data.vertices]
    mn=[round(min(p[i] for p in P),3) for i in range(3)]; mx=[round(max(p[i] for p in P),3) for i in range(3)]
    print('OB',o.name,mn,mx,len(o.data.polygons))
