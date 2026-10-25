"""Copy only inspected and visually passed FBX/PNG trial assets; no Unity/meta edits."""
from pathlib import Path
import csv,hashlib,json,shutil
ROOT=Path(__file__).resolve().parents[2]
SRC=ROOT/'art/models/bayloop_buildings_v01'
REV=ROOT/'art/_review/2026-10-25-bayloop-buildings'
DST=ROOT/'F1/Assets/Art/BayLoop/Buildings'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def rel(p):return p.relative_to(ROOT).as_posix()
manifest=json.loads((SRC/'manifest.json').read_text(encoding='utf8'))
gates=json.loads((REV/'gate-summary.json').read_text(encoding='utf8'))
checks={a['id']:a for a in json.loads((REV/'checks.json').read_text())['assets']}
fbx={a['id']:a for a in json.loads((REV/'fbx-checks.json').read_text())['assets']}
plans={};accepted=[];withheld=[]
for a in manifest['assets']:
 name=a['id'];g=gates[name]
 if g['score']<7 or not g['yes']:
  withheld.append(name);continue
 assert (REV/g['report']).is_file(),name
 assert checks[name]['pass'] and fbx[name]['pass'],name
 for key in ['model','fbx','texture']:
  path=ROOT/a[key]
  assert sha(path)==checks[name]['hashes'][key]==fbx[name]['source_sha256'][key],(name,key,'stale check')
 for key in ['fbx','texture']:
  path=ROOT/a[key];dest=DST/path.name
  assert not dest.exists() or sha(dest)==sha(path),('Existing different file; do not overwrite',dest)
  plans[dest]=path
 accepted.append(name)
assert accepted
ledger=ROOT/'art/provenance.csv'
with ledger.open(encoding='utf-8-sig',newline='') as f:
 reader=csv.DictReader(f);fields=reader.fieldnames;rows=list(reader)
by_path={r['path']:r for r in rows};delivery=[]
DST.mkdir(parents=True,exist_ok=True)
for dest,path in plans.items():
 row=dict(by_path[rel(path)]);row.update(asset_id='bayloop_trial_'+path.stem+'_'+path.suffix[1:],path=rel(dest),sha256=sha(path),notes=row['notes']+'; Trial copy authorized PD 20261025-1645; visual gate >=7; Unity import/prefab/game capture pending')
 shutil.copy2(path,dest);assert sha(dest)==sha(path)
 rows=[r for r in rows if r['path']!=rel(dest)];rows.append(row)
 delivery.append({'source':rel(path),'target':rel(dest),'sha256':sha(dest)})
with ledger.open('w',encoding='utf8',newline='') as f:
 writer=csv.DictWriter(f,fieldnames=fields);writer.writeheader();writer.writerows(rows)
(REV/'trial-delivery.json').write_text(json.dumps({'accepted':accepted,'withheld':withheld,'files':delivery,'unity_run':False,'prefabs_created':False,'scope':'FBX + required PNG only; no scene/code/meta/atlas replacements'},ensure_ascii=False,indent=2),encoding='utf8')
print(json.dumps({'accepted':accepted,'withheld':withheld,'copied_files':len(delivery)}))
