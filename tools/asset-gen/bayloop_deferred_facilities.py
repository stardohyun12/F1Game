"""Deferred facilities repair; reuse proven metric UV and CC0 atlases, no Unity."""
from pathlib import Path
import sys,json,math,hashlib
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bpy
import bayloop_buildgen as g
import bayloop_buildgen_facilities as f
from bayloop_buildgen_textures import select_style
b=g.b; ROOT=b.ROOT
OUT=ROOT/'art/models/bayloop_deferred_v02'; REV=ROOT/'art/_review/2026-10-26-landmarks/deferred'
box=b.box;beam=b.beam;face=b.face
_original_face=b.face
def corrected_face(points,tile='plaster',offset=(0,0)):
 if tile=='glass':offset=(80,80)
 if tile=='cloth':offset=(0,1)
 return _original_face(points,tile,offset)
b.face=corrected_face

def cup(x,y,z,r=.08,h=.14):
 for k in range(8):
  a=k*math.tau/8;c=(k+1)*math.tau/8
  face([(x+r*math.cos(a),y+r*math.sin(a),z),(x+r*math.cos(c),y+r*math.sin(c),z),(x+r*math.cos(c),y+r*math.sin(c),z+h),(x+r*math.cos(a),y+r*math.sin(a),z+h)],'trim')
 face([(x+r*.83*math.cos(k*math.tau/8),y+r*.83*math.sin(k*math.tau/8),z+h-.01) for k in range(8)],'wood_dark')
 beam((x+r,y,z+h*.75),(x+r*1.55,y,z+h*.75),.035,'trim');beam((x+r*1.55,y,z+h*.75),(x+r*1.55,y,z+h*.25),.035,'trim')

def bus(p):
 w,d=p['width'],p['depth'];h=2.45;b.PART='shelter_frame'
 box(0,d/2,0,w,d,.08,'stone')
 for x in [-w/2+.14,w/2-.14]:
  for y in [.15,d-.15]:box(x,y,.08,.14,.14,h,'teal')
  beam((x,.15,2.02),(x,.65,2.46),.09,'teal');beam((x,d-.15,2.02),(x,d-.65,2.46),.09,'teal')
 box(0,d/2,2.46,w+.25,d+.3,.16,'metal')
 for y in [-.15,d+.15]:box(0,y,2.35,w+.25,.10,.16,'teal')
 box(0,d-.10,.1,w-.25,.12,1.1,'plaster');box(0,d-.08,1.2,w-.25,.08,1.12,'glass')
 for x in [-1.4,0,1.4]:box(x,d-.16,1.2,.07,.09,1.12,'teal')
 b.PART='bench'
 for y in [d-.72,d-.55,d-.38]:box(0,y,.47,3.35,.14,.075,'wood')
 for z in [.73,.91]:box(0,d-.30,z,3.35,.06,.14,'wood')
 for x in [-1.2,1.2]:box(x,d-.55,.08,.1,.44,.39,'metal');box(x,d-.30,.46,.07,.07,.61,'metal')
 b.PART='route_information';x=-w/2+.17
 box(x,-.22,0,.09,.09,2.90,'teal');box(x,-.22,2.34,.62,.10,.6,'trim')
 # Recognizable bus silhouette, windscreen and two wheels at 32 px/m.
 box(x,-.28,2.48,.39,.025,.30,'teal');box(x,-.30,2.61,.28,.018,.11,'glass')
 for dx in [-.125,.125]:box(x+dx,-.28,2.43,.07,.04,.08,'wood_dark')
 # Same modest icon on the real reverse side of the stop plate.
 box(x,-.155,2.48,.39,.025,.30,'teal');box(x,-.135,2.61,.28,.018,.11,'glass')
 for dx in [-.125,.125]:box(x+dx,-.15,2.43,.07,.04,.08,'wood_dark')
 box(1.9,d-.19,1.25,.6,.10,.98,'wood_dark');box(1.9,d-.25,1.32,.48,.025,.82,'trim')
 box(1.75,d-.27,1.44,.035,.02,.56,'teal')
 for z in [1.48,1.64,1.80,1.96]:box(1.91,d-.28,z,.26,.018,.035,'wood_dark')
 return dict(footprint_m=[w,d],storeys=1,front_wall_z=0)

