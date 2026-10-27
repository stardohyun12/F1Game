from pathlib import Path
from PIL import Image
import numpy as np,json,hashlib
R=Path(__file__).resolve().parents[3];W=Path(__file__).parent;E=R/'art/export/kart-v6b';S=R/'art/source/kart-v6'
def read(p):return np.array(Image.open(p).convert('RGB').resize((4096,1024),Image.Resampling.NEAREST))
old=read(R/'art/export/kart-v6/kart_v6_baked32.png');H,AW=old.shape[:2]
raw=read(S/'raw.png');recon=read(W/'reconstructed.png');neutral=read(W/'neutral_sky.png')
groups=['plastic','tire','metal','seat','warm','plate'];mask=np.full((H,AW),-1,np.int8);out=np.full_like(old,24);filled=np.zeros((H,AW),bool)
base={'plastic':(174,71,49),'tire':(29,29,29),'metal':(65,65,65),'seat':(69,60,54),'warm':(123,94,66)}
ramps={g:np.array([[np.rint(np.array(c)*s*n).astype('uint8') for n in [.96,1,1.04]] for s in [.70,.90,1.10]]) for g,c in base.items()}
light=np.array([-.7,-.65,.45]);light/=np.linalg.norm(light)
for d in json.loads((W/'triangles.json').read_text()):
 q=np.array(d['uv'])*[AW,H];q[:,1]=H-q[:,1];a,b,c=q
 mn=np.maximum(0,np.floor(q.min(0)).astype(int));mx=np.minimum([AW-1,H-1],np.ceil(q.max(0)).astype(int));yy,xx=np.mgrid[mn[1]:mx[1]+1,mn[0]:mx[0]+1];x=xx+.5;y=yy+.5
 den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
 if abs(den)<1e-10:continue
 aa=((b[1]-c[1])*(x-c[0])+(c[0]-b[0])*(y-c[1]))/den;bb=((c[1]-a[1])*(x-c[0])+(a[0]-c[0])*(y-c[1]))/den;cc=1-aa-bb;inside=(aa>=-1e-6)&(bb>=-1e-6)&(cc>=-1e-6)
 sy,sx=yy[inside],xx[inside];g=d['group'];mask[sy,sx]=groups.index(g);filled[sy,sx]=True
 if g=='plate':
  p=np.array(d['p']);xyz=aa[...,None]*p[0]+bb[...,None]*p[1]+cc[...,None]*p[2];st=np.zeros((*aa.shape,2))
  if d['name']=='front_07':
   st[...,1]=(.61-xyz[...,1])/.26;st[...,0]=.5-xyz[...,0]/(2*(.125-.02*st[...,1]))
  else:st[...,0]=(xyz[...,0]+.18)/.26;st[...,1]=(xyz[...,2]-.33)/.17
  stamp=np.array(Image.open(S/'surfaces/number07.png').convert('RGB'));tx=np.clip((st[...,0]*80).astype(int),0,79);ty=np.clip(((1-st[...,1])*64).astype(int),0,63)
  out[sy,sx]=np.where((stamp[ty[inside],tx[inside],0]>120)[:,None],[238,238,238],[24,24,24]);continue
 p=np.array(d['p']);xyz=aa[...,None]*p[0]+bb[...,None]*p[1]+cc[...,None]*p[2];cell=np.floor(xyz*32/3)*3/32
 value=np.sin(cell[...,0]*13+cell[...,2]*7)*np.cos(cell[...,1]*9-cell[...,2]*5)
 ni=np.where(value>.65,2,np.where(value<-.65,0,1));nl=float(np.dot(d['normal'],light));shade=2 if nl>.4 else (1 if nl>-.35 else 0)
 out[sy,sx]=ramps[g][shade,ni[inside]]
# Bleed existing islands into gutters without changing original atlas or UV.
for _ in range(4):
 new=filled.copy()
 for dy,dx in [(1,0),(-1,0),(0,1),(0,-1)]:
  take=~new&np.roll(filled,(dy,dx),(0,1));out[take]=np.roll(out,(dy,dx),(0,1))[take];new[take]=True
 filled=new
Image.fromarray(out).save(E/'kart_v6b.png');np.save(W/'group-mask.npy',mask)
oldpal=[tuple(map(int,l.split()[:3])) for l in (R/'art/export/scene-a-v5b-garage/palette32.gpl').read_text().splitlines() if l[:1].isdigit()]
newpal=[]
for r in ramps.values():
 for color in r.reshape(-1,3):
  t=tuple(map(int,color))
  if t not in oldpal+newpal:newpal.append(t)
for c in [(238,238,238),(24,24,24)]:
 if c not in oldpal+newpal:newpal.append(c)
(E/'palette-v6b.gpl').write_text('GIMP Palette\nName: V5 original 32 + material-specific neutral/red ramps\nColumns: 8\n#\n'+''.join(f'{r} {g} {b} '+('v5' if i<32 else 'v6b')+f'_{i}\n' for i,(r,g,b) in enumerate(oldpal+newpal)),encoding='utf-8')
def stat(a,m):
 v=a[m].astype(float);avg=v.mean(0);return dict(pixels=len(v),mean_rgb=np.round(avg,2).tolist(),mean_G_minus_R=round(float((v[:,1]-v[:,0]).mean()),2),mean_B_minus_R=round(float((v[:,2]-v[:,0]).mean()),2),cool_pixel_percent=round(float(np.mean(np.maximum(v[:,1],v[:,2])-v[:,0]>=5)*100),2),rgb_spread_max=int(np.max(v.max(1)-v.min(1))))
results={}
for i,g in enumerate(groups):
 m=mask==i;results[g]={k:stat(a,m) for k,a in [('v6_raw',raw),('reconstructed',recon),('neutral_sky',neutral),('v6_png',old),('v6b_png',out)]}
pal=np.array(oldpal);probes=[]
for grey in [24,32,48,64,80,96,128]:
 c=pal[((pal-grey)**2).sum(1).argmin()];probes.append(dict(input=[grey]*3,nearest=c.tolist(),G_minus_R=int(c[1]-c[0])))
valid=mask>=0
result=dict(groups=results,neutral_gray_quantization_probes=probes,reconstruction_raw_MAE=float(np.abs(raw[valid].astype(float)-recon[valid]).mean()),neutral_sky_RGB=[.30*.2126+.52*.7152+.65*.0722]*3,original_sky_RGB=[.30,.52,.65],lighting_note='neutral sky keeps Rec709 luminance; source textures, sun, samples and geometry unchanged',new_palette_size=len(oldpal+newpal),new_used_colors=len(np.unique(out.reshape(-1,3),axis=0)),noise_scalar=[.96,1,1.04],shade_levels=[.70,.90,1.10],source_hashes={str(p.relative_to(R)):hashlib.sha256(p.read_bytes()).hexdigest() for p in [R/'art/export/kart-v6/kart_v6.fbx',R/'art/export/kart-v6/kart_v6_baked32.png']})
assert all(results[g]['v6b_png']['rgb_spread_max']==0 for g in ['tire','metal'])
(E/'color-analysis.json').write_text(json.dumps(result,indent=2));print(json.dumps(dict(reproduction_MAE=result['reconstruction_raw_MAE'],palette=result['new_palette_size'],used=result['new_used_colors'],tire=results['tire'],metal=results['metal'],probes=probes),indent=2))


