"""Deterministic UV raster painting, no generative image model; original UV/alpha retained."""
from pathlib import Path
import json, hashlib, shutil
import numpy as np
from PIL import Image
R=Path(__file__).resolve().parents[3]; SRC=R/'art/export/scene-a2-kit'; OUT=R/'art/export/scene-a2-kit-style'; WORK=Path(__file__).parent
palette=[]
for line in (R/'art/palette/style32.gpl').read_text(encoding='utf-8-sig').splitlines():
 parts=line.split()
 if len(parts)>=3 and all(x.isdigit() for x in parts[:3]): palette.append(tuple(map(int,parts[:3])))
P=np.array(palette,dtype=np.uint8); assert len(P)==32
shutil.copyfile(R/'art/palette/style32.gpl',OUT/'style32-snapshot.gpl')
def nearest(rgb):return int(np.argmin(np.sum((P.astype(float)-rgb)**2,axis=1)))
# Match intended RGB, never mutable line numbers in another team's palette.
shade=nearest((78,107,108));foot=nearest((76,79,68))
def ramp(mid,bright):return [nearest(mid),nearest(bright),shade,foot]
ramps={'drum_01':ramp((180,79,58),(191,101,64)),'drum_02':ramp((140,127,111),(189,153,101)),'drum_03':ramp((146,176,174),(168,196,188)), 'tires':[nearest((97,97,84)),nearest((107,108,91)),shade,foot], 'pot':ramp((180,79,58),(191,101,64)), 'pole':ramp((140,127,111),(189,153,101)), 'fence':ramp((140,127,111),(175,169,143)), 'bicycle':ramp((146,176,174),(168,196,188)), 'cone':ramp((180,79,58),(222,185,149)), 'bench':ramp((140,127,111),(175,169,143))}
stats=[]
sun=np.array([-.5,-.6,.75]); sun/=np.linalg.norm(sun)
for geo in json.loads((WORK/'geometry.json').read_text()):
 name=geo['name']; d=np.load(WORK/(name+'.npz')); pos,norm,uv=d['p'],d['n'],d['u']
 old=np.array(Image.open(SRC/(name+'.png')).convert('RGBA')); h,w=old.shape[:2]
 idx=ramps.get(name,ramps.get(name.split('_')[0])); colors=P[idx]
 dst=np.zeros_like(old); dst[:,:,:3]=colors[0]; dst[:,:,3]=old[:,:,3]
 covered=np.zeros((h,w),bool); stage=np.full((h,w),-1,np.int8)
 for p,n,u in zip(pos,norm,uv):
  q=u*np.array([w,h]); q[:,1]=h-q[:,1]
  lo=np.maximum(0,np.floor(q.min(0)-1).astype(int)); hi=np.minimum([w-1,h-1],np.ceil(q.max(0)+1).astype(int))
  if np.any(hi<lo):continue
  yy,xx=np.mgrid[lo[1]:hi[1]+1,lo[0]:hi[0]+1]; x=xx+.5;y=yy+.5
  a,b,c=q; den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
  if abs(den)<1e-10:continue
  wa=((b[1]-c[1])*(x-c[0])+(c[0]-b[0])*(y-c[1]))/den
  wb=((c[1]-a[1])*(x-c[0])+(a[0]-c[0])*(y-c[1]))/den; wc=1-wa-wb
  mask=(wa>=-1e-5)&(wb>=-1e-5)&(wc>=-1e-5)
  if not mask.any():continue
  xyz=wa[...,None]*p[0]+wb[...,None]*p[1]+wc[...,None]*p[2]
  normal=wa[...,None]*n[0]+wb[...,None]*n[1]+wc[...,None]*n[2]
  normal/=np.maximum(1e-8,np.linalg.norm(normal,axis=-1,keepdims=True))
  cell=np.floor(xyz*11)/11
  # Broad 0.3--0.6m patches. No random noise or single-pixel stippling.
  blot=np.sin(cell[...,0]*5+cell[...,2]*3)*np.cos(cell[...,1]*4-cell[...,2]*4)*.18
  light=normal@sun+blot
  levels=np.where(light>.53,1,np.where(light>-.18,0,2))
  # One shade per 9.09cm world cell on each original UV triangle.
  cells=np.floor(xyz[mask]*11).astype(np.int32)
  _,groups=np.unique(cells,axis=0,return_inverse=True)
  sums=np.bincount(groups,weights=light[mask]);counts=np.bincount(groups)
  avg=sums/counts; levels[mask]=np.where(avg[groups]>.53,1,np.where(avg[groups]>-.18,0,2))
  # Each original material's contact is a discrete band, not baked smooth AO.
  bottom=geo['bounds_min'][2]; height=geo['bounds_max'][2]-bottom
  # Preserve semantic material masses, without copying photographic noise.
  srcsamples=old[yy[mask],xx[mask],:3]; median=np.median(srcsamples,axis=0)
  if name.startswith('bicycle') and median.max()<115:
   levels=np.where(light>.3,2,3)
  if name.startswith('pot'):
   # Pot interior: close to centre and above base, independent of baked photo AO.
   cx=(geo['bounds_min'][0]+geo['bounds_max'][0])/2;cy=(geo['bounds_min'][1]+geo['bounds_max'][1])/2
   rx=(geo['bounds_max'][0]-geo['bounds_min'][0])/2;ry=(geo['bounds_max'][1]-geo['bounds_min'][1])/2
   inner=((xyz[...,0]-cx)/rx)**2+((xyz[...,1]-cy)/ry)**2<.56
   levels=np.where(inner&(xyz[...,2]>bottom+height*.45),2,levels)
  if name.startswith('drum'):
   levels=np.where(xyz[...,2]>bottom+height*.94,2,levels)
  if name.startswith('cone'):
   levels=np.where((xyz[...,2]>bottom+height*.42)&(xyz[...,2]<bottom+height*.68),1,levels)
  levels=np.where(xyz[...,2]<bottom+min(.09,height*.09),3,levels)
  sy,sx=yy[mask],xx[mask]; dst[sy,sx,:3]=colors[levels[mask]]; covered[sy,sx]=True; stage[sy,sx]=levels[mask]
 # Bleed nearest occupied colour 4 pixels into atlas gutters, keeping original alpha.
 for _ in range(4):
  filled=covered.copy()
  for dy,dx in [(1,0),(-1,0),(0,1),(0,-1)]:
   srcmask=np.roll(covered,(dy,dx),(0,1)); take=~filled&srcmask
   dst[take,:3]=np.roll(dst[:,:,:3],(dy,dx),(0,1))[take]; filled[take]=True
  covered=filled
 Image.fromarray(dst).save(OUT/(name+'.png'))
 stats.append(dict(name=name,size=[w,h],triangles=geo['tris'],world_cell_m=1/11,texels_per_m=11,colors_rgb=[list(map(int,c)) for c in colors],color_count=len(np.unique(dst[:,:,:3].reshape(-1,3),axis=0)),shade_steps=3,contact_steps=1,alpha_identical=bool(np.array_equal(dst[:,:,3],old[:,:,3])),source_png_sha256=hashlib.sha256((SRC/(name+'.png')).read_bytes()).hexdigest(),fbx_sha256=hashlib.sha256((SRC/(name+'.fbx')).read_bytes()).hexdigest()))
 print(name,flush=True)
# 32px square, 32/11m world repeat; large seamless patches, no tiny specks.
for name,ids in [('bush_style',ramp((102,126,80),(149,156,112))),('mound_style',ramp((126,134,98),(149,156,112))),('rock_style',ramp((140,127,111),(175,169,143)))]:
 yy,xx=np.mgrid[:32,:32]; x=2*np.pi*xx/32; y=2*np.pi*yy/32
 field=np.sin(x+.4)*np.cos(y)+.32*np.sin(2*x-y)
 levels=np.where(field>.40,1,np.where(field<-.4,2,0)); arr=P[ids][levels]
 Image.fromarray(arr).save(OUT/(name+'.png'))
 stats.append(dict(name=name,size=[32,32],texels_per_m=11,repeat_metres=32/11,color_count=3,shade_steps=3,contact_steps=0,contact_note='mesh-local contact band required in shader; tile must not contain repeating contact'))
(OUT/'metrics.json').write_text(json.dumps(stats,ensure_ascii=False,indent=2),encoding='utf-8')
