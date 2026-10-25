"""D140 deterministic metre-scale building grammar; Blender background only.
--catalog writes 63 reproducible recipes. --type TYPE [--ids ...] builds them.
--config JSON accepts a list of explicit recipes; no Unity invocation.
"""
from pathlib import Path
import sys,math,json,argparse,random,csv,hashlib
import bpy
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bayloop_buildings as b
import bayloop_italy_buildings as italy
ROOT=b.ROOT;OUT=ROOT/'art/models/bayloop_variety_v01';REV=ROOT/'art/_review/2026-10-25-bayloop-variety'
COUNTS={'town_house':16,'detached_house':8,'shop_house':12,'terrace_house':12,'cliff_villa':6,'farmhouse':4,'chapel_bell':1,'school_small':1,'bus_stop':1,'fuel_stand':1,'cafe_kiosk':1}
COLORS=['cream','chalk','sand','ochre','blue','mist','rose','warm_grey']
ROOFS=['flat','terracotta_gable','vault','metal']
FACILITIES={'chapel_bell':('terracotta_gable','entry_steps',1),'school_small':('terracotta_gable','entry_steps',2),'bus_stop':('flat','windows',1),'fuel_stand':('flat','garage',1),'cafe_kiosk':('terracotta_gable','shop_arch',1)}
box=b.box;face=b.face;beam=b.beam

def catalog():
 recipes=[]
 for ti,(typ,count) in enumerate(COUNTS.items()):
  for j in range(count):
   seed=140000+ti*100+j;r=random.Random(seed)
   w=round([4.6,5.4,6.25,7.2,8.1,9.2,5.8,7.65][j%8],2);d=[6.,7.2,8.,6.6][(j+ti)%4]
   floors=2+j%2;roof=['flat','terracotta_gable','flat','vault','flat','terracotta_gable','metal','flat'][j%8]
   zone='medium';ground=['entry_steps','windows','garage'][j%3]
   if typ=='shop_house':zone='dense';ground='shop_arch';floors=[2,3,2,4][j%4]
   elif typ=='terrace_house':zone='dense' if j%3==0 else 'medium';floors=[2,3,2,4][j%4];roof='flat';ground='entry_steps'
   elif typ in ('detached_house','cliff_villa','farmhouse'):zone='sparse';w=[6.2,7.4,8.6,9.6][j%4];floors=1+j%2;ground='entry_steps'
   if typ=='cliff_villa':floors=2;roof='flat';d=7.+j%3
   if typ=='farmhouse':roof='terracotta_gable' if j%2==0 else 'metal'
   if typ=='detached_house' and j==3:roof='terracotta_gable'
   special={'chapel_bell':(7,9,1),'school_small':(10,9,2),'bus_stop':(4.8,2.2,1),'fuel_stand':(7,5,1),'cafe_kiosk':(4,3.5,1)}
   if typ in special:w,d,floors=special[typ];zone='sparse' if typ in ('chapel_bell','fuel_stand') else 'medium';roof,ground,floors=FACILITIES[typ]
   recipes.append(dict(id=f'{typ}_{j+1:02}',type=typ,width=w,depth=d,floors=floors,roof=roof,ground_floor=ground,
    opening_rhythm=['paired','regular','offset'][j%3],balcony=['none','single','full','alternating'][j%4],
    shutter_color='green' if j%3 else 'brown',awning=j%3!=1,wall_color=COLORS[(j+ti*2)%8],wear=['light','medium','heavy'][j%3],
    life=dict(sign_count=(1+j%3) if typ=='shop_house' else j%2,ac_unit=bool(j%3==0),planters=1+j%3,laundry=bool(j%2)),seed=seed,zone=zone,
    front_space_m=3.2+(j%3)*.45 if typ=='detached_house' else 1.8 if zone=='sparse' else 1.25))
 for p in recipes:
  if p['id']=='detached_house_04':p['wall_color']='cream'
  if p['id']=='shop_house_01':
   p['wall_color']='cream';p['shop_display']=True;p['life']['planters']=0
  if p['id'] in ('terrace_house_06','terrace_house_12'):
   p['life'].update(planters=1 if p['id'].endswith('06') else 2,sign_count=0)
   p['balcony']='alternating'
   p['terrace_living']=True
   if p['id'].endswith('12'):p['wall_color']='ochre'
  if p['type'] in FACILITIES:
   p.update(opening_rhythm='regular',balcony='none',shutter_color='green',awning=p['type']=='cafe_kiosk',wear='medium',life=dict(sign_count=0,ac_unit=False,planters=0,laundry=False),front_space_m=0)
 return recipes

