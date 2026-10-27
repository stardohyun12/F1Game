import bpy,json
from pathlib import Path
R=Path('D:/Github/F1Game');W=R/'art/source/q2-garage';W.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(R/'art/models/garage_scene_a_v5_combo/baked-lit.blend'))
man=json.loads((R/'art/models/garage_scene_a_v5_combo/manifest.json').read_text());g=next(x for x in man if x['group']=='garage');rows=[]
for n in g['objects']:
 o=bpy.data.objects[n];p=[o.matrix_world@v.co for v in o.data.vertices];rows.append(dict(name=n,low=[min(v[k] for v in p) for k in range(3)],high=[max(v[k] for v in p) for k in range(3)],materials=[m.name for m in o.data.materials],images=[im.image.filepath for m in o.data.materials if m and m.use_nodes for im in m.node_tree.nodes if im.type=='TEX_IMAGE' and im.image]))
(W/'source-inventory.json').write_text(json.dumps(rows,indent=2));print('\n'.join(str({k:v for k,v in d.items() if k!='images'}) for d in rows))
print('SOURCE_IMAGES',sorted(set(i for d in rows for i in d['images'])))
o=bpy.data.objects['roof'];o.data.calc_loop_triangles();uv=o.data.uv_layers['BakedUV'];jobs=[]
for t in o.data.loop_triangles:jobs.append(dict(uv=[list(uv.data[i].uv) for i in t.loops],world=[list(o.matrix_world@o.data.vertices[i].co) for i in t.vertices]))
(W/'roof-jobs.json').write_text(json.dumps(jobs))
