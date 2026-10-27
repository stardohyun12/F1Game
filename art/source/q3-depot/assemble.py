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
box('front_door_bay',(3.66,.12,1.8),(2.28,.24,3.6),'wall_cream')
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
box('half_open_shutter',(-2.1,-.04,2.20),(3.80,.06,1.40),'shutter')
box('shutter_bottom_rail',(-2.1,-.10,1.50),(3.88,.12,.08),'gutter')
box('shutter_casing',(-2.1,-.12,3.03),(4.0,.30,.22),'window_frame')
# People door and reception window. Insets retained behind framing.
box('person_door',(3.78,-.035,1.18),(.92,.07,2.12),'wood_door')
for x in [3.26,4.30]:box('door_jamb',(x,-.11,1.19),(.10,.14,2.22),'door_frame')
box('door_header',(3.78,-.11,2.30),(1.14,.14,.10),'door_frame')
box('door_handle',(4.07,-.20,1.15),(.07,.06,.15),'gutter')
box('reception_glass',(2.20,-.035,1.95),(.64,.07,1.10),'glass')
for x in [.66,2.58]:box('window_jamb',(x,-.12,1.95),(.10,.16,1.22),'window_frame')
for z in [1.34,2.56]:box('window_sill',(1.62,-.13,z),(2.02,.18,.10),'window_frame')
box('window_mullion',(1.88,-.15,1.95),(.07,.08,1.12),'window_frame')
box('reception_counter',(1.62,.08,1.30),(2.10,.30,.09),'wood_door')
# Existing blank sign base; text is actual mesh sharing existing dark material.
box('sign_back',(2.35,-.15,3.08),(4.0,.17,.64),'wood_door')
face('sign_blank',[(.40,-.245,2.82),(4.30,-.245,2.82),(4.30,-.245,3.34),(.40,-.245,3.34)],'sign_cream',[(0,0),(1,0),(1,1),(0,1)])
def lettering(body,loc,width):
 cu=bpy.data.curves.new('sign_lettering','FONT');cu.body=body;cu.align_x='CENTER';cu.size=.45;cu.extrude=.003;cu.resolution_u=1
 o=bpy.data.objects.new('sign_'+body,cu);bpy.context.collection.objects.link(o);o.location=loc;o.rotation_euler=(math.pi/2,0,0)
 bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.convert(target='MESH');o=bpy.context.object;bpy.context.view_layer.update();o.scale*=width/o.dimensions.x
 o.data.materials.append(mats['trim']);layer=o.data.uv_layers.new(name='UVMap')
 for loop in layer.data:loop.uv=(.50,1-190/512)
 o.select_set(False)
lettering('BAY PARCEL',(2.35,-.257,2.91),3.40)
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
# Shelves located close enough to be visible below the half-open shutter.
for yy in [2.1,4.7]:
 for xx in [-3.55,-.75]:box('rack_upright',(xx,yy,1.35),(.08,.10,2.45),'gutter')
 for zz in [.35,1.12,1.95]:
  box('rack_shelf',(-2.15,yy,zz),(2.9,.65,.07),'wood_door')
  for i in range(4):
   if (i==1 and zz>1.5) or (i==3 and zz<.5):continue
   parcel('stored_parcel',-3.20+i*.67,yy+(.08 if i%2 else -.08),zz+.04,.44 if i%2 else .58,.36 if i%2 else .50,.30 if i%2 else .49)
table('packing_table',3.75,-1.3,1.8,.85,.88)
parcel('egg_delivery_box',3.35,-1.3,.93,.55,.42,.24)
parcel('packing_box',4.10,-1.3,.93,.45,.36,.32)
table('inside_workbench',2.10,3.4,2.2,.9,.90)
parcel('sort_box',1.4,3.4,.95,.62,.50,.40)
parcel('sort_box_small',2.3,3.4,.95,.42,.35,.28)
# Outside goods are tight to left wall, beyond central parking aisle.
for i in range(3):parcel('outside_parcel',-4.3+i*.56,-.52,.12,.50,.48,.40)
parcel('outside_top',-4.0,-.52,.53,.62,.48,.33)
def wheel(name,x,y,z):
 bpy.ops.mesh.primitive_cylinder_add(vertices=10,radius=.14,depth=.10,location=(x,y,z),rotation=(0,math.pi/2,0));o=bpy.context.object;o.name=name;o.data.materials.append(mats['floor'])
 for loop in o.data.uv_layers.active.data:loop.uv=(.75,.25)
for xx in [-4.55,-4.0]:wheel('handcart_wheel',xx,-1.03,.18)
box('handcart_toe',(-4.28,-.95,.25),(.62,.45,.05),'gutter')
for xx in [-4.51,-4.05]:box('handcart_upright',(xx,-.77,.76),(.06,.06,1.15),'gutter')
box('handcart_handle',(-4.28,-.77,1.34),(.52,.06,.06),'wood_door')
parcel('handcart_load',-4.28,-1.02,.28,.45,.35,.58)
# Correct all flat primitive winding; no smoothing, no texture baking.
asset=[o for o in bpy.context.scene.objects if o.type=='MESH']
for o in asset:
 bm=bmesh.new();bm.from_mesh(o.data);
 if len(bm.faces)>1:bmesh.ops.recalc_face_normals(bm,faces=bm.faces)
 bm.to_mesh(o.data);bm.free()
root=bpy.data.objects.new('Q3_Depot',None);bpy.context.collection.objects.link(root)
for o in asset:o.parent=root
tris=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in asset)
assert tris<10000,tris
bpy.ops.wm.save_as_mainfile(filepath=str(W/'depot.blend'))
bpy.ops.object.select_all(action='DESELECT')
for o in asset+[root]:o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.fbx(filepath=str(E/'q3_depot.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',path_mode='COPY',bake_anim=False)
validation={'tris':tris,'mesh_count':len(asset),'body_m':[10,7,4],'front':'Unity +Z / Blender -Y','pivot':'bottom front centre','local_clear_parking_unity':{'x':[-2.6,2.6],'z':[0,4]},'textures':{n:hashlib.sha256((E/(n+'.png')).read_bytes()).hexdigest() for n in mats},'light_bake':False,'uv_edges':uvstats}
(E/'validation.json').write_text(json.dumps(validation,indent=2))
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
