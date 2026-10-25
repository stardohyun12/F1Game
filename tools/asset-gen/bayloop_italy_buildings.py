"""D137 Italian coastal massing + eye-level everyday attachments. bpy only."""
from pathlib import Path
import sys,argparse,json,math
import bpy
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bayloop_buildings as b
ROOT=b.ROOT;OUT=ROOT/'art/models/bayloop_italy_v01';REV=ROOT/'art/_review/2026-10-25-bayloop-italy'
box=b.box;face=b.face;beam=b.beam
LOUVRES=6

def shifted(fn,dx=0,dy=0,dz=0):
 n=len(b.V);fn()
 for i in range(n,len(b.V)):
  x,y,z=b.V[i];b.V[i]=(x+dx,y+dy,z+dz)

def shutters(x,y,z,w,h):
 for xx in [x-w*.76,x+w*.76]:
  box(xx,y-.075,z,w*.43,.085,h,'teal')
  for k in range(1,LOUVRES+1):box(xx,y-.13,z+k*h/(LOUVRES+1),w*.4,.055,.035,'wood_dark')

def window(x,y,z,w=1,h=1.65,door=False,arch=False):
 if arch:b.arched_window(x,y+.10,z,w,h)
 else:
  box(x,y+.12,z,w,.08,h,'glass')
  for xx in [x-w/2,x+w/2]:box(xx,y-.015,z,.07,.14,h,'trim')
  for zz in [z,z+h]:box(x,y-.015,zz,w+.12,.15,.07,'trim')
  box(x,y-.03,z,.04,.1,h,'wood_dark')
  box(x,y-.02,max(0,z-.07),w+.18,.3,.08,'stone')
 if door:
  box(x,y+.055,z,w-.1,.06,.55,'wood_dark');box(x+.25,y-.015,z+1,.045,.09,.18,'trim')
 else:shutters(x,y,z,w,h)

def balcony(x,y,z,w=1.75,depth=.72):
 box(x,y-depth/2,z-.09,w,depth,.12,'stone')
 shifted(lambda:b.railing(w-.12,0,0),x,y-depth+.04,z+.03)
 for xx in [x-w/2+.06,x+w/2-.06]:
  beam((xx,y-depth,z+1.03),(xx,y,z+1.03),.045,'metal')
  beam((xx,y-.05,z-.55),(xx,y-depth+.12,z-.08),.065,'metal')

def awning(x,y,z,w):
 shifted(lambda:b.canopy(w,0,0,1.0,'cloth'),x,y,z)
 box(x,y-1.0,z-.52,w,.025,.19,'cloth')

def signs(x,y,z):
 # Boards layered at human eye level; metric UV offsets select actual font words.
 for k,(dx,dz,w,h) in enumerate([(0,0,.95,.4),(.14,-.55,.7,.35),(-.06,-1.0,.58,.3)]):
  box(x+dx,y-.07,z+dz,w,.1,h,'wood_dark')
  face([(x+dx-w/2,y-.126,z+dz),(x+dx+w/2,y-.126,z+dz),(x+dx+w/2,y-.126,z+dz+h),(x+dx-w/2,y-.126,z+dz+h)],'sign',offset=(0,k*32))

def services(w,d,h):
 b.PART='daily_services'
 # Local-use meter and conduit; attached to wall, leaving the entrance clear.
 box(-w/2+.3,-.11,.95,.32,.18,.46,'metal')
 beam((-w/2+.3,-.13,1.4),(-w/2+.3,-.13,2.8),.025,'metal')
 for xx in [-w/2+.16,w/2-.16]:beam((xx,.02,.1),(xx,.02,h-.15),.06,'metal')
 b.pot(-w/2+.45,-.45);b.pot(-w/2+.98,-.35)
 # Side windows sit proud of the wall, avoiding coplanar faces.
 for yy in [2.1,d-1.6]:
  n=len(b.V);window(0,-.16,1.25,.85,1.25)
  for i in range(n,len(b.V)):
   x,y,z=b.V[i];b.V[i]=(w/2-y,yy+x,z)
 n=len(b.V);window(0,-.16,3.5,1.,1.4)
 for i in range(n,len(b.V)):
  x,y,z=b.V[i];b.V[i]=(x,d-y,z)

