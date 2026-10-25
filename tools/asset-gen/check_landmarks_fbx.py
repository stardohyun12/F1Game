"""Audit actual reimported FBX geometry, separate from GLB and visual gates."""
from pathlib import Path
import bpy,json,hashlib,sys
R=Path(__file__).resolve().parents[2];V=R/'art/_review/2026-10-26-landmarks';results=[]
for folder in ['bayloop_harbor_landmarks_v01','bayloop_chapel_market_v01','bayloop_teahouse_v01','bayloop_deferred_v02']:
 m=R/'art/models'/folder/'manifest.json'
 for a in json.loads(m.read_text(encoding='utf8'))['assets']:
  bpy.ops.wm.read_factory_settings(use_empty=True);p=R/a['fbx'];bpy.ops.import_scene.fbx(filepath=str(p))
  meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];vs=[];tris=0
  for o in meshes:
   tris+=sum(len(f.vertices)-2 for f in o.data.polygons)
   for v in o.data.vertices:
    q=o.matrix_world@v.co;vs.append((q.x,q.z,-q.y))
  lo=[min(v[k] for v in vs) for k in range(3)];hi=[max(v[k] for v in vs) for k in range(3)]
  error=max(abs(p-q) for p,q in zip(lo+hi,a['bounds_min']+a['bounds_max']))
  checks={'triangles_equal_glb':tris==a['tris'],'bounds_equal_glb_metres':error<1e-4,'meshes_present':bool(meshes),'no_camera_light':not any(o.type in ['CAMERA','LIGHT'] for o in bpy.context.scene.objects)}
  results.append(dict(id=a['id'],folder=folder,tris=tris,bounds_min=lo,bounds_max=hi,max_bounds_error=error,checks=checks,passed=all(checks.values()),sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
result=dict(passed=all(a['passed'] for a in results),assets=results,scope='Actual headless Blender FBX reimport geometry. Unity import/material remap/collider not executed.')
(V/'fbx-check.json').write_text(json.dumps(result,indent=2),encoding='utf8');print(json.dumps(result));sys.exit(0 if result['passed'] else 1)
