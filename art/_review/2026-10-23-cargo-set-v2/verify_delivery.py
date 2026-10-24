from pathlib import Path
import csv,hashlib,json,re
from PIL import Image
R=Path(__file__).resolve().parent
repo=R.parents[2]
dirs={'box':'cargo_box_final_v2','cake':'cargo_cake_v02','eggs':'cargo_eggs_v02','wine':'cargo_wine_v01','fish':'cargo_fish_v02'}
ledger=list(csv.DictReader((repo/'art/provenance.csv').open(encoding='utf-8-sig')))
results=[]
for kind,folder in dirs.items():
    check=json.loads((R/f'validation/{kind}.json').read_text())
    model=repo/'art/models'/folder/(folder+'.glb')
    textures=list((repo/'art/models'/folder).glob('*atlas_64.png'))
    assert len(textures)==1
    tex=textures[0]
    assert check['pass'] and hashlib.sha256(model.read_bytes()).hexdigest()==check['model_sha256']
    assert hashlib.sha256(tex.read_bytes()).hexdigest()==check['texture_sha256']
    assert any(check['texture_sha256'] in row.values() for row in ledger)
    assert Image.open(R/kind/'scene-960x540.png').size==(960,540)
    assert Image.open(R/kind/'six-faces.png').size==(1920,1080)
    for view in ['close','scene-angle','front','right','back','left','top','bottom']:
        im=Image.open(R/kind/(view+'.png')).convert('RGBA');box=im.getchannel('A').getbbox()
        assert box and box[0]>0 and box[1]>0 and box[2]<im.width and box[3]<im.height,(kind,view,box)
    results.append({'asset':kind,'hashes_current':True,'provenance_hash_present':True,'views_uncropped':True,'checks_pass':True})
html=(R/'index.html').read_text(encoding='utf-8')
missing=[p for p in re.findall(r'(?:href|src)="([^"]+)"',html) if not p.startswith(('http','#')) and p!='validation/delivery-integrity.json' and not (R/p).exists()]
for name in ['compare-960x540.png','lineup-scene-960x540.png','lineup-reduced-960x540.png','silhouette-960x540.png']:
    assert Image.open(R/name).size==(960,540)
out={'pass':not missing,'assets':results,'missing_index_links':missing,'scope':'file hashes, provenance, image dimensions and alpha border; no Unity verification'}
(R/'validation/delivery-integrity.json').write_text(json.dumps(out,indent=2),encoding='utf-8')
print(json.dumps(out,indent=2))
assert not missing,missing
