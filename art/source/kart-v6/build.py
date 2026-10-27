"""Kart v6: metre geometry, named wheels, V5 Cycles diffuse + AO bake."""
import bpy,bmesh,math,json
import numpy as np
from pathlib import Path
from mathutils import Vector
R=Path('D:/Github/F1Game');W=R/'art/source/kart-v6';E=R/'art/export/kart-v6'
bpy.ops.wm.read_factory_settings(use_empty=True)
mats={};obs=[]
for name in ['plastic','metal','rubber','seat','engine','cream','strap','number07']:
 m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;n.clear();o=n.new('ShaderNodeOutputMaterial');d=n.new('ShaderNodeBsdfDiffuse');t=n.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(W/'surfaces'/(name+'.png')));t.interpolation='Closest';uv=n.new('ShaderNodeUVMap');uv.uv_map='SourceUV';m.node_tree.links.new(uv.outputs[0],t.inputs[0]);m.node_tree.links.new(t.outputs[0],d.inputs[0]);m.node_tree.links.new(d.outputs[0],o.inputs[0]);mats[name]=m
def finish(o,name,mat):
 o.name=name;o.data.materials.append(mats[mat]);bpy.context.view_layer.objects.active=o
 bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
 if not o.data.uv_layers:o.data.uv_layers.new(name='SourceUV')
 o.data.uv_layers[0].name='SourceUV'
 # face-local source UV; baked UV is physically measured later
 for f in o.data.polygons:
  p=[o.data.vertices[i].co for i in f.vertices];u=(p[1]-p[0]).normalized();v=f.normal.cross(u).normalized();q=[((a-p[0]).dot(u),(a-p[0]).dot(v)) for a in p];lo=[min(a[i] for a in q) for i in range(2)];hi=[max(a[i] for a in q) for i in range(2)]
  for li,a in zip(f.loop_indices,q):o.data.uv_layers[0].data[li].uv=tuple((a[i]-lo[i])/max(1e-6,hi[i]-lo[i]) for i in range(2))
 obs.append(o);return o
def mesh(name,verts,faces,mat,loc=(0,0,0)):
 me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update()
 bm=bmesh.new();bm.from_mesh(me)
 if len(faces)>1:bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
 elif mat=='number07':bmesh.ops.reverse_faces(bm,faces=list(bm.faces))
 bm.to_mesh(me);bm.free();me.update()
 o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);o.location=loc;finish(o,name,mat)
 if mat=='number07':
  corners=[(0,1),(1,1),(1,0),(0,0)] if name=='rear_07' else [(0,0),(1,0),(1,1),(0,1)]
  for lp in me.loops:me.uv_layers['SourceUV'].data[lp.index].uv=corners[lp.vertex_index]
 return o
