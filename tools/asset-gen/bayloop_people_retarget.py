"""Quaternius CC0 UAL retarget: independent trial; does not mutate original assets."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Matrix,Vector
ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'art/source/people-animation/standard/Universal Animation Library[Standard]/Unity/UAL1_Standard.fbx'
MAP={'Hips':'pelvis','Spine':'spine_01','Chest':'spine_03','Head':'Head'}
for dst,src in [('L','r'),('R','l')]:
 for a,b in [('Thigh','thigh'),('Shin','calf'),('Foot','foot'),('UpperArm','upperarm'),('Forearm','lowerarm'),('Hand','hand')]:MAP[a+'_'+dst]=b+'_'+src

def retarget(target,mesh):
 before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(SOURCE));added=set(bpy.data.objects)-before
 source=next(o for o in added if o.type=='ARMATURE');source.animation_data_create();target.animation_data_create()
 for tr in list(target.animation_data.nla_tracks):target.animation_data.nla_tracks.remove(tr)
 sc=bpy.context.scene;sc.render.fps=30
 corrections={}
 for tn,sn in MAP.items():
  tb=target.data.bones[tn];sb=source.data.bones[sn]
  td=(tb.tail_local-tb.head_local).normalized();sd=(sb.tail_local-sb.head_local).normalized()
  virtual=td.rotation_difference(sd)@tb.matrix_local.to_quaternion()
  corrections[tn]=sb.matrix_local.to_quaternion().inverted()@virtual
 scale=target.data.bones['Hips'].head_local.z/source.data.bones['pelvis'].head_local.z
 report=[]
 for label,clip in [('Walk','Walk_Loop'),('Stand','Idle_Loop'),('Sit','Sitting_Idle_Loop')]:
  sa=next(a for a in bpy.data.actions if a.name.endswith('|'+clip));source.animation_data.action=sa
  act=bpy.data.actions.new('UAL_'+label);act.use_fake_user=True;target.animation_data.action=act
  start,end=map(int,sa.frame_range);samples=[]
  for f in range(start,end+1):
   sc.frame_set(f)
   for pb in target.pose.bones:pb.rotation_mode='QUATERNION';pb.matrix_basis=Matrix.Identity(4)
   bpy.context.view_layer.update()
   for pb in target.pose.bones:
    if pb.name not in MAP:continue
    sp=source.pose.bones[MAP[pb.name]]
    q=sp.matrix.to_quaternion()@corrections[pb.name]
    mat=q.to_matrix().to_4x4();mat.translation=pb.matrix.translation
    if pb.name=='Hips':
     delta=(sp.matrix.translation-sp.bone.head_local)*scale
     mat.translation=pb.bone.head_local+Vector((0,0,delta.z))
    pb.matrix=mat;bpy.context.view_layer.update()
   ev=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get());pts=[ev.matrix_world@v.co for v in ev.data.vertices];bottom=min(v.z for v in pts)
   # Preserve authored seated height while lifting any below-ground soles.
   correction=-bottom if label!='Sit' else max(0,-bottom)
   root=target.pose.bones['Root'];root.location+=root.bone.matrix_local.to_3x3().inverted()@Vector((0,0,correction));bpy.context.view_layer.update()
   for pb in target.pose.bones:
    pb.keyframe_insert('rotation_quaternion',frame=f,group=pb.name);pb.keyframe_insert('location',frame=f,group=pb.name)
   if f in {start,(start+end)//2,end}:
    ev=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get());pts=[ev.matrix_world@v.co for v in ev.data.vertices]
    samples.append({'frame':f,'min_z':min(v.z for v in pts),'max_z':max(v.z for v in pts),'finite':all(math.isfinite(c) for v in pts for c in v),'hips_z':target.pose.bones['Hips'].head.z,'feet':[list(target.pose.bones['Foot_'+s].head) for s in ['L','R']]})
  report.append({'name':act.name,'source':sa.name,'frames':[start,end],'fps':30,'samples':samples})
 for ob in added:bpy.data.objects.remove(ob,do_unlink=True)
 target.animation_data.action=bpy.data.actions['UAL_Stand'];sc.frame_set(1)
 return report

def cleanup_actions(target, rename=True):
 """Call only in standalone resident build after retarget, before all-actions FBX export."""
 keep=[bpy.data.actions['UAL_'+n] for n in ['Walk','Stand','Sit']]
 target.animation_data.action=keep[1]
 for a in list(bpy.data.actions):
  if a not in keep:bpy.data.actions.remove(a)
 if rename:
  for a in keep:a.name=a.name.removeprefix('UAL_')
 return [a.name for a in keep]

if __name__=='__main__':
 bpy.ops.wm.open_mainfile(filepath=str(ROOT/'art/models/bayloop_people_v01/people_young_01.blend'))
 rig=bpy.data.objects['ResidentRig'];mesh=next(o for o in bpy.data.objects if o.type=='MESH' and any(m.type=='ARMATURE' and m.object==rig for m in o.modifiers))
 r=retarget(rig,mesh);out=ROOT/'art/source/people-animation';(out/'retarget-trial.json').write_text(json.dumps(r,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(out/'retarget-trial.blend'));print('RETARGET_TRIAL',json.dumps(r))
