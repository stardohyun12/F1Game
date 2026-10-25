"""External skill method trial. Headless bpy, no MCP/Unity/global installation."""
from pathlib import Path
import sys,json,hashlib,argparse
import bpy,bmesh
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bayloop_buildings as b
ROOT=b.ROOT;O=ROOT/'art/models/bskills_trial_v01';R=ROOT/'art/_review/2026-10-25-bskills-trial';OLD=ROOT/'art/models/bayloop_props2_v01'
IDS=['parked_car_01','parked_car_02','parked_car_03','three_wheeler_01','laundry_line_01']
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--ids',nargs='+',default=IDS);ap.add_argument('--no-render',action='store_true');ap.add_argument('--geometry-only',action='store_true');ap.add_argument('--silhouette',action='store_true');args=ap.parse_args(sys.argv[sys.argv.index('--')+1:]);assert set(args.ids)<=set(IDS)
 tm=json.loads((O/'texture-manifest.json').read_text());old=json.loads((OLD/'manifest.json').read_text(encoding='utf8'));assets=[]
 for name in args.ids:
  typ=name.rsplit('_',1)[0];variant=int(name[-2:])-1;p=dict(id=name,type=typ,variant=variant,seed=232100+variant)
  bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.scene.unit_settings.system='METRIC'
  b.V.clear();b.F.clear();b.UV.clear();b.PARTS.clear();b.TILES.update(tm['aliases']);b.TILES['body']=[0,3,10][variant%3];b.TEX=O/'BskillsTrialPhotoAtlas.png'
  if typ=='parked_car':from bskills_trial_cars import build
  elif typ=='three_wheeler':from bskills_trial_three import build
  else:from bskills_trial_laundry import build
  info=build(p,b);assert len(b.F)==len(b.UV)==len(b.PARTS)
  lo=[min(v[k] for v in b.V) for k in range(3)];hi=[max(v[k] for v in b.V) for k in range(3)];off=[(lo[0]+hi[0])/2,(lo[1]+hi[1])/2,lo[2]];b.V[:]=[tuple(v[k]-off[k] for k in range(3)) for v in b.V]
  tris=sum(len(f)-2 for f in b.F);assert tris<=1500,(name,tris)
  verts=[(x,z,-y) for x,y,z in b.V];lo=[min(v[k] for v in verts) for k in range(3)];hi=[max(v[k] for v in verts) for k in range(3)]
  geom=dict(vertices_y_up=verts,faces=b.F,part_per_face=b.PARTS,uv_per_face=b.UV);gh=hashlib.sha256(json.dumps(geom,sort_keys=True).encode()).hexdigest();(O/(name+'.geometry.json')).write_text(json.dumps(geom))
  if args.geometry_only:print(name,tris,gh);continue
  col=bpy.data.collections.new('COL_'+name);bpy.context.scene.collection.children.link(col)
  mat=b.material();mat.name='MAT_BskillsTrial_Photo32';objects=[]
  for part in sorted(set(b.PARTS)):
   faces=[i for i,q in enumerate(b.PARTS) if q==part];used=sorted({v for i in faces for v in b.F[i]});remap={v:i for i,v in enumerate(used)}
   me=bpy.data.meshes.new('SM_'+part);me.from_pydata([b.V[v] for v in used],[],[tuple(remap[v] for v in b.F[i]) for i in faces]);me.update();uv=me.uv_layers.new(name='UV0')
   for f,idx in zip(me.polygons,faces):
    for li,co in zip(f.loop_indices,b.UV[idx]):uv.data[li].uv=co
   ob=bpy.data.objects.new('SM_'+name+'_'+part,me);col.objects.link(ob);me.materials.append(mat);objects.append(ob)
   if typ=='laundry_line' and part.endswith(('_front','_back')):
    bm=bmesh.new();bm.from_mesh(me);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-5);bm.to_mesh(me);bm.free()
    for poly in me.polygons:poly.use_smooth=True
  bpy.ops.object.select_all(action='DESELECT')
  for ob in objects:ob.select_set(True)
  bpy.context.view_layer.objects.active=objects[0]
  glb=O/(name+'.glb');fbx=O/(name+'.fbx')
  bpy.ops.export_scene.gltf(filepath=str(glb),export_format='GLB',use_selection=True,export_yup=True)
  bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,axis_forward='-Z',axis_up='Y',bake_space_transform=True,apply_scale_options='FBX_SCALE_ALL',add_leaf_bones=False,mesh_smooth_type='FACE',path_mode='COPY',embed_textures=True)
  bpy.ops.wm.save_as_mainfile(filepath=str(O/(name+'.blend')))
  data=dict(id=name,type=typ,asset_kind='prop',max_tris=1500,pivot_kind='floor_center',model=b.rel(glb),fbx=b.rel(fbx),texture=b.rel(b.TEX),tris=tris,bounds_min=lo,bounds_max=hi,dimensions_m=[hi[k]-lo[k] for k in range(3)],source_urls=sorted(set([t['source_url'] for t in tm['tiles']]+['https://polyhaven.com/a/rough_linen'])),parameters=p,geometry_sha256=gh,design=info,systems=list(sorted(set(b.PARTS))),wear_locations=['lower sill','bumper ends','loading bed contact','cloth hems/pegs'])
  assets.append(data)
  if not args.no_render:
   sc=b.stage();sc.cycles.samples=12
   # Identical camera and framing to archived baseline, even where geometry changed.
   prior=next(a for a in old['assets'] if a['id']==name);w,h,d=prior['dimensions_m'];scale=1.2*max(1.3,w*1.5,d*1.4,h*2.15)
   for view,loc in [('front',(4,-7,3.5)),('side',(8,0,h*.45 if typ=='three_wheeler' else 1.9)),('top',(0,0,10))]:b.render(sc,R/(name+'_'+view+'.png'),loc,(0,0,h*.45) if view!='top' else (0,0,0),scale)
   if args.silhouette:
    silhouette=bpy.data.materials.new('REVIEW_ONLY_silhouette');silhouette.diffuse_color=(.06,.06,.06,1)
    for ob in objects:ob.data.materials[0]=silhouette
    b.render(sc,R/(name+'_silhouette.png'),(4,-7,3.5),(0,0,h*.45),scale)
  print('TRIAL_DONE',name,tris,flush=True)
 if not args.geometry_only:
  m=dict(palette=old['palette'],density_px_m=32,max_tris=1500,source_manifests=old['source_manifests'],assets=assets)
  path=O/('manifest-'+args.ids[0]+'.json');path.write_text(json.dumps(m,ensure_ascii=False,indent=2),encoding='utf8')
if __name__=='__main__':main()
