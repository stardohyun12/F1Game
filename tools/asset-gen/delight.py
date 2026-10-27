"""Deterministic photo -> unlit material. Python 3.13 / Pillow / NumPy / OpenCV.

CLI: py -3.13 tools/asset-gen/delight.py job.json
quad_px order: TL, TR, BR, BL, in original-image pixels.
physical_m determines output pixels at px_per_m (default 32).
No generative model, stochastic noise, AO, light bake, or automatic crop search.
"""
from pathlib import Path
import argparse, hashlib, json
import cv2
import numpy as np
from PIL import Image

VERSION = '1.0.0'
# Linear sRGB values, physicallybased.info (snapshot 2026-10-28).
PBR = {'asphalt': [0.043, 0.041, 0.040], 'brick': [0.262, 0.095, 0.061],
       'concrete': [0.510]*3, 'plaster': [0.510]*3}
# Style interval, not measured material values. Preserve ordering, allow paint hues.
BOUNDS = {'asphalt': (.025,.080), 'brick': (.090,.250),
          'concrete': (.300,.650), 'plaster': (.280,.650)}
LUMA = np.array([.2126,.7152,.0722])

def linear(a):
    a=np.asarray(a,dtype=np.float64)/255
    return np.where(a<=.04045,a/12.92,((a+.055)/1.055)**2.4)

def srgb(a):
    a=np.clip(a,0,1)
    return np.where(a<=.0031308,a*12.92,1.055*a**(1/2.4)-.055)*255

