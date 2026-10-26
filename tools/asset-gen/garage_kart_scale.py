from pathlib import Path
import bpy,json,hashlib
R=Path(__file__).resolve().parents[2];V=R/'art/_review/2026-10-27-garage-kart';items=[]
for path in ['art/models/car_candidates_v01/A.fbx','art/models/garage_kart_v01/delivery_kart_v01.fbx']:
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(R/path));ps=[o.matrix_world@v.co for o in bpy.context.scene.objects if o.type=='MESH' for v in o.data.vertices];lo=[min(p[k] for p in ps) for k in range(3)];hi=[max(p[k] for p in ps) for k in range(3)];items.append(dict(path=path,bounds=[lo,hi],dimensions=[hi[k]-lo[k] for k in range(3)],sha256=hashlib.sha256((R/path).read_bytes()).hexdigest()))
(V/'scale-vs-A.json').write_text(json.dumps(dict(units='metres; actual FBX reimport Blender XYZ',assets=items,length_ratio=items[1]['dimensions'][1]/items[0]['dimensions'][1]),indent=2));print(json.dumps(items))