def cafe(p):
 info=f.build(p,b,g.italy);b.PART='coffee_identity'
 box(.65,.45,1.03,.68,.4,.55,'teal');box(.65,.23,1.28,.52,.035,.17,'metal')
 for x in [.49,.81]:beam((x,.20,1.28),(x,.07,1.28),.04,'metal');cup(x,.12,1.04)
 for x in [-.8,-.45,-.1]:cup(x,-.02,1.02)
 box(1.35,-.1,1.07,.42,.06,.48,'teal')
 for z in [1.18,1.29,1.40]:box(1.35,-.14,z,.26,.025,.03,'trim')
 # Projecting framed sign with cup symbol, above rather than behind awning.
 box(-1.35,-.11,2.88,.86,.14,.56,'teal');box(-1.35,-.20,2.99,.31,.025,.24,'trim')
 beam((-1.16,-.2,3.16),(-1.04,-.2,3.16),.045,'trim');beam((-1.04,-.2,3.16),(-1.04,-.2,3.03),.045,'trim')
 box(-1.35,-.21,2.95,.45,.03,.035,'trim')
 b.PART='rear_service';g.moved(lambda:b.front_window(0,-.06,1.12,1.1,1.0,shutters=True),dx=.5,dy=p['depth'],side='back')
 for x in [-1.75]:beam((x,p['depth']+.09,.1),(x,p['depth']+.09,2.7),.075,'metal')
 return info

def farm(p):
 # Lower visual noise: rendered actual photo plaster above continuous stone plinth.
 b.TILES['stonewall']=b.TILES['plaster'];info=g.house(p);w=p['width'];d=p['depth']
 b.PART='localized_stone_base'
 box(0,-.025,0,w,.05,.48,'stone');box(w/2+.025,d/2+.05,0,.05,d-.1,.48,'stone')
 # Overlapping eave lip and ridge: actual geometry gives roof depth at distance.
 fh=2.8
 b.PART='roof_edge'
 # house roof elevations are recipe-driven: detect existing roof vertices.
 top=max(v[2] for v in b.V)
 # Actual shallow overlapping terracotta courses resolve the printed roof silhouette.
 for side in [-1,1]:
  for k in range(1,9):
   x=side*(w/2+.35)*k/9;z=2.8+1.1*(1-k/9)+.025
   beam((x,-.5,z),(x,d+.3,z),.065,'roof_clay')
 # Low stone retaining wall and utilitarian timber log shelter distinguish farm use.
 box(-w/2-1.13,d*.37-.065,.05,1.38,.08,1.82,'wood')
 for z in [.25,.52,.79]:
  for x in [-w/2-1.65,-w/2-1.15,-w/2-.65]:beam((x,d*.3,z),(x+.34,d*.3,z),.20,'wood')
 # Rear utility door and small vent reveal annex use without new decoration.
 g.moved(lambda:b.front_window(0,-.07,.08,.75,1.7),dx=-w/2-1.05,dy=d*.91,side='back')
 return info

def place_photo_islands():
 import numpy as np,itertools
 im=bpy.data.images.load(str(b.TEX),check_existing=True)
 px=np.array(im.pixels[:],dtype=float).reshape(im.size[1],im.size[0],4)[::-1,:,:3]
 px=np.rint(px*255).astype(np.uint8)
 def varied(uv,vs):
  for ids in itertools.combinations(range(len(uv)),3):
   pos=np.array([vs[k] for k in ids]);area=np.linalg.norm(np.cross(pos[1]-pos[0],pos[2]-pos[0]))/2
   if area<.04:continue
   t=uv[list(ids)];m=np.column_stack([t[1]-t[0],t[2]-t[0]])
   if abs(np.linalg.det(m))<1e-8:continue
   lo=np.floor(t.min(0)).astype(int);hi=np.ceil(t.max(0)).astype(int);xs,ys=np.meshgrid(np.arange(lo[0],hi[0]),np.arange(lo[1],hi[1]));q=np.column_stack([xs.ravel()+.5,ys.ravel()+.5]);ab=(q-t[0])@np.linalg.inv(m).T;inside=(ab[:,0]>=0)&(ab[:,1]>=0)&(ab.sum(1)<=1)
   q=q[inside].astype(int)
   if len(q) and len(np.unique(px[q[:,1],q[:,0]],axis=0))<2:return False
  return True
 for j,(f,coords) in enumerate(zip(b.F,b.UV)):
  uv=np.array([(u*1024,(1-v)*1024) for u,v in coords]);vs=[b.V[k] for k in f]
  if varied(uv,vs):continue
  cell=np.floor(uv.min(0)/256)*256;span=uv.max(0)-uv.min(0);relative=uv-uv.min(0)
  for oy in range(2,100,4):
   found=False
   for ox in range(2,100,4):
    if ox+span[0]>254 or oy+span[1]>254:continue
    trial=relative+cell+[ox,oy]
    if varied(trial,vs):b.UV[j]=[(u/1024,1-v/1024) for u,v in trial];found=True;break
   if found:break

