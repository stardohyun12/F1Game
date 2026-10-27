import bpy,json
from pathlib import Path
R=Path('D:/Github/F1Game');W=R/'art/source/po1';W.mkdir(parents=True,exist_ok=True);summary={}
names=['transformer_01','tires_01','tires_02']+[f'autumn_tree_{i:02d}' for i in range(1,6)]
for name in names:
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(R/'art/export/scene-a2-kit'/(name+'.fbx')));rows=[];desc=[]
 for o in bpy.context.scene.objects:
  if o.type!='MESH':continue
  me=o.data;me.calc_loop_triangles();adj={v.index:set() for v in me.vertices}
  for e in me.edges:a,b=e.vertices;adj[a].add(b);adj[b].add(a)
  comp={};cs=[]
  for v in me.vertices:
   if v.index in comp:continue
   todo=[v.index];ids=[];cid=len(cs);comp[v.index]=cid
   while todo:
    n=todo.pop();ids.append(n)
    for j in adj[n]:
     if j not in comp:comp[j]=cid;todo.append(j)
   pts=[o.matrix_world@me.vertices[i].co for i in ids];lo=[min(p[i] for p in pts) for i in range(3)];hi=[max(p[i] for p in pts) for i in range(3)];cs.append(dict(id=cid,verts=len(ids),lo=lo,hi=hi))
  desc.append(dict(object=o.name,materials=[m.name for m in me.materials],components=cs))
  for t in me.loop_triangles:
   rows.append(dict(object=o.name,component=comp[t.vertices[0]],material=me.materials[t.material_index].name if me.materials else '',uv=[list(me.uv_layers.active.data[i].uv) for i in t.loops],p=[list(o.matrix_world@me.vertices[i].co) for i in t.vertices],normal=list(o.matrix_world.to_3x3()@t.normal)))
 (W/(name+'.json')).write_text(json.dumps(rows));summary[name]=desc
(W/'geometry.json').write_text(json.dumps(summary,indent=2));print(json.dumps(summary),flush=True)
