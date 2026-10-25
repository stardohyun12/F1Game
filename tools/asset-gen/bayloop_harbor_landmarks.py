"""Headless, metric, photo-textured harbour landmarks; no Unity edits."""
from pathlib import Path
import bpy,sys,math,json,argparse
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bayloop_buildings as b
from bayloop_buildgen_textures import select_style
R=b.ROOT;O=R/'art/models/bayloop_harbor_landmarks_v01';V=R/'art/_review/2026-10-26-landmarks/harbor';V.mkdir(parents=True,exist_ok=True)

def lathe(rows,tile='plaster',n=16,x=0,y=0,axis='z'):
 def co(r,a,z):
  q=(r*math.cos(a),r*math.sin(a),z)
  return (x+q[2],y+q[0],q[1]) if axis=='x' else (x+q[0],y+q[1],q[2])
 rings=[[co(r,2*math.pi*i/n,z) for i in range(n)] for z,r in rows]
 for p,q in zip(rings,rings[1:]):
  for i in range(n):b.face([p[i],p[(i+1)%n],q[(i+1)%n],q[i]],tile)
 b.face(list(reversed(rings[0])),tile);b.face(rings[-1],tile)

def roundrail(radius,z,height=.95,n=16):
 for i in range(n):
  a=math.tau*i/n;c=math.tau*(i+1)/n
  p=(radius*math.cos(a),radius*math.sin(a),z);q=(radius*math.cos(c),radius*math.sin(c),z)
  b.beam(p,(p[0],p[1],z+height),.045,'wood_dark')
  for h in [height*.48,height]:b.beam((p[0],p[1],z+h),(q[0],q[1],z+h),.045,'wood_dark')

def lighthouse():
 b.PART='foundation';b.box(0,0,0,3.6,3.6,.35,'stone')
 b.PART='tower';lathe([(0.35,1.48),(1.15,1.46)],'wear_wall');lathe([(1.15,1.46),(4.4,1.38),(8.4,1.27),(12.5,1.12)],'plaster')
 b.PART='gallery';lathe([(12.45,1.25),(12.65,1.88),(12.82,1.88)],'trim');roundrail(1.79,12.82)
 b.PART='lantern';lathe([(12.83,1.04),(13.02,1.04)],'rust');lathe([(13.02,.94),(14.75,.94)],'glass',12)
 for i in range(12):
  a=math.tau*i/12;b.beam((.95*math.cos(a),.95*math.sin(a),13.02),(.95*math.cos(a),.95*math.sin(a),14.78),.055,'rust')
 lathe([(14.72,1.04),(14.86,1.04),(14.95,1.21),(15.25,1.02),(15.65,.48),(15.85,.09)],'rust',12)
 lathe([(15.85,.055),(16,.055)],'wood_dark',8)
 b.PART='door';b.box(0,-1.44,.35,.92,.13,2.02,'wood_dark');b.box(0,-1.54,.35,.74,.065,1.9,'wood')
 for x in [-.5,.5]:b.box(x,-1.48,.35,.12,.2,2.1,'trim')
 b.box(0,-1.48,2.39,1.12,.20,.13,'trim');b.box(.25,-1.59,1.2,.035,.04,.23,'metal')
 for z,angle in [(4.4,0),(7.2,math.pi),(10.1,0)]:
  # Small framed apertures follow the actual tower facet, not billboard panels.
  start=len(b.V);radius=1.46-(z-1.15)*.030
  b.box(0,-radius-.015,z,.47,.08,.85,'glass')
  for xx in [-.28,.28]:b.box(xx,-radius-.05,z-.06,.08,.12,1,'trim')
  for zz in [z-.06,z+.86]:b.box(0,-radius-.05,zz,.63,.12,.08,'trim')
  if angle:
   for i in range(start,len(b.V)):x,y,h=b.V[i];b.V[i]=(-x,-y,h)
 return 'LANDMARK_등대(곶 끝·해안 직선 시작)',16