def validate(p):
 assert isinstance(p.get('terrace_living',False),bool),p
 assert isinstance(p.get('shop_display',False),bool),p
 assert isinstance(p['floors'],int) and not isinstance(p['floors'],bool) and isinstance(p['seed'],int),p
 assert p['type'] in COUNTS,p
 assert 4<=p['width']<=10 and 2<=p['depth']<=14 and 1<=p['floors']<=4,p
 assert p['roof'] in ROOFS and p['wall_color'] in COLORS,p
 assert p['ground_floor'] in ['shop_arch','garage','entry_steps','windows'],p
 assert p['wear'] in ['light','medium','heavy'] and p['shutter_color'] in ['brown','green'],p
 assert p['opening_rhythm'] in ['paired','regular','offset'] and p['balcony'] in ['none','single','full','alternating'],p
 assert 0<=p['life']['sign_count']<=3 and 0<=p['life']['planters']<=4,p
 assert isinstance(p['life']['ac_unit'],bool) and isinstance(p['life']['laundry'],bool) and isinstance(p['awning'],bool),p
 if p['type'] in ('terrace_house','cliff_villa'):
  setback=.9 if p['type']=='terrace_house' else 1.25
  assert p['depth']-(p['floors']-1)*setback>=3.,'Upper storey must retain >=3m usable depth'
 if p['type'] in FACILITIES:
  roof,ground,floors=FACILITIES[p['type']]
  assert (p['roof'],p['ground_floor'],p['floors'])==(roof,ground,floors),'Facilities use fixed functional profiles; use a house type for the complete facade grammar'
  assert p['opening_rhythm']=='regular' and p['balcony']=='none' and p['shutter_color']=='green' and p['awning']==(p['type']=='cafe_kiosk') and p['wear']=='medium' and p['life']==dict(sign_count=0,ac_unit=False,planters=0,laundry=False),'Unsupported facility override'

def moved(fn,dx=0,dy=0,dz=0,side=None):
 n=len(b.V);fn()
 for k in range(n,len(b.V)):
  x,y,z=b.V[k]
  if side=='right':x,y=-y,x
  elif side=='left':x,y=y,-x
  elif side=='back':x,y=-x,-y
  b.V[k]=(x+dx,y+dy,z+dz)

def shell(w,d,h,ops,y=0,z=0,x=0,tile='plaster'):
 # Planar wall grammar with actual front holes and recessed jambs.
 xs=sorted(set([-w/2,w/2]+[a for op in ops for a in op[:2]]));zs=sorted(set([z,z+h]+[a for op in ops for a in op[2:]]))
 assert all(-w/2<=a<c<=w/2 and z<=lo<hi<=z+h for a,c,lo,hi in ops)
 for a,c in zip(xs,xs[1:]):
  for lo,hi in zip(zs,zs[1:]):
   if any(a>=l-1e-6 and c<=r+1e-6 and lo>=bot-1e-6 and hi<=top+1e-6 for l,r,bot,top in ops):continue
   face([(x+a,y,lo),(x+c,y,lo),(x+c,y,hi),(x+a,y,hi)],tile)
 for a,c,lo,hi in ops:
  for q in [[(a,lo),(c,lo)],[(c,lo),(c,hi)],[(c,hi),(a,hi)],[(a,hi),(a,lo)]]:
   (aa,zz),(cc,tt)=q;face([(x+aa,y,zz),(x+cc,y,tt),(x+cc,y+.18,tt),(x+aa,y+.18,zz)],'trim')
 box(x-w/2+.08,y+(d+.18)/2,z,.16,d-.18,h,tile);box(x+w/2-.08,y+(d+.18)/2,z,.16,d-.18,h,tile)
 box(x,y+d-.08,z,w-.32,.16,h,tile);box(x,y+d/2,z,w-.32,d-.32,.06,'stone');box(x,y+d/2,z+h-.08,w-.32,d-.32,.08,'stone')

