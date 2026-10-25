from pathlib import Path
import csv,json,hashlib
R=Path(__file__).resolve().parents[3]
rows=json.loads((R/'art/source/car-v01/provenance-rows.json').read_text(encoding='utf-8'))
tris=json.loads((R/'art/models/car_v01/geometry.json').read_text(encoding='utf-8'))['triangles']
for asset,path,src,lic,ai,tool,note in [
 ('car_v01_glb','art/models/car_v01/car_v01.glb','art/models/car_v01/generate.py','Original geometry; CC0 texture; Galmuri OFL-1.1','no','Blender bpy + glTF exporter',f'{tris}tris;64px/m;separate4wheels;measured source bounds'),
 ('car_v01_fbx','art/models/car_v01/car_v01.fbx','art/models/car_v01/generate.py','Original geometry; CC0 texture; Galmuri OFL-1.1','no','Blender bpy + FBX exporter','Same geometry and pivot as GLB; requires Unity import QA'),
 ('car_v01_review_plate','art/_review/2026-10-23-car/coast-plate.png','art/_prompts/20261023-car.md','Review-only composite','yes','built-in image_gen + deterministic masked composite','Original screenshot outside mask. Generated road underneath old car only. Not a game asset'),
]:
 rows.append(dict(asset_id=asset,path=path,source_url=src,license=lic,accessed='2026-10-23',tool=tool,ai_generated=ai,sha256=hashlib.sha256((R/path).read_bytes()).hexdigest(),notes=note))
for row in rows:assert hashlib.sha256((R/row['path']).read_bytes()).hexdigest()==row['sha256'],row['asset_id']
path=R/'art/provenance.csv'
with path.open(encoding='utf-8-sig',newline='') as f:reader=csv.DictReader(f);fields=reader.fieldnames;old=list(reader)
ids={r['asset_id'] for r in rows};kept=[r for r in old if r['asset_id'] not in ids]
with path.open('w',encoding='utf-8',newline='') as f:w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(kept+rows)
print('Registered',len(rows),'car rows; retained',len(kept),'other rows')
