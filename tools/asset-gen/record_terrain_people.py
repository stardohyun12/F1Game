"""Merge task-owned source records; copying is a separate, gate-controlled step."""
from pathlib import Path
import csv,json,hashlib
ROOT=Path(__file__).resolve().parents[2]
ledger=ROOT/'art/provenance.csv'
with ledger.open(encoding='utf-8-sig',newline='') as f:
    reader=csv.DictReader(f);fields=reader.fieldnames;rows=list(reader)
new=[]
for rel in ['art/textures/2026-10-26-bayloop-terrain/provenance-rows.json','art/models/bayloop_people_v01/provenance-rows.json']:
    path=ROOT/rel
    if not path.exists():continue
    for r in json.loads(path.read_text(encoding='utf8')):
        r=dict(r);r['sha256']=hashlib.sha256((ROOT/r['path']).read_bytes()).hexdigest()
        r.setdefault('accessed','2026-10-26');r.setdefault('tool','bayloop_people.py; bayloop_people_retarget.py; Blender bpy')
        r.setdefault('notes',r.get('processing',''))
        if r.get('ai_generated') is False:r['ai_generated']='no'
        new.append({k:r.get(k,'') for k in fields})
for suffix in ['Universal Animation Library[Standard].zip','standard/Universal Animation Library[Standard]/Unity/UAL1_Standard.fbx','cc0_legalcode.txt']:
    rel='art/source/people-animation/'+suffix;path=ROOT/rel
    if not path.is_file():continue
    r=dict(asset_id='people_ual_source_'+path.suffix.strip('.'),path=rel,
        source_url='https://quaternius.itch.io/universal-animation-library; https://creativecommons.org/publicdomain/zero/1.0/legalcode.txt',
        license='CC0-1.0',accessed='2026-10-26',tool='Official Standard download; ZIP extraction; Blender FBX import inspection',
        ai_generated='no',sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
        notes='Original archive/source retained unchanged. Source license in ZIP. Producer declares no generative AI; motion retarget modifications documented separately.')
    new.append({k:r.get(k,'') for k in fields})
paths={r['path'] for r in new};rows=[r for r in rows if r['path'] not in paths]+new
with ledger.open('w',encoding='utf8',newline='') as f:
    writer=csv.DictWriter(f,fieldnames=fields);writer.writeheader();writer.writerows(rows)
print(json.dumps({'recorded_paths':len(paths),'ledger_rows':len(rows)}))
