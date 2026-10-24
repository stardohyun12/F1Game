"""Merge this delivery's rows; preserve all other teams' current CSV rows."""
from pathlib import Path
import csv,json,hashlib
R=Path(__file__).resolve().parents[3];S=R/'art/source/hillhouse-surfaces'
rows=json.loads((S/'provenance-rows.json').read_text(encoding='utf-8'))
tris=json.loads((R/'art/models/hill_house_v01/geometry.json').read_text(encoding='utf-8'))['tris']
urls='; '.join(sorted(set(r['source_url'] for r in rows)))
for asset,path,source,lic,ai,tool,note in [
 ('hill_house_v01_atlas','art/models/hill_house_v01/HillHouseAtlas.png',urls,'CC0-1.0; Galmuri OFL-1.1','no','hill_surfaces.py; photo_to_pixel.py; generate.py','Copy of extension atlas; cells51-58; photo pixels and scripted information layer only.'),
 ('hill_house_v01','art/models/hill_house_v01/hill_house_v01.glb','art/models/hill_house_v01/generate.py','Original geometry; CC0-1.0 texture','no','Blender bpy; glTF exporter',f'{tris} tris; 32px/m; metres; no generated-image texture pixels.'),
 ('hill_house_v01_concept','art/_review/2026-10-23-hillhouse/concept.png','art/_prompts/20261023-hillhouse.md','Review-only generated concept','yes','built-in image_gen','Not an import asset. Review-only composition reference.'),
 ('hill_house_v01_empty_plate','art/_review/2026-10-23-hillhouse/concept-empty.png','art/_prompts/20261023-hillhouse.md','Review-only generated concept','yes','built-in image_gen','Not an import asset. Review-only montage background.'),
]:
 rows.append(dict(asset_id=asset,path=path,source_url=source,license=lic,accessed='2026-10-23',tool=tool,ai_generated=ai,sha256=hashlib.sha256((R/path).read_bytes()).hexdigest(),notes=note))
for row in rows:
 assert hashlib.sha256((R/row['path']).read_bytes()).hexdigest()==row['sha256'],row['asset_id']
path=R/'art/provenance.csv'
with path.open(encoding='utf-8-sig',newline='') as f:
 reader=csv.DictReader(f);fields=reader.fieldnames;old=list(reader)
ids={r['asset_id'] for r in rows};old=[r for r in old if r['asset_id'] not in ids]
with path.open('w',encoding='utf-8',newline='') as f:
 w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(old+rows)
print('Registered',len(rows),'hillhouse rows; retained',len(old),'other rows')
