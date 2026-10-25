"""D137 real-sized street kit. CC0 photo atlas, metric planar UV, no AI images."""
from pathlib import Path
import sys,json,math,hashlib,argparse
sys.path.insert(0,str(Path(__file__).parent))
import bayloop_buildings as B
import bpy
R=B.ROOT;O=R/'art/models/bayloop_italy_props_v01';REV=R/'art/_review/2026-10-25-bayloop-italy/props'
B.TEX=O/'PropsPhotoAtlas.png'
box=B.box;beam=B.beam;face=B.face
FAILED={'small_signs_b','small_signs_d','planter_row','meter_box','bicycle','lemon_tree_pot'}
LAST_FAILED=FAILED-{'bicycle'}
def cyl(x,y,z,r,h,tile='rust',n=10,r2=None,cap=True):
 r2=r if r2 is None else r2
 lo=[(x+r*math.cos(i*2*math.pi/n),y+r*math.sin(i*2*math.pi/n),z) for i in range(n)]
 hi=[(x+r2*math.cos(i*2*math.pi/n),y+r2*math.sin(i*2*math.pi/n),z+h) for i in range(n)]
 for i in range(n):j=(i+1)%n;face([lo[i],lo[j],hi[j],hi[i]],tile)
 face(list(reversed(lo)),tile)
 if cap:face(hi,tile)
def ring(x,y,z,r,thick=.035,n=16,tile='darkroof'):
 for i in range(n):
  a=i*2*math.pi/n;b=(i+1)*2*math.pi/n
  for side in range(4):
   pts=[]
   for ang,cross in [(a,side),(b,side),(b,(side+1)%4),(a,(side+1)%4)]:
    t=math.pi/4+cross*math.pi/2;rr=r+thick*math.cos(t);pts.append((x+rr*math.cos(ang),y+thick*math.sin(t),z+rr*math.sin(ang)))
   face(pts,tile)
def foliage(x,y,z,size=.3,seed=0,origin=None,count=8):
 for i in range(count):
  a=i*2.399+seed
  ux=math.cos(a);uy=math.sin(a);length=.055+.009*(i%4);width=.025+.004*(i%3)
  coords=[(-1,0),(-.5,-.86),(.5,-.86),(1,0),(.5,.86),(-.5,.86)]
  pitch=.15+.38*math.sin(a+seed);roll=.25*math.cos(a*.7)
  origin=origin or (x,y,z-.2);t=.38+i*.085 if count==8 else .18+i*.8/(count-1);attach=tuple(origin[k]+t*((x,y,z)[k]-origin[k]) for k in range(3));cx=attach[0]+length*ux;cy=attach[1]+length*uy;cz=attach[2]+length*pitch
  outline=[(cx+u*length*ux-v*width*uy,cy+u*length*uy+v*width*ux,cz+u*length*pitch+v*width*roll) for u,v in coords]
  centre=(cx,cy,cz+.01)
  for j in range(6):face([outline[j],outline[(j+1)%6],centre],'stonewall')
def hollowpot(x,y,h,r0,r1,n=8):
 cyl(x,y,0,r0,h,'rust',n,r1,False)
 inner=r1-.035
 for i in range(n):
  a=i*2*math.pi/n;b=(i+1)*2*math.pi/n
  p=lambda r,t,z:(x+r*math.cos(t),y+r*math.sin(t),z)
  face([p(r1,a,h),p(r1,b,h),p(inner,b,h),p(inner,a,h)],'rust')
  face([p(inner,a,h),p(inner,b,h),p(inner,b,h-.075),p(inner,a,h-.075)],'rust')
 cyl(x,y,h-.085,inner,.01,'wood_dark',n)
def planter(x,y,scale=1):
 hollowpot(x,y,.375*scale,.15*scale,.225*scale)
 for k in range(3):
  seed=x*4; a=k*2.1+seed;xx=x+math.sin(a)*.1*scale;yy=y+math.cos(a)*.1*scale;zz=.48*scale+k*.11
  beam((x,y,.29*scale),(xx,yy,zz),.017,'teal');foliage(xx,yy,zz,.33*scale,k+seed,(x,y,.29*scale))