def window(x,y,z,w=1,h=1.5,door=False,shutters=True,arch=False):
 if arch:b.arched_window(x,y+.12,z,w,h)
 else:
  face([(x-w/2,y+.17,z),(x+w/2,y+.17,z),(x+w/2,y+.17,z+h),(x-w/2,y+.17,z+h)],'glass')
  for xx in [x-w/2,x+w/2]:box(xx,y-.015,z,.065,.12,h,'trim')
  for zz in [z,z+h]:box(x,y-.025,zz,w+.1,.14,.065,'trim')
  box(x,y+.02,z,.04,.07,h,'wood_dark');box(x,y-.03,z+h*.56,w,.06,.035,'wood_dark')
  box(x,y-.07,max(0,z-.065),w+.17,.25,.065,'stone')
 if door:
  box(x,y+.08,z,w-.08,.04,.6,'wood_dark');box(x+w*.3,y-.02,z+1,.035,.07,.16,'metal')
 elif shutters:
  for xx in [x-w*.76,x+w*.76]:
   box(xx,y-.075,z,w*.43,.08,h,'teal')
   for k in [1,2]:box(xx,y-.125,z+k*h/3,w*.39,.035,.035,'wood_dark')
 # A partial photo-cloth curtain leaves a dark depth interval above it.
 if not door:face([(x-w*.4,y+.145,z+.1),(x+w*.4,y+.145,z+.1),(x+w*.4,y+.145,z+h*.38),(x-w*.4,y+.145,z+h*.38)],'cloth')

def rail(w,y,z,x=0):
 for xx in [x-w/2,x+w/2]:box(xx,y,z,.045,.045,.95,'metal')
 box(x,y,z+.95,w,.045,.045,'metal')
 for k in range(1,max(2,round(w/.36))):box(x-w/2+k*w/round(w/.36),y,z,.025,.025,.9,'metal')

def balcony(x,y,z,w,depth=.7):
 box(x,y-depth/2,z-.07,w,depth,.1,'stone');rail(w-.08,y-depth+.035,z+.04,x)
 for xx in [x-w/2+.05,x+w/2-.05]:beam((xx,y-depth,z+1),(xx,y,z+1),.035,'metal')

def rooftop(p,w,d,z,y=0,x=0):
 b.PART='roof_'+p['roof']
 if p['roof']=='flat':
  box(x,y+d/2,z,w+.12,d+.12,.08,'stone')
  for xx in [x-w/2,x+w/2]:box(xx,y+d/2,z+.08,.13,d,.64,'plaster')
  for yy in [y,y+d]:box(x,yy,z+.08,w,.13,.64,'plaster')
  # Each flat roof has a plausible small access room, varied to avoid identical silhouettes.
  ax=x+(-.22 if p['seed']%2 else .22)*w;ay=y+d-1.
  box(ax,ay,z+.08,1.25,1.55,1.65,'plaster');box(ax,ay-.8,z+.08,.66,.045,1.45,'wood_dark');box(ax,ay,z+1.73,1.4,1.7,.09,'stone')
 elif p['roof'] in ('terracotta_gable','metal'):
  moved(lambda:b.roof(w,d,z,1.1 if p['roof']=='terracotta_gable' else .65,'roof_clay' if p['roof']=='terracotta_gable' else 'metal',along_x=False),x,y)
  box(x-w*.25,y+d*.65,z+.4,.42,.48,1.35,'stonewall');box(x-w*.25,y+d*.65,z+1.75,.55,.59,.08,'stone')
 else:
  radius=w/2+.06;rise=.85
  for k in range(10):
   a=k*math.pi/10;c=(k+1)*math.pi/10;xx1=x+radius*math.cos(a);xx2=x+radius*math.cos(c);z1=z+rise*math.sin(a);z2=z+rise*math.sin(c)
   face([(xx1,y-.12,z1),(xx1,y+d+.12,z1),(xx2,y+d+.12,z2),(xx2,y-.12,z2)],'roof_clay')
   for yy in [y-.12,y+d+.12]:face([(xx1,yy,z),(xx2,yy,z),(xx2,yy,z2),(xx1,yy,z1)],'plaster')