def roof_terrace(w,d,h):
 b.PART='roof_terrace'
 box(0,d/2,h,w+.12,d+.12,.1,'stone')
 for x in [-w/2,w/2]:box(x,d/2,h+.1,.15,d,.86,'plaster')
 for y in [0,d]:box(0,y,h+.1,w,.15,.86,'plaster')
 # Rooftop access room makes the terrace a usable part of the house.
 box(w*.23,d-.9,h+.1,1.5,1.65,1.7,'plaster')
 box(w*.23,d-1.74,h+.1,.7,.05,1.45,'wood_dark')
 box(w*.23,d-.9,h+1.8,1.65,1.8,.12,'stone')

def vault(w,d,h):
 b.PART='vaulted_roof';r=w/2+.1;y0=1.5;y1=d+.06
 for i in range(12):
  a=i*math.pi/12;c=(i+1)*math.pi/12
  x1=r*math.cos(a);x2=r*math.cos(c);z1=h+.85*math.sin(a);z2=h+.85*math.sin(c)
  face([(x1,y0,z1),(x1,y1,z1),(x2,y1,z2),(x2,y0,z2)],'roof_clay')
  for yy in [y0,y1]:face([(x1,yy,h),(x2,yy,h),(x2,yy,z2),(x1,yy,z1)],'plaster')
 box(0,.7,h,w,1.4,.1,'stone');box(0,0,h+.1,w,.14,.8,'plaster')

def shop(v):
 w=[5.5,6.75,6.25][v];d=8.;floors=[2,3,2][v];fh=3.0
 b.PART='italian_shop_shell';xs=[-w*.23,w*.23]
 openings=[(x-.78,x+.78,.0,2.72) for x in xs]
 for level in range(1,floors):
  openings += [(x-.5,x+.5,level*fh+.15,level*fh+2.25) for x in xs]
 b.shell(w,d,fh*floors,'plaster',openings)
 b.PART='arched_storefront'
 for i,x in enumerate(xs):
  b.arched_window(x,.1,.08,1.56,2.64)
  box(x,.13,.1,1.42,.04,.55,'wood_dark')
  box(x+.3,-.01,.98,.045,.1,.2,'trim')
  if i==0:awning(x,0,2.98,2.05)
 for level in range(1,floors):
  b.PART='persiane_balconies'
  for i,x in enumerate(xs):
   window(x,0,level*fh+.15,1.,2.1,door=True,arch=(v==2 and i==0))
   shutters(x,0,level*fh+.15,1.,2.1)
   if level==1 or i==1:balcony(x,0,level*fh+.1,1.8)
 roof_terrace(w,d,fh*floors)
 if v==2:
  # Low roof pergola belongs to the terrace, without a detached full-width cap.
  for xx in [-w*.32,w*.02]:
   for yy in [2.2,4.3]:box(xx,yy,fh*floors+.1,.08,.08,1.9,'wood_dark')
  for i in range(5):box(-w*.15,2.2+i*.525,fh*floors+2.,w*.42,.08,.08,'wood_dark')
 b.PART='small_layered_signs';signs(0,0,2.05)
 services(w,d,fh*floors)
 return dict(footprint_m=[w,d],storeys=floors,front_wall_z=0,wear_locations=['threshold','window sill','shutter edges','lower wall','old sign support'],form='Italian arched shop, persiane, iron balconies, roof terrace; C has a light pergola',life_layer='small Korean signs, meter/conduit, planter row')

def terrace(v):
 global LOUVRES
 # Reuse stepped metre geometry; replace window grammar and roof closure.
 original_window=b.front_window
 def coastal(x,y,z,w,h,lattice=False,shutters=False):window(x,y-.12,z,w,h,door=h>1.8)
 b.front_window=coastal;LOUVRES=3
 info=b.terrace(1 if v==1 else 0)
 b.front_window=original_window;LOUVRES=6
 w=info['footprint_m'][0];levels=info['storeys'];front=(levels-1)*(1.1 if v==1 else .9)
 b.PART='roof_parapet';box(0,front,levels*2.8+.12,w-.35,.14,.55,'plaster')
 if v==2:
  z=levels*2.8+.12
  box(w*.25,5.12,z,1.35,1.55,1.65,'plaster')
  box(w*.25,4.33,z,.7,.05,1.45,'wood_dark')
  box(w*.25,5.12,z+1.65,1.5,1.7,.1,'stone')
  beam((w/2-.15,-.03,.1),(w/2-.15,-.03,z+.45),.065,'metal')
  # A short roof drying rail adds a different upper silhouette and purpose.
  for xx in [-1.7,-.2]:beam((xx,3.9,z),(xx,3.9,z+1.1),.04,'metal')
  beam((-1.7,3.9,z+1.1),(-.2,3.9,z+1.1),.03,'metal')
  box(-1.2,3.9,z+.45,.6,.02,.62,'cloth')
 info.update(form='Stepped Italian roof terraces, arched head, persiane and outside stair',life_layer='laundry, pots, small service parts')
 return info

