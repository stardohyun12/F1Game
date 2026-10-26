from pathlib import Path
from PIL import Image,ImageDraw
import json
R=Path(__file__).resolve().parents[2];O=R/'art/models/bayloop_people_v04';O.mkdir(parents=True,exist_ok=True)
P=json.loads((R/'art/source/palette/pitcrew_d123_32.json').read_text())['colors'];im=Image.new('RGB',(128,128));d=ImageDraw.Draw(im)
cols=[3,23,28,18,0,2,19,2,25,15,2,0,22,13,18,21]
for t,c in enumerate(cols):
 x=t%4*32;y=t//4*32;d.rectangle((x,y,x+31,y+31),fill=tuple(P[c]))
for t,shade,light in [(0,18,13),(1,22,25),(2,15,16)]:
 x=t%4*32;y=t//4*32
 d.rectangle((x,y,x+4,y+31),fill=tuple(P[shade]));d.rectangle((x+26,y,x+31,y+31),fill=tuple(P[shade]))
 for a,b,c,e in [(8,18,11,19),(18,24,23,25),(9,29,16,30)]:d.rectangle((x+a,y+b,x+c,y+e),fill=tuple(P[shade]))
 d.line((x+16,y+7,x+16,y+28),fill=tuple(P[light]),width=1)
# One authored face tile: exactly three skin tones, eyebrows/eyes/mouth, no blush/no gradient.
x=96;y=64;d.rectangle((x,y,x+31,y+31),fill=tuple(P[0]));d.rectangle((x,y,x+3,y+31),fill=tuple(P[16]));d.rectangle((x+28,y,x+31,y+31),fill=tuple(P[16]));d.rectangle((x+6,y+4,x+25,y+13),fill=tuple(P[25]))
for a in [8,20]:
 d.rectangle((x+a,y+12,x+a+4,y+12),fill=tuple(P[2]));d.rectangle((x+a+1,y+14,x+a+3,y+15),fill=tuple(P[2]))
d.rectangle((x+13,y+24,x+19,y+24),fill=tuple(P[15]));d.rectangle((x+15,y+18,x+16,y+20),fill=tuple(P[16]))
# Rule11: face has only 8 actual authored texels across; nearest expands storage.
face=Image.new('RGB',(8,8),tuple(P[0]));f=ImageDraw.Draw(face)
f.rectangle((0,0,0,7),fill=tuple(P[16]));f.rectangle((7,0,7,7),fill=tuple(P[16]))
for a in [2,5]:f.point((a,4),fill=tuple(P[21]));f.line((a-1,2,a,2),fill=tuple(P[16]))
f.line((3,6,4,6),fill=tuple(P[16]));im.paste(face.resize((32,32),Image.Resampling.NEAREST),(96,64))
# Pants: uninterrupted base colour, two restrained pocket lines only.
d.line((103,5,107,7),fill=tuple(P[5]),width=1);d.line((117,5,121,7),fill=tuple(P[5]),width=1)
# Clothing identities: apron, long coat, cardigan, waistcoat, work pockets.
for t in [1,12,13,14]:
 x=t%4*32;y=t//4*32
 if t==1:d.rectangle((x+8,y+9,x+24,y+30),fill=tuple(P[25]));d.rectangle((x+12,y+3,x+21,y+13),fill=tuple(P[25]))
 elif t==12:
  d.rectangle((x+14,y+3,x+16,y+30),fill=tuple(P[21]));d.rectangle((x+5,y+24,x+10,y+26),fill=tuple(P[21]));d.rectangle((x+22,y+24,x+27,y+26),fill=tuple(P[21]))
 elif t==13:
  for yb in [8,14,20,26]:d.rectangle((x+15,y+yb,x+16,y+yb+1),fill=tuple(P[18]))
 else:
  d.polygon([(x+9,y),(x+17,y+13),(x+24,y)],fill=tuple(P[25]));d.line((x+17,y+13,x+17,y+30),fill=tuple(P[5]),width=1)
for px in [6,21]:d.rectangle((64+px,11,64+px+5,16),outline=tuple(P[16]))
im.save(O/'PeoplePS1Atlas_v4.png')

im.save(O/'PeoplePS1Atlas_v4_2106.png')
