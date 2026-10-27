from pathlib import Path
from PIL import Image
import numpy as np,json,hashlib
R=Path(__file__).resolve().parents[3];W=Path(__file__).parent;E=R/'art/export/kart-v6d';src=R/'art/export/kart-v6c/kart_v6c.png';old=np.array(Image.open(src).convert('RGB'));out=old.copy();filled=np.zeros(old.shape[:2],bool);H,AW=old.shape[:2]
for d in json.loads((W/'texture-jobs.json').read_text()):
 q=np.array(d['uv'])*[AW,H];q[:,1]=H-q[:,1];a,b,c=q;mn=np.maximum(0,np.floor(q.min(0)).astype(int));mx=np.minimum([AW-1,H-1],np.ceil(q.max(0)).astype(int));yy,xx=np.mgrid[mn[1]:mx[1]+1,mn[0]:mx[0]+1];x=xx+.5;y=yy+.5;den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
 if abs(den)<1e-10:continue
 aa=((b[1]-c[1])*(x-c[0])+(c[0]-b[0])*(y-c[1]))/den;bb=((c[1]-a[1])*(x-c[0])+(a[0]-c[0])*(y-c[1]))/den;cc=1-aa-bb;inside=(aa>=-1e-6)&(bb>=-1e-6)&(cc>=-1e-6);sy,sx=yy[inside],xx[inside];filled[sy,sx]=True
 if d['source_uv']:
  uv=np.array(d['source_uv']);st=aa[...,None]*uv[0]+bb[...,None]*uv[1]+cc[...,None]*uv[2];tx=np.clip((st[...,0]*AW).astype(int),0,AW-1);ty=np.clip(((1-st[...,1])*H).astype(int),0,H-1);out[sy,sx]=old[ty[inside],tx[inside]]
 else:
  p=np.array(d['p']);xyz=aa[...,None]*p[0]+bb[...,None]*p[1]+cc[...,None]*p[2];cell=np.floor(xyz*32).astype(int);noise=((cell[...,0]*17+cell[...,1]*31+cell[...,2]*7)%5-2)*.015
  if d['name']=='delivery_box':base=np.array([190,163,114]);shade=1 if d['normal'][2]>.5 else (.94 if d['normal'][1]<-.5 else .88)
  else:base=np.array([94,77,52]);shade=1 if d['normal'][2]>.5 else .90
  out[sy,sx]=np.rint(base[None,:]*shade*(1+noise[inside,None])).astype('uint8')
for _ in range(4):
 new=filled.copy()
 for dy,dx in [(1,0),(-1,0),(0,1),(0,-1)]:
  take=~new&np.roll(filled,(dy,dx),(0,1));out[take]=np.roll(out,(dy,dx),(0,1))[take];new[take]=True
 filled=new
assert np.array_equal(out[300:],old[300:]);Image.fromarray(out).save(E/'kart_v6d.png')
(E/'texture-validation.json').write_text(json.dumps(dict(size=[AW,H],original_occupied_region_byte_identical=True,changed_pixels=int(np.any(out!=old,axis=2).sum()),carton_base=[190,163,114],carton_noise_percent=3,carton_texels_per_m=32,source_sha256=hashlib.sha256(src.read_bytes()).hexdigest()),indent=2));print('TEXTURE_READY')
