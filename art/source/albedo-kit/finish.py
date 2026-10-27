from pathlib import Path
from PIL import Image
import numpy as np,json,cv2,hashlib
R=Path(__file__).resolve().parents[3];W=Path(__file__).parent;E=R/'art/export/scene-albedo-kit';jobs=json.loads((W/'jobs.json').read_text());stats={};L=np.array([.2126,.7152,.0722])
def read(p):return np.array(Image.open(p).convert('RGBA'))
def save(name,a,before,method):
 Image.fromarray(a.astype('uint8')).save(E/name);b=read(before);assert a.shape==b.shape;assert np.array_equal(a[:,:,3],b[:,:,3]);stats[name]=dict(source=str(Path(before).relative_to(R)),size=[a.shape[1],a.shape[0]],alpha_identical=True,method=method,source_sha256=hashlib.sha256(Path(before).read_bytes()).hexdigest())
def palette_reduce(a,colors=64):
 # Per-texture exact palette; add original material colours instead of sending greys into teal bins.
 rgb=Image.fromarray(a[:,:,:3].astype('uint8')).quantize(colors=colors,method=Image.Quantize.MEDIANCUT,dither=Image.Dither.NONE).convert('RGB');return np.dstack([np.array(rgb),a[:,:,3]])
for d in jobs:
 name=d['name'];b=read(d['before'])
 if name.startswith(('tires_','transformer_')):a=b.copy()
 else:a=read(W/(name+'_color.png'));a[:,:,3]=b[:,:,3]
 if name.startswith('tires_'):
  # PO1 104/70/40 assigned by face normal: remove only that face-light ramp, keep tread ink at 40.
  a[np.all(a[:,:,:3]==104,axis=2),:3]=70
  # Bottom-facing 40 was orientation shading, while side-facing 40 is drawn tread.
  for t in json.loads((R/'art/source/po1'/(name+'.json')).read_text()):
   if t['normal'][2]>=-.25:continue
   q=np.array(t['uv'])*[a.shape[1],a.shape[0]];q[:,1]=a.shape[0]-q[:,1];mask=np.zeros(a.shape[:2],np.uint8);cv2.fillConvexPoly(mask,np.rint(q).astype('int32'),1);m=(mask>0)&np.all(a[:,:,:3]==40,axis=2);a[m,:3]=70
 elif name=='transformer_01':
  for base in [(147,164,165),(72,94,101),(100,124,130)]:
   for factor in [.98,1,1.02]:
    c=np.rint(np.array(base)*factor).astype(int);m=np.all(a[:,:,:3]==c,axis=2);a[m,:3]=np.rint(np.array([100,124,130])*factor)
  for c in [(160,173,169),(50,68,74)]:a[np.all(a[:,:,:3]==c,axis=2),:3]=[100,124,130]
 elif name.startswith('autumn_tree'):
  # Leaf albedo is the authored source picture, not a flat average of the baked leaf lump.
  trunk=np.load(R/'art/source/po1'/(name+'-mask.npy'));a[trunk,:3]=[111,80,59]
  # Retain PO1 bark's existing narrow streak positions; suppress normal-dependent 3-level shading.
  old=b[:,:,:3].astype(float);colors=np.array([(80,60,49),(111,80,59),(142,105,73)]);delta=np.min(np.linalg.norm(old[:,:,None,:]-colors[None,None,:,:],axis=3),axis=2);a[trunk&(delta>1),:3]=[107,77,57]
  a=palette_reduce(a,96)
 else:
  a=palette_reduce(a,64)
 save(d['file'],a,d['before'],'Original Base Color -> emission-only UV transfer; PO1 painted features restored; no lighting/AO/tone lift')
# Flat photo tile sources already contain perspective/crop/illumination flattening, but no Blender light.
photo=R/'art/export/scene-a6-photo-kit'
for d in json.loads((R/'art/source/photo-kit/manifest.json').read_text()):
 if d['kind']!='tile':continue
 name=d['name'];b=read(photo/(name+'.png'));a=read(R/'art/source/photo-kit/source'/(name+'.png'));a[:,:,3]=b[:,:,3]
 if name.startswith('stone'):
  y=a[:,:,:3]@L;a[:,:,:3]=np.clip(y[:,:,None]+np.array([6,0,-8]),0,255)
 a=palette_reduce(a,32);a[:,0]=a[:,-1]=a[:,0];a[0]=a[-1]=a[0];save(name+'.png',a,photo/(name+'.png'),'Pre-light, perspective-corrected photo tile; no sun/sky/AO')
