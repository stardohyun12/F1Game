import bpy
bpy.ops.wm.open_mainfile(filepath='D:/Github/F1Game/art/models/garage_scene_a2_kit/background_building_04.blend');ob=bpy.data.objects['background_building_04'];print('UV',[(u.name,u.active_render) for u in ob.data.uv_layers],ob.data.uv_layers.active_index);print('VIS',ob.visible_diffuse,ob.visible_shadow,ob.visible_glossy,ob.hide_render)
for m in set(ob.data.materials):
 print('MAT',m.name,[(n.image.filepath,n.image.size[:]) for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image],m.node_tree.nodes.active.name)
