from pathlib import Path
from PIL import Image
import numpy as np,json
R=Path(__file__).resolve().parents[3];W=Path(__file__).parent;E=R/'art/export/scene-albedo-kit';bases=json.loads((W/'bases.json').read_text())
for n,c in bases.items():
 a=np.full((256,256,3),c,dtype='uint8');f=np.array(Image.open(W/'fronts'/(n+'.png')).convert('RGB'));h,w=f.shape[:2];a[5:8+h+3,5:8+w+3]=np.pad(f,((3,3),(3,3),(0,0)),mode='edge');a[8:72,128:192]=c;a[35:39,150:160]=np.rint(np.array(c)*.98);a[8:40,208:240]=[65,65,65];Image.fromarray(a).save(E/(n+'.png'))
# Same UV exterior mask as the existing F3 replacement; restore blue without affecting other surfaces.
p=W/'before/garage_tin_F3_atlas.png';old=np.array(Image.open(p).convert('RGB'));a=old.copy();h,w=a.shape[:2];mask=np.zeros((h,w),bool)
for t in json.loads((R/'art/source/albedo-kit/garage-tin-mask.json').read_text()):
 q=np.array(t['uv'])*[w,h];q[:,1]=h-q[:,1];aa,bb,cc=q;lo=np.maximum(0,np.floor(q.min(0)).astype(int));hi=np.minimum([w-1,h-1],np.ceil(q.max(0)).astype(int));yy,xx=np.mgrid[lo[1]:hi[1]+1,lo[0]:hi[0]+1];x=xx+.5;y=yy+.5;den=(bb[1]-cc[1])*(aa[0]-cc[0])+(cc[0]-bb[0])*(aa[1]-cc[1])
 if abs(den)<1e-12:continue
 u=((bb[1]-cc[1])*(x-cc[0])+(cc[0]-bb[0])*(y-cc[1]))/den;v=((cc[1]-aa[1])*(x-cc[0])+(aa[0]-cc[0])*(y-cc[1]))/den;ok=(u>=0)&(v>=0)&(u+v<=1);mask[yy[ok],xx[ok]]=True
base=np.array(json.loads((W/'palette-samples.json').read_text())['F3 blue']['RGB']);val=old[mask].mean(1);fac=1+np.clip((val-np.median(val))/max(val.std(),1),-1,1)*.04;fac=np.round(fac/.02)*.02;a[mask]=np.clip(np.rint(base*fac[:,None]),0,255);assert np.array_equal(a[~mask],old[~mask]);Image.fromarray(a).save(E/'garage_tin_F3_atlas.png');np.save(W/'f3-mask.npy',mask)