def vending():
 box(0,0,.09,1.181,.731,1.74,'trim');box(0,-.376,.12,1.11,.035,1.64,'rose_wall')
 box(-.125,-.402,.73,.79,.035,.89,'glass')
 for z in [.78,1.07,1.36]:
  box(-.125,-.437,z,.79,.09,.04,'metal')
  for i in range(5):
   x=-.43+i*.15;box(x,-.45,z+.05,.083,.07,.155,['trim','teal','rust'][i%3]);box(x,-.455,z+.205,.06,.055,.02,'metal')
 box(.42,-.421,1.08,.16,.03,.22,'darkroof');box(.42,-.448,1.19,.1,.01,.027,'metal');box(.42,-.448,1.09,.06,.01,.04,'trim')
 box(-.12,-.415,.29,.66,.035,.23,'darkroof');box(-.12,-.46,.27,.7,.12,.045,'metal')
 for x in [-.45,.45]:box(x,0,0,.12,.56,.09,'darkroof')
 for z in [.32,.41,.5]:box(.425,-.425,z,.18,.025,.025,'metal')
def signs(v):
 w,h=[(.78,.4),(30/32+.07,18/32+.06),(1.10,.50),(16/32+.07,18/32+.06)][v]
 depth=.03 if v in [1,3] else .08
 box(0,-depth/2,0,w,depth,h,'wood_dark')
 face([(-w/2+.035,-depth-.005,.03),(w/2-.035,-depth-.005,.03),(w/2-.035,-depth-.005,h-.03),(-w/2+.035,-depth-.005,h-.03)],'sign',(0,32*v))
 for x in [-w*.4,w*.4]:box(x,-depth-.008 if v in [1,3] else -.091,h*.8,.018 if v in [1,3] else .025,.006 if v in [1,3] else .012,.018 if v in [1,3] else .025,'metal')
def row():
 for x,s,y in [(-.55,1,0),(0,.8,.08),(.52,1.15,-.04)]:planter(x,y,s)
def ac():
 box(0,0,.08,.765,.285,.55,'trim');box(-.13,-.152,.17,.43,.02,.37,'glass')
 ring(-.13,-.17,.355,.166,.019,12,'metal')
 for i in range(6):box(-.13,-.19,.22+i*.052,.34,.025,.015,'metal')
 for x in [-.24,.24]:box(x,0,0,.075,.36,.08,'metal')
 for z in [.19,.27,.35,.43,.51]:box(.24,-.155,z,.17,.03,.02,'metal')
 beam((.38,.05,.18),(.43,.05,.18),.045,'metal');beam((.43,.05,.18),(.43,.1,.55),.045,'metal')
def meter():
 box(0,-.09,0,.46,.18,.68,'metal');box(0,-.186,.025,.425,.015,.63,'darkroof');box(0,-.199,.038,.40,.012,.605,'trim')
 face([(-.1875,-.215,.31),(.1875,-.215,.31),(.1875,-.215,.6225),(-.1875,-.215,.6225)],'sign',(0,128))
 ring(0,-.22,.19,.085,.012,10,'metal');beam((0,-.235,.19),(.045,-.235,.235),.012,'darkroof')
 for a in [0,math.pi/2,math.pi,math.pi*1.5]:beam((.065*math.cos(a),-.235,.19+.065*math.sin(a)),(.08*math.cos(a),-.235,.19+.08*math.sin(a)),.01,'darkroof')
 for z in [.12,.44]:box(-.2,-.209,z,.035,.025,.06,'metal')
 box(.15,-.225,.22,.025,.018,.045,'metal')
 beam((-.11,-.06,.68),(-.11,-.06,.8),.032,'metal');beam((-.11,-.06,.8),(-.11,.05,.8),.032,'metal')
 beam((.1,-.06,0),(.1,-.06,-.12),.032,'metal');beam((.1,-.06,-.12),(.1,.05,-.12),.032,'metal')
def mailbox():
 box(0,-.12,0,.36,.24,.46,'rose_wall');box(0,-.24,.33,.28,.015,.035,'darkroof')
 face([(-.2,-.27,.46),(.2,-.27,.46),(.2,.015,.5),(-.2,.015,.5)],'rust')
 box(0,-.25,.12,.2,.02,.065,'trim');box(.12,-.26,.2,.027,.02,.04,'metal')
