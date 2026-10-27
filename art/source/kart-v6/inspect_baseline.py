import bpy,json
from pathlib import Path
from mathutils import Vector
R=Path('D:/Github/F1Game');out={}
for label,path in [('kart','art/export/scene-a2-kart/kart_a2.fbx'),('garage','art/export/scene-a-v5b-garage/garage_v5_combo.fbx')]:
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(R/path));rows=[]
 for o in bpy.context.scene.objects:
  if o.type!='MESH':continue
  p=[o.matrix_world@v.co for v in o.data.vertices];lo=[min(v[i] for v in p) for i in range(3)];hi=[max(v[i] for v in p) for i in range(3)]
  rows.append(dict(name=o.name,origin=list(o.matrix_world.translation),lo=lo,hi=hi,tris=sum(len(f.vertices)-2 for f in o.data.polygons)))
 out[label]=rows
(R/'art/source/kart-v6/baseline.json').write_text(json.dumps(out,indent=2))
print(json.dumps(out['kart'],indent=2))
