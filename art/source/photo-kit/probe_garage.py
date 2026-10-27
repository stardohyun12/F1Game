import bpy,json
from pathlib import Path
R=Path('D:/Github/F1Game');W=R/'art/source/photo-kit';W.mkdir(exist_ok=True,parents=True)
bpy.ops.wm.open_mainfile(filepath=str(R/'art/models/garage_scene_a_v5_combo/baked-lit.blend'))
rows=[]
for o in bpy.context.scene.objects:
 if o.type=='MESH' and any(c.name=='garage' for c in o.users_collection):
  rows.append(dict(name=o.name,materials=[m.name for m in o.data.materials],textures=[n.image.filepath for m in o.data.materials if m.use_nodes for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image],uv=[u.name for u in o.data.uv_layers]))
(W/'garage-materials.json').write_text(json.dumps(rows,indent=2));print(json.dumps(rows[:20]),flush=True)
