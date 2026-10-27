from pathlib import Path
from PIL import Image
import numpy as np,json,cv2
R=Path(__file__).resolve().parents[3];W=Path(__file__).parent;E=R/'art/export/scene-albedo-kit';L=np.array([.2126,.7152,.0722]);records=[]
def linear(a):
 s=a/255;return np.where(s<=.04045,s/12.92,((s+.055)/1.055)**2.4)
def raster(t,w,h):
 q=np.array(t['uv'])*[w,h];q[:,1]=h-q[:,1];aa,bb,cc=q;lo=np.maximum(0,np.floor(q.min(0)).astype(int));hi=np.minimum([w-1,h-1],np.ceil(q.max(0)).astype(int));yy,xx=np.mgrid[lo[1]:hi[1]+1,lo[0]:hi[0]+1];x=xx+.5;y=yy+.5;den=(bb[1]-cc[1])*(aa[0]-cc[0])+(cc[0]-bb[0])*(aa[1]-cc[1])
 if abs(den)<1e-12:return np.array([],int),np.array([],int)
 u=((bb[1]-cc[1])*(x-cc[0])+(cc[0]-bb[0])*(y-cc[1]))/den;v=((cc[1]-aa[1])*(x-cc[0])+(aa[0]-cc[0])*(y-cc[1]))/den;ok=(u>=0)&(v>=0)&(u+v<=1);return yy[ok],xx[ok]
for i in range(1,6):
 name=f'autumn_tree_{i:02d}';old=np.array(Image.open(R/'art/export/scene-a2-kit-po1'/(name+'.png')).convert('RGBA'));out=np.array(Image.open(E/(name+'.png')).convert('RGBA'));h,w=old.shape[:2];albedo=np.array(Image.open(W/(name+'_color.png')).convert('RGB'),float)
 raw=np.array(Image.open(R/'art/models/garage_scene_a2_kit'/(name+'_raw.png')).convert('RGB').resize((w,h),Image.Resampling.BILINEAR),float);ao=np.array(Image.open(R/'art/models/garage_scene_a2_kit'/(name+'_ao.png')).convert('L').resize((w,h),Image.Resampling.BILINEAR),float)/255
 srcY=linear(albedo)@L;litY=linear(raw)@L*(.70+.30*ao);tri=json.loads((R/'art/source/po1'/(name+'.json')).read_text());face=[]
 for t in tri:
  if t['component']==0:continue
  sy,sx=raster(t,w,h)
  if not len(sy):continue
  good=(old[sy,sx,3]>128)&(srcY[sy,sx]>.004);ratio=float(np.sum(litY[sy[good],sx[good]])/max(np.sum(srcY[sy[good],sx[good]]),.00001)) if good.any() else 1;face.append((sy,sx,ratio))
 ref=float(np.median([r for _,_,r in face]));gains=[];leafmask=np.zeros((h,w),bool)
 for sy,sx,r in face:
  gain=float(np.clip((ref/max(r,.001))**(1/2.2),.96,1.04));gains.append(gain);out[sy,sx,:3]=np.clip(np.rint(old[sy,sx,:3].astype(float)*gain),0,255);leafmask[sy,sx]=True
 # Bleed only into non-leaf gutters; alpha and within-face painted boundaries are retained.
 filled=leafmask.copy();trunk=np.load(R/'art/source/po1'/(name+'-mask.npy'))
 for _ in range(3):
  new=filled.copy()
  for dy,dx in [(1,0),(-1,0),(0,1),(0,-1)]:
   take=~new&~trunk&np.roll(filled,(dy,dx),(0,1));out[take,:3]=np.roll(out,(dy,dx),(0,1))[take,:3];new[take]=True
  filled=new
 out[:,:,3]=old[:,:,3];rgb=Image.fromarray(out[:,:,:3]).quantize(colors=96,dither=Image.Dither.NONE).convert('RGB');out[:,:,:3]=np.array(rgb);Image.fromarray(out).save(E/(name+'.png'))
 records.append(dict(name=name,alpha_identical=bool(np.array_equal(out[:,:,3],old[:,:,3])),leaf_face_count=len(face),irradiance_reference=ref,gain_min=min(gains),gain_max=max(gains),method='Retain current PO1/R2 painted leaf colour regions; compare face-mean original diffuse+AO irradiance against colour-only source; conservative correction limited to +/-4% to avoid changing authored leaf clumps',limitation='R2 palette and smoothing are irreversible. Exact removal of historical lighting inside the painted leaf clumps cannot be verified. Strong inverse correction created triangle artifacts and was rejected. Clump contrast intentionally retained.'))
(W/'tree-decisions.json').write_text(json.dumps(records,indent=2));print('TREES',records)
