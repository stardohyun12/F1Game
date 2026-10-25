"""Documented approximate photo-tile overlay on real captures; no AI pixels."""
from pathlib import Path
import json,hashlib
import numpy as np
from PIL import Image,ImageDraw,ImageFilter
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'art/_review/2026-10-26-terrain-people/terrain'
TEX=ROOT/'art/textures/2026-10-26-bayloop-terrain/albedo'
def polygon(points):
 im=Image.new('L',(960,540));ImageDraw.Draw(im).polygon(points,fill=255);return np.asarray(im)>0
def tile(name,ppm):
 photo=Image.open(TEX/f'BL_Terrain_{name}_32ppm.png').convert('RGB');result=np.zeros((540,960,3));step=np.broadcast_to(np.asarray(ppm),(540,960));cache={}
 for y in range(540):
  for n in np.unique(np.maximum(1,np.rint(step[y]*2).astype(int))):
   if n not in cache:cache[n]=np.asarray(photo.resize((int(n),int(n)),Image.Resampling.BOX if n<64 else Image.Resampling.NEAREST))
   use=np.maximum(1,np.rint(step[y]*2).astype(int))==n;xs=np.where(use)[0];result[y,xs]=cache[n][y%n,xs%n]
 return result
def run(name,path):
 src=Image.open(ROOT/path).convert('RGB').resize((960,540),Image.Resampling.NEAREST);a=np.asarray(src).astype(float);h,w=540,960
 grass=(a[:,:,1]>a[:,:,0]*.85)&(a[:,:,2]<a[:,:,1]*.98)
 yy,xx=np.indices((h,w));regions=[]
 if name=='topdown':
  # Isolated undeveloped eastern plateau, south field and central meadow.
  region=polygon([(647,54),(960,43),(960,540),(315,540),(350,510),(500,472),(630,450),(650,364),(614,299),(643,210)])
  meadow=polygon([(557,188),(584,176),(625,183),(627,235),(605,262),(556,250)])
  mask=(region|meadow)&grass
  # Approximate map crop 368m N/S road spans346px -> .94px/m.
  ppm=.94
  kind=np.full((h,w),'grass',dtype=object);kind[polygon([(690,40),(960,40),(960,340),(904,299),(846,236),(798,190),(743,152)])]='dry_grass';kind[polygon([(960,329),(905,354),(867,416),(849,540),(960,540)])]='rock'
  kind[polygon([(630,370),(663,376),(654,428),(620,450)])]='soil'
  fog=0.;shade=np.ones((h,w))
 else:
  # Screen-space hand mask excludes roads, buildings, crane, car and HUD.
  near=polygon([(0,208),(111,210),(120,227),(194,233),(370,230),(299,275),(215,307),(135,341),(18,341),(0,329)])
  far=polygon([(2,61),(180,40),(342,35),(342,66),(615,65),(615,43),(697,19),(733,63),(960,16),(960,79),(710,79),(709,171),(514,172),(489,184),(398,162),(354,165),(331,178),(216,175),(218,204),(201,204),(198,165),(176,161),(156,128),(65,124),(2,94)])
  mask=(near|far)&grass
  ppm=np.where(near,np.maximum(5,(yy-195)*.40),.8)
  kind=np.full((h,w),'grass',dtype=object);kind[far]='dry_grass';kind[far&(xx>555)]='rock'
  kind[near&(yy<235)&(xx>245)]='soil'
  # Approximate distant haze, not exact shader reconstruction.
  fog=np.where(far,.64,.10);shade=np.ones((h,w))
 out=a.copy();masks={};layers=np.zeros_like(a);weights=np.zeros((h,w))
 # Keep original render per-pixel lighting/landform; no blur or reconstructed light.
 light=a@np.array([.2126,.7152,.0722])
 for k in ['grass','soil','dry_grass','rock']:
  m=mask&(kind==k);tex=tile(k,ppm);f=np.asarray(fog)
  if f.ndim:tex=tex*(1-f[:,:,None])+np.array([131,146,145])*f[:,:,None]
  elif f:tex=tex*(1-f)+np.array([131,146,145])*f
  # Match original luminance, apply a restrained chroma/detail overlay only.
  tlum=np.maximum(1,tex@np.array([.2126,.7152,.0722]))
  tex*=light[:,:,None]/tlum[:,:,None]
  alpha=np.asarray(Image.fromarray((m*255).astype('uint8')).filter(ImageFilter.GaussianBlur(3))).astype(float)/255
  alpha[~mask]=0;layers+=tex*alpha[:,:,None];weights+=alpha;masks[k]=int(m.sum())
 valid=weights>0
 candidate=layers[valid]/weights[valid,None]
 strength=np.minimum(weights[valid],1)*.28
 out[valid]=a[valid]*(1-strength[:,None])+candidate*strength[:,None]
 result=Image.fromarray(np.clip(out,0,255).astype('uint8'));src.save(OUT/f'{name}-before-960.png');result.save(OUT/f'{name}-after-960.png')
 maskimage=Image.fromarray((mask*255).astype('uint8'));maskimage.save(OUT/f'{name}-mask.png')
 comparison=Image.new('RGB',(1920,576),(225,221,210));comparison.paste(src,(0,36));comparison.paste(result,(960,36));d=ImageDraw.Draw(comparison);d.text((12,10),name+' | actual game capture',fill=(35,31,27));d.text((972,10),'PHOTO CHROMA OVERLAY 28% / ORIGINAL LIGHT PRESERVED / NOT UNITY',fill=(35,31,27));comparison.save(OUT/f'{name}-before-after.png')
 return {'capture':path,'source_sha256':hashlib.sha256((ROOT/path).read_bytes()).hexdigest(),'changed_pixels':int(mask.sum()),'regions':masks,'outside_mask_unchanged':bool(np.array_equal(np.asarray(result)[~mask],np.asarray(src)[~mask]))}
records=[run('topdown','docs/qa/evidence/bayloop-v5b/09_topdown.png'),run('coast','docs/qa/evidence/opening-v0/tut_5_coast_record.png')]
sheet=Image.new('RGB',(1920,1152))
for i,n in enumerate(['topdown','coast']):sheet.paste(Image.open(OUT/f'{n}-before-after.png'),(0,i*576))
sheet.save(OUT/'before-after.png');(OUT/'composite-verification.json').write_text(json.dumps(records,indent=2))
