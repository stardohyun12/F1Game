"""Depot geometry; shared kit only. Blender X right, -Y front, Z up."""
exec((__import__('pathlib').Path(__file__).parent/'build.py').read_text(),globals())
import bmesh
# Wall body: split long spans to stay inside 8m atlas cells. Genuine open portal.
for x in [-2.4,2.4]:
 box('rear_wall',(x,6.48,1.8),(4.8,.24,3.6),'wall_cream')
box('left_wall',(-4.68,3.3,1.8),(.24,6.6,3.6),'wall_apricot')
box('right_wall',(4.68,3.3,1.8),(.24,6.6,3.6),'wall_cream')
# Shutter x=-4.0..-.2. Reception bay to right.
box('front_pier_left',(-4.4,.12,1.8),(.8,.24,3.6),'wall_cream')
box('front_pier_mid',(.12,.12,1.8),(.64,.24,3.6),'wall_cream')
box('front_window_left',(.58,.12,1.8),(.28,.24,3.6),'wall_cream')
box('front_door_bay',(2.92,.12,1.8),(.80,.24,3.6),'wall_cream')  # 20261028-1623: 문칸을 세 조각으로 → 실제 구멍
box('front_door_bay',(4.52,.12,1.8),(.56,.24,3.6),'wall_cream')
box('front_door_bay',(3.78,.12,2.95),(.92,.24,1.30),'wall_cream')
box('front_window_lower',(1.62,.12,.70),(1.80,.24,1.40),'wall_cream')
box('front_window_upper',(1.62,.12,3.05),(1.80,.24,1.10),'wall_cream')
box('shutter_lintel',(-2.1,.12,3.25),(3.8,.24,.70),'wall_cream')
for x in [-2.5,2.5]:box('floor_slab',(x,3.5,.06),(5,7,.12),'floor_concrete')
# Low pitched tiled roof, split into <8m patches. Total bounds 10x7x4m.
for x0,x1 in [(-5,0),(0,5)]:
 face('tile_roof_front',[(x0,0,3.64),(x1,0,3.64),(x1,3.5,4),(x0,3.5,4)],'roof_E2')
 face('tile_roof_back',[(x1,7,3.64),(x0,7,3.64),(x0,3.5,4),(x1,3.5,4)],'roof_E2')
for y in [.04,6.96]:box('eave_long',(0,y,3.60),(10,.08,.16),'eaves')
for x in [-4.97,4.97]:box('eave_side',(x,3.5,3.60),(.06,7,.16),'eaves')
for x in [-4.57,4.57]:box('downpipe',(x,-.05,1.75),(.12,.12,3.50),'gutter')
for x in [-4.03,-.17]:box('shutter_track',(x,-.05,1.50),(.10,.14,2.95),'gutter')
box('half_open_shutter',(-2.1,-.04,2.80),(3.80,.06,.20),'shutter')  # 1623: 거의 다 올라감
box('shutter_bottom_rail',(-2.1,-.10,2.70),(3.88,.12,.08),'gutter')
box('shutter_casing',(-2.1,-.12,3.03),(4.0,.30,.22),'window_frame')
# People door and reception window. Insets retained behind framing.
DOOR=box('person_door',(3.78,.21,1.18),(.92,.05,2.12),'wood_door')  # 1623: 안쪽으로 열림(아래에서 돌림)
for x in [3.26,4.30]:box('door_jamb',(x,-.11,1.19),(.10,.14,2.22),'door_frame')
box('door_header',(3.78,-.11,2.30),(1.14,.14,.10),'door_frame')
DOOR+=box('person_door_handle',(4.10,.16,1.15),(.07,.05,.15),'gutter')
box('reception_glass',(2.20,-.035,1.95),(.64,.07,1.10),'glass')
for x in [.66,2.58]:box('window_jamb',(x,-.12,1.95),(.10,.16,1.22),'window_frame')
for z in [1.34,2.56]:box('window_sill',(1.62,-.13,z),(2.02,.18,.10),'window_frame')
box('window_mullion',(1.88,-.15,1.95),(.07,.08,1.12),'window_frame')
box('reception_counter',(1.62,.08,1.30),(2.10,.30,.09),'wood_door')
# Existing blank sign base; text is actual mesh sharing existing dark material.
box('sign_back',(2.35,-.15,3.10),(.96,.12,.92),'wood_door')  # 1623: 글자 대신 택배 아이콘 판
face('sign_icon',[(1.93,-.215,2.68),(2.77,-.215,2.68),(2.77,-.215,3.52),(1.93,-.215,3.52)],'parcel',[(0,0),(1,0),(1,1),(0,1)])
def lettering(body,loc,width):
 cu=bpy.data.curves.new('sign_lettering','FONT');cu.body=body;cu.align_x='CENTER';cu.size=.45;cu.extrude=.003;cu.resolution_u=1
 o=bpy.data.objects.new('sign_'+body,cu);bpy.context.collection.objects.link(o);o.location=loc;o.rotation_euler=(math.pi/2,0,0)
 bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.convert(target='MESH');o=bpy.context.object;bpy.context.view_layer.update();o.scale*=width/o.dimensions.x
 o.data.materials.append(mats['trim']);layer=o.data.uv_layers.new(name='UVMap')
 for loop in layer.data:loop.uv=(.50,1-190/512)
 o.select_set(False)
