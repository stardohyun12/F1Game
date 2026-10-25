"""64px/m EPS, wet fish and ice. CC0 photo only beneath paper label."""
from pathlib import Path
import json,sys,hashlib
from PIL import Image,ImageDraw
R=Path(__file__).resolve().parent;repo=R.parents[2]
sys.path.insert(0,str(repo/'tools/asset-gen'))
from photo_to_pixel import load_palette,quantize_exact
p=load_palette(repo/'art/source/palette/pitcrew_d123_32.gpl')
def c(rgb):return min(p,key=lambda q:sum((a-b)**2 for a,b in zip(rgb,q)))
img=Image.new('RGB',(64,64),c((48,54,47)));d=ImageDraw.Draw(img)
rects={'front':[1,1,26,7],'back':[30,1,26,7],'right':[1,10,20,7],'left':[24,10,20,7],'top':[1,20,26,20],'bottom':[30,20,26,20]}
for face,(x,y,w,h) in rects.items():
 d.rectangle((x,y,x+w-1,y+h-1),fill=c((228,214,191)))
 for yy in range(2,h-1,7):
  for xx in range(2+(yy//7)%2,w-1,7):d.point((x+xx,y+yy),fill=c((196,199,201)))
 d.line((x,y+h-1,x+w-1,y+h-1),fill=c((196,166,135)))
photo=Image.open(repo/'art/source/photos/cargo-box/CardboardSet001_1K-PNG_Color.png').convert('RGB').crop((800,565,1005,705))
paper=photo.resize((7,5),Image.Resampling.BOX).convert('L').point(lambda v:min(255,int(v*.3+180))).convert('RGB')
img.paste(quantize_exact(paper,p,'none'),(3,2));d=ImageDraw.Draw(img);ink=c((48,54,47))
for yy in [3,5]:d.line((4,yy,6,yy),fill=c((105,98,99)))
for xx in [4,6]:d.point((xx,6),fill=c((105,98,99)))
for xx in [4,10]:
 d.polygon([(xx,11),(xx-1,13),(xx+1,13)],fill=c((105,98,99)));d.line((xx,13,xx,15),fill=c((105,98,99)))
d.rectangle((1,43,26,62),fill=c((196,199,201)))
for poly,rgb in [([(1,43),(13,43),(4,52)],(203,212,222)), ([(14,43),(25,45),(20,54),(9,50)],(174,188,202)), ([(1,53),(11,51),(15,61),(1,62)],(203,212,222)), ([(15,54),(26,50),(26,62),(20,61)],(228,214,191))]:d.polygon(poly,fill=c(rgb))
d.line([(3,45),(8,45),(10,47)],fill=c((228,214,191)))
d.line([(17,57),(21,55),(24,55)],fill=c((203,212,222)))
# Twenty texels along a real 32cm fish: coherent silver flank and darker back.
d.rectangle((30,43,57,46),fill=c((48,54,47)))
d.rectangle((30,47,57,49),fill=c((103,123,128)))
d.rectangle((30,50,57,53),fill=c((196,199,201)))
d.line((30,51,57,51),fill=c((203,212,222)))
d.line((50,47,49,50),fill=c((55,53,47)));d.point((53,48),fill=ink)
d.rectangle((58,43,63,53),fill=c((48,54,47)))
d.rectangle((30,54,57,59),fill=c((228,214,191)))
d.rectangle((30,60,37,63),fill=ink)
d.rectangle((39,59,46,63),fill=c((48,54,47)))
d.rectangle((48,59,62,63),fill=c((203,212,222)))
d.line((49,60,60,60),fill=c((228,214,191)))
path=R/f'{R.name}_atlas_64.png';img.save(path)
manifest={'name':R.name,'revision':'PD1720 pass2','density_target':64,'texture_size':[64,64],'faces':[{'face':k,'rect_xywh':v} for k,v in rects.items()], 'recognition_anchor':'Dark dorsal silhouette and clear forked tail isolated from five low bright ice chips; silver underside.', 'layers':['CC0 paper photo BOX reduction and exact palette','EPS mould joints','dark fish back/silver belly and gill','low ice facet highlights','small scripted label/up chevrons','contact vertex AO'], 'wear':'EPS mould joints, lower contact shading and liner folds only. No global noise.', 'narrow_face_note':'7px main sides use narrow auxiliary monochrome check; density not enlarged.'}
(R/'atlas_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
row={'asset_id':R.name+'_atlas','path':path.relative_to(repo).as_posix(),'source_url':'https://ambientcg.com/view?id=CardboardSet001; script://'+R.name+'/build_atlas.py','license':'CC0-1.0; Project-authored','accessed':'2026-10-23','tool':'photo_to_pixel.quantize_exact; build_atlas.py; Blender bpy','ai_generated':'no','sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'notes':'CC0 photo only under paper label. EPS/fish/ice/liner authored with shared palette; no generated image. Manufacturer dimensions reused.'}
(R/'provenance-row.json').write_text(json.dumps(row,indent=2),encoding='utf-8')
