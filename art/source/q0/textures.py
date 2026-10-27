from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json,shutil
R=Path(__file__).resolve().parents[3];W=Path(__file__).parent;E=R/'art/export/scene-albedo-kit';V=R/'art/_review/2026-10-28-q0'
for p in [W/'before',W/'fronts',V]:p.mkdir(parents=True,exist_ok=True)
names=['vending_G1','vending_G2','postbox_G1','ac_G3','tin_F3','garage_tin_F3_atlas']
for n in names:
 p=E/(n+'.png')
 if not (W/'before'/p.name).exists():shutil.copy2(p,W/'before'/p.name)
 for suffix in ['.fbx','.gpl']:
  p=E/(n+suffix)
  if p.exists() and not (W/'before'/p.name).exists():shutil.copy2(p,W/'before'/p.name)
samples={}
def color(name,file,box,neutral=False):
 a=np.array(Image.open(R/'art/source/photo-kit/crops'/file).convert('RGB').crop(box));c=np.rint(np.median(a.reshape(-1,3),axis=0)).astype(int)
 if neutral:c[:]=round(c.mean())
 c=np.clip(c,35,210);samples[name]=dict(file=file,box=box,RGB=c.tolist(),neutral=neutral);return tuple(c)
cream=color('cream','vending_G1.png',(12,280,55,310));orange=color('orange','vending_G2.png',(12,300,35,380));blue=color('blue','vending_G1.png',(90,240,130,280));red=color('red','vending_G1.png',(228,247,243,267));green=color('green','vending_G1.png',(235,45,245,65));white=color('label','ac_G3.png',(165,38,192,43),True);dark=color('charcoal','ac_G3.png',(45,20,58,35),True);metal=color('metal','ac_G3.png',(155,95,195,108),True);fblue=color('F3 blue','tin_F3.png',(14,12,230,180))
# Fixed large 2–4 texel primitives, not downsampled photo fronts.
for name,base in [('vending_G1',cream),('vending_G2',orange)]:
 im=Image.new('RGB',(76,114),base);d=ImageDraw.Draw(im);d.rectangle((4,5,61,64),fill=dark)
 for row in range(3):
  y=8+row*18
  for col in range(7):
   x=6+col*8;c=[blue,green,white,red,blue,white,green][(col+row)%7]
   d.rectangle((x+1,y,x+4,y+1),fill=white);d.rectangle((x,y+2,x+5,y+10),fill=c);d.rectangle((x,y+6,x+5,y+8),fill=white)
  d.rectangle((5,y+12,60,y+14),fill=cream)
  for col in range(7):d.rectangle((7+col*8,y+15,10+col*8,y+16),fill=metal)
 # Controls and a clear lower dispensing aperture.
 d.rectangle((65,13,70,38),fill=metal);d.rectangle((66,16,69,20),fill=dark);d.rectangle((65,43,70,46),fill=red)
 d.rectangle((7,71,39,86),fill=blue)
 if name=='vending_G1':
  d.rectangle((12,75,34,77),fill=white);d.rectangle((12,81,28,83),fill=white)
 else:
  for x,c in [(10,red),(19,green),(28,white)]:d.rectangle((x,75,x+5,83),fill=c);d.rectangle((x+1,72,x+4,74),fill=cream)
 d.rectangle((48,70,69,87),fill=metal);d.rectangle((51,73,57,77),fill=blue);d.rectangle((61,72,68,75),fill=dark);d.rectangle((61,80,68,83),fill=red)
 d.rectangle((15,93,60,104),fill=dark);d.rectangle((18,94,57,96),fill=metal);d.rectangle((63,97,68,102),fill=dark)
 for x in range(8,60,6):d.rectangle((x,109,x+2,110),fill=metal)
 # Two-pixel paint wear, fixed location; scalar variation only, no directional shading.
 d.rectangle((2,84,3,89),fill=tuple(round(v*.97) for v in base));d.rectangle((69,107,72,108),fill=tuple(round(v*1.02) for v in base))
 im.save(W/'fronts'/(name+'.png'))
im=Image.new('RGB',(52,32),white);d=ImageDraw.Draw(im);d.ellipse((3,2,31,30),fill=dark);d.ellipse((5,4,29,28),fill=(66,66,66))
# Fan blade geometry drawn inside a distinct circular grille.
for poly in [[(17,16),(9,7),(17,6),(21,12)],[(17,16),(28,13),(26,21),(20,22)],[(17,16),(16,27),(8,23),(10,17)]]:d.polygon(poly,fill=dark)
mask=Image.new('L',im.size);md=ImageDraw.Draw(mask);md.ellipse((5,4,29,28),fill=255);gr=Image.new('RGB',im.size,(124,124,124));gd=ImageDraw.Draw(gr);gm=Image.new('L',im.size);gmd=ImageDraw.Draw(gm)
for x in range(7,29,5):gmd.rectangle((x,4,x+1,28),fill=255)
for y in [10,21]:gmd.rectangle((5,y,29,y+1),fill=255)
gm=Image.fromarray(np.minimum(np.array(gm),np.array(mask)));im.paste(gr,(0,0),gm);d=ImageDraw.Draw(im);d.ellipse((14,13,20,19),fill=white)
d.rectangle((36,4,46,6),fill=blue)
for y in [12,18,24]:d.rectangle((35,y,47,y+1),fill=(98,98,98))
im.save(W/'fronts/ac_G3.png')
# Restore blue hue without painting sun/shadow into the sheet. Existing groove locations remain.
old=np.array(Image.open(W/'before/tin_F3.png').convert('RGB'),float);y=old.mean(2);factor=1+np.clip((y-np.median(y))/max(y.std(),1),-1,1)*.04;factor=np.round(factor/.02)*.02;new=np.clip(np.rint(np.array(fblue)*factor[:,:,None]),0,255).astype('uint8');new[:,-1]=new[:,0];new[-1]=new[0];Image.fromarray(new).save(E/'tin_F3.png')
(W/'palette-samples.json').write_text(json.dumps(samples,indent=2));(W/'bases.json').write_text(json.dumps(dict(vending_G1=cream,vending_G2=orange,ac_G3=white),default=lambda x:int(x)));print(samples)
