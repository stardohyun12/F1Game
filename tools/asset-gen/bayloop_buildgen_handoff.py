"""D140 guarded numeric-name delivery. No metadata, scene, prefab or legacy edits."""
from pathlib import Path
from PIL import Image
import json,csv,hashlib,shutil
ROOT=Path(__file__).resolve().parents[2];REV=ROOT/'art/_review/2026-10-25-bayloop-variety';OUT=ROOT/'art/models/bayloop_variety_v01';DEST=ROOT/'F1/Assets/Art/BayLoop/Buildings'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
g=json.loads((REV/'gate-summary.json').read_text());m=json.loads((OUT/'manifest.json').read_text());checks={a['id']:a for a in json.loads((REV/'checks.json').read_text())['assets']};fbx={a['id']:a for a in json.loads((REV/'fbx-checks.json').read_text())['assets']}
before=json.loads((REV/'existing-trial-files-before.json').read_text());assert all((ROOT/p).exists() and sha(ROOT/p)==h for p,h in before.items()),'Pre-existing trial files changed; inspect before delivery'
assert len(m['assets'])==63 and len(g['types'])==11,'Incomplete production/review manifest'
tone=g['tone'];tone_pass=tone['yes'] and tone['score']>=7
assert hashlib.sha256(Image.open(REV/tone['render']).convert('RGBA').tobytes()).hexdigest()==tone['render_pixels_sha256'],'Stale tone sheet'
accepted=[];held=[];plans={}
for a in m['assets']:
 name=a['id'];gate=g['types'][a['type']]
 for rep in gate['representatives']:
  assert hashlib.sha256(Image.open(REV/rep['render']).convert('RGBA').tobytes()).hexdigest()==rep['render_pixels_sha256'],('Stale representative',rep['id'])
 visual=tone_pass and gate['yes'] and gate['score']>=7 and all(rep['yes'] and rep['score']>=7 for rep in gate['representatives'])
 # A failing technical asset holds its entire type, exactly like the visual type gate.
 group=[x for x in m['assets'] if x['type']==a['type']]
 technical=all(checks[x['id']]['pass'] and fbx[x['id']]['pass'] for x in group)
 if not visual or not technical:held.append(name);continue
 for key in ['model','fbx','texture']:assert sha(ROOT/a[key])==checks[name]['hashes'][key]==fbx[name]['source_sha256'][key],(name,'stale check',key)
 for key in ['fbx','texture']:
  src=ROOT/a[key];target=DEST/src.name
  assert target.relative_to(ROOT).as_posix() not in before,('Would change existing asset',target)
  assert not target.exists() or sha(target)==sha(src),('Conflicting new target',target)
  plans[target]=src
 accepted.append(name)
prov=ROOT/'art/provenance.csv'
with prov.open(encoding='utf-8-sig',newline='') as f:r=csv.DictReader(f);fields=r.fieldnames;rows=list(r)
by={r['path']:r for r in rows};files=[]
for target,src in plans.items():
 shutil.copy2(src,target);assert sha(src)==sha(target)
 row=dict(by[src.relative_to(ROOT).as_posix()]);row.update(path=target.relative_to(ROOT).as_posix(),asset_id='d140_trial_'+target.stem+'_'+target.suffix[1:],notes=row['notes']+'; type representative and full-grid visual gate >=7; trial import only; Unity pending')
 rows=[r for r in rows if r['path']!=row['path']]+[row];files.append(dict(source=src.relative_to(ROOT).as_posix(),target=row['path'],sha256=sha(target)))
with prov.open('w',encoding='utf8',newline='') as f:w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows)
result=dict(accepted=accepted,withheld=held,files=files,tone_pass=tone_pass,existing_files_unchanged=all(sha(ROOT/p)==h for p,h in before.items()),unity_run=False,prefabs_created=False)
(REV/'trial-delivery.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print('DELIVERED',len(accepted),'HELD',len(held),'FILES',len(files))
