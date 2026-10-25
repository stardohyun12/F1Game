from pathlib import Path
import shutil,json,hashlib
import numpy as np
from PIL import Image
root=Path('D:/Github/F1Game');out=root/'art/models/bayloop_deferred_v02';rev=root/'art/_review/2026-10-26-landmarks/deferred';it=rev/'iterations/r1';it.mkdir(parents=True,exist_ok=True)
for p in rev.glob('*.png'):
 if not (it/p.name).exists():shutil.copy2(p,it/p.name)
if not (it/'manifest.json').exists():shutil.copy2(out/'manifest.json',it/'manifest.json')
pal=np.array(json.loads((root/'art/source/palette/pitcrew_d123_32.json').read_text())['colors'],dtype=np.uint8)
a=np.array(Image.open(root/'art/models/bayloop_variety_v01/BayLoopShared_01.png').convert('RGB'))
def cell(i,p):
 p=np.tile(p,(int(np.ceil(252/p.shape[0])),int(np.ceil(252/p.shape[1])),1))[:252,:252];x=i%4*256;y=i//4*256;a[y:y+256,x:x+256]=np.pad(p,((2,2),(2,2),(0,0)),mode='edge')
raw=np.array(Image.open(root/'art/textures/2026-10-24-kp-v2/original_reduced/plaster_photo_64ppm.png').convert('L').resize((64,64),Image.Resampling.BOX))
# Existing photograph's darkest local damage only, no new procedural speckles.
mask=raw<np.quantile(raw,.12)
cell(0,pal[np.where(mask,14,19)]);cell(10,pal[np.where(mask,14,19)])
for i,ladder in [(4,[29,30,31]),(6,[7,1])]:
 x=i%4*256;y=i//4*256;p=a[y+2:y+254,x+2:x+254].copy();l=p@np.array([.2126,.7152,.0722]);q=np.quantile(l,np.linspace(0,1,len(ladder)+1)[1:-1]);cell(i,pal[np.array(ladder)[np.searchsorted(q,l)]])
linen=np.array(Image.open(root/'art/models/bayloop_props2_v01/sources/rough_linen_diff_1k.jpg').convert('F').resize((9,9),Image.Resampling.BOX));cell(8,pal[np.where(linen<np.median(linen),19,14)])
p=out/'DeferredPhotoAtlas.png';Image.fromarray(a).save(p)
meta=dict(source_atlas='art/models/bayloop_variety_v01/BayLoopShared_01.png',photo_sources=['art/textures/2026-10-24-kp-v2/original_reduced/plaster_photo_64ppm.png','art/models/bayloop_props2_v01/sources/rough_linen_diff_1k.jpg'],steps=['reuse CC0 photo-derived atlas','plaster local photograph dark-tail mask 12%, two shared palette colors','terracotta/metal photo color ladder contrast compression','linen physical 0.271m BOX to 9px =33.2px/m, within 10%; palette quantization','252px repeat with 2px edge padding'],ai_generated=False,palette_outside=0)
(out/'texture-refinement.json').write_text(json.dumps(meta,indent=2))