def warehouse(v):
 info=b.warehouse(v)
 info.update(form='Working harbour shed; weathered sheet metal retained, not a domestic tiled villa',life_layer='Korean service board, loading goods')
 return info

def villa(v):
 global LOUVRES
 w=8.;d=7.;b.PART='cliff_villa_shell'
 xs=[-2.45,0,2.45];ops=[(x-.8,x+.8,0,2.75) for x in xs]
 b.shell(w,d,3.2,'plaster',ops)
 for x in xs:b.arched_window(x,.1,.08,1.6,2.67)
 awning(0,0,2.95,2.05)
 box(0,-.65,0,8.2,1.35,.08,'stone')
 b.PART='upper_retreat';uw=6.;uy=1.25;ux=-.6;z=3.2
 ops=[(x-.5,x+.5,z+.1,z+2.2) for x in [-1.6,1.6]]
 shifted(lambda:b.shell(uw,d-uy,2.8,'plaster',ops,uy,z),ux)
 for x in [-1.6,1.6]:window(x+ux,uy,z+.1,1,2.1,door=True);shutters(x+ux,uy,z+.1,1,2.1)
 box(ux,.6,z-.05,uw+.2,1.3,.12,'stone')
 shifted(lambda:b.railing(uw+.1,0,z+.1),ux)
 for xx in [ux-uw/2,ux+uw/2]:beam((xx,0,z+1.05),(xx,uy,z+1.05),.045,'metal')
 shifted(lambda:roof_terrace(uw,d-uy,6.),ux,uy)
 b.PART='cafe_life';b.bench(-2.6,-.62,w=1.15);b.pot(3.5,-.5);signs(-1.22,0,2.1)
 services(w,d,3.2)
 # Rear service entrance and upper side windows make the whole house usable.
 b.PART='villa_side_rear_life'
 LOUVRES=3
 for yy in [3.1,5.8]:
  n=len(b.V);window(0,-.16,3.65,.85,1.5)
  for i in range(n,len(b.V)):
   x,y,zz=b.V[i];b.V[i]=(ux+uw/2-y,yy+x,zz)
 for xx in [-2.3,2.3]:
  n=len(b.V);window(xx,-.16,1.1,1.,1.5)
  for i in range(n,len(b.V)):
   x,y,zz=b.V[i];b.V[i]=(x,d-y,zz)
 LOUVRES=6
 box(0,d+.06,.08,.85,.10,2.15,'wood_dark')
 box(.32,d+.13,1.05,.06,.07,.18,'trim')
 box(0,d+.28,0,1.15,.65,.10,'stone')
 beam((3.83,6.95,.1),(3.83,6.95,3.15),.06,'metal')
 # Distinguish a solid entrance from glazed bays and partial curtains.
 box(0,.025,.08,1.4,.05,2.1,'wood_dark')
 box(.4,-.02,1.05,.05,.1,.2,'trim')
 for xx in [-2.45,2.45]:
  box(xx,.045,.22,1.35,.035,.65,'cloth')
  box(xx,.025,1.25,1.4,.055,.05,'trim')
 # One modest café table and two stools on the outdoor terrace.
 box(.8,.5,3.94,.75,.6,.07,'wood_dark')
 box(.8,.5,3.3,.08,.08,.64,'metal')
 for xx in [.12,1.48]:
  box(xx,.5,3.72,.35,.35,.06,'wood_dark')
  box(xx,.5,3.3,.075,.075,.42,'metal')
 return dict(footprint_m=[w,d],storeys=2,front_wall_z=0,wear_locations=['terrace coping','entry threshold','shutter edges','drain pipe'],form='Coastal villa/cafe with three masonry arches, recessed upper storey and wide terrace',life_layer='short Korean signs, bench, meter and pots')

