from pathlib import Path
from PIL import Image
import numpy as np,json,colorsys,hashlib
R=Path(__file__).resolve().parents[3];W=Path(__file__).parent;E=R/'art/export/scene-a2-kit-po1';K=R/'art/export/kart-v6e';V=R/'art/_review/2026-10-27-po1'
for p in [E,K,V]:p.mkdir(parents=True,exist_ok=True)
names=['transformer_01','tires_01','tires_02']+[f'autumn_tree_{i:02d}' for i in range(1,6)];stats={};sources={}
for name in names:
 is_tree=name.startswith('autumn');src=R/'art/export/scene-a2-kit'/(name+'.png')
 if is_tree:
  active=R/'F1/Assets/Art/SceneA3/Gen'/(name+'_r2.png');soft=R/'F1/Assets/Art/SceneA2/Gen'/(name+'_soft.png');src=active if active.exists() else soft if soft.exists() else src
 else:
  active=R/'F1/Assets/Art/SceneA6d/Tex'/(name+'_mono.png')
  if active.exists():src=active
 sources[name]=str(src.relative_to(R));old=np.array(Image.open(src).convert('RGBA'));out=old.copy();H,AW=old.shape[:2];mask=np.zeros((H,AW),bool);leafmask=np.zeros_like(mask)
 for d in json.loads((W/(name+'.json')).read_text()):
  q=np.array(d['uv'])*[AW,H];q[:,1]=H-q[:,1];a,b,c=q;mn=np.maximum(0,np.floor(q.min(0)).astype(int));mx=np.minimum([AW-1,H-1],np.ceil(q.max(0)).astype(int));yy,xx=np.mgrid[mn[1]:mx[1]+1,mn[0]:mx[0]+1];x=xx+.5;y=yy+.5;den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
  if abs(den)<1e-10:continue
  aa=((b[1]-c[1])*(x-c[0])+(c[0]-b[0])*(y-c[1]))/den;bb=((c[1]-a[1])*(x-c[0])+(a[0]-c[0])*(y-c[1]))/den;cc=1-aa-bb;inside=(aa>=-1e-6)&(bb>=-1e-6)&(cc>=-1e-6);sy,sx=yy[inside],xx[inside]
  if is_tree and d['component']!=0:leafmask[sy,sx]=True;continue
  p=np.array(d['p']);xyz=(aa[...,None]*p[0]+bb[...,None]*p[1]+cc[...,None]*p[2])[inside];n=np.array(d['normal']);n/=np.linalg.norm(n);mask[sy,sx]=True
  if is_tree:
   light=np.dot(n,[-.5,-.45,.74]);base=np.array([(80,60,49),(111,80,59),(142,105,73)])[2 if light>.35 else 0 if light<-.3 else 1];rgb=np.tile(base,(len(sy),1));grain=(np.floor(xyz[:,0]*8)+np.floor(xyz[:,1]*9))%7==0;rgb[grain]=np.rint(rgb[grain]*.96);out[sy,sx,:3]=rgb
  elif name.startswith('tires'):
   value=104 if n[2]>.35 else 40 if n[2]<-.25 else 70
   val=np.full(len(sy),value);tread=((np.floor(xyz[:,0]*22+xyz[:,1]*15+xyz[:,2]*20).astype(int)%7)==0)
   if abs(n[2])<.15:val[tread]=40
   out[sy,sx,:3]=val[:,None]
  else:
   X,Y,Z=xyz.T;top=n[2]>.5
   base=np.array((147,164,165) if top else (100,124,130) if n[1]<-.5 else (72,94,101));rgb=np.tile(base,(len(sy),1))
   if d['component']==0:rgb[:]=[76,83,82]
   else:
    grain=((np.floor(X*16)+np.floor(Y*16)+np.floor(Z*16))%3-1)*.02;rgb=np.rint(rgb*(1+grain[:,None])).astype(int)
    edge=((np.abs(X)>.295)|(np.abs(Y)>.425)) if top else (((np.abs(Y)>.425) if abs(n[0])>.5 else (np.abs(X)>.295))|(Z>1.665)|(Z<.235));rgb[edge]=[160,173,169] if top or n[1]<-.5 else [50,68,74]
    # A large inset door and warning plaque on the front, broad ventilation slots on the side.
    if n[1]<-.5:
     door=(np.abs(X)<.245)&(Z>.32)&(Z<1.58);rgb[door]=[111,134,138];seam=door&((np.abs(X)>.215)|(Z<.35)|(Z>1.55));rgb[seam]=[57,77,83]
     plate=(np.abs(X)<.13)&(Z>1.05)&(Z<1.35);rgb[plate]=[191,170,104]
     u=(X+.13)/.26;v=(1.35-Z)/.30;poly=[(.57,.04),(.22,.56),(.46,.56),(.36,.96),(.80,.40),(.56,.40)];bolt=np.zeros(len(X),bool)
     for (px,py),(qx,qy) in zip(poly,poly[1:]+poly[:1]):
      bolt^=((py>v)!=(qy>v))&(u<(qx-px)*(v-py)/(qy-py+1e-12)+px)
     rgb[plate&bolt]=[63,68,60]
     handle=(X>.15)&(X<.20)&(Z>.77)&(Z<.98);rgb[handle]=[53,69,74]
    if abs(n[0])>.5:
     slots=(np.abs(Y)<.31)&(Z>.55)&(Z<1.30)&((np.floor((Z-.55)/.12)%1)==0)
     slots=(np.abs(Y)<.31)&(Z>.55)&(Z<1.30)&(((Z-.55)%.16)<.05);rgb[slots]=[48,66,73]
   out[sy,sx,:3]=rgb
 # Pad into gutters only; never change leaf pixels or alpha.
 occupied=mask|leafmask;filled=mask.copy()
 for _ in range(8 if is_tree else 2):
  new=filled.copy()
  for dy,dx in [(1,0),(-1,0),(0,1),(0,-1)]:
   take=~occupied&~new&np.roll(filled,(dy,dx),(0,1));out[take,:3]=np.roll(out,(dy,dx),(0,1))[take,:3];new[take]=True
  filled=new
 assert np.array_equal(old[:,:,3],out[:,:,3])
 if is_tree:assert np.array_equal(old[leafmask&~mask],out[leafmask&~mask])
 Image.fromarray(out).save(E/(name+'.png'));np.save(W/(name+'-mask.npy'),mask)
 stats[name]=dict(source=str(src.relative_to(R)),size=[AW,H],changed_pixels=int(np.any(old[:,:,:3]!=out[:,:,:3],axis=2).sum()),edited_surface_pixels=int(mask.sum()),leaf_overlap_pixels=int((leafmask&mask).sum()),alpha_identical=True,leaf_only_pixels_identical=bool(np.array_equal(old[leafmask&~mask],out[leafmask&~mask])),edited_rgb_mean=out[mask,:3].mean(0).tolist(),surface_colors=np.unique(out[mask,:3],axis=0).tolist(),source_sha256=hashlib.sha256(src.read_bytes()).hexdigest())
 if name.startswith('tires'):assert np.all(np.ptp(out[mask,:3],axis=1)==0)
