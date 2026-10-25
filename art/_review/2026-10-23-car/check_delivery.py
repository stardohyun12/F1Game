"""Final delivery audit; no Unity or game-file writes."""
from pathlib import Path
import json,csv,hashlib,re
from PIL import Image
R=Path(__file__).resolve().parents[3];H=Path(__file__).resolve().parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
checks={};detail={}
for name in ['check_car.json','check_fbx.json','check_closed_volume_wheel_spin.json']:
    result=json.loads((H/name).read_text('utf-8'));checks[name]=result['pass']
main=json.loads((H/'check_car.json').read_text('utf-8'))
checks['model_check_hash_current']=main['model_sha256']==sha(R/'art/models/car_v01/car_v01.glb')
checks['texture_check_hash_current']=main['texture_sha256']==sha(R/'art/models/car_v01/CarAtlas.png')
original=json.loads((H/'current-car-measure.json').read_text('utf-8'))['source_files']
detail['input_hashes_unchanged']={p:sha(R/p)==value for p,value in original.items()}
checks['measured_source_files_unchanged']=all(detail['input_hashes_unchanged'].values())
links=re.findall(r'(?:src|href)="([^"]+)"',(H/'index.html').read_text('utf-8'))
missing=[s for s in links if not s.startswith(('http','#')) and not (H/s).resolve().exists()]
checks['html_local_links_exist']=not missing;detail['missing_links']=missing
with (R/'art/provenance.csv').open(encoding='utf-8-sig',newline='') as f:rows=list(csv.DictReader(f))
ours=[r for r in rows if r['asset_id'].startswith('car_v01_')]
checks['seven_current_provenance_hashes']=len(ours)==7 and all(sha(R/r['path'])==r['sha256'] for r in ours)
checks['only_review_plate_is_ai']=all(r['ai_generated'] in ('no','아니오') for r in ours if r['asset_id']!='car_v01_review_plate')
checks['four_960_images']=all(Image.open(H/name).size==(960,540) for name in ['current-coast-960.png','new-coast-960.png','current-current-framing-960.png','new-current-framing-960.png'])
checks['reference_comparison_exists']=(H/'reference-compare.png').exists()
checks['no_temp_paths_in_html']='generated_images' not in (H/'index.html').read_text('utf-8')
detail['model_sha256']=sha(R/'art/models/car_v01/car_v01.glb');detail['fbx_sha256']=sha(R/'art/models/car_v01/car_v01.fbx');detail['atlas_sha256']=sha(R/'art/models/car_v01/CarAtlas.png')
out={'pass':all(checks.values()),'checks':checks,'details':detail,'unity_editor_used':False,'scope':'Static delivery, image dimensions, recorded source hash comparison. Does not claim whole-repository mutation audit or browser rendering.'}
(H/'delivery-check.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(out,ensure_ascii=False))
raise SystemExit(0 if out['pass'] else 1)
