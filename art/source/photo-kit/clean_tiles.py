from pathlib import Path
from PIL import Image
import numpy as np,json
E=Path(__file__).resolve().parents[3]/'art/export/scene-a6-photo-kit';m=json.loads((E/'texture-validation.json').read_text())
for name in ['stone_H1','stone_H2','stone_H3']:
 p=E/(name+'.png');a=np.array(Image.open(p).convert('RGB'));h,w=a.shape[:2]
 for y in range(1,h-1):
  for x in range(1,w-1):
   n=a[y-1:y+2,x-1:x+2];same=np.all(n==a[y,x],axis=2)
   if same.sum()==1:a[y,x]=a[y,x-1]
 assert np.array_equal(a[0],a[-1]) and np.array_equal(a[:,0],a[:,-1]);Image.fromarray(a).save(p);m[name]['colors']=len(np.unique(a.reshape(-1,3),axis=0))
(E/'texture-validation.json').write_text(json.dumps(m,indent=2))
