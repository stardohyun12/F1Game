"""D141 metric street props; background Blender, no Unity or game writes."""
from pathlib import Path
import sys,json,hashlib,argparse,random
import bpy
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bayloop_buildings as b
ROOT=b.ROOT;O=ROOT/'art/models/bayloop_props2_v01';R=ROOT/'art/_review/2026-10-25-bayloop-props2'
COUNTS={'parked_car':3,'three_wheeler':1,'scooter':2,'laundry_line':1,'traffic_mirror':1,'street_sign':3,'crosswalk_paint':1,'road_patch':2,'shop_display':1,'cafe_set':1,'parasol':1,'mailbox_cluster':1,'garage_door_inset':1}
VEHICLES={'parked_car','three_wheeler','scooter'}
def recipes():return [dict(id=f'{typ}_{i+1:02}',type=typ,variant=i,seed=141000+j*100+i) for j,(typ,n) in enumerate(COUNTS.items()) for i in range(n)]
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--group',choices=['vehicles','street','all'],default='all');ap.add_argument('--ids',nargs='+');ap.add_argument('--seed',type=int);ap.add_argument('--geometry-only',action='store_true');ap.add_argument('--no-render',action='store_true');args=ap.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
 O.mkdir(parents=True,exist_ok=True);R.mkdir(parents=True,exist_ok=True)
 tm=json.loads((O/'texture-manifest.json').read_text());old=json.loads((ROOT/'art/models/bayloop_variety_v01/manifest.json').read_text());urls=sorted(set(t['source_url'] for t in tm['tiles']))
 if args.group in ('vehicles','all'):
  from bayloop_props2_vehicles import build as vehicle
 if args.group in ('street','all'):
  from bayloop_props2_street import build as street
 ps=[p for p in recipes() if (args.group=='all' or (p['type'] in VEHICLES)==(args.group=='vehicles')) and (not args.ids or p['id'] in args.ids)]
 assert ps and (not args.ids or set(args.ids)=={p['id'] for p in ps})
 assets=[];geometry=[]
 for p in ps:
  if args.seed is not None:p['seed']=args.seed;p['colorway']=args.seed%3
  bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.scene.unit_settings.system='METRIC'
  b.V.clear();b.F.clear();b.UV.clear();b.PARTS.clear();b.PART=p['type'];b.TEX=O/'Props2PhotoAtlas.png';b.TILES.update(tm['aliases']);b.TILES['body']=[0,3,10][p['seed']%3]
  if p.get('colorway',0):b.TILES['teal']=[11,3,10][p['colorway']]
  if p['type'] in VEHICLES and (O/'Props2VehiclesAtlas.png').exists():
   b.TEX=O/'Props2VehiclesAtlas.png';b.TILES['rubber']=13
  if (p['type'] in ('road_patch','crosswalk_paint') or p['type']=='street_sign' and p['variant']>0) and (O/'Props2RoadAtlas.png').exists():
   b.TEX=O/'Props2RoadAtlas.png';b.TILES.update(warning=13,paint=9,roadbase=12)
  if p['type'] in ('shop_display','cafe_set','parasol','mailbox_cluster','garage_door_inset','three_wheeler','scooter') and (O/'Props2FurnitureAtlas.png').exists():
   b.TEX=O/'Props2FurnitureAtlas.png'
   if 'colorway' not in p and (p['type']=='three_wheeler' or p['type']=='scooter' and p['variant']==1):b.TILES['body']=6
  if p['type'] in ('road_patch','crosswalk_paint') and (O/'Props2PavingAtlas.png').exists():
   b.TEX=O/'Props2PavingAtlas.png';b.TILES['seam']=13
  if p['type']=='three_wheeler':
   b.TEX=O/'Props2VehiclesAtlas.png';b.TILES['darkroof']=13
   if 'colorway' not in p:b.TILES['body']=0
  if p['type']=='garage_door_inset':
   b.TEX=O/'Props2VehiclesAtlas.png';b.TILES['metal']=0
  if p['type']=='parasol' and (O/'Props2LinenAtlas.png').exists():b.TEX=O/'Props2LinenAtlas.png'
  info=(vehicle if p['type'] in VEHICLES else street)(p,b) or {}
  lo=[min(v[k] for v in b.V) for k in range(3)];hi=[max(v[k] for v in b.V) for k in range(3)]
  off=((lo[0]+hi[0])/2,(lo[1]+hi[1])/2,lo[2]);b.V[:]=[(x-off[0],y-off[1],z-off[2]) for x,y,z in b.V]
  tris=sum(len(f)-2 for f in b.F);assert 0<tris<=1500,(p['id'],tris)
  name=p['id'];verts=[(x,z,-y) for x,y,z in b.V];lo=[min(v[k] for v in verts) for k in range(3)];hi=[max(v[k] for v in verts) for k in range(3)]
  geom=dict(vertices_y_up=verts,faces=b.F,part_per_face=b.PARTS,uv_per_face=b.UV)
  gh=hashlib.sha256(json.dumps(geom,sort_keys=True).encode()).hexdigest();geometry.append(dict(id=name,tris=tris,geometry_sha256=gh,parameters=p,body_tile=b.TILES['body']))
  if args.geometry_only:continue
  mat=b.material();mat.name='BayLoop_Props2_Photo32';obj=b.mesh(name,mat);obj['recipe']=json.dumps(p);bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
  glb=O/(name+'.glb');fbx=O/(name+'.fbx')
  bpy.ops.export_scene.gltf(filepath=str(glb),export_format='GLB',use_selection=True,export_yup=True)
  bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,axis_forward='-Z',axis_up='Y',bake_space_transform=True,apply_scale_options='FBX_SCALE_ALL',add_leaf_bones=False,mesh_smooth_type='FACE',path_mode='COPY',embed_textures=True)
  (O/(name+'.geometry.json')).write_text(json.dumps(geom));bpy.ops.wm.save_as_mainfile(filepath=str(O/(name+'.blend')))
  asset_urls=sorted(set(urls+(['https://polyhaven.com/a/rough_linen'] if b.TEX.name in ('Props2FurnitureAtlas.png','Props2LinenAtlas.png') else [])))
  data=dict(id=name,type=p['type'],asset_kind='prop',max_tris=1500,pivot_kind='floor_center',model=b.rel(glb),fbx=b.rel(fbx),texture=b.rel(b.TEX),tris=tris,bounds_min=lo,bounds_max=hi,dimensions_m=[hi[k]-lo[k] for k in range(3)],source_urls=asset_urls,parameters=p,body_tile=b.TILES['body'],geometry_sha256=gh,wear_locations=['ground-contact feet','handled edges','metal seams','loading lip'],design=info)
  assets.append(data)
  if not args.no_render:
   sc=b.stage();sc.cycles.samples=12;w,h,d=data['dimensions_m'];scale=1.2*max(1.3,w*1.5,d*1.4,h*2.15)
   if p['type'] in ('crosswalk_paint','road_patch'):
    bpy.data.objects['REVIEW_ONLY_ground'].hide_render=True
    b.V.clear();b.F.clear();b.UV.clear();b.PARTS.clear();b.PART='REVIEW_ONLY_road'
    b.face([(-30,-30,-.001),(30,-30,-.001),(30,30,-.001),(-30,30,-.001)],'roadbase')
    b.mesh('REVIEW_ONLY_asphalt',mat)
   for view,loc in [('front',(4,-7,3.5)),('side',(8,0,1.9)),('top',(0,0,10))]:
    if p['type']=='three_wheeler' and view=='side':loc=(8,0,h*.45)
    b.render(sc,R/(name+'_'+view+'.png'),loc,(0,0,h*.45) if view!='top' else (0,0,0),scale)
  print('PROP_DONE',name,tris,flush=True)
 if args.geometry_only:
  (R/('geometry-'+args.group+'.json')).write_text(json.dumps(geometry,indent=2));print('GEOMETRY',len(geometry));return
 manifest=dict(palette=old['palette'],density_px_m=32,max_tris=1500,source_manifests=old['source_manifests']+[b.rel(O/'asphalt-source.json')],assets=assets)
 if (O/'linen-source.json').exists():manifest['source_manifests'].append(b.rel(O/'linen-source.json'))
 path=O/('manifest-'+args.group+'.json')
 if path.exists():manifest['assets']=[a for a in json.loads(path.read_text())['assets'] if a['id'] not in {x['id'] for x in assets}]+assets
 path.write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf8');print('GROUP_DONE',args.group,len(assets))
if __name__=='__main__':main()
