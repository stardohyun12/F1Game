"""Reuse CC0 photographs and A1 studies at WORLD 32px/m; no image generation."""
from pathlib import Path
import sys,json,hashlib,csv
import numpy as np
from PIL import Image,ImageDraw,ImageFont
from photo_to_pixel import load_palette,quantize_exact
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'art/models/bayloop_buildings_v01'
PAL=ROOT/'art/source/palette/pitcrew_d123_32.json'
def main():
 OUT.mkdir(parents=True,exist_ok=True);palette=load_palette(PAL)
 old=ROOT/'art/textures/2026-10-24-kp-v2';m=json.loads((old/'manifest.json').read_text(encoding='utf-8'))
 photos={t['id']:(Image.open(ROOT/t['albedo']).convert('RGB').resize((64,64),Image.Resampling.BOX),t['source']['url'],t['albedo']) for t in m['tiles']}
 # Reuse the A1 pre-quantization photograph for plaster: quantizing an already
 # flat cream swatch again destroys the photographic variation at world scale.
 photos['plaster']=(Image.open(old/'original_reduced/plaster_photo_64ppm.png').convert('RGB').resize((64,64),Image.Resampling.BOX),photos['plaster'][1],'art/textures/2026-10-24-kp-v2/original_reduced/plaster_photo_64ppm.png')
 grey=json.loads((old/'source_roof_candidates.json').read_text(encoding='utf-8'))['candidates'][2]
 photos['greyroof']=(Image.open(ROOT/grey['path']).convert('RGB').resize((96,96),Image.Resampling.BOX),grey['url'],grey['path'])
 hs=json.loads((ROOT/'art/source/hillhouse-surfaces/sources.json').read_text(encoding='utf-8'))
 for key,asset in [('linen','fabric_pattern_05'),('stonewall','stone_brick_wall_001')]:
  s=next(s for s in hs if s['id']==asset);size=tuple(round(v/1000*32) for v in s['dimensions_mm'])
  photos[key]=(Image.open(ROOT/s['path']).convert('RGB').resize(size,Image.Resampling.BOX),s['url'],s['path'])
 metal=ROOT/'art/source/photos/course-signs/rusty_painted_metal_diff_1k.png'
 # Same photograph used by the established sign pipeline; source dimensions queried separately.
 import urllib.request
 req=urllib.request.Request('https://api.polyhaven.com/info/rusty_painted_metal',headers={'User-Agent':'PitCrewArt/1.0 (CC0 texture study)'})
 info=json.loads((OUT/'metal-info.json').read_text()) if (OUT/'metal-info.json').exists() else json.loads(urllib.request.urlopen(req,timeout=60).read());(OUT/'metal-info.json').write_text(json.dumps(info,indent=2),encoding='utf-8')
 source=json.loads((ROOT/'art/source/photos/course-signs/sources.json').read_text())[0]
 (OUT/'metal-source.json').write_text(json.dumps({'id':'rusty_painted_metal','path':metal.relative_to(ROOT).as_posix(),'url':source['source_url'],'download_url':source['download_url'],'sha256':source['sha256'],'license':'CC0-1.0','dimensions_mm':info['dimensions']},indent=2),encoding='utf-8')
 size=tuple(round(v/1000*32) for v in info['dimensions'])
 photos['metal']=(Image.open(metal).convert('RGB').resize(size,Image.Resampling.BOX),'https://polyhaven.com/a/rusty_painted_metal',metal.relative_to(ROOT).as_posix())
 # Ladders change only photographic colour values, never add stochastic wear.
 spec=[('plaster','plaster',[17,4,4,4]),('wood','wood',[9,21,24,17]),('wood_dark','wood',[6,2,9,5]),('roof','greyroof',[9,5,8,7]),('roof_clay','roof',None),('stone','stone',None),('metal','metal',[9,5,8,7]),('glass','linen',[9,5,17,4]),('cloth','linen',[17,4,0,25]),('trim','plaster',[4,0,25]),('rose_wall','plaster',[15,4,0,25]),('teal','wood',[2,26,5,18]),('stonewall','stonewall',[9,5,8,7,17]),('sign','wood',[6,2,9,21]),('rust','metal',[9,15,21,4]),('darkroof','greyroof',[6,9,5,8])]
 atlas=Image.new('RGB',(1024,1024),palette[2]);tiles=[]
 for i,(name,key,ladder) in enumerate(spec):
  im,url,path=photos[key];a=np.asarray(im).astype(float)
  if ladder:
   lum=a@np.array([.2126,.7152,.0722]);cuts=np.quantile(lum,np.linspace(0,1,len(ladder)+1)[1:-1]);inds=np.searchsorted(cuts,lum)
   im=Image.fromarray(np.array([palette[k] for k in ladder],dtype='uint8')[inds])
  else:im=quantize_exact(im,palette,'none')
  im.save(OUT/(name+'_32ppm.png'))
  inner=Image.fromarray(np.tile(np.asarray(im),(int(np.ceil(252/im.height)),int(np.ceil(252/im.width)),1))[:252,:252])
  if name=='sign':
   font=ImageFont.truetype(str(ROOT/'F1/Assets/UI/Fonts/Pretendard/Pretendard-Regular.ttf'),13)
   draw=ImageDraw.Draw(inner);draw.fontmode='1';draw.text((4,0),'차 · TEA',font=font,fill=palette[25])
  # Already exact palette: tiling and fontmode=1 preserve these byte values.
  assert set(map(tuple,np.unique(np.asarray(inner).reshape(-1,3),axis=0)))<=set(palette)
  cell=Image.fromarray(np.pad(np.asarray(inner),((2,2),(2,2),(0,0)),mode='edge'))
  atlas.paste(cell,((i%4)*256,(i//4)*256))
  tiles.append({'id':name,'index':i,'source_url':url,'source_path':path,'physical_photo_repeat_m':[im.width/32,im.height/32],'source_pixels_at_32ppm':list(im.size),'palette_ladder':ladder,'atlas_rect_xywh':[(i%4)*256+2,(i//4)*256+2,252,252]})
 atlas.save(OUT/'BayLoopPhotoAtlas.png')
 terrace=atlas.copy();arr=np.asarray(terrace).copy()
 # Same physical photographs, close-value whitewash ladder for the coastal type.
 for idx,mapping in [(0,{17:19,4:14}),(10,{15:19,4:14,0:14,25:14}),(7,{9:7,5:18,17:18,4:18}),(8,{17:19,4:14,0:14,25:14})]:
  x=(idx%4)*256;y=(idx//4)*256;cell=arr[y:y+256,x:x+256].copy()
  for old,new in mapping.items():arr[y:y+256,x:x+256][np.all(cell==palette[old],axis=2)]=palette[new]
 Image.fromarray(arr).save(OUT/'terrace_house_Atlas.png')
 warehouse=atlas.copy();cell=atlas.crop((2*256,0,3*256,256))
 font=ImageFont.truetype(str(ROOT/'F1/Assets/UI/Fonts/Pretendard/Pretendard-Regular.ttf'),13)
 draw=ImageDraw.Draw(cell);draw.fontmode='1';draw.text((6,2),'창고 · 3',font=font,fill=palette[25])
 warehouse.paste(cell,(256,3*256));warehouse.save(OUT/'harbor_warehouse_Atlas.png')
 data={'palette':PAL.relative_to(ROOT).as_posix(),'density_px_m':32,'atlas_size':1024,'cell_size':256,'interior':252,'tiles':tiles,'process':'Existing A1 CC0 photo-derived 64px/m tiles BOX to 32px/m + exact palette; supplementary real photos at provider physical scale; photographed luminance ladders; no random noise; 2px gutter; signs Pretendard OFL'}
 (OUT/'texture-manifest.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
 prov=ROOT/'art/provenance.csv'
 with prov.open(encoding='utf-8-sig',newline='') as f:r=csv.DictReader(f);fields=r.fieldnames;rows=list(r)
 paths=[OUT/'BayLoopPhotoAtlas.png',OUT/'terrace_house_Atlas.png',OUT/'harbor_warehouse_Atlas.png']+[OUT/(s[0]+'_32ppm.png') for s in spec]
 for s in hs+[{'path':metal.relative_to(ROOT).as_posix(),'url':'https://polyhaven.com/a/rusty_painted_metal'}]:
  if s['url'] not in {t['source_url'] for t in tiles}:continue
  path=s['path'];p=ROOT/path
  if any(r['path']==path for r in rows):continue
  rows.append(dict(asset_id='bayloop_raw_'+p.stem,path=path,source_url=s['url'],license='CC0-1.0',accessed='2026-10-25',tool='existing official direct photo download; checksum verified',ai_generated='no',sha256=hashlib.sha256(p.read_bytes()).hexdigest(),notes='Original diffuse photograph; provider metadata retained beside sources or metal-info.json'))
 for p in paths:
  path=p.relative_to(ROOT).as_posix();rows=[r for r in rows if r['path']!=path]
  rows.append(dict(asset_id='bayloop_20261025_'+p.stem,path=path,source_url='; '.join(sorted({t['source_url'] for t in tiles})),license='CC0-1.0; Pretendard OFL-1.1',accessed='2026-10-25',tool='photo_to_pixel.py; bayloop_textures.py',ai_generated='no',sha256=hashlib.sha256(p.read_bytes()).hexdigest(),notes=data['process']))
 with prov.open('w',encoding='utf-8',newline='') as f:w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows)
 print('TEXTURES',len(tiles),atlas.size)
if __name__=='__main__':main()