# D7: alter only the existing plastic UVs and their gutters; no changes to tyres/plate/cargo.
src=R/'art/export/kart-v6d/kart_v6d.png';a=np.array(Image.open(src).convert('RGB'));out=a.copy();m=np.load(R/'art/source/kart-v6b/group-mask.npy');plastic=m==0;colors=np.unique(a[plastic],axis=0);mapping={};weights=np.array([.2126,.7152,.0722]);meanY=float((a[plastic]@weights).mean())
for c in colors:
 h,s,v=colorsys.rgb_to_hsv(*(c/255));unit=np.array(colorsys.hsv_to_rgb(.025+.65*h,min(.90,s*1.25+.06),1));Y=np.clip(meanY+1.15*(c@weights-meanY)+2,20,225);rgb=unit*(Y/(unit@weights))
 if rgb.max()>235:rgb=Y+(rgb-Y)*((235-Y)/(rgb.max()-Y))
 mapping[tuple(c)]=np.rint(rgb).astype('uint8')
for c,new in mapping.items():out[plastic&np.all(a==c,axis=2)]=new
filled=plastic.copy()
for _ in range(4):
 new=filled.copy()
 for dy,dx in [(1,0),(-1,0),(0,1),(0,-1)]:
  take=(m<0)&~new&np.roll(filled,(dy,dx),(0,1));out[take]=np.roll(out,(dy,dx),(0,1))[take];new[take]=True
 filled=new
assert np.array_equal(out[(m>0)],a[(m>0)])
Image.fromarray(out).save(K/'kart_v6e.png')
def colorstat(v):
 f=v.astype(float)/255;mx=f.max(1);mn=f.min(1);y=v@np.array([.2126,.7152,.0722]);return dict(mean_saturation=float(np.mean((mx-mn)/np.maximum(mx,.0001))),luma_p10_p90=np.percentile(y,[10,90]).tolist(),mean_rgb=v.mean(0).tolist())
stats['kart']=dict(before=colorstat(a[plastic]),after=colorstat(out[plastic]),other_surface_pixels_identical=True,source_sha256=hashlib.sha256(src.read_bytes()).hexdigest())
(W/'sources.json').write_text(json.dumps(sources,indent=2));(E/'texture-validation.json').write_text(json.dumps(stats,indent=2));(K/'texture-validation.json').write_text(json.dumps(stats['kart'],indent=2));print('PAINTED',[(k,v['leaf_overlap_pixels']) for k,v in stats.items() if k.startswith('autumn')],stats['kart'])
# Same exporter/camera used before, but import current v6d and bind v6e texture.
s=(R/'art/source/kart-v6b/export_render.py').read_text(encoding='utf-8-sig').replace('kart-v6b','kart-v6e').replace('kart_v6b','kart_v6e').replace("R/'art/export/kart-v6/kart_v6.fbx'","R/'art/export/kart-v6d/kart_v6d.fbx'").replace("R/'art/export/kart-v6/kart_v6_baked32.png'","R/'art/export/kart-v6d/kart_v6d.png'").replace("('v6',old),('v6b',m)","('v6d',old),('v6e',m)")
s=s.replace("R/'art/_review/2026-10-27-kart-v6e'","R/'art/_review/2026-10-27-po1'").replace("R/'art/source/kart-v6e/preview.blend'","R/'art/source/po1/kart-preview.blend'")
(W/'kart_render.py').write_text(s,encoding='utf-8')
