from pathlib import Path
import json,hashlib
import numpy as np
from PIL import Image
R=Path(__file__).resolve().parents[2];O=R/'art/models/bayloop_harbor_landmarks_v01';O.mkdir(parents=True,exist_ok=True)
P=np.array(json.loads((R/'art/source/palette/pitcrew_d123_32.json').read_text())['colors'],dtype='uint8')
a=np.array(Image.open(R/'art/models/bayloop_variety_v01/BayLoopShared_01.png').convert('RGB'))
# Existing photographic rusty-metal cell; reuse photographic luminance only.
metal=a[256:512,512:768].copy();lum=metal@np.array([.2126,.7152,.0722]);mask=lum<np.median(lum)
for index,pair in [(6,(29,30)),(14,(15,28))]:
 x=index%4*256;y=index//4*256;a[y:y+256,x:x+256]=np.where(mask[:,:,None],P[pair[0]],P[pair[1]])
path=O/'HarborPhotoAtlas.png';Image.fromarray(a).save(path)
json.dump({'source_atlas':'art/models/bayloop_variety_v01/BayLoopShared_01.png','processing':'Reuse verified CC0 photo cells at unchanged UV density; metal photographic luminance into shared palette pairs 29/30 and15/28. No generated noise.','colors':len(np.unique(a.reshape(-1,3),axis=0)),'outside':len(set(map(tuple,a.reshape(-1,3)))-set(map(tuple,P))),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()},(O/'texture-processing.json').open('w'),indent=2)
