import bpy,json
from pathlib import Path
bpy.ops.wm.open_mainfile(filepath='D:/Github/F1Game/art/models/garage_scene_a_v4_baked/source-v5.blend')
print('COLLECTIONS',[(c.name,len(c.objects)) for c in bpy.data.collections])
for m in bpy.data.materials:
 if m.name in ['planks','ground','tin_roof','record','tire']:
  print(m.name,[(n.type,n.name, n.image.filepath if n.type=='TEX_IMAGE' and n.image else '') for n in m.node_tree.nodes])
