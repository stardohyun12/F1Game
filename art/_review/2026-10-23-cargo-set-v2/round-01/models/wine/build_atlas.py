"""Authored prop palette layers; CC0 paper is only used for paper labels."""
from pathlib import Path
import json,sys,hashlib
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parent;repo=R.parents[2]
sys.path.insert(0,str(repo/'tools/asset-gen'))
from photo_to_pixel import load_palette,quantize_exact
p=load_palette(repo/'art/source/palette/pitcrew_d123_32.gpl')
def c(rgb):return min(p,key=lambda q:sum((a-b)**2 for a,b in zip(rgb,q)))
fish='fish' in R.name
name=R.name
img=Image.new('RGB',(64,64),c((48,54,47)));d=ImageDraw.Draw(img)
if fish:
 rects={'front':[1,1,26,7],'back':[30,1,26,7],'right':[1,10,20,7],'left':[24,10,20,7],'top':[1,20,26,20],'bottom':[30,20,26,20]}
else:
 rects={'front':[1,1,5,20],'back':[9,1,5,20],'right':[17,1,5,20],'left':[25,1,5,20],'top':[48,1,5,5],'bottom':[41,1,5,5]}
for fi,(face,(x,y,w,h)) in enumerate(rects.items()):
 base=c((216,211,193) if fish else (62+fi*2,74+fi*2,45))
 mid=c((188,186,165) if fish else (82,90,57));bright=c((228,218,194) if fish else (108,111,75))
 d.rectangle((x,y,x+w-1,y+h-1),fill=base)
 # Material structure: moulded EPS bead joins / longitudinal glass value ladder.
 if fish:
  for yy in range(1,h-1,4):
   for xx in range(1+(yy//4)%2,w-1,4):d.point((x+xx,y+yy),fill=mid)
  d.line((x,y+h-1,x+w-1,y+h-1),fill=mid)
 else:
  d.line((x+1,y,x+1,y+h-1),fill=mid)
  d.line((x+w-1,y,x+w-1,y+h-1),fill=bright)
# Real paper photo resized and palette quantized only inside label.
photo=Image.open(repo/'art/source/photos/cargo-box/CardboardSet001_1K-PNG_Color.png').convert('RGB').crop((800,565,1005,705))
lw,lh=(7,5) if fish else (4,8)
paper=quantize_exact(photo.resize((lw,lh),Image.Resampling.BOX),p,'none')
paper=photo.resize((lw,lh),Image.Resampling.BOX).convert('L').point(lambda v: min(255,int(v*.3+180))).convert('RGB')
paper=quantize_exact(paper,p,'none')
img.paste(paper,(3,2) if fish else (1,8));d=ImageDraw.Draw(img)
ink=c((48,54,47));font=ImageFont.truetype(str(repo/'art/models/cargo_box_v01/fonts/Galmuri7.ttf'),8)
if fish:
 for yy in [3,5]:d.line((4,yy,8,yy),fill=ink)
 for xx in [4,6,9]:d.point((xx,6),fill=ink)
 # Small unmistakable paired triangles + stems.
 for xx in [4,9]:
  d.polygon([(xx,11),(xx-1,13),(xx+1,13)],fill=ink);d.line((xx,13,xx,15),fill=ink)
else:
 mask=Image.new('1',(64,64));ImageDraw.Draw(mask).text((1,7),'M',font=font,fill=1);img.paste(ink,(0,0),mask)
 # Neck capsule in a separate region used by geometry.
 d.rectangle((48,1,55,9),fill=c((138,70,49)));d.line((49,1,49,9),fill=c((184,147,96)))
 # Tiny paired orientation symbols on back.
 for xx in [9,12]:
  d.polygon([(xx+1,2),(xx,4),(xx+2,4)],fill=c((228,214,191)));d.point((xx+1,5),fill=c((228,214,191)))
if fish:
 # Dedicated ice and fish patches, no generated imagery or photograph pretence.
 d.rectangle((1,44,26,62),fill=c((136,162,192)));d.line((2,45,25,61),fill=c((228,214,191)))
 for yy in range(46,63,5):d.line((1,yy,26,yy),fill=c((169,182,181)))
 d.rectangle((30,44,57,62),fill=c((105,98,99)))
 d.line((30,49,57,49),fill=c((169,182,181)));d.line((30,55,57,55),fill=c((48,54,47)))
img.save(R/f'{name}_atlas_64.png')
manifest={'name':name,'density_target':64,'texture_size':[64,64],'faces':[{'face':k,'rect_xywh':v} for k,v in rects.items()],'narrow_face_note':'Manufactured dimensions at 64px/m produce surfaces smaller than 8px; no enlarged fake UV region.','layers':['authored material colour ladder','scripted EPS structure or glass strips','scripted label and orientation','contact vertex AO'],'wear':'No global wear/noise. EPS mould seams and glass reflection value strips are material structure.'}
(R/'atlas_manifest.json').write_text(json.dumps(manifest,indent=2))
row={'asset_id':name+'_atlas','path':(R/f'{name}_atlas_64.png').relative_to(repo).as_posix(),'source_url':'https://ambientcg.com/view?id=CardboardSet001; script://'+name+'/build_atlas.py','license':'CC0-1.0; Project-authored; Galmuri OFL-1.1','accessed':'2026-10-23','tool':'photo_to_pixel.quantize_exact; build_atlas.py; Blender bpy','ai_generated':'no','sha256':hashlib.sha256((R/f'{name}_atlas_64.png').read_bytes()).hexdigest(),'notes':'Prop hand-pixel material and information layers, D123 rule2. CC0 paper photograph brightened only under label. No AI image used. Reference colors only.'}
(R/'provenance-row.json').write_text(json.dumps(row,indent=2))