def bicycle():
 for x in [-.55,.55]:
  ring(x,0,.335,.31,.035,16)
  for i in range(4):
   a=i*math.pi/4;beam((x-.29*math.cos(a),0,.335-.29*math.sin(a)),(x+.29*math.cos(a),0,.335+.29*math.sin(a)),.008,'metal')
 rear=(-.55,0,.335);crank=(-.12,0,.32);seat=(-.22,0,.75);head=(.35,0,.77);front=(.55,0,.335)
 for a,b in [(rear,crank),(rear,seat),(crank,seat),(seat,head),(head,crank),(head,front)]:beam(a,b,.037,'teal')
 beam(seat,(-.24,0,.87),.025,'metal');box(-.25,0,.86,.24,.15,.05,'darkroof')
 beam(head,(.28,0,1.03),.045,'metal');beam((.28,0,1.03),(.18,0,1.03),.035,'metal');beam((.18,-.25,1.03),(.18,.25,1.03),.035,'metal')
 box(.55,0,.82,.32,.34,.025,'metal')
 for yy in [-.17,.17]:beam((.39,yy,1.02),(.71,yy,1.02),.018,'metal')
 for xx in [.39,.71]:
  beam((xx,-.17,1.02),(xx,.17,1.02),.018,'metal')
  for yy in [-.17,.17]:beam((xx,yy,.83),(xx,yy,1.02),.012,'metal')
 ring(-.12,-.07,.32,.085,.012,8,'metal');ring(-.55,-.07,.335,.045,.01,6,'metal')
 for zz,z2 in [(.405,.38),(.235,.29)]:beam((-.12,-.07,zz),(-.55,-.07,z2),.012,'darkroof')
 beam(crank,(-.05,-.12,.25),.022,'metal');box(-.05,-.15,.24,.15,.07,.025,'darkroof')
 beam((-.1,.04,.4),(-.17,.16,.006),.018,'metal')
def lemon():
 hollowpot(0,0,.49,.23,.34,10)
 beam((0,0,.46),(.04,0,1.36),.055,'wood')
 for i in range(7):
  a=i*2.399;x=math.cos(a)*(.28+.04*(i%2));y=math.sin(a)*.3;z=.95+i*.085
  beam((.02,0,.75+i*.06),(x,y,z),.025,'wood');foliage(x,y,z,.57,i,(.02,0,.75+i*.06),11 if i<6 else 8)
  if i%2==0:
   cx=x;cy=y-.045;cz=z-.065;r=.06
   beam((x,y,z),(cx,cy,cz+.08),.012,'wood')
   upper=[(cx+r*math.cos(j*math.pi/3),cy+r*math.sin(j*math.pi/3),cz+.028) for j in range(6)]
   lower=[(cx+r*.9*math.cos(j*math.pi/3),cy+r*.9*math.sin(j*math.pi/3),cz-.028) for j in range(6)]
   for j in range(6):
    k=(j+1)%6;face([upper[j],upper[k],(cx,cy,cz+.08)],'roof_clay');face([lower[k],lower[j],(cx,cy,cz-.075)],'roof_clay');face([lower[j],lower[k],upper[k],upper[j]],'roof_clay')
