"""Photograph-based material revision for failed props only; accepted atlas untouched."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,numpy as np
R=Path(__file__).resolve().parents[2];O=R/'art/models/bayloop_italy_props_v01'
P=json.loads((R/'art/source/palette/pitcrew_d123_32.json').read_text())['colors']
im=Image.open(O/'PropsPhotoAtlas.png').convert('RGB');base=im.crop((512,0,768,256));a=np.array(base)
# Quiet old painted wood, retain photographed value pattern.
for old,new in [(6,2),(2,9),(9,2),(5,9)]:a[np.all(np.array(base)==P[old],axis=2)]=P[new]
base=Image.fromarray(a);d=ImageDraw.Draw(base);d.fontmode='1'
font=ImageFont.truetype(str(R/'F1/Assets/UI/Fonts/Pretendard/Pretendard-Regular.ttf'),10)
for text,offset,w,h in [('세탁',32,23,18),('차',96,11,22)]:
 b=d.textbbox((0,0),text,font=font);tw=b[2]-b[0];th=b[3]-b[1];d.text((2+(w-tw)//2-b[0],2+offset+(h-th)//2-b[1]),text,font=font,fill=tuple(P[14]))
font5=ImageFont.truetype(str(R/'F1/Assets/UI/Fonts/Pretendard/Pretendard-Regular.ttf'),5)
d.rectangle((2,130,14,137),fill=tuple(P[6]));d.text((2,129),'073',font=font5,fill=tuple(P[14]));im.paste(base,(256,768))
# Existing actual roof photograph is recoloured into warm yellow/ochre fruit values.
old=np.array(im.crop((768,768,1024,1024)));a=old.copy()
for color,new in [(6,22),(9,23),(5,23),(8,31)]:a[np.all(old==P[color],axis=2)]=P[new]
im.paste(Image.fromarray(a),(0,256));im.save(O/'PropsPhotoAtlas_r2.png')
