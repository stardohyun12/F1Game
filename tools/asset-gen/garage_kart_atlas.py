"""Original painted proposal, no photos/AI. Not the art2 shared generators."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageFilter
import json
R=Path(__file__).resolve().parents[2];O=R/'art/models/garage_kart_v01';V=R/'art/_review/2026-10-27-garage-kart';O.mkdir(parents=True,exist_ok=True);V.mkdir(parents=True,exist_ok=True)
colors=['#d6bd96','#b47a5c','#98774f','#426767','#8b9b98','#364b50','#af604d','#a18b68','#ded1b2','#b0a28c','#dbc9a6','#637c73','#417579','#896f4f','#426568','#a68b61']
atlas=Image.new('RGB',(512,512));fontpath=R/'art/_review/2026-10-23-course-ui/fonts/Galmuri11.ttf';font=ImageFont.truetype(str(fontpath),14);small=ImageFont.truetype(str(fontpath),10)
for t,color in enumerate(colors):
 im=Image.new('RGB',(128,128),color)
 if t<10 or t==15:
  wash=Image.new('RGBA',(128,128));d=ImageDraw.Draw(wash)
  for box,fill in [((8,14,110,36),(235,220,183,54)),((-15,71,92,101),(40,73,73,42)),((80,20,150,120),(225,211,170,36))]:d.ellipse(box,fill=fill)
  im=Image.alpha_composite(im.convert('RGBA'),wash.filter(ImageFilter.GaussianBlur(2.5))).convert('RGB')
 d=ImageDraw.Draw(im)
 if t==10:
  d.rectangle((7,7,120,120),outline='#ab795b',width=2);d.text((16,12),'COAST CUP',font=small,fill='#455f63');d.line((12,91,116,46),fill='#57797c',width=12);d.polygon([(30,70),(91,50),(84,65),(40,83)],fill='#ae604e')
  for x,y in [(31,75),(78,58)]:d.rectangle((x,y,x+12,y+7),fill='#374e52')
  d.text((18,104),'27  /  AUTUMN',font=small,fill='#5c675b')
 if t==11:
  for i,label in enumerate(['차','실력','응원']):
   x=5+i*41;d.text((x+11,12),label,font=small,fill='#e4d4b2');d.rectangle((x,36,x+35,106),outline='#ddc9a5',width=2)
 if t==12:d.text((11,32),'항구 정비소',font=font,fill='#f1dfb9');d.text((20,66),'PITCREW',font=small,fill='#e5cda2')
 if t==13:
  for x in [22,49,78,105]:d.line((x,20,x,105),fill='#5a5546',width=2)
 if t==14:
  d.text((17,9),'구간 기록',font=font,fill='#dfd3b4')
  for y,txt in [(39,'01  01:42'),(64,'02  01:37'),(89,'03  --:--')]:d.text((13,y),txt,font=small,fill='#d8cba8')
 atlas.paste(im,(t%4*128,t//4*128))
atlas.save(O/'GarageKartPaint.png');(O/'paint-source.json').write_text(json.dumps(dict(method='authored large soft paint strokes and drawn information, no photos/no AI',colors=colors,font=str(fontpath.relative_to(R)),license='original artwork; Galmuri SIL OFL 1.1'),indent=2))
# Pre-build composition drawing, approval proposal, not an approved production drawing.
im=Image.new('RGB',(1200,700),'#eee3cd');d=ImageDraw.Draw(im);f=ImageFont.truetype(str(fontpath),22)
d.text((35,20),'항구 정비소 + 첫 배달 고카트 / 승인용 구성',font=f,fill='#365657')
d.rectangle((70,130,640,520),fill='#cfb68f',outline='#776d55',width=4);d.polygon([(50,130),(355,58),(660,130)],fill='#b7785d');d.rectangle((175,245,490,520),fill='#426767');d.rectangle((185,260,480,310),fill='#77928d');d.rectangle((525,345,590,520),fill='#6b7d71')
d.text((75,555),'8m × 7m / 셔터 4.4m / 작업·꿈의 벽',font=f,fill='#365657');d.rectangle((745,355,1090,406),fill='#b56550');d.rectangle((900,265,980,353),fill='#536c66');d.rectangle((1010,282,1110,350),fill='#b69a70')
for x in [770,1020]:d.ellipse((x,375,x+75,450),fill='#3b5358')
d.text((720,500),'950mm 축간 / 뒤 작은 짐상자',font=f,fill='#365657');d.text((720,545),'차 A와 동일: 1단위 = 1m',font=f,fill='#365657');d.text((40,640),'도현 승인 필요 · 제작 방식 확인용 bpy 미리보기까지',font=f,fill='#9f604d');im.save(V/'concept.png')
