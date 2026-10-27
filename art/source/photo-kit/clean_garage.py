from pathlib import Path
from PIL import Image
import numpy as np
R=Path(__file__).resolve().parents[3];E=R/'art/export/scene-a6-photo-kit';old=np.array(Image.open(R/'art/export/scene-a-v5b-garage/garage_baked32.png').convert('RGB'))
for p in E.glob('garage_tin_*_atlas.png'):
 a=np.array(Image.open(p).convert('RGB'));mask=np.any(a!=old,axis=2)
 for _ in range(4):
  same=np.zeros(a.shape[:2],bool)
  for dy,dx in [(1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)]:same|=np.all(a==np.roll(a,(dy,dx),(0,1)),axis=2)
  bad=(~same)&mask;bad[[0,-1]]=False;bad[:,[0,-1]]=False
  if not bad.any():break
  for y,x in np.argwhere(bad):
   n=a[y-1:y+2,x-1:x+2].reshape(-1,3);colors,counts=np.unique(n,axis=0,return_counts=True);a[y,x]=colors[counts.argmax()]
 assert np.array_equal(a[~mask],old[~mask]);Image.fromarray(a).save(p)
