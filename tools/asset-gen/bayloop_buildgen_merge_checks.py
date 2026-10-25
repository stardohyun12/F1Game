"""Merge per-batch checks only after matching every current source SHA."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parents[2];R=ROOT/'art/_review/2026-10-25-bayloop-variety'
assets=json.loads((ROOT/'art/models/bayloop_variety_v01/manifest.json').read_text())['assets']
for kind,files,key in [
 ('checks',['checks-stage54.json','checks-facilities-farm.json','checks-terrace-final.json','checks-shop-final.json','checks-shop01-final.json'],'hashes'),
 ('fbx-checks',['fbx-stage54.json','fbx-facilities-farm.json','fbx-repairs-final.json','fbx-shop01-final.json'],'source_sha256')]:
 rows={}
 for filename in files:
  data=json.loads((R/filename).read_text());rows.update({a['id']:a for a in data['assets']})
 assert len(rows)==len(assets)==63
 for a in assets:
  for k in ['model','fbx','texture']:
   assert rows[a['id']][key][k]==hashlib.sha256((ROOT/a[k]).read_bytes()).hexdigest(),(a['id'],k,'stale result')
 result=dict(pass_=all(a['pass'] for a in rows.values()),assets=[rows[a['id']] for a in assets],batch_sources=files,all_current_source_hashes_verified=True)
 result['pass']=result.pop('pass_')
 (R/(kind+'.json')).write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
 print(kind,len(rows),'passed',sum(a['pass'] for a in rows.values()))
