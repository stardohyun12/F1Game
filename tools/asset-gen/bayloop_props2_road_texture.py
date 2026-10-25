"""D141 asphalt/paint/symbol atlas from actual source photography."""
from pathlib import Path
from PIL import Image
import numpy as np,json,hashlib
ROOT=Path(__file__).resolve().parents[2];O=ROOT/'art/models/bayloop_props2_v01'
p=np.array(json.loads((ROOT/'art/source/palette/pitcrew_d123_32.json').read_text())['colors'],dtype=np.uint8)
photo=Image.open(O/'sources/asphalt_02_diff_1k.jpg').convert('RGB').resize((96,96),Image.Resampling.BOX);lum=np.asarray(photo)@np.array([.2126,.7152,.0722]);mask=(lum>=np.median(lum)).astype(int)
arr=np.asarray(Image.open(O/'Props2PhotoAtlas.png').convert('RGB')).copy()
for idx,ladder in [(9,[19,14]),(11,[26,27]),(12,[5,8]),(15,[2,9]),(13,[15,28])]:
 patch=p[np.array(ladder)[mask]];inner=np.tile(patch,(3,3,1))[:252,:252];x=idx%4*256;y=idx//4*256;arr[y:y+256,x:x+256]=np.pad(inner,((2,2),(2,2),(0,0)),mode='edge')
out=O/'Props2RoadAtlas.png';Image.fromarray(arr).save(out)
assert set(map(tuple,np.unique(arr.reshape(-1,3),axis=0)))<=set(map(tuple,p))
(O/'road-texture-change.json').write_text(json.dumps(dict(path=out.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(out.read_bytes()).hexdigest(),source_url='https://polyhaven.com/a/asphalt_02',size=[1024,1024],outside_palette_colors=0,process='Actual 3m asphalt diffuse BOX to96px, exact palette ladders; white road paint and red sign are photo-grounded information colours; no noise'),indent=2))
print('ROAD_ATLAS',len(np.unique(arr.reshape(-1,3),axis=0)))
