from pathlib import Path
import csv,json,hashlib
r=Path('.'); o=r/'art/models/car_candidates_v01'; p=r/'art/provenance.csv'
with p.open(encoding='utf-8-sig',newline='') as f:
 rd=csv.DictReader(f);fields=rd.fieldnames; rows=list(rd)
new=json.loads((o/'texture-provenance-rows.json').read_text(encoding='utf8'))
for v in ['A','C']:
 for ext in ['glb','fbx']:
  a=o/f'{v}.{ext}'
  if a.exists():
   new.append(dict(asset_id=f'car_candidate_{v}_{ext}',path=a.as_posix(),source_url='https://polyhaven.com/a/rusty_metal_02; https://polyhaven.com/a/rubberized_track; https://polyhaven.com/a/rough_linen',license='CC0-1.0 photo; original bpy geometry',accessed='2026-10-25',tool='car_candidates.py; car_candidates_geometry.py; Blender bpy',ai_generated='no',sha256=hashlib.sha256(a.read_bytes()).hexdigest(),notes='D142 32px/m; wheel and pivot contract preserved; no physics edits'))
for row in new:rows=[x for x in rows if x['path']!=row['path']]+[row]
with p.open('w',encoding='utf8',newline='') as f:
 w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows)
print('registered',len(new))
