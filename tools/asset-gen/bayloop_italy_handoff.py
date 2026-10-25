"""Guarded D137 trial copy; pass gates + current binary checks; preserve all Unity metadata."""
from pathlib import Path
import json,csv,hashlib,shutil
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];REV=ROOT/'art/_review/2026-10-25-bayloop-italy'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def rel(p):return p.relative_to(ROOT).as_posix()
g=json.loads((REV/'gate-summary.json').read_text());allassets=[];checks={};fbx={}
for kind,folder in [('building','bayloop_italy_v01'),('prop','bayloop_italy_props_v01')]:
 m=json.loads((ROOT/'art/models'/folder/'manifest.json').read_text());allassets+=m['assets']
 checks.update({x['id']:x for x in json.loads((REV/(kind+'-checks.json')).read_text())['assets']})
 fbx.update({x['id']:x for x in json.loads((REV/(kind+'-fbx-checks.json')).read_text())['assets']})
ledger=ROOT/'art/provenance.csv'
with ledger.open(encoding='utf-8-sig',newline='') as f:r=csv.DictReader(f);fields=r.fieldnames;rows=list(r)
by={r['path']:r for r in rows};plans={};accepted=[];withheld=[]
for a in allassets:
 name=a['id'];gate=g[name]
 if gate['score']<7 or not gate['yes']:withheld.append(name);continue
 assert checks[name]['pass'] and fbx[name]['pass'],name
 assert hashlib.sha256(Image.open(REV/gate['render']).convert('RGBA').tobytes()).hexdigest()==gate['render_pixels_sha256'],(name,'stale render')
 for key in ['model','texture','fbx']:assert sha(ROOT/a[key])==checks[name]['hashes'][key]==fbx[name]['source_sha256'][key],(name,key,'stale inspection')
 dest=ROOT/'F1/Assets/Art/BayLoop'/('Props' if a.get('asset_kind')=='prop' else 'Buildings')
 for key in ['fbx','texture']:
  src=ROOT/a[key];target=dest/src.name
  if target.exists() and sha(target)!=sha(src):
   backup=REV/'iterations/pre-d137/trial-buildings'/target.name
   assert backup.exists() and sha(backup)==sha(target),('unexpected concurrent edit',target)
  plans[target]=src
 accepted.append(name)
delivery=[]
for target,src in plans.items():
 target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(src,target);assert sha(target)==sha(src)
 row=dict(by[rel(src)]);row.update(asset_id='d137_trial_'+src.stem+'_'+src.suffix[1:],path=rel(target),sha256=sha(target),notes=row['notes']+'; PD 20261025-1929 trial import; visual gate >=7; Unity material remap/prefab/capture pending')
 rows=[r for r in rows if r['path']!=rel(target)];rows.append(row);delivery.append(dict(source=rel(src),target=rel(target),sha256=sha(target)))
with ledger.open('w',encoding='utf8',newline='') as f:w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows)
(REV/'trial-delivery.json').write_text(json.dumps(dict(accepted=accepted,withheld=withheld,files=delivery,unity_run=False,prefabs_created=False,scope='FBX+new PNG only; existing meta/material/prefab/scenes unchanged'),ensure_ascii=False,indent=2),encoding='utf8')
print('D137_DELIVERED',len(accepted),'WITHHELD',withheld,'FILES',len(delivery))
