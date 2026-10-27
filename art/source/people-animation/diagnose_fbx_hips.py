import bpy,pathlib,json
R=pathlib.Path('D:/Github/F1Game');p=R/'art/models/bayloop_people_v01';out=[]
for kind in ['blend','fbx']:
 if kind=='blend':bpy.ops.wm.open_mainfile(filepath=str(p/'people_young_01.blend'))
 else:bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(p/'people_young_01.fbx'))
 rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');mesh=next(o for o in bpy.data.objects if o.type=='MESH' and any(m.type=='ARMATURE' for m in o.modifiers));act=next(a for a in bpy.data.actions if a.name.split('|')[-1]=='Sit');rig.animation_data.action=act;bpy.context.scene.frame_set(1);bpy.context.view_layer.update()
 def sample(label):
  ev=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get());data_min=min((ev.matrix_world@v.co).z for v in ev.data.vertices);tmp=ev.to_mesh();temp_min=min((ev.matrix_world@v.co).z for v in tmp.vertices);ev.to_mesh_clear()
  return {'type':kind,'test':label,'data_min_z':data_min,'to_mesh_min_z':temp_min,'Hips_connected':rig.data.bones['Hips'].use_connect,'Hips_head':list(rig.pose.bones['Hips'].head),'Hips_location':list(rig.pose.bones['Hips'].location),'Root_head':list(rig.pose.bones['Root'].head)}
 out.append(sample('original'))
 if kind=='fbx':
  bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT');rig.data.edit_bones['Hips'].use_connect=False;bpy.ops.object.mode_set(mode='OBJECT');bpy.context.scene.frame_set(2);bpy.context.scene.frame_set(1);bpy.context.view_layer.update();out.append(sample('disconnect_hips_memory_only'))
(R/'art/source/people-animation/fbx-hips-diagnostic.json').write_text(json.dumps(out,indent=2));print(json.dumps(out))
