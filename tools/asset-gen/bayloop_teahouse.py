"""D137 tea destination, real metre geometry and 32px/m CC0 photo atlas."""
from pathlib import Path
import sys,json,math,hashlib,argparse
import bpy
sys.path.insert(0,str(Path(__file__).parent))
import bayloop_buildings as b
import bayloop_buildgen as g
import bayloop_italy_buildings as it
import bayloop_italy_props as p
R=b.ROOT;O=R/'art/models/bayloop_teahouse_v01';REV=R/'art/_review/2026-10-26-landmarks/teahouse'
b.TEX=O/'TeaHousePhotoAtlas.png';box=b.box;beam=b.beam;face=b.face

def move(fn,x=0,y=0,z=0,angle=0):
 n=len(b.V);fn();c=math.cos(angle);s=math.sin(angle)
 for k in range(n,len(b.V)):
  a,d,h=b.V[k];b.V[k]=(x+a*c-d*s,y+a*s+d*c,z+h)

def cup(x,y,z,r=.055,h=.075,tile='trim'):
 n=8
 for k in range(n):
  a=k*math.tau/n;c=(k+1)*math.tau/n
  at=lambda rr,t,zz:(x+rr*math.cos(t),y+rr*math.sin(t),zz)
  face([at(r*.8,a,z),at(r*.8,c,z),at(r,c,z+h),at(r,a,z+h)],tile,(18,8))
  face([at(r,a,z+h),at(r,c,z+h),at(r-.012,c,z+h),at(r-.012,a,z+h)],tile,(18,8))
  face([at(r-.012,a,z+h),at(r-.012,c,z+h),at(r*.65,c,z+.02),at(r*.65,a,z+.02)],'wood_dark',(0,0))
 p.cyl(x,y,z+.021,r*.65,.001,'wood_dark',8)

def kettle(x,y,z):
 p.cyl(x,y,z,.065,.1,'teal',8,.085);p.cyl(x,y,z+.1,.085,.055,'teal',8,.045)
 p.cyl(x,y,z+.155,.05,.01,'metal',8);p.cyl(x,y,z+.165,.015,.015,'wood_dark',6)
 beam((x+.055,y,z+.055),(x+.135,y,z+.14),.035,'teal')
 # Actual open handle, not an opaque block.
 for a,c in [((x-.065,y,z+.035),(x-.12,y,z+.035)),((x-.12,y,z+.035),(x-.12,y,z+.13)),((x-.12,y,z+.13),(x-.065,y,z+.13))]:beam(a,c,.018,'wood_dark')

def chair():
 for x in [-.19,.19]:
  for y in [-.17,.17]:box(x,y,0,.045,.045,.45,'wood_dark')
 for y in [-.17,0,.17]:box(0,y,.45,.46,.145,.035,'wood')
 for x in [-.19,.19]:box(x,.19,.47,.045,.045,.39,'wood_dark')
 for z in [.64,.79]:box(0,.19,z,.43,.04,.09,'wood')
 for x in [-.19,.19]:beam((x,-.17,.16),(x,.17,.16),.03,'wood_dark')

def tea_table():
 b.PART='tea_table_wood'
 for y in [-.25,-.083,.083,.25]:box(0,y,.72,.8,.155,.045,'wood')
 for x in [-.3,.3]:
  for y in [-.23,.23]:box(x,y,0,.045,.045,.72,'wood_dark')
 for y in [-.24,.24]:beam((-.32,y,.64),(.32,y,.64),.055,'wood_dark')
 move(chair,0,.78);move(chair,0,-.78,angle=math.pi)
 b.PART='used_tea_service';box(0,0,.765,.45,.31,.018,'wood_dark');kettle(-.07,0,.783)
 cup(.12,-.08,.783);cup(.12,.09,.783)

def tray_display():
 b.PART='tray_stand'
 for x in [-.34,.34]:
  for y in [-.2,.2]:box(x,y,0,.045,.045,.85,'wood_dark')
 box(0,0,.79,.76,.49,.045,'wood');box(0,0,.23,.72,.43,.035,'wood')
 for x in [-.34,.34]:beam((x,-.2,.2),(x,.2,.75),.03,'wood_dark')
 box(0,0,.835,.66,.4,.02,'wood_dark')
 for y in [-.19,.19]:box(0,y,.855,.66,.025,.03,'wood')
 for x in [-.32,.32]:box(x,0,.855,.025,.4,.03,'wood')
 for x,y in [(-.2,-.07),(-.04,-.07),(.12,-.07),(-.2,.09),(-.04,.09),(.12,.09)]:cup(x,y,.855)
 p.cyl(.24,.07,.855,.055,.14,'teal',8);p.cyl(.24,.07,.995,.058,.015,'metal',8)
 # Wiping cloth folded on the lower shelf; empty shelf space stays readable.
 box(-.1,0,.265,.32,.28,.04,'cloth')

