"""Last failed-prop revision: real bitmap typography at upstream native pixel sizes."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,numpy as np
R=Path(__file__).resolve().parents[2];O=R/'art/models/bayloop_italy_props_v01';P=json.loads((R/'art/source/palette/pitcrew_d123_32.json').read_text())['colors'];im=Image.open(O/'PropsPhotoAtlas_r2.png').convert('RGB')
# Photograph colour changes, no stochastic pattern. Accepted atlas remains untouched.
base=im.crop((512,0,768,256));orig=np.array(base);a=orig.copy()
for old,new in [(6,9),(2,5),(9,9),(5,5)]:a[np.all(orig==P[old],axis=2)]=P[new]
base=Image.fromarray(a);d=ImageDraw.Draw(base);d.fontmode='1';samples=[]
fonts={'Galmuri11':(R/'art/_review/2026-10-23-ui-final/fonts/Galmuri11.ttf',12),'Galmuri7':(R/'art/models/cargo_box_v01/fonts/Galmuri7.ttf',8)}
for text,offset,w,h,fname in [('세탁',32,30,18,'Galmuri11'),('차',96,16,18,'Galmuri11'),('07',128,12,10,'Galmuri7')]:
 fpath,size=fonts[fname];f=ImageFont.truetype(str(fpath),size);bbox=d.textbbox((0,0),text,font=f);tw=bbox[2]-bbox[0];th=bbox[3]-bbox[1];assert tw<=w and th<=h
 if fname=='Galmuri7':d.rectangle((2,2+offset,2+w-1,2+offset+h-1),fill=tuple(P[6]))
 x=2+(w-tw)//2-bbox[0];y=2+offset+(h-th)//2-bbox[1];d.text((x,y),text,font=f,fill=tuple(P[25]))
 sample=base.crop((2,2+offset,2+w,2+offset+h));sample.resize((w*16,h*16),Image.Resampling.NEAREST).save(O/('glyph_'+fname+'_'+str(offset)+'.png'))
 samples.append({'text':text,'font':str(fpath.relative_to(R)),'size_px':size,'glyph_bbox':bbox,'uv_region_px':[w,h],'full_glyph_inside_uv':True,'face_m':[w/32,h/32]})
im.paste(base,(256,768))
for idx,mapping in [(14,{15:21,21:24}),(4,{22:23,23:25,31:23})]:
 x=idx%4*256;y=idx//4*256;orig=np.array(im.crop((x,y,x+256,y+256)));a=orig.copy()
 for old,new in mapping.items():a[np.all(orig==P[old],axis=2)]=P[new]
 im.paste(Image.fromarray(a),(x,y))
im.save(O/'PropsPhotoAtlas_r3.png');(O/'glyph-validation-r3.json').write_text(json.dumps({'upstream':'https://github.com/quiple/galmuri','native_sizes':'Galmuri11 12px; Galmuri7 8px per upstream README','license':'OFL-1.1','samples':samples},ensure_ascii=False,indent=2))
