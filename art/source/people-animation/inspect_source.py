import bpy,json,pathlib
p=pathlib.Path('D:/Github/F1Game/art/source/people-animation')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(p/'standard/Universal Animation Library[Standard]/Unity/UAL1_Standard.fbx'))
r={'blender':bpy.app.version_string,'armatures':[],'actions':[]}
for o in bpy.data.objects:
 if o.type=='ARMATURE':r['armatures'].append({'name':o.name,'bones':[{'name':b.name,'parent':b.parent.name if b.parent else None,'head':list(b.head_local),'tail':list(b.tail_local)} for b in o.data.bones]})
for a in bpy.data.actions:r['actions'].append({'name':a.name,'frame_range':list(a.frame_range),'slots':len(a.slots)})
(p/'blender-source-inspection.json').write_text(json.dumps(r,indent=2))
print('SOURCE_CHECK',len(r['armatures']),len(r['actions']),[(a['name'],a['frame_range']) for a in r['actions'] if any(s in a['name'] for s in ['Walk_Loop','Idle_Loop','Sitting'])])
