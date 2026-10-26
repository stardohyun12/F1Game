from pathlib import Path
from PIL import Image,ImageDraw
import json
R=Path(__file__).resolve().parents[2];V=R/'art/_review/2026-10-26-people-v4';O=R/'art/models/bayloop_people_v04'
m=json.loads((O/'manifest.json').read_text());sheet=Image.new('RGB',(1920,1800),'#e4dfd3');d=ImageDraw.Draw(sheet)
for i,a in enumerate(m['assets']):
 for j,view in enumerate(['front','threequarter','side','walk']):
  x=j*480;y=i*300;sheet.paste(Image.open(V/(a['id']+'_'+view+'.png')).convert('RGB').resize((480,270)),(x,y));d.text((x+14,y+275),a['id']+' '+view,fill='black')
sheet.save(V/'sheet.png')
frames=sorted(V.glob('walkproof_*.png'))
if frames:
 ims=[Image.open(p).convert('RGB') for p in frames];ims[0].save(V/'walkproof.gif',save_all=True,append_images=ims[1:],duration=83,loop=0)
 strip=Image.new('RGB',(960,360))
 for j,k in enumerate([0,4,8,12]):strip.paste(ims[k],(j*240,0))
 strip.save(V/'walkproof-contact.png')
P=set(map(tuple,json.loads((R/'art/source/palette/pitcrew_d123_32.json').read_text())['colors']));im=Image.open(O/'PeoplePS1Atlas_v4.png').convert('RGB');colors=set(im.getdata());(V/'texture-check.json').write_text(json.dumps(dict(passed=not(colors-P),palette_outside=len(colors-P),size=im.size,colors=len(colors),photographs=False),indent=2))
