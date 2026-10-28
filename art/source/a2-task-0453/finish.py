from pathlib import Path
import json,hashlib,csv
from PIL import Image
R=Path(__file__).resolve().parents[3];E=R/'art/export/scene-a2-kit';W=Path(__file__).parent;V=R/'art/_review/2026-10-27-scene-a2-kit'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
kit=json.loads((E/'kit.json').read_text(encoding='utf-8'));before=json.loads((W/'before-hashes.json').read_text());validation=json.loads((W/'fbx-validation.json').read_text())
assert len(kit['assets'])==29 and len(validation)==29 and all(v['passed'] for v in validation)
palette=[]
for line in (E/'palette32.gpl').read_text().splitlines():
 q=line.split()
 if len(q)>=3 and q[0].isdigit():palette.append(tuple(map(int,q[:3])))
union=set();checks=[]
for a in kit['assets']:
 p=E/a['texture'];im=Image.open(p).convert('RGBA');colors=im.getcolors(maxcolors=100000)
 assert colors is not None,p
 visible={c[:3] for n,c in colors if c[3]};union|=visible;assert visible<=set(palette),(p,'palette mismatch')
 assert all(c[3] in [0,255] for n,c in colors),(p,'non cutout alpha')
 fbm=E/(a['name']+'.fbm')/a['texture'];assert fbm.exists() and sha(fbm)==sha(p),(a['name'],'fbm mismatch')
 a['output_sha256']={'fbx':sha(E/a['model']),'png':sha(p)}
 a['task_20261028_0453']='new authored-source derivative' if a['name']=='background_building_05' else ('photo remap and rebake; original geometry kept' if a['name'] in ['tires_01','pole_01'] else 'reused existing V5 bake, verified this task')
 checks.append({'name':a['name'],'size':list(im.size),'visible_colors':len(visible),'fbm_matches':True})
assert len(palette)==32 and len(union)==32
unchanged=[]
for n,h in before.items():
 if n in ['tires_01.fbx','tires_01.png','pole_01.fbx','pole_01.png']:continue
 assert sha(E/n)==h,('unexpected modification',n);unchanged.append(n)
for title in ['Bicycle','Telephone pole','Tire','old bike','Vending Machine']:assert '"'+title+'"' in (E/'CREDITS.md').read_text(encoding='utf-8')
trees=[a for a in kit['assets'] if a['name'].startswith('autumn_tree')];buildings=[a for a in kit['assets'] if a['name'].startswith('background_building')]
assert len(trees)==5 and len(buildings)==5 and any(a['original_pack']=='chillydurango-shacks-sheds' for a in buildings)
kit.update(last_task='20261028-0453',approval='Dohyun approval required; Unity not run',workflow_exception='This task explicitly requests V5 baked lighting/unlit; not the D-185 albedo kit',reused_count=26,repainted_count=2,new_count=1)
(E/'kit.json').write_text(json.dumps(kit,ensure_ascii=False,indent=2),encoding='utf-8')
result=dict(assets=29,visible_palette_union=32,reused_assets=26,repainted=['tires_01','pole_01'],new_asset='background_building_05',untouched_files_verified=len(unchanged),tree_heights_m=[a['size_m_xyz'][2] for a in trees],wall_reference_m=4.5,tree_wall_ratios=[a['size_m_xyz'][2]/4.5 for a in trees],buildings=5,models_reimport_passed=29,credit_entries_preserved=5,textures=checks)
(W/'delivery-validation.json').write_text(json.dumps(result,indent=2));(V/'task-0453-validation.json').write_text(json.dumps(result,indent=2))
# Correct the ledger run date and distinguish reused work from new execution.
reg=R/'art/provenance.csv'
with reg.open(encoding='utf-8-sig',newline='') as f:reader=csv.DictReader(f);fields=reader.fieldnames;rows=list(reader)
for a in rows:
 if a['asset_id'].startswith('scene_a2_kit_'):
  a['accessed']='2026-10-28';a['notes']=a['notes'].split('; task0453 verified')[0]+'; task0453 verified; 26 retained / tires_01 pole_01 rebaked / Shack A newly baked'
with reg.open('w',encoding='utf-8',newline='') as f:writer=csv.DictWriter(f,fieldnames=fields);writer.writeheader();writer.writerows(rows)
print('DELIVERY PASS',result['assets'],'assets / 32 colors / 29 reimports / 5 credits /',len(unchanged),'unchanged files',flush=True)
