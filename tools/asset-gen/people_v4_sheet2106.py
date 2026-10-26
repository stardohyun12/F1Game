from pathlib import Path
from PIL import Image,ImageDraw
import json
R=Path(__file__).resolve().parents[2];V=R/'art/_review/2026-10-26-people-v4-2106';O=R/'art/models/bayloop_people_v04'
assets=json.loads((O/'manifest.json').read_text())['assets'];sheet=Image.new('RGB',(1920,2880),'#e8dfcc');d=ImageDraw.Draw(sheet)
for i,a in enumerate(assets):
 for j,view in enumerate(['front','threequarter','side','walk']):
  x=j*480;y=i*300;im=Image.open(V/(a['id']+'_'+view+'.png')).convert('RGB');sheet.paste(im,(x,y));d.text((x+12,y+275),a['id']+' '+view,fill='black')
im=Image.open(V/'town_480.png').convert('RGB');big=im.resize((1920,1080),Image.Resampling.NEAREST);big.save(V/'town_480_nearest4x.png');sheet.paste(big,(0,1800));sheet.save(V/'sheet.png')
P=set(map(tuple,json.loads((R/'art/source/palette/pitcrew_d123_32.json').read_text())['colors']));im=Image.open(O/'PeoplePS1Atlas_v4.png').convert('RGB');colors=set(im.getdata());(V/'texture-check.json').write_text(json.dumps(dict(passed=not(colors-P),palette_outside=len(colors-P),size=im.size,colors=len(colors),photographs=False),indent=2))
