from pathlib import Path
import json,hashlib
import numpy as np,cv2
from PIL import Image,ImageDraw
R=Path(__file__).resolve().parents[3];W=Path(__file__).parent;E=R/'art/export/q2-garage';B=E/'v1'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
source=R/'F1/Assets/Art/SceneA7/Tex/garage_tin_F2_atlas_clean.png'
a=np.array(Image.open(source).convert('RGBA'));old=np.array(Image.open(B/'garage_baked32.png').convert('RGBA'))
# Preserve q2's already cleaned ceiling; A7 supplies exact red side-wall placement.
roof=np.load(W/'roof-mask.npy');a[roof,:3]=old[roof,:3]
def grain(name,base,size):
 p=R/f'art/source/photos/scene-a/{name}/{name}_1K-JPG_Color.jpg';im=np.array(Image.open(p).convert('RGB'),float);y=im@np.array([.299,.587,.114]);low=cv2.GaussianBlur(y,(0,0),80);g=y/np.maximum(low,1)-1;g=cv2.resize(g,size,interpolation=cv2.INTER_AREA);g=cv2.GaussianBlur(g,(0,0),.65);g=np.clip(g/max(np.percentile(abs(g),85),1e-6),-1,1)
 return np.rint(np.array(base)*(1+.03*g[:,:,None])).clip(0,255).astype('uint8'),dict(path=str(p.relative_to(R)),sha256=sha(p),license='CC0-1.0',url='https://ambientcg.com/view?id='+name)
tile,floorsource=grain('Concrete042A',[103,107,107],(256,256));mask=np.zeros(a.shape[:2],bool);h,w=mask.shape
for t in json.loads((W/'floor-jobs.json').read_text()):
 q=np.array(t['uv'])*[w,h];q[:,1]=h-q[:,1];aa,bb,cc=q;lo=np.maximum(0,np.floor(q.min(0)).astype(int));hi=np.minimum([w-1,h-1],np.ceil(q.max(0)).astype(int));yy,xx=np.mgrid[lo[1]:hi[1]+1,lo[0]:hi[0]+1];x=xx+.5;y=yy+.5;den=(bb[1]-cc[1])*(aa[0]-cc[0])+(cc[0]-bb[0])*(aa[1]-cc[1])
 if abs(den)<1e-12:continue
 u=((bb[1]-cc[1])*(x-cc[0])+(cc[0]-bb[0])*(y-cc[1]))/den;v=((cc[1]-aa[1])*(x-cc[0])+(aa[0]-cc[0])*(y-cc[1]))/den;ok=(u>=0)&(v>=0)&(u+v<=1);p=u[:,:,None]*t['world'][0]+v[:,:,None]*t['world'][1]+(1-u-v)[:,:,None]*t['world'][2];tx=np.floor(p[:,:,0]*32).astype(int)%256;ty=np.floor(p[:,:,1]*32).astype(int)%256;a[yy[ok],xx[ok],:3]=tile[ty[ok],tx[ok]];mask[yy[ok],xx[ok]]=True
for _ in range(2):
 n=mask.copy()
 for dy,dx in [(1,0),(-1,0),(0,1),(0,-1)]:
  m=~n&np.roll(mask,(dy,dx),(0,1));a[m,:3]=np.roll(a,(dy,dx),(0,1))[m,:3];n[m]=True
 mask=n
Image.fromarray(a).save(E/'garage_baked32.png');Image.fromarray(a).save(E/'garage_tin_F2_atlas.png')
# Alternate F3 stays optional, but must not bring old floor circles back.
f3=np.array(Image.open(B/'garage_tin_F3_atlas.png').convert('RGBA'));f3[mask,:3]=a[mask,:3];Image.fromarray(f3).save(E/'garage_tin_F3_atlas.png')
dirt=np.array(Image.open(B/'dirt_baked32.png').convert('RGBA'));dt,dirtsource=grain('Ground081',[132,124,105],(128,128));hh,ww=dirt.shape[:2];dirt[:,:,:3]=np.tile(dt,(int(np.ceil(hh/128)),int(np.ceil(ww/128)),1))[:hh,:ww];Image.fromarray(dirt).save(E/'dirt_baked32.png')
poster=Image.open(B/'garage_festival.png').convert('RGB');d=ImageDraw.Draw(poster);d.rectangle((2,2,125,125),outline=(115,91,65),width=2)
for x,y in [(5,5),(122,5),(5,122),(122,122)]:d.ellipse((x-2,y-2,x+2,y+2),fill=(142,67,48));d.point((x-1,y-1),fill=(201,166,117))
poster.save(E/'garage_festival.png')
fp=a[mask,:3];dp=dirt[dirt[:,:,3]>0,:3]
j=dict(A7_reference=str(source.relative_to(R)),A7_sha256=sha(source),floor_pixels=int(mask.sum()),floor_RGB_min=fp.min(0).tolist(),floor_RGB_max=fp.max(0).tolist(),dirt_RGB_min=dp.min(0).tolist(),dirt_RGB_max=dp.max(0).tolist(),floor_navy_pixels=int(((fp[:,2]>fp[:,0]+12)&(fp.mean(1)<90)).sum()),dirt_red_speck_pixels=int(((dp[:,0]>dp[:,1]+35)&(dp[:,0]>150)).sum()),sources=[floorsource,dirtsource],light_bake=False)
assert j['floor_navy_pixels']==0 and j['dirt_red_speck_pixels']==0
np.save(W/'floor-mask-v2.npy',mask);(E/'texture-fix-v2.json').write_text(json.dumps(j,indent=2));print(j)
