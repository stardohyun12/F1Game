from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json,random
W=Path(__file__).parent;R=W.parents[2];E=R/'art/export/q2-plaza';E.mkdir(parents=True,exist_ok=True)
rng=random.Random(252)
# H2/H3 are colour/stone-size references only. No photograph pixels in deliverables.
samples={}
for n,box in [('H2_stone_wall_dordogne.jpg',(280,110,420,210)),('H3_old_stone_wall.jpg',(420,410,660,590))]:
 a=np.array(Image.open(R/'art/source/photo-picks/H'/n).convert('RGB').crop(box));samples[n]={'box':box,'median_RGB':np.median(a.reshape(-1,3),axis=0).tolist()}
(W/'photo-samples.json').write_text(json.dumps(samples,indent=2))
stone=[(161,157,144),(170,164,149),(153,153,143),(178,171,154)];joint=(131,130,119)
im=Image.new('RGB',(128,128),joint);d=ImageDraw.Draw(im)
for row in range(8):
 shift=[0,11,5,17,8,2,14,6][row];x=-32+shift
 while x<128:
  width=rng.choice([22,26,30,34]);c=rng.choice(stone);box=(x+1,row*16+1,x+width-1,row*16+15);d.rectangle(box,fill=c)
  # Tiny clipped corners, no painted bevel or shadow.
  for px,py in [(x+1,row*16+1),(x+width-1,row*16+15)]:d.point((px,py),fill=joint)
  for j in range(3):
   xx=rng.randint(x+3,x+width-4);yy=rng.randint(row*16+3,row*16+11);d.rectangle((xx,yy,xx+2,yy+1),fill=tuple(round(v*rng.choice([.98,1.02])) for v in c))
  x+=width
# Opposite borders share mortar; tile is periodic at 4 m, no continuous dark border >1px.
a=np.array(im);a[-1]=a[0];a[:,-1]=a[:,0];Image.fromarray(a).save(E/'plaza_paving.png')
im=Image.new('RGB',(64,32),(174,170,154));d=ImageDraw.Draw(im)
for x in [0,32]:d.rectangle((x,0,x,31),fill=(138,137,125))
for x,y in [(7,6),(18,22),(40,12),(52,24)]:d.rectangle((x,y,x+4,y+1),fill=(170,167,151))
a=np.array(im);a[:,-1]=a[:,0];a[-1]=a[0];Image.fromarray(a).save(E/'plaza_curb.png')
colors=[(143,105,66),(66,66,66),(178,164,127),(166,95,59),(66,114,125),(67,89,51),(92,116,61),(117,133,65),(92,72,48),(186,149,70),(186,179,154),(122,85,52),(80,80,80),(148,78,48),(102,123,70),(167,158,133)]
im=Image.new('RGB',(256,256));d=ImageDraw.Draw(im)
for k,c in enumerate(colors):
 x=k%4*64;y=k//4*64;d.rectangle((x,y,x+63,y+63),fill=c)
 for j in range(32):
  xx=x+rng.randrange(1,59);yy=y+rng.randrange(1,60);scale=rng.choice([.98,1.02]);d.rectangle((xx,yy,xx+rng.choice([2,4]),yy+1),fill=tuple(round(v*scale) for v in c))
 if k in [0,11]:
  for j in range(5):
   yy=y+7+j*11;d.rectangle((x+8+j*2,yy,x+35+j*3,yy+1),fill=tuple(round(v*.97) for v in c))
im.save(E/'plaza_props.png')
# Original 5x7 bitmap lettering. No external font asset.
glyphs={'A':['01110','10001','10001','11111','10001','10001','10001'],'B':['11110','10001','10001','11110','10001','10001','11110'],'E':['11111','10000','10000','11110','10000','10000','11111'],'H':['10001','10001','10001','11111','10001','10001','10001'],'M':['10001','11011','10101','10101','10001','10001','10001'],'O':['01110','10001','10001','10001','10001','10001','01110'],'R':['11110','10001','10001','11110','10100','10010','10001'],'T':['11111','00100','00100','00100','00100','00100','00100'],'↑':['00100','01110','10101','00100','00100','00100','00100']}
im=Image.new('RGB',(128,128),(143,105,66));d=ImageDraw.Draw(im)
for row,word in enumerate(['TEA ↑','HARBOR','MART']):
 y=row*32;d.rectangle((0,y,127,y+31),fill=(186,179,154));start=18 if row==1 else 18
 for idx,ch in enumerate(word):
  if ch==' ':continue
  for gy,line in enumerate(glyphs[ch]):
   for gx,bit in enumerate(line):
    if bit=='1':d.rectangle((start+idx*18+gx*3,y+5+gy*3,start+idx*18+gx*3+2,y+5+gy*3+2),fill=(57,57,53))
 # local edge wear, not baked illumination
 d.rectangle((10,y+29,24,y+30),fill=(180,173,149))
im.save(E/'plaza_sign.png')
pal=set()
for p in E.glob('*.png'):pal.update(Image.open(p).convert('RGB').getdata())
(E/'palette.gpl').write_text('GIMP Palette\nName: Q2 material colours\nColumns: 8\n#\n'+''.join(f'{r} {g} {b} material\n' for r,g,b in sorted(pal)))
print('textures',len(pal),'colours')
