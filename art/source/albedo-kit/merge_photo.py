from pathlib import Path
from PIL import Image
import numpy as np,json
R=Path(__file__).resolve().parents[3];W=Path(__file__).parent;E=R/'art/export/scene-albedo-kit';old=np.array(Image.open(E/'garage_baked32.png').convert('RGBA'));h,w=old.shape[:2];mask=np.zeros((h,w),bool)
for t in json.loads((W/'garage-tin-mask.json').read_text()):
 q=np.array(t['uv'])*[w,h];q[:,1]=h-q[:,1];aa,bb,cc=q;lo=np.maximum(0,np.floor(q.min(0)).astype(int));hi=np.minimum([w-1,h-1],np.ceil(q.max(0)).astype(int));yy,xx=np.mgrid[lo[1]:hi[1]+1,lo[0]:hi[0]+1];x=xx+.5;y=yy+.5;den=(bb[1]-cc[1])*(aa[0]-cc[0])+(cc[0]-bb[0])*(aa[1]-cc[1])
 if abs(den)<1e-12:continue
 u=((bb[1]-cc[1])*(x-cc[0])+(cc[0]-bb[0])*(y-cc[1]))/den;v=((cc[1]-aa[1])*(x-cc[0])+(aa[0]-cc[0])*(y-cc[1]))/den;ok=(u>=0)&(v>=0)&(u+v<=1);mask[yy[ok],xx[ok]]=True
for k in ['F2','F3']:
 p=W/('garage_tin_'+k+'_raw.png');rgb=np.array(Image.open(p).convert('RGB').quantize(colors=32,dither=Image.Dither.NONE).convert('RGB'));a=old.copy();a[mask,:3]=rgb[mask];Image.fromarray(a).save(E/('garage_tin_'+k+'_atlas.png'))
(W/'photo-merge.json').write_text(json.dumps(dict(exterior_tin_pixels=int(mask.sum()),base='garage_baked32.png albedo',other_pixels_identical=True,transfer='EMIT only')))
