"""Furniture repair: actual plain linen replaces printed checker cloth."""
from pathlib import Path
import json,hashlib,urllib.request
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];O=ROOT/'art/models/bayloop_props2_v01';S=O/'sources'
def fetch(url):return urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'PitCrewArt/1.0 CC0 texture study'}),timeout=45).read()
info=json.loads(fetch('https://api.polyhaven.com/info/rough_linen'));files=json.loads(fetch('https://api.polyhaven.com/files/rough_linen'))
item=files['Diffuse']['1k']['jpg'];src=S/'rough_linen_diff_1k.jpg'
if not src.exists():src.write_bytes(fetch(item['url']))
(S/'rough-linen-info.json').write_text(json.dumps(info,indent=2))
source=dict(id='rough_linen',path=src.relative_to(ROOT).as_posix(),url='https://polyhaven.com/a/rough_linen',download_url=item['url'],license='CC0-1.0',sha256=hashlib.sha256(src.read_bytes()).hexdigest(),dimensions_mm=info['dimensions'])
(O/'linen-source.json').write_text(json.dumps(source,indent=2))
pal=np.array(json.loads((ROOT/'art/source/palette/pitcrew_d123_32.json').read_text())['colors'],dtype=np.uint8)
arr=np.asarray(Image.open(O/'Props2PhotoAtlas.png').convert('RGB')).copy()
linen=Image.open(src).convert('RGB').resize(tuple(round(x/1000*32) for x in info['dimensions']),Image.Resampling.BOX)
wood=Image.open(ROOT/'art/textures/2026-10-24-kp-v2/albedo/A1_KP_wood_64ppm.png').convert('RGB').resize((64,64),Image.Resampling.BOX)
metal=Image.open(ROOT/'art/source/photos/course-signs/rusty_painted_metal_diff_1k.png').convert('RGB').crop((340,260,650,620)).resize((21,25),Image.Resampling.BOX)
spec={2:(wood,[21,24]),4:(linen,[15,28]),6:(metal,[7,8]),8:(linen,[19,14]),9:(linen,[19,14]),11:(metal,[26,27]),12:(linen,[26,27]),13:(metal,[2,26]),14:(metal,[15,28]),15:(metal,[2,26])}
for idx,(photo,ladder) in spec.items():
 lum=np.asarray(photo)@np.array([.2126,.7152,.0722]);mask=(lum>=np.median(lum)).astype(int);patch=pal[np.array(ladder)[mask]]
 inner=np.tile(patch,(int(np.ceil(252/patch.shape[0])),int(np.ceil(252/patch.shape[1])),1))[:252,:252];x=idx%4*256;y=idx//4*256
 arr[y:y+256,x:x+256]=np.pad(inner,((2,2),(2,2),(0,0)),mode='edge')
out=O/'Props2FurnitureAtlas.png';Image.fromarray(arr).save(out)
assert set(map(tuple,np.unique(arr.reshape(-1,3),axis=0)))<=set(map(tuple,pal))
(O/'furniture-texture-change.json').write_text(json.dumps(dict(path=out.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(out.read_bytes()).hexdigest(),new_source=source,ladders={k:v[1] for k,v in spec.items()},process='Actual plain woven linen replaces printed checked fabric; BOX at provider physical scale to 32px/m; close-value fixed-palette ladders for wood/metal; fruit colour stylization retains photographed microvariation. No AI/no procedural noise.',outside_palette_colors=0),indent=2))
print('FURNITURE_ATLAS',len(np.unique(arr.reshape(-1,3),axis=0)))