def main():
 OUT.mkdir(parents=True,exist_ok=True);REV.mkdir(parents=True,exist_ok=True)
 old=json.loads((ROOT/'art/models/bayloop_variety_v01/manifest.json').read_text());assets=[]
 for typ,fn in [('bus_stop',bus),('cafe_kiosk',cafe),('farmhouse',farm)]:
  p=next(p for p in g.catalog() if p['id']==typ+'_01');p['wall_color']='cream';p['wear']='light';p['life']['ac_unit']=False
  bpy.ops.wm.read_factory_settings(use_empty=True);b.V.clear();b.F.clear();b.UV.clear();b.PARTS.clear();b.PART='shell'
  st=select_style('cream','light','green');b.TEX=OUT/'DeferredPhotoAtlas.png';b.TILES.update(st['tiles']);mat=b.material();info=fn(p)
  place_photo_islands();name=p['id'];obj=b.mesh(name,mat);bpy.context.view_layer.objects.active=obj;obj.select_set(True)
  glb=OUT/(name+'.glb');fbx=OUT/(name+'.fbx')
  bpy.ops.export_scene.gltf(filepath=str(glb),export_format='GLB',use_selection=True,export_yup=True)
  bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,axis_forward='-Z',axis_up='Y',bake_space_transform=True,apply_scale_options='FBX_SCALE_ALL',add_leaf_bones=False,path_mode='COPY',embed_textures=True)
  vs=[(x,z,-y) for x,y,z in b.V];lo=[min(v[k] for v in vs) for k in range(3)];hi=[max(v[k] for v in vs) for k in range(3)]
  a=dict(id=name,type=typ,model=b.rel(glb),fbx=b.rel(fbx),texture=b.rel(b.TEX),tris=sum(len(f)-2 for f in b.F),dimensions_m=[hi[k]-lo[k] for k in range(3)],bounds_min=lo,bounds_max=hi,source_urls=old['assets'][0]['source_urls']+['https://polyhaven.com/a/rough_linen'],parameters=p,**info);assets.append(a)
  bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(name+'.blend')))
  sc=b.stage();sc.cycles.samples=12;scale=max(10,(hi[0]-lo[0])*1.95,(hi[1]-lo[1])*2.6)
  for view,loc in [('front',(13,-23,12)),('back',(-15,23,12)),('side',(30,p['depth']/2,5))]:b.render(sc,REV/(name+'_'+view+'.png'),loc,(0,p['depth']/2,hi[1]*.43),scale)
  print('DONE',name,a['tris'],flush=True)
 manifest={k:old[k] for k in ['palette','density_px_m','max_tris','source_manifests']};manifest['assets']=assets;manifest['source_manifests'].append('art/models/bayloop_props2_v01/linen-source.json')
 (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
 rows=[]
 path=OUT/'DeferredPhotoAtlas.png';rows.append(dict(asset_id='deferred_v02_atlas',path=b.rel(path),source_url='; '.join(assets[0]['source_urls']),license='CC0-1.0',accessed='2026-10-26',tool='bayloop_deferred_textures.py; photo_to_pixel.py',ai_generated='no',sha256=hashlib.sha256(path.read_bytes()).hexdigest(),notes='Verified CC0 photo atlas reused; localized photographic plaster mask; reduced roof/metal contrast; rough linen 0.271m at9px; shared palette only; Point'))
 for a in assets:
  for key in ['model','fbx']:
   path=ROOT/a[key];rows.append(dict(asset_id='deferred_v02_'+path.stem,path=a[key],source_url='; '.join(a['source_urls']),license='CC0-1.0 (photos); project-authored geometry',accessed='2026-10-26',tool='bayloop_deferred_facilities.py; bayloop_buildgen.py; photo_to_pixel.py',ai_generated='no',sha256=hashlib.sha256(path.read_bytes()).hexdigest(),notes='Metric bpy repaired facility; existing verified CC0 photo atlas; 32px/m; no AI'))
 (OUT/'provenance-rows.json').write_text(json.dumps(rows,indent=2),encoding='utf8')
if __name__=='__main__':main()