# 1623: 'BAY PARCEL' 글자 삭제(도현: 이름 써두지 말고 아이콘만)
def decal(name,x,y,z,w,h,cell):
 return face(name,[(x-w/2,y,z),(x+w/2,y,z),(x+w/2,y,z+h),(x-w/2,y,z+h)],cell,[(0,0),(1,0),(1,1),(0,1)])
decal('notice',2.90,-.136,.90,.35,.525,'flyer')
decal('number',4.48,-.136,1.91,.32,.20,'number_07')
decal('local_poster',.45,-.136,2.10,.37,.50,'poster_harbor')
decal('corner_wear',-4.44,-.136,.15,.65,.55,'edge_chip')
decal('lintel_wear',-3.62,-.136,3.30,.8,.30,'edge_chip')
def parcel(name,x,y,z,w=.55,d=.42,h=.38):
 box(name,(x,y,z+h/2),(w,d,h),'wall_apricot_B')
 box(name+'_tape_top',(x,y,z+h+.004),(.06,d+.006,.008),'wall_cream')
 box(name+'_tape_face',(x,y-d/2-.004,z+h/2),(.06,.008,h),'wall_cream')
def table(name,x,y,w,d,h):
 box(name+'_top',(x,y,h),(w,d,.09),'wood_door')
 for dx in [-w/2+.10,w/2-.10]:
  for dy in [-d/2+.10,d/2-.10]:box(name+'_leg',(x+dx,y+dy,h/2),(.09,.09,h),'gutter')
 box(name+'_shelf',(x,y,.25),(w-.1,d-.1,.06),'wood_door')
# 1623: 가운데 선반 대신 뒷벽 선반 3줄(아래 INTERIOR)
table('packing_table',1.62,.78,1.7,.8,.88)  # 1623: 계란 포장대는 안쪽 창가
parcel('egg_delivery_box',1.25,.78,.93,.55,.42,.24)
parcel('packing_box',1.98,.78,.93,.45,.36,.32)
# Outside goods are tight to left wall, beyond central parking aisle.
for i in range(3):parcel('outside_parcel',-4.3+i*.56,-.52,.12,.50,.48,.40)
parcel('outside_top',-4.0,-.52,.53,.62,.48,.33)
def wheel(name,x,y,z):
 bpy.ops.mesh.primitive_cylinder_add(vertices=10,radius=.14,depth=.10,location=(x,y,z),rotation=(0,math.pi/2,0));o=bpy.context.object;o.name=name;o.data.materials.append(mats['floor'])
 for loop in o.data.uv_layers.active.data:loop.uv=(.75,.25)
