import bpy,json,math
from pathlib import Path
from mathutils import Vector
R=Path('D:/Github/F1Game');E=R/'art/export/kart-v6';bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(E/'kart_v6.fbx'))
obs=[o for o in bpy.context.scene.objects if o.type=='MESH'];p=[o.matrix_world@v.co for o in obs for v in o.data.vertices];lo=[min(v[i] for v in p) for i in range(3)];hi=[max(v[i] for v in p) for i in range(3)];size=[hi[i]-lo[i] for i in range(3)];tris=sum(len(f.vertices)-2 for o in obs for f in o.data.polygons)
checks={};lookup={o.name:o for o in obs}
for code,x,y,z,r in [('FL',-.44,.54,.24,.148),('FR',.44,.54,.24,.148),('RL',-.52,-.51,.34,.222),('RR',.52,-.51,.34,.222)]:
 row={}
 for part in ['tire','hub']:
  name='k_'+part+'_'+code;o=lookup[name];vs=[o.matrix_world@v.co for v in o.data.vertices];mn=Vector([min(v[i] for v in vs) for i in range(3)]);mx=Vector([max(v[i] for v in vs) for i in range(3)]);center=(mn+mx)/2;expected=Vector((x,y,z));err=(center-expected).length
  row[part]=dict(name=name,center=list(center),center_error=err,origin_error=(o.matrix_world.translation-expected).length,radius=(mx.z-mn.z)/2)
  assert err<1e-5 and row[part]['origin_error']<1e-5,row
  if part=='tire':assert abs(row[part]['radius']-r)<1e-5,row
 checks[code]=row
assert 1000<=tris<=2000 and max(abs(a-b) for a,b in zip(size,[1.28,1.8,.82]))<1e-5
uv_ok=all(len(o.data.uv_layers)==1 and all(math.isfinite(v) and -.001<=v<=1.001 for uv in o.data.uv_layers[0].data for v in uv.uv) for o in obs);assert uv_ok
out=dict(pass_geometry=True,triangles=tris,mesh_objects=len(obs),size_xyz=size,wheel_checks=checks,uv0_valid=uv_ok,unity_imported=False)
(E/'fbx-validation.json').write_text(json.dumps(out,indent=2));print('PASS',tris,size,'8 wheel parts/centres/pivots/radii; UV0',uv_ok)
