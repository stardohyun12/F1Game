"""Final paving repair, preserve approved sign atlas byte-for-byte."""
from pathlib import Path
from PIL import Image
import numpy as np,json,hashlib
ROOT=Path(__file__).resolve().parents[2];O=ROOT/'art/models/bayloop_props2_v01'
p=np.array(json.loads((ROOT/'art/source/palette/pitcrew_d123_32.json').read_text())['colors'],dtype=np.uint8)
photo=Image.open(O/'sources/asphalt_02_diff_1k.jpg').convert('RGB').resize((96,96),Image.Resampling.BOX);lum=np.asarray(photo)@np.array([.2126,.7152,.0722]);mask=(lum>=np.median(lum)).astype(int)
a=np.asarray(Image.open(O/'Props2RoadAtlas.png').convert('RGB')).copy()
for idx,ladder,m in [(12,[7,20],mask),(15,[2,9],np.roll(np.rot90(mask),31,axis=0)),(13,[2,26],np.roll(mask,43,axis=1))]:
 patch=p[np.array(ladder)[m]];inner=np.tile(patch,(3,3,1))[:252,:252];x=idx%4*256;y=idx//4*256;a[y:y+256,x:x+256]=np.pad(inner,((2,2),(2,2),(0,0)),mode='edge')
out=O/'Props2PavingAtlas.png';Image.fromarray(a).save(out)
(O/'paving-texture-change.json').write_text(json.dumps(dict(path=out.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(out.read_bytes()).hexdigest(),source_url='https://polyhaven.com/a/asphalt_02',process='Same actual 3m96px photo; background close-value ladder; repair rotated90deg and shifted31pixels to break background continuity; tar seam close-dark photo ladder. Physical density unchanged.',outside_palette_colors=0),indent=2))
assert set(map(tuple,np.unique(a.reshape(-1,3),axis=0)))<=set(map(tuple,p))
