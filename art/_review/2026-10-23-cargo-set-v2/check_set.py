from pathlib import Path
import csv,json,hashlib,subprocess,sys
R=Path(__file__).resolve().parent;repo=R.parents[2]
specs={'box':('cargo_box_final_v2',[.4,.3,.3]),'cake':('cargo_cake_v02',[.254,.127,.254]),'eggs':('cargo_eggs_v02',[.28,.069,.28]),'wine':('cargo_wine_v01',[.0752,.3,.0752]),'fish':('cargo_fish_v02',[.4,.095,.3])}
ledger=repo/'art/provenance.csv'
with ledger.open(encoding='utf-8-sig',newline='') as f:r=csv.DictReader(f);fields=r.fieldnames;rows=list(r)
for kind,(folder,dims) in specs.items():
 p=repo/'art/models'/folder/'provenance-row.json'
 if p.exists():
  row=json.loads(p.read_text(encoding='utf-8'));assert hashlib.sha256((repo/row['path']).read_bytes()).hexdigest()==row['sha256']
  rows=[r for r in rows if r['asset_id']!=row['asset_id']];rows.append(row)
with ledger.open('w',encoding='utf-8',newline='') as f:w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows)
(R/'validation').mkdir(exist_ok=True)
allpass=True
for kind,(folder,dims) in specs.items():
 d=repo/'art/models'/folder;tex=next(d.glob('*atlas_64.png'));out=R/'validation'/f'{kind}.json'
 cmd=[sys.executable,str(repo/'tools/asset-gen/check_asset.py'),'--model',str(d/(folder+'.glb')),'--texture',str(tex),'--palette',str(repo/'art/source/palette/pitcrew_d123_32.gpl'),'--density','64','--dimensions',*map(str,dims),'--shape','cargo','--surface-manifest',str(d/'atlas_manifest.json'),'--out',str(out)]
 result=subprocess.run(cmd,capture_output=True,text=True);data=json.loads(out.read_text(encoding='utf-8'));allpass&=data['pass'];print(kind,{k:data.get(k) for k in ['pass','tris','density_min','density_max','error']},flush=True)
sys.exit(0 if allpass else 1)