def old_bicycle():
 b.PART='owner_bicycle'
 # 24-sided narrow commuting tyres, with true bottom vertex on z=0.
 cz=.338;r=.32;t=.018;n=24
 for x in [-.55,.55]:
  for k in range(n):
   a=k*math.tau/n;c=(k+1)*math.tau/n
   for j in range(3):
    pts=[]
    for ang,sect in [(a,j),(c,j),(c,(j+1)%3),(a,(j+1)%3)]:
     q=sect*math.tau/3;rr=r+t*math.cos(q);pts.append((x+rr*math.cos(ang),t*math.sin(q),cz+rr*math.sin(ang)))
    face(pts,'darkroof')
  # 16 spokes are narrow planar strips, not chunky structural tubes.
  for k in range(16):
   a=k*math.tau/16;dx=math.cos(a);dz=math.sin(a);w=.002
   face([(x-w*dz,0,cz+w*dx),(x+.3*dx-w*dz,0,cz+.3*dz+w*dx),(x+.3*dx+w*dz,0,cz+.3*dz-w*dx),(x+w*dz,0,cz-w*dx)],'metal')
 rear=(-.55,0,cz);crank=(-.12,0,.32);seat=(-.22,0,.75);head=(.35,0,.77);front=(.55,0,cz)
 for a,c in [(rear,crank),(rear,seat),(crank,seat),(seat,head),(head,crank),(head,front)]:beam(a,c,.032,'teal')
 beam(seat,(-.24,0,.87),.024,'metal');box(-.25,0,.86,.24,.15,.05,'darkroof')
 beam(head,(.28,0,1.03),.035,'metal');beam((.28,0,1.03),(.18,0,1.03),.028,'metal');beam((.18,-.25,1.03),(.18,.25,1.03),.028,'metal')
 # Solid shallow pressed-metal basket retains small goods; top stays open.
 box(.55,0,.82,.32,.34,.02,'metal')
 for yy in [-.17,.17]:
  face([(.39,yy,.84),(.71,yy,.84),(.71,yy,1.02),(.39,yy,1.02)],'metal')
  beam((.39,yy,1.02),(.71,yy,1.02),.014,'metal')
 for xx in [.39,.71]:
  face([(xx,-.17,.84),(xx,.17,.84),(xx,.17,1.02),(xx,-.17,1.02)],'metal')
  beam((xx,-.17,1.02),(xx,.17,1.02),.014,'metal')
 # Polygon sprocket, chain, pedal and deployed double-leg stand.
 ring=[(-.12+.075*math.cos(k*math.tau/10),-.055,.32+.075*math.sin(k*math.tau/10)) for k in range(10)]
 face(ring,'metal')
 for zz,z2 in [(.395,.37),(.245,.305)]:beam((-.12,-.06,zz),(-.55,-.06,z2),.01,'darkroof')
 beam(crank,(-.05,-.12,.25),.018,'metal');box(-.05,-.15,.24,.15,.07,.025,'darkroof')
 for yy in [-.18,.18]:beam((-.1,0,.3),(-.22,yy,.009),.018,'metal')
 # Replaceable rear rack with actual stays attached to the rear axle.
 box(-.55,0,.71,.34,.25,.025,'wood_dark')
 for y in [-.1,.1]:beam((-.55,y,.335),(-.65,y,.71),.02,'metal')

