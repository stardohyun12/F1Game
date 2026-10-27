from pathlib import Path
import json,csv,hashlib
R=Path(__file__).resolve().parents[3];E=R/'art/export/q3-depot';V=R/'art/_review/2026-10-28-q3-depot'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
j=json.loads((E/'fbx-validation.json').read_text());assert j['fbx_reimport'] and j['front_4m_clear'] and all(j['textures_byte_identical_to_kit'].values())
j['output_sha256']={p.name:sha(p) for p in E.iterdir() if p.suffix in ['.fbx','.png']}
j['numeric_gate']=json.loads((V/'audit.json').read_text())
j['approval']='pending Dohyun; Unity import not run'
(E/'delivery-validation.json').write_text(json.dumps(j,indent=2))
p=R/'art/provenance.csv';rows=list(csv.DictReader(p.open(encoding='utf-8-sig',newline='')));fields=list(rows[0]);rows=[r for r in rows if not r['asset_id'].startswith('q3_depot_')]
for name,digest in j['output_sha256'].items():
 path=E/name;row={k:'' for k in fields};row.update(asset_id='q3_depot_'+path.stem,path=path.relative_to(R).as_posix(),source_url='art/export/q3-depot/provenance.csv',license='CC0-1.0 photographs; original scripted geometry',accessed='2026-10-28',tool='Blender bpy; shared kit v2 UV assembly',ai_generated='no',sha256=digest,notes='Shared albedo unchanged; sign converted from Blender built-in font, no font file distributed; no light bake; pending Dohyun approval.');rows.append(row)
with p.open('w',encoding='utf-8',newline='') as f:
 w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows)
print('DELIVERY CHECK',j['tris'],j['mesh_count'],j['numeric_gate']['passed'],flush=True)
