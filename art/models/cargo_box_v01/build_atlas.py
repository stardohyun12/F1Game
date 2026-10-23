"""Real CC0 cardboard patches + intentional packing marks, native texel grid."""
from pathlib import Path
import subprocess,sys,json,hashlib
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parent;repo=R.parents[2]
sys.path.insert(0,str(repo/'tools/asset-gen'))
from photo_to_pixel import load_palette,quantize_exact
palette=repo/'art/source/palette/pitcrew_d123_32.json';p=json.loads(palette.read_text());colors=load_palette(palette)
roles={e['name']:tuple(e['rgb']) for e in p['entries']}
source=repo/'art/source/photos/cargo-box/CardboardSet001_1K-PNG_Color.png'
R.joinpath('patches').mkdir(exist_ok=True)
faces=[('front',.4,.3,[2,2,13,10]),('back',.4,.3,[19,2,13,10]),('right',.3,.3,[36,2,10,10]),('left',.3,.3,[49,2,10,10]),('top',.4,.3,[2,18,13,10]),('bottom',.4,.3,[19,18,13,10])]
atlas=Image.new('RGB',(64,64),colors[0]);records=[]
for name,w,h,rect in faces:
 patch=R/'patches'/f'{name}.png'
 subprocess.run([sys.executable,str(repo/'tools/asset-gen/photo_to_pixel.py'),str(source),str(patch),'--palette',str(palette),'--width-m',str(w),'--height-m',str(h),'--density','32','--dither','none','--resample','box','--source-id','ambientCG/CardboardSet001','--source-url','https://ambientcg.com/view?id=CardboardSet001','--license-url','https://docs.ambientcg.com/license/','--crop','55','145','220','235'],check=True,stdout=subprocess.DEVNULL)
 im=Image.open(patch).convert('RGB');d=ImageDraw.Draw(im);pw,ph=im.size
 if name=='front':
  # Galmuri7 is authored at 8px. No shrink, AA, fake address, or invented barcode.
  d.rectangle((2,1,7,8),fill=roles['label'])
  mask=Image.new('1',im.size);md=ImageDraw.Draw(mask);font=ImageFont.truetype(str(R/'fonts/Galmuri7.ttf'),8)
  md.text((3,0),'1',font=font,fill=1);im.paste(roles['ink'],(0,0),mask)
 elif name=='right':
  # Filled triangular arrowheads distinguish UP from a cross at native scale.
  for x in [0,6]:
   d.rectangle((x+1,1,x+2,1),fill=roles['stamp'])
   d.rectangle((x,2,x+3,3),fill=roles['stamp'])
   d.rectangle((x+1,4,x+2,7),fill=roles['stamp'])
  d.line((0,9,9,9),fill=roles['stamp'])
 elif name=='top':
  d.rectangle((6,0,7,ph-1),fill=roles['tape']);d.line((0,5,pw-1,5),fill=roles['carton_dark'])
 elif name=='bottom':
  d.rectangle((6,0,7,ph-1),fill=roles['tape'])
  # Wear only at contact corners, no global procedural noise.
  for box in [(0,0,2,0),(0,0,0,1),(pw-3,ph-1,pw-1,ph-1)]:d.rectangle(box,fill=roles['carton_dark'])
 elif name=='back':d.line((pw//2,0,pw//2,2),fill=roles['tape'])
 elif name=='left':d.point((0,ph-1),fill=roles['carton_light'])
 im=quantize_exact(im,colors);im.save(R/'patches'/f'{name}_marked.png')
 x,y,tw,th=rect
 assert im.size==(tw,th)
 # One-pixel gutters are extruded duplicates, not new texels allocated on mesh.
 padded=Image.new('RGB',(tw+2,th+2));padded.paste(im,(1,1))
 for xx in range(tw):padded.putpixel((xx+1,0),im.getpixel((xx,0)));padded.putpixel((xx+1,th+1),im.getpixel((xx,th-1)))
 for yy in range(th):padded.putpixel((0,yy+1),im.getpixel((0,yy)));padded.putpixel((tw+1,yy+1),im.getpixel((tw-1,yy)))
 for xx,yy,sx,sy in [(0,0,0,0),(tw+1,0,tw-1,0),(0,th+1,0,th-1),(tw+1,th+1,tw-1,th-1)]:padded.putpixel((xx,yy),im.getpixel((sx,sy)))
 atlas.paste(padded,(x-1,y-1));records.append({'face':name,'rect_xywh':rect,'physical_wh_m':[w,h],'density_xy':[tw/w,th/h],'source_metadata':f'patches/{name}.json'})
atlas.save(R/'cargo_box_atlas_64.png')
manifest={'name':'cargo_box_v01','dimensions_whd_m':[.4,.3,.3],'dimension_basis':'TART manufacturer external 400x300x300mm; not physically measured by us','texture':'cargo_box_atlas_64.png','texture_size':[64,64],'density_target':32,'faces':records,'source_photo_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'palette_sha256':hashlib.sha256(palette.read_bytes()).hexdigest(),'font':'Galmuri7 8px, real route number 1; no AI text','artwork_provenance':'CC0 Surface Photogrammetry photo + scripted label/tape/arrows only. Reference image used solely to sample palette.'}
(R/'atlas_manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print('64x64 atlas, six unique UV rectangles, native 13x10 / 10x10 texels')