for xx in [-4.55,-4.0]:wheel('handcart_wheel',xx+3.4,1.27,.18)
box('handcart_toe',(-.88,1.35,.25),(.62,.45,.05),'gutter')
for xx in [-4.51,-4.05]:box('handcart_upright',(xx+3.4,1.53,.76),(.06,.06,1.15),'gutter')
box('handcart_handle',(-.88,1.53,1.34),(.52,.06,.06),'wood_door')
parcel('handcart_load',-.88,1.28,.28,.45,.35,.58)
# ---------- 1623 INTERIOR: 천장·실내등·접수 카운터·벽 선반 3줄·손수레·롤 테이프·시계·작업복 걸이 ----------
import random as _r;RS=_r.Random(23)
for x in [-2.28,2.28]:box('ceiling',(x,3.30,3.52),(4.56,6.12,.04),'wall_cream')
box('lamp',(0,3.3,3.46),(.9,.18,.08),'glass')
PX=64  # 작은 물건 = 64px/m(같은 사진 칸을 촘촘하게)
box('reception_desk',(2.75,2.70,.56),(.60,2.40,.88),'wood_door');box('reception_desk_top',(2.72,2.70,1.03),(.72,2.52,.06),'wood_door')
box('scale_base',(2.75,1.95,1.10),(.30,.30,.08),'shutter');box('scale_plate',(2.75,1.95,1.16),(.34,.34,.02),'shutter');box('scale_display',(2.93,1.95,1.12),(.02,.14,.06),'glass')
box('slip_holder',(2.80,2.70,1.16),(.10,.32,.20),'wood_door')
for k in range(2):face('slip_paper',[(2.856,2.84-k*.16,1.08),(2.856,2.70-k*.16,1.08),(2.856,2.70-k*.16,1.29),(2.856,2.84-k*.16,1.29)],'slip',[(0,0),(1,0),(1,1),(0,1)])
box('stamp_pad',(2.75,3.40,1.075),(.12,.16,.03),'gutter');box('stamp_body',(2.70,3.20,1.12),(.06,.06,.12),'eaves');box('stamp_grip',(2.70,3.20,1.20),(.08,.08,.05),'roof_F2')
for xs in [(-4.30,-.40),(.50,4.30)]:
 xm=(xs[0]+xs[1])/2;ww=xs[1]-xs[0]
 for xx in [xs[0]+.03,xm,xs[1]-.03]:box('wall_shelf_upright',(xx,6.08,1.30),(.05,.50,2.60),'shutter')
 for zz in [.40,1.15,1.90]:
  box('wall_shelf',(xm,6.08,zz),(ww,.52,.05),'wood_door')
  x=xs[0]+.10
  while x<xs[1]-.35:
   w=RS.choice([.30,.38,.46,.58]);h=RS.choice([.20,.28,.36,.44]);d=RS.choice([.32,.40,.46])
   if x+w>xs[1]-.08:break
   parcel('stored_parcel',x+w/2,6.10,zz+.03,w,d,h)
   if RS.random()<.35:face('stored_parcel_fragile',[(x+w/2-.14,6.10-d/2-.008,zz+.03+h*.35),(x+w/2+.14,6.10-d/2-.008,zz+.03+h*.35),(x+w/2+.14,6.10-d/2-.008,zz+.03+h*.35+.14),(x+w/2-.14,6.10-d/2-.008,zz+.03+h*.35+.14)],'fragile',[(0,0),(1,0),(1,1),(0,1)])
   x+=w+RS.uniform(.04,.12)
def roll(name,x,y,z,r=.07,dep=.05):
 bpy.ops.mesh.primitive_cylinder_add(vertices=10,radius=r,depth=dep,location=(x,y,z));o=bpy.context.object;o.name=name;o.data.materials.append(mats['trim'])
 for loop in o.data.uv_layers.active.data:loop.uv=(.50,.25)
roll('tape_roll',-.30,.95,.145);roll('tape_roll',-.12,1.05,.145);roll('tape_roll_desk',2.65,2.45,1.085)
bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=.22,depth=.04,location=(-2.35,6.335,2.95),rotation=(math.pi/2,0,0));o=bpy.context.object;o.name='clock_disc';o.data.materials.append(mats['trim'])
for loop in o.data.uv_layers.active.data:loop.uv=(.30,1-8/512)
face('clock_face',[(-2.57,6.31,2.73),(-2.13,6.31,2.73),(-2.13,6.31,3.17),(-2.57,6.31,3.17)],'clock',[(0,0),(1,0),(1,1),(0,1)])
box('hanger_rail',(4.42,1.75,1.88),(.05,1.00,.04),'gutter')
for yy in [1.28,2.22]:box('hanger_bracket',(4.49,yy,1.88),(.14,.04,.04),'gutter')
for yy,c in [(1.52,'wall_mint'),(1.98,'wall_mint')]:
 box('jacket_body',(4.42,yy,1.42),(.10,.44,.78),c);box('jacket_collar',(4.42,yy,1.84),(.08,.20,.06),'skirting')
 for dy in [-.25,.25]:box('jacket_sleeve',(4.42,yy+dy,1.50),(.08,.08,.62),c)
