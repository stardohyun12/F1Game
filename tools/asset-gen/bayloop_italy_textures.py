"""D137 photo-derived, shared-palette atlas variants. No generated noise."""
from pathlib import Path
import json,csv,hashlib
import numpy as np
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2];OLD=ROOT/'art/models/bayloop_buildings_v01';OUT=ROOT/'art/models/bayloop_italy_v01'
OUT.mkdir(parents=True,exist_ok=True)
pal=json.loads((ROOT/'art/source/palette/pitcrew_d123_32.json').read_text())['colors']
base=np.asarray(Image.open(OLD/'BayLoopPhotoAtlas.png').convert('RGB'))
font=ImageFont.truetype(str(ROOT/'F1/Assets/UI/Fonts/Pretendard/Pretendard-Regular.ttf'),11)
meta=json.loads((OLD/'texture-manifest.json').read_text());urls=sorted({t['source_url'] for t in meta['tiles']})
records=[]
for name,wall in [('cream',[19,14]),('ochre',[29,30]),('blue',[13,19])]:
 arr=base.copy()
 for idx,colors in [(0,wall),(10,wall),(1,[26,27]),(2,[26,5]),(7,[2,26]),(8,[19,14]),(9,[19,14]),(11,[26,27]),(14,[15,24])]:
  x=idx%4*256;y=idx//4*256;cell=base[y:y+256,x:x+256];lum=cell@np.array([.2126,.7152,.0722]);cut=(lum.min()+lum.max())/2
  arr[y:y+256,x:x+256]=np.array([pal[i] for i in colors],dtype=np.uint8)[(lum>cut).astype(int)]
 # Four independently addressable small signs: real font, Korean only.
 cell=Image.fromarray(arr[0:256,512:768].copy());d=ImageDraw.Draw(cell);d.fontmode='1'
 for y,text in [(2,'상점'),(34,'수리'),(66,'배달'),(98,'영업')]:d.text((5,y),text,font=font,fill=tuple(pal[25]))
 arr[768:1024,256:512]=np.asarray(cell)
 path=OUT/f'Italy_{name}_Atlas.png';Image.fromarray(arr).save(path)
 records.append(dict(asset_id='d137_'+path.stem,path=path.relative_to(ROOT).as_posix(),source_url='; '.join(urls),license='CC0-1.0; Pretendard OFL-1.1',accessed='2026-10-25',tool='bayloop_italy_textures.py; bayloop_textures.py; photo_to_pixel.py',ai_generated='no',sha256=hashlib.sha256(path.read_bytes()).hexdigest(),notes='Existing CC0 photo atlas, close-value palette ladders, photo variation preserved at 32px/m; Korean font-only signs; no random wear'))
meta.update(process='D137 close-value colour ladders of existing CC0 photographs; all texture details retain world32px/m; signs Pretendard OFL Korean',variants={'cream':[19,14],'ochre':[29,30],'blue':[13,19]})
(OUT/'texture-manifest.json').write_text(json.dumps(meta,ensure_ascii=False,indent=2),encoding='utf8')
p=ROOT/'art/provenance.csv'
with p.open(encoding='utf-8-sig',newline='') as f:r=csv.DictReader(f);fields=r.fieldnames;rows=list(r)
for row in records:rows=[r for r in rows if r['path']!=row['path']];rows.append(row)
with p.open('w',encoding='utf8',newline='') as f:w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows)
print('ITALY_ATLASES',len(records))
