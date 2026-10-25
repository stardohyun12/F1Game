"""Copy only independently approved, currently checked new D141 files."""
from pathlib import Path
import json,hashlib,shutil,csv
ROOT=Path(__file__).resolve().parents[2];O=ROOT/'art/models/bayloop_props2_v01';R=ROOT/'art/_review/2026-10-25-bayloop-props2';D=ROOT/'F1/Assets/Art/BayLoop/Props'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p):return json.loads(p.read_text(encoding='utf8'))
m=read(O/'manifest.json');g=read(R/'gates.json');glb={a['id']:a for a in read(R/'glb-checks.json')['assets']};fbx={a['id']:a for a in read(R/'fbx-checks.json')['assets']}
existing=read(R/'existing-files-before.json');changes=[p for p,h in existing.items() if not (ROOT/p).exists() or sha(ROOT/p)!=h]
# Concurrent team material edits were observed before this script's first write.
# Preserve the current bytes, never restore or overwrite the old snapshot.
assert all((ROOT/p).exists() and Path(p).suffix=='.mat' for p in changes),('Unreviewed concurrent change',changes)
current_existing={p:sha(ROOT/p) for p in existing}
approved=[];held=[];copies={}
for a in m['assets']:
 name=a['id'];v=g[name]
 if not(v['yes'] and v['score']>=7):held.append(name);continue
 assert glb[name]['pass'] and fbx[name]['pass'],name
 for key in ('model','fbx','texture'):
  assert sha(ROOT/a[key])==glb[name]['hashes'][key],(name,key,'stale GLB check')
  assert sha(ROOT/a[key])==fbx[name]['source_sha256'][key],(name,key,'stale FBX check')
 for p,h in v['render_sha256'].items():assert sha(R/p)==h,(name,'stale visual gate')
 for key in ('fbx','texture'):
  src=ROOT/a[key];dst=D/src.name
  assert dst.relative_to(ROOT).as_posix() not in existing,'Do not replace an existing asset'
  if dst.exists():assert sha(dst)==sha(src),'Refuse mismatched destination'
  copies[str(src)]=dst
 approved.append(dict(id=name,fbx=(D/Path(a['fbx']).name).relative_to(ROOT).as_posix(),texture=(D/Path(a['texture']).name).relative_to(ROOT).as_posix(),tris=a['tris'],score=v['score']))
D.mkdir(parents=True,exist_ok=True)
for src,dst in copies.items():shutil.copy2(src,dst);assert sha(Path(src))==sha(dst)
with (ROOT/'art/provenance.csv').open(encoding='utf-8-sig',newline='') as f:r=csv.DictReader(f);fields=r.fieldnames;rows=list(r)
for src,dst in copies.items():
 row=next(r.copy() for r in rows if r['path']==Path(src).relative_to(ROOT).as_posix());row.update(path=dst.relative_to(ROOT).as_posix(),asset_id=row['asset_id']+'_trial',sha256=sha(dst));row['notes']+=' D141 gated trial copy; Unity import not run.';rows=[r for r in rows if r['path']!=row['path']]+[row]
with (ROOT/'art/provenance.csv').open('w',encoding='utf8',newline='') as f:w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows)
assert all((ROOT/p).exists() and sha(ROOT/p)==h for p,h in current_existing.items())
result=dict(approved=approved,held=held,file_count=len(copies),copied=[dict(path=p.relative_to(ROOT).as_posix(),sha256=sha(p)) for p in copies.values()],existing_files_preserved_during_handoff=len(existing),preexisting_concurrent_material_changes=changes,unity_run=False,prefabs_created=False,commit=False)
(R/'delivery.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print('DELIVERED',len(approved),'HELD',len(held),'FILES',len(copies))
