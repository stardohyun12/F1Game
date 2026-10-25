"""Actual blend skin, loop endpoints and animated FBX reimport verification."""
from pathlib import Path
import bpy,json,math
from mathutils import Vector
R=Path(__file__).resolve().parents[2];O=R/'art/models/bayloop_people_v01';out=[]
for path in sorted(O.glob('people_*.blend')):
 bpy.ops.wm.open_mainfile(filepath=str(path));rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');ob=next(o for o in bpy.data.objects if o.type=='MESH');sc=bpy.context.scene
 weights=[sum(g.weight for g in v.groups) for v in ob.data.vertices];item=dict(id=path.stem,vertices=len(weights),unweighted=sum(v<.999 or v>1.001 for v in weights),bones=len(rig.data.bones),actions=[])
 for act in list(bpy.data.actions):
  rig.animation_data.action=act;first,last=[round(v) for v in act.frame_range];poses=[];bounds=[]
  for f in [first,(first+last)//2,last]:
   sc.frame_set(f);bpy.context.view_layer.update();eo=ob.evaluated_get(bpy.context.evaluated_depsgraph_get());vs=[eo.matrix_world@v.co for v in eo.data.vertices];poses.append(vs);bounds.append([min(v[k] for v in vs) for k in range(3)])
  endpoint=max((a-c).length for a,c in zip(poses[0],poses[-1]));mid=max((a-c).length for a,c in zip(poses[0],poses[1]));item['actions'].append(dict(name=act.name,first=first,last=last,loop_max_vertex_delta_m=endpoint,mid_motion_max_m=mid,mins=bounds))
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(path.with_suffix('.fbx')));item['fbx_actions']=[dict(name=a.name,frames=list(a.frame_range)) for a in bpy.data.actions];item['fbx_rig_count']=len([o for o in bpy.data.objects if o.type=='ARMATURE']);item['fbx_armature_modifiers']=sum(m.type=='ARMATURE' for o in bpy.data.objects if o.type=='MESH' for m in o.modifiers)
 item['passed']=item['unweighted']==0 and item['fbx_rig_count']==1 and len(item['fbx_actions'])==3 and all(a['loop_max_vertex_delta_m']<1e-5 for a in item['actions']);out.append(item)
(O/'rig-checks.json').write_text(json.dumps(dict(passed=all(i['passed'] for i in out),assets=out),indent=2));print(json.dumps(out))
