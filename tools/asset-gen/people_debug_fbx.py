import bpy,json
from pathlib import Path
O=Path('D:/Github/F1Game/art/models/bayloop_people_v01');out=[]
for typ in ['blend','fbx']:
 if typ=='blend':bpy.ops.wm.open_mainfile(filepath=str(O/'people_young_01.blend'))
 else:bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(O/'people_young_01.fbx'))
 rig=next(o for o in bpy.data.objects if o.type=='ARMATURE')
 for act in bpy.data.actions:
  rig.animation_data.action=act
  if act.slots:rig.animation_data.action_slot=act.slots[0]
  bpy.context.scene.frame_set(1);bpy.context.view_layer.update()
  out.append(dict(type=typ,act=act.name,bones={n:dict(head=list(rig.pose.bones[n].head),loc=list(rig.pose.bones[n].location),scale=list(rig.pose.bones[n].scale))for n in ['Root','Hips','Thigh_L','Foot_L']},matrix=[list(r)for r in rig.matrix_world]))
(O/'debug-fbx.json').write_text(json.dumps(out,indent=2));print(json.dumps(out))
