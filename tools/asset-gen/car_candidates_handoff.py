"""Copy only technical + independent visual approved D142 candidates."""
from pathlib import Path
import json,csv,shutil,hashlib
ROOT=Path(__file__).resolve().parents[2];O=ROOT/'art/models/car_candidates_v01';R=ROOT/'art/_review/2026-10-25-car-candidates';D=ROOT/'F1/Assets/Art/BayLoop/Car'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
g=json.loads((R/'gates.json').read_text(encoding='utf8'));copied=[];held=[]
for k in ['A','C']:
 a=g[k];check=json.loads((R/f'check-{k}.json').read_text(encoding='utf8'));fbx=json.loads((R/f'fbx-{k}.json').read_text(encoding='utf8'))
 if not(a['yes'] and a['score']>=7 and check['pass'] and fbx['pass']):held.append(k);continue
 assert check['model_sha256']==sha(O/f'{k}.glb') and check['texture_sha256']==sha(O/f'{k}_Atlas.png')
 assert fbx['fbx']['sha256']==sha(O/f'{k}.fbx')
 assert all(sha(R/name)==value for name,value in a['reviewed_images'].items())
 D.mkdir(parents=True,exist_ok=True)
 for name in [f'{k}.fbx',f'{k}_Atlas.png']:
  src=O/name;dst=D/name
  if dst.exists() and sha(dst)!=sha(src):
   old=R/'iterations/preexisting-delivery';old.mkdir(parents=True,exist_ok=True);shutil.copy2(dst,old/name)
  shutil.copy2(src,dst);assert sha(src)==sha(dst);copied.append(dict(path=dst.relative_to(ROOT).as_posix(),sha256=sha(dst),source=src.relative_to(ROOT).as_posix()))
prov=ROOT/'art/provenance.csv'
with prov.open(encoding='utf-8-sig',newline='') as f:rd=csv.DictReader(f);fields=rd.fieldnames;rows=list(rd)
for item in copied:
 original=next(r for r in rows if r['path']==item['source']);row=dict(original,path=item['path'],asset_id=original['asset_id']+'_testcopy',sha256=item['sha256'],notes=original['notes']+'; D142 test handoff only, Unity import pending')
 rows=[r for r in rows if r['path']!=row['path']]+[row]
with prov.open('w',encoding='utf8',newline='') as f:w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows)
(R/'delivery.json').write_text(json.dumps(dict(files=copied,held=held,unity_run=False,commit_created=False,physics_changed=False,meta_or_scene_changed=False),indent=2),encoding='utf8')
print('copied',len(copied),'held',held)
