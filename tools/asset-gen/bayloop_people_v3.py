"""D-146: reuse v1 mesh construction, rig hierarchy and baked UAL actions."""
from pathlib import Path
import sys,json,math
import bpy
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).parent))
import bayloop_people as p
b=p.b;R=b.ROOT;O=R/'art/models/bayloop_people_v03';V=R/'art/_review/2026-10-26-people-v3'
O.mkdir(parents=True,exist_ok=True);V.mkdir(parents=True,exist_ok=True)
palette=json.loads((R/'art/source/palette/pitcrew_d123_32.json').read_text())['colors']
# 4x4 solid 16px swatches, no photograph/no noise. Metric UV12px/m.
col=[23,3,16,18,25,2,19,2,25,23,2,13,0,0,0,0]
tex=bpy.data.images.new('PeopleRetroAtlas_v3',64,64,alpha=True);pixels=[]
for y in range(64):
 for x in range(64):pixels.extend([c/255 for c in palette[col[(y//16)*4+x//16]]]+[1])
tex.pixels=pixels;tex.filepath_raw=str(O/'PeopleRetroAtlas_v3.png');tex.file_format='PNG';tex.save()
original_cube=p.cuboid;original_ring=p.rings
def cube(name,c,sz,tile):
 if name in ['nose','mouth']:return
 if name=='eye':
  x,y,z=c;b.PART='eye'
  p.face([(p.S*(x+.011*math.cos(j*math.tau/8)),p.S*(-.094),p.S*(z+.011*math.sin(j*math.tau/8))) for j in range(8)],'eye')
  b.PART='cheek'
  p.face([(p.S*(x*1.48+.012*math.cos(j*math.tau/8)),p.S*(-.086),p.S*(z-.031+.009*math.sin(j*math.tau/8))) for j in range(8)],'lip')
  return
 if name=='torso' and sz[0]<.02:return
 original_cube(name,c,sz,tile)
def ring(name,rows,tile,n=8):
 if name.startswith('thumb_'):return
 if name=='torso':
  rows=[(0,0,.82,.15,.11),(0,0,.92,.19,.14),(0,0,1.08,.205,.15),(0,0,1.25,.19,.135),(0,0,1.35,.18,.10),(0,0,1.40,.12,.075)]
 if name.startswith('hand_'):
  x,y,_,_,_=rows[1];rows=[(x,y,.88,.02,.02),(x,y,.83,.046,.039),(x,y,.775,.035,.03),(x,y,.76,.012,.012)];n=8
 original_ring(name,rows,tile,n)
p.cuboid=cube;p.rings=ring
def age_shape(q,scaled=True):
 return tuple(q) if scaled else tuple(c*p.S for c in q)
p.age_shape=age_shape
def shape(q):
 x,y,z=(c/p.S for c in q)
 t=max(0,min(1,(z-1.40)/.045));x*=1+1.15*t;y*=1+1.15*t
 if z<=.88:z=z*.45/.88
 elif z<=1.445:z=.45+(z-.88)*.51/.565
 else:z=.96+(z-1.445)*.33/.235
 if p.ELDER:
  if z>.72:
   dz=z-.72;ang=math.radians(8);y,z=y*math.cos(ang)-dz*math.sin(ang),.72+y*math.sin(ang)+dz*math.cos(ang)
  x*=.98;y*=.98;z*=.98
 return Vector((x,y,z))
def accessory(ob,rig,age,var,tiles):
 parts=[]
 def box(c,sz,tile,bone):
  bpy.ops.mesh.primitive_cube_add(size=1,location=c);o=bpy.context.object;o.dimensions=sz;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);parts.append((o,tile,bone))
 def hat(z,r,depth,tile):
  bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=r,depth=depth,location=(0,-.07 if age=='elder' else .015,z));parts.append((bpy.context.object,tile,'Head'))
 if age=='young' and var==0:
  hat(1.285,.19,.06,0);box((0,-.17,1.27),(.26,.18,.025),0,'Head')
 elif age=='young' and var==1:
  box((0,-.151,.65),(.29,.026,.31),8,'Spine');box((0,-.12,.86),(.18,.025,.16),8,'Chest')
 elif age=='young':
  box((0,-.055,.91),(.28,.18,.075),0,'Chest');box((.065,-.16,.80),(.075,.028,.22),0,'Chest')
 elif var==0:
  hat(1.215,.24,.035,0);hat(1.26,.17,.065,0)
 elif var==1:
  box((0,-.02,.86),(.40,.21,.09),8,'Chest');box((-.14,-.13,.75),(.07,.035,.22),8,'Chest')
 else:
  box((0,-.155,.60),(.32,.025,.25),0,'Spine');box((0,-.01,.73),(.37,.29,.035),0,'Spine')
 for o,tile,bone in parts:
  bpy.context.view_layer.objects.active=o;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
  o.vertex_groups.new(name=bone).add(list(range(len(o.data.vertices))),1,'REPLACE');o.data.materials.append(ob.data.materials[0]);o.data.uv_layers.new();tiles.extend([tile]*len(o.data.polygons))
  bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);o.select_set(True);bpy.context.view_layer.objects.active=ob;bpy.ops.object.join()
if '--no-render' in sys.argv:b.render=lambda *args,**kwargs:None
assets=[]
for age in ['young','elder']:
 for var in range(3):
  bpy.ops.wm.read_factory_settings(use_empty=True);name=f'people_{age}_{var+1:02d}_v3';ob,rig=p.build(age,var)
  # Capture source atlas tile labels before replacing UVs.
  tiles=[min(3,int(ob.data.uv_layers.active.data[f.loop_start].uv.x*4))+4*(3-min(3,int(ob.data.uv_layers.active.data[f.loop_start].uv.y*4))) for f in ob.data.polygons]
  for v in ob.data.vertices:v.co=shape(v.co)
  bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
  for bone in rig.data.edit_bones:bone.head=shape(bone.head);bone.tail=shape(bone.tail)
  bpy.ops.object.mode_set(mode='OBJECT')
  if age=='elder' and var==0:
   # A plain curved-handle walking stick, bound to existing hand (no new bone).
   verts=[];faces=[]
   for cx,cy,cz in [(-.35,-.07,.01),(-.35,-.07,.405),(-.33,-.07,.435),(-.29,-.07,.435)]:
    verts.extend([(cx+.012*math.cos(j*math.tau/6),cy+.012*math.sin(j*math.tau/6),cz) for j in range(6)])
   for row in range(3):
    for j in range(6):faces.append((row*6+j,row*6+(j+1)%6,(row+1)*6+(j+1)%6,(row+1)*6+j))
   me=bpy.data.meshes.new('Cane');me.from_pydata(verts,[],faces);stick=bpy.data.objects.new('Cane',me);bpy.context.collection.objects.link(stick)
   stick.vertex_groups.new(name='Hand_L').add(list(range(len(verts))),1,'REPLACE');stick.data.materials.append(ob.data.materials[0]);stick.data.uv_layers.new()
   bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);stick.select_set(True);bpy.context.view_layer.objects.active=ob;bpy.ops.object.join();tiles.extend([5]*len(faces))
  accessory(ob,rig,age,var,tiles)
  uv=ob.data.uv_layers.active
  for f,tile in zip(ob.data.polygons,tiles):
   coords=[ob.data.vertices[i].co for i in f.vertices];normal=f.normal;axes=sorted(range(3),key=lambda k:abs(normal[k]))[:2]
   lows=[min(v[k] for v in coords) for k in axes]
   for li,v in zip(f.loop_indices,coords):uv.data[li].uv=((tile%4*16+2+(v[axes[0]]-lows[0])*12)/64,(tile//4*16+2+(v[axes[1]]-lows[1])*12)/64)
  b.TEX=O/'PeopleRetroAtlas_v3.png';ob.data.materials.clear();mat=b.material();mat.name='People_D146_Retro12';ob.data.materials.append(mat)
  src=R/f'art/models/bayloop_people_v01/people_{age}_{var+1:02d}.blend'
  with bpy.data.libraries.load(str(src),link=False) as (fr,to):to.actions=[n for n in fr.actions if n in ['Stand','Walk','Sit']]
  rig.animation_data_create();sc=bpy.context.scene;sc.render.fps=30
  for act in to.actions:
   rig.animation_data.action=act
   if act.slots:rig.animation_data.action_slot=act.slots[0]
   first,last=map(int,act.frame_range)
   for frame in range(first,last+1):
    sc.frame_set(frame)
    for pb in rig.pose.bones:pb.location*=.45/.88;pb.keyframe_insert('location',frame=frame)
    bpy.context.view_layer.update();ev=ob.evaluated_get(bpy.context.evaluated_depsgraph_get());bottom=min(v.co.z for v in ev.data.vertices)
    root=rig.pose.bones['Root'];root.location+=root.bone.matrix_local.to_3x3().inverted()@Vector((0,0,-bottom));root.keyframe_insert('location',frame=frame)
  rig.animation_data.action=bpy.data.actions['Stand'];sc.frame_set(1)
  bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
  bpy.ops.export_scene.fbx(filepath=str(O/(name+'.fbx')),use_selection=True,object_types={'ARMATURE','MESH'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_ALL',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='COPY',embed_textures=True)
  bpy.ops.wm.save_as_mainfile(filepath=str(O/(name+'.blend')))
  tris=sum(len(f.vertices)-2 for f in ob.data.polygons);h=max(v.co.z for v in ob.data.vertices);assert tris<=1200
  assets.append(dict(id=name,fbx=b.rel(O/(name+'.fbx')),tris=tris,height=h,bones=len(rig.data.bones),head_ratio_design=1.29/.33,leg_design_m=.45,torso_design_m=.51,source=b.rel(src)))
  sc=b.stage();sc.cycles.samples=8;sc.world.node_tree.nodes['Background'].inputs[1].default_value=.9
  b.render(sc,V/(name+'.png'),(2,-6,1.65),(0,0,.65),3.0)
  if False:
   for action in ['Walk','Sit']:
    rig.animation_data.action=bpy.data.actions[action];sc.frame_set(11);b.render(sc,V/(name+'_'+action+'.png'),(3,-5,1.4),(0,0,.6),3.0)
  print('DONE',name,tris,flush=True)
(O/'manifest.json').write_text(json.dumps(dict(direction='D-146',density=12,photographs=False,assets=assets),indent=2),encoding='utf8')
