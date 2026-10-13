"""Original low-poly delivery formula. Blender -Y forward; FBX Unity +Z.
Run blender -b -P tools/asset-gen/coastal_formula.py from repository root.
"""
import bpy, math, random, json
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'F1/Assets/Art/CoastalFormula'
SRC=ROOT/'art/source/coastal-formula'
OUT.mkdir(parents=True,exist_ok=True); SRC.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
palette=[('red',(173,49,36)),('cream',(226,214,182)),('rubber',(39,42,43)),('steel',(104,111,109)),('dark',(33,49,51)),('orange',(208,123,42)),('rim',(177,171,148)),('lamp',(241,211,137)),('tail',(145,27,23)),('patch',(108,133,127)),('seat',(85,64,48)),('lid',(228,149,57))]
image=bpy.data.images.new('FormulaAtlas',width=256,height=256)
pixels=[0.0]*(256*256*4); rng=random.Random(27)
for yy in range(256):
 for xx in range(256):
  idx=(yy//64)*4+xx//64; rgb=palette[min(idx,11)][1]
  # Sparse 2px clusters; vehicle paint is intentionally quieter than buildings.
  block=random.Random((xx//2)*179+(yy//2)*433+27).random()
  delta=-7 if block<.055 else (4 if block>.97 else 0)
  p=(yy*256+xx)*4; pixels[p:p+4]=[max(0,min(255,c+delta))/255 for c in rgb]+[1]
image.pixels=pixels; image.filepath_raw=str(OUT/'FormulaAtlas.png'); image.file_format='PNG';image.save()
# Wear is authored per material tile: chipped paint, dusty rubber and oxidized metal.
# Separate files let restored parts swap appearance without changing shared assets.
for state,strength in [('Worn',1.0),('Serviced',.35)]:
 variant=list(pixels)
 for yy in range(256):
  for xx in range(256):
   idx=(yy//64)*4+xx//64;u=xx%64;v=yy%64;p=(yy*256+xx)*4
   rgb=[c*255 for c in pixels[p:p+3]]
   if idx in [0,1,3,5,6,9,11]:
    dust=(.12 if idx==0 else .07)*strength
    rgb=[c*(1-dust)+d*dust for c,d in zip(rgb,(149,128,102))]
    # Small connected marks, concentrated near edges rather than uniform noise.
    scrape=((v in [7,8] and 8<u<24) or (v in [47,48] and 37<u<51) or (u in [6,7] and 30<v<39))
    chip=((u-49)**2+(v-12)**2<13 or (u-12)**2+(v-51)**2<8)
    if scrape or chip:
     target=(103,104,96) if idx in [0,1,9] else ((115,78,47) if idx in [3,6] else (158,125,77))
     rgb=[c*(1-strength*.85)+d*strength*.85 for c,d in zip(rgb,target)]
   elif idx==2:
    rgb=[c*(1-.16*strength)+d*.16*strength for c,d in zip(rgb,(101,92,74))]
   variant[p:p+3]=[c/255 for c in rgb]
 worn=bpy.data.images.new('FormulaAtlas'+state,width=256,height=256);worn.pixels=variant;worn.filepath_raw=str(OUT/('FormulaAtlas'+state+'.png'));worn.file_format='PNG';worn.save()
mats={}
for i,(name,rgb) in enumerate(palette):
 m=bpy.data.materials.new(name);m.diffuse_color=(*[c/255 for c in rgb],1);mats[name]=m
groups={}; current='body'
def add(o,name,color):
 group=current
 if current=='body':
  if any(k in name for k in ['Wing','Endplate','Diffuser']):group='aero'
  elif name in ['EngineCover','Exhaust','ExhaustDark']:group='engine'
  elif name=='Wishbone':group='suspension'
 o.name=name;o.data.materials.append(mats[color]);groups.setdefault(group,[]).append(o);return o
def box(name,loc,size,color,bevel=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.scale=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new('Small planar corners','BEVEL');mod.width=bevel;mod.segments=1
  bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
 return add(o,name,color)
def cyl(name,loc,r,depth,color,rot=(0,0,0),n=16):
 bpy.ops.mesh.primitive_cylinder_add(vertices=n,radius=r,depth=depth,location=loc,rotation=rot)
 return add(bpy.context.object,name,color)
def bar(name,a,b,r,color):
 d=Vector(b)-Vector(a);o=cyl(name,(Vector(a)+Vector(b))/2,r,d.length,color,n=6);o.rotation_euler=d.to_track_quat('Z','Y').to_euler();return o
def section(name,sections,color):
 # Each section: y, half-width, bottom, top. Explicit closed hull.
 vs=[]
 for y,w,b,t in sections:vs.extend([(-w,y,b),(w,y,b),(w,y,t),(-w,y,t)])
 fs=[(3,2,1,0)]
 for j in range(len(sections)-1):
  for k in range(4):fs.append((j*4+k,j*4+(k+1)%4,(j+1)*4+(k+1)%4,(j+1)*4+k))
 fs.append(tuple(range((len(sections)-1)*4,len(sections)*4)))
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);return add(o,name,color)

# Slim central nose, broad rear pods: open-wheel silhouette first.
box('Floor',(0,.1,.20),(1.28,2.45,.10),'dark',.04)
section('TaperedNose',[(-2.03,.13,.29,.39),(-1.7,.17,.29,.46),(-.77,.27,.29,.61),(-.36,.34,.25,.65)],'red')
section('NoseCreamStripe',[(-2.025,.045,.397,.403),(-1.69,.055,.467,.473),(-.78,.075,.614,.620)],'cream')
box('FrontWing',(0,-1.79,.17),(1.77,.32,.065),'cream',.012)
box('FrontWingFlap',(0,-1.56,.235),(1.50,.14,.045),'red')
for s in [-1,1]:
 box('FrontEndplate',(s*.875,-1.75,.225),(.035,.46,.24),'red',.009)
 section('Sidepod', [(-.39,.16,.24,.51),(-.19,.24,.23,.55),(.74,.22,.23,.51),(1.11,.12,.23,.43)],'red').location.x=s*.43
 box('Intake',(s*.45,-.395,.425),(.26,.018,.135),'dark')
 box('PodCream',(s*.666,.23,.42),(.012,.87,.075),'cream')
 box('CockpitSill',(s*.30,.03,.57),(.10,.79,.13),'red',.025)
 bar('MirrorStem',(s*.30,-.34,.62),(s*.51,-.39,.71),.018,'steel')
 box('Mirror',(s*.52,-.39,.73),(.15,.10,.075),'red',.015)
 box('MirrorGlass',(s*.52,-.336,.73),(.10,.008,.04),'rim')
 # Double wishbones visible between wheel and body.
 for y in [-1.20,1.10]:
  for z in [.29,.44]:
   for dy in [-.23,.23]:bar('Wishbone',(s*.24,y+dy,z),(s*.79,y,.36),.019,'steel')
box('CockpitDark',(0,.025,.46),(.50,.69,.08),'dark')
box('SeatBack',(0,.29,.64),(.39,.13,.42),'seat',.04)
box('Dashboard',(0,-.31,.58),(.47,.12,.16),'dark',.02)
bar('RollLeft',(-.23,.35,.60),(-.18,.35,.97),.032,'steel');bar('RollRight',(.23,.35,.60),(.18,.35,.97),.032,'steel')
bar('RollTop',(-.18,.35,.97),(.18,.35,.97),.032,'steel')
section('EngineCover',[(.39,.26,.29,.66),(.81,.22,.29,.61),(1.30,.14,.29,.41)],'red')
box('Rack',(0,.93,.66),(.78,.73,.065),'steel',.018)
for s in [-1,1]:
 bar('RackStay',(s*.30,.57,.36),(s*.35,.65,.63),.025,'steel')
 bar('RackStay',(s*.30,1.25,.30),(s*.35,1.20,.63),.025,'steel')
 box('WingPost',(s*.38,1.49,.54),(.04,.12,.53),'steel')
 box('RearEndplate',(s*.72,1.54,.81),(.04,.43,.25),'red',.015)
box('RearWing',(0,1.54,.855),(1.46,.34,.055),'cream',.014)
box('RearWingFlap',(0,1.39,.915),(1.40,.11,.055),'red')
box('Diffuser',(0,1.42,.24),(.89,.36,.10),'dark')
for x in [-.34,-.17,0,.17,.34]:box('DiffuserFin',(x,1.43,.18),(.025,.32,.10),'dark')
box('TailLight',(0,1.612,.45),(.11,.025,.10),'tail')
for s in [-1,1]:
 cyl('Headlamp',(s*.24,-1.30,.51),.063,.07,'dark',(math.pi/2,0,0),12)
 cyl('Lens',(s*.24,-1.341,.51),.049,.009,'lamp',(math.pi/2,0,0),12)
 cyl('Exhaust',(s*.20,1.53,.32),.045,.20,'steel',(math.pi/2,0,0),10)
 cyl('ExhaustDark',(s*.20,1.637,.32),.032,.01,'dark',(math.pi/2,0,0),10)
# Deliberate repair patch and a pair of small cream delivery marks.
box('RepairPanel',(-.681,.42,.36),(.009,.24,.14),'patch')
for x in [-.676,.676]:
 box('DoorMark',(x,.08,.46),(.014,.16,.13),'cream')

current='driver'
box('DriverShoulders',(0,.10,.62),(.37,.29,.18),'cream',.05)
bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=.19,location=(0,.065,.88));add(bpy.context.object,'Helmet','cream')
box('Visor',(0,-.105,.89),(.29,.065,.095),'dark',.023)
box('HelmetStripe',(0,.05,1.068),(.055,.15,.008),'red')
for s in [-1,1]:bar('Sleeve',(s*.16,.02,.67),(s*.17,-.23,.65),.056,'cream')
cyl('SteeringWheel',(0,-.24,.65),.13,.025,'dark',(math.radians(65),0,0),12)

current='delivery_bag'
box('CanvasBag',(0,.92,.925),(.68,.60,.45),'orange',.045)
box('BagLid',(0,.92,1.16),(.72,.64,.055),'lid',.02)
box('BagPocket',(0,1.231,.91),(.39,.025,.21),'lid',.018)
box('Label',(0,1.25,.93),(.16,.008,.09),'cream')
for s in [-1,1]:
 box('StrapTop',(s*.24,.92,1.192),(.055,.64,.012),'dark')
 for y in [.609,1.238]:box('StrapVertical',(s*.24,y,.935),(.055,.014,.43),'dark')
 box('Buckle',(s*.24,1.25,.90),(.075,.022,.065),'steel')
bar('HandleL',(-.12,.93,1.195),(-.12,.93,1.25),.018,'dark');bar('HandleR',(.12,.93,1.195),(.12,.93,1.25),.018,'dark');bar('HandleTop',(-.12,.93,1.25),(.12,.93,1.25),.018,'dark')

# Tires have chamfered shoulders instead of solid toy cylinders.
wheel_centers={}
for axle,y in [('front',-1.20),('rear',1.10)]:
 for side,s in [('left',1),('right',-1)]:
  # This FBX axis conversion mirrors Blender X; names describe Unity sides.
  current=f'wheel_{axle}_{side}';cx=s*.79;r=.35 if axle=='front' else .375;z=r;w=.26 if axle=='front' else .32
  wheel_centers[current]=(cx,y,z)
  verts=[];faces=[];n=20
  for dx,rad in [(-w/2,r*.87),(-w/2+.035,r),(w/2-.035,r),(w/2,r*.87)]:
   for j in range(n):a=2*math.pi*j/n;verts.append((cx+dx,y+math.sin(a)*rad,z+math.cos(a)*rad))
  for k in range(3):
   for j in range(n):faces.append((k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j))
  faces.extend([tuple(reversed(range(n))),tuple(range(3*n,4*n))])
  me=bpy.data.meshes.new(current);me.from_pydata(verts,[],faces);me.update();o=bpy.data.objects.new(current,me);bpy.context.collection.objects.link(o);add(o,'Tire','rubber')
  for ss in [-1,1]:
   xx=cx+ss*(w/2+.002)
   cyl('Rim',(xx,y,z),r*.55,.008,'rim',(0,math.pi/2,0),12)
   cyl('Inset',(xx+ss*.006,y,z),r*.38,.008,'dark',(0,math.pi/2,0),12)
   cyl('Hub',(xx+ss*.013,y,z),.059,.016,'steel',(0,math.pi/2,0),8)
   for j in range(5):
    a=j*2*math.pi/5
    bar('Spoke',(xx+ss*.011,y+math.sin(a)*.045,z+math.cos(a)*.045),(xx+ss*.011,y+math.sin(a)*r*.48,z+math.cos(a)*r*.48),.019,'rim')

# Apply transforms, project each polygon into its palette tile; separate animation pivots.
objects=[];report={}
for name,parts in groups.items():
 bpy.ops.object.select_all(action='DESELECT')
 for o in parts:o.select_set(True)
 bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
 bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
 bpy.context.scene.cursor.location=wheel_centers.get(name,(0,0,0));bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
 if name=='delivery_bag':
  for v in o.data.vertices:v.co.z=.69+(v.co.z-.69)*.60
 bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
 uv=o.data.uv_layers.new(name='PixelAtlas') if not o.data.uv_layers else o.data.uv_layers.active
 for face in o.data.polygons:
  ci=[p[0] for p in palette].index(o.data.materials[face.material_index].name);axis=max(range(3),key=lambda a:abs(face.normal[a]));axes=[a for a in range(3) if a!=axis]
  coords=[o.data.vertices[o.data.loops[l].vertex_index].co for l in face.loop_indices]
  lows=[min(v[a] for v in coords) for a in axes];spans=[max(v[a] for v in coords)-lows[k] for k,a in enumerate(axes)]
  for l,v in zip(face.loop_indices,coords):
   q=[(v[a]-lows[k])/max(spans[k],.001) for k,a in enumerate(axes)]
   uv.data[l].uv=((ci%4*64+3+q[0]*57)/256,(ci//4*64+3+q[1]*57)/256)
  face.material_index=0
 o.data.materials.clear()
 if 'FormulaShared' not in bpy.data.materials:
  m=bpy.data.materials.new('FormulaShared');m.use_nodes=True;t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=image;t.interpolation='Closest';m.node_tree.links.new(t.outputs['Color'],m.node_tree.nodes['Principled BSDF'].inputs['Base Color']);m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.85
 o.data.materials.append(bpy.data.materials['FormulaShared'])
 objects.append(o);report[name]={'triangles':sum(len(p.vertices)-2 for p in o.data.polygons),'pivot_blender':list(o.location),'dimensions':list(o.dimensions)}
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'CoastalFormula.blend'))
bpy.ops.export_scene.fbx(filepath=str(OUT/'CoastalFormula.fbx'),use_selection=True,axis_forward='-Z',axis_up='Y',bake_space_transform=True,mesh_smooth_type='FACE',add_leaf_bones=False,apply_scale_options='FBX_SCALE_ALL')
bpy.ops.export_scene.gltf(filepath=str(SRC/'CoastalFormula.glb'),use_selection=True,export_format='GLB')
(SRC/'geometry-report.json').write_text(json.dumps(report,indent=2));print('FORMULA_OK',json.dumps(report))
