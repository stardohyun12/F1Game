from pathlib import Path
import csv,json,hashlib,importlib.util
from PIL import Image
R=Path(__file__).resolve().parents[2];O=R/'art/models/garage_kart_v01';V=R/'art/_review/2026-10-27-garage-kart'
spec=importlib.util.spec_from_file_location('noise',R/'tools/qa/surface_noise.py');noise=importlib.util.module_from_spec(spec);spec.loader.exec_module(noise);im=Image.open(O/'GarageKartPaint.png');results=[]
for t in [0,1,2,3,5,6,9,15]:
 crop=im.crop((t%4*128+8,t//4*128+8,t%4*128+120,t//4*128+120)).resize((64,64));r=noise.region(crop,(0,0,64,64));results.append(dict(tile=t,**r))
(V/'paint-check.json').write_text(json.dumps(dict(size=im.size,power_of_two=True,scope='paint cell interiors only; excludes authored labels and semantic edges; not game-motion noise test',speck_limit=.3,passed=all(r['speck']<=.3 for r in results),cells=results),indent=2))
ledger=R/'art/provenance.csv'
with ledger.open(encoding='utf-8-sig',newline='') as f:rd=csv.DictReader(f);fields=rd.fieldnames;rows={r['path']:r for r in rd}
for p in [*O.glob('*.fbx'),*O.glob('*.glb'),*O.glob('*.blend'),O/'GarageKartPaint.png']:
 rel=p.relative_to(R).as_posix();rows[rel]=dict(asset_id=p.stem+'_'+p.suffix[1:],path=rel,source_url='original bpy geometry and scripted paint; https://www.tonykart.com/telai-rookie-my25_en.php (dimensions reference only)',license='Original project artwork; Galmuri SIL OFL 1.1 for lettering',accessed='2026-10-27',tool='garage_kart.py; garage_kart_atlas.py',ai_generated='no',sha256=hashlib.sha256(p.read_bytes()).hexdigest(),notes='D160/D161 approval proposal; no photos/no AI; original painted soft strokes; 950mm kart wheelbase; no engine copy')
with ledger.open('w',encoding='utf8',newline='') as f:w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows.values())
print('paint PASS',all(r['speck']<=.3 for r in results),'provenance updated')
