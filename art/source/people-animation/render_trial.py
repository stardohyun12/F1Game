import bpy,sys,pathlib
r=pathlib.Path('D:/Github/F1Game');sys.path.insert(0,str(r/'tools/asset-gen'));import bayloop_buildings as b
bpy.ops.wm.open_mainfile(filepath=str(r/'art/source/people-animation/retarget-trial.blend'));rig=bpy.data.objects['ResidentRig'];sc=b.stage();sc.cycles.samples=12
for label in ['Stand','Walk','Sit']:
 rig.animation_data.action=bpy.data.actions['UAL_'+label];sc.frame_set(1);b.render(sc,r/f'art/source/people-animation/trial-{label}.png',(3,-5,1.8),(0,0,.84),3.7)