def main():
 p=argparse.ArgumentParser();p.add_argument('--type',choices=['shop_house','terrace_house','harbor_warehouse','cliff_villa'],required=True);p.add_argument('--ids',nargs='*');a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);typ=a.type
 OUT.mkdir(parents=True,exist_ok=True);REV.mkdir(parents=True,exist_ok=True);b.REVIEW=REV;b.OUT=OUT
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.scene.unit_settings.system='METRIC'
 count=1 if typ=='cliff_villa' else 2 if typ=='harbor_warehouse' else 3;assets=[];models=[];old=json.loads((ROOT/'art/models/bayloop_buildings_v01/manifest.json').read_text());urls=old['assets'][0]['source_urls']
 for v in range(count):
  if a.ids and typ+'_'+chr(97+v) not in a.ids:continue
  b.V.clear();b.F.clear();b.UV.clear();b.PARTS.clear()
  b.TEX=OUT/('Italy_'+['cream','ochre','blue'][v]+'_Atlas.png');mat=b.material();name=typ+'_'+chr(97+v)
  info={'shop_house':shop,'terrace_house':terrace,'harbor_warehouse':warehouse,'cliff_villa':villa}[typ](v)
  obj=b.mesh(name,mat);tris=sum(len(f)-2 for f in b.F);assert tris<=4000,(name,tris)
  bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
  glb=OUT/(name+'.glb');fbx=OUT/(name+'.fbx')
  bpy.ops.export_scene.gltf(filepath=str(glb),export_format='GLB',use_selection=True,export_yup=True)
  bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,axis_forward='-Z',axis_up='Y',bake_space_transform=True,apply_scale_options='FBX_SCALE_ALL',add_leaf_bones=False,mesh_smooth_type='FACE',path_mode='COPY',embed_textures=True)
  vs=[(x,z,-y) for x,y,z in b.V];lo=[min(q[k] for q in vs) for k in range(3)];hi=[max(q[k] for q in vs) for k in range(3)]
  data=dict(id=name,type=typ,model=b.rel(glb),fbx=b.rel(fbx),texture=b.rel(b.TEX),dimensions_m=[hi[k]-lo[k] for k in range(3)],bounds_min=lo,bounds_max=hi,source_urls=urls,tris=tris,**info)
  (OUT/(name+'.geometry.json')).write_text(json.dumps(dict(vertices_y_up=vs,faces=b.F,part_per_face=b.PARTS,uv_per_face=b.UV)),encoding='utf8');b.register([glb,fbx],urls);assets.append(data);models.append(obj)
 dest=OUT/'manifest.json';m=json.loads(dest.read_text()) if dest.exists() else {k:old[k] for k in ['palette','density_px_m','max_tris','source_manifests']};m['assets']=[x for x in m.get('assets',[]) if x['id'] not in {q['id'] for q in assets}]+assets;dest.write_text(json.dumps(m,ensure_ascii=False,indent=2),encoding='utf8')
 sc=b.stage();sc.cycles.samples=16
 for i,obj in enumerate(models):
  for q in models:q.hide_render=q!=obj
  h=assets[i]['dimensions_m'][1];scale=max(20,h*2.65,assets[i]['dimensions_m'][0]*1.8)
  b.render(sc,REV/(obj.name+'_front.png'),(13,-22,12),(0,3,h*.46),scale)
 for i,obj in enumerate(models):
  for q in models:q.hide_render=q!=obj
  h=assets[i]['dimensions_m'][1];scale=max(20,h*2.65,assets[i]['dimensions_m'][0]*1.8)
  for view,loc in [('back',(-14,22,13)),('side',(30,3,h*.5)),('top',(0,3,35))]:b.render(sc,REV/(obj.name+'_'+view+'.png'),loc,(0,3,0) if view=='top' else (0,3,h*.46),scale)
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/((a.ids[0] if a.ids else typ)+'.blend')))
 print('ITALY_DONE',[(x['id'],x['tris']) for x in assets])
if __name__=='__main__':main()
