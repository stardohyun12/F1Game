"""Reimport delivered FBX files and measure actual skin and baked animation."""
import bpy,json,sys,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
manifest=ROOT/'art/models/bayloop_people_v01/manifest.json'
out=ROOT/'art/_review/2026-10-26-terrain-people/people/fbx-check.json'
results=[]
for item in json.loads(manifest.read_text(encoding='utf8'))['assets']:
    path=ROOT/item['fbx'];bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(path),use_anim=True)
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    rigs=[o for o in bpy.context.scene.objects if o.type=='ARMATURE']
    checks={'one_rig':len(rigs)==1,'meshes_present':bool(meshes)}
    rig=rigs[0];rig.animation_data.action=None;rig.data.pose_position='REST'
    bpy.context.view_layer.update()
    points=[o.matrix_world@v.co for o in meshes for v in o.data.vertices]
    height=max(p.z for p in points)-min(p.z for p in points)
    tris=sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons)
    weight_errors=[]
    for o in meshes:
        valid={g.index for g in o.vertex_groups if g.name in rig.data.bones}
        for v in o.data.vertices:
            total=sum(g.weight for g in v.groups if g.group in valid)
            if abs(total-1)>1e-4:weight_errors.append([o.name,v.index,total])
    checks.update(tri_budget=tris<=1500,height=1.6<=height<=1.7,skin_normalized=not weight_errors,armature_modifier=all(any(m.type=='ARMATURE' and m.object==rig for m in o.modifiers) for o in meshes))
    rig.data.pose_position='POSE';actions=[]
    for action in bpy.data.actions:
        rig.animation_data.action=action
        if hasattr(action,'slots') and action.slots:rig.animation_data.action_slot=action.slots[0]
        first,last=map(int,action.frame_range);samples=[];bottoms=[]
        for f in range(first,last+1):
            bpy.context.scene.frame_set(f);bpy.context.view_layer.update()
            samples.append([list(row) for p in rig.pose.bones for row in p.matrix])
            dep=bpy.context.evaluated_depsgraph_get()
            bottoms.append(min((o.matrix_world@v.co).z for o in meshes for v in o.evaluated_get(dep).data.vertices))
        delta=lambda a,b:max(abs(x-y) for ra,rb in zip(a,b) for x,y in zip(ra,rb))
        closure=delta(samples[0],samples[-1]);motion=max(delta(samples[0],s) for s in samples)
        fps=bpy.context.scene.render.fps/bpy.context.scene.render.fps_base
        actions.append(dict(name=action.name,frames=[first,last],fps=fps,duration_s=(last-first)/fps,loop_matrix_error=closure,motion_matrix_delta=motion,ground_min=min(bottoms),ground_max=max(bottoms)))
    for name in ['Stand','Walk','Sit']:
        matches=[a for a in actions if a['name'].endswith(name)]
        checks[name+'_baked_loop']=len(matches)==1 and matches[0]['frames'][1]>matches[0]['frames'][0] and matches[0]['loop_matrix_error']<1e-4 and matches[0]['motion_matrix_delta']>1e-4
    checks['ground_penetration_absent']=all(a['ground_min']>=-.02 for a in actions)
    checks['ground_contact_every_frame']=all(max(abs(a['ground_min']),abs(a['ground_max']))<=.02 for a in actions)
    results.append(dict(id=item['id'],sha256=hashlib.sha256(path.read_bytes()).hexdigest(),height_m=height,tris=tris,bones=len(rig.data.bones),actions=actions,weight_errors=weight_errors[:5],checks=checks,passed=all(checks.values())))
result=dict(passed=all(r['passed'] for r in results),assets=results,scope='Actual FBX reimport: rest bounds, triangles, weights, bone matrices all baked frames, loop closure and floor penetration. Visual gait/seat contacts require independent rendered review; Unity import not run.')
out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(result,indent=2),encoding='utf8')
print(json.dumps(result,indent=2));sys.exit(0 if result['passed'] else 1)
