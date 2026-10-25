"""Final artifact/link/provenance integrity, no editor required."""
from pathlib import Path
from html.parser import HTMLParser
from PIL import Image
import csv,json,hashlib
R=Path(__file__).resolve().parent;ROOT=R.parents[2]
class Links(HTMLParser):
    paths=[]
    def handle_starttag(self,tag,attrs):
        for k,v in attrs:
            if k in ('src','href') and v and not v.startswith(('http:','https:','#','data:')):self.paths.append(v)
p=Links();p.feed((R/'index.html').read_text(encoding='utf-8'))
missing=[x for x in p.paths if not (R/x).exists()]
surface_ids={r['asset_id'] for r in json.loads((ROOT/'art/source/hillhouse-surfaces/provenance-rows.json').read_text(encoding='utf-8'))}
with (ROOT/'art/provenance.csv').open(encoding='utf-8-sig',newline='') as f:rows=[x for x in csv.DictReader(f) if x['asset_id'].startswith('hill_house_') or x['asset_id'] in surface_ids]
hash_errors=[]
for row in rows:
    path=ROOT/row['path']
    if not path.exists() or hashlib.sha256(path.read_bytes()).hexdigest()!=row['sha256']:hash_errors.append(row['asset_id'])
sizes={str(n):list(Image.open(R/f'composite-{n}m-960.png').size) for n in [30,45,60]}
glb=json.loads((R/'check-hillhouse.json').read_text(encoding='utf-8'));fbx=json.loads((R/'check-hillhouse-fbx.json').read_text(encoding='utf-8'))
model=ROOT/'art/models/hill_house_v01'
glb_match=hashlib.sha256((model/'hill_house_v01.glb').read_bytes()).hexdigest()==glb['model_sha256']
fbx_match=hashlib.sha256((model/'hill_house_v01.fbx').read_bytes()).hexdigest()==fbx['fbx_sha256']
legacy=json.loads((ROOT/'art/source/hillhouse-surfaces/checks.json').read_text(encoding='utf-8'))
legacy_match=hashlib.sha256((ROOT/'F1/Assets/Art/LofiPilot/LofiAtlas.png').read_bytes()).hexdigest()==legacy['legacy_original_sha256']
result={'pass':not missing and not hash_errors and len(rows)==len(surface_ids)+4 and all(v==[960,540] for v in sizes.values()) and glb_match and fbx_match and legacy_match and glb['pass'] and fbx['pass'],'missing_html_links':missing,'provenance_rows':len(rows),'provenance_hash_errors':hash_errors,'distance_image_sizes':sizes,'glb_evidence_hash_current':glb_match,'fbx_evidence_hash_current':fbx_match,'original_atlas_unchanged':legacy_match,'glb_pass':glb['pass'],'fbx_geometry_uv_pass':fbx['pass'],'scope':'Static deliverable integrity. Browser layout, Unity import, animation and runtime not executed by this script.'}
(R/'delivery-integrity.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(result,ensure_ascii=False))
raise SystemExit(0 if result['pass'] else 1)
