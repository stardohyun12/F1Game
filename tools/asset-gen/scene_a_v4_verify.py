from pathlib import Path
import bpy,json,numpy as np,hashlib
R=Path(__file__).resolve().parents[2];O=R/'art/models/garage_scene_a_v4_baked';V=R/'art/_review/2026-10-27-scene-a-v4-baked';E=R/'art/export/scene-a-v4-baked'
def snapshot(path):
 bpy.ops.wm.open_mainfile(filepath=str(path));dg=bpy.context.evaluated_depsgraph_get();result={}
 for o in bpy.context.scene.objects:
  if o.type!='MESH':continue
  ev=o.evaluated_get(dg);me=ev.to_mesh();pts=np.array([list(o.matrix_world@v.co) for v in me.vertices]);pts=pts[np.lexsort((pts[:,2],pts[:,1],pts[:,0]))];result[o.name]=dict(points=pts,tris=sum(len(f.vertices)-2 for f in me.polygons));ev.to_mesh_clear()
 return result
src=snapshot(O/'source-v5.blend');dst=snapshot(O/'unlit-final.blend');checks=[]
for n,s in src.items():
 t=dst.get(n);same=t is not None and s['points'].shape==t['points'].shape;err=float(max(np.sqrt(((s['points'][:,None,:]-t['points'][None,:,:])**2).sum(2)).min(0).max(),np.sqrt(((s['points'][:,None,:]-t['points'][None,:,:])**2).sum(2)).min(1).max())) if same else 999;checks.append(dict(name=n,coordinate_error=err,tris_match=t is not None and s['tris']==t['tris'],passed=err<.0001 and s['tris']==t['tris']))
unlit=[]
for o in bpy.context.scene.objects:
 if o.type=='MESH':
  m=o.active_material;out=next(n for n in m.node_tree.nodes if n.type=='OUTPUT_MATERIAL');unlit.append(bool(out.inputs['Surface'].is_linked and out.inputs['Surface'].links[0].from_node.type=='EMISSION'))
fbx=[]
for m in json.loads((O/'manifest.json').read_text()):
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(E/(m['group']+'_v4_baked.fbx')));obs=[o for o in bpy.context.scene.objects if o.type=='MESH'];tris=sum(len(f.vertices)-2 for o in obs for f in o.data.polygons);fbx.append(dict(group=m['group'],tris=tris,expected=m['tris'],passed=tris==m['tris'] and all(o.data.uv_layers for o in obs)))
res=dict(geometry_pass=all(x['passed'] for x in checks),all_surfaces_emission=all(unlit),mesh_objects=len(checks),triangles=sum(x['tris'] for x in src.values()),geometry=checks,fbx=fbx,source_original_sha256=hashlib.sha256((R/'art/models/opus_garage_kart_v5.blend').read_bytes()).hexdigest(),source_copy_sha256=hashlib.sha256((O/'source-v5.blend').read_bytes()).hexdigest());(V/'technical-check.json').write_text(json.dumps(res,indent=2));print({k:v for k,v in res.items() if k not in ['geometry','fbx']})