def crane():
 b.PART='footings'
 for x in [-1.4,1.4]:
  for y in [-1.4,1.4]:b.box(x,y,0,.9,.9,.45,'stone');b.box(x,y,.45,.5,.5,.18,'rust')
 b.PART='tower_truss'
 for z0,z1 in [(0.63,4),(4,7.3),(7.3,10.6),(10.6,13.9),(13.9,16.5)]:
  r0=1.4-(z0-.63)*.045;r1=1.4-(z1-.63)*.045
  p=[(-r0,-r0,z0),(r0,-r0,z0),(r0,r0,z0),(-r0,r0,z0)];q=[(-r1,-r1,z1),(r1,-r1,z1),(r1,r1,z1),(-r1,r1,z1)]
  for i in range(4):
   b.beam(p[i],q[i],.24,'metal');b.beam(q[i],q[(i+1)%4],.16,'metal');b.beam(p[i],q[(i+1)%4],.13,'metal')
 b.box(0,0,16.05,2.9,3.0,.25,'metal');lathe([(16.3,.95),(16.62,.95)],'wood_dark',12)
 b.PART='boom'
 for x in [-.7,.7]:
  for z in [16.45,18.0]:b.beam((x,-11,z),(x,5,z),.19,'metal')
 for k in range(9):
  y=-11+k*2
  b.beam((-.7,y,16.45),(.7,y,16.45),.13,'metal');b.beam((-.7,y,18.0),(.7,y,18.0),.13,'metal')
  if k<8:
   for x in [-.7,.7]:b.beam((x,y,16.45),(x,y+2,18.0),.13,'metal')
 b.PART='cab';b.box(0,1.7,13.85,2.0,2.2,2.0,'metal');b.box(0,.57,14.65,1.65,.07,.95,'glass');b.box(1.02,1.4,14.65,.07,1.5,.95,'glass');b.box(-1.02,1.4,14.65,.07,1.5,.95,'glass');b.box(0,1.7,15.85,2.2,2.4,.12,'rust')
 for x in [-.93,.93]:
  b.beam((x,.7,13.75),(x,.7,16.4),.13,'wood_dark');b.beam((x,2.7,13.75),(x,2.7,16.4),.13,'wood_dark')
 for z in [14.6,15.62]:b.box(0,.50,z,1.88,.1,.085,'wood_dark')
 for x in [-.91,0,.91]:b.box(x,.50,14.6,.07,.1,1.1,'wood_dark')
 b.box(0,4.3,16.18,2.05,1.35,1.4,'stone')
 for x in [-1,1]:b.beam((x,3.55,16.18),(x,5.05,16.18),.24,'metal');b.beam((x,3.6,16.18),(.7*x,3.0,18),.17,'metal')
 b.PART='rigging';b.beam((0,-10.55,17.48),(0,-10.55,8.8),.045,'wood_dark');b.beam((.16,-10.55,17.48),(.16,-10.55,8.8),.045,'wood_dark')
 # Pulley plate and visible cable block, axle links every suspended element.
 for z,r in [(17.5,.31),(8.9,.28)]:
  for i in range(12):
   a=math.tau*i/12;c=math.tau*(i+1)/12;b.beam((0,-10.55+r*math.cos(a),z+r*math.sin(a)),(0,-10.55+r*math.cos(c),z+r*math.sin(c)),.07,'wood_dark')
 b.beam((0,-10.55,8.7),(0,-10.55,8.25),.12,'rust');b.beam((0,-10.55,8.25),(0,-10.25,8.08),.12,'rust');b.beam((0,-10.25,8.08),(0,-10.08,8.35),.12,'rust')
 b.PART='access'
 for x in [-.22,.22]:b.beam((x,1.5,.65),(x,.88,15.4),.045,'wood_dark')
 for i in range(40):z=.85+i*.36;y=1.5-(z-.65)*.042;b.beam((-.22,y,z),(.22,y,z),.035,'wood_dark')
 return 'LANDMARK_크레인(항구 직선 시작)',18

