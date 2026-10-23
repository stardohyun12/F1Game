from pathlib import Path
from PIL import Image
import json,re,hashlib
R=Path(__file__).resolve().parent
spec=json.loads((R/'sprites.json').read_text(encoding='utf-8'))
old=json.loads((R.parent/'2026-10-23-d121/sprites.json').read_text(encoding='utf-8'))
checks=[]
for s in spec:
 p=R/'sprites'/f'{s["slot"]}.png';im=Image.open(p)
 checks.append({'slot':s['slot'],'size':list(im.size),'mode':im.mode,'size_pass':list(im.size)==s['size'],'alpha':im.getchannel('A').getextrema() if im.mode=='RGBA' else None,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
missing=[]
for p in R.rglob('*.html'):
 text=p.read_text(encoding='utf-8');base=p.parent
 bm=re.search(r'<base href="([^"]+)"',text)
 if bm:base=(base/bm.group(1)).resolve()
 for link in re.findall(r'(?:src|href)="([^"]+)"',text):
  if '://' in link or link.startswith('data:') or (bm and link==bm.group(1)):continue
  if (base/link).resolve() != (R/'validation.json').resolve() and not (base/link).exists():missing.append([str(p.relative_to(R)),link])
out={'same_manifest_as_d121':spec==old,'sprite_count':len(checks),'size_pass_count':sum(x['size_pass'] for x in checks),'rgba_count':sum(x['mode']=='RGBA' for x in checks),'sprites':checks,'missing_local_html_links':missing,'rendered_screens':{p.name:list(Image.open(p).size) for p in R.glob('mockup-*.png')},'compare_size':list(Image.open(R/'compare.png').size),'cargo_sheet_size':list(Image.open(R/'cargo-sheet.png').size),'font_license_present':(R/'fonts/LICENSE.txt').exists(),'unity_executed':False,'actual_64px_atlas_created':False,'triangles_measured':False}
(R/'validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in out.items() if k!='sprites'},ensure_ascii=False))
assert spec==old and all(x['size_pass'] for x in checks) and len(checks)==10 and not missing
