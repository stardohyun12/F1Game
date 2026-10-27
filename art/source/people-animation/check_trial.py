import bpy,json,pathlib,sys
p=pathlib.Path('D:/Github/F1Game/art/source/people-animation');bpy.ops.wm.open_mainfile(filepath=str(p/'retarget-trial.blend'));rig=bpy.data.objects['ResidentRig'];mesh=next(o for o in bpy.data.objects if o.type=='MESH' and any(m.type=='ARMATURE' and m.object==rig for m in o.modifiers));r=[]
for name in ['UAL_Walk','UAL_Stand','UAL_Sit']:
 rig.animation_data.action=bpy.data.actions[name];a=rig.animation_data.action;fs=[int(a.frame_range[0]),int(sum(a.frame_range)/2),int(a.frame_range[1])];samples=[]
 for f in fs:
  bpy.context.scene.frame_set(f);ev=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get());v=[ev.matrix_world@x.co for x in ev.data.vertices];samples.append({'frame':f,'min_z':min(x.z for x in v),'max_z':max(x.z for x in v)})
 r.append({'action':name,'samples':samples})
(p/'retarget-reload-check.json').write_text(json.dumps(r,indent=2));print(json.dumps(r))