def yard(p):
 w=p['width'];depth=p['front_space_m'];b.PART='private_front_yard'
 # An open gate + continuous path distinguishes private space from the pavement supplied by the map builder.
 box(0,-depth/2,0,w+.8,depth,.055,'stonewall');box(-w*.17,-depth/2,.055,1.15,depth,.025,'stone')
 for xx in [-w/2-.32,w/2+.32]:box(xx,-depth/2,.055,.18,depth,.7,'stonewall')
 gatex=-w*.17;gap=1.2;left=-w/2-.32;right=w/2+.32
 for a,c in [(left,gatex-gap/2),(gatex+gap/2,right)]:box((a+c)/2,-depth,.055,c-a,.18,.72,'stonewall')
 for xx in [gatex-gap/2,gatex+gap/2]:box(xx,-depth,.055,.22,.22,1.02,'plaster')
 # Gate leaf is swung along the path; entrance remains physically open.
 box(gatex-gap/2,-depth+.45,.12,.045,.9,.8,'wood_dark')
 b.bench(w*.25,-depth*.6,.08,w=1.15)

def life(p,w,d,h):
 b.PART='eye_level_life';n=p['life']['sign_count']
 for k in range(n):
  x=-w/2+.6+(k%2)*.1;z=2.1-k*.5;ww=.95
  offset=k*32 if p['type']=='shop_house' else 96
  box(x,-.09,z,ww,.075,.45,'wood_dark');face([(x-ww/2,-.131,z),(x+ww/2,-.131,z),(x+ww/2,-.131,z+.45),(x-ww/2,-.131,z+.45)],'sign',offset=(0,offset))
 for k in range(p['life']['planters']):b.pot(w/2-.35-k*.48,-.4)
 if p['life']['ac_unit']:
  x=w/2-.6;box(x,-.23,2.35,.78,.38,.5,'trim');box(x-.15,-.44,2.43,.32,.025,.32,'metal')
  for zz in [2.48,2.58,2.68]:box(x+.22,-.45,zz,.22,.02,.025,'metal')
  beam((x+.35,-.25,2.4),(x+.45,-.1,1.8),.04,'metal')
 if p['life']['laundry']:
  yy=d+.12;beam((-1.2,yy,2.1),(.8,yy,2.1),.025,'metal')
  for x,hh in [(-.85,.65),(.15,.45)]:box(x,yy,2.1-hh,.55,.025,hh,'cloth')
 beam((w/2-.16,-.04,.05),(w/2-.16,-.04,h-.1),.055,'metal')
 # Local damage is concentrated at a lower strip and pipe foot, never random full-wall noise.
 level={'light':.09,'medium':.18,'heavy':.32}[p['wear']]
 box(w*.3,-.018,.04,w*.27,.022,level,'wear_wall')
 box(w/2-.16,-.021,.05,.22,.023,.65 if p['wear']=='heavy' else .28,'wear_wall')

