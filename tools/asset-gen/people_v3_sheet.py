from pathlib import Path
from PIL import Image,ImageDraw
import json
R=Path(__file__).resolve().parents[2];V=R/'art/_review/2026-10-26-people-v3';O=R/'art/models/bayloop_people_v03'
m=json.loads((O/'manifest.json').read_text());sheet=Image.new('RGB',(1440,600),'#e4dfd3');d=ImageDraw.Draw(sheet)
for i,a in enumerate(m['assets']):
 x=i%3*480;y=i//3*300;sheet.paste(Image.open(V/(a['id']+'.png')).convert('RGB').resize((480,270)),(x,y));d.text((x+14,y+275),a['id'],fill='black')
sheet.save(V/'sheet.png')
pal=set(map(tuple,json.loads((R/'art/source/palette/pitcrew_d123_32.json').read_text())['colors']));im=Image.open(O/'PeopleRetroAtlas_v3.png').convert('RGB');colors=set(im.getdata())
(V/'texture-check.json').write_text(json.dumps(dict(passed=not(colors-pal),palette_outside=len(colors-pal),size=im.size,colors=len(colors),photographs=False),indent=2))