def box(name,loc,size,mat,bevel=0,rot=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.dimensions=size;o.rotation_euler.x=rot;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new('one bevel','BEVEL');mod.width=bevel;mod.segments=1;bpy.ops.object.modifier_apply(modifier=mod.name)
 return finish(o,name,mat)
def tube(name,a,b,r,mat='metal',n=6):
 a,b=Vector(a),Vector(b);bpy.ops.mesh.primitive_cylinder_add(vertices=n,radius=r,depth=(b-a).length,location=(a+b)/2);o=bpy.context.object;o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return finish(o,name,mat)
def ring(name,center,rad,width,mat='rubber',segments=12):
 verts=[];profile=[(-width/2,.50),(-width/2,.87),(-width*.34,1),(width*.34,1),(width/2,.87),(width/2,.50)]
 for x,r in profile:
  for j in range(segments):
   a=2*math.pi*j/segments;verts.append((x,rad*r*math.sin(a),rad*r*math.cos(a)))
 faces=[]
 for k in range(len(profile)):
  for j in range(segments):faces.append((k*segments+j,k*segments+(j+1)%segments,((k+1)%6)*segments+(j+1)%segments,((k+1)%6)*segments+j))
 return mesh(name,verts,faces,mat,center)
# Existing Race-relative visual datum: X right, Y forward, Z up (Unity X,Z,Y).
wheels={}
for code,x,y,z,r,w in [('FL',-.44,.54,.24,.148,.16),('FR',.44,.54,.24,.148,.16),('RL',-.52,-.51,.34,.222,.24),('RR',.52,-.51,.34,.222,.24)]:
 ring('k_tire_'+code,(x,y,z),r,w)
 tube('k_hub_'+code,(x-w*.47,y,z),(x+w*.47,y,z),r*.48,'cream',12)
 # same tyre/hub origin at true centre; axes remain vehicle aligned
 wheels[code]=dict(center_blender=[x,y,z],center_unity=[x,z,y],radius=r,width=w)
# Low tubular chassis, open cockpit.
perimeter=[(-.29,-.63,.235),(-.34,-.30,.205),(-.32,.18,.19),(-.26,.60,.19),(.26,.60,.19),(.32,.18,.19),(.34,-.30,.205),(.29,-.63,.235)]
for i in range(8):tube('frame_'+str(i),perimeter[i],perimeter[(i+1)%8],.025)
for y in [-.48,-.08,.45]:tube('cross_'+str(y),(-.30,y,.205),(.30,y,.205),.023)
for x in [-.25,.25]:tube('front_bumper_support_'+str(x),(x,.58,.19),(x,.81,.235),.022)
tube('rear_axle',(-.60,-.51,.34),(.60,-.51,.34),.025)
box('floor_pan',(0,.17,.205),(.56,.74,.025),'metal')
# Chamfered nose and side pods; wheels stay exposed.
box('front_bumper',(0,.875,.235),(1.07,.23,.16),'plastic',.045)
for sign in [-1,1]:
 box('side_pod_'+str(sign),(sign*.405,-.015,.31),(.25,.68,.20),'plastic',.045)
 box('side_stripe_'+str(sign),(sign*.534,-.015,.33),(.006,.44,.055),'cream')
# Tapered sloping central fairing (front tip low, steering end narrow and high).
mesh('nose', [(-.23,.73,.28),(.23,.73,.28),(.14,.25,.55),(-.14,.25,.55),(-.23,.73,.23),(.23,.73,.23),(.14,.25,.48),(-.14,.25,.48)],[(0,1,2,3),(4,7,6,5),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],'plastic')
mesh('front_07',[(-.125,.61,.351),(.125,.61,.351),(.105,.35,.497),(-.105,.35,.497)],[(0,1,2,3)],'number07')
box('rear_bumper',(0,-.75,.32),(1.10,.12,.16),'plastic',.035)
# Open bucket: pan + shaped back + side bolsters, not a solid box.
box('seat_pan',(-.075,-.14,.29),(.39,.38,.065),'seat',.025)
mesh('seat_back',[(-.285,-.30,.30),(.135,-.30,.30),(.16,-.48,.85),(.09,-.50,.912),(-.24,-.50,.912),(-.31,-.48,.85),(-.30,-.34,.31),(.15,-.34,.31),(.175,-.52,.85),(.105,-.54,.912),(-.255,-.54,.912),(-.325,-.52,.85)],[(0,1,2,3,4,5),(6,11,10,9,8,7),(0,6,7,1),(1,7,8,2),(2,8,9,3),(3,9,10,4),(4,10,11,5),(5,11,6,0)],'seat')
for sign in [-1,1]:
 mesh('seat_bolster_'+str(sign),[(sign*.18-.075,.02,.29),(sign*.23-.075,-.26,.31),(sign*.23-.075,-.34,.56),(sign*.18-.075,.02,.39)],[(0,1,2,3)],'seat')
# Small steering wheel and column, open ring / three spokes.
tube('steering_column',(0,.28,.23),(0,.08,.59),.018)
bpy.ops.mesh.primitive_torus_add(major_segments=12,minor_segments=4,location=(0,.075,.61),major_radius=.115,minor_radius=.014,rotation=(math.radians(52),0,0));finish(bpy.context.object,'steering_rim','rubber')
for a in [math.pi/2,math.pi*7/6,math.pi*11/6]:
 x=.105*math.cos(a);y=.105*math.sin(a)*math.cos(math.radians(52));z=.105*math.sin(a)*math.sin(math.radians(52));tube('steering_spoke',(0,.075,.61),(x,.075+y,.61+z),.009,n=4)
for x in [-.14,.14]:box('pedal',(x,.56,.235),(.09,.08,.03),'metal',rot=.35)
# Right rear air-cooled engine, recoil cover, fins and covered chain.
box('engine_block',(.29,-.45,.47),(.24,.28,.25),'engine',.025)
for i in range(3):box('cooling_fin_'+str(i),(.29,-.47,.54+i*.035),(.27,.29,.018),'metal')
box('fuel_tank',(.29,-.43,.69),(.24,.25,.10),'cream',.022)
tube('fuel_cap',(.29,-.43,.74),(.29,-.43,.765),.034,'rubber',8)
tube('recoil_cover',(.405,-.45,.47),(.435,-.45,.47),.095,'rubber',10)
tube('exhaust',(.29,-.61,.54),(.29,-.70,.54),.038,'metal',8)
box('chain_guard',(.37,-.41,.335),(.065,.37,.13),'metal',.018)
# Only delivery modification: compact rear rack + two webbing straps.
for x in [-.27,.12]:tube('rack_post',(x,-.66,.30),(x,-.66,.48),.018,n=5)
box('cargo_rack',(-.075,-.665,.49),(.44,.26,.025),'metal')
for x in [-.285,.135]:tube('rack_rail',(x,-.79,.51),(x,-.54,.51),.015,n=5)
for x in [-.18,.035]:box('rack_strap',(x,-.665,.507),(.035,.25,.009),'strap')
mesh('rear_07',[(-.18,-.793,.50),(.08,-.793,.50),(.08,-.793,.33),(-.18,-.793,.33)],[(0,1,2,3)],'number07')
# Bake atlas: same face-planar 32 texels/m and 2px tiny-face minimum as V5.
bpy.context.view_layer.update();rects=[]
for o in obs:
 o.data.uv_layers.new(name='BakedUV');o.data.uv_layers.active_index=1
 for f in o.data.polygons:
  ps=[o.data.vertices[i].co for i in f.vertices];u=(ps[1]-ps[0]).normalized();v=f.normal.cross(u).normalized();q=[((p-ps[0]).dot(u),(p-ps[0]).dot(v)) for p in ps];lo=[min(a[i] for a in q) for i in range(2)];hi=[max(a[i] for a in q) for i in range(2)]
  wh=[max(2,math.ceil((hi[i]-lo[i])*32)) for i in range(2)];rects.append((o,f,q,lo,hi,*wh))
AW=1024;x=y=4;rh=0;placed=[]
for o,f,q,lo,hi,w,h in sorted(rects,key=lambda r:r[-1],reverse=True):
 if x+w+4>AW:x=4;y+=rh+8;rh=0
 placed.append((o,f,q,lo,hi,w,h,x,y));x+=w+8;rh=max(rh,h)
AH=2**math.ceil(math.log2(y+rh+8))
for o,f,q,lo,hi,w,h,x,y in placed:
 for li,a in zip(f.loop_indices,q):o.data.uv_layers['BakedUV'].data[li].uv=((x+(a[0]-lo[0])/max(1e-8,hi[0]-lo[0])*w)/AW,(y+(a[1]-lo[1])/max(1e-8,hi[1]-lo[1])*h)/AH)
decals=[]
for o in obs:
 if o.name not in ['front_07','rear_07']:continue
 o.data.calc_loop_triangles()
 for tri in o.data.loop_triangles:decals.append(dict(name=o.name,source=[list(o.data.uv_layers['SourceUV'].data[i].uv) for i in tri.loops],baked=[list(o.data.uv_layers['BakedUV'].data[i].uv) for i in tri.loops]))
(W/'decal-uv.json').write_text(json.dumps(decals))
P=[o.matrix_world@v.co for o in obs for v in o.data.vertices];lo=[min(p[i] for p in P) for i in range(3)];hi=[max(p[i] for p in P) for i in range(3)];tris=sum(len(f.vertices)-2 for o in obs for f in o.data.polygons)
metrics=dict(triangles=tris,objects=len(obs),bounds_min=lo,bounds_max=hi,dimensions_xyz=[hi[i]-lo[i] for i in range(3)],wheels=wheels,track_width_parameter=.96,wheelbase=1.05,atlas=[AW,AH],density=32,forward='Blender +Y -> Unity +Z',datum='Race-relative metres; original visual wheel centres kept; do not reapply 1.47 scaling or rear -0.08 correction')
(W/'geometry.json').write_text(json.dumps(metrics,indent=2));assert 1000<=tris<=2000,tris
assert abs(hi[0]-lo[0]-1.28)<.001 and abs(hi[1]-lo[1]-1.8)<.001 and abs(hi[2]-lo[2]-.82)<.001,(lo,hi)
sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=128;sc.cycles.max_bounces=4;sc.cycles.use_denoising=True;sc.view_settings.view_transform='Standard'
try:
 prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
 for d in prefs.devices:d.use=d.type=='OPTIX'
 sc.cycles.device='GPU'
except Exception as e:print('GPU unavailable',e)
bpy.ops.object.light_add(type='SUN');sun=bpy.context.object;sun.data.energy=2.4;sun.data.color=(1,.83,.60);sun.data.angle=.10;sun.rotation_euler=Vector((.7,.65,-.45)).to_track_quat('-Z','Y').to_euler()
sc.world=bpy.data.worlds.new('V5 teal sky');sc.world.use_nodes=True;bg=sc.world.node_tree.nodes['Background'];bg.inputs[0].default_value=(.30,.52,.65,1);bg.inputs[1].default_value=.65
bpy.ops.mesh.primitive_plane_add(size=12,location=(0,0,.09));floor=bpy.context.object;floor.name='bake_ground';fm=bpy.data.materials.new('ground');fm.diffuse_color=(.42,.36,.27,1);floor.data.materials.append(fm)
img=bpy.data.images.new('bake_target',AW,AH,alpha=False)
for m in mats.values():t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=img;m.node_tree.nodes.active=t
sc.render.bake.margin=3;sc.render.bake.use_clear=True;sc.render.bake.use_pass_direct=True;sc.render.bake.use_pass_indirect=True;sc.render.bake.use_pass_color=True
# Join a temporary copy to avoid baking the atlas once per tiny component.
bpy.ops.object.select_all(action='DESELECT')
for o in obs:o.select_set(True)
bpy.context.view_layer.objects.active=obs[0];bpy.ops.object.duplicate();dups=list(bpy.context.selected_objects);bpy.context.view_layer.objects.active=dups[0];bpy.ops.object.join();joined=bpy.context.object
for o in obs:o.hide_render=True
for mode,filename in [('DIFFUSE','raw.png'),('AO','ao.png')]:
 bpy.ops.object.bake(type=mode);img.filepath_raw=str(W/filename);img.file_format='PNG';img.save();print('BAKED',mode,flush=True)
bpy.data.objects.remove(joined,do_unlink=True)
for o in obs:o.hide_render=False
bpy.data.objects.remove(floor,do_unlink=True);bpy.data.objects.remove(sun,do_unlink=True)
bpy.ops.wm.save_as_mainfile(filepath=str(W/'kart-v6-baked.blend'))
print('GEOMETRY',tris,lo,hi,flush=True)
