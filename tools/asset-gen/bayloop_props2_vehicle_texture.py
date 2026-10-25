"""R3 vehicle paint: quiet close-value palette ladders, photographic input."""
from pathlib import Path
import json,hashlib
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];O=ROOT/'art/models/bayloop_props2_v01'
pal=np.array(json.loads((ROOT/'art/source/palette/pitcrew_d123_32.json').read_text())['colors'],dtype=np.uint8)
raw=Image.open(ROOT/'art/source/photos/course-signs/rusty_painted_metal_diff_1k.png').convert('RGB')
photo=raw.crop((340,260,650,620)).resize((21,25),Image.Resampling.BOX)
lum=np.asarray(photo)@np.array([.2126,.7152,.0722]);mask=(lum>=np.quantile(lum,.5)).astype(int)
a=np.asarray(Image.open(O/'Props2PhotoAtlas.png').convert('RGB')).copy()
spec={0:[7,20],3:[1,7],7:[2,26],13:[2,26]}
for idx,ladder in spec.items():
 patch=pal[np.array(ladder)[mask]];inner=np.tile(patch,(11,12,1))[:252,:252];x=idx%4*256;y=idx//4*256;a[y:y+256,x:x+256]=np.pad(inner,((2,2),(2,2),(0,0)),mode='edge')
 # Deliberate narrow upper reflection, not high-contrast grain over all glass.
 if idx==7:
  a[y+2:y+5,x+2:x+254]=pal[np.array([7,8])[np.tile(mask,(11,12))[:3,:252]]]
path=O/'Props2VehiclesAtlas.png';Image.fromarray(a).save(path)
assert set(map(tuple,np.unique(a.reshape(-1,3),axis=0)))<=set(map(tuple,pal))
(O/'vehicle-texture-change.json').write_text(json.dumps(dict(path=path.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),source_url='https://polyhaven.com/a/rusty_painted_metal',source_crop_xyxy=[340,260,650,620],source_crop_m=[310/1024*2.2,360/1024*2.2],ladders=spec,reason='Reduce whole-body camouflage contrast within fixed shared palette; no invented noise or colour. Warm grey, weathered grey, ochre paint; rubber and hub visually separated.',outside_palette_colors=0),indent=2),encoding='utf8')
print('VEHICLE_ATLAS',len(np.unique(a.reshape(-1,3),axis=0)))