# Reconstruct the v6e current atlas from its known material groups, preserving plate and box/strap positions.
p=R/'art/export/kart-v6e/kart_v6e.png';b=read(p);a=b.copy();source=read(W/'kart_source_color.png');mask=np.load(R/'art/source/kart-v6b/group-mask.npy');groups=['plastic','tire','metal','seat','warm','plate'];kartstats={}
for i,g in enumerate(groups[:-1]):
 m=mask==i;v=b[m,:3].astype(float);base=np.mean(v,axis=0);base=np.round(base)
 if g in ['tire','metal']:base[:]=round(base@L)
 # Existing grain positions survive as a bounded scalar; material average stays the approved v6e colour.
 y=source[m,:3]@L;grain=np.clip((y-np.median(y))/max(y.std()*4,1),-1,1)*.04;grain=np.round(grain/.02)*.02;a[m,:3]=np.clip(np.rint(base*(1+grain[:,None])),0,255);kartstats[g]=dict(base=base.tolist(),variation_percent=4,grain_source='unlit original material image, not baked luminance')
def raster(t,w,h):
 q=np.array(t['uv'])*[w,h];q[:,1]=h-q[:,1];aa,bb,cc=q;lo=np.maximum(0,np.floor(q.min(0)).astype(int));hi=np.minimum([w-1,h-1],np.ceil(q.max(0)).astype(int));yy,xx=np.mgrid[lo[1]:hi[1]+1,lo[0]:hi[0]+1];x=xx+.5;y=yy+.5;den=(bb[1]-cc[1])*(aa[0]-cc[0])+(cc[0]-bb[0])*(aa[1]-cc[1])
 if abs(den)<1e-12:return np.array([],int),np.array([],int)
 u=((bb[1]-cc[1])*(x-cc[0])+(cc[0]-bb[0])*(y-cc[1]))/den;v=((cc[1]-aa[1])*(x-cc[0])+(aa[0]-cc[0])*(y-cc[1]))/den;ok=(u>=0)&(v>=0)&(u+v<=1);return yy[ok],xx[ok]
occupied=mask>=0
for t in json.loads((R/'art/source/kart-v6d/texture-jobs.json').read_text()):
 sy,sx=raster(t,a.shape[1],a.shape[0]);occupied[sy,sx]=True
 if t['name']=='seat_back':
  base=np.array(kartstats['seat']['base']);q=np.array(t['uv'])*[a.shape[1],a.shape[0]];q[:,1]=a.shape[0]-q[:,1];weights=np.c_[sx+.5,sy+.5,np.ones(len(sx))]@np.linalg.inv(np.c_[q,np.ones(3)]);uv=weights@np.array(t['source_uv']);tx=np.clip((uv[:,0]*a.shape[1]).astype(int),0,a.shape[1]-1);ty=np.clip(((1-uv[:,1])*a.shape[0]).astype(int),0,a.shape[0]-1);v=source[ty,tx,:3]@L;grain=np.clip((v-np.median(v))/max(v.std()*4,1),-1,1)*.04;a[sy,sx,:3]=np.rint(base*(1+np.round(grain/.02)[:,None]*.02))
 else:
  shade=(1 if t['normal'][2]>.5 else (.94 if t['normal'][1]<-.5 else .88)) if t['name']=='delivery_box' else (1 if t['normal'][2]>.5 else .90)
  a[sy,sx,:3]=np.rint(b[sy,sx,:3]/shade)
for _ in range(16):
 new=occupied.copy()
 for dy,dx in [(1,0),(-1,0),(0,1),(0,-1)]:
  take=~new&np.roll(occupied,(dy,dx),(0,1));a[take]=np.roll(a,(dy,dx),(0,1))[take];new[take]=True
 occupied=new
assert np.array_equal(a[mask==5],b[mask==5]);save('kart_v6e.png',a,p,'Per-material lighting ramp removed, v6e mean hue preserved, grain limited to +/-4%; plate identical; carton shading divided out')
(W/'kart-decisions.json').write_text(json.dumps(kartstats,indent=2));(W/'processing.json').write_text(json.dumps(stats,indent=2));print('FINISHED',len(stats),flush=True)