def main():
 parser=argparse.ArgumentParser();parser.add_argument('--only','--ids',dest='only');parser.add_argument('--render-ids');args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
 REV.mkdir(parents=True,exist_ok=True);bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.scene.unit_settings.system='METRIC';mat=B.material()
 funcs=[('vending_machine',vending,'floor_back_center'),*[(f'small_signs_{chr(97+i)}',lambda v=i:signs(v),'floor_back_center') for i in range(4)],('planter_row',row,'floor_center'),('ac_unit',ac,'floor_back_center'),('meter_box',meter,'floor_back_center'),('mailbox',mailbox,'floor_back_center'),('bicycle',bicycle,'floor_center'),('lemon_tree_pot',lemon,'floor_center')]
 assets=[];objs=[];urls=sorted({t['source_url'] for t in B.TM['tiles']}|{'https://polyhaven.com/a/leafy_grass'});pending=[];mats={str(B.TEX):mat};textures=set()
 def record(p):pending.append(dict(asset_id='italy_props_'+p.stem+'_'+p.suffix[1:],path=B.rel(p),source_url='; '.join(urls),license='CC0-1.0; original bpy geometry; Pretendard/Galmuri OFL-1.1',accessed='2026-10-25',tool='bayloop_italy_props.py; bayloop_italy_props_texture.py; photo_to_pixel.py inherited palette pipeline',ai_generated='no',sha256=B.sha(p),notes='Photographed CC0 material, exact 32 px/m metric UV; no AI; wear at lower feet, hinges, grips.'))
 for name,fn,pivot in funcs:
  if args.only and name not in args.only.split(','):continue
  B.TEX=O/('PropsPhotoAtlas_r3.png' if name in LAST_FAILED else 'PropsPhotoAtlas_r2.png' if name in FAILED else 'PropsPhotoAtlas.png')
  if str(B.TEX) not in mats:mats[str(B.TEX)]=B.material()
  mat=mats[str(B.TEX)];textures.add(B.TEX)
  B.V.clear();B.F.clear();B.UV.clear();B.PARTS.clear();B.PART=name;fn()
  lo=[min(p[k] for p in B.V) for k in range(3)];hi=[max(p[k] for p in B.V) for k in range(3)];offx=(lo[0]+hi[0])/2;offy=hi[1] if pivot=='floor_back_center' else (lo[1]+hi[1])/2
  B.V[:]=[(x-offx,y-offy,z-lo[2]) for x,y,z in B.V]
  obj=B.mesh(name,mat);tris=sum(len(f)-2 for f in B.F);assert tris<=800,(name,tris)
  bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
  glb=O/(name+'.glb');fbx=O/(name+'.fbx')
  bpy.ops.export_scene.gltf(filepath=str(glb),export_format='GLB',use_selection=True,export_yup=True)
  bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,axis_forward='-Z',axis_up='Y',bake_space_transform=True,apply_scale_options='FBX_SCALE_ALL',add_leaf_bones=False,mesh_smooth_type='FACE',path_mode='COPY',embed_textures=True)
  verts=[(x,z,-y) for x,y,z in B.V];lo=[min(p[k] for p in verts) for k in range(3)];hi=[max(p[k] for p in verts) for k in range(3)]
  asset=dict(id=name,type=name,asset_kind='prop',max_tris=800,pivot_kind=pivot,model=B.rel(glb),fbx=B.rel(fbx),texture=B.rel(B.TEX),dimensions_m=[hi[k]-lo[k] for k in range(3)],bounds_min=lo,bounds_max=hi,source_urls=urls,tris=tris,wear_locations=['feet','lower edges','handles','metal joints'])
  (O/(name+'.geometry.json')).write_text(json.dumps({'vertices_y_up':verts,'faces':B.F,'part_per_face':B.PARTS,'uv_per_face':B.UV}));assets.append(asset);objs.append(obj);record(glb);record(fbx)
 old=json.loads((R/'art/models/bayloop_buildings_v01/manifest.json').read_text());man={'palette':old['palette'],'density_px_m':32,'max_tris':800,'source_manifests':old['source_manifests']+[B.rel(O/'leaf-source.json')],'assets':assets}
 if args.only and (O/'manifest.json').exists():
  prev=json.loads((O/'manifest.json').read_text());man['assets']=[a for a in prev['assets'] if a['id'] not in args.only.split(',')]+assets
 (O/'manifest.json').write_text(json.dumps(man,ensure_ascii=False,indent=2))
 for texture in textures:record(texture)
 raw=json.loads((O/'leaf-source.json').read_text());pending.append(dict(asset_id='italy_props_leafy_grass_photo',path=raw['path'],source_url=raw['url'],license='CC0-1.0',accessed='2026-10-25',tool='direct HTTP download from Poly Haven',ai_generated='no',sha256=raw['sha256'],notes='Unmodified photograph albedo, provider 2m scale, not AI'))
 if args.only and (O/'provenance-pending.json').exists():
  paths={r['path'] for r in pending};pending=[r for r in json.loads((O/'provenance-pending.json').read_text()) if r['path'] not in paths]+pending
 (O/'provenance-pending.json').write_text(json.dumps(pending,ensure_ascii=False,indent=2))
 sc=B.stage();sc.cycles.samples=8
 for i,obj in enumerate(objs):
  if args.render_ids and obj.name not in args.render_ids.split(','):continue
  for ob in objs:ob.hide_render=ob!=obj
  h=assets[i]['dimensions_m'][1];w=assets[i]['dimensions_m'][0];scale=max(h*2.25,w*1.5,1.5);B.render(sc,REV/(obj.name+'_front.png'),(3,-6,3),(0,-.1,h*.48),scale)
 # Front views first so the independent gate can start before rear renders.
 for i,obj in enumerate(objs):
  for ob in objs:ob.hide_render=ob!=obj
  h=assets[i]['dimensions_m'][1];w=assets[i]['dimensions_m'][0];scale=max(h*2.25,w*1.5,1.5)
  B.render(sc,REV/(obj.name+'_back.png'),(-3,6,3),(0,-.1,h*.48),scale)
 bpy.ops.wm.save_as_mainfile(filepath=str(O/('props_'+args.only+'.blend' if args.only else 'props.blend')))
 print('PROPS_DONE',[(a['id'],a['tris']) for a in assets])
if __name__=='__main__':main()
