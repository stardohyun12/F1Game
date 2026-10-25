"""Measured adult inhabitants, photo atlas and baked Generic armature actions."""
from pathlib import Path
import sys,math,json,argparse
import bpy,bmesh
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bayloop_buildings as b
R=b.ROOT;O=R/'art/models/bayloop_people_v01';REV=R/'art/_review/2026-10-26-terrain-people/people';REV.mkdir(parents=True,exist_ok=True)
S=1.;ELDER=False;WEIGHTS={}
def age_shape(p,scaled=True):
 x,y,z=p
 if scaled:x/=S;y/=S;z/=S
 if ELDER:
  # Mild thoracic stoop, narrower shoulder and forward head, not a caricature.
  t=max(0,min(1,(z-1.03)/.30));x*=1-.08*t
  if z>1.03:
   dz=z-1.03;ang=math.radians(7);y,z=y*math.cos(ang)-dz*math.sin(ang),1.03+y*math.sin(ang)+dz*math.cos(ang)
  if z>1.44:y-=.02*(z-1.44)/.24
 return (x*S,y*S,z*S)
def face(ps,tile):
 ps=[age_shape(p) for p in ps]
 offset=(45+int(sum(p[0] for p in ps)/len(ps)*32),29+int(sum(p[2] for p in ps)/len(ps)*32)) if tile=='trouser' else (23,29)
 for i in range(1,len(ps)-1):b.face([ps[0],ps[i],ps[i+1]],tile,offset=offset)
def rings(name,rows,tile,n=8):
 b.PART=name;pts=[]
 for x,y,z,rx,ry in rows:pts.append([(S*(x+rx*math.cos(2*math.pi*j/n+math.pi/8)),S*(y+ry*math.sin(2*math.pi*j/n+math.pi/8)),S*z) for j in range(n)])
 face(list(reversed(pts[0])),tile);face(pts[-1],tile)
 for p,q in zip(pts,pts[1:]):
  for j in range(n):face([p[j],p[(j+1)%n],q[(j+1)%n],q[j]],tile)
def cuboid(name,c,sz,tile):
 b.PART=name;x,y,z=c;w,d,h=sz;ps=[(S*(x+i*w/2),S*(y+j*d/2),S*(z+k*h/2)) for k in [-1,1] for j in [-1,1] for i in [-1,1]]
 for ids in [(0,1,3,2),(4,6,7,5),(0,4,5,1),(2,3,7,6),(0,2,6,4),(1,5,7,3)]:face([ps[i] for i in ids],tile)
