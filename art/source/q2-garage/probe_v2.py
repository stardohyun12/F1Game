import bpy,json
from pathlib import Path
R=Path('D:/Github/F1Game');W=R/'art/source/q2-garage'
bpy.ops.wm.open_mainfile(filepath=str(W/'garage-v1.blend'))
jobs=[];rows=[]
for o in bpy.context.scene.objects:
 if o.type!='MESH' or o.get('q2_before_only'):continue
 rows.append(dict(name=o.name,uv=[u.name for u in o.data.uv_layers],active=o.data.uv_layers.active.name,material=[m.name for m in o.data.materials]))
 if o.name in ['floor','apron']:
  o.data.calc_loop_triangles();u=o.data.uv_layers['BakedUV']
  for t in o.data.loop_triangles:jobs.append(dict(name=o.name,uv=[list(u.data[i].uv) for i in t.loops],world=[list(o.matrix_world@o.data.vertices[i].co) for i in t.vertices]))
(W/'floor-jobs.json').write_text(json.dumps(jobs));(W/'uv-before.json').write_text(json.dumps(rows,indent=2))
print([r for r in rows if r['name'] in ['floor','festival_board','festival_registration','memory_frame_0','race_photo_0']])
