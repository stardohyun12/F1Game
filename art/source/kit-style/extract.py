import bpy, numpy as np, json
from pathlib import Path
R=Path('D:/Github/F1Game'); S=R/'art/export/scene-a2-kit'; O=R/'art/source/kit-style'
names=[p.stem for p in S.glob('*.fbx') if p.stem.split('_')[0] in ['drum','tires','pot','pole','fence','bicycle','cone','bench']]
stats=[]
for name in sorted(names):
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.fbx(filepath=str(S/(name+'.fbx')))
 pp=[]; nn=[]; uu=[]
 for ob in bpy.context.scene.objects:
  if ob.type!='MESH': continue
  me=ob.data; me.calc_loop_triangles(); uv=me.uv_layers.active.data
  for tri in me.loop_triangles:
   pp.append([list(ob.matrix_world@me.vertices[v].co) for v in tri.vertices])
   nn.append([list((ob.matrix_world.to_3x3()@me.vertices[v].normal).normalized()) for v in tri.vertices])
   uu.append([list(uv[l].uv) for l in tri.loops])
 p=np.array(pp); n=np.array(nn); u=np.array(uu)
 np.savez_compressed(O/(name+'.npz'),p=p,n=n,u=u)
 stats.append(dict(name=name,tris=len(p),bounds_min=p.min((0,1)).tolist(),bounds_max=p.max((0,1)).tolist()))
 print(name,stats[-1],flush=True)
(O/'geometry.json').write_text(json.dumps(stats,indent=2))