def build(age,variant):
 global S,ELDER
 S=1 if age=='young' else 1.62/1.68
 ELDER=age=='elder'
 b.V.clear();b.F.clear();b.UV.clear();b.PARTS.clear();b.TILES.update(coat=variant,trouser=3,skin=4,hair=5 if age=='young' else 6,shoe=7,shirt=8,lip=9,eye=10)
 # Adult landmarks: sole0, ankle.10, knee.48, pelvis.88, shoulder1.38, chin1.45, crown1.68.
 wide=1 if age=='young' else 1.13
 rings('torso',[(0,0,.82,.16*wide,.10),(0,-.01 if ELDER else 0,.91,.17*wide,.128 if ELDER else .115),(0,-.018 if ELDER else 0,1.06,.17*wide,.137 if ELDER else .115),(0,0,1.23,.185,.12),(0,0,1.35,.205,.095),(0,0,1.40,.13,.08)],'coat')
 rings('pelvis',[(0,0,.80,.14,.10),(0,0,.90,.155*wide,.115)],'trouser')
 rings('neck',[(0,0,1.38,.055,.052),(0,0,1.48,.055,.052)],'skin')
 rings('head',[(0,-.008,1.445,.052,.055),(0,-.012,1.48,.083,.08),(0,0,1.56,.093,.085),(0,.006,1.63,.084,.08),(0,.01,1.665,.055,.052)],'skin')
 rings('hair',[(0,.016,1.59,.09,.079),(0,.012,1.65,.083,.073),(0,.012,1.68,.035,.038)],'hair')
 # Raised nose and ears, small eyes, mouth. All geometry uses same 32px/m, no resolution exception.
 cuboid('nose',(0,-.093,1.545),(.033,.036,.045),'skin')
 for side in [-1,1]:
  rings('ear',[(side*.095,.003,1.51,.013,.02),(side*.095,.003,1.56,.014,.02)],'skin',6)
  cuboid('eye',(side*.034,-.081,1.574),(.019,.007,.012),'eye')
 cuboid('mouth',(0,-.082,1.495),(.040,.006,.009),'lip')
 for side,suffix in [(-1,'L'),(1,'R')]:
  x=side*.100
  rings('leg_'+suffix,[(x,0,.88,.085,.095),(x,-.005,.68,.076,.080),(x,-.008,.49,.062,.068),(x,0,.31,.063,.066),(x,.008,.10,.050,.052)],'trouser',8)
  rings('shoe_'+suffix,[(x,-.055,0,.064,.132),(x,-.055,.075,.065,.13),(x,-.025,.12,.046,.067)],'shoe',8)
  rings('arm_'+suffix,[(side*.16,0,1.36,.055,.060),(side*.20,0,1.28,.055,.058),(side*.23,0,1.20,.057,.059),(side*.26,-.015,1.08,.047,.05),(side*.285,-.028,.95,.046,.049),(side*.295,-.035,.86,.036,.036)],'coat',8)
  rings('hand_'+suffix,[(side*.297,-.038,.87,.036,.033),(side*.30,-.042,.79,.042,.032),(side*.30,-.046,.745,.028,.024)],'skin',6)
  # Thumb is small but improves hand reading.
  rings('thumb_'+suffix,[(side*.27,-.053,.84,.015,.017),(side*.27,-.067,.79,.012,.012)],'skin',6)
 # Shirt wedge/collar and front placket, restrained physical construction rather than random decals.
 b.PART='torso';face([(-.05*S,-.079*S,1.392*S),(0,-.116*S,1.25*S),(.05*S,-.079*S,1.392*S)],'shirt')
 cuboid('torso',(0,-.117,1.12),(.015,.007,.22),'coat')
 me=bpy.data.meshes.new('adult_mesh');me.from_pydata(b.V,[],b.F);me.update();uv=me.uv_layers.new(name='UV0')
 for poly,uvs in zip(me.polygons,b.UV):
  for li,co in zip(poly.loop_indices,uvs):uv.data[li].uv=co
 for poly,part in zip(me.polygons,b.PARTS):
  if part in ['head','neck','ear','nose','torso','pelvis'] or part.startswith(('arm_','leg_','hand_','shoe_')):poly.use_smooth=True
 ob=bpy.data.objects.new('Resident',me);bpy.context.collection.objects.link(ob);b.TEX=O/'PeoplePhotoAtlas.png';mat=b.material();mat.name='MAT_Resident_Photo32';me.materials.append(mat)
 ad=bpy.data.armatures.new('Resident_Generic');rig=bpy.data.objects.new('ResidentRig',ad);bpy.context.collection.objects.link(rig);bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
 bones={}
 def bone(n,h,t,p=None):
  q=ad.edit_bones.new(n);q.head=age_shape(h,False);q.tail=age_shape(t,False)
  if p:q.parent=ad.edit_bones[p]
  bones[n]=(h,t)
 bone('Root',(0,0,0),(0,0,.1));bone('Hips',(0,0,.88),(0,0,1.02),'Root');bone('Spine',(0,0,1.02),(0,0,1.23),'Hips');bone('Chest',(0,0,1.23),(0,0,1.40),'Spine');bone('Head',(0,0,1.40),(0,0,1.66),'Chest')
 for side,suf in [(-1,'L'),(1,'R')]:
  x=side*.100;bone('Thigh_'+suf,(x,0,.88),(x,-.008,.49),'Hips');bone('Shin_'+suf,(x,-.008,.49),(x,.008,.10),'Thigh_'+suf);bone('Foot_'+suf,(x,.008,.10),(x,-.17,.06),'Shin_'+suf)
  bone('UpperArm_'+suf,(side*.188,0,1.355),(side*.26,-.015,1.08),'Chest');bone('Forearm_'+suf,(side*.26,-.015,1.08),(side*.295,-.035,.86),'UpperArm_'+suf);bone('Hand_'+suf,(side*.295,-.035,.86),(side*.30,-.046,.765),'Forearm_'+suf)
 bpy.ops.object.mode_set(mode='OBJECT')
 groups={n:ob.vertex_groups.new(name=n) for n in bones}
 def blend(z,joint,lo,hi):
  t=max(0,min(1,(z-joint+.045)/.09));return {lo:1-t,hi:t}
 for f,part in zip(me.polygons,b.PARTS):
  for vi in f.vertices:
   z=me.vertices[vi].co.z/S
   if part=='torso':weights=blend(z,1.22,'Spine','Chest') if z>1.12 else blend(z,1.02,'Hips','Spine')
   elif part=='pelvis':weights={'Hips':1}
   elif part.startswith('leg_'):suf=part[-1];weights=blend(z,.49,'Shin_'+suf,'Thigh_'+suf)
   elif part.startswith('shoe_'):weights={'Foot_'+part[-1]:1}
   elif part.startswith('arm_'):suf=part[-1];weights=blend(z,1.08,'Forearm_'+suf,'UpperArm_'+suf)
   elif part.startswith(('hand_','thumb_')):weights={'Hand_'+part[-1]:1}
   else:weights={'Head':1}
   for name,w in weights.items():
    if w>0:groups[name].add([vi],w,'REPLACE')
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
 mod=ob.modifiers.new('Actual_skin','ARMATURE');mod.object=rig;ob.parent=rig
 return ob,rig
