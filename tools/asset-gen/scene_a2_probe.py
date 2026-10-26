import bpy
from pathlib import Path
for name in ['autumn_tree_03','autumn_tree_04','background_building_04','transformer_01']:
 bpy.ops.wm.open_mainfile(filepath='D:/Github/F1Game/art/models/garage_scene_a2_kit/'+name+'.blend');ob=bpy.data.objects[name];bpy.context.view_layer.update();print('PROBE',name,list(ob.dimensions),list(ob.bound_box[0]),ob.hide_viewport,ob.hide_get(),ob.hide_render,[(c.name,c.hide_viewport) for c in ob.users_collection],flush=True)
 for mat in ob.data.materials:
  print('MAT',mat.name,[(n.image.name,n.image.filepath,n.image.has_data) for n in mat.node_tree.nodes if n.type=='TEX_IMAGE' and n.image],flush=True)
