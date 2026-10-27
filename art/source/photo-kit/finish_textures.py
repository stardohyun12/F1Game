from pathlib import Path
from PIL import Image
import numpy as np,json
R=Path(__file__).resolve().parents[3];W=Path(__file__).parent;E=R/'art/export/scene-a6-photo-kit';items=json.loads((W/'manifest.json').read_text());pal=np.array([list(map(int,l.split()[:3])) for l in (E/'palette-photo-kit.gpl').read_text().splitlines() if l[:1].isdigit()]);metrics={}
extra=np.array([[v,v,v] for v in [65,85,105,125,145,165,185]]+[[v+8,v+2,v-8] for v in [90,110,130,150,170,185]]+[[v,int(v*.53),int(v*.39)] for v in [135,150,165,180,195,210]])
pal=np.unique(np.concatenate([pal,extra]),axis=0);QPAL=pal
(E/'palette-photo-kit.gpl').write_text('GIMP Palette\nName: garage32 + photo kit material ramps\nColumns: 8\n#\n'+'\n'.join(' '.join(map(str,c))+' photo-kit' for c in pal)+'\n')
def select_palette(name):
 global QPAL
 QPAL=extra[7:13] if 'stone_' in name else extra[13:] if 'tin_F2' in name else extra[:7] if name=='ac_G3' or 'tin_F3' in name else pal
def quant(a):
 f=a.reshape(-1,3).astype(float);out=np.empty_like(f)
 for i in range(0,len(f),8192):out[i:i+8192]=QPAL[((f[i:i+8192,None]-QPAL)**2).sum(2).argmin(1)]
 return out.reshape(a.shape).astype('uint8')
def finish(name):
 raw=np.array(Image.open(W/(name+'_raw.png')).convert('RGB'),float);ao=np.array(Image.open(W/(name+'_ao.png')).convert('L'),float)/255;a=raw*(.70+.30*ao)[:,:,None];shade=(1-ao)*.16;a[:,:,1]+=shade*24;a[:,:,2]+=shade*28;s=np.clip(a/255,0,1);lin=np.where(s<=.04045,s/12.92,((s+.055)/1.055)**2.4);lin=lin*2+(1-s.max(2))[:,:,None]**2*np.array([.009,.017,.019]);enc=np.where(lin<=.0031308,lin*12.92,1.055*np.maximum(lin,0)**(1/2.4)-.055)*255
 y=enc@np.array([.2126,.7152,.0722]);target=np.clip(y,55,195);enc*=target[:,:,None]/np.maximum(y[:,:,None],1);return quant(np.clip(enc,0,255))
def clean(a):
 for _ in range(3):
  neighbors=[np.roll(a,(dy,dx),(0,1)) for dy,dx in [(1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)]];same=np.zeros(a.shape[:2],bool)
  for n in neighbors:same|=np.all(a==n,axis=2)
  bad=~same;bad[[0,-1],:]=False;bad[:,[0,-1]]=False
  if not bad.any():break
  med=np.median(np.stack(neighbors),axis=0);a[bad]=quant(med)[bad]
 return a
for d in items:
 select_palette(d['name']);a=finish(d['name']);a=a if d['name']=='ac_G3' else clean(a)
 if d['kind']=='tile':
  a[:,0]=a[:,-1]=quant(np.rint((a[:,0].astype(float)+a[:,-1])/2).reshape(-1,1,3))[:,0];a[0]=a[-1]=quant(np.rint((a[0].astype(float)+a[-1])/2).reshape(1,-1,3))[0]
 Image.fromarray(a).save(E/(d['name']+'.png'));metrics[d['name']]=dict(size=[a.shape[1],a.shape[0]],colors=len(np.unique(a.reshape(-1,3),axis=0)),seam_lr_max=int(np.max(np.abs(a[:,0].astype(int)-a[:,-1].astype(int)))) if d['kind']=='tile' else None,seam_tb_max=int(np.max(np.abs(a[0].astype(int)-a[-1].astype(int)))) if d['kind']=='tile' else None,luma_p5_p95=np.percentile(a@np.array([.2126,.7152,.0722]),[5,95]).tolist())
if (W/'garage-tin-mask.json').exists():
 old=np.array(Image.open(R/'art/export/scene-a-v5b-garage/garage_baked32.png').convert('RGB'));H,AW=old.shape[:2];mask=np.zeros((H,AW),bool)
 for d in json.loads((W/'garage-tin-mask.json').read_text()):
  q=np.array(d['uv'])*[AW,H];q[:,1]=H-q[:,1];a,b,c=q;mn=np.maximum(0,np.floor(q.min(0)).astype(int));mx=np.minimum([AW-1,H-1],np.ceil(q.max(0)).astype(int));yy,xx=np.mgrid[mn[1]:mx[1]+1,mn[0]:mx[0]+1];x=xx+.5;y=yy+.5;den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
  if abs(den)<1e-9:continue
  aa=((b[1]-c[1])*(x-c[0])+(c[0]-b[0])*(y-c[1]))/den;bb=((c[1]-a[1])*(x-c[0])+(a[0]-c[0])*(y-c[1]))/den;cc=1-aa-bb;inside=(aa>=-1e-6)&(bb>=-1e-6)&(cc>=-1e-6);mask[yy[inside],xx[inside]]=True
 for variant in ['tin_F2','tin_F3']:
  if not (W/('garage_'+variant+'_ao.png')).exists():continue
  select_palette(variant);a=finish('garage_'+variant);out=old.copy();out[mask]=a[mask];Image.fromarray(out).save(E/('garage_'+variant+'_atlas.png'));assert np.array_equal(out[~mask],old[~mask]);metrics['garage_'+variant]=dict(size=[AW,H],changed_region_pixels=int(mask.sum()),outside_tin_identical=True)
(E/'texture-validation.json').write_text(json.dumps(metrics,indent=2));print('FINISHED',metrics)
