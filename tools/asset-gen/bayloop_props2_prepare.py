from pathlib import Path
import json,csv,hashlib
ROOT=Path(__file__).resolve().parents[2];O=ROOT/'art/models/bayloop_props2_v01'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
manifests=[json.loads(p.read_text(encoding='utf8')) for p in O.glob('manifest-*.json')];assert manifests
m=manifests[0].copy();m['source_manifests']=sorted({p for x in manifests for p in x['source_manifests']});m['assets']=sorted([a for x in manifests for a in x['assets']],key=lambda a:a['id']);assert len(m['assets'])==len({a['id'] for a in m['assets']})
(O/'manifest.json').write_text(json.dumps(m,ensure_ascii=False,indent=2),encoding='utf8')
prov=ROOT/'art/provenance.csv'
with prov.open(encoding='utf-8-sig',newline='') as f:r=csv.DictReader(f);fields=r.fieldnames;rows=list(r)
urls=sorted({u for a in m['assets'] for u in a['source_urls']});paths={a[k] for a in m['assets'] for k in ['model','fbx','texture']}
for path in paths:
 p=ROOT/path;row=dict(asset_id='d141_'+p.stem+'_'+p.suffix[1:],path=path,source_url='; '.join(urls),license='CC0-1.0; original bpy geometry',accessed='2026-10-25',tool='bayloop_props2.py; bayloop_props2_textures.py; inherited photo_to_pixel exact-palette pipeline',ai_generated='no',sha256=sha(p),notes='32px/m, photograph-based surfaces, common palette, actual metric geometry; floor-centre pivot; no AI. Symbols geometry only; unbranded parked vehicles.')
 rows=[r for r in rows if r['path']!=path]+[row]
s=json.loads((O/'asphalt-source.json').read_text());path=s['path'];rows=[r for r in rows if r['path']!=path]+[dict(asset_id='d141_asphalt_photo',path=path,source_url=s['url'],license='CC0-1.0',accessed='2026-10-25',tool='direct download from official Poly Haven API URL',ai_generated='no',sha256=s['sha256'],notes='Unmodified diffuse photo; provider physical scale and download URL in asphalt-source.json')]
if (O/'linen-source.json').exists():
 s=json.loads((O/'linen-source.json').read_text());path=s['path'];rows=[r for r in rows if r['path']!=path]+[dict(asset_id='d141_linen_photo',path=path,source_url=s['url'],license='CC0-1.0',accessed='2026-10-25',tool='direct download official Poly Haven API; furniture atlas BOX at32px/m; fixed palette',ai_generated='no',sha256=s['sha256'],notes='Unmodified diffuse; provider real dimensions in linen-source.json; replaces printed cloth')]
with prov.open('w',encoding='utf8',newline='') as f:w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows)
print('PREPARED',len(m['assets']))
