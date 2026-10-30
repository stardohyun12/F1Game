"""Measure rendered field colours inside the field box (read-only)."""
from PIL import Image
import numpy as np, colorsys, sys
d='D:/Github/F1Game/art/_review/2026-10-30-field_terrace/'
f=sys.argv[1] if len(sys.argv)>1 else 'dbg_B_top.png'
full=np.asarray(Image.open(d+f).convert('RGB')).astype(float)
t=full[40:690,230:1080]
r,g,b=t[...,0],t[...,1],t[...,2]
soil=(r>g+10)&(r>b+25)
sv=t[soil]
print('soil px',int(soil.sum()),'median',np.median(sv,0),'p90',np.percentile(sv,90,0),'p10',np.percentile(sv,10,0))
lum=sv@np.array([.299,.587,.114]);print('lum cv',round(float(lum.std()/lum.mean()),3))
c=full[320:350,420:800].reshape(-1,3);print('path',np.median(c,0))
