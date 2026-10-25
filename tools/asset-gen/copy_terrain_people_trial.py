"""Explicit post-review trial copy. Never touches scenes, materials or meta files."""
from pathlib import Path
import argparse,json,csv,hashlib,shutil
ROOT=Path(__file__).resolve().parents[2];REV=ROOT/'art/_review/2026-10-26-terrain-people'
ap=argparse.ArgumentParser();ap.add_argument('--people',action='store_true');ap.add_argument('--people-gate');args=ap.parse_args()
assert json.loads((REV/'terrain/checks.json').read_text())['pass']
assert '7/10' in (REV/'terrain/visual-gate-r1.md').read_text(encoding='utf8')
source=ROOT/'art/textures/2026-10-26-bayloop-terrain';pairs=[]
for p in sorted((source/'albedo').glob('*.png')):pairs.append((p,ROOT/'F1/Assets/Art/BayLoop/Terrain'/p.name,'terrain/visual-gate-r1.md'))
if args.people:
    assert json.loads((REV/'people/checks.json').read_text())['pass']
    assert json.loads((REV/'people/fbx-check.json').read_text())['passed']
    assert args.people_gate,'Pass the reviewed final set gate path relative to review root'
    gate=(REV/args.people_gate).resolve();gate.relative_to(REV.resolve())
    assert gate.exists() # The caller must also review the actual yes/score decision.
    folder=ROOT/'art/models/bayloop_people_v01'
    for p in sorted(folder.glob('people_*.fbx')):pairs.append((p,ROOT/'F1/Assets/Art/BayLoop/People'/p.name,args.people_gate))
    p=folder/'PeoplePhotoAtlas.png';pairs.append((p,ROOT/'F1/Assets/Art/BayLoop/People'/p.name,args.people_gate))
ledger=ROOT/'art/provenance.csv'
with ledger.open(encoding='utf-8-sig',newline='') as f:r=csv.DictReader(f);fields=r.fieldnames;rows=list(r)
records=[];add=[]
for src,dst,gate in pairs:
    rel=src.relative_to(ROOT).as_posix();target=dst.relative_to(ROOT).as_posix();digest=hashlib.sha256(src.read_bytes()).hexdigest()
    original=next(r for r in rows if r['path']==rel and r['sha256']==digest)
    if dst.exists() and dst.read_bytes()!=src.read_bytes():
        old=REV/'iterations/pre-trial-copy'/dst.parent.name/dst.name;old.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(dst,old)
    dst.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(src,dst)
    copied=hashlib.sha256(dst.read_bytes()).hexdigest();assert copied==digest
    row=dict(original);row.update(asset_id=original['asset_id']+'_unity_trial',path=target,notes=original['notes']+'; Trial copy only; Unity import not run');add.append(row)
    records.append(dict(source=rel,destination=target,sha256=copied,gate=gate))
paths={r['path'] for r in add};rows=[r for r in rows if r['path'] not in paths]+add
with ledger.open('w',encoding='utf8',newline='') as f:w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows)
(REV/'trial-copy.json').write_text(json.dumps(dict(files=records,unity_import_run=False),indent=2),encoding='utf8')
print('Verified trial copies:',len(records))
