from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,sys
R=Path(__file__).resolve().parents[2];O=R/'art/models/codex_garage_kart_pixel_v1';name=sys.argv[1]
rects=json.loads((O/(name+'_uv.json')).read_text());atlas=Image.new('RGB',(1024,1024),'#526c68')
# Each face uses at most six explicit colours; no random/noise/photo/gradient.
P=[['#b09b74','#d0b990','#827759','#526c68','#c3a379','#946e50'],['#788982','#98a49a','#536e6c','#a07451','#ba936a','#665f4b'],['#a58b60','#c3aa79','#766a4c','#526961','#b0986b','#dfcda0'],['#54817d','#8eada1','#3f6462','#bf8d5e','#95694c','#c1c9a4'],['#8faaa3','#b8c4ad','#5b7a78','#718e85','#a4b4a4','#d1cfac'],['#3c5756','#56706b','#74877b','#455f5b','#6c7c6e','#8a9581'],['#ad6750','#c58a67','#795b4a','#d5bd8b','#536e69','#987459'],['#b29d70','#cfba8c','#807d5d','#526c68','#a28e65','#dec99a'],['#d9c89e','#b4aa85','#7d8873','#526c68','#c9bb91','#a29472'],['#a1997c','#beb293','#777e69','#4e6863','#8d8e75','#c6bb9b']]
fpath=R/'art/_review/2026-10-23-course-ui/fonts/Galmuri11.ttf'
for r in rects:
 w,h,t=r['w'],r['h'],r['tile'];pal=P[t] if t<10 else P[2 if t==15 else 3];im=Image.new('RGB',(w,h),pal[0]);d=ImageDraw.Draw(im);nm=r['name']
 if t==0 or t==2 or t==15:
  # Planks with deliberately placed splice, split, and exposed end grain.
  step=5 if t==0 else 4
  for xx in [j for j in range(3,w) if j%19 in [3,9,14]]:d.line((xx,0,xx,h-1),fill=pal[2]);d.line((xx-1,0,xx-1,h-1),fill=pal[1])
  if w>9 and h>8:
   d.rectangle((w//3,h//2,w//3+4,min(h-1,h//2+2)),fill=pal[4]);d.line((w//3,h//2,w//3+4,h//2),fill=pal[2]);d.line((w-3,h-6,w-3,h-2),fill=pal[2])
 if t==1:
  for yy in range(3,h,5):d.line((0,yy,w-1,yy),fill=pal[2]);d.line((0,yy-1,w-1,yy-1),fill=pal[1])
  for xx,yy in [(w//4,h//3),(w*3//4,h*2//3)]:d.rectangle((xx,yy,min(w-1,xx+3),min(h-1,yy+1)),fill=pal[3]);d.line((xx,yy+2,xx,min(h-1,yy+4)),fill=pal[4])
 if t in [3,4,6,7,8] and w>3 and h>2:
  d.line((0,h-1,w-1,h-1),fill=pal[2]);d.line((1,0,min(w-1,4),0),fill=pal[1]);d.rectangle((max(0,w-3),max(0,h-3),w-1,h-2),fill=pal[3]);d.point((w-2,h-1),fill=pal[4])
 if t==5:
  for xx in range(2,w,4):d.line((xx,0,xx,min(h-1,2)),fill=pal[1])
 if t==9 and w>12 and h>12:
  for xx,yy in [(w//3,h//2),(w*2//3,h*3//4)]:d.polygon([(xx,yy),(xx+4,yy),(xx+4,yy+2),(xx+6,yy+2),(xx+6,yy+4),(xx+1,yy+4),(xx+1,yy+3),(xx-2,yy+3),(xx-2,yy+1),(xx,yy+1)],fill=pal[3])
 if t in [10,11,12,13,14]:
  # Semantic pixel information, nearest sized; same 6-colour ceiling.
  raw=Image.new('RGB',(96,64),pal[0]);q=ImageDraw.Draw(raw);f=ImageFont.truetype(str(fpath),10)
  if t==11:
   for i,txt in enumerate(['?','??','??']):q.text((i*32+4,3),txt,font=f,fill=pal[1]);q.rectangle((i*32+2,18,i*32+28,60),outline=pal[1])
  elif t==12:q.text((10,16),'?? ???',font=f,fill=pal[1]);q.text((23,36),'PIT 07',font=f,fill=pal[1])
  elif t==14:
   for yy,txt in [(3,'?? ??'),(22,'01 1:42'),(36,'02 1:37'),(50,'03 --:--')]:q.text((3,yy),txt,font=f,fill=pal[1])
  elif t==10:
   q.rectangle((3,3,92,60),fill=pal[1]);q.text((8,7),'COAST 07',font=f,fill=pal[2]);q.polygon([(14,42),(65,27),(80,38),(30,48)],fill=pal[3]);q.rectangle((18,41,30,49),fill=pal[2]);q.rectangle((65,32,76,40),fill=pal[2])
  else:
   for xx in [9,28,47,66,85]:q.line((xx,18,xx,51),fill=pal[1],width=3);q.rectangle((xx-4,12,xx+4,20),outline=pal[1],width=2)
  # Draw small information at its final texel size; never shrink away one-pixel borders.
  colors=Image.new('P',(1,1));rgb=[tuple(bytes.fromhex(c[1:])) for c in pal];colors.putpalette([v for c in rgb for v in c]+[0]*(768-18));raw=raw.quantize(palette=colors,dither=Image.Dither.NONE).convert('RGB');im=raw.resize((w,h),Image.Resampling.NEAREST)
  d=ImageDraw.Draw(im)
  if t==11:
   d.rectangle((0,0,w-1,h-1),fill=pal[0])
   for i in range(3):
    xx=i*w//3;d.rectangle((xx+1,7,min(w-1,xx+w//3-1),h-2),outline=pal[1])
   # 5x5 hand-pixel labels: ? / ?? / ??.
   glyphs=['00100/11111/00100/01010/10001','10101/10101/01101/11111/10000','11101/00111/11101/00111/11101','01110/10001/01110/11111/00100','01101/10111/01101/11111/00101']
   positions=[3,w//3+1,w//3+7,2*w//3+1,2*w//3+7]
   for i,g in enumerate(glyphs):
    for yy,row in enumerate(g.split('/')):
     for xx,c in enumerate(row):
      if c=='1':d.point((positions[i]+xx,yy+1),fill=pal[1])
  if t==14:
   d.rectangle((0,0,w-1,h-1),fill=pal[0])
   for yy in [3,8,13]:d.line((2,yy,w-3,yy),fill=pal[1]);d.point((2,yy-1),fill=pal[1])
  if t==12:
   d.rectangle((0,0,w-1,h-1),fill=pal[0]);f=ImageFont.truetype(str(fpath),8);d.text((1,0),'???',font=f,fill=pal[1],stroke_width=0)
 if t in [0,1] and w>20 and h>12:
  d=ImageDraw.Draw(im)
  # One purposeful failed paint area along the damp lower edge, not random dots.
  xx=w//2;yy=h-5;d.polygon([(xx,yy),(xx+8,yy),(xx+8,yy+1),(xx+12,yy+1),(xx+12,h-1),(xx-3,h-1),(xx-3,yy+2),(xx,yy+2)],fill=pal[3]);d.line((xx,yy,xx+5,yy),fill=pal[5])
 if nm.startswith('Nose') and w>=8 and h>=3:
  d=ImageDraw.Draw(im);d.rectangle((w//2-2,0,w//2+2,min(h-1,4)),fill=pal[3]);d.line((w//2-1,1,w//2+1,1),fill=pal[2]);d.line((w//2+1,1,w//2,min(h-1,3)),fill=pal[2])
 if w*h>=16:
  d=ImageDraw.Draw(im);d.line((0,0,w-1,0),fill=pal[2]);d.line((0,h-1,w-1,h-1),fill=pal[1])
 colors=Image.new('P',(1,1));rgb=[tuple(bytes.fromhex(c[1:])) for c in pal];colors.putpalette([v for c in rgb for v in c]+[v for _ in range(250) for v in rgb[0]]);im=im.quantize(palette=colors,dither=Image.Dither.NONE).convert('RGB')
 atlas.paste(im,(r['x'],r['y']))
atlas.save(O/(name+'.png'))
