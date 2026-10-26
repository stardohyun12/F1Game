import bpy
from mathutils import Vector
bpy.ops.wm.open_mainfile(filepath='D:/Github/F1Game/art/models/garage_scene_a2_kit/background_building_04.blend');ob=bpy.data.objects['background_building_04'];print('NORMALS',[(list(f.center),list(f.normal)) for f in ob.data.polygons[:4]])
for m in ob.data.materials:
 for n in m.node_tree.nodes:
  if n.type=='TEX_IMAGE' and n.image and n.image.name!='background_building_04_bake':
   im=n.image;im.reload();print('IMG',im.name,im.filepath,im.size[:],list(im.pixels[:12]))
