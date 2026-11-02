import bpy,json
p='D:/Github/F1Game/art/source/packing-materials/adapters/packing_tape_roll_primitive_mesh.fbx'
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=p);bpy.context.view_layer.update()
for o in bpy.data.objects:
 if o.type=='MESH':
  co=[v.co for v in o.data.vertices];print('LOCAL',list(o.location),list(o.rotation_euler),list(o.scale),[max(v[k] for v in co)-min(v[k] for v in co) for k in range(3)],'WORLD',o.matrix_world)