PX=32
# 돌출 간판(셔터 쪽 모서리, 길 쪽으로) — 같은 아이콘 양면
box('sign_blade_arm',(-4.35,-.55,3.36),(.05,.90,.05),'gutter')
box('sign_blade_board',(-4.35,-.58,2.92),(.05,.84,.84),'wood_door')
face('sign_blade_icon_a',[(-4.38,-.20,2.54),(-4.38,-.96,2.54),(-4.38,-.96,3.30),(-4.38,-.20,3.30)],'parcel',[(0,0),(1,0),(1,1),(0,1)])
face('sign_blade_icon_b',[(-4.32,-.96,2.54),(-4.32,-.20,2.54),(-4.32,-.20,3.30),(-4.32,-.96,3.30)],'parcel',[(0,0),(1,0),(1,1),(0,1)])
from mathutils import Matrix
_h=Vector((3.32,.21,0));_M=Matrix.Translation(_h)@Matrix.Rotation(math.radians(80),4,'Z')@Matrix.Translation(-_h)
for o in DOOR:o.matrix_world=_M@o.matrix_world

# Correct all flat primitive winding; no smoothing, no texture baking.
asset=[o for o in bpy.context.scene.objects if o.type=='MESH']
for o in asset:
 bm=bmesh.new();bm.from_mesh(o.data);
 if len(bm.faces)>1:bmesh.ops.recalc_face_normals(bm,faces=bm.faces)
 bm.to_mesh(o.data);bm.free()
root=bpy.data.objects.new('Q3_Depot',None);bpy.context.collection.objects.link(root)
for o in asset:o.parent=root
for nm,loc in [('STUDIO_CamIn',(3.78,2.0,1.5)),('STUDIO_CamInLook',(-1.8,6.0,1.1)),('STUDIO_Lamp',(0,3.3,3.25))]:
 e=bpy.data.objects.new(nm,None);bpy.context.collection.objects.link(e);e.location=loc;e.parent=root
tris=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in asset)
assert tris<10000,tris
bpy.ops.wm.save_as_mainfile(filepath=str(W/'depot.blend'))
bpy.ops.object.select_all(action='DESELECT')
for o in asset+[root]:o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.fbx(filepath=str(E/'q3_depot.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',path_mode='COPY',bake_anim=False)
validation={'tris':tris,'mesh_count':len(asset),'body_m':[10,7,4],'front':'Unity +Z / Blender -Y','pivot':'bottom front centre','local_clear_parking_unity':{'x':[-2.6,2.6],'z':[0,4]},'textures':{n:hashlib.sha256((E/(n+'.png')).read_bytes()).hexdigest() for n in mats},'light_bake':False,'uv_edges':uvstats}
(E/'validation.json').write_text(json.dumps(validation,indent=2))
import os
import sys
if os.environ.get('NO_RENDER') or '--no-render' in sys.argv:print('DEPOT BUILT (no render)',flush=True);bpy.ops.wm.quit_blender()
# Preview only ground and lighting. Not exported.
for x in range(-16,16,8):
 for y in range(-16,16,8):face('preview_ground',[(x,y,-.02),(x+8,y,-.02),(x+8,y+8,-.02),(x,y+8,-.02)],'floor_stone')
sc=bpy.context.scene
bpy.ops.object.light_add(type='SUN');sun=bpy.context.object;sun.data.energy=1.6;sun.data.color=(1,.94,.85);sun.data.angle=.15;sun.rotation_euler=Vector((.3,.5,-.8)).to_track_quat('-Z','Y').to_euler()
sc.world=bpy.data.worlds.new('sky');sc.world.use_nodes=True;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.48,.59,.64,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.85
sc.render.engine='CYCLES';sc.cycles.samples=64;sc.cycles.use_denoising=True;sc.cycles.device='GPU';prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices();[(setattr(d,'use',d.type=='OPTIX')) for d in prefs.devices];sc.render.resolution_x=1600;sc.render.resolution_y=900;sc.render.resolution_percentage=100;sc.render.dither_intensity=0
sc.view_settings.view_transform='Standard';sc.view_settings.look='None';sc.view_settings.exposure=0
bpy.ops.object.camera_add();cam=bpy.context.object;sc.camera=cam;cam.data.type='PERSP';cam.data.sensor_fit='VERTICAL';cam.data.sensor_height=32;cam.data.lens=32/(2*math.tan(math.radians(64)/2))
views={'15m':((0,-15,2.8),(0,1,1.8)),'5m':((5,-5,2.8),(0,1,1.6)),'inside':((-2.1,-.9,1.15),(-2.1,3.3,1.1))}
for name,(loc,target) in views.items():
 cam.location=loc;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(V/(name+'.png'));bpy.ops.render.render(write_still=True)
print('DEPOT BUILT',tris,flush=True)
