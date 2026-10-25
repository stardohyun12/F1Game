"""Shared two-package photo/palette pipeline; explicit material recolor and stamps."""
from pathlib import Path
import json,sys,subprocess,hashlib
import numpy as np
from PIL import Image,ImageDraw
repo=Path(__file__).resolve().parents[3]
kind=sys.argv[1] if len(sys.argv)>1 else 'cake'
root=repo/f'art/models/cargo_{kind}_v02';(root/'patches').mkdir(exist_ok=True)
photo=repo/'art/source/photos/cargo-box/CardboardSet001_1K-PNG_Color.png'
pal=repo/'art/source/palette/pitcrew_d123_32.json';meta=json.loads(pal.read_text());c={x['name']:tuple(x['rgb']) for x in meta['entries']}
W,H=(.254,.127) if kind=='cake' else (.280,.069)
tw,th=round(W*64),round(H*64)
rects={'front':[1,1,tw,th],'back':[22,1,tw,th],'right':[43,1,tw,th],'left':[1,12,tw,th],'top':[22,12,tw,tw],'bottom':[43,12,tw,tw]}
atlas=Image.new('RGB',(64,64),c['paper_fold']);records=[]
for face,(x,y,w,h) in rects.items():
 p=root/'patches'/f'{face}_quantized.png'
 subprocess.run([sys.executable,str(repo/'tools/asset-gen/photo_to_pixel.py'),str(photo),str(p),'--palette',str(pal),'--width-m',str(W),'--height-m',str(W if face in ['top','bottom'] else H),'--density','64','--source-id','ambientCG/CardboardSet001','--crop','55','145','220','235'],check=True,stdout=subprocess.DEVNULL)
 raw=np.asarray(Image.open(p).convert('RGB')).mean(axis=2)
 ramp=[c['scene_14'],c['label'],c['label']] if kind=='cake' else [c['paper_fold'],c['paper_flat'],c['carton_light']]
 if face=='bottom':ramp=[c['carton_dark'],c['paper_fold'],c['paper_flat']]
 if face in ['left','right','back'] and kind=='eggs':ramp=[c['tape'],c['paper_fold'],c['paper_fibre']]
 a=np.asarray(ramp,dtype=np.uint8)[np.clip(np.rint(1+(raw-raw.mean())/max(raw.std(),1)*.24),0,2).astype(int)]
 im=Image.fromarray(a);d=ImageDraw.Draw(im)
 # Discrete folds/contact; the photo residual is not random wear.
 d.line((0,h-1,w-1,h-1),fill=c['paper_fold'])
 if face not in ['top','bottom']:d.line((0,0,w-1,0),fill=c['scene_00'] if kind=='cake' else c['tape'])
 if face=='front':
  d.rectangle((2,2,5,min(h-1,6)),fill=c['label']);d.line((2,2,4,2),fill=c['paper_fold']);d.line((2,min(h-1,4),4,min(h-1,4)),fill=c['ink']);d.point((2,min(h-1,5)),fill=c['ink'])
 if face=='right':
  for q in [5,12]:
   # Filled chevron head; never the previous crossbar-plus-stem pattern.
   if kind=='eggs':d.polygon([(q,1),(q-2,3),(q+2,3)],fill=c['ink'])
   else:
    d.polygon([(q,1),(q-2,3),(q+2,3)],fill=c['ink']);d.line((q,3,q,5),fill=c['ink'])
 if kind=='cake' and face in ['front','back']:d.line((7,h-2,w-2,h-2),fill=c['stamp'])
 if kind=='eggs' and face=='front':
  # Three actual egg silhouettes printed on the wrapper, not fake letters.
  for q in [8,11,14]:d.ellipse((q,1,q+1,3),fill=c['label'])
 # Minimum deterministic adjacent-color variation only if an 8x8 window is flat.
 for yy in range(max(0,h-7)):
  for xx in range(max(0,w-7)):
   if len(set(im.crop((xx,yy,xx+8,yy+8)).getdata()))==1:d.point((xx+4,yy+4),fill=ramp[0] if im.getpixel((xx+4,yy+4))!=ramp[0] else ramp[-1])
 im.save(root/'patches'/f'{face}_final.png')
 atlas.paste(im,(x,y));records.append({'face':face,'rect_xywh':[x,y,w,h],'physical_wh_m':[W,W if face in ['top','bottom'] else H]})
# Interior material strips: actual shared palette only, script-painted cream/fruit.
dr=ImageDraw.Draw(atlas)
# Semantic blocks: cream shell/top, terracotta-red berries, sponge, leaves, glint.
# Each geometry facet receives an orthonormal 64px/m projection within these blocks.
dr.rectangle((1,34,20,53),fill=c['label'])
dr.rectangle((23,34,42,53),fill=c['stamp'])
dr.rectangle((45,34,62,53),fill=c['scene_00'])
dr.rectangle((1,56,20,62),fill=c['wine'])
dr.rectangle((23,56,42,62),fill=c['scene_14'])
# Fruit has one seed point and a darker lower edge: native pixels, no scattered noise.
atlas.putpixel((24,35),c['label'])
dr.line((23,37,26,37),fill=c['scene_15'])
atlas.save(root/f'cargo_{kind}_atlas_64.png')
manifest={'density_target':64,'texture_size':[64,64],'faces':records,'layer_order':['CC0 photo','BOX downscale','exact 32 palette','material color ladder','barcode and upright stamps','conditional sparse fill','fold/contact AO'],'label_width_fraction':4/tw,'photo_sha256':hashlib.sha256(photo.read_bytes()).hexdigest(),'ai_generated':False}
(root/'atlas_manifest.json').write_text(json.dumps(manifest,indent=2))
row={'asset_id':f'cargo_{kind}_v02_atlas','path':f'art/models/cargo_{kind}_v02/cargo_{kind}_atlas_64.png','source_url':'https://ambientcg.com/view?id=CardboardSet001','license':'CC0-1.0','accessed':'2026-10-23','tool':'photo_to_pixel.py; cargo_cake_v02/build_atlas.py','ai_generated':'no','sha256':hashlib.sha256((root/f'cargo_{kind}_atlas_64.png').read_bytes()).hexdigest(),'notes':'CC0 photo paper/pulp substrate recolored; scripted packaging stamps and natural cream/egg/fruit palette pixels. Clear windows represented by apertures and narrow opaque film-edge glints, no stochastic transparency noise. No generated image texture.'}
(root/'provenance-row.json').write_text(json.dumps(row,indent=2))