def house():
 w=6.4;d=6.;b.PART='tea_ground_shell'
 ops=[(-2.5,-.95,.12,2.65),(.1,1.15,0,2.35)]
 g.shell(w,d,3.05,ops)
 b.arched_window(-1.725,.1,.12,1.55,2.53)
 g.window(.625,0,.03,1.05,2.32,door=True,shutters=False)
 # Low counter/curtain makes the display opening inhabited.
 box(-1.725,.08,.2,1.38,.055,.55,'cloth');box(-1.725,-.08,.83,1.7,.33,.09,'wood')
 b.PART='upper_retreat';uy=2.15;uw=4.6;ux=-.65
 move(lambda:g.shell(uw,d-uy,2.6,[(-1.45,-.55,3.2,5.25),(.6,1.5,3.8,5.15)],y=uy,z=3.05),ux)
 g.window(ux-1,uy,3.2,.9,2.05,door=True);g.window(ux+1.05,uy,3.8,.9,1.35)
 # Roof parapet under the upper room, open front terrace rather than full-height wall.
 box(0,1.05,3.05,6.55,2.1,.12,'stone')
 move(lambda:b.railing(6.2,0,3.17),0,.03)
 for x in [-3.1,3.1]:beam((x,.03,4.12),(x,2.13,4.12),.045,'metal')
 # Modest terracotta cap over the rear room gives a legible asymmetric silhouette.
 move(lambda:b.roof(uw,d-uy,5.65,.55,'roof_clay',y=uy),ux)
 b.PART='shade';it.awning(.625,-.02,2.95,1.5)
 b.PART='tea_lettered_sign';box(1.99,-.1,2.15,1.,.13,.57,'wood_dark')
 face([(1.99-.4375,-.172,2.19),(1.99+.4375,-.172,2.19),(1.99+.4375,-.172,2.69),(1.99-.4375,-.172,2.69)],'sign')
 b.PART='terrace';box(0,-1.08,0,6.6,2.25,.1,'stone');box(.63,-2.3,0,1.3,.3,.05,'stone')
 # Owner receiving patch kept open at x .6, y -1.0, z .1.
 b.PART='pots';move(lambda:p.planter(0,0,.9),-2.9,-.5,.1);move(lambda:p.planter(0,0,.75),2.8,-.55,.1)
 b.PART='wood_chair';move(chair,2.45,-1.55,.1,angle=-.25)
 b.PART='service';box(2.9,-.095,1.02,.22,.16,.36,'metal');beam((2.9,-.06,1.36),(2.9,-.06,2.6),.022,'metal')
 beam((-3.12,-.1,.1),(-3.12,-.1,3.0),.06,'metal')
 # Attached side/rear service windows, shuttered at adult eye level.
 for yy in [2.,4.65]:move(lambda:g.window(0,-.15,1.1,.85,1.25,shutters=True),3.2,yy,angle=math.pi/2)
 move(lambda:g.window(0,-.15,1.1,.95,1.25,shutters=False),-1,6,angle=math.pi)
 move(lambda:g.window(0,-.15,3.85,.85,1.2,shutters=True),-.65,6,angle=math.pi)
 return dict(footprint_m=[w,d],storeys=2,front_wall_z=0,wear_locations=['threshold stone','wood table hand edges','photo plaster lower wall','downpipe base','sign board'],recipient_anchor_blender=[.6,-1.,.1],recipient_clearance_m=[1.2,1.2])

def build(name,fn,isprop):
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.scene.unit_settings.system='METRIC'
 b.V.clear();b.F.clear();b.UV.clear();b.PARTS.clear();info=fn() or {}
 if isprop:
  lo=[min(v[k] for v in b.V) for k in range(3)];hi=[max(v[k] for v in b.V) for k in range(3)]
  c=[(lo[0]+hi[0])/2,(lo[1]+hi[1])/2,lo[2]];b.V[:]=[(x-c[0],y-c[1],z-c[2]) for x,y,z in b.V]
 tris=sum(len(f)-2 for f in b.F);assert tris<=(1500 if isprop else 4000),(name,tris)
 obj=b.mesh(name,b.material());bpy.context.view_layer.objects.active=obj;obj.select_set(True)
 bpy.ops.export_scene.gltf(filepath=str(O/(name+'.glb')),export_format='GLB',use_selection=True,export_yup=True)
 bpy.ops.export_scene.fbx(filepath=str(O/(name+'.fbx')),use_selection=True,axis_forward='-Z',axis_up='Y',bake_space_transform=True,apply_scale_options='FBX_SCALE_ALL',add_leaf_bones=False,mesh_smooth_type='FACE',path_mode='COPY',embed_textures=True)
 bpy.ops.wm.save_as_mainfile(filepath=str(O/(name+'.blend')))
 vs=[(x,z,-y) for x,y,z in b.V];lo=[min(v[k] for v in vs) for k in range(3)];hi=[max(v[k] for v in vs) for k in range(3)]
 (O/(name+'.geometry.json')).write_text(json.dumps(dict(vertices_y_up=vs,faces=b.F,part_per_face=b.PARTS,uv_per_face=b.UV)),encoding='utf8')
 data=dict(id=name,type='props' if isprop else 'teahouse',asset_kind='prop' if isprop else 'building',pivot_kind='floor_center' if isprop else 'front_wall_ground_center',model=b.rel(O/(name+'.glb')),fbx=b.rel(O/(name+'.fbx')),texture=b.rel(b.TEX),tris=tris,max_tris=800 if isprop else 4000,dimensions_m=[hi[k]-lo[k] for k in range(3)],bounds_min=lo,bounds_max=hi,source_urls=URLS,**info)
 sc=b.stage();sc.cycles.samples=16
 if name=='tea_owner_bicycle_01':
  bpy.data.objects['REVIEW_ONLY_ground'].location.z=-.001
  for light in [ob for ob in bpy.context.scene.objects if ob.type=='LIGHT']:light.data.size=5
 if isprop:
  scale=max(2.6,(hi[0]-lo[0])*1.8,(hi[2]-lo[2])*1.6);target=(0,0,(hi[1]-lo[1])*.45)
  views={'front':(3,-5,3),'back':(-3,5,2.5),'side':(5,0,1.4)}
 else:scale=18;target=(0,1.4,2.8);views={'front':(10,-18,10),'back':(-12,18,10),'side':(20,2,4)}
 for view,loc in views.items():b.render(sc,REV/(name+'_'+view+'.png'),loc,target,scale)
 print('TEA_ASSET',name,tris,flush=True);return data

