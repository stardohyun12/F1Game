"""Materials trial: quiet protected paint and distinct surface value families."""
from pathlib import Path
import json,hashlib
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];O=ROOT/'art/models/bskills_trial_v01';OLD=ROOT/'art/models/bayloop_props2_v01'
pal=np.array(json.loads((ROOT/'art/source/palette/pitcrew_d123_32.json').read_text())['colors'],dtype=np.uint8)
metal=Image.open(ROOT/'art/source/photos/course-signs/rusty_painted_metal_diff_1k.png').convert('RGB');crop=(450,400,640,650)
paint=metal.crop(crop);paint_size=[round(190/1024*2.2*32),round(250/1024*2.2*32)]
linen=Image.open(OLD/'sources/rough_linen_diff_1k.jpg').convert('RGB')
def mask(im,size,q=.5):
 lum=np.asarray(im)@np.array([.2126,.7152,.0722]);a=np.asarray(Image.fromarray(lum.astype(np.float32)).resize(tuple(size),Image.Resampling.BOX));return(a>np.quantile(a,q)).astype(int)
pm=mask(paint,paint_size,.9);cm=mask(linen,[9,9]);white=mask(linen,[9,9],.85)
arr=np.asarray(Image.open(OLD/'Props2FurnitureAtlas.png').convert('RGB')).copy()
spec={0:(pm,[7,10]),3:(pm,[1,11]),10:(pm,[29,30]),6:(pm,[7,10]),7:(pm,[2,26]),9:(white,[19,14]),11:(cm,[1,8]),13:(pm,[2,26]),14:(pm,[15,28]),15:(pm,[2,26]),8:(cm,[1,11]),12:(pm,[7,8]),4:(cm,[29,30])}
for idx,(pattern,ladder) in spec.items():
 patch=pal[np.array(ladder)[pattern]];inner=np.tile(patch,(int(np.ceil(252/patch.shape[0])),int(np.ceil(252/patch.shape[1])),1))[:252,:252];x=idx%4*256;y=idx//4*256
 arr[y:y+256,x:x+256]=np.pad(inner,((2,2),(2,2),(0,0)),mode='edge')
out=O/'BskillsTrialPhotoAtlas.png';Image.fromarray(arr).save(out)
meta=json.loads((OLD/'texture-manifest.json').read_text());meta['aliases'].update(window_light=12,rubber=13,darkroof=15,rose_wall=4)
meta.update(atlas=out.relative_to(ROOT).as_posix(),trial=dict(protected_paint_crop=crop,photo_crop_m=[190/1024*2.2,250/1024*2.2],photo_crop_px=paint_size,method='Float photo luminance BOX at32px/m; lower paint contrast; different surface value families, no generated noise',ladders={str(i):v[1] for i,v in spec.items()},rough_linen_url='https://polyhaven.com/a/rough_linen'))
(O/'texture-manifest.json').write_text(json.dumps(meta,indent=2),encoding='utf8')
colors=np.unique(arr.reshape(-1,3),axis=0);assert set(map(tuple,colors))<=set(map(tuple,pal))
(O/'texture-check.json').write_text(json.dumps(dict(colors=len(colors),outside_palette=0,power_of_two=True,sha256=hashlib.sha256(out.read_bytes()).hexdigest()),indent=2))
print('ATLAS',len(colors))
