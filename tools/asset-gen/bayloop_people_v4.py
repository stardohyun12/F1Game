from pathlib import Path
import sys,json,math
import bpy
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).parent))
import bayloop_people as p
b=p.b;R=b.ROOT;O=R/'art/models/bayloop_people_v04';V=R/'art/_review/2026-10-26-people-v4';V.mkdir(parents=True,exist_ok=True)
oldring=p.rings;oldcube=p.cuboid
def ring(name,rows,tile,n=8):
 if name.startswith(('thumb_','ear')):return
 if p.ELDER and tile=='coat':b.TILES['elder_coat']=12+var;tile='elder_coat'
 if p.ELDER and var==0 and name=='torso':rows=[(x,y,.73 if z==.82 else z,rx*1.07,ry) for x,y,z,rx,ry in rows]
 if name=='hair' and var==2:rows=[(x,y,z,rx*1.18,ry*1.13) for x,y,z,rx,ry in rows]
 if name=='hair' and var==1 and not p.ELDER:b.TILES['brown_hair']=15;tile='brown_hair'
 if name=='head':rows=[(0,-.008,1.43,.055,.06),(0,-.012,1.48,.083,.08),(0,0,1.56,.093,.085),(0,.006,1.63,.084,.08),(0,.01,1.665,.055,.052)]
 oldring(name,rows,tile,6)
def cube(name,c,sz,tile):
 if name in ['nose','eye','mouth']:return
 oldcube(name,c,sz,tile)
