from pathlib import Path
from PIL import Image
import numpy as np
E=Path(__file__).resolve().parents[3]/'art/export/scene-albedo-kit'
for name in ['vending_G1','vending_G2','postbox_G1','ac_G3']:
 p=E/(name+'.png');a=np.array(Image.open(p).convert('RGBA'));filled=a[:,:,:3].max(2)>3
 for _ in range(3):
  new=filled.copy()
  for dy,dx in [(1,0),(-1,0),(0,1),(0,-1)]:
   take=~new&np.roll(filled,(dy,dx),(0,1));a[take,:3]=np.roll(a,(dy,dx),(0,1))[take,:3];new[take]=True
  filled=new
 Image.fromarray(a).save(p)