def main():
 ap=argparse.ArgumentParser();ap.add_argument('--no-render',action='store_true');args=ap.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
 old=json.loads((R/'art/models/bayloop_italy_v01/manifest.json').read_text());assets=[];rows=[]
 b.TILES.update(select_style()['tiles']);b.TEX=O/'HarborPhotoAtlas.png'
 for ident,fn in [('lighthouse_01',lighthouse),('harbor_crane_01',crane)]:
  bpy.ops.wm.read_factory_settings(use_empty=True);b.V.clear();b.F.clear();b.UV.clear();b.PARTS.clear();scene_name,height=fn()
  if ident.startswith('harbor'):b.V[:]=[(-x,-y,z) for x,y,z in b.V] # Preserve legacy boom direction: Unity local -Z.
  ob=b.mesh(scene_name,b.material());tri=sum(len(f)-2 for f in b.F);assert tri<=4000,tri
  bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob
  for fmt in ['glb','fbx']:
   path=O/(ident+'.'+fmt)
   if fmt=='glb':bpy.ops.export_scene.gltf(filepath=str(path),export_format='GLB',use_selection=True,export_yup=True)
   else:bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,axis_forward='-Z',axis_up='Y',bake_space_transform=True,apply_scale_options='FBX_SCALE_ALL',mesh_smooth_type='FACE',path_mode='COPY',embed_textures=True)
  bpy.ops.wm.save_as_mainfile(filepath=str(O/(ident+'.blend')))
  vs=[(x,z,-y) for x,y,z in b.V];lo=[min(p[k] for p in vs) for k in range(3)];hi=[max(p[k] for p in vs) for k in range(3)]
  assets.append(dict(id=ident,type=ident.rsplit('_',1)[0],asset_kind='landmark',max_tris=4000,pivot_kind='floor_center',scene_object_name=scene_name,model=b.rel(O/(ident+'.glb')),fbx=b.rel(O/(ident+'.fbx')),texture=b.rel(b.TEX),bounds_min=lo,bounds_max=hi,dimensions_m=[hi[k]-lo[k] for k in range(3)],source_urls=old['assets'][0]['source_urls'],tris=tri,original_height_m=height))
  if not args.no_render:
   sc=b.stage();sc.cycles.samples=12
   for view,loc in [('front',(22,-32,20)),('back',(-22,32,20)),('side',(35,0,16)),('top',(0,0,45))]:b.render(sc,V/(ident+'_'+view+'.png'),loc,(0,2 if ident.startswith('harbor') else 0,9 if ident.startswith('harbor') else 8),42 if ident.startswith('harbor') else 34)
   sc.camera.data.type='PERSP';sc.camera.data.lens=24
   for distance in [20,60]:sc.camera.location=(distance*.342,-distance*.9396989,2.8);b.aim(sc.camera,(0,0,8));sc.render.filepath=str(V/(ident+f'_distance_{distance}m.png'));bpy.ops.render.render(write_still=True)
  print('HARBOR_DONE',ident,tri,flush=True)
 m={k:old[k] for k in ['palette','source_manifests','density_px_m','max_tris']};m['assets']=assets;(O/'manifest.json').write_text(json.dumps(m,ensure_ascii=False,indent=2),encoding='utf8')
 for a in assets:
  for ext in ['fbx','glb','blend']:
   p=O/(a['id']+'.'+ext);rows.append(dict(asset_id=a['id']+'_'+ext,path=b.rel(p),source_url='; '.join(a['source_urls']),license='CC0 photographs; original bpy mesh',accessed='2026-10-26',tool='bayloop_harbor_landmarks.py; bayloop_harbor_texture.py',ai_generated='no',sha256=b.sha(p),notes='32px/m metric UV; base-centre pivot; actual bpy render; no Unity import'))
 rows.append(dict(asset_id='harbor_photo_atlas',path=b.rel(b.TEX),source_url='; '.join(assets[0]['source_urls']),license='CC0-1.0',accessed='2026-10-26',tool='bayloop_harbor_texture.py; verified photo pipeline reuse',ai_generated='no',sha256=b.sha(b.TEX),notes='Original photo atlas reuse; shared palette pair recolor of photographed metal; no generated noise'))
 (O/'provenance-rows.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf8')
if __name__=='__main__':main()
