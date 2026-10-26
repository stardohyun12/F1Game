from pathlib import Path
import bpy,json,math,sys
R=Path(__file__).resolve().parents[2];O=R/'art/models/garage_kart_v01';V=R/'art/_review/2026-10-27-garage-kart';result=[]
for a in json.loads((O/'manifest.json').read_text())['assets']:
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(O/(a['id']+'.fbx')))
 obs=[o for o in bpy.context.scene.objects if o.type=='MESH'];pts=[o.matrix_world@v.co for o in obs for v in o.data.vertices];tris=sum(len(f.vertices)-2 for o in obs for f in o.data.polygons);lo=[min(v[k] for v in pts) for k in range(3)];hi=[max(v[k] for v in pts) for k in range(3)];err=max(abs(v-q) for p,qrow in zip([lo,hi],a['bounds_blender']) for v,q in zip(p,qrow))
 checks=dict(triangle_budget=tris<=a['budget'],tris_match=tris==a['tris'],bounds_match=err<1e-4,finite=all(math.isfinite(c) for v in pts for c in v),uv_present=all(o.data.uv_layers for o in obs),uv_unit_range=all(-1e-5<=c<=1.00001 for o in obs for u in o.data.uv_layers.active.data for c in u.uv),no_cameras_lights=not any(o.type in ['CAMERA','LIGHT'] for o in bpy.context.scene.objects),ground=abs(lo[2])<1e-4)
 if 'kart' in a['id']:checks['four_separate_wheels']=len([o for o in obs if o.name.startswith('wheel_')])==4
 result.append(dict(id=a['id'],tris=tris,bounds_blender=[lo,hi],bounds_error=err,checks=checks,passed=all(checks.values())))
(V/'technical-check.json').write_text(json.dumps(dict(passed=all(r['passed'] for r in result),assets=result,scope='actual FBX reimport; no Unity import or collision test'),indent=2));print(json.dumps(result));sys.exit(0 if all(r['passed'] for r in result) else 1)
