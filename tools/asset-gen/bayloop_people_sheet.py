"""Layout existing actual Blender renders; no generated imagery."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parents[2];O=R/'art/_review/2026-10-26-terrain-people/people'
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',18)
def sheet(name,items,cols):
 w,h=480,294;im=Image.new('RGB',(w*cols,h*((len(items)+cols-1)//cols)),(225,223,214));d=ImageDraw.Draw(im)
 for i,(label,path) in enumerate(items):
  x=i%cols*w;y=i//cols*h;tile=Image.open(O/path).convert('RGB').resize((480,270),Image.Resampling.LANCZOS);im.paste(tile,(x,y));d.text((x+12,y+272),label,font=font,fill=(45,45,42))
 im.save(O/name)
sheet('people-contact.png',[(f'{age} {v:02d}',f'people_{age}_{v:02d}_front.png')for age in ['young','elder']for v in [1,2,3]],3)
sheet('people-motion.png',[(f'{action} frame {f}',f'people_young_01_{action}_{f:02d}.png')for action,frames in [('walk',[1,11,21]),('stand',[1,19,38]),('sit',[1,13,26])]for f in frames],3)