def house(p):
 w=p['width'];d=p['depth'];floors=p['floors'];fh=2.8;typ=p['type'];r=random.Random(p['seed'])
 terrace=typ in ('terrace_house','cliff_villa');ground=p['ground_floor'];step=.9 if typ=='terrace_house' else 1.25 if typ=='cliff_villa' else 0
 doorx=-w*.17 if typ=='detached_house' else w*.24
 for level in range(floors):
  z=level*fh;y=level*step;ww=w-(.35*level if typ=='terrace_house' else w*.2 if typ=='cliff_villa' and level else 0)
  bays=2 if ww<7 or floors==4 else 3;xs=[(k-(bays-1)/2)*(ww/(bays+.5)) for k in range(bays)]
  if p['opening_rhythm']=='paired':xs=[-ww*.23,ww*.23]
  elif p['opening_rhythm']=='offset':xs=[x+(.12 if k%2 else -.12) for k,x in enumerate(xs)]
  xs=[x+r.choice([-.035,0,.035]) for x in xs]
  apertures=[]
  if level==0 and ground=='shop_arch':
   for x in xs:apertures.append((x,.08,1.45,2.47,'arch'))
  elif level==0:
   if ground=='garage':apertures=[(-ww*.17,0,min(2.5,ww*.46),2.35,'garage'),(ww*.32,.08,.78,2.1,'door')]
   else:
    apertures=[(doorx,.14 if ground=='entry_steps' else .05,.9,2.15,'door')]
    for x in xs:
     if abs(x-doorx)>1.35:apertures.append((x,.85,.9,1.25,'window'))
  else:
   for k,x in enumerate(xs):
    on_balcony=terrace or p['balcony']=='full' or p['balcony']=='single' and k==0 or p['balcony']=='alternating' and (k+level)%2==0
    if terrace and p.get('terrace_living',False):on_balcony=(k+level)%2==0
    apertures.append((x,.1 if on_balcony else .8,.92,2.15 if on_balcony else 1.35,'door' if on_balcony else 'window'))
  ops=[(x-a/2,x+a/2,z+zz,z+zz+hh) for x,zz,a,hh,kind in apertures]
  b.PART='storey_'+str(level);shell(ww,d-y,fh,ops,y,z,tile='stonewall' if typ=='farmhouse' and level==0 else 'plaster')
  for k,(x,zz,a,hh,kind) in enumerate(apertures):
   if kind=='garage':
    box(x,y+.13,z+zz,a,.05,hh,'metal')
    for t in range(1,7):box(x,y+.09,z+zz+t*hh/7,a,.035,.025,'wood_dark')
   else:window(x,y,z+zz,a,hh,door=kind in ('door','arch'),shutters=kind=='window' or level>0,arch=kind=='arch')
   if kind=='arch' and p['awning'] and k==0:moved(lambda:italy.awning(0,0,2.73,min(2.1,ww*.42)),x,y,z)
   if level>0 and kind=='door' and not terrace:balcony(x,y,z+.05,min(1.55,ww/bays-.2))
   if level==0 and kind=='door' and ground=='entry_steps':
    box(x,y-.3,0,1.15,.6,.08,'stone');box(x,y-.16,.08,1.05,.3,.07,'stone')
  if terrace and level:
   box(0,y-step/2,z-.04,ww+.15,step,.1,'stone');rail(ww-.08,y-step+.07,z+.08)
   for xx in [-ww/2,ww/2]:beam((xx,y-step,z+1.03),(xx,y,z+1.03),.04,'metal')
   if p.get('terrace_living',False):
    xx=(-1 if level%2 else 1)*ww*.27
    if level%2:
     beam((xx-.6,y-.45,z+1.65),(xx+.6,y-.45,z+1.65),.025,'metal')
     face([(xx-.5,y-.45,z+.8),(xx+.05,y-.45,z+.8),(xx+.05,y-.45,z+1.65),(xx-.5,y-.45,z+1.65)],'cloth')
    else:
     box(xx,y-.45,z+.1,.08,.08,.62,'wood_dark');box(xx,y-.45,z+.72,.65,.5,.055,'wood_dark')
  # Every exposed rear/side gets a different but inhabited window rhythm.
  for yy in [max(y+1.7,2.),d-1.5]:
   if yy>y+.8:moved(lambda:window(0,-.23,z+.85,.8,1.2,shutters=False),ww/2,yy,side='right')
  moved(lambda:window(0,-.23,z+.85,.9,1.2,shutters=level%2==0),-ww*.18,d,side='back')
  if level==floors-1:rooftop(p,ww,d-y,z+fh,y)
 life(p,w,d,floors*fh)
 if p.get('shop_display',False):
  b.PART='shop_produce_display';xx=w*.23
  box(xx,-.45,0,1.12,.65,.72,'wood_dark')
  for zz in [.18,.4,.62]:box(xx,-.79,zz,1.14,.035,.045,'wood_dark')
  for k in range(5):
   box(xx-.43+k*.21,-.45,.72,.16,.3,.12,'teal' if k%2 else 'cloth')
  box(0,-.07,3.03,1.05,.08,.44,'wood_dark')
  face([(-.5,-.12,3.05),(.5,-.12,3.05),(.5,-.12,3.45),(-.5,-.12,3.45)],'sign',offset=(0,0))
 if typ=='detached_house':yard(p)
 elif p['front_space_m']>=1.5:
  box(0,-.65,0,w+.2,1.3,.055,'stone');b.bench(-w*.25,-.5,.055,1.2)
 if typ=='farmhouse':
  # A clearly subordinate lean-to for stored tools, outside the house's main volume.
  xx=-w/2-1.05;box(xx,d*.64,0,2.1,d*.54,2.1,'stonewall');box(xx,d*.37-.02,.06,1.2,.06,1.85,'wood_dark')
  face([(xx-1.15,d*.35,2.12),(xx+1.15,d*.35,2.42),(xx+1.15,d*.93,2.42),(xx-1.15,d*.93,2.12)],'metal')
 return dict(footprint_m=[w,d],storeys=floors,front_wall_z=0,zone=p['zone'],front_space_m=p['front_space_m'],front_space_owner='private yard/entry furniture; public pavement supplied by map builder',wear_locations=['lower entry wall','drainpipe foot','shutter edges','threshold'])

