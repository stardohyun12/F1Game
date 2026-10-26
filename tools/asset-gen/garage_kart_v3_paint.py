from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,sys
R=Path(__file__).resolve().parents[2];O=R/'art/models/garage_kart_v3';name=sys.argv[1];rs=json.loads((O/(name+'_uv.json')).read_text());im=Image.new('RGB',(1024,1024),'#cdbd99')
palettes=[['#d8bd8c','#ead4a5','#a38562','#677f76','#b78a60','#ddc49b'],['#bd7551','#d79564','#8e624d','#6e8880','#d7ad7a','#a36c50'],['#b49768','#d6b77d','#786d51','#62766a','#c2a472','#e2c795'],['#53887e','#80aa94','#3e655f','#b5875b','#d6c79c','#769686'],['#9baca1','#d5d8b5','#637d77','#54736e','#b6c0a8','#e6d7ac'],['#354c4b','#586a5e','#293d3d','#7e8873','#aab19a','#465c54'],['#b96340','#d18958','#814e3f','#e4c890','#4d756e','#aa7852'],['#8d8762','#b8ac7b','#626b55','#5f7f72','#d4c28d','#978c69'],['#e4d3a7','#bdac83','#7d8c77','#5d776c','#d4c697','#a99470'],['#b0a17b','#c9b68b','#918a6d','#5d7a70','#a89976','#d1bb90']]
font=R/'art/_review/2026-10-23-course-ui/fonts/Galmuri11.ttf'
for r in rs:
 w,h,t,n=r['w'],r['h'],r['tile'],r['name'];p=palettes[t] if t<10 else palettes[2 if t==15 else 3];a=Image.new('RGB',(w,h),p[0]);d=ImageDraw.Draw(a)
 # Broad surfaces stay flat. Only placed repairs, material edge, and purposeful information.
 if t==0 and w>25 and h>20:
  d.polygon([(0,h-4),(w//5,h-4),(w//5,h-2),(w//3,h-2),(w//3,h-1),(0,h-1)],fill=p[2])
  d.rectangle((w*3//4,h//2,w*3//4+5,h//2+2),fill=p[1]);d.line((w*3//4,h//2+3,w*3//4+5,h//2+3),fill=p[2])
 if t==1 and w>24 and h>20:
  # Broad unequal roof sections and one seam, not a repeating tile pattern.
  d.polygon([(0,h//3),(w//3,h//3),(w//3,h//2),(w//2,h//2),(w//2,h//2+3),(0,h//2+3)],fill=p[1]);d.line((w*2//3,0,w*2//3,h-1),fill=p[2])
  d.rectangle((w//4,h//3,w//4+7,h//3+10),fill=p[3]);d.line((w//4,h//3,w//4+7,h//3),fill=p[4])
 if t in [2,15] and w>8 and h>5:
  d.line((0,h-2,w-1,h-2),fill=p[2]);d.line((1,1,min(w-1,6),1),fill=p[1])
  if 'Cargo_box' in n and w>12 and h>7:
   d.rectangle((w//2-5,h//2-3,w//2+5,h//2+3),fill=p[5])
   for xx,hh in [(0,3),(2,4),(3,3),(5,4)]:d.line((w//2-3+xx,h//2-2,w//2-3+xx,h//2-2+hh),fill=p[2])
 if t in [3,4,6,7] and w>6 and h>3:
  d.line((0,h-1,w-1,h-1),fill=p[2]);d.line((0,0,w-1,0),fill=p[1])
  d.line((1,h-1,min(w-1,5),h-1),fill=p[2]);d.rectangle((w-4,0,w-2,1),fill=p[1])
 if 'Engine' in n and w>7 and h>5:
  for yy in [2,4,6]:d.line((2,yy,w-3,yy),fill=p[2])
 if 'Hub' in n and w>3 and h>3:d.rectangle((w//2-1,h//2-1,w//2+1,h//2+1),fill=p[1])
 if 'Floorpan' in n and w>12 and h>12:
  d.rectangle((1,1,w-2,h-2),outline=p[2]);d.rectangle((w//3,h//3,w//3+4,h//3+5),fill=p[3])
 if 'Side_pod' in n and w>6 and h>3:
  d.line((2,0,2,h-1),fill=p[2]);d.line((w-3,0,w-3,h-1),fill=p[2])
 if t==9 and w>20 and h>20:d.polygon([(w//3,h//2),(w//3+5,h//2),(w//3+5,h//2+2),(w//3+8,h//2+2),(w//3+8,h//2+4),(w//3+1,h//2+4)],fill=p[3])
 if t in [10,11,12,13,14]:
  f=ImageFont.truetype(str(font),8)
  if t==10:
   d.rectangle((0,0,w-1,h-1),fill='#e5ca8e');d.text((1,1),'07',font=f,fill=p[2]);d.polygon([(1,h-5),(w-3,h//2),(w-2,h//2+3),(2,h-2)],fill='#ad573b')
  if t==11:
   for i,txt in enumerate(['차','실력','응원']):
    xx=i*w//3;d.text((xx+1,0),txt,font=f,fill=p[4]);d.rectangle((xx+1,10,min(w-1,xx+w//3-1),h-2),outline=p[4])
  if t==12:d.text((1,max(0,h//2-4)),'항구 정비소',font=f,fill=p[4])
  if t==14:
   for yy,txt in [(1,'기록'),(10,'1:42'),(19,'1:37')]:d.text((1,yy),txt,font=f,fill=p[4])
  if t==13:
   for i,xx in enumerate(range(3,w-3,7)):
    yy=3+(i%2)*2;d.line((xx,yy,xx,min(h-2,yy+8)),fill=p[4]);d.line((xx-2,yy,xx+2,yy),fill=p[4]);d.point((xx-2,yy-1),fill=p[4]);d.point((xx+2,yy-1),fill=p[4])
 if 'Replacement_nose_panel' in n and w>=3 and h>=3:
  d.line((w//2-1,0,w//2+1,0),fill=p[3]);d.line((w//2+1,0,w//2-1,h-1),fill=p[3])
 # Author colours only. Font antialiasing is snapped to the six face colours.
 rgb=[tuple(bytes.fromhex(c[1:])) for c in p];q=Image.new('P',(1,1));q.putpalette([v for c in rgb for v in c]+[v for _ in range(250) for v in rgb[0]]);a=a.quantize(palette=q,dither=Image.Dither.NONE).convert('RGB');im.paste(a,(r['x'],r['y']))
im.save(O/(name+'.png'))
