from pathlib import Path
import bpy,sys,json,hashlib,math
from mathutils import Vector
R=Path('D:/Github/F1Game');E=R/'art/export/scene-a2-kit';W=R/'art/source/a2-task-0453';O=R/'art/models/garage_scene_a2_kit';sys.path.insert(0,str(R/'tools/asset-gen'))
from scene_a2_helpers import imp,world_bbox
meta=json.loads((O/'manifest.json').read_text(encoding='utf-8'));results=[]
def meshdata(path):
 bpy.ops.wm.read_factory_settings(use_empty=True);obs=imp(str(path));bpy.context.view_layer.update();meshes=[o for o in obs if o.type=='MESH'];points=[o.matrix_world@v.co for o in meshes for v in o.data.vertices];mn,mx=world_bbox(meshes)
 return meshes,points,mn,mx
def signature(points):
 # Account for FBX's tiny float conversion errors. Vertices can split at UV seams.
 return sorted(set(tuple(round(float(v),4) for v in p) for p in points))
for item in meta:
 n=item['name'];obs,points,mn,mx=meshdata(E/(n+'.fbx'));size=list(mx-mn);tris=sum(len(f.vertices)-2 for o in obs for f in o.data.polygons)
 check=dict(name=n,triangles=tris,size_m=size,size_pass=all(abs(x-y)<.005 for x,y in zip(sorted(size),sorted(item['size']))),uv_present=all(bool(o.data.uv_layers) for o in obs),source_hash_pass=hashlib.sha256((R/item['source']).read_bytes()).hexdigest()==item['source_sha256'])
 if (W/'before'/(n+'.fbx')).exists():
  before=meshdata(W/'before'/(n+'.fbx'))[1];check['geometry_unchanged']=signature(points)==signature(before)
 if n=='background_building_05':
  # Compare saved bake mesh and FBX, before any preview or material substitution.
  bpy.ops.wm.open_mainfile(filepath=str(O/(n+'.blend')));o=bpy.data.objects[n];srcpoints=[o.matrix_world@v.co for v in o.data.vertices];check['bake_vs_export_geometry']=signature(points)==signature(srcpoints)
  _,authored,amin,amax=meshdata(R/item['source']);k=item['scale_applied'];shift=Vector((-(amin.x+amax.x)*k/2,-(amin.y+amax.y)*k/2,-amin.z*k))
  expected=[p*k+shift for p in authored];check['authored_vertices_retained']=signature(points)==signature(expected)
 check['passed']=check['size_pass'] and check['uv_present'] and check['source_hash_pass'] and tris==item['tris'] and check.get('geometry_unchanged',True) and check.get('bake_vs_export_geometry',True) and check.get('authored_vertices_retained',True)
 assert check['passed'],check
 results.append(check);print('VERIFIED',n,flush=True)
(W/'fbx-validation.json').write_text(json.dumps(results,indent=2));print('ALL FBX PASS',len(results),flush=True)
