from pathlib import Path
import json,hashlib,csv
ROOT=Path(__file__).resolve().parents[2];O=ROOT/'art/models/bskills_trial_v01';R=ROOT/'art/_review/2026-10-25-bskills-trial'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
ms=[json.loads(p.read_text(encoding='utf8')) for p in O.glob('manifest-*.json')];assert ms
assets={a['id']:a for m in ms for a in m['assets']};m=dict(ms[0],assets=sorted(assets.values(),key=lambda a:a['id']));(O/'manifest.json').write_text(json.dumps(m,ensure_ascii=False,indent=2),encoding='utf8')
prov=ROOT/'art/provenance.csv'
with prov.open(encoding='utf-8-sig',newline='') as f:reader=csv.DictReader(f);fields=reader.fieldnames;rows=list(reader)
for path in {a[k] for a in m['assets'] for k in ['model','fbx','texture']}:
 urls=sorted({u for a in m['assets'] if path in [a['model'],a['fbx'],a['texture']] for u in a['source_urls']});p=ROOT/path
 row=dict(asset_id='bskills_trial_'+p.stem+'_'+p.suffix[1:],path=path,source_url='; '.join(urls),license='CC0-1.0; original bpy geometry; workflow guidance MIT',accessed='2026-10-25',tool='bskills_trial.py; bskills_trial_texture.py; float photo BOX at32px/m; fixed32 palette; headless Blender',ai_generated='no',sha256=sha(p),notes='Production briefs precede execution; physical shape, shared photograph palette, separate systems. Metal protected crop450,400,640,650 of2.2m photo; linen0.271m to9px. No AI/no procedural noise. Skill pack SHA8f778d2405a214b508d4c7d80742be8e43acdd52; MIT notice in source folder.')
 rows=[r for r in rows if r['path']!=path]+[row]
with prov.open('w',encoding='utf8',newline='') as f:w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows)
print('PREPARED',len(m['assets']))
