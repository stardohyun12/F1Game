"""Merge completed type manifests and centrally register their current provenance."""
from pathlib import Path
import json,csv,hashlib
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'art/models/bayloop_variety_v01';REV=ROOT/'art/_review/2026-10-25-bayloop-variety'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
assets=[];m=None
for p in sorted(OUT.glob('manifest-*.json')):
 a=json.loads(p.read_text(encoding='utf8'));m=a;assets+=a['assets']
assert m and len({a['id'] for a in assets})==len(assets)
m['assets']=assets;(OUT/'manifest.json').write_text(json.dumps(m,ensure_ascii=False,indent=2),encoding='utf8')
prov=ROOT/'art/provenance.csv'
with prov.open(encoding='utf-8-sig',newline='') as f:r=csv.DictReader(f);fields=r.fieldnames;rows=list(r)
pending=json.loads((OUT/'texture-provenance-pending.json').read_text(encoding='utf8'))
for a in assets:
 for key in ['model','fbx']:
  p=ROOT/a[key];pending.append(dict(asset_id='d140_'+a['id']+'_'+p.suffix[1:],path=a[key],source_url='; '.join(a['source_urls']),license='CC0-1.0; original bpy geometry; Galmuri OFL-1.1',accessed='2026-10-25',tool='bayloop_buildgen.py; Blender bpy; bayloop_buildgen_textures.py; photo_to_pixel.py',ai_generated='no',sha256=sha(p),notes='D140 seeded metre grammar; 32px/m shared albedo; front wall ground centre +Z front; recipe in manifest; no AI'))
names={p['path'] for p in pending};rows=[r for r in rows if r['path'] not in names]+pending
with prov.open('w',encoding='utf8',newline='') as f:w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows)
print('PREPARED',len(assets),'models, provenance rows',len(pending))