def main():
 parser=argparse.ArgumentParser();parser.add_argument('--catalog',action='store_true');parser.add_argument('--type',choices=list(COUNTS));parser.add_argument('--ids',nargs='*');parser.add_argument('--config');parser.add_argument('--no-render',action='store_true');parser.add_argument('--geometry-only',action='store_true');parser.add_argument('--views',default='front');args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
 OUT.mkdir(parents=True,exist_ok=True);REV.mkdir(parents=True,exist_ok=True)
 recipes=json.loads(Path(args.config).read_text(encoding='utf8')) if args.config else catalog()
 assert recipes and len({p['id'] for p in recipes})==len(recipes),'Empty recipes or duplicate ids'
 if args.catalog:(OUT/'recipes.json').write_text(json.dumps(recipes,ensure_ascii=False,indent=2),encoding='utf8');print('RECIPES',len(recipes));return
 from bayloop_buildgen_textures import select_style
 from bayloop_buildgen_facilities import build as facility
 old=json.loads((ROOT/'art/models/bayloop_italy_v01/manifest.json').read_text());urls=old['assets'][0]['source_urls'];assets=[]
 selected=[p for p in recipes if (not args.type or p['type']==args.type) and (not args.ids or p['id'] in args.ids)]
 assert selected and (not args.ids or set(args.ids)=={p['id'] for p in selected}),'Selection empty or requested IDs incompatible with type/catalog'
 for p in recipes:
  if args.type and p['type']!=args.type or args.ids and p['id'] not in args.ids:continue
  validate(p);bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.scene.unit_settings.system='METRIC'
  b.V.clear();b.F.clear();b.UV.clear();b.PARTS.clear();b.PART='shell'
  style=select_style(p['wall_color'],p['wear'],p['shutter_color']);b.TEX=Path(style['atlas']);b.TILES.update(style['tiles'])
  mat=b.material();info=house(p) if p['type'] not in ['chapel_bell','school_small','bus_stop','fuel_stand','cafe_kiosk'] else facility(p,b,italy)
  info.update(parameter_profile='full_house_grammar' if p['type'] not in FACILITIES else 'fixed_functional_facility: width/depth/palette; fixed roof/floors/openings; seed records identity',zone=p['zone'],front_space_m=p['front_space_m'])
  name=p['id'];tris=sum(len(f)-2 for f in b.F);assert 0<tris<=4000,(name,tris)
  if args.geometry_only:
   assets.append(dict(id=name,tris=tris,parameters=p,geometry_sha256=hashlib.sha256(json.dumps([b.V,b.F,b.UV],sort_keys=True).encode()).hexdigest()));continue
  obj=b.mesh(name,mat);bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
  glb=OUT/(name+'.glb');fbx=OUT/(name+'.fbx')
  bpy.ops.export_scene.gltf(filepath=str(glb),export_format='GLB',use_selection=True,export_yup=True)
  bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,axis_forward='-Z',axis_up='Y',bake_space_transform=True,apply_scale_options='FBX_SCALE_ALL',add_leaf_bones=False,mesh_smooth_type='FACE',path_mode='COPY',embed_textures=True)
  vs=[(x,z,-y) for x,y,z in b.V];lo=[min(v[k] for v in vs) for k in range(3)];hi=[max(v[k] for v in vs) for k in range(3)]
  data=dict(id=name,type=p['type'],model=b.rel(glb),fbx=b.rel(fbx),texture=b.rel(b.TEX),tris=tris,dimensions_m=[hi[k]-lo[k] for k in range(3)],bounds_min=lo,bounds_max=hi,source_urls=urls,parameters=p,**info)
  assets.append(data);(OUT/(name+'.geometry.json')).write_text(json.dumps(dict(vertices_y_up=vs,faces=b.F,part_per_face=b.PARTS,uv_per_face=b.UV)),encoding='utf8')
  # Save geometry before review lights/ground; exact recipe is embedded in the object.
  obj['buildgen_recipe']=json.dumps(p);bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(name+'.blend')))
  if not args.no_render:
   sc=b.stage();sc.cycles.samples=12;h=hi[1];scale=max(18,h*2.2,(hi[0]-lo[0])*1.7)
   count=COUNTS[p['type']];representatives=([1,6,12] if count>=12 else [1,2,4] if count>=4 else [1]);number=int(name.rsplit('_',1)[1])
   views=list(dict.fromkeys(args.views.split(',')+(['back','side','top'] if number in representatives else [])))
   for view in views:
    loc={'front':(13,-23,12),'back':(-15,23,12),'side':(30,d if (d:=p['depth']/2) else 3,h*.5),'top':(0,p['depth']/2,35)}[view]
    b.render(sc,REV/(name+'_'+view+'.png'),loc,(0,p['depth']/2,h*.43) if view!='top' else (0,p['depth']/2,0),scale)
  print('BUILDGEN_ASSET',name,tris,flush=True)
 # Per-type manifests avoid concurrent write races; merge centrally afterwards.
 if args.geometry_only:
  (REV/'geometry-preflight.json').write_text(json.dumps(dict(pass_=True,count=len(assets),assets=assets),indent=2),encoding='utf8');print('GEOMETRY_PREFLIGHT',len(assets),'maxtris',max(a['tris'] for a in assets));return
 manifest={k:old[k] for k in ['palette','density_px_m','max_tris','source_manifests']};manifest['assets']=assets
 suffix=args.type or 'custom';dest=OUT/('manifest-'+suffix+'.json');existing=json.loads(dest.read_text())['assets'] if dest.exists() else []
 manifest['assets']=[a for a in existing if a['id'] not in {x['id'] for x in assets}]+assets;dest.write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf8')
 print('BUILDGEN_DONE',suffix,len(assets))
if __name__=='__main__':main()
