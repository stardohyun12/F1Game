from pathlib import Path
import json,csv,shutil,hashlib
R=Path(__file__).resolve().parents[2];O=R/'art/models/bayloop_people_v03';V=R/'art/_review/2026-10-26-people-v3';D=R/'F1/Assets/Art/BayLoop/People'
assert json.loads((V/'fbx-check.json').read_text())['passed']
assert json.loads((V/'texture-check.json').read_text())['passed']
assert (V/'visual-gate.md').exists()
assert json.loads((V/'gate-approved.json').read_text())['score']>=7
m=json.loads((O/'manifest.json').read_text());copies=[];files=[]
for a in m['assets']:
 files.extend([O/(a['id']+ext) for ext in ['.fbx','.blend']])
files.append(O/'PeopleRetroAtlas_v3.png');D.mkdir(parents=True,exist_ok=True)
for p in files:
 if p.suffix not in ['.fbx','.png']:continue
 dst=D/p.name;shutil.copy2(p,dst);copies.append(dict(source=p.relative_to(R).as_posix(),target=dst.relative_to(R).as_posix(),sha256=hashlib.sha256(dst.read_bytes()).hexdigest()));assert p.read_bytes()==dst.read_bytes()
files.extend(D/Path(c['target']).name for c in copies)
ledger=R/'art/provenance.csv'
with ledger.open(encoding='utf-8-sig',newline='') as f:rd=csv.DictReader(f);fields=rd.fieldnames;rows={x['path']:x for x in rd}
for p in files:
 key=p.relative_to(R).as_posix();rows[key]=dict(asset_id=p.stem+'_D146_v3',path=key,source_url='https://quaternius.com/packs/universalanimationlibrary.html' if p.suffix!='.png' else 'original scripted shared-palette swatches',license='UAL Standard CC0; original bpy geometry and palette artwork',accessed='2026-10-26',tool='bayloop_people_v3.py',ai_generated='no',sha256=hashlib.sha256(p.read_bytes()).hexdigest(),notes='D-146; no photograph/no noise; v1 rig and baked rotations reused; scaled rest and translations; floor rebake; 12px/m equivalent UV')
with ledger.open('w',encoding='utf8',newline='') as f:w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows.values())
(V/'copied.json').write_text(json.dumps(copies,indent=2),encoding='utf8')
(V/'index.html').write_text('<!doctype html><meta charset="utf-8"><title>D146_v3 사람 v2</title><style>body{font:18px sans-serif;background:#e4e0d6;padding:24px}img{width:100%;max-width:1440px}</style><h1>D146_v3 사람6종</h1><p>실제 Blender 렌더 / Unity 미실행 / QA·프로그래밍 임포트 필요</p><img src="sheet.png"><p><a href="visual-gate.md">독립 게이트</a> · <a href="fbx-check.json">실제FBX 검사</a> · <a href="texture-check.json">팔레트 검사</a> · <a href="copied.json">시험 복사</a></p>',encoding='utf8')
print('Copied',len(copies),'files. Preserved all v1 and v2.')