p.rings=ring;p.cuboid=cube
assets=[]
for age in ['young','elder']:
 for var in range(3):
  bpy.ops.wm.read_factory_settings(use_empty=True);name=f'people_{age}_{var+1:02d}_v4';ob,rig=p.build(age,var)
  tiles=[min(3,int(ob.data.uv_layers.active.data[f.loop_start].uv.x*4))+4*(3-min(3,int(ob.data.uv_layers.active.data[f.loop_start].uv.y*4))) for f in ob.data.polygons]
  if age=='elder' and var==0:
   vs=[];fs=[]
   for x,y,z in [(-.32,-.08,0),(-.32,-.08,.76),(-.29,-.08,.80),(-.255,-.08,.80)]:vs.extend([(x+.014*math.cos(j*math.tau/6),y+.014*math.sin(j*math.tau/6),z) for j in range(6)])
   for i in range(3):
    for j in range(6):fs.append((i*6+j,i*6+(j+1)%6,(i+1)*6+(j+1)%6,(i+1)*6+j))
   me=bpy.data.meshes.new('Cane');me.from_pydata(vs,[],fs);q=bpy.data.objects.new('Cane',me);bpy.context.collection.objects.link(q);q.vertex_groups.new(name='Hand_L').add(list(range(len(vs))),1,'REPLACE');q.data.materials.append(ob.data.materials[0]);q.data.uv_layers.new();tiles.extend([5]*len(fs));bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);q.select_set(True);bpy.context.view_layer.objects.active=ob;bpy.ops.object.join()
  uv=ob.data.uv_layers.active
  for f,t in zip(ob.data.polygons,tiles):
   f.use_smooth=False;coords=[ob.data.vertices[i].co for i in f.vertices]
   # Front head skin receives a single continuous hand-pixel face, not a face per polygon.
   head=all(any(ob.vertex_groups[g.group].name=='Head' for g in ob.data.vertices[i].groups) for i in f.vertices)
   front=head and t==4 and f.normal.y<-.3 and sum(v.z for v in coords)/len(coords)>1.43*p.S
   if front:
    for li,v in zip(f.loop_indices,coords):
     z=v.z/p.S;u=max(0,min(1,(v.x/p.S+.095)/.19));vv=max(0,min(1,(z-1.43)/.25));uv.data[li].uv=((96+2+u*27)/128,1-(64+2+(1-vv)*27)/128)
   elif t in [0,1,2,3,12,13,14]:
    # Continuous garment coordinates: folds occur once on the garment, never per triangle.
    for li,v in zip(f.loop_indices,coords):
     px=max(1,min(30,(v.x/p.S+.35)*32));py=max(1,min(30,((.95 if t==3 else 1.48)-v.z/p.S)*32));uv.data[li].uv=((t%4*32+px)/128,1-(t//4*32+py)/128)
   else:
    origin=coords[0];u=(coords[1]-origin).normalized();n=f.normal;vaxis=n.cross(u).normalized();ps=[((v-origin).dot(u),(v-origin).dot(vaxis)) for v in coords];lo=[min(v[k] for v in ps) for k in [0,1]];hi=[max(v[k] for v in ps) for k in [0,1]]
    for li,v in zip(f.loop_indices,ps):uv.data[li].uv=((t%4*32+2+(v[0]-lo[0])/max(hi[0]-lo[0],1e-6)*27)/128,1-(t//4*32+2+(v[1]-lo[1])/max(hi[1]-lo[1],1e-6)*27)/128)
  b.TEX=O/'PeoplePS1Atlas_v4_2106.png';ob.data.materials.clear();mat=b.material();mat.name='People_PS1_Point';ob.data.materials.append(mat)
  src=R/f'art/models/bayloop_people_v01/people_{age}_{var+1:02d}.blend'
  with bpy.data.libraries.load(str(src),link=False) as (fr,to):to.actions=[n for n in fr.actions if n in ['Stand','Walk','Sit']]
  rig.animation_data_create();sc=bpy.context.scene;sc.render.fps=12
  originals=list(to.actions)
  for old in originals:
   label=old.name;old.name='SOURCE_'+label;rig.animation_data.action=old
   if old.slots:rig.animation_data.action_slot=old.slots[0]
   start,end=map(int,old.frame_range);count=round((end-start)/30*12);samples=[]
   for i in range(count+1):
    f=start+(end-start)*i/count;sc.frame_set(int(f),subframe=f-int(f));samples.append({pb.name:(pb.location.copy(),pb.rotation_quaternion.copy(),pb.scale.copy()) for pb in rig.pose.bones})
   samples[-1]=samples[0];act=bpy.data.actions.new(label);act.use_fake_user=True;rig.animation_data.action=act
   for i,pose in enumerate(samples,1):
    sc.frame_set(i)
    for pb in rig.pose.bones:pb.rotation_mode='QUATERNION';pb.location,pb.rotation_quaternion,pb.scale=pose[pb.name]
    bpy.context.view_layer.update();ev=ob.evaluated_get(bpy.context.evaluated_depsgraph_get());bottom=min(v.co.z for v in ev.data.vertices);root=rig.pose.bones['Root'];root.location+=root.bone.matrix_local.to_3x3().inverted()@Vector((0,0,-bottom))
    for pb in rig.pose.bones:pb.keyframe_insert('location',frame=i);pb.keyframe_insert('rotation_quaternion',frame=i)
  for old in originals:bpy.data.actions.remove(old)
  rig.animation_data.action=bpy.data.actions['Stand'];sc.frame_set(1)
  bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
  bpy.ops.export_scene.fbx(filepath=str(O/(name+'.fbx')),use_selection=True,object_types={'ARMATURE','MESH'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_ALL',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='COPY',embed_textures=True)
  bpy.ops.wm.save_as_mainfile(filepath=str(O/(name+'.blend')));tris=sum(len(f.vertices)-2 for f in ob.data.polygons);assert 300<=tris<=800,tris
  assets.append(dict(id=name,fbx=b.rel(O/(name+'.fbx')),tris=tris,bones=17,source=b.rel(src),fps=12))
  if '--no-render' not in sys.argv:
   sc=b.stage();sc.cycles.samples=8;sc.world.node_tree.nodes['Background'].inputs[1].default_value=.9
   for view,loc in [('front',(0,-6,1.4)),('threequarter',(3,-6,1.6)),('side',(6,0,1.4))]:b.render(sc,V/(name+'_'+view+'.png'),loc,(0,0,.84),3.7)
   rig.animation_data.action=bpy.data.actions['Walk'];sc.frame_set(2);b.render(sc,V/(name+'_walk.png'),(3,-6,1.6),(0,0,.84),3.7)
   if age=='young' and var==0:
    sc.render.resolution_x=240;sc.render.resolution_y=360
    for frame in range(1,17):sc.frame_set(frame);b.render(sc,V/f'walkproof_{frame:02d}.png',(3,-6,1.6),(0,0,.84),2.2)
  print('DONE',name,tris,flush=True)
(O/'manifest.json').write_text(json.dumps(dict(direction='D-148',texture='PeoplePS1Atlas_v4_2106.png',assets=assets),indent=2),encoding='utf8')
