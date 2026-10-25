import sys,os
from PIL import Image,ImageDraw,ImageFont
f=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',26)
keys=sys.argv[2].split(',');cols=3;tw,th=640,427
rows=(len(keys)+2)//3;s=Image.new('RGB',(cols*tw,rows*th));d=ImageDraw.Draw(s)
for i,k in enumerate(keys):
  im=Image.open('_cand/'+k+'.jpg').convert('RGB');im.thumbnail((tw-4,th-4));x,y=i%3*tw,i//3*th;s.paste(im,(x+2,y+2));d.text((x+8,y+4),k,font=f,fill='yellow',stroke_width=2,stroke_fill='black')
s.save(sys.argv[1],quality=88)