def sha(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest()

def palette(path):
    colors=[]
    for s in Path(path).read_text(encoding='utf-8-sig').splitlines():
        q=s.split()
        if len(q)>=3 and all(t.isdigit() for t in q[:3]): colors.append(list(map(int,q[:3])))
    if not colors or len(colors)>256: raise ValueError('GPL must contain 1..256 colors')
    return np.array(colors,dtype=np.uint8)

def quantize(a, colors):
    p=np.asarray(colors,dtype=np.float64); a=np.asarray(a)
    # Small chunks bound memory and deterministic first-index ties.
    flat=a.reshape(-1,3); out=np.empty_like(flat,dtype=np.uint8)
    for i in range(0,len(flat),4096):
        d=((flat[i:i+4096,None,:].astype(float)-p[None,:,:])**2).sum(2)
        out[i:i+4096]=p[np.argmin(d,axis=1)]
    return out.reshape(a.shape)

def periodic(a, axes=(0,1), width=8):
    """Symmetric edge crossfade; equal boundary pixels, no copied fake stains."""
    a=np.array(a,dtype=float,copy=True)
    for axis in axes:
        b=np.moveaxis(a,axis,0); n=min(width,len(b)//4)
        for i in range(n):
            t=(1-i/n)**2 * .5
            x=b[i].copy();y=b[-1-i].copy()
            b[i]=(1-t)*x+t*y;b[-1-i]=(1-t)*y+t*x
        a=np.moveaxis(b,0,axis)
    return a

def remove_isolated(a):
    """Remove isolated palette texels using existing neighboring photo colors only."""
    a=a.copy()
    for _ in range(4):
        same=np.zeros(a.shape[:2],bool)
        for yy in [-1,0,1]:
            for xx in [-1,0,1]:
                if xx or yy:same|=np.all(a==np.roll(a,(yy,xx),(0,1)),axis=2)
        mask=~same
        if not mask.any():break
        a[mask]=np.roll(a,1,axis=0)[mask]
    return a

def process(job, root=Path('.'), colors=None):
    cv2.setNumThreads(1)
    path=root/job['input']; im=np.array(Image.open(path).convert('RGB'))
    h,w=im.shape[:2]; q=np.asarray(job['quad_px'],dtype=np.float32)
    if q.shape!=(4,2) or not np.isfinite(q).all(): raise ValueError('four finite corners required')
    if (q<0).any() or (q[:,0]>w-1).any() or (q[:,1]>h-1).any(): raise ValueError('quad outside source')
    if not cv2.isContourConvex(q) or abs(cv2.contourArea(q))<4: raise ValueError('convex ordered quad required')
    physical=np.asarray(job['physical_m'],float);ppm=int(job.get('px_per_m',32))
    size=np.rint(physical*ppm).astype(int)
    if np.any(size<2) or np.any(size>8192): raise ValueError('output dimensions outside 2..8192')
    # Rectify at >=4x delivery resolution, capped at 2048 on each axis.
    raw=np.minimum(2048,np.maximum(size*4,[256,256])).astype(int)
    matrix=cv2.getPerspectiveTransform(q,np.float32([[0,0],[raw[0]-1,0],[raw[0]-1,raw[1]-1],[0,raw[1]-1]]))
    rect=cv2.warpPerspective(im,matrix,tuple(raw),flags=cv2.INTER_LINEAR,borderMode=cv2.BORDER_REFLECT_101)
    a=linear(rect); y=a@LUMA
    # Low frequency only: evaluate Gaussian on a bounded 128px proxy, then upscale.
    # Avoid an O(image_size * thousand-pixel kernel) pass on rectified photos.
    proxy_scale=min(1.,128/float(max(raw)))
    proxy=cv2.resize(y,None,fx=proxy_scale,fy=proxy_scale,interpolation=cv2.INTER_AREA)
    low=cv2.GaussianBlur(proxy,(0,0),max(1,float(min(raw))*proxy_scale*job.get('blur_fraction',1/6)))
    low=cv2.resize(low,tuple(raw),interpolation=cv2.INTER_LINEAR)
    flat=a*(max(float(y.mean()),1e-8)/np.maximum(low,1e-6))[:,:,None]
    # Restore each channel's mean: division removes light falloff, not paint color.
    flat*=a.mean((0,1))/np.maximum(flat.mean((0,1)),1e-8)
    reduced=cv2.resize(flat,tuple(size),interpolation=cv2.INTER_AREA)
    base=linear(job['base_srgb']) if 'base_srgb' in job else np.array(PBR.get(job.get('material'),a.mean((0,1))))
    kind=job.get('material','paint'); by=float(base@LUMA)
    if kind in BOUNDS: base*=np.clip(by,*BOUNDS[kind])/max(by,1e-8)
    lum=reduced@LUMA; detail=lum/max(float(lum.mean()),1e-8)-1
    # Keep actual photograph's grain locations; fixed smooth sampling suppresses isolated specks.
    detail=cv2.GaussianBlur(detail,(0,0),job.get('grain_sigma_px',.65))
    scale=max(float(np.percentile(np.abs(detail),85)),1e-6)
    detail=np.clip(detail/scale,-1,1)
    amp=float(job.get('amplitude',.04))
    if not 0<=amp<=.04: raise ValueError('albedo grain amplitude must be 0..0.04')
    out=srgb(base)[None,None,:]*(1+amp*detail[:,:,None])
    axes=tuple(job.get('tile_axes',[0,1]))
    if axes: out=periodic(out,axes,job.get('edge_width',8))
    out=np.clip(np.rint(out),0,255).astype('uint8')
    if colors is not None: out=remove_isolated(quantize(out,colors))
    info={'tool_version':VERSION,'input':str(job['input']),'input_sha256':sha(path),'job':job,
          'output_size':size.tolist(),'rectification_matrix':matrix.tolist(),
          'mean_linear_luminance':float((linear(out)@LUMA).mean()),'base_srgb':srgb(base).round(4).tolist(),
          'light_bake':False,'randomness':False,'photo_mean_restored_before_style':True}
    return out,info

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('job',type=Path);p.add_argument('--root',type=Path,default=Path('.'));args=p.parse_args()
    job=json.loads(args.job.read_text(encoding='utf-8-sig'));pal=palette(args.root/job['palette']) if job.get('palette') else None
    a,info=process(job,args.root,pal);out=args.root/job['output'];out.parent.mkdir(parents=True,exist_ok=True);Image.fromarray(a).save(out)
    info['output_sha256']=sha(out);out.with_suffix('.json').write_text(json.dumps(info,indent=2,ensure_ascii=False),encoding='utf-8')
    print(out,info['output_sha256'])

if __name__=='__main__':main()
