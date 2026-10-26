import bpy,json
from pathlib import Path
R=Path('D:/Github/F1Game');bpy.ops.wm.open_mainfile(filepath=str(R/'art/models/garage_scene_a_v4_baked/source-v5.blend'))
rows=[]
for o in bpy.context.scene.objects:
 if o.type=='MESH':rows.append(dict(name=o.name,dim=list(o.dimensions),loc=list(o.location),verts=len(o.data.vertices),tris=sum(len(p.vertices)-2 for p in o.data.polygons),mats=[m.name if m else '' for m in o.data.materials]))
(R/'art/_review/2026-10-27-scene-a-v4-baked/source-inventory.json').write_text(json.dumps(rows,indent=2));print(json.dumps(rows))
print('CAM',[(o.name,list(o.location)) for o in bpy.context.scene.objects if o.type=='CAMERA'])
