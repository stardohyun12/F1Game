from pathlib import Path
from PIL import Image
import numpy as np,json,hashlib
R=Path(__file__).resolve().parents[3];W=Path(__file__).parent;E=R/'art/export/kart-v6c';V=R/'art/_review/2026-10-27-kart-v6c'
E.mkdir(parents=True,exist_ok=True);V.mkdir(parents=True,exist_ok=True)
oldsmall=np.array(Image.open(R/'art/export/kart-v6/kart_v6_baked32.png').convert('RGB'))
old=np.array(Image.fromarray(oldsmall).resize((4096,1024),Image.Resampling.NEAREST)).astype(float)
b=np.array(Image.open(R/'art/export/kart-v6b/kart_v6b.png').convert('RGB'));mask=np.load(R/'art/source/kart-v6b/group-mask.npy');out=b.copy();weights=np.array([.2126,.7152,.0722]);groups=['plastic','tire','metal','seat','warm','plate'];stats={}
for i,g in enumerate(groups[:-1]):
 m=mask==i;x=old[m]@weights;target=b[m].mean(0);mean=target@weights;y=x/x.mean()*mean
 # Preserve source relative luminance contrast; reduce chroma only to remain in RGB gamut.
 y=np.minimum(y,250);chroma=target-mean
 if g in ['tire','metal']:rgb=np.repeat(y[:,None],3,axis=1)
 else:
  f=np.ones(len(y))
  for c in chroma:
   if c>0:f=np.minimum(f,(255-y)/c)
   elif c<0:f=np.minimum(f,y/-c)
  rgb=y[:,None]+chroma[None,:]*f[:,None]
 out[m]=np.clip(np.rint(rgb),0,255).astype('uint8');z=out[m]@weights
 # Run lengths are measured in the ORIGINAL atlas texels, excluding group boundaries.
 sm=mask[2::4,2::4];lum=oldsmall@weights;runs=[]
 for row,ids in zip(lum,sm):
  run=0;prev=None
  for a,gid in zip(row,ids):
   if gid==i and a==prev:run+=1
   else:
    if run:runs.append(run)
    run=1 if gid==i else 0
   prev=a if gid==i else None
  if run:runs.append(run)
 stats[g]=dict(v6_mean=float(x.mean()),v6b_mean=float(mean),v6c_mean=float(z.mean()),v6_relative_std=float(x.std()/x.mean()),v6c_relative_std=float(z.std()/z.mean()),luminance_correlation=float(np.corrcoef(x,z)[0,1]),v6_relative_p10_p90=(np.percentile(x,[10,90])/x.mean()).tolist(),v6c_relative_p10_p90=(np.percentile(z,[10,90])/z.mean()).tolist(),source_horizontal_run_texels_p50_p90=np.percentile(runs,[50,90]).tolist(),rgb_spread_max=int(np.ptp(out[m].astype(int),axis=1).max()),gamut_luminance_cap_pixels=int((x/x.mean()*mean>250).sum()))
 assert g not in ['tire','metal'] or np.all(out[m,0]==out[m,1]) and np.all(out[m,1]==out[m,2])
 # Color relation guarantees orange/red/brown hue, never cyan/green, on plastic.
 if g=='plastic':assert np.all(out[m,0]>=out[m,1]) and np.all(out[m,1]>=out[m,2])
# Regenerate island gutters from recolored pixels. Original plate art is retained verbatim.
filled=mask>=0
out[~filled]=24
for _ in range(16):
 new=filled.copy()
 for dy,dx in [(1,0),(-1,0),(0,1),(0,-1)]:
  take=~new&np.roll(filled,(dy,dx),(0,1));out[take]=np.roll(out,(dy,dx),(0,1))[take];new[take]=True
 filled=new
assert np.array_equal(out[mask==5],b[mask==5])
Image.fromarray(out).save(E/'kart_v6c.png');colors=np.unique(out.reshape(-1,3),axis=0)
(E/'palette-v6c.gpl').write_text('GIMP Palette\nName: v6c material constrained luminance transfer\nColumns: 8\n#\n'+''.join(f'{r} {g} {b} c{i}\n' for i,(r,g,b) in enumerate(colors)),encoding='utf-8')
files=[R/'art/export/kart-v6/kart_v6.fbx',R/'art/export/kart-v6/kart_v6_baked32.png',R/'art/export/kart-v6b/kart_v6b.fbx',R/'art/export/kart-v6b/kart_v6b.png']
result=dict(method='original v6 luminance at original UV positions, normalized by material mean; v6b mean color; chroma gamut compression; nearest 4x preserves source block scale',groups=stats,texture_size=[4096,1024],used_colors=len(colors),plate_identical_to_v6b=True,source_hashes={str(p.relative_to(R)):hashlib.sha256(p.read_bytes()).hexdigest() for p in files})
(E/'texture-validation.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2))
# Reuse verified same-geometry exporter and all camera settings, with three material variants.
s=(R/'art/source/kart-v6b/export_render.py').read_text(encoding='utf-8-sig').replace('kart-v6b','kart-v6c').replace('kart_v6b','kart_v6c').replace('v6b_preview','v6c_preview')
s=s.replace("for tag,mat in [('v6',old),('v6b',m)]:","for tag,mat in [('v6',old),('v6b',bmat),('v6c',m)]:")
s=s.replace("sc.render.engine='CYCLES'","bmat=material(R/'art/export/kart-v6b/kart_v6b.png','v6b_preview')\nsc.render.engine='CYCLES'")
(W/'export_render.py').write_text(s,encoding='utf-8')
