from pathlib import Path
from PIL import Image,ImageDraw
import json
R=Path(__file__).resolve().parents[2];V=R/'art/_review/2026-10-26-people-v2';O=R/'art/models/bayloop_people_v02'
m=json.loads((O/'manifest.json').read_text());sheet=Image.new('RGB',(1440,920),'#deddd6');draw=ImageDraw.Draw(sheet)
for i,a in enumerate(m['assets']):
 im=Image.open(V/(a['id']+'.png')).convert('RGB');im=im.resize((480,270));x=i%3*480;y=i//3*300;sheet.paste(im,(x,y));draw.text((x+12,y+273),a['id'],fill='black')
for i,(age,action) in enumerate([('young','Walk'),('young','Sit'),('elder','Walk')]):
 im=Image.open(V/f'people_{age}_01_v2_{action}.png').convert('RGB').resize((480,270));sheet.paste(im,(i*480,610));draw.text((i*480+12,887),age+' '+action,fill='black')
sheet.save(V/'sheet.png')
pal=set(map(tuple,json.loads((R/'art/source/palette/pitcrew_d123_32.json').read_text())['colors']));im=Image.open(O/'PeopleRetroAtlas.png').convert('RGB');outside=set(im.getdata())-pal
result={'palette_outside':len(outside),'size':im.size,'colors':len(set(im.getdata())),'photographs':False,'pass':not outside and im.size==(64,64)}
(V/'texture-check.json').write_text(json.dumps(result,indent=2));print(result)
