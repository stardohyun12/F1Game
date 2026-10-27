from pathlib import Path
from PIL import Image,ImageDraw
import random
O=Path(__file__).parent/'surfaces';O.mkdir(exist_ok=True)
colors={'plastic':('#398995','#70a4a2','#c7c5a1'),'metal':('#566565','#8c9387','#9d764e'),'rubber':('#606a69','#737b70','#66706a'),'seat':('#6a7973','#859188','#a4ab90'),'engine':('#8f6743','#b3824c','#4a5b59'),'cream':('#c6c4a8','#ded9b9','#808a79'),'strap':('#ab7949','#d4a66c','#665b43')}
for name,(base,light,mark) in colors.items():
 im=Image.new('RGB',(64,64),base);d=ImageDraw.Draw(im);rng=random.Random(name)
 d.rectangle((0,0,63,6),fill=light)
 for i in range(10 if name in ['plastic','metal'] else 4):
  x=rng.randrange(4,54);y=rng.randrange(8,60);w=rng.randrange(3,13)
  d.rectangle((x,y,min(63,x+w),y+(1 if name=='metal' else 2)),fill=mark if i%3==0 else light)
 if name=='rubber':
  for x in [10,30,50]:d.rectangle((x,0,x+2,63),fill=light)
 im.save(O/(name+'.png'))
# Explicit drawn digits, no font dependency or generative images.
im=Image.new('RGB',(80,64),'#d6cfaa');d=ImageDraw.Draw(im)
digits=['111101101101111','111001010010010']
for k,pattern in enumerate(digits):
 for y in range(5):
  for x in range(3):
   if pattern[y*3+x]=='1':d.rectangle((8+k*36+x*10,7+y*10,17+k*36+x*10,16+y*10),fill='#344b50')
im.save(O/'number07.png')
