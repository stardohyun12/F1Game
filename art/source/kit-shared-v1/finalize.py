from pathlib import Path
import csv,json,hashlib
import numpy as np
from PIL import Image
R=Path(__file__).resolve().parents[3];W=Path(__file__).parent;E=R/'art/export/kit-shared-v1';V=R/'art/_review/2026-10-28-kit-shared-v1'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
validation=json.loads((E/'validation.json').read_text());proof=json.loads((W/'proof-validation.json').read_text())
assert np.allclose(proof['house_dimensions_m'],[6,5,7]) and proof['windows']==4 and proof['doors']==1
assert set(proof['material_slots'])=={'wall','trim','floor','roof','decals'}
edges=[e for o in proof['texel_density_edges'] for e in o['texel_per_m']]
assert np.allclose(edges,32,atol=.001)
for name in ['wall','trim','floor','roof','decals']:assert validation[name]['sha256']==sha(E/(name+'.png'))
proof_summary={'bounds':proof['bounds'],'dimensions':proof['house_dimensions_m'],'tris':proof['tris'],'material_slots':proof['material_slots'],'texel_edge_min':min(edges),'texel_edge_max':max(edges),'windows':4,'doors':1}
(E/'proof-validation.json').write_text(json.dumps(proof_summary,indent=2))
outputs={p.name:sha(p) for p in E.iterdir() if p.suffix in ['.png','.gpl','.json','.md','.csv'] and p.name!='output-hashes.json'}
(E/'output-hashes.json').write_text(json.dumps(outputs,indent=2))
# Shared provenance register: read latest immediately before writing, preserve others' rows.
reg=R/'art/provenance.csv'
with reg.open(encoding='utf-8-sig',newline='') as f:reader=csv.DictReader(f);fields=reader.fieldnames;rows=list(reader)
new=[]
for name in ['wall','trim','floor','roof','decals']:
 p=E/(name+'.png');new.append(dict(asset_id='kit_shared_v1_'+name,path=p.relative_to(R).as_posix(),source_url='art/export/kit-shared-v1/provenance.csv',license='CC0-1.0 source photos; original layout and lettering',accessed='2026-10-28',tool='delight.py 1.0.0 / Pillow / Blender proof; deterministic non-AI',ai_generated='false',sha256=sha(p),notes='Shared 512px albedo atlas; sources and crops in material-records.json and jobs.json; approval pending'))
ids={v['asset_id'] for v in new}
with reg.open('w',encoding='utf-8',newline='') as f:writer=csv.DictWriter(f,fieldnames=fields);writer.writeheader();writer.writerows([r for r in rows if r['asset_id'] not in ids]+new)
print('DELIVERY CHECK PASS',proof_summary,flush=True)