def actions(ob,rig):
 rig.animation_data_create();out=[];sc=bpy.context.scene;sc.render.fps=24
 for name,end in [('Stand',49),('Walk',25),('Sit',49)]:
  act=bpy.data.actions.new(name);act.use_fake_user=True;rig.animation_data.action=act
  for f in range(1,end+1):
   t=2*math.pi*(f-1)/(end-1)
   for p in rig.pose.bones:p.rotation_mode='XYZ';p.rotation_euler=(0,0,0);p.location=(0,0,0)
   if name=='Walk':
    for sign,suf in [(1,'L'),(-1,'R')]:
     phase=t+(0 if sign==1 else math.pi)
     rig.pose.bones['Thigh_'+suf].rotation_euler.x=.34*math.sin(phase)
     rig.pose.bones['Shin_'+suf].rotation_euler.x=-.40*max(0,math.sin(phase))
     rig.pose.bones['UpperArm_'+suf].rotation_euler.x=-.25*math.sin(phase)
     rig.pose.bones['Forearm_'+suf].rotation_euler.x=.08
   elif name=='Sit':
    rig.pose.bones['Root'].location.z=-.39*S
    for suf in ['L','R']:
     rig.pose.bones['Thigh_'+suf].rotation_euler.x=-math.pi/2
     rig.pose.bones['Shin_'+suf].rotation_euler.x=math.pi/2
     rig.pose.bones['UpperArm_'+suf].rotation_euler.x=-.20
     rig.pose.bones['Forearm_'+suf].rotation_euler.x=-.8
    rig.pose.bones['Chest'].rotation_euler.x=.025*math.sin(t)
   else:rig.pose.bones['Chest'].rotation_euler.x=.012*math.sin(t)
   bpy.context.view_layer.update()
   if name!='Sit':
    eo=ob.evaluated_get(bpy.context.evaluated_depsgraph_get());bottom=min((eo.matrix_world@v.co).z for v in eo.data.vertices);rig.pose.bones['Root'].location.z-=bottom
   for p in rig.pose.bones:
    p.keyframe_insert('rotation_euler',frame=f,group=p.name);p.keyframe_insert('location',frame=f,group=p.name)
  out.append(dict(name=name,first=1,last=end,fps=24,loop=True))
 return out
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--no-render',action='store_true');ap.add_argument('--ages',nargs='+',default=['young','elder']);args=ap.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
 assets=[]
 for age in args.ages:
  for variant in range(3):
   name=f'people_{age}_{variant+1:02d}';bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.scene.unit_settings.system='METRIC';ob,rig=build(age,variant)
   from bayloop_people_retarget import retarget,cleanup_actions
   retarget_evidence=retarget(rig,ob);cleanup_actions(rig,rename=True)
   # FBX infers connected pelvis, which discards child translation; bake it into Root.
   for act in bpy.data.actions:
    rig.animation_data.action=act;first,last=map(int,act.frame_range)
    for frame in range(first,last+1):
     bpy.context.scene.frame_set(frame);root=rig.pose.bones['Root'];hips=rig.pose.bones['Hips'];root.location+=hips.location;hips.location=(0,0,0)
     root.keyframe_insert('location',frame=frame);hips.keyframe_insert('location',frame=frame)
   # Original idle endpoints differ by ~4mm at the head; exact loop closure on baked endpoint.
   for act in bpy.data.actions:
    rig.animation_data.action=act;first,last=map(int,act.frame_range);bpy.context.scene.frame_set(first)
    first_pose={p.name:(p.location.copy(),p.rotation_quaternion.copy(),p.scale.copy()) for p in rig.pose.bones}
    bpy.context.scene.frame_set(last)
    for p in rig.pose.bones:
     p.location,p.rotation_quaternion,p.scale=first_pose[p.name];p.keyframe_insert('location',frame=last);p.keyframe_insert('rotation_quaternion',frame=last)
   acts=[dict(name=a.name,first=int(a.frame_range[0]),last=int(a.frame_range[1]),fps=30,loop=True,source='Quaternius UAL Standard CC0') for a in bpy.data.actions]
   rig.animation_data.action=bpy.data.actions['Stand'];bpy.context.scene.frame_set(1)
   bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
   fbx=O/(name+'.fbx');glb=O/(name+'.glb')
   bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'ARMATURE','MESH'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_ALL',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='COPY',embed_textures=True)
   bpy.ops.export_scene.gltf(filepath=str(glb),export_format='GLB',use_selection=True,export_yup=True,export_animations=False)
   bpy.ops.wm.save_as_mainfile(filepath=str(O/(name+'.blend')))
   tris=sum(len(p.vertices)-2 for p in ob.data.polygons);assert tris<=1500,tris
   verts=[tuple(v.co) for v in ob.data.vertices];lo=[min(v[k] for v in verts) for k in range(3)];hi=[max(v[k] for v in verts) for k in range(3)]
   info=dict(id=name,type='resident',asset_kind='person',max_tris=1500,model=b.rel(glb),fbx=b.rel(fbx),texture=b.rel(O/'PeoplePhotoAtlas.png'),tris=tris,bounds_min=[lo[0],lo[2],-hi[1]],bounds_max=[hi[0],hi[2],-lo[1]],dimensions_m=[hi[0]-lo[0],hi[2]-lo[2],hi[1]-lo[1]],source_urls=['https://polyhaven.com/a/rough_linen'],parameters=dict(age=age,clothing=variant+1),rig_bones=len(rig.data.bones),actions=acts,pivot_kind='floor_center',density_px_m=32)
   info['retarget_evidence']=retarget_evidence;assets.append(info)
   if not args.no_render:
    sc=b.stage();sc.cycles.samples=12;bpy.data.objects['REVIEW_ONLY_ground'].location.z=0
    b.render(sc,REV/(name+'_front.png'),(0,-5,1.2),(0,0,.84),3.7)
    if variant==0:
     b.render(sc,REV/(name+'_side.png'),(5,0,1.2),(0,0,.84),3.7)
     for action,frames in [('Walk',[1,11,21]),('Stand',[1,19,38]),('Sit',[1,13,26])]:
      rig.animation_data.action=bpy.data.actions[action]
      for frame in frames:
       sc.frame_set(frame);b.render(sc,REV/(name+'_'+action.lower()+f'_{frame:02d}.png'),(3,-5,1.8),(0,0,.8),3.7)
   print('PERSON_DONE',name,tris,flush=True)
 (O/'manifest.json').write_text(json.dumps(dict(palette='art/source/palette/pitcrew_d123_32.json',density_px_m=32,max_tris=1500,source_manifests=['art/models/bayloop_props2_v01/linen-source.json'],assets=assets),ensure_ascii=False,indent=2),encoding='utf8')
 rows=[]
 for a in assets:
  for suffix in ['fbx','glb','blend']:
   path=O/(a['id']+'.'+suffix);rows.append(dict(asset_id=a['id']+'_'+suffix,path=b.rel(path),source_url='https://polyhaven.com/a/rough_linen; https://quaternius.com/packs/universalanimationlibrary.html',license='CC0 photograph and UAL Standard animations; original bpy mesh',processing='photo BOX9px per .271m; shared-palette ladders; metric32px/m UV; adult mesh+17-bone weighted rig; UAL rest-basis retarget+3 baked actions',ai_generated=False,sha256=b.sha(path)))
 rows.append(dict(asset_id='people_photo_atlas',path=b.rel(O/'PeoplePhotoAtlas.png'),source_url='https://polyhaven.com/a/rough_linen',license='CC0 photograph',processing='luminance BOX9px per .271m; shared-palette ladders; Point',ai_generated=False,sha256=b.sha(O/'PeoplePhotoAtlas.png')))
 (O/'provenance-rows.json').write_text(json.dumps(rows,indent=2),encoding='utf8')
if __name__=='__main__':main()