def arrival():
 bpy.ops.wm.read_factory_settings(use_empty=True)
 for name,loc,ang in [('teahouse_01',(0,0,0),0),('tea_table_01',(-1.7,-1.15,.1),.15),('tea_tray_display_01',(1.65,-.4,.1),0),('tea_owner_bicycle_01',(4.45,.5,0),math.pi/2)]:
  with bpy.data.libraries.load(str(O/(name+'.blend')),link=False) as (a,c):c.objects=[name]
  for ob in c.objects:
   bpy.context.collection.objects.link(ob);ob.location=loc;ob.rotation_euler.z=ang
 sc=b.stage();sc.cycles.samples=24
 # Keep the same shared light and modest adult-height arrival perspective.
 sc.camera.data.type='PERSP';sc.camera.data.lens=42;sc.camera.location=(8,-15,3.6);b.aim(sc.camera,(0,.4,2.5));sc.render.filepath=str(REV/'arrival.png');bpy.ops.render.render(write_still=True)
 sc.camera.location=(5,-7,3.1);sc.camera.data.lens=35;b.aim(sc.camera,(0,-.55,1.15));sc.render.filepath=str(REV/'arrival_close.png');bpy.ops.render.render(write_still=True)
 bpy.ops.wm.save_as_mainfile(filepath=str(O/'review_scene.blend'))

old=json.loads((R/'art/models/bayloop_italy_v01/manifest.json').read_text(encoding='utf8'));URLS=old['assets'][0]['source_urls']
if __name__=='__main__':
 O.mkdir(parents=True,exist_ok=True);REV.mkdir(parents=True,exist_ok=True)
 parser=argparse.ArgumentParser();parser.add_argument('--ids',nargs='*');args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
 assets=[build(n,f,prop) for n,f,prop in [('teahouse_01',house,False),('tea_table_01',tea_table,True),('tea_tray_display_01',tray_display,True),('tea_owner_bicycle_01',old_bicycle,True)] if not args.ids or n in args.ids]
 if args.ids:
  prior=json.loads((O/'manifest.json').read_text(encoding='utf8'))['assets'];assets=[a for a in prior if a['id'] not in args.ids]+assets
 manifest={k:old[k] for k in ['palette','density_px_m','max_tris','source_manifests']};manifest['assets']=assets;(O/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf8')
 arrival()
 rows=[]
 for path in list(O.glob('*.png'))+list(O.glob('*.fbx'))+list(O.glob('*.glb'))+list(O.glob('*.blend')):
  rows.append(dict(asset_id='bayloop_tea_'+path.stem+'_'+path.suffix[1:],path=b.rel(path),source_url='; '.join(URLS),license='CC0-1.0; original bpy geometry; Galmuri OFL-1.1',accessed='2026-10-26',tool='bayloop_teahouse.py; bayloop_teahouse_textures.py; bayloop_buildings.py; photo_to_pixel.py; Blender bpy',ai_generated='no',sha256=b.sha(path),notes='Reused audited CC0 photographic atlas; Galmuri Korean information only; 32px/m; review_scene is review-only and not exported to game'))
 (O/'provenance-rows.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf8')
