from pathlib import Path
import bpy,sys,math,json
from mathutils import Vector
R=Path(__file__).resolve().parents[2];O=R/'art/models/garage_kart_v01';V=R/'art/_review/2026-10-27-garage-kart';sys.path.insert(0,str(Path(__file__).parent));import bayloop_buildings as b
ASSETS=[];objects=[]
def material():
 m=bpy.data.materials.new('GarageKart_HandPaint');m.use_nodes=True;n=m.node_tree.nodes;tex=n.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(O/'GarageKartPaint.png'),check_existing=True);tex.interpolation='Linear';m.node_tree.links.new(tex.outputs['Color'],n.get('Principled BSDF').inputs['Base Color']);n.get('Principled BSDF').inputs['Roughness'].default_value=.9;n.get('Principled BSDF').inputs['Emission Color'].default_value=(.035,.075,.085,1);n.get('Principled BSDF').inputs['Emission Strength'].default_value=.65;return m
def finish(ob,name,tile):
 ob.name=name;bpy.context.view_layer.objects.active=ob;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);ob.data.materials.append(MAT)
 uv=ob.data.uv_layers.active or ob.data.uv_layers.new()
 for f in ob.data.polygons:
  ps=[ob.data.vertices[i].co for i in f.vertices];origin=ps[0];u=(ps[1]-origin).normalized();v=f.normal.cross(u).normalized();qs=[((p-origin).dot(u),(p-origin).dot(v)) for p in ps];lo=[min(q[k] for q in qs) for k in (0,1)];hi=[max(q[k] for q in qs) for k in (0,1)]
  for li,q in zip(f.loop_indices,qs):uv.data[li].uv=((tile%4*128+3+(q[0]-lo[0])/max(hi[0]-lo[0],1e-8)*122)/512,1-(tile//4*128+3+(1-(q[1]-lo[1])/max(hi[1]-lo[1],1e-8))*122)/512)
 objects.append(ob);return ob
def box(name,c,sz,t=0,rot=None):
 bpy.ops.mesh.primitive_cube_add(size=1,location=c);ob=bpy.context.object;ob.dimensions=sz
 if rot:ob.rotation_euler=rot
 return finish(ob,name,t)
def beam(name,a,c,w,t=2):
 a=Vector(a);c=Vector(c);ob=box(name,(a+c)/2,(w,w,(c-a).length),t);ob.rotation_euler=(c-a).to_track_quat('Z','Y').to_euler();return ob
def cyl(name,c,r,h,t=4,axis='Z',n=12):
 bpy.ops.mesh.primitive_cylinder_add(vertices=n,radius=r,depth=h,location=c);ob=bpy.context.object
 if axis=='X':ob.rotation_euler.y=math.pi/2
 if axis=='Y':ob.rotation_euler.x=math.pi/2
 return finish(ob,name,t)
def torus(name,c,r,minor,t=5,axis='Z',n=16,k=6):
 bpy.ops.mesh.primitive_torus_add(major_segments=n,minor_segments=k,location=c,major_radius=r,minor_radius=minor);ob=bpy.context.object
 if axis=='X':ob.rotation_euler.y=math.pi/2
 if axis=='Y':ob.rotation_euler.x=math.pi/2
 return finish(ob,name,t)
def panel(name,c,w,h,t):
 x,y,z=c;me=bpy.data.meshes.new(name);me.from_pydata([(x-w/2,y,z-h/2),(x+w/2,y,z-h/2),(x+w/2,y,z+h/2),(x-w/2,y,z+h/2)],[],[(0,1,2,3)]);ob=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(ob);return finish(ob,name,t)
def garage():
 box('Foundation',(0,0,.09),(8.4,7.4,.18),9)
 for x in [-3.9,3.9]:box('Warehouse_side',(x,0,2.15),(.2,7,4.1),0)
 box('Back_wall',(0,3.4,2.15),(8,.2,4.1),0)
 box('Front_left',(-3.15,-3.4,1.7),(1.7,.2,3.2),0);box('Front_right',(3.15,-3.4,1.7),(1.7,.2,3.2),0);box('Lintel',(0,-3.4,3.72),(8,.2,.96),0)
 for x in [-2.24,2.24]:box('Shutter_track',(x,-3.55,1.72),(.12,.16,3.25),3)
 for z in [2.8,3.,3.2,3.4]:box('Raised_shutter',(0,-3.55,z),(4.35,.08,.18),3)
 box('Shutter_housing',(0,-3.5,3.61),(4.65,.38,.32),3)
 # Raised roof and visibly supported old timber frame.
 for sign in [-1,1]:box('Clay_roof',(sign*2.1,0,4.90),(4.65,7.8,.18),1,(0,sign*math.radians(19),0))
 beam('Ridge',(0,-3.9,5.62),(0,3.9,5.62),.18,2)
 for y in [-3.4,3.4]:
  me=bpy.data.meshes.new('Gable');me.from_pydata([(-4,y,4.19),(4,y,4.19),(0,y,5.62)],[],[(0,1,2)]);ob=bpy.data.objects.new('Gable',me);bpy.context.collection.objects.link(ob);finish(ob,'Gable',0)
 for y in [-3.3,0,3.2]:
  beam('Tie_beam',(-3.8,y,4.04),(3.8,y,4.04),.16);beam('Rafter',(-3.95,y,4.12),(0,y,5.40),.16);beam('Rafter',(0,y,5.40),(3.95,y,4.12),.16)
 for x in [-3.85,3.85]:
  for y in [-3.2,3.1]:box('Old_post',(x,y,2.08),(.18,.18,3.85),2)
 panel('Workshop_sign',(-.15,-3.64,4.03),3.0,.64,12)
 panel('Old_side_door',(3.03,-3.515,1.3),.95,2.1,3)
 for x in [-3.94,3.94]:
  # Side windows are simple inset glass, no outlines.
  box('Side_window',(4.015 if x>0 else -4.015,-.3,2.55),(.03,1.8,1.1),3);box('Window_sill',(x,-.3,1.98),(.3,2,.09),2)
 # Bench, timber top and vice; tool wall remains open to the entrance.
 box('Bench_top',(-1.8,2.6,1.02),(3.3,.85,.12),2)
 for x in [-3.2,-.4]:
  for y in [2.32,2.87]:box('Bench_leg',(x,y,.57),(.12,.12,.9),3)
 box('Bench_drawers',(-2.4,2.6,.65),(1.2,.65,.55),3)
 for z in [.53,.75]:box('Drawer_handle',(-2.4,2.23,z),(.45,.06,.04),4)
 panel('Tool_board',(-1.8,3.27,2.12),2.9,1.4,13)
 for x in [-2.8,-2.3,-1.8,-1.25,-.75]:
  beam('Spanner',(x,3.19,1.83),(x,3.19,2.32),.055,4);torus('Spanner_head',(x,3.19,2.35),.07,.019,4,'Y',8,4)
 box('Vice',(-2.85,2.39,1.19),(.35,.28,.22),4);box('Vice_jaws',(-2.85,2.39,1.32),(.4,.12,.05),3)
 cyl('Old_race_helmet',(-.85,2.54,1.24),.19,.32,6,n=12)
 panel('Old_race_poster',(-3.35,3.275,2.75),.75,1.12,10)
 panel('Segment_records',(.65,3.275,2.12),1.03,1.4,14)
 panel('Three_empty_conditions',(2.5,3.27,2.12),2.05,1.4,11)
 box('Board_shelf',(1.9,3.06,1.32),(3.2,.26,.08),2)
 for x in [-.8,.8]:box('Service_runner',(x,.2,.2),(.2,2.8,.06),4)
 for stack,(x,y) in enumerate([(-3.3,-4.05),(-2.65,-4.1)]):
  for j in range(3 if stack==0 else 2):torus('Used_tyres',(x,y,.3+j*.23),.28,.10,5,n=16,k=6)
 box('Parts_crate',(3,-3.95,.42),(1.05,.65,.45),15)
 box('Repainted_wall_patch',(-3.6,-3.515,.8),(.55,.012,.38),8)
 for x in [-2,1.5]:box('Ceiling_work_lamp',(x,1.4,3.85),(.75,.3,.10),8)
def kart():
 # 950mm cadet wheelbase. Cargo carrier is a story modification, not racing homologation.
 for x in [-.28,.28]:beam('Chassis_rail',(x,-.70,.15),(x,.70,.15),.045,6)
 for y in [-.62,-.20,.35,.64]:beam('Crossmember',(-.4,y,.15),(.4,y,.15),.045,6)
 box('Floorpan',(0,-.22,.18),(.63,.85,.035),4)
 box('Nose',(0,-.77,.25),(.76,.22,.17),6)
 for x in [-.39,.39]:box('Side_pod',(x,0,.25),(.16,.74,.19),6)
 for x in [-.48,.48]:
  for y,label in [(-.50,'front'),(.45,'rear')]:
   wheel=torus('wheel_'+label+('_left' if x<0 else '_right'),(x,y,.17),.115,.055,5,'X',16,6);hub=cyl('Hub',(x,y,.17),.085,.135,4,'X',12);world=hub.matrix_world.copy();hub.parent=wheel;hub.matrix_world=world
 beam('Rear_axle',(-.50,.45,.17),(.50,.45,.17),.035,4)
 box('Seat_base',(0,.13,.29),(.36,.38,.10),7);box('Seat_back',(0,.32,.48),(.36,.08,.39),7,(math.radians(-12),0,0))
 for x in [-.20,.20]:box('Seat_cheek',(x,.12,.35),(.045,.38,.18),7)
 beam('Steering_column',(0,-.42,.20),(0,-.14,.57),.027,4)
 wheel=torus('Steering_wheel',(0,-.14,.59),.145,.014,7,'Y',16,4);wheel.rotation_euler.x=math.radians(40)
 for x in [-.12,.12]:box('Pedal',(x,-.48,.24),(.07,.09,.035),4)
 box('Engine',(.28,.42,.31),(.22,.30,.25),3)
 for z in [.26,.30,.34,.38]:box('Cooling_fin',(.29,.42,z),(.26,.33,.015),4)
 cyl('Exhaust',(.38,.63,.22),.035,.25,3,'Y',10)
 for x in [-.25,.25]:beam('Cargo_bracket',(x,.40,.19),(x,.88,.37),.035,4)
 box('Cargo_box',(0,.77,.46),(.52,.40,.28),15);box('Cargo_lid',(0,.77,.61),(.55,.43,.045),2)
 for x in [-.16,.16]:box('Box_strap',(x,.77,.636),(.035,.44,.012),3)
 box('Tape_repair',(-.38,-.13,.35),(.18,.06,.012),8);box('Replacement_nose_panel',(.23,-.885,.27),(.19,.008,.10),8)
 for x in [-.28,.28]:beam('Rear_bumper_support',(x,.64,.15),(x,1.01,.19),.045,4)
 beam('Rear_bumper',(-.38,1.01,.19),(.38,1.01,.19),.04,6)
 for ob in objects:
  if ob.name.startswith(('Nose','Side_pod','Cargo_box','Cargo_lid')):
   bpy.context.view_layer.objects.active=ob;mod=ob.modifiers.new('Worn_edges','BEVEL');mod.width=.018;mod.segments=1;bpy.ops.object.modifier_apply(modifier=mod.name)
def export(name,budget):
 bpy.ops.object.select_all(action='DESELECT')
 for ob in objects:ob.select_set(True)
 bpy.context.view_layer.objects.active=objects[0]
 tris=sum(sum(len(f.vertices)-2 for f in ob.data.polygons) for ob in objects);assert tris<=budget,(name,tris)
 points=[ob.matrix_world@v.co for ob in objects for v in ob.data.vertices];lo=[min(v[k] for v in points) for k in range(3)];hi=[max(v[k] for v in points) for k in range(3)]
 bpy.ops.export_scene.fbx(filepath=str(O/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,path_mode='COPY',embed_textures=True)
 bpy.ops.export_scene.gltf(filepath=str(O/(name+'.glb')),export_format='GLB',use_selection=True,export_yup=True)
 bpy.ops.wm.save_as_mainfile(filepath=str(O/(name+'.blend')))
 ASSETS.append(dict(id=name,tris=tris,budget=budget,bounds_blender=[lo,hi],dimensions=[hi[k]-lo[k] for k in range(3)],mesh_count=len(objects),wheels={ob.name:list(ob.location) for ob in objects if ob.name.startswith('wheel_')}))
def stage(scale=20):
 sc=b.stage();sc.cycles.samples=16;sc.render.resolution_x=1200;sc.render.resolution_y=800
 sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.24,.48,.58,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.7
 for ob in sc.objects:
  if ob.type=='LIGHT':ob.location=(-5,-7,12);ob.data.color=(1,.82,.61);ob.data.energy=1900;ob.data.size=5
 sc.view_settings.view_transform='Standard';return sc
for name,budget,make in [('story_garage_v01',6000,garage),('delivery_kart_v01',2500,kart)]:
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.scene.unit_settings.system='METRIC';objects=[];MAT=material();make();export(name,budget);sc=stage()
 if name.startswith('story'):
  for x in [-2,1.5]:
   bpy.ops.object.light_add(type='AREA',location=(x,1.4,3.7));light=bpy.context.object;light.data.energy=160;light.data.color=(.78,.9,1);light.data.size=2.3
  bpy.ops.object.light_add(type='AREA',location=(0,-3,2.8));light=bpy.context.object;light.data.energy=100;light.data.size=3;light.data.color=(.90,.94,1);b.aim(light,(0,2.8,1.8))
  for view,loc,target,scale in [('front',(0,-17,7),(0,0,2.3),14),('threequarter',(12,-16,10),(0,0,2.3),15),('interior',(0,-2.8,2.65),(0,2.7,1.95),9.3),('boards',(1.5,-.4,2.5),(1.5,3.2,2.1),4.7),('top',(0,-.01,22),(0,0,0),14),('side',(17,0,7),(0,0,2.2),14)]:b.render(sc,V/(name+'_'+view+'.png'),loc,target,scale)
 else:
  for view,loc in [('front',(0,-5,2.3)),('threequarter',(3,-4,2.4)),('side',(5,0,1.8)),('top',(0,0,6))]:b.render(sc,V/(name+'_'+view+'.png'),loc,(0,.1,.3),3.3)
(O/'manifest.json').write_text(json.dumps(dict(status='approval proposal only; no engine copy',units='metres',texture='GarageKartPaint.png',assets=ASSETS),ensure_ascii=False,indent=2),encoding='utf8')
