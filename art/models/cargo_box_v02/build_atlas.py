"""Native 32px/m cardboard, small scripted shipping marks, no generated texture."""
from pathlib import Path
import subprocess,sys,json,hashlib,csv
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parent;repo=R.parents[2];sys.path.insert(0,str(repo/'tools/asset-gen'))
from photo_to_pixel import load_palette
palette=repo/'art/source/palette/pitcrew_d123_32.json';data=json.loads(palette.read_text());colors=load_palette(palette)
c={e['name']:tuple(e['rgb']) for e in data['entries']};source=repo/'art/source/photos/cargo-box/CardboardSet001_1K-PNG_Color.png'
(R/'patches').mkdir(exist_ok=True)
faces=[('front',.4,.3,[2,2,13,10]),('back',.4,.3,[19,2,13,10]),('right',.3,.3,[36,2,10,10]),('left',.3,.3,[49,2,10,10]),('top',.4,.3,[2,18,13,10]),('bottom',.4,.3,[19,18,13,10])]
atlas=Image.new('RGB',(64,64),c['carton_mid']);records=[]
for name,w,h,rect in faces:
 path=R/'patches'/f'{name}.png'
 subprocess.run([sys.executable,str(repo/'tools/asset-gen/photo_to_pixel.py'),str(source),str(path),'--palette',str(palette),'--width-m',str(w),'--height-m',str(h),'--density','32','--dither','none','--resample','box','--source-id','ambientCG/CardboardSet001','--source-url','https://ambientcg.com/view?id=CardboardSet001','--license-url','https://docs.ambientcg.com/license/','--crop','55','145','220','235'],check=True,stdout=subprocess.DEVNULL)
 im=Image.open(path).convert('RGB');d=ImageDraw.Draw(im)
 if name=='front':
  # 7x5 texels = 219x156mm; reduced height vs v01 6x8 label.
  # Barcodes/address RULES are symbolic pixels explicitly requested by PD, not fake text.
  d.rectangle((3,4,9,8),fill=c['label'])
  for x in [3,5]:d.line((x,5,x,7),fill=c['ink'])
  for y,end in [(4,9),(6,8),(8,9)]:d.line((7,y,end,y),fill=c['paper_fold'])
 elif name=='right':
  # Small ink-only directions + a separate stemmed glass pictogram.
  for x in [1,5]:
   d.point((x+1,5),fill=c['ink']);d.line((x,6,x+2,6),fill=c['ink']);d.line((x+1,7,x+1,8),fill=c['ink'])
 elif name=='back':d.line((11,1,11,8),fill=c['paper_fold'])
 elif name=='left':
  d.line((0,7,1,8),fill=c['carton_light'])
  d.line((4,5,4,6),fill=c['ink']);d.line((6,5,6,6),fill=c['ink']);d.point((5,6),fill=c['ink']);d.point((5,7),fill=c['ink']);d.line((4,8,6,8),fill=c['ink'])
 elif name=='top':
  # Fold seam aligns with real separate flap geometry; tape is separate mesh.
  d.line((6,0,6,9),fill=c['carton_dark'])
 elif name=='bottom':
  d.rectangle((6,0,7,9),fill=c['tape']);d.line((0,9,3,9),fill=c['carton_dark'])
 im.save(R/'patches'/f'{name}_marked.png');x,y,tw,th=rect
 # Extrude gutters. Keep all authored pixels on the native grid.
 for yy in range(-1,th+1):
  for xx in range(-1,tw+1):atlas.putpixel((x+xx,y+yy),im.getpixel((max(0,min(tw-1,xx)),max(0,min(th-1,yy)))))
 records.append({'face':name,'rect_xywh':rect,'source_metadata':f'patches/{name}.json'})
# Shared flat color patches for sub-texel-width physical bevels and tape.
ad=ImageDraw.Draw(atlas);ad.rectangle((1,40,20,62),fill=c['tape']);ad.rectangle((23,40,45,62),fill=c['carton_light'])
atlas.save(R/'cargo_box_atlas_64.png')
# Real font evidence, review-only. Never silently bake unreadable reduced Korean.
font=ImageFont.truetype(str(repo/'art/_review/2026-10-23-d123/fonts/Galmuri11.ttf'),12)
label=Image.new('RGB',(32,24),c['label']);ld=ImageDraw.Draw(label)
mask=Image.new('1',label.size);ImageDraw.Draw(mask).text((1,0),'조심',font=font,fill=1);label.paste(c['ink'],(0,0),mask)
for x in [1,3,4,7,9,12,14]:ld.line((x,15,x,21),fill=c['ink'])
for y,end in [(15,30),(18,27),(21,29)]:ld.line((18,y,end,y),fill=c['paper_fold'])
label.save(R/'label-font-candidate_NOT_APPLIED.png')
manifest={'name':'cargo_box_v02','dimensions_m':[.4,.3,.3],'density':32,'faces':records,'font_candidate':'Galmuri11 native12px 조심; review only NOT APPLIED. Await density exception; model label has symbolic bars/lines only.','source_photo_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'palette_sha256':hashlib.sha256(palette.read_bytes()).hexdigest(),'wear':'upper front-left crush, bottom contact edge only; photo fibre is material variation, not added random wear'}
(R/'atlas_manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
# Preserve existing records. Register only this newly produced, non-AI game texture.
ledger=repo/'art/provenance.csv'
with ledger.open(encoding='utf-8-sig',newline='') as f:reader=csv.DictReader(f);fields=reader.fieldnames;rows=list(reader)
rows=[r for r in rows if r['asset_id']!='cargo_box_v02_atlas']
rows.append(dict(zip(fields,['cargo_box_v02_atlas','art/models/cargo_box_v02/cargo_box_atlas_64.png','https://ambientcg.com/view?id=CardboardSet001','CC0-1.0','2026-10-23','photo_to_pixel.py; cargo_box_v02/build_atlas.py','no',hashlib.sha256((R/'cargo_box_atlas_64.png').read_bytes()).hexdigest(),'CC0 photo cardboard + scripted barcode/address lines/arrows/glass. No Korean glyph baked. Reference sampled colors only.'])))
with ledger.open('w',encoding='utf-8',newline='') as f:writer=csv.DictWriter(f,fieldnames=fields);writer.writeheader();writer.writerows(rows)
print('v02 64px atlas and provenance record written')
