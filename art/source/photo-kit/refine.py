from pathlib import Path
from PIL import Image
import numpy as np
W=Path(__file__).parent
for name,base in [('tin_F2',[165,83,64]),('tin_F3',[145,145,145])]:
 a=np.array(Image.open(W/'flat'/(name+'.png')),float);y=a@np.array([.2126,.7152,.0722]);column=np.median(y,axis=0);profile=(column-column.mean())/max(column.std(),1);factor=1+np.clip(profile,-2,2)*.07+np.clip((y-column)/max(y.mean(),1),-.025,.025)
 out=np.clip(factor[:,:,None]*np.array(base),0,255).astype('uint8');Image.fromarray(out).save(W/'source'/(name+'.png'))
name='ac_G3';a=np.array(Image.open(W/'flat'/(name+'.png')),float);y=a@np.array([.2126,.7152,.0722]);Image.fromarray(np.repeat(y[:,:,None],3,axis=2).astype('uint8')).save(W/'source'/(name+'.png'))
