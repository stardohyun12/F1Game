from pathlib import Path
from PIL import Image
import numpy as np,json,shutil
R=Path(__file__).resolve().parents[3];W=Path(__file__).parent;E=R/'art/export/kart-v6';p=R/'art/export/scene-a-v5b-garage/palette32.gpl'
pal=np.array([[int(v) for v in l.split()[:3]] for l in p.read_text().splitlines() if l[:1].isdigit()],np.float32)
raw=np.array(Image.open(W/'raw.png').convert('RGB'),np.float32);ao=np.array(Image.open(W/'ao.png').convert('L'),np.float32)/255
arr=raw*(.70+.30*ao)[:,:,None];shade=(1-ao)*.16;arr[:,:,1]+=shade*24;arr[:,:,2]+=shade*28
s=np.clip(arr/255,0,1);lin=np.where(s<=.04045,s/12.92,((s+.055)/1.055)**2.4);lin=lin*2+(1-s.max(2))[:,:,None]**2*np.array([.009,.017,.019]);enc=np.clip(np.where(lin<=.0031308,lin*12.92,1.055*np.maximum(lin,0)**(1/2.4)-.055)*255,0,255)
flat=enc.reshape(-1,3);idx=np.empty(len(flat),np.int32)
for i in range(0,len(flat),65536):idx[i:i+65536]=((flat[i:i+65536,None,:]-pal[None,:,:])**2).sum(2).argmin(1)
out=pal[idx].reshape(raw.shape).astype('uint8')
# Stamp the 07 ink after lighting quantization at the SAME atlas density, avoiding bake-filter loss.
stamp=np.array(Image.open(W/'surfaces/number07.png').convert('RGB'));H,AW=out.shape[:2]
for d in json.loads((W/'decal-uv.json').read_text()):
 q=np.array(d['baked'])*[AW,H];q[:,1]=H-q[:,1];uv=np.array(d['source']);a,b,c=q
 xmin,ymin=np.maximum(0,np.floor(q.min(0)).astype(int));xmax,ymax=np.minimum([AW-1,H-1],np.ceil(q.max(0)).astype(int))
 yy,xx=np.mgrid[ymin:ymax+1,xmin:xmax+1];x=xx+.5;y=yy+.5;den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
 aa=((b[1]-c[1])*(x-c[0])+(c[0]-b[0])*(y-c[1]))/den;bb=((c[1]-a[1])*(x-c[0])+(a[0]-c[0])*(y-c[1]))/den;cc=1-aa-bb;inside=(aa>=0)&(bb>=0)&(cc>=0)
 st=aa[...,None]*uv[0]+bb[...,None]*uv[1]+cc[...,None]*uv[2];sx=np.clip((st[...,0]*80).astype(int),0,79);sy=np.clip(((1-st[...,1])*64).astype(int),0,63);ink=stamp[sy,sx,0]<100
 out[yy[inside],xx[inside]]=np.where(ink[inside,None],pal[24],pal[0]).astype('uint8')
Image.fromarray(out).save(E/'kart_v6_baked32.png');shutil.copyfile(p,E/'palette32.gpl')
(E/'texture-check.json').write_text(json.dumps(dict(size=[out.shape[1],out.shape[0]],palette_colors=32,used_colors=int(len(np.unique(idx))),no_black=not np.any(np.all(out==0,axis=2)),texels_per_m=32,method='V5 diffuse + AO(.70+.30*AO), teal shadow lift, +1 stop, nearest palette, no dither'),indent=2))
print('texture',out.shape,'colors',len(np.unique(idx)))
